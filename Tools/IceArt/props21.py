"""Japan theme (a 1960s-70s Shinto shrine and temple garden in spring): vermilion torii gates (the landmark: a big
myojin torii behind each spawn, rows of small torii over the stone paths in the arena), stone lanterns, a komainu
guardian, cherry blossom trees, cloud-pruned black pines, bamboo thickets as the hiding bush, sake barrels, white plaster
walls under grey tile roofs, castle stone walls, mossy garden rocks. Original designs in the chunky Brawl style, same
palette as the other themes. The palette has no pink: blossom faces go to material slot 1 on the sand ramp and that slot
is SakuraPink.mat in Unity (the lit props material tinted pink, like the baobab bark).

Walls ('#'): IshigakiStones, BambooFenceWall, SakeBarrels, PlasterWall, MossRock (one family per wall).
Small ('x'): StoneLanternLow, WaterBasin, OfferingBox, TaruStack.
Fence ('f'): BambooFence.
Tall ('t'): SakuraTree, BlackPine, KasugaLantern, Komainu.
Bush ('b'): BambooGrass (a thicket of culms with leaf sprays).
Decor: SteppingStones, MossTuft, PetalScatter, ZenStones.
Torii paths: ToriiSmall, PathSlab (placed by the builder over the speed paths).
Border: ShrineWallA/B/C (earthen walls under tile roofs on a stone base; B with a cherry tree behind, C with a belfry).
Gate: ToriiGate. Backdrop: Pagoda, ShrineHall, SnowPeak."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import *
from props3 import blob
from props7 import tube
from props8 import Kit, rbox, sphere
from props16 import SKit
from props20 import bezier, pad, UP, disc_facing

PINK_TINT = (1.0, 0.70, 1.15)   # Blender preview of SakuraPink (slot 1 on the sand ramp)

def pink_preview(ob):
    """Blender preview only, called by build_all AFTER the FBX is exported (see props18.pink_preview)"""
    if len(ob.data.materials) < 2: return ob
    m = ob.data.materials[1].copy(); m.name = 'XmasGlowSakura'
    nt = m.node_tree; bsdf = nt.nodes['Principled BSDF']; tex = nt.nodes['Image Texture']
    mix = nt.nodes.new('ShaderNodeMixRGB'); mix.blend_type = 'MULTIPLY'; mix.inputs['Fac'].default_value = 1.0
    mix.inputs['Color2'].default_value = PINK_TINT + (1.0,)
    nt.links.new(tex.outputs['Color'], mix.inputs['Color1']); nt.links.new(mix.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Emission Strength'].default_value = 0.0
    ob.data.materials[1] = m
    return ob

def tagged(ob):
    ob['sakura_preview'] = 1
    return ob

# ------------------------------------------------------------------ colours (by facing, never by the light)
def stonec(f):
    """weathered granite: lighter on top, a little moss green where it faces up and is rough"""
    n = f.normal
    if n.z > 0.6: return 'ash'
    return 'bone_dark' if n.z > -0.3 else 'basalt'
def gravel(f): return 'ash' if f.normal.z > 0.5 else 'bone_dark'
def wood(f): return 'wood' if f.normal.z > -0.3 else 'wood_dark'
def tiles(f): return 'stone' if f.normal.z > 0.2 else 'stone_dark'   # grey kawara roof tiles

def skirt(k, sx, sy, seed, h=0.05, x=0.0, y=0.0):
    """raked gravel heaped round the foot of a piece"""
    k.add(lambda bm: blob(bm, 1.0, (x, y, 0), (sx, sy, h), 3, 0.02, seed), gravel)

def moss_cap(k, c, sc, seed):
    k.grad(lambda bm: pad(bm, Vector(c), sc[0], sc[1], sc[2], 0.0, seed), 'papyrus_grad', lambda v, z=c[2], h=sc[2]: 0.15 + 0.4 * min(1.0, max(0.0, (v.co.z - z + h) / (2 * h))))

def sweep_box(bm, pts, w, h):
    """a rectangular beam along pts: w across (horizontal), h tall; smooth along its length, crisp corners"""
    n = len(pts); rings = []
    for i, p in enumerate(pts):
        d = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        side = d.cross(UP).normalized(); u = side.cross(d).normalized()
        cs = [p + side * w / 2 + u * h / 2, p - side * w / 2 + u * h / 2, p - side * w / 2 - u * h / 2, p + side * w / 2 - u * h / 2]
        rings.append([bm.verts.new(c) for c in cs])
    fs = []
    for i in range(n - 1):
        for j in range(4):
            fs.append(bm.faces.new((rings[i][j], rings[i][(j + 1) % 4], rings[i + 1][(j + 1) % 4], rings[i + 1][j])))
    fs.append(bm.faces.new(rings[0])); fs.append(bm.faces.new(list(reversed(rings[-1]))))
    bmesh.ops.recalc_face_normals(bm, faces=fs)

def curved(x0, x1, z, rise, n=21, power=2.6, y=0.0):  # noqa
    """points along x with the ends turned up (the 'sori' of a torii lintel or a temple roof)"""
    L = max(abs(x0), abs(x1))
    return [Vector((x, y, z + rise * (abs(x) / L) ** power)) for x in (x0 + (x1 - x0) * i / (n - 1) for i in range(n))]

def hip_roof(k, cx, cy, z, W, D, h, lift, col=tiles, under='wood_dark', th=0.12, n=8):
    """a hipped tile roof (eaves W x D, ridge h above) whose eaves curve up towards the corners, a fascia and soffit"""
    rl = max(0.0, (W - D) / 2)
    loop = []
    corners = [(-W / 2, -D / 2), (W / 2, -D / 2), (W / 2, D / 2), (-W / 2, D / 2)]
    for e in range(4):
        a, b = corners[e], corners[(e + 1) % 4]
        for i in range(n):
            t = i / n; x = a[0] + (b[0] - a[0]) * t; y = a[1] + (b[1] - a[1]) * t
            u = abs(t - 0.5) * 2 if i else 1.0
            loop.append((x, y, lift * u ** 3))
    def ridge_of(x, y):
        if abs(y) >= D / 2 - 1e-6 and abs(x) < W / 2 - 1e-6: return (max(-rl, min(rl, x)), 0.0)
        return ((rl if x > 0 else -rl), 0.0)
    def build(bm):
        rv = {}
        def R(p):
            key = (round(p[0], 4), round(p[1], 4))
            if key not in rv: rv[key] = bm.verts.new((cx + p[0], cy + p[1], z + h))
            return rv[key]
        top = [bm.verts.new((cx + x, cy + y, z + dz)) for (x, y, dz) in loop]
        low = [bm.verts.new((cx + x, cy + y, z + dz - th)) for (x, y, dz) in loop]
        N = len(loop)
        for i in range(N):
            j = (i + 1) % N
            ra, rb = ridge_of(loop[i][0], loop[i][1]), ridge_of(loop[j][0], loop[j][1])
            if ra == rb: bm.faces.new((top[i], top[j], R(ra)))
            else: bm.faces.new((top[i], top[j], R(rb), R(ra)))
            bm.faces.new((low[i], low[j], top[j], top[i]))
        bm.faces.new(list(reversed(low)))
    k.add(build, lambda f: under if f.normal.z < -0.5 else (col(f) if callable(col) else col))
    if rl > 0:   # the ridge: a rounded tile cap with raised ends
        k.add(lambda bm: tube(bm, [Vector((cx - rl - 0.05, cy, z + h + 0.05)), Vector((cx, cy, z + h + 0.02)), Vector((cx + rl + 0.05, cy, z + h + 0.05))], 0.09 * max(1.0, h), 0.09 * max(1.0, h), 8), 'stone_dark')

def hex_roof(k, x, y, z, R, h, lift, col='ash', segs=24):
    """a hexagonal stone-lantern roof: straight eave edges, corners curling up, a little dome on top"""
    def rim(i, kk, rr, zz):
        a = i * 2 * math.pi / segs; sec = math.pi / 3
        off = (a % sec) - sec / 2
        if kk == 0 or kk == 1:
            rr = rr * math.cos(sec / 2) / math.cos(off)
            corner = (abs(off) / (sec / 2)) ** 4
            zz = zz + lift * corner
        return rr, zz
    k.add(lambda bm: lathe(bm, [(R * 0.9, -0.06 * R), (R, 0.0), (R * 0.42, h * 0.75), (R * 0.12, h * 0.95), (0.0, h)], segs, rim, 0, Matrix.Translation((x, y, z))), col)

# ------------------------------------------------------------------ torii
def torii(k, X, H, R, lint_ext, rise, detail=True, seed=0):
    """a myojin torii: two vermilion pillars leaning in a little on black sleeves and stone footings, the nuki tie beam
    with wedges, the vermilion shimaki and the black kasagi above it curving up at the ends, the central strut with its
    plaque. X = half the pillar spacing, H = pillar height, R = pillar radius."""
    segs = 24 if detail else 10
    lean = 0.025
    for s in (-1, 1):
        x0 = s * X
        M = Matrix.Translation((x0, 0, 0)) @ Matrix.Shear('XY', 4, (-s * lean, 0.0))
        if detail:
            k.add(lambda bm, x0=x0: lathe(bm, [(R * 1.55, 0.0), (R * 1.5, 0.18 * H / 7), (R * 1.3, 0.26 * H / 7), (0.0, 0.26 * H / 7)], segs, None, 0, Matrix.Translation((x0, 0, 0))), 'ash')   # stone footing
        z0 = 0.26 * H / 7 if detail else 0.0
        k.add(lambda bm, M=M, z0=z0: lathe(bm, [(R * 1.12, z0), (R * 1.12, z0 + 0.75 * H / 7), (R * 1.04, z0 + 0.8 * H / 7), (0.0, z0 + 0.8 * H / 7)], segs, None, 0, M), 'black')   # sleeve
        k.add(lambda bm, M=M, z0=z0: lathe(bm, [(R, z0 + 0.78 * H / 7), (R * 0.93, H), (0.0, H)], segs, None, 0, M), 'red')
        if detail:   # daiwa: the ring where the pillar meets the shimaki
            xt = x0 - s * lean * H
            k.add(lambda bm, xt=xt: lathe(bm, [(R * 1.12, H - 0.25), (R * 1.12, H - 0.05), (0.0, H - 0.05)], segs, None, 0, Matrix.Translation((xt, 0, 0))), 'black')
    zn = H * 0.76
    Lx = X + R * 3.2
    k.add(lambda bm: rbox(bm, (2 * Lx, R * 1.0, R * 1.3), (0, 0, zn - R * 0.65), 0.02 * R / 0.4, segs=1), 'red')   # nuki
    if detail:
        for s in (-1, 1):
            xs = s * (X - lean * zn + R * 1.15)
            k.add(lambda bm, xs=xs: rbox(bm, (R * 0.35, R * 1.15, R * 1.05), (xs, 0, zn - R * 0.52), 0.01, segs=1), 'black')   # kusabi wedges
    zt = H                                      # top of the pillars
    L2 = X + lint_ext
    nc = 21 if detail else 9                    # points along the curved beams (dozens of small torii stand in a map)
    sh = R * 0.95; kh = R * 1.15
    k.add(lambda bm: sweep_box(bm, curved(-L2 * 0.94, L2 * 0.94, zt + sh / 2, rise * 0.7, nc), R * 1.25, sh), 'red')          # shimaki
    k.add(lambda bm: sweep_box(bm, curved(-L2, L2, zt + sh + kh / 2, rise, nc), R * 1.55, kh), 'black')                  # kasagi
    k.add(lambda bm: sweep_box(bm, curved(-L2 * 1.01, L2 * 1.01, zt + sh + kh + R * 0.06, rise * 1.02, nc), R * 1.6, R * 0.12), 'black')   # its thin top board
    k.add(lambda bm: rbox(bm, (R * 0.9, R * 0.75, zt - zn), (0, 0, zn), 0.02, segs=1), 'red')                        # gakuzuka strut
    if detail:   # the plaque: a black panel in a gold frame (no lettering), a small gold cap on top
        zp = (zn + zt) / 2 - R * 1.6
        k.add(lambda bm: rbox(bm, (R * 2.9, R * 0.22, R * 3.6), (0, -R * 0.42, zp - R * 0.15), 0.03, segs=1), 'gold')
        k.add(lambda bm: rbox(bm, (R * 2.5, R * 0.3, R * 3.2), (0, -R * 0.5, zp + R * 0.05), 0.03, segs=1), 'black')
        k.add(lambda bm: rbox(bm, (R * 3.2, R * 0.4, R * 0.25), (0, -R * 0.45, zp + R * 3.45), 0.03, segs=1), 'gold')

def shimenawa(k, X, z, sag, rnd):
    """a twisted straw rope hung between the pillars, white zigzag paper streamers (shide) and straw tassels"""
    pts = [Vector((x, -0.05, z - sag * (1 - (x / X) ** 2))) for x in (-X + 2 * X * i / 24 for i in range(25))]
    for ph in (0.0, math.pi):
        tw = [p + Vector((0, 0.07 * math.cos(i * 0.9 + ph), 0.07 * math.sin(i * 0.9 + ph))) for i, p in enumerate(pts)]
        k.add(lambda bm, tw=tw: tube(bm, tw, 0.11, 0.11, 8), lambda f: 'khaki' if f.normal.z > -0.2 else 'sandstone_dark')
    for j in range(3):   # straw tassels
        x = (j - 1) * X * 0.5; zz = z - sag * (1 - (x / X) ** 2)
        k.add(lambda bm, x=x, zz=zz: lathe(bm, [(0.06, 0.0), (0.12, -0.15), (0.1, -0.5), (0.0, -0.55)], 8, None, 0, Matrix.Translation((x, -0.05, zz - 0.08))), 'khaki')
    for j in range(4):   # shide: four-step zigzag paper strips
        x = (j - 1.5) * X * 0.5; zz = z - sag * (1 - (x / X) ** 2) - 0.1
        def shide(bm, x=x, zz=zz):
            w = 0.13; t = 0.012; step = 0.16; pts2 = []
            for i in range(5):
                pts2.append(Vector((x + (w if i % 2 else 0.0), -0.12, zz - i * step)))
            for a, b in zip(pts2, pts2[1:]):
                v = [bm.verts.new(a + Vector((-w * 0.5, 0, 0))), bm.verts.new(a + Vector((w * 0.5, 0, 0))), bm.verts.new(b + Vector((w * 0.5, 0, 0))), bm.verts.new(b + Vector((-w * 0.5, 0, 0)))]
                vb = [bm.verts.new(q.co + Vector((0, t, 0))) for q in v]
                bm.faces.new(v); bm.faces.new(list(reversed(vb)))
        k.add(shide, 'snow')

def build_toriigate(seed=3501):
    """the gateway behind each spawn (in place of the base scene's gate arch, ~10.8 m wide): a big vermilion myojin
    torii, a shimenawa rope with paper streamers between the pillars, stone lanterns at its feet, raked gravel"""
    rnd = random.Random(seed)
    k = SKit(['red', 'black', 'gold', 'ash', 'bone_dark', 'basalt', 'khaki', 'sandstone_dark', 'snow', 'palm_dark', 'pine'])
    torii(k, 3.7, 7.0, 0.42, 1.75, 0.55, True, seed)
    shimenawa(k, 3.45, 4.75, 0.45, rnd)
    for s in (-1, 1):
        lantern_kasuga(k, s * 5.0, -1.3, 1.3, rnd, seed + (s + 1))
    k.add(lambda bm: blob(bm, 1.0, (0, -0.8, 0), (5.8, 1.6, 0.06), 3, 0.02, seed), gravel)
    return k.obj('ToriiGate')

def build_toriismall(seed=3511):
    """the torii that marks the start of a stone speed path (~3 m between the pillars): the same detailed myojin torii
    as the gate, smaller. One per path end, never in rows (Tho: a row of torii read as clutter from the top-down camera)"""
    k = SKit(['red', 'black', 'gold', 'ash', 'bone_dark', 'basalt'])
    torii(k, 1.42, 3.3, 0.16, 0.62, 0.24, True, seed)
    return k.obj('ToriiSmall')

def build_pathslab(seed=3521):
    """a cut stone slab of the path under the torii (2.3 m across, laid every ~0.8 m)"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'basalt'])
    k.add(lambda bm: (rbox(bm, (2.3, 0.62, 0.08), (0, 0, -0.03), 0.04, segs=1), jitter([v for v in bm.verts if v[k.seen] == 0 and v.co.z > 0.0], 0.015, seed)), lambda f: 'ash' if f.normal.z > 0.5 else 'bone_dark')
    return k.obj('PathSlab')

# ------------------------------------------------------------------ lanterns, guardian, basin
def lantern_kasuga(k, x, y, s, rnd, seed):
    """a tall kasuga stone lantern: base, post, platform, light box with windows, a hexagonal roof curling up at the
    corners, a jewel on top, moss on the roof"""
    M = lambda z: Matrix.Translation((x, y, z * s))
    k.add(lambda bm: lathe(bm, [(0.42 * s, 0.0), (0.42 * s, 0.14 * s), (0.3 * s, 0.24 * s), (0.0, 0.24 * s)], 6, None, 0.52, M(0)), stonec)
    k.add(lambda bm: lathe(bm, [(0.13 * s, 0.24 * s), (0.11 * s, 1.15 * s), (0.0, 1.15 * s)], 12, None, 0, M(0)), stonec)
    k.add(lambda bm: lathe(bm, [(0.16 * s, 0.0), (0.34 * s, 0.12 * s), (0.34 * s, 0.2 * s), (0.0, 0.2 * s)], 6, None, 0.52, M(1.12)), stonec)
    k.add(lambda bm: lathe(bm, [(0.24 * s, 0.0), (0.24 * s, 0.42 * s), (0.0, 0.42 * s)], 6, None, 0.52, M(1.32)), stonec)
    for a in (0.0, math.pi):   # the light windows (dark openings front and back)
        d = Vector((0, -math.cos(a), 0))
        k.add(lambda bm, d=d: disc_facing(bm, Vector((x, y, 1.53 * s)) + d * 0.215 * s, d, 0.11 * s, 4), 'black')
    hex_roof(k, x, y, 1.74 * s, 0.56 * s, 0.4 * s, 0.1 * s, stonec)
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.09 * s, 0.03 * s), (0.08 * s, 0.12 * s), (0.0, 0.22 * s)], 10, None, 0, M(2.12)), stonec)   # jewel
    moss_cap(k, (x + 0.12 * s, y + 0.1 * s, 1.92 * s), (0.2 * s, 0.17 * s, 0.07 * s), seed)

def build_kasugalantern(seed=3531):
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'basalt', 'black'])
    lantern_kasuga(k, 0, 0, 1.15, rnd, seed)
    for j in range(3):
        a = rnd.uniform(0, 6.28)
        moss_cap(k, (math.cos(a) * 0.5, math.sin(a) * 0.5, 0.03), (0.18, 0.15, 0.08), seed + j)
    skirt(k, 0.65, 0.65, seed)
    return k.obj('KasugaLantern')

def build_stonelanternlow(seed=3541):
    """a snow-viewing lantern (yukimi): three curved legs, a light box, a very wide round roof"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'basalt', 'black'])
    for j in range(3):
        a = j * 2 * math.pi / 3 + 0.3; d = Vector((math.cos(a), math.sin(a), 0))
        pts = [d * 0.42, d * 0.36 + UP * 0.25, d * 0.2 + UP * 0.48]
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.07, 0.06, 8), stonec)
    k.add(lambda bm: lathe(bm, [(0.18, 0.44), (0.26, 0.5), (0.26, 0.56), (0.0, 0.56)], 6, None, 0.52), stonec)
    k.add(lambda bm: lathe(bm, [(0.2, 0.56), (0.2, 0.84), (0.0, 0.84)], 6, None, 0.52), stonec)
    for a in (0.0, math.pi):
        d = Vector((0, -math.cos(a), 0))
        k.add(lambda bm, d=d: disc_facing(bm, Vector((0, 0, 0.7)) + d * 0.18, d, 0.08, 4), 'black')
    k.add(lambda bm: lathe(bm, [(0.62, 0.82), (0.66, 0.86), (0.4, 1.0), (0.12, 1.08), (0.0, 1.1)], 20, lambda i, kk, rr, zz: (rr * (1 + 0.04 * math.sin(i * 6.28 / 20 * 6)), zz), 0), stonec)
    k.add(lambda bm: lathe(bm, [(0.0, 1.08), (0.07, 1.12), (0.0, 1.24)], 8), stonec)
    moss_cap(k, (0.2, -0.15, 1.02), (0.22, 0.18, 0.06), seed)
    skirt(k, 0.75, 0.7, seed)
    return k.obj('StoneLanternLow')

def build_komainu(seed=3551):
    """a komainu guardian (an original chunky lion-dog) sitting on a stone plinth, curly mane, a ball under one paw"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'basalt', 'black', 'palm_dark'])
    k.add(lambda bm: rbox(bm, (1.0, 0.9, 0.55), (0, 0, 0), 0.05, segs=1), stonec)
    k.add(lambda bm: rbox(bm, (0.86, 0.76, 0.12), (0, 0, 0.55), 0.03, segs=1), stonec)
    z0 = 0.67
    k.add(lambda bm: blob(bm, 1.0, (0.0, 0.12, z0 + 0.32), (0.3, 0.32, 0.36), 2, 0.01, seed, floor=False), stonec)           # body (sitting)
    k.add(lambda bm: blob(bm, 1.0, (0.0, 0.25, z0 + 0.12), (0.34, 0.28, 0.14), 2, 0.01, seed + 1, floor=False), stonec)      # haunches
    for s in (-1, 1):
        k.add(lambda bm, s=s: tube(bm, [Vector((s * 0.16, -0.1, z0 + 0.5)), Vector((s * 0.17, -0.2, z0 + 0.2)), Vector((s * 0.17, -0.22, z0 + 0.02))], 0.08, 0.075, 8), stonec)   # front legs
        k.add(lambda bm, s=s: sphere(bm, 0.085, (s * 0.17, -0.27, z0 + 0.05), (1.1, 1.3, 0.7), 1), stonec)
    k.add(lambda bm: sphere(bm, 0.13, (0.3, -0.28, z0 + 0.13), sub=2), stonec)                                                # the ball
    k.add(lambda bm: blob(bm, 1.0, (0.0, -0.1, z0 + 0.86), (0.26, 0.24, 0.24), 2, 0.01, seed + 2, floor=False), stonec)      # head
    k.add(lambda bm: blob(bm, 1.0, (0.0, -0.3, z0 + 0.8), (0.14, 0.1, 0.1), 1, 0.005, seed + 3, floor=False), stonec)        # muzzle
    for j in range(9):   # mane curls round the head
        a = -math.pi * 0.1 + j * math.pi * 1.2 / 8
        p = Vector((math.cos(a) * 0.26, -0.02 + math.sin(a) * 0.08, z0 + 0.86 + math.sin(a) * 0.2))
        k.add(lambda bm, p=p, j=j: sphere(bm, 0.075, (p.x, p.y, p.z), (1, 0.9, 1), 1), stonec)
    for s in (-1, 1):
        k.add(lambda bm, s=s: sphere(bm, 0.035, (s * 0.1, -0.3, z0 + 0.93), sub=1), 'black')                                   # eyes
        k.add(lambda bm, s=s: sphere(bm, 0.07, (s * 0.2, -0.04, z0 + 1.07), (0.8, 0.6, 1.0), 1), stonec)                       # ears
    k.add(lambda bm: tube(bm, [Vector((0, 0.42, z0 + 0.15)), Vector((0, 0.5, z0 + 0.45)), Vector((0, 0.42, z0 + 0.62))], 0.06, 0.1, 8), stonec)   # tail
    moss_cap(k, (0.25, 0.2, 0.62), (0.18, 0.14, 0.05), seed)
    skirt(k, 0.7, 0.65, seed)
    return k.obj('Komainu')

def build_waterbasin(seed=3561):
    """a stone water basin for purification: rough round stone with water, a bamboo spout on a post, wooden ladles"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'basalt', 'ice_deep', 'palm', 'olive', 'wood', 'wood_dark', 'pine', 'palm_dark'])
    k.add(lambda bm: lathe(bm, [(0.45, 0.0), (0.5, 0.35), (0.46, 0.58), (0.36, 0.6), (0.34, 0.5), (0.0, 0.5)], 18, lambda i, kk, rr, zz: (rr * (1 + 0.05 * math.sin(i * 1.7)), zz)), stonec)
    k.add(lambda bm: lathe(bm, [(0.35, 0.52), (0.0, 0.52)], 18), 'ice_deep')
    bam = lambda f: 'palm' if f.normal.z > -0.5 else 'olive'
    k.add(lambda bm: tube(bm, [Vector((0.55, 0.0, 0.0)), Vector((0.55, 0.0, 0.95))], 0.05, 0.05, 8), bam)
    k.add(lambda bm: tube(bm, [Vector((0.55, 0.0, 0.88)), Vector((0.22, 0.0, 0.8))], 0.035, 0.035, 8), bam)
    for j, a in enumerate((0.4, -0.6)):   # ladles across the rim
        d = Vector((math.cos(a), math.sin(a), 0))
        k.add(lambda bm, d=d: tube(bm, [d * -0.48 + UP * 0.62, d * 0.3 + UP * 0.62], 0.012, 0.012, 4), 'wood')
        k.add(lambda bm, d=d: lathe(bm, [(0.0, 0.0), (0.07, 0.0), (0.07, 0.08), (0.06, 0.08), (0.06, 0.01), (0.0, 0.01)], 10, None, 0, Matrix.Translation(d * 0.34 + UP * 0.6)), 'wood')
    for j in range(5):
        a = rnd.uniform(0, 6.28)
        k.add(lambda bm, a=a, j=j: blob(bm, 1.0, (math.cos(a) * 0.62, math.sin(a) * 0.62, 0.05), (0.12, 0.1, 0.08), 1, 0.02, seed + j), stonec)
    skirt(k, 0.8, 0.75, seed)
    return k.obj('WaterBasin')

def build_offeringbox(seed=3571):
    """an offering box: dark wooden chest with a slatted top and gold corner fittings, a red-and-white bell rope coiled
    on it"""
    rnd = random.Random(seed)
    k = SKit(['wood', 'wood_dark', 'gold', 'xmas_red', 'snow', 'ash', 'bone_dark'])
    k.add(lambda bm: rbox(bm, (1.1, 0.7, 0.62), (0, 0, 0.1), 0.03, segs=1), wood)
    for x in (-0.48, 0.48):
        k.add(lambda bm, x=x: rbox(bm, (0.1, 0.74, 0.1), (x, 0, 0.0), 0.01, segs=1), 'wood_dark')   # feet
    for j in range(7):
        x = -0.42 + j * 0.14
        k.add(lambda bm, x=x: rbox(bm, (0.08, 0.66, 0.06), (x, 0, 0.72), 0.01, segs=1), 'wood_dark')
    for sx in (-1, 1):
        for sy in (-1, 1):
            k.add(lambda bm, sx=sx, sy=sy: rbox(bm, (0.12, 0.12, 0.1), (sx * 0.52, sy * 0.32, 0.64), 0.01, segs=1), 'gold')
    for j in range(2):
        col = 'xmas_red' if j == 0 else 'snow'
        k.add(lambda bm, j=j: lathe(bm, [(0.12 + j * 0.05, 0.78), (0.15 + j * 0.05, 0.8), (0.12 + j * 0.05, 0.82), (0.09 + j * 0.05, 0.8)], 12, None, 0, Matrix.Translation((0.25, 0.05, 0.0))), col)
    skirt(k, 0.75, 0.55, seed)
    return k.obj('OfferingBox')

def taru(k, x, y, z, r, h, yaw, col, rnd):
    """a straw-wrapped sake barrel lying on its side, the painted lid facing out"""
    M = Matrix.Translation((x, y, z)) @ Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Rotation(math.pi / 2, 4, 'X')
    k.add(lambda bm: lathe(bm, [(0.0, -h / 2), (r * 0.94, -h / 2), (r, -h * 0.3), (r * 1.04, 0.0), (r, h * 0.3), (r * 0.94, h / 2), (0.0, h / 2)], 16, None, 0, M), lambda f: 'khaki' if abs((M.inverted().to_3x3() @ f.normal).z) < 0.8 else col)
    for zz in (-0.32, 0.0, 0.32):
        k.add(lambda bm, zz=zz: lathe(bm, [(r * 1.05, zz * h - 0.03), (r * 1.06, zz * h), (r * 1.05, zz * h + 0.03)], 16, None, 0, M), 'sandstone_dark')   # rope bands
    face = M @ Vector((0, 0, -h / 2 - 0.005))
    d = (M.to_3x3() @ Vector((0, 0, -1))).normalized()
    k.add(lambda bm: disc_facing(bm, face, d, r * 0.55, 12), 'gold' if col != 'gold' else 'xmas_red')

def build_sakebarrels(seed=3581):
    """an offering wall of sake barrels (kazaridaru): straw-wrapped barrels with painted lids stacked three high on a
    wooden rack"""
    rnd = random.Random(seed)
    k = SKit(['khaki', 'sandstone_dark', 'xmas_red', 'lapis', 'gold', 'cream', 'black', 'wood', 'wood_dark', 'ash', 'bone_dark'])
    k.add(lambda bm: rbox(bm, (1.5, 0.95, 0.18), (0, 0.05, 0.0), 0.02, segs=1), wood)
    cols = ['xmas_red', 'lapis', 'cream', 'black', 'xmas_red', 'lapis']
    r, h = 0.24, 0.5
    j = 0
    for row, n in enumerate((3, 2, 1)):
        for i in range(n):
            x = (i - (n - 1) / 2) * 0.5; z = 0.18 + r + row * r * 1.75
            for yy in (-0.2, 0.3) if row < 2 else (0.05,):
                taru(k, x, yy, z, r, h, 0.0, cols[j % len(cols)], rnd); j += 1
    skirt(k, 0.85, 0.75, seed)
    return k.obj('SakeBarrels')

def build_tarustack(seed=3591):
    rnd = random.Random(seed)
    k = SKit(['khaki', 'sandstone_dark', 'xmas_red', 'lapis', 'gold', 'cream', 'wood', 'ash', 'bone_dark'])
    taru(k, -0.27, 0.0, 0.26, 0.25, 0.52, 0.2, 'xmas_red', rnd)
    taru(k, 0.27, 0.05, 0.26, 0.25, 0.52, -0.1, 'lapis', rnd)
    taru(k, 0.0, 0.02, 0.7, 0.24, 0.5, 0.05, 'cream', rnd)
    skirt(k, 0.75, 0.6, seed)
    return k.obj('TaruStack')

# ------------------------------------------------------------------ walls
def build_ishigaki(seed=3601):
    """a block of castle stone wall: big fitted granite stones in a battered slope, smaller wedges between, moss on top"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'basalt', 'papyrus_grad', 'sandstone_dark'])
    z = 0.0; rows = [0.5, 0.45, 0.42, 0.35]
    for ri, h in enumerate(rows):
        inset = ri * 0.06
        x = -0.72 + inset; w0 = 1.44 - 2 * inset
        while x < 0.72 - inset - 0.05:
            w = min(rnd.uniform(0.38, 0.62), 0.72 - inset - x)
            k.add(lambda bm, x=x, w=w, z=z, h=h, inset=inset: (rbox(bm, (w - 0.04, 1.36 - 2 * inset, h - 0.04), (x + w / 2, 0, z), 0.08, rnd.uniform(-0.02, 0.02), segs=2),
                                                                jitter([v for v in bm.verts if v[k.seen] == 0], 0.02, seed + int(x * 100) + ri)), stonec)
            x += w
        z += h
    moss_cap(k, (0.1, 0.0, z + 0.02), (0.5, 0.45, 0.1), seed)
    skirt(k, 0.85, 0.85, seed)
    return k.obj('IshigakiStones')

def build_bamboofencewall(seed=3611):
    """a tall bamboo fence (takegaki): dried bamboo poles side by side between posts, split-bamboo rails tied with
    black rope, a little roof of bamboo on top, stones at its foot"""
    rnd = random.Random(seed)
    k = SKit(['khaki', 'sandstone', 'sandstone_dark', 'black', 'wood_dark', 'ash', 'bone_dark', 'palm_dark'])
    bam = lambda f: 'sandstone' if f.normal.z > 0.5 else 'khaki'
    for j in range(17):
        x = -0.72 + j * 0.09
        h = 1.62 + 0.03 * math.sin(j * 1.7)
        k.add(lambda bm, x=x, h=h: tube(bm, [Vector((x, 0, 0.12)), Vector((x, 0, h))], 0.045, 0.043, 8), bam)
        for zn in (0.55, 1.1):   # nodes
            k.add(lambda bm, x=x, zn=zn: lathe(bm, [(0.05, zn - 0.01), (0.05, zn + 0.01), (0.0, zn + 0.01)], 8, None, 0, Matrix.Translation((x, 0, 0))), 'sandstone_dark')
    for x in (-0.76, 0.76):
        k.add(lambda bm, x=x: tube(bm, [Vector((x, 0, 0.0)), Vector((x, 0, 1.75))], 0.07, 0.07, 10), 'wood_dark')
    for z in (0.35, 0.9, 1.4):
        for y in (-0.07, 0.07):
            k.add(lambda bm, z=z, y=y: rbox(bm, (1.52, 0.04, 0.07), (0, y, z), 0.015, segs=1), 'sandstone_dark')
        for j in range(6):   # black rope ties
            x = -0.6 + j * 0.24
            k.add(lambda bm, x=x, z=z: rbox(bm, (0.05, 0.2, 0.11), (x, 0, z - 0.02), 0.01, segs=1), 'black')
    k.add(lambda bm: tube(bm, [Vector((-0.8, 0, 1.72)), Vector((0.8, 0, 1.72))], 0.08, 0.08, 10), bam)   # the top cap
    for j in range(5):
        x = rnd.uniform(-0.6, 0.6); y = rnd.choice((-0.25, 0.25))
        k.add(lambda bm, x=x, y=y, j=j: blob(bm, 1.0, (x, y, 0.06), (0.15, 0.12, 0.1), 1, 0.02, seed + j), stonec)
    skirt(k, 0.85, 0.4, seed)
    return k.obj('BambooFenceWall')

def build_plasterwall(seed=3621):
    """a white plaster wall on a stone base, the lower part in namako tiles (grey squares, white diamond grid), a grey
    tile roof on top"""
    rnd = random.Random(seed)
    k = SKit(['snow', 'cream', 'ash', 'bone_dark', 'basalt', 'stone', 'stone_dark', 'wood_dark'])
    k.add(lambda bm: rbox(bm, (1.5, 0.75, 0.35), (0, 0, 0), 0.03, segs=1), stonec)                       # stone base
    k.add(lambda bm: rbox(bm, (1.5, 0.6, 1.25), (0, 0, 0.35), 0.02, segs=1), lambda f: 'snow' if f.normal.z > -0.5 else 'cream')
    for side in (-1, 1):   # namako tiles: grey squares set diagonally with white joints
        for i in range(5):
            for j in range(2):
                x = -0.6 + i * 0.3 + (0.15 if j % 2 else 0.0); z = 0.5 + j * 0.2
                if abs(x) > 0.66: continue
                M = Matrix.Translation((x, side * 0.305, z)) @ Matrix.Rotation(math.pi / 4, 4, 'Y') @ Matrix.Diagonal((0.17, 0.02, 0.17, 1))
                k.add(lambda bm, M=M: bmesh.ops.create_cube(bm, size=1.0, matrix=M), 'stone')
    hip_roof(k, 0, 0, 1.62, 1.6, 1.05, 0.4, 0.06, tiles, 'wood_dark', 0.1, 6)
    skirt(k, 0.85, 0.6, seed)
    return k.obj('PlasterWall')

def build_mossrock(seed=3631):
    """a big garden rock half covered in moss, a small fern"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'basalt', 'papyrus_grad', 'palm', 'palm_dark'])
    k.add(lambda bm: blob(bm, 1.0, (0.0, 0.0, 0.7), (0.74, 0.66, 0.85), 2, 0.07, seed), stonec)
    k.add(lambda bm: blob(bm, 1.0, (0.45, -0.35, 0.28), (0.34, 0.3, 0.3), 2, 0.04, seed + 1), stonec)
    moss_cap(k, (-0.05, 0.05, 1.38), (0.5, 0.45, 0.2), seed)
    moss_cap(k, (0.45, -0.35, 0.5), (0.25, 0.2, 0.08), seed + 1)
    fern(k, -0.5, -0.45, 0.0, 0.8, rnd)
    skirt(k, 0.85, 0.8, seed)
    return k.obj('MossRock')

def fern(k, x, y, z, s, rnd):
    for j in range(7):
        a = j * 2 * math.pi / 7 + rnd.uniform(-0.2, 0.2); d = Vector((math.cos(a), math.sin(a), 0))
        p0 = Vector((x, y, z)); p2 = p0 + d * 0.38 * s + UP * 0.18 * s
        k.grad(lambda bm, p0=p0, p2=p2, d=d: tube(bm, bezier(p0, p0 + d * 0.12 * s + UP * 0.32 * s, p2, 5), 0.05 * s, 0.01, 3), 'papyrus_grad', lambda v, z=z: 0.25 + 0.4 * min(1.0, (v.co.z - z) / (0.35 * s)))

# ------------------------------------------------------------------ fence, bush, trees
def build_bamboofence(seed=3641):
    """low cover: a four-eyed bamboo lattice fence (yotsume-gaki) with black rope ties, between two posts"""
    rnd = random.Random(seed)
    k = SKit(['khaki', 'sandstone', 'sandstone_dark', 'black', 'wood_dark', 'ash', 'bone_dark'])
    bam = lambda f: 'sandstone' if f.normal.z > 0.5 else 'khaki'
    for x in (-0.72, 0.72):
        k.add(lambda bm, x=x: tube(bm, [Vector((x, 0, 0.0)), Vector((x, 0, 1.0))], 0.06, 0.06, 10), 'wood_dark')
    for z in (0.3, 0.6, 0.88):
        k.add(lambda bm, z=z: tube(bm, [Vector((-0.75, 0.05, z)), Vector((0.75, 0.05, z))], 0.03, 0.03, 8), bam)
    for j in range(6):
        x = -0.55 + j * 0.22
        k.add(lambda bm, x=x: tube(bm, [Vector((x, -0.02, 0.02)), Vector((x, -0.02, 0.95))], 0.028, 0.028, 8), bam)
        for z in (0.3, 0.6, 0.88):
            k.add(lambda bm, x=x, z=z: rbox(bm, (0.06, 0.09, 0.05), (x, 0.015, z - 0.025), 0.005, segs=1), 'black')
    skirt(k, 0.85, 0.35, seed)
    return k.obj('BambooFence')

def build_bamboograss(seed=3651):
    """the hiding bush: a thicket of green bamboo culms with nodes, leaning a little apart, sprays of narrow leaves at
    the top (~1.5 m; the builder scales it 1.3)"""
    rnd = random.Random(seed)
    k = SKit(['palm', 'palm_dark', 'pine_light', 'olive', 'papyrus_grad'])
    culm = lambda f: 'pine_light' if f.normal.z > 0.6 else 'palm'
    tops = []
    for j in range(11):
        a = j * 2 * math.pi / 11 + rnd.uniform(-0.2, 0.2); r = rnd.uniform(0.05, 0.25)
        b = Vector((math.cos(a) * r, math.sin(a) * r, 0.0))
        h = rnd.uniform(1.2, 1.5); lean = Vector((math.cos(a), math.sin(a), 0)) * rnd.uniform(0.06, 0.16)
        top = b + lean + UP * h
        k.add(lambda bm, b=b, top=top: tube(bm, [b, (b + top) / 2 + Vector((0, 0, 0.02)), top], 0.035, 0.028, 8), culm)
        for t in (0.3, 0.55, 0.78):
            p = b.lerp(top, t)
            k.add(lambda bm, p=p: lathe(bm, [(0.042, -0.012), (0.042, 0.012), (0.0, 0.012)], 8, None, 0, Matrix.Translation(p)), 'palm_dark')
        tops.append((top, Vector((math.cos(a), math.sin(a), 0))))
    for (top, d) in tops:   # leaf sprays: narrow leaves fanning out and drooping, on the papyrus ramp by height
        for m in range(5):
            dd = (d + Vector((rnd.uniform(-0.8, 0.8), rnd.uniform(-0.8, 0.8), 0))).normalized()
            p0 = top - UP * rnd.uniform(0.0, 0.35); p2 = p0 + dd * 0.32 - UP * 0.08
            k.grad(lambda bm, p0=p0, p2=p2, dd=dd: tube(bm, bezier(p0, p0 + dd * 0.16 + UP * 0.07, p2, 4), 0.035, 0.004, 3), 'papyrus_grad', lambda v: 0.3 + 0.35 * min(1.0, max(0.0, v.co.z / 1.5)))
    return k.obj('BambooGrass')

def build_sakuratree(seed=3661, name='SakuraTree', H=4.2):
    """a cherry tree in full bloom: a dark crooked trunk forking into branches that spread wide, soft clouds of pink
    blossom (slot 1, SakuraPink in Unity: lighter on top, deeper pink underneath), petals on the ground"""
    rnd = random.Random(seed)
    k = SKit(['char_grad', 'sand_grad', 'ash', 'bone_dark'])
    s = H / 4.2
    bark = lambda v: 0.25 + 0.3 * min(1.0, v.co.z / (H * 0.7))
    fork = Vector((0.15 * s, -0.05 * s, 1.6 * s))
    k.grad(lambda bm: tube(bm, bezier(Vector((0, 0, 0)), Vector((-0.12 * s, 0.05 * s, 0.9 * s)), fork, 6), 0.24 * s, 0.17 * s, 12), 'char_grad', bark)
    k.grad(lambda bm: lathe(bm, [(0.36 * s, 0.0), (0.24 * s, 0.3 * s), (0.0, 0.32 * s)], 12), 'char_grad', bark)
    tips = []
    for j in range(5):
        a = j * 2 * math.pi / 5 + rnd.uniform(-0.3, 0.3); d = Vector((math.cos(a), math.sin(a), 0))
        L = s * rnd.uniform(1.5, 2.0)
        p2 = fork + d * L + UP * s * rnd.uniform(1.0, 1.6)
        pts = bezier(fork, fork + d * L * 0.45 + UP * s * 0.75, p2, 6)
        k.grad(lambda bm, pts=pts: tube(bm, pts, 0.14 * s, 0.05 * s, 10), 'char_grad', bark)
        for m in range(2):
            b = pts[3 + m]; dd = (d + Vector((-d.y, d.x, 0)) * (0.8 if m else -0.8)).normalized()
            q2 = b + dd * L * 0.4 + UP * s * 0.4
            k.grad(lambda bm, b=b, q2=q2, dd=dd: tube(bm, bezier(b, b + dd * L * 0.2 + UP * s * 0.25, q2, 4), 0.06 * s, 0.025 * s, 8), 'char_grad', bark)
            tips.append(q2)
        tips.append(p2)
    # the blossom: one big soft cloud. Lumps on the branch tips and on a shell round the crown (an ellipsoid, wide and
    # flattish, a little hanging down at the rim), overlapping; the sand ramp in slot 1 (the pink tint) by height over
    # the whole crown: deeper pink underneath, near white on top
    C = fork + UP * s * 1.35
    z_lo, z_hi = C.z - 0.9 * s, C.z + 1.3 * s
    bt = lambda v: 0.15 + 0.82 * min(1.0, max(0.0, (v.co.z - z_lo) / (z_hi - z_lo)))
    spots = [Vector((t.x, t.y, t.z + 0.1 * s)) for t in tips]
    for j in range(16):   # a golden-angle spiral over the upper part of the shell
        u = 0.85 - 1.15 * (j + 0.5) / 16; a = j * 2.39996
        r = math.sqrt(max(0.0, 1 - u * u))
        spots.append(C + Vector((math.cos(a) * r * 1.75 * s, math.sin(a) * r * 1.6 * s, u * 0.85 * s)))
    for j, c in enumerate(spots):
        sc = s * rnd.uniform(0.85, 1.1)
        k.grad(lambda bm, c=c, sc=sc, j=j: pad(bm, c, 0.6 * sc, 0.55 * sc, 0.42 * sc, j * 1.3, seed + j), 'sand_grad', bt, glow=True)
    petals(k, 1.8 * s, 26, rnd, 0.0)
    skirt(k, 0.55 * s, 0.55 * s, seed)
    return tagged(k.obj(name))

def petals(k, R, n, rnd, z):
    for j in range(n):
        a = rnd.uniform(0, 6.28); r = R * math.sqrt(rnd.random())
        p = Vector((math.cos(a) * r, math.sin(a) * r, z + 0.012))
        k.grad(lambda bm, p=p, a=a: lathe(bm, [(0.05, 0.0), (0.05, 0.008), (0.0, 0.01)], 5, None, a, Matrix.Translation(p) @ Matrix.Diagonal((1.0, 0.6, 1.0, 1))), 'sand_grad', 0.85, glow=True)

def build_blackpine(seed=3671):
    """a cloud-pruned black pine (niwaki): a twisted leaning trunk, near-horizontal branches, flat dark-green pads of
    needles on the ends in layers"""
    rnd = random.Random(seed)
    k = SKit(['char_grad', 'papyrus_grad', 'ash', 'bone_dark'])
    bark = lambda v: 0.2 + 0.35 * min(1.0, v.co.z / 3.0)
    trunk = [Vector((0, 0, 0)), Vector((0.25, 0.05, 0.9)), Vector((-0.1, 0.1, 1.8)), Vector((0.2, -0.05, 2.7)), Vector((0.05, 0.0, 3.3))]
    k.grad(lambda bm: tube(bm, trunk, 0.2, 0.08, 12), 'char_grad', bark)
    k.grad(lambda bm: lathe(bm, [(0.3, 0.0), (0.2, 0.25), (0.0, 0.27)], 12), 'char_grad', bark)
    needles = lambda v, c, h: 0.06 + 0.42 * min(1.0, max(0.0, (v.co.z - (c.z - h)) / (2 * h)))
    levels = [(1.3, 1.3, 0.3), (2.0, 1.15, 2.0), (2.6, 0.95, 4.1), (3.1, 0.7, 5.5)]
    for j, (z, L, a) in enumerate(levels):
        d = Vector((math.cos(a), math.sin(a), 0))
        base = Vector((0.1, 0.03, z))
        tip = base + d * L + UP * 0.15
        k.grad(lambda bm, base=base, tip=tip: tube(bm, bezier(base, base + d * L * 0.5 - UP * 0.1, tip, 5), 0.08, 0.04, 8), 'char_grad', bark)
        c = tip + UP * 0.12
        sc = 0.55 + 0.1 * (3 - j)
        k.grad(lambda bm, c=c, sc=sc, j=j: pad(bm, c, sc, sc * 0.85, 0.2, j * 1.1, seed + j), 'papyrus_grad', lambda v, c=c: needles(v, c, 0.2))
    c = trunk[-1] + UP * 0.15
    k.grad(lambda bm: pad(bm, c, 0.55, 0.5, 0.25, 0.0, seed + 9), 'papyrus_grad', lambda v: needles(v, c, 0.25))
    skirt(k, 0.5, 0.5, seed)
    return k.obj('BlackPine')

# ------------------------------------------------------------------ decor
def build_steppingstones(seed=3681):
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'basalt'])
    for j, (x, y) in enumerate([(-0.45, -0.1), (0.0, 0.12), (0.45, -0.05)]):
        r = rnd.uniform(0.2, 0.26)
        k.add(lambda bm, x=x, y=y, r=r, j=j: lathe(bm, [(r, 0.0), (r, 0.04), (r * 0.9, 0.06), (0.0, 0.06)], 10, lambda i, kk, rr, zz: (rr * (1 + 0.1 * math.sin(i * 2.1 + j)), zz), 0, Matrix.Translation((x, y, 0))), lambda f: 'ash' if f.normal.z > 0.5 else 'bone_dark')
    return k.obj('SteppingStones')

def build_mosstuft(seed=3691):
    rnd = random.Random(seed)
    k = SKit(['papyrus_grad', 'palm', 'palm_dark'])
    moss_cap(k, (0.0, 0.0, 0.04), (0.32, 0.28, 0.1), seed)
    moss_cap(k, (0.25, 0.18, 0.03), (0.16, 0.14, 0.06), seed + 1)
    fern(k, -0.1, 0.05, 0.05, 0.6, rnd)
    return k.obj('MossTuft')

def build_petalscatter(seed=3701):
    rnd = random.Random(seed)
    k = SKit(['sand_grad'])
    petals(k, 0.55, 22, rnd, 0.0)
    return tagged(k.obj('PetalScatter'))

def build_zenstones(seed=3711):
    """a small patch of raked gravel: concentric ridges round a rock"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'bone', 'basalt'])
    k.add(lambda bm: lathe(bm, [(0.7, 0.0)] + [(0.7 - i * 0.11 + (0.055 if i % 2 else 0.0), 0.02 + (0.025 if i % 2 else 0.0)) for i in range(1, 6)] + [(0.0, 0.03)], 24), lambda f: 'bone' if f.normal.z > 0.7 else 'ash')
    k.add(lambda bm: blob(bm, 1.0, (0.05, 0.0, 0.12), (0.2, 0.17, 0.2), 2, 0.03, seed), stonec)
    return k.obj('ZenStones')

# ------------------------------------------------------------------ border, backdrop
def build_shrinewall(kind, seed, name):
    """a border piece in place of the old border cliffs (7.6 m long, 5 m deep, ~3.5 m tall: low): a battered stone
    base, an earthen wall of white plaster with timber posts on it, a grey tile roof. Front = -Y (+Z in Unity). 'A'
    plain, 'B' with a cherry tree in bloom behind, 'C' with a belfry (a bronze bell under a roof)."""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'basalt', 'snow', 'cream', 'wood', 'wood_dark', 'stone', 'stone_dark', 'papyrus_grad',
              'bone', 'char_grad', 'sand_grad', 'gold_dark', 'gold', 'olive'])
    L, D = 7.6, 5.0
    k.add(lambda bm: rbox(bm, (L, 3.4, 1.1), (0, 0.5, 0), 0.08, segs=1), stonec)                                 # stone base
    for j in range(9):   # the stone courses showing on the front face
        x = -L / 2 + 0.45 + j * (L - 0.9) / 8
        k.add(lambda bm, x=x, j=j: (rbox(bm, (0.78, 0.2, 0.48), (x, -1.25, 0.06 + (j % 2) * 0.5), 0.06, segs=1), jitter([v for v in bm.verts if v[k.seen] == 0], 0.02, seed + j)), stonec)
    k.add(lambda bm: rbox(bm, (L, 0.7, 1.7), (0, -0.4, 1.1), 0.03, segs=1), lambda f: 'snow' if f.normal.z > -0.5 else 'cream')   # plaster wall
    for j in range(6):   # timber posts and a rail
        x = -L / 2 + 0.2 + j * (L - 0.4) / 5
        k.add(lambda bm, x=x: rbox(bm, (0.16, 0.74, 1.7), (x, -0.4, 1.1), 0.01, segs=1), 'wood_dark')
    k.add(lambda bm: rbox(bm, (L, 0.74, 0.12), (0, -0.4, 2.45), 0.01, segs=1), 'wood_dark')
    hip_roof(k, 0, -0.4, 2.8, L + 0.4, 1.5, 0.45, 0.1, tiles, 'wood_dark', 0.12, 10)
    for j in range(6):   # moss and ferns at the foot
        x = rnd.uniform(-L / 2 + 0.4, L / 2 - 0.4)
        moss_cap(k, (x, -1.5, 0.03), (rnd.uniform(0.25, 0.4), 0.2, 0.08), seed + j)
    if kind == 'B':   # a cherry tree in bloom behind the wall, its crown spilling over the roof
        sub = build_cherry_into(k, -1.6, 0.9, 0.9, 5.2, rnd, seed + 50)
    if kind == 'C':   # a belfry on the base: four posts, a roof, the bronze bell and its striking log
        cx, cy = 2.2, 1.0
        for sx in (-1, 1):
            for sy in (-1, 1):
                k.add(lambda bm, sx=sx, sy=sy: tube(bm, [Vector((cx + sx * 0.8, cy + sy * 0.6, 1.1)), Vector((cx + sx * 0.75, cy + sy * 0.55, 4.3))], 0.09, 0.08, 8), 'wood_dark')
        hip_roof(k, cx, cy, 4.3, 2.4, 2.0, 0.8, 0.2, tiles, 'wood_dark', 0.14, 8)
        k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.46, 0.0), (0.42, 0.25), (0.36, 0.95), (0.3, 1.1), (0.0, 1.15)], 16, None, 0, Matrix.Translation((cx, cy, 2.85))), lambda f: 'gold_dark' if f.normal.z > -0.5 else 'olive')
        k.add(lambda bm: tube(bm, [Vector((cx - 1.0, cy, 3.25)), Vector((cx - 0.55, cy, 3.25))], 0.1, 0.1, 8), 'wood')
    k.add(lambda bm: blob(bm, 1.0, (0, -D / 2 + 0.7, 0), (L * 0.49, 0.6, 0.06), 3, 0.02, seed), gravel)
    ob = k.obj(name)
    return tagged(ob) if kind == 'B' else ob

def build_cherry_into(k, x0, y0, z0, H, rnd, seed):
    """a cherry tree added into another kit (the border wall B, the backdrop)"""
    s = H / 4.2
    bark = lambda v: 0.25 + 0.3 * min(1.0, (v.co.z - z0) / (H * 0.7))
    base = Vector((x0, y0, z0)); fork = base + Vector((0.15 * s, -0.05 * s, 1.6 * s))
    k.grad(lambda bm: tube(bm, bezier(base, base + Vector((-0.12 * s, 0.05 * s, 0.9 * s)), fork, 6), 0.24 * s, 0.17 * s, 12), 'char_grad', bark)
    tips = []
    for j in range(5):
        a = j * 2 * math.pi / 5 + rnd.uniform(-0.3, 0.3); d = Vector((math.cos(a), math.sin(a), 0))
        L = s * rnd.uniform(1.4, 1.8); p2 = fork + d * L + UP * s * rnd.uniform(1.0, 1.5)
        k.grad(lambda bm, p2=p2, d=d, L=L: tube(bm, bezier(fork, fork + d * L * 0.45 + UP * s * 0.75, p2, 6), 0.14 * s, 0.05 * s, 10), 'char_grad', bark)
        tips.append(p2)
    for j, t in enumerate(tips + [fork + UP * s * 1.5]):
        sc = s * rnd.uniform(1.0, 1.25)
        k.grad(lambda bm, t=t, sc=sc, j=j: pad(bm, t, 0.75 * sc, 0.68 * sc, 0.45 * sc, j, seed + j), 'sand_grad',
               lambda v, t=t, sc=sc: 0.2 + 0.75 * min(1.0, max(0.0, (v.co.z - t.z + 0.45 * sc) / (0.9 * sc))), glow=True)

def build_pagoda(seed=3721):
    """a five-storey pagoda (backdrop): a stone platform, vermilion storeys with white walls, five grey tile roofs
    curving up at the corners, getting smaller upwards, the bronze spire with its rings"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'basalt', 'red', 'snow', 'cream', 'stone', 'stone_dark', 'wood_dark', 'gold_dark', 'gold'])
    k.add(lambda bm: rbox(bm, (7.5, 7.5, 1.0), (0, 0, 0), 0.1, segs=1), stonec)
    z = 1.0
    for f in range(5):
        w = 4.6 - f * 0.55; h = 2.4 - f * 0.12
        k.add(lambda bm, w=w, z=z, h=h: rbox(bm, (w, w, h), (0, 0, z), 0.04, segs=1), lambda f2: 'snow' if f2.normal.z > -0.5 else 'cream')
        for sx in (-1, 1):
            for sy in (-1, 1):
                k.add(lambda bm, w=w, z=z, h=h, sx=sx, sy=sy: rbox(bm, (0.32, 0.32, h), (sx * (w / 2 - 0.12), sy * (w / 2 - 0.12), z), 0.02, segs=1), 'red')
        k.add(lambda bm, w=w, z=z, h=h: rbox(bm, (w + 0.05, w + 0.05, 0.3), (0, 0, z + h - 0.3), 0.02, segs=1), 'red')
        hip_roof(k, 0, 0, z + h, w + 2.6, w + 2.6, 0.9, 0.45, tiles, 'wood_dark', 0.2, 8)
        z += h + 0.55
    k.add(lambda bm: tube(bm, [Vector((0, 0, z - 0.3)), Vector((0, 0, z + 4.0))], 0.16, 0.08, 10), 'gold_dark')
    for j in range(7):
        k.add(lambda bm, j=j: lathe(bm, [(0.42 - j * 0.03, 0.0), (0.42 - j * 0.03, 0.08), (0.0, 0.08)], 12, None, 0, Matrix.Translation((0, 0, z + 0.6 + j * 0.42))), 'gold_dark')
    k.add(lambda bm: sphere(bm, 0.28, (0, 0, z + 4.15), sub=2), 'gold')
    return k.obj('Pagoda')

def build_shrinehall(seed=3731):
    """a shrine hall (backdrop): a raised timber floor on posts, vermilion posts and white walls, a deep grey roof with
    a long sweeping curve, steps up the front"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'basalt', 'red', 'snow', 'cream', 'stone', 'stone_dark', 'wood', 'wood_dark', 'gold'])
    W, D = 12.0, 8.0
    k.add(lambda bm: rbox(bm, (W + 1.0, D + 1.0, 0.8), (0, 0, 0), 0.08, segs=1), stonec)
    k.add(lambda bm: rbox(bm, (W, D, 0.35), (0, 0, 1.5), 0.04, segs=1), wood)   # raised floor
    for i in range(6):
        for j in range(4):
            if 0 < i < 5 and 0 < j < 3: continue
            x = -W / 2 + 0.3 + i * (W - 0.6) / 5; y = -D / 2 + 0.3 + j * (D - 0.6) / 3
            k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, 0.8)), Vector((x, y, 5.6))], 0.2, 0.2, 10), 'red')
    k.add(lambda bm: rbox(bm, (W - 1.2, D - 1.2, 3.8), (0, 0, 1.85), 0.03, segs=1), lambda f: 'snow' if f.normal.z > -0.5 else 'cream')
    k.add(lambda bm: rbox(bm, (W + 0.2, D + 0.2, 0.35), (0, 0, 5.4), 0.02, segs=1), 'red')
    hip_roof(k, 0, 0, 5.75, W + 4.5, D + 4.5, 3.6, 0.9, tiles, 'wood_dark', 0.35, 10)
    for st in range(5):
        k.add(lambda bm, st=st: rbox(bm, (3.0, 0.45, 0.3), (0, -D / 2 - 0.3 - st * 0.42, 1.5 - (st + 1) * 0.3), 0.02, segs=1), wood)
    return k.obj('ShrineHall')

def build_snowpeak(seed=3741):
    """a far snowy volcano cone (backdrop): blue-grey slopes, a white snow cap with ragged streaks down the sides"""
    rnd = random.Random(seed)
    k = SKit(['stone', 'stone_light', 'snow', 'snow_shade'])
    segs = 48
    def rim(i, kk, rr, zz):
        a = i * 2 * math.pi / segs
        return rr * (1 + 0.04 * math.sin(a * 5 + 1.3) + 0.02 * math.sin(a * 13)), zz
    def colour(f):
        c = f.calc_center_median(); a = math.atan2(c.y, c.x)
        snowline = 0.62 + 0.12 * math.sin(a * 9) + 0.06 * math.sin(a * 23)
        if c.z / 30.0 > snowline: return 'snow' if f.normal.z > 0.2 else 'snow_shade'
        return 'stone_light' if f.normal.z > 0.45 else 'stone'
    k.add(lambda bm: lathe(bm, [(60.0, 0.0), (38.0, 8.0), (20.0, 18.0), (9.0, 26.0), (5.0, 30.0), (3.5, 30.6), (0.0, 30.2)], segs, rim), colour)
    return k.obj('SnowPeak')
