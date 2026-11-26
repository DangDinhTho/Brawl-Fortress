"""Savanna theme (wild Africa in the 1960s-70s: a game reserve with a safari expedition camp): granite kopjes, red
termite mounds, thorn-branch bomas, the fat bottle trunks of baobabs, flat-topped umbrella acacias, candelabra
euphorbias, golden elephant grass as the hiding bush, wooden expedition crates, canvas, hurricane lanterns, jerry cans,
a pith helmet; giraffes, elephants and a camp with an expedition truck out on the plain. Original designs in the
chunky Brawl style, same palette as the other themes; lantern glass goes to material slot 1 (the warm glow).

Walls ('#'): KopjeRockA/B, ThornBoma, TermiteMound, SafariStack (one family per wall).
Small ('x'): SavannaBoulder, DeadLog, ElephantSkull, WaterBarrels, SafariCrate.
Fence ('f'): ThornHedge.
Tall ('t'): BaobabTree, AcaciaTree, TermiteSpire, Euphorbia.
Bush ('b'): SavannaGrass (the Brawl leaf clump, narrow blades, the sand ramp: brown root to pale straw tips).
Decor: StrawTuft (the grass clump, small: not a bush), AloePlant, HornSkull, SavannaStones, PondReeds (by the water holes).
Border: SavannaRidgeA/B/C (low red-earth banks with granite boulders; B with a baobab, C with a ranger's lookout).
Gate: SafariGate. Backdrop: BaobabGiant, Giraffe, Elephant, SafariCamp."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import *
from props3 import blob
from props7 import tube
from props8 import Kit, rbox, sphere
from props16 import SKit
import grass

UP = Vector((0, 0, 1))

# ------------------------------------------------------------------ colours (by facing, never by the light)
def granite(f):
    """kopje granite: sun-bleached tops, warm grey sides with ochre weathering streaks, darker underneath"""
    n = f.normal
    if n.z > 0.55: return 'bone_dark'
    if n.z < -0.35: return 'sandstone_dark'
    c = f.calc_center_median()
    return 'khaki' if noise.noise(c * 1.3 + Vector((3.1, 0.7, 5.3))) > 0.28 else 'ash'

def earth(f):
    """red termite-mound earth, dusty on what faces up, darker in the grooves under the lumps"""
    n = f.normal
    return 'sandstone_dark' if n.z > 0.62 else ('wood' if n.z < -0.25 else 'terracotta')

def soil(f): return 'sandstone_dark' if f.normal.z > 0.5 else 'wood'          # the floor's red ochre, for skirts and banks
def deadwood(f): return 'bone_dark' if f.normal.z > 0.3 else 'ash'            # sun-bleached dead wood
def leaves(f):
    n = f.normal
    return 'palm' if n.z > 0.3 else ('palm_dark' if n.z < -0.45 else 'olive')
def canvas(f): return 'khaki' if f.normal.z > -0.3 else 'olive'

def skirt(k, sx, sy, seed, h=0.06, x=0.0, y=0.0, z=0.0):
    """red earth heaped round the foot of a piece (ties it to the floor)"""
    k.add(lambda bm: blob(bm, 1.0, (x, y, z), (sx, sy, h), 3, 0.02, seed), soil)

def tuft(k, x, y, z, s, rnd, n=7):
    """a tuft of dry savanna grass: tapered blades on the sand ramp (brown root, pale straw tip)"""
    for j in range(n):
        a = rnd.uniform(0, 6.28); h = rnd.uniform(0.35, 0.6) * s; lean = rnd.uniform(0.15, 0.4) * s
        d = Vector((math.cos(a), math.sin(a), 0)); b = Vector((x, y, z)) + d * 0.04 * s
        pts = [b, b + d * lean * 0.25 + UP * h * 0.55, b + d * lean + UP * h]
        k.grad(lambda bm, pts=pts: tube(bm, pts, 0.035 * s, 0.004, 3), 'sand_grad', lambda v, z=z, h=h: min(1.0, max(0.0, (v.co.z - z) / h)))

def boulder(k, x, y, z, sx, sy, sz, seed, floor=False):
    k.add(lambda bm: blob(bm, 1.0, (x, y, z), (sx, sy, sz), 2, 0.07 * min(sx, sy, sz) / 0.5, seed, floor=floor), granite)

def lichen(k, c, sc, rnd, n=4):
    """flat orange / yellow lichen spots on the upper side of a boulder (centre c, half sizes sc)"""
    for j in range(n):
        a = rnd.uniform(0, 6.28); u = rnd.uniform(0.35, 0.8)
        d = Vector((math.cos(a) * math.sqrt(1 - u * u), math.sin(a) * math.sqrt(1 - u * u), u))
        p = Vector((c[0] + d.x * sc[0] * 0.97, c[1] + d.y * sc[1] * 0.97, c[2] + d.z * sc[2] * 0.97))
        r = rnd.uniform(0.07, 0.13)
        k.add(lambda bm, p=p, r=r: blob(bm, 1.0, (p.x, p.y, p.z), (r, r, r * 0.4), 1, 0.01, 0, floor=False), rnd.choice(['orange', 'yellow', 'orange']))

def disc_facing(bm, p, d, r, segs=10):
    """a flat disc at p looking along d (dark openings: eye sockets, holes)"""
    M = Matrix.Translation(p) @ d.normalized().to_track_quat('Z', 'Y').to_matrix().to_4x4()
    lathe(bm, [(r, 0.0), (0.0, 0.0)], segs, None, 0, M)

# ------------------------------------------------------------------ trees
# baobab bark: pinkish brown. The palette is full and has no such colour, so bark faces go to material slot 1 on the
# sand ramp (dark foot -> light top) and that slot is BaobabBark.mat in Unity: the lit props material tinted pink-brown
# (SavannaThemeSetup.Bark), like the candy pink. BARK_TINT is the same tint for the Blender preview.
BARK_TINT = (0.80, 0.69, 0.88)

def bark_preview(ob):
    """Blender preview only, called by build_all AFTER the FBX is exported: the slot-1 material lit and tinted like
    BaobabBark in Unity (the material name keeps sorting after 'IcePalette', see props18.pink_preview)"""
    if len(ob.data.materials) < 2: return ob
    m = ob.data.materials[1].copy(); m.name = 'XmasGlowBark'
    nt = m.node_tree; bsdf = nt.nodes['Principled BSDF']; tex = nt.nodes['Image Texture']
    mix = nt.nodes.new('ShaderNodeMixRGB'); mix.blend_type = 'MULTIPLY'; mix.inputs['Fac'].default_value = 1.0
    mix.inputs['Color2'].default_value = BARK_TINT + (1.0,)
    nt.links.new(tex.outputs['Color'], mix.inputs['Color1']); nt.links.new(mix.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Emission Strength'].default_value = 0.0
    ob.data.materials[1] = m
    return ob

def bezier(p0, p1, p2, n=6):
    return [p0 * (1 - t) ** 2 + p1 * (2 * (1 - t) * t) + p2 * (t * t) for t in (i / (n - 1) for i in range(n))]

def pad(bm, c, sx, sy, sz, yaw=0.0, seed=0):
    """a clump of foliage: a lumpy flattened ball (jittered: an uneven edge, not a disc), smooth shaded"""
    M = Matrix.Translation(c) @ Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Diagonal((sx, sy, sz, 1))
    vs = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix())['verts']
    o = Vector((seed * 1.37 % 17.0, seed * 2.11 % 13.0, seed * 0.73 % 11.0))
    for v in vs:   # soft lumps from smooth noise (per-vertex random gave faceted, spiky silhouettes)
        k = 1.0 + 0.2 * noise.noise(v.co * 1.7 + o) * (1.0 - 0.5 * abs(v.co.z))
        v.co = Vector((v.co.x * k, v.co.y * k, v.co.z * (1.0 + 0.12 * noise.noise(v.co * 2.1 + o * 0.5))))
    bmesh.ops.transform(bm, matrix=M, verts=vs)

def baobab(k, x, y, z0, H, rnd, seed, nb=7):
    """a baobab like the Avenue of the Baobabs: a tall smooth column of pinkish-brown bark, gently flared at the foot
    and tapering a little, shallow vertical grooves; at the very top a crown of curved branches spreading up and out like
    an umbrella, forking at the ends, flat soft clumps of foliage on the tips forming a flat-topped crown"""
    s = H / 5.6
    Ht = H * 0.68                      # the column, up to where the branches start
    R = 0.58 * s
    ph = rnd.uniform(0, 6.28)
    lean = (rnd.uniform(-0.02, 0.02), rnd.uniform(-0.02, 0.02))
    prof = [(R * 1.34, 0.0), (R * 1.14, 0.12 * s), (R * 1.04, 0.42 * s), (R * 1.02, Ht * 0.25), (R * 1.04, Ht * 0.4), (R * 1.0, Ht * 0.55),
            (R * 0.95, Ht * 0.7), (R * 0.88, Ht * 0.83), (R * 0.8, Ht * 0.93), (R * 0.72, Ht), (R * 0.56, Ht + 0.14 * s), (R * 0.3, Ht + 0.25 * s), (0.0, Ht + 0.29 * s)]
    segs = 28
    def rim(i, kk, rr, zz):
        a = i * 2 * math.pi / segs
        g = 0.022 * math.sin(a * 9 + ph) + 0.014 * math.sin(a * 15 + ph * 1.7) + 0.012 * math.sin(a * 4 + zz * 0.7 + ph)
        return rr * (1 + g), zz
    M = Matrix.Translation((x, y, z0)) @ Matrix.Shear('XY', 4, lean)
    Htop = Ht + 0.3 * s
    bt = lambda z: 0.04 + 0.72 * min(1.0, max(0.0, (z - z0) / (H * 0.95)))
    k.grad(lambda bm: lathe(bm, prof, segs, rim, 0, M), 'sand_grad', lambda v: bt(v.co.z), glow=True)
    C = Vector((x + lean[0] * Ht, y + lean[1] * Ht, z0 + Ht + 0.12 * s))
    tips = []
    for j in range(nb):
        a = j * 2 * math.pi / nb + rnd.uniform(-0.22, 0.22)
        d = Vector((math.cos(a), math.sin(a), 0)); L = s * rnd.uniform(1.5, 2.0)
        rise = rnd.uniform(0.7, 1.0)
        p0 = C + d * R * 0.32 - UP * 0.1 * s
        p1 = p0 + d * L * 0.42 + UP * L * 0.18 * rise
        p2 = p0 + d * L * 0.82 + UP * L * 0.72 * rise
        pts = bezier(p0, p1, p2, 7)
        r0 = 0.2 * s
        k.grad(lambda bm, pts=pts, r0=r0: tube(bm, pts, r0, r0 * 0.36, 10), 'sand_grad', lambda v: bt(v.co.z), glow=True)
        side = Vector((-d.y, d.x, 0))
        forks = [(pts[-1], d * 0.55 + side * 0.75, 0.6), (pts[-1], d * 0.55 - side * 0.75, 0.55), (pts[4], d * 0.4 + side * rnd.choice((-0.6, 0.6)), 0.8)]
        for (b, dd, up) in forks[:rnd.choice((2, 3, 3))]:
            q2 = b + dd.normalized() * L * 0.36 + UP * L * up * 0.42
            q1 = b + dd.normalized() * L * 0.2 + UP * L * up * 0.1
            q = bezier(b, q1, q2, 4)
            k.grad(lambda bm, q=q, r0=r0: tube(bm, q, r0 * 0.34, r0 * 0.13, 9), 'sand_grad', lambda v: bt(v.co.z), glow=True)   # 9 sides: 7 (51 deg) went past the 50 deg smoothing angle
            tips.append(q2)
        tips.append(pts[-1] + d * 0.08 * s + UP * 0.1 * s)
    zc = max(t.z for t in tips)
    for j, t in enumerate(tips):   # small soft clumps on the twig ends, the branches showing between them; the
        # highest are pressed down a little so the crown has the flat top of the baobab avenue
        c = Vector((t.x, t.y, max(t.z + 0.03 * s, min(t.z + 0.1 * s, zc - 0.05 * s))))   # always over the twig end
        sc = s * rnd.uniform(0.85, 1.15)
        # coloured per vertex on the papyrus ramp (dark green underneath -> light green on top): a smooth clump, not
        # the facet mosaic that colouring each triangle by its facing gave
        lt = lambda v, c=c, h=0.26 * sc: 0.12 + 0.68 * min(1.0, max(0.0, (v.co.z - (c.z - h)) / (2.0 * h)))
        k.grad(lambda bm, c=c, sc=sc, j=j: pad(bm, c, 0.46 * sc, 0.4 * sc, 0.24 * sc, j * 1.7, seed + j), 'papyrus_grad', lt)
        if j % 2 == 0:   # a second, smaller clump on top, off centre: a cloud, not a plate
            c2 = c + Vector((math.cos(j * 2.3), math.sin(j * 2.3), 0)) * 0.18 * sc + UP * 0.14 * sc
            k.grad(lambda bm, c2=c2, sc=sc, j=j: pad(bm, c2, 0.28 * sc, 0.26 * sc, 0.17 * sc, j, seed + 50 + j), 'papyrus_grad', lt)

def acacia(k, x, y, z0, H, rnd, seed, R=1.5):
    """an umbrella thorn acacia: a crooked dark trunk splitting into limbs, a wide flat canopy in two layers"""
    fork = Vector((x + 0.12 * H / 4.2, y + 0.05, z0 + H * 0.36))
    k.add(lambda bm: tube(bm, [Vector((x, y, z0)), Vector((x + 0.05, y, z0 + H * 0.18)), fork], 0.17 * H / 4.2, 0.12 * H / 4.2, 8), 'wood_dark')
    k.add(lambda bm: lathe(bm, [(0.3 * H / 4.2, 0.0), (0.17 * H / 4.2, 0.25), (0.0, 0.3)], 8, None, 0, Matrix.Translation((x, y, z0))), 'wood_dark')
    tops = []
    for j in range(4):
        a = j * 2 * math.pi / 4 + rnd.uniform(-0.4, 0.4)
        d = Vector((math.cos(a), math.sin(a), 0))
        mid = fork + d * R * 0.3 + UP * H * 0.2
        top = fork + d * R * rnd.uniform(0.5, 0.7) + UP * H * 0.42
        k.add(lambda bm, mid=mid, top=top: tube(bm, [fork, mid, top], 0.1 * H / 4.2, 0.05 * H / 4.2, 6), 'wood_dark')
        tops.append(top)
    zc = z0 + H * 0.82
    k.add(lambda bm: blob(bm, 1.0, (x + 0.1, y, zc), (R, R * 0.92, 0.3 * H / 4.2), 2, 0.07, seed, floor=False), leaves)
    k.add(lambda bm: blob(bm, 1.0, (x - 0.25, y + 0.2, zc + 0.28 * H / 4.2), (R * 0.62, R * 0.58, 0.22 * H / 4.2), 2, 0.05, seed + 1, floor=False), leaves)
    for j, t in enumerate(tops):   # a few clumps sticking out of the rim
        k.add(lambda bm, t=t, j=j: blob(bm, 1.0, (t.x * 1.15 - x * 0.15, t.y * 1.15 - y * 0.15, zc - 0.05), (0.45, 0.42, 0.18), 1, 0.04, seed + 5 + j, floor=False), leaves)

# ------------------------------------------------------------------ safari kit (1960s-70s)
def crate(k, x, y, z, sx, sy, sz, yaw=0.0):
    """a wooden expedition crate: light boards, darker corner battens and a band round the middle"""
    k.add(lambda bm: rbox(bm, (sx, sy, sz), (x, y, z), 0.025, yaw, segs=1), 'wood_light')
    R = Matrix.Rotation(yaw, 4, 'Z')
    for cx in (-1, 1):
        for cy in (-1, 1):
            o = R @ Vector((cx * (sx / 2 - 0.035), cy * (sy / 2 - 0.035), 0))
            k.add(lambda bm, o=o: rbox(bm, (0.08, 0.08, sz + 0.01), (x + o.x, y + o.y, z), 0.01, yaw, segs=1), 'wood')
    k.add(lambda bm: rbox(bm, (sx + 0.02, sy + 0.02, 0.07), (x, y, z + sz * 0.5 - 0.035), 0.01, yaw, segs=1), 'wood')

def jerrycan(k, x, y, z, yaw=0.0, col='olive'):
    R = Matrix.Rotation(yaw, 4, 'Z')
    k.add(lambda bm: rbox(bm, (0.34, 0.16, 0.46), (x, y, z), 0.03, yaw, segs=1), col)
    k.add(lambda bm: rbox(bm, (0.36, 0.17, 0.05), (x, y, z + 0.2), 0.01, yaw, segs=1), col)   # the pressed rim
    for dx in (-0.08, 0.0, 0.08):   # three handles
        o = R @ Vector((dx, 0, 0))
        k.add(lambda bm, o=o: tube(bm, [Vector((x + o.x, y + o.y, z + 0.46)), Vector((x + o.x, y + o.y, z + 0.53))], 0.018, 0.018, 4), col)
    o = R @ Vector((0.13, 0, 0))
    k.add(lambda bm: lathe(bm, [(0.035, 0.0), (0.035, 0.06), (0.0, 0.06)], 8, None, 0, Matrix.Translation((x + o.x, y + o.y, z + 0.44))), 'metal')

def hurricane_lantern(k, x, y, z, s=1.0):
    """a 1960s hurricane lantern: red fuel tank, wire frame, a glowing glass globe, a ring handle"""
    M = Matrix.Translation((x, y, z))
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.09 * s, 0.0), (0.1 * s, 0.05 * s), (0.08 * s, 0.08 * s), (0.0, 0.08 * s)], 10, None, 0, M), 'red')
    k.add(lambda bm: lathe(bm, [(0.0, 0.08 * s), (0.055 * s, 0.09 * s), (0.07 * s, 0.15 * s), (0.055 * s, 0.22 * s), (0.0, 0.23 * s)], 10, None, 0, M), 'lamp_warm', glow=True)
    k.add(lambda bm: lathe(bm, [(0.0, 0.23 * s), (0.07 * s, 0.23 * s), (0.04 * s, 0.28 * s), (0.0, 0.29 * s)], 10, None, 0, M), 'red')
    for a in (0.6, 2.7, 4.8):
        o = Vector((math.cos(a), math.sin(a), 0)) * 0.08 * s
        k.add(lambda bm, o=o: tube(bm, [M @ (o + Vector((0, 0, 0.07 * s))), M @ (o + Vector((0, 0, 0.24 * s)))], 0.008 * s, 0.008 * s, 3), 'metal')
    k.add(lambda bm: lathe(bm, [(0.06 * s, -0.008 * s), (0.07 * s, 0.0), (0.06 * s, 0.008 * s), (0.05 * s, 0.0)], 10, None, 0, M @ Matrix.Translation((0, 0, 0.36 * s)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), 'metal')

def pith_helmet(k, x, y, z, s=1.0, yaw=0.0):
    M = Matrix.Translation((x, y, z)) @ Matrix.Rotation(yaw, 4, 'Z')
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.2 * s, 0.0), (0.22 * s, 0.015 * s), (0.13 * s, 0.03 * s), (0.13 * s, 0.1 * s), (0.08 * s, 0.17 * s), (0.0, 0.19 * s)], 12,
                             lambda i, kk, rr, zz: (rr * (1.18 if kk <= 2 and (i < 3 or i > 9) else 1.0), zz), 0, M @ Matrix.Diagonal((1.0, 1.18, 1.0, 1))), 'khaki')
    k.add(lambda bm: lathe(bm, [(0.135 * s, 0.035 * s), (0.135 * s, 0.065 * s), (0.0, 0.065 * s)], 12, None, 0, M @ Matrix.Diagonal((1.0, 1.18, 1.0, 1))), 'bone_dark')

def thorn_branch(k, p0, d, L, rnd, col=None):
    """a cut thorn-tree branch: a crooked bleached stick with forks and short pale thorns"""
    col = col or rnd.choice(['ash', 'bone_dark', 'wood'])
    side = d.cross(UP).normalized() if abs(d.z) < 0.9 else Vector((1, 0, 0))
    p1 = p0 + d * L * 0.5 + side * rnd.uniform(-0.08, 0.08) + UP * rnd.uniform(-0.05, 0.08)
    p2 = p0 + d * L + side * rnd.uniform(-0.12, 0.12) + UP * rnd.uniform(-0.08, 0.1)
    r = rnd.uniform(0.04, 0.06)
    k.add(lambda bm: tube(bm, [p0, p1, p2], r, r * 0.4, 5), col)
    for j in range(2):
        b = p0.lerp(p2, rnd.uniform(0.35, 0.8))
        t = b + (d * 0.4 + side * rnd.choice((-1, 1)) + UP * rnd.uniform(-0.3, 0.6)).normalized() * L * rnd.uniform(0.25, 0.4)
        k.add(lambda bm, b=b, t=t: tube(bm, [b, t], r * 0.5, r * 0.2, 4), col)
        for m in range(2):   # thorns
            q = b.lerp(t, rnd.uniform(0.2, 0.9))
            dd = Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(-0.3, 1))).normalized()
            k.add(lambda bm, q=q, dd=dd: tube(bm, [q, q + dd * 0.09], 0.012, 0.002, 3), 'bone')

# ------------------------------------------------------------------ walls
def build_kopjerock(kind, seed, name):
    """a kopje: rounded granite boulders piled and balanced (A: a big one with a second balanced on top; B: a tilted
    slab leaning on a round boulder), orange lichen, dry grass and an aloe in the joints"""
    rnd = random.Random(seed)
    k = SKit(['bone_dark', 'ash', 'khaki', 'sandstone_dark', 'orange', 'yellow', 'wood', 'pine_light', 'pine', 'red'])
    if kind == 'A':
        parts = [((0.0, 0.0, 0.5), (0.74, 0.7, 0.58)), ((0.12, -0.05, 1.38), (0.52, 0.48, 0.42)), ((-0.45, 0.42, 0.32), (0.34, 0.32, 0.3))]
        for j, (c, sc) in enumerate(parts):
            boulder(k, c[0], c[1], c[2], sc[0], sc[1], sc[2], seed + j * 7, floor=(j != 1))
            lichen(k, c, sc, rnd, 3 if j == 0 else 2)
    else:   # a big round boulder split in two by the weather, the halves leaning apart, a small one in the crack
        for j, sx in enumerate((-1, 1)):
            M = Matrix.Translation((sx * 0.36, 0.0, 0.0)) @ Matrix.Rotation(sx * 0.12, 4, 'Y')
            def half(bm, sx=sx, M=M, j=j):
                vs = bmesh.ops.create_icosphere(bm, subdivisions=3, radius=1.0, matrix=Matrix.Diagonal((0.42, 0.72, 0.85, 1)))['verts']
                jitter(vs, 0.05, seed + j)
                for v in vs:
                    if v.co.x * sx < -0.02: v.co.x = -0.02 * sx         # the flat split face
                    v.co.z = max(0.0, v.co.z + 0.85 * 0.92)
                bmesh.ops.transform(bm, matrix=M, verts=vs)
            k.add(half, granite)
            lichen(k, (sx * 0.5, 0.0, 0.95), (0.35, 0.6, 0.75), rnd, 3)
        boulder(k, 0.0, -0.35, 0.95, 0.18, 0.2, 0.16, seed + 9)
        boulder(k, 0.62, -0.5, 0.18, 0.26, 0.24, 0.2, seed + 11, floor=True)
    tuft(k, 0.55, -0.55, 0.0, 1.0, rnd); tuft(k, -0.6, -0.45, 0.0, 0.8, rnd, 5)
    aloe(k, -0.55, 0.6 if kind == 'A' else -0.65, 0.0, 0.7, rnd)
    skirt(k, 0.85, 0.85, seed)
    return k.obj(name)

def build_thornboma(seed=3201):
    """a boma: a hedge of cut thorn branches piled between stakes (the old way to fence a camp against lions)"""
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'ash', 'bone_dark', 'wood', 'wood_light', 'bone', 'sandstone_dark', 'olive'])
    for row, y in enumerate((-0.12, 0.12)):   # two staggered rows of rough stakes, sharpened
        for j in range(8):
            x = -0.68 + j * 0.19 + (0.095 if row else 0.0) + rnd.uniform(-0.025, 0.025)
            h = rnd.uniform(1.45, 1.85); top = Vector((x + rnd.uniform(-0.04, 0.04), y + rnd.uniform(-0.04, 0.04), h))
            col = rnd.choice(['bone_dark', 'ash', 'wood_light', 'bone_dark'])
            k.add(lambda bm, x=x, y=y, top=top: tube(bm, [Vector((x, y, 0.0)), top], 0.075, 0.062, 6), col)
            k.add(lambda bm, top=top: lathe(bm, [(0.062, 0.0), (0.0, 0.16)], 6, None, 0, Matrix.Translation(top)), col)
    for z in (0.55, 1.2):   # bark lashing rails on both faces
        for y in (-0.21, 0.21):
            k.add(lambda bm, z=z, y=y: tube(bm, [Vector((-0.78, y, z + rnd.uniform(-0.04, 0.04))), Vector((0.0, y * 1.08, z)), Vector((0.78, y, z + rnd.uniform(-0.04, 0.04)))], 0.045, 0.045, 5), 'wood_dark')
    for j in range(14):   # thorn brush heaped against both faces
        side = 1 if j % 2 else -1
        p0 = Vector((rnd.uniform(-0.8, 0.1), side * rnd.uniform(0.3, 0.55), rnd.uniform(0.08, 0.55)))
        d = Vector((1.0, side * rnd.uniform(-0.1, 0.25), rnd.uniform(-0.1, 0.35))).normalized()
        thorn_branch(k, p0, d, rnd.uniform(0.6, 0.85), rnd)
    for j in range(4):   # and woven along the top
        p0 = Vector((rnd.uniform(-0.75, -0.1), rnd.uniform(-0.1, 0.1), rnd.uniform(1.35, 1.6)))
        thorn_branch(k, p0, Vector((1.0, rnd.uniform(-0.3, 0.3), rnd.uniform(-0.1, 0.2))).normalized(), rnd.uniform(0.6, 0.8), rnd)
    skirt(k, 0.85, 0.72, seed)
    return k.obj('ThornBoma')

def mound(k, x, y, H, R, rnd, seed, spires=((0.35, 0.25, 0.62), (-0.3, 0.3, 0.42))):
    """a termite mound: a lumpy tapering column of red earth with fused side spires and air holes"""
    ph = rnd.uniform(0, 6.28)
    def rim(i, kk, rr, zz, segs=16):
        a = i * 6.28 / segs
        return rr * (1 + 0.16 * math.sin(a * 4 + ph + zz * 1.6) + 0.08 * math.sin(a * 9 + zz * 5) + 0.06 * math.sin(zz * 7 + a)), zz
    prof = [(R * 1.08, 0.0), (R * 0.9, H * 0.1), (R * 0.84, H * 0.25), (R * 0.62, H * 0.42), (R * 0.56, H * 0.55), (R * 0.4, H * 0.7),
            (R * 0.34, H * 0.8), (R * 0.2, H * 0.93), (R * 0.1, H), (0.0, H * 1.02)]
    T = Matrix.Translation((x, y, 0)) @ Matrix.Shear('XY', 4, (0.04, -0.03))
    k.add(lambda bm: lathe(bm, prof, 16, rim, 0, T), earth)
    for j, (sx, sy, hf) in enumerate(spires):   # fused side spires, leaning a little out
        h = H * hf; r = R * 0.62
        p2 = [(r, 0.0), (r * 0.8, h * 0.3), (r * 0.62, h * 0.55), (r * 0.36, h * 0.8), (r * 0.14, h), (0.0, h * 1.02)]
        Ms = Matrix.Translation((x + sx * R * 0.85, y + sy * R * 0.85, 0)) @ Matrix.Shear('XY', 4, (sx * 0.12, sy * 0.12))
        k.add(lambda bm, p2=p2, Ms=Ms: lathe(bm, p2, 12, lambda i, kk, rr, zz: rim(i, kk, rr, zz, 12), 0.4, Ms), earth)
    for j in range(9):   # lumps of fresh earth round the foot and up the sides
        a = rnd.uniform(0, 6.28); z = rnd.uniform(0.05, 0.55) * H
        rr = R * (1.02 - z / H * 0.85)
        sz = rnd.uniform(0.16, 0.26) * R / 0.72
        k.add(lambda bm, a=a, z=z, rr=rr, j=j, sz=sz: blob(bm, 1.0, (x + math.cos(a) * rr, y + math.sin(a) * rr, z), (sz, sz, sz * 1.3), 1, 0.04, seed + j, floor=False), earth)
    for j in range(3):   # air holes
        a = ph + j * 2.1; z = H * (0.35 + j * 0.15); rr = R * (1 - z / H * 0.75) * 0.98
        d = Vector((math.cos(a), math.sin(a), 0.15))
        k.add(lambda bm, a=a, z=z, rr=rr, d=d: disc_facing(bm, Vector((x + math.cos(a) * rr, y + math.sin(a) * rr, z)), d, 0.06, 8), 'wood_dark')

def build_termitemound(seed=3211):
    rnd = random.Random(seed)
    k = SKit(['terracotta', 'sandstone_dark', 'wood', 'wood_dark', 'khaki'])
    mound(k, 0.0, 0.0, 2.35, 0.72, rnd, seed, spires=((0.55, 0.3, 0.6), (-0.45, 0.4, 0.42), (0.1, -0.6, 0.32)))
    tuft(k, 0.6, -0.5, 0.0, 0.8, rnd, 5)
    skirt(k, 0.85, 0.85, seed, h=0.1)
    return k.obj('TermiteMound')

def build_safaristack(seed=3221):
    """expedition stores: wooden crates stacked, a canvas tarp roped over the top one, jerry cans, a hurricane lantern"""
    rnd = random.Random(seed)
    k = SKit(['wood_light', 'wood', 'wood_dark', 'khaki', 'olive', 'bone_dark', 'metal', 'red', 'lamp_warm', 'sandstone_dark'])
    crate(k, -0.22, -0.12, 0.0, 0.95, 0.8, 0.78, 0.05)
    crate(k, 0.42, 0.32, 0.0, 0.62, 0.7, 0.62, -0.2)
    crate(k, -0.12, 0.05, 0.78, 0.82, 0.66, 0.56, 0.22)
    k.add(lambda bm: blob(bm, 1.0, (-0.12, 0.05, 1.34), (0.5, 0.42, 0.09), 2, 0.02, seed, floor=False), canvas)   # tarp over the top crate
    for s in (-1, 1):
        k.add(lambda bm, s=s: bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((-0.12, 0.05, 1.08)) @ Matrix.Rotation(0.22, 4, 'Z') @ Matrix.Translation((s * 0.44, 0, 0)) @ Matrix.Diagonal((0.04, 0.72, 0.52, 1))), canvas)
    for dz in (1.0, 1.25):   # ropes over the tarp
        k.add(lambda bm, dz=dz: tube(bm, [Vector((-0.6, -0.1, dz - 0.1)), Vector((-0.45, 0.0, 1.43)), Vector((0.2, 0.15, 1.43)), Vector((0.35, 0.22, dz - 0.1))], 0.018, 0.018, 4), 'bone_dark')
    jerrycan(k, 0.45, -0.45, 0.0, 0.3)
    jerrycan(k, 0.08, -0.58, 0.0, -0.1, 'enamel')
    hurricane_lantern(k, 0.42, 0.32, 0.62, 1.1)
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.16, 0.0), (0.17, 0.05), (0.1, 0.06), (0.0, 0.06)], 12, None, 0, Matrix.Translation((-0.62, -0.62, 0.0))), 'bone_dark')   # coiled rope
    skirt(k, 0.85, 0.8, seed)
    return k.obj('SafariStack')

# ------------------------------------------------------------------ small
def build_savannaboulder(seed=3231):
    rnd = random.Random(seed)
    k = SKit(['bone_dark', 'ash', 'khaki', 'sandstone_dark', 'orange', 'yellow', 'wood'])
    boulder(k, -0.1, 0.0, 0.42, 0.6, 0.55, 0.48, seed, floor=True); lichen(k, (-0.1, 0.0, 0.42), (0.6, 0.55, 0.48), rnd, 3)
    boulder(k, 0.48, 0.3, 0.2, 0.3, 0.28, 0.24, seed + 3, floor=True)
    tuft(k, 0.4, -0.45, 0.0, 0.8, rnd, 6)
    skirt(k, 0.8, 0.7, seed)
    return k.obj('SavannaBoulder')

def build_deadlog(seed=3241):
    """a fallen dead acacia trunk bleached by the sun, broken branch stubs, dry grass round it"""
    rnd = random.Random(seed)
    k = SKit(['bone_dark', 'ash', 'wood_light', 'wood', 'wood_dark', 'sandstone_dark'])
    pts = [Vector((-0.75, -0.1, 0.2)), Vector((-0.2, 0.05, 0.24)), Vector((0.35, -0.02, 0.22)), Vector((0.75, 0.1, 0.18))]
    k.add(lambda bm: tube(bm, pts, 0.24, 0.17, 9), lambda f: 'wood_light' if abs(f.normal.x) > 0.85 else deadwood(f))
    for (t, a, L) in [(0.3, 0.5, 0.55), (0.55, -0.9, 0.4), (0.8, 2.2, 0.35)]:
        b = pts[0].lerp(pts[-1], t) + UP * 0.18
        d = Vector((math.cos(a) * 0.4, math.sin(a) * 0.4, 0.9)).normalized()
        k.add(lambda bm, b=b, d=d, L=L: tube(bm, [b, b + d * L], 0.08, 0.05, 6), deadwood)
    k.add(lambda bm: disc_facing(bm, pts[0] + Vector((-0.005, 0, 0)), Vector((-1, -0.25, 0.05)), 0.12, 8), 'wood')   # the dark heart of the broken end
    tuft(k, 0.1, -0.35, 0.0, 0.8, rnd, 6); tuft(k, -0.45, 0.32, 0.0, 0.7, rnd, 5)
    skirt(k, 0.85, 0.45, seed)
    return k.obj('DeadLog')

def build_elephantskull(seed=3251):
    """a sun-bleached elephant skull half sunk in the earth: the big domed cranium, the trunk opening in the forehead,
    eye sockets, empty tusk sockets, the lower jaw lying beside it"""
    rnd = random.Random(seed)
    k = SKit(['bone', 'bone_dark', 'wood_dark', 'sandstone_dark', 'wood', 'khaki'])
    bonec = lambda f: 'bone' if f.normal.z > -0.2 else 'bone_dark'
    k.add(lambda bm: sphere(bm, 0.5, (0, 0.05, 0.42), (1.05, 0.9, 0.82), 2), bonec)
    k.add(lambda bm: sphere(bm, 0.3, (0, -0.32, 0.42), (1.15, 0.6, 0.95), 2), bonec)                 # the face
    k.add(lambda bm: disc_facing(bm, Vector((0, -0.5, 0.56)), Vector((0, -1, 0.15)), 0.12, 10), 'wood_dark')   # trunk opening
    for s in (-1, 1):
        k.add(lambda bm, s=s: disc_facing(bm, Vector((s * 0.33, -0.38, 0.5)), Vector((s * 0.8, -0.6, 0.1)), 0.075, 8), 'wood_dark')   # eye sockets
        a = Vector((s * 0.2, -0.4, 0.3)); b = Vector((s * 0.24, -0.56, 0.2))
        k.add(lambda bm, a=a, b=b: tube(bm, [a, b], 0.11, 0.105, 8), bonec)                          # tusk sockets (empty)
        k.add(lambda bm, b=b: disc_facing(bm, b + Vector((0, -0.02, -0.01)), Vector((0, -0.85, -0.5)), 0.075, 8), 'wood_dark')
    jaw = [Vector((0.75, -0.25, 0.08)), Vector((0.62, -0.62, 0.1)), Vector((0.95, -0.72, 0.1)), Vector((1.0, -0.35, 0.08))]
    k.add(lambda bm: tube(bm, jaw, 0.08, 0.07, 6), bonec)
    skirt(k, 0.85, 0.75, seed, h=0.12)
    tuft(k, -0.55, 0.45, 0.0, 0.8, rnd, 6)
    return k.obj('ElephantSkull')

def build_waterbarrels(seed=3261):
    """a camp's water: an oak barrel with iron hoops, jerry cans and a tin bucket"""
    rnd = random.Random(seed)
    k = SKit(['wood', 'wood_dark', 'metal', 'olive', 'enamel', 'sandstone_dark', 'ice_deep'])
    M = Matrix.Translation((-0.25, 0.15, 0.0))
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.3, 0.0), (0.35, 0.42), (0.3, 0.86), (0.0, 0.86)], 14, None, 0, M), lambda f: 'wood_dark' if f.normal.z > 0.9 else 'wood')
    for z, r in ((0.12, 0.315), (0.42, 0.355), (0.72, 0.32)):
        k.add(lambda bm, z=z, r=r: lathe(bm, [(r, z), (r + 0.012, z + 0.02), (r + 0.012, z + 0.06), (r, z + 0.08)], 14, None, 0, M), 'metal')
    jerrycan(k, 0.38, -0.2, 0.0, 0.4)
    jerrycan(k, 0.45, 0.25, 0.0, 1.9, 'enamel')
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.13, 0.0), (0.16, 0.24), (0.14, 0.24), (0.11, 0.02), (0.0, 0.02)], 12, None, 0, Matrix.Translation((-0.2, -0.5, 0.0))), 'metal')
    k.add(lambda bm: lathe(bm, [(0.0, 0.16), (0.14, 0.16), (0.0, 0.16)], 12, None, 0, Matrix.Translation((-0.2, -0.5, 0.0))), 'ice_deep')   # water in it
    skirt(k, 0.8, 0.7, seed)
    return k.obj('WaterBarrels')

def build_safaricrate(seed=3271):
    """a camp crate with a rolled canvas bedroll on it, a pith helmet and a hurricane lantern"""
    rnd = random.Random(seed)
    k = SKit(['wood_light', 'wood', 'wood_dark', 'khaki', 'olive', 'bone_dark', 'metal', 'red', 'lamp_warm', 'sandstone_dark'])
    crate(k, 0.0, 0.0, 0.0, 1.0, 0.7, 0.62, 0.0)
    k.add(lambda bm: tube(bm, [Vector((-0.42, 0.08, 0.76)), Vector((0.2, 0.08, 0.76))], 0.14, 0.14, 10), canvas)   # bedroll
    for x in (-0.28, 0.06):
        k.add(lambda bm, x=x: lathe(bm, [(0.145, -0.02), (0.15, 0.0), (0.145, 0.02)], 10, None, 0, Matrix.Translation((x, 0.08, 0.76)) @ Matrix.Rotation(math.pi / 2, 4, 'Y')), 'wood_dark')
    pith_helmet(k, 0.3, -0.12, 0.62, 1.0, 0.4)
    hurricane_lantern(k, 0.36, 0.2, 0.62, 1.0)
    skirt(k, 0.75, 0.6, seed)
    return k.obj('SafariCrate')

# ------------------------------------------------------------------ fence
def build_thornhedge(seed=3281):
    """low cover: thorn branches piled along a fallen bleached log"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'wood', 'wood_dark', 'bone', 'wood_light', 'sandstone_dark'])
    k.add(lambda bm: tube(bm, [Vector((-0.75, 0.0, 0.16)), Vector((0.0, 0.05, 0.18)), Vector((0.75, -0.02, 0.15))], 0.17, 0.14, 8), lambda f: 'wood_light' if abs(f.normal.x) > 0.85 else deadwood(f))
    for j in range(12):
        z = 0.2 + (j % 4) * 0.15
        p0 = Vector((rnd.uniform(-0.8, 0.2), rnd.uniform(-0.25, 0.25), z))
        d = Vector((1.0, rnd.uniform(-0.3, 0.3), rnd.uniform(-0.1, 0.35))).normalized()
        thorn_branch(k, p0, d, rnd.uniform(0.6, 0.9), rnd)
    tuft(k, 0.5, -0.32, 0.0, 0.7, rnd, 5)
    skirt(k, 0.85, 0.4, seed)
    return k.obj('ThornHedge')

# ------------------------------------------------------------------ tall
def build_baobabtree(seed=3291, name='BaobabTree', H=5.6, nb=7):
    rnd = random.Random(seed)
    k = SKit(['palm', 'palm_dark', 'olive', 'sandstone_dark', 'wood'])
    baobab(k, 0, 0, 0, H, rnd, seed, nb=nb)
    s = H / 5.6
    for j in range(5 if H < 7 else 10):
        a = rnd.uniform(0, 6.28); r = 0.9 * s * rnd.uniform(0.95, 1.25)
        tuft(k, math.cos(a) * r, math.sin(a) * r, 0.0, min(1.2, s * 0.9), rnd, 6)
    skirt(k, 1.0 * s, 1.0 * s, seed, h=0.07 * s)
    ob = k.obj(name); ob['bark_preview'] = 1
    return ob

def build_acaciatree(seed=3301):
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'palm', 'palm_dark', 'olive', 'sandstone_dark', 'wood'])
    acacia(k, 0, 0, 0, 4.2, rnd, seed, R=1.45)
    tuft(k, 0.45, -0.35, 0.0, 0.8, rnd, 6)
    skirt(k, 0.55, 0.55, seed)
    return k.obj('AcaciaTree')

def build_termitespire(seed=3311):
    rnd = random.Random(seed)
    k = SKit(['terracotta', 'sandstone_dark', 'wood', 'wood_dark', 'khaki'])
    mound(k, 0.0, 0.0, 3.2, 0.55, rnd, seed, spires=((0.5, 0.2, 0.55), (-0.35, -0.35, 0.32)))
    skirt(k, 0.7, 0.7, seed, h=0.1)
    return k.obj('TermiteSpire')

def build_euphorbia(seed=3321):
    """a candelabra euphorbia: a short grey trunk holding up a crown of ribbed green arms that bend upwards"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'bone_dark', 'pine_light', 'pine', 'pine_dark', 'red', 'yellow', 'sandstone_dark'])
    top = Vector((0.03, 0.0, 1.3))
    k.add(lambda bm: tube(bm, [Vector((0, 0, 0)), Vector((0.02, 0, 0.7)), top], 0.17, 0.14, 8), deadwood)
    arm = lambda f: 'pine_light' if f.normal.z > -0.3 else 'pine'
    def seg_arm(pts, r):
        for a, b in zip(pts, pts[1:]):   # jointed: a short ribbed segment, a pinch, the next
            k.add(lambda bm, a=a, b=b: tube(bm, [a, b], r, r * 0.82, 5), arm)
    tips = []
    for j in range(6):   # primary arms: out, then up, each holding two or three uprights
        a = j * 2 * math.pi / 6 + rnd.uniform(-0.15, 0.15); d = Vector((math.cos(a), math.sin(a), 0))
        elbow = top + d * rnd.uniform(0.6, 0.85) + UP * 0.22
        seg_arm([top, top + d * 0.4 + UP * 0.02, elbow], 0.12)
        for m in range(rnd.choice((2, 3))):
            side = Vector((-d.y, d.x, 0)) * (m - 0.8) * 0.36
            base = elbow + side
            h = rnd.uniform(0.8, 1.5)
            pts = [elbow, base + d * 0.12 + UP * 0.22, base + d * 0.2 + UP * (0.22 + h * 0.5), base + d * 0.24 + UP * (0.22 + h)]
            seg_arm(pts, 0.095)
            tips.append(pts[-1])
    for j, p3 in enumerate(tips):
        k.add(lambda bm, p3=p3: sphere(bm, 0.085, (p3.x, p3.y, p3.z), (1, 1, 0.8), 1), 'pine_light')
        if j % 3 == 0:   # small red-yellow flowers on some tips
            k.add(lambda bm, p3=p3: sphere(bm, 0.045, (p3.x + 0.05, p3.y, p3.z + 0.07), sub=1), 'red')
            k.add(lambda bm, p3=p3: sphere(bm, 0.04, (p3.x - 0.04, p3.y + 0.04, p3.z + 0.08), sub=1), 'yellow')
    tuft(k, 0.4, -0.3, 0.0, 0.8, rnd, 5)
    skirt(k, 0.55, 0.55, seed)
    return k.obj('Euphorbia')

def build_savannagrass():
    """golden elephant grass (the hiding bush): the Brawl leaf clump, narrow blades, the sand ramp"""
    return grass.build_frostgrass(231, 'SavannaGrass', 'sand_grad', 0.55)

# ------------------------------------------------------------------ decor
def build_strawtuft():
    """a small dry tuft on the open ground (the grass clump; placed small; no 'grass' in the name: not a hiding bush)"""
    return grass.build_frostgrass(241, 'StrawTuft', 'sand_grad', 0.7)

def aloe(k, x, y, z, s, rnd, flower=True):
    """an aloe: a rosette of thick pointed blue-green leaves, an orange-red flower spike"""
    for j in range(10):
        a = j * 2 * math.pi / 10 + rnd.uniform(-0.15, 0.15); d = Vector((math.cos(a), math.sin(a), 0))
        up = 0.7 if j % 2 else 0.4
        p0 = Vector((x, y, z + 0.03 * s)); p1 = p0 + d * 0.15 * s + UP * up * 0.25 * s; p2 = p0 + d * 0.32 * s + UP * up * 0.42 * s
        k.add(lambda bm, p0=p0, p1=p1, p2=p2: tube(bm, [p0, p1, p2], 0.06 * s, 0.006, 3), lambda f: 'pine_light' if f.normal.z > -0.2 else 'pine')
    if flower:
        p = Vector((x + 0.02 * s, y, z)); q = p + UP * 0.5 * s
        k.add(lambda bm: tube(bm, [p, q], 0.018 * s, 0.014 * s, 4), 'olive')
        k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.06 * s, 0.03 * s), (0.05 * s, 0.14 * s), (0.0, 0.2 * s)], 8, None, 0, Matrix.Translation(q)), lambda f: 'red' if f.calc_center_median().z > q.z + 0.08 * s else 'orange')

def build_aloeplant(seed=3331):
    rnd = random.Random(seed)
    k = SKit(['pine_light', 'pine', 'olive', 'red', 'orange', 'sandstone_dark', 'wood'])
    aloe(k, 0.0, 0.0, 0.0, 1.3, rnd)
    aloe(k, 0.32, 0.22, 0.0, 0.7, rnd, flower=False)
    skirt(k, 0.42, 0.38, seed, h=0.05)
    return k.obj('AloePlant')

def build_hornskull(seed=3341):
    """the bleached skull of a big antelope lying in the grass, its long spiral horns"""
    rnd = random.Random(seed)
    k = SKit(['bone', 'bone_dark', 'wood_dark', 'sandstone_dark', 'wood'])
    T = Matrix.Rotation(0.5, 4, 'Z') @ Matrix.Translation((0, 0, 0.1)) @ Matrix.Rotation(-0.25, 4, 'X')
    k.add(lambda bm: (sphere(bm, 0.14, (0, 0, 0), (0.8, 1.8, 0.7), 2), bmesh.ops.transform(bm, matrix=T, verts=[v for v in bm.verts if v[k.seen] == 0])), 'bone')
    for s in (-1, 1):
        k.add(lambda bm, s=s: disc_facing(bm, T @ Vector((s * 0.1, -0.08, 0.04)), T.to_3x3() @ Vector((s, -0.3, 0.3)), 0.035, 6), 'wood_dark')
        pts = []
        for i in range(9):
            t = i / 8; a = t * 2.4 * math.pi
            pts.append(T @ Vector((s * (0.08 + 0.1 * t + 0.07 * math.cos(a) * t), 0.12 + 0.55 * t, 0.06 + 0.07 * math.sin(a) * t + 0.12 * t)))
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.04, 0.008, 6), 'bone_dark')
    return k.obj('HornSkull')

def build_savannastones(seed=3351):
    rnd = random.Random(seed)
    k = SKit(['bone_dark', 'ash', 'khaki', 'sandstone_dark', 'orange', 'wood'])
    for j, (x, y, r) in enumerate([(-0.25, 0.05, 0.16), (0.12, 0.18, 0.11), (0.28, -0.15, 0.13), (-0.02, -0.25, 0.08)]):
        boulder(k, x, y, r * 0.5, r, r * 0.9, r * 0.7, seed + j, floor=True)
    return k.obj('SavannaStones')

def build_pondreeds():
    """reeds and sedge by a water hole (the papyrus ramp, narrow blades; not a hiding bush)"""
    return grass.build_frostgrass(251, 'PondReeds', 'papyrus_grad', 0.4)

# ------------------------------------------------------------------ border, gate, backdrop
def lookout(k, cx, cy, cz, rnd):
    """a game ranger's lookout: four poles, a plank platform with a rail, a thatched cone roof, a ladder"""
    for sx in (-1, 1):
        for sy in (-1, 1):
            k.add(lambda bm, sx=sx, sy=sy: tube(bm, [Vector((cx + sx * 0.8, cy + sy * 0.8, cz)), Vector((cx + sx * 0.7, cy + sy * 0.7, cz + 4.2))], 0.09, 0.08, 6), 'wood_dark')
    k.add(lambda bm: rbox(bm, (1.9, 1.9, 0.14), (cx, cy, cz + 2.4), 0.02, segs=1), 'wood')
    for (a, b) in [((-0.9, -0.9), (0.9, -0.9)), ((0.9, -0.9), (0.9, 0.9)), ((0.9, 0.9), (-0.9, 0.9)), ((-0.9, 0.9), (-0.9, -0.9))]:
        k.add(lambda bm, a=a, b=b: tube(bm, [Vector((cx + a[0], cy + a[1], cz + 3.15)), Vector((cx + b[0], cy + b[1], cz + 3.15))], 0.04, 0.04, 4), 'wood_dark')
    k.grad(lambda bm: lathe(bm, [(1.55, 0.0), (1.4, 0.08), (0.8, 0.6), (0.0, 1.25)], 14, lambda i, kk, rr, zz: (rr * (1 + 0.04 * math.sin(i * 2.4)), zz), 0, Matrix.Translation((cx, cy, cz + 4.1))),
           'sand_grad', lambda v: min(1.0, max(0.0, 0.3 + (v.co.z - cz - 4.1) / 1.6)))
    for s in (-1, 1):
        k.add(lambda bm, s=s: tube(bm, [Vector((cx + s * 0.25, cy + 1.6, cz)), Vector((cx + s * 0.25, cy + 0.95, cz + 2.45))], 0.035, 0.035, 4), 'wood')
    for j in range(7):   # the ladder at the back (on the flat top of the bank)
        t = (j + 0.5) / 7
        k.add(lambda bm, t=t: tube(bm, [Vector((cx - 0.25, cy + 1.6 - 0.65 * t, cz + 2.45 * t)), Vector((cx + 0.25, cy + 1.6 - 0.65 * t, cz + 2.45 * t))], 0.022, 0.022, 4), 'wood')

def build_savannaridge(kind, seed, name):
    """a border piece in place of the old border cliffs (7.6 m long, 5 m deep, ~3.5 m tall: low): a red-earth bank
    crowned with granite boulders, dry grass and aloes. Front = -Y (+Z in Unity). 'A' plain, 'B' with a baobab on it,
    'C' with an acacia and a ranger's lookout."""
    rnd = random.Random(seed)
    k = SKit(['terracotta', 'sandstone_dark', 'wood', 'bone_dark', 'ash', 'khaki', 'orange', 'yellow', 'pine_light', 'pine', 'red', 'olive',
              'palm', 'palm_dark', 'basalt', 'wood_dark'])
    L, D = 7.6, 5.0
    k.add(lambda bm: rbox(bm, (L, 3.6, 1.9), (0, 0.6, 0), 0.45, segs=2), soil)
    k.add(lambda bm: blob(bm, 1.0, (0, -1.45, 0.0), (L * 0.5, 0.9, 1.3), 2, 0.12, seed), soil)
    for t in range(2):
        n = 6 - t * 2
        for j in range(n):
            x = -L / 2 + 0.7 + j * (L - 1.4) / max(1, n - 1) + rnd.uniform(-0.3, 0.3)
            if kind == 'B' and -2.8 < x < -0.4: continue
            if kind == 'C' and 0.8 < x < 3.4: continue
            sc = (rnd.uniform(0.7, 1.0), rnd.uniform(0.6, 0.8), rnd.uniform(0.55, 0.75))
            c = (x, -1.2 + t * 1.0, 1.55 + t * 0.55)
            boulder(k, c[0], c[1], c[2], sc[0], sc[1], sc[2], seed + j * 3 + t)
            lichen(k, c, sc, rnd, 2)
    for j in range(16):   # dry grass along the top and the foot
        x = -L / 2 + 0.3 + j * (L - 0.6) / 15 + rnd.uniform(-0.15, 0.15)
        tuft(k, x, rnd.uniform(-1.0, 0.3), 1.85, 1.4, rnd, 6)
        if j % 2: tuft(k, x, rnd.uniform(-2.45, -2.3), 0.0, 1.1, rnd, 5)
    for j in range(3):
        aloe(k, rnd.uniform(-L / 2 + 0.6, L / 2 - 0.6), rnd.uniform(-0.6, 0.4), 1.85, 1.0, rnd)
    if kind == 'B':
        baobab(k, -1.6, -0.6, 1.6, 7.8, rnd, seed + 50, nb=8)
    if kind == 'C':
        acacia(k, -2.2, -0.2, 1.7, 5.0, rnd, seed + 60, R=1.9)
        lookout(k, 2.1, -0.7, 1.75, rnd)
    k.add(lambda bm: blob(bm, 1.0, (0, -D / 2 + 0.45, 0), (L * 0.49, 0.65, 0.07), 3, 0.02, seed), soil)
    ob = k.obj(name)
    if kind == 'B': ob['bark_preview'] = 1
    return ob

def stone_pillar(k, x, y, w, h, rnd, seed):
    """a gate pillar of rough granite blocks set in mortar, a little thatched hat"""
    k.add(lambda bm: rbox(bm, (w, w, h), (x, y, 0), 0.06, segs=1), lambda f: 'khaki' if f.normal.z > 0.6 else 'ash')
    rows = int(h / 0.45)
    for r in range(rows):
        z = 0.08 + r * 0.45
        for side in range(4):
            a = side * math.pi / 2; d = Vector((math.cos(a), math.sin(a), 0)); t = Vector((-d.y, d.x, 0))
            off = (r % 2) * 0.35
            for m in range(2):
                u = -w / 2 + 0.2 + off + m * 0.7
                if u > w / 2 - 0.15: continue
                p = Vector((x, y, z)) + d * (w / 2 - 0.02) + t * u
                k.add(lambda bm, p=p, r=r, side=side, m=m: blob(bm, 1.0, (p.x, p.y, p.z + 0.2), (0.3 if abs(d.x) < 0.5 else 0.08, 0.08 if abs(d.x) < 0.5 else 0.3, 0.18), 1, 0.03, seed + r * 9 + side * 3 + m, floor=False), granite)
    k.grad(lambda bm: lathe(bm, [(w * 0.78, 0.0), (w * 0.72, 0.08), (w * 0.35, 0.5), (0.0, 0.8)], 12, lambda i, kk, rr, zz: (rr * (1 + 0.05 * math.sin(i * 2.7)), zz), 0, Matrix.Translation((x, y, h))),
           'sand_grad', lambda v: min(1.0, max(0.0, 0.3 + (v.co.z - h) / 1.0)))

def build_safarigate(seed=3361):
    """the gateway behind each spawn (in place of the base scene's gate arch, ~10.8 m wide): a 1960s game reserve gate,
    two rough granite pillars with thatched hats, a heavy log lintel, a carved board with an elephant, hurricane
    lanterns, the log-rail gates swung open"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'khaki', 'bone_dark', 'sandstone_dark', 'wood_dark', 'wood', 'wood_light', 'bone', 'red', 'metal', 'lamp_warm', 'orange', 'yellow'])
    X = 4.4
    for s in (-1, 1):
        stone_pillar(k, s * X, 0.0, 1.4, 4.6, rnd, seed + (s + 1) * 50)
    lint = [Vector((-X - 0.9, 0.0, 4.25)), Vector((0.0, 0.0, 4.2)), Vector((X + 0.9, 0.0, 4.25))]
    k.add(lambda bm: tube(bm, lint, 0.33, 0.3, 10), lambda f: 'wood_light' if abs(f.normal.x) > 0.85 else ('wood' if f.normal.z > 0.2 else 'wood_dark'))
    for s in (-1, 1):   # ropes / chains holding the board
        k.add(lambda bm, s=s: tube(bm, [Vector((s * 1.6, -0.15, 4.0)), Vector((s * 1.6, -0.15, 3.55))], 0.025, 0.025, 4), 'metal')
    k.add(lambda bm: rbox(bm, (4.0, 0.14, 1.0), (0, -0.15, 2.6), 0.05, segs=1), 'wood_light')
    k.add(lambda bm: rbox(bm, (4.12, 0.12, 1.12), (0, -0.12, 2.54), 0.04, segs=1), 'wood_dark')   # frame behind
    # the elephant, carved and painted dark on the board (front face at y = -0.22)
    F = -0.25; ex, ez = -0.2, 3.2
    k.add(lambda bm: sphere(bm, 1.0, (ex, F, ez), (0.55, 0.04, 0.3), 2), 'wood_dark')                         # body
    k.add(lambda bm: sphere(bm, 1.0, (ex + 0.58, F - 0.01, ez + 0.12), (0.24, 0.04, 0.24), 2), 'wood_dark')   # head
    k.add(lambda bm: sphere(bm, 1.0, (ex + 0.48, F - 0.03, ez + 0.08), (0.17, 0.03, 0.22), 1), 'wood')       # ear
    k.add(lambda bm: tube(bm, [Vector((ex + 0.78, F - 0.01, ez + 0.08)), Vector((ex + 0.92, F - 0.01, ez - 0.15)), Vector((ex + 0.88, F - 0.01, ez - 0.38)), Vector((ex + 0.98, F - 0.01, ez - 0.42))], 0.07, 0.035, 6), 'wood_dark')
    k.add(lambda bm: tube(bm, [Vector((ex + 0.72, F - 0.04, ez - 0.05)), Vector((ex + 0.88, F - 0.04, ez - 0.18))], 0.03, 0.015, 5), 'bone')   # tusk
    for lx in (-0.38, -0.12, 0.18, 0.4):
        k.add(lambda bm, lx=lx: rbox(bm, (0.14, 0.06, 0.32), (ex + lx, F, ez - 0.42), 0.02, segs=1), 'wood_dark')
    k.add(lambda bm: tube(bm, [Vector((ex - 0.54, F, ez + 0.05)), Vector((ex - 0.66, F, ez - 0.2))], 0.02, 0.015, 4), 'wood_dark')     # tail
    for j in range(9):   # a band of painted dots under it
        k.add(lambda bm, j=j: sphere(bm, 0.04, (-1.6 + j * 0.4, F, 2.69), (1, 0.4, 1), 1), 'orange' if j % 2 else 'yellow')
    for s in (-1, 1):
        hurricane_lantern(k, s * (X - 1.1), -0.2, 3.35, 1.5)
        k.add(lambda bm, s=s: tube(bm, [Vector((s * (X - 1.1), -0.2, 3.9)), Vector((s * (X - 1.1), -0.1, 3.95))], 0.015, 0.015, 3), 'metal')
        # the gate swung open against the pillar (a log-rail frame)
        gx = s * (X + 0.75); gy = -1.6
        for z in (0.35, 1.0, 1.6):
            k.add(lambda bm, z=z, gx=gx: tube(bm, [Vector((gx, -0.75, z)), Vector((gx + s * 0.1, gy - 1.2, z))], 0.06, 0.055, 6), 'wood')
        k.add(lambda bm, gx=gx: tube(bm, [Vector((gx, -0.75, 0.2)), Vector((gx + s * 0.1, gy - 1.2, 1.7))], 0.05, 0.05, 6), 'wood')
        k.add(lambda bm, gx=gx: tube(bm, [Vector((gx + s * 0.1, gy - 1.2, 0.0)), Vector((gx + s * 0.1, gy - 1.2, 1.85))], 0.07, 0.07, 6), 'wood_dark')
        tuft(k, s * (X + 0.2), -1.0, 0.0, 1.3, rnd, 7)
    k.add(lambda bm: blob(bm, 1.0, (0, -1.0, 0), (5.8, 1.3, 0.06), 3, 0.02, seed), soil)
    return k.obj('SafariGate')

def coat(f):
    """a giraffe's coat: big chestnut patches parted by pale lines (Voronoi cells), pale legs and belly"""
    c = f.calc_center_median()
    if c.z < 1.3 or (f.normal.z < -0.6 and c.z < 2.6): return 'cream'
    try:
        d, _ = noise.voronoi(c * 4.2, distance_metric='DISTANCE')
        return 'cream' if d[1] - d[0] < 0.12 else 'terracotta'
    except Exception:
        g = c * 4.2; v = (math.sin(g.x * 1.7) + math.sin(g.y * 2.3 + g.z) + math.sin(g.z * 1.9 + g.x)) / 3
        return 'cream' if abs(v) < 0.12 else 'terracotta'

def build_giraffe(seed=3371):
    """a giraffe standing on the plain (backdrop), browsing: long legs, the sloping back, the long neck, ossicones"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'terracotta', 'wood', 'wood_dark', 'bone_dark', 'black'])
    k.add(lambda bm: (sphere(bm, 1.0, (0, 0, 0), (1.05, 0.45, 0.55), 2),
                      bmesh.ops.transform(bm, matrix=Matrix.Translation((0, 0, 2.55)) @ Matrix.Rotation(-0.18, 4, 'Y'), verts=[v for v in bm.verts if v[k.seen] == 0])), coat)
    neck = [Vector((0.75, 0, 2.75)), Vector((1.05, 0, 3.5)), Vector((1.35, 0, 4.3)), Vector((1.5, 0, 4.75))]
    k.add(lambda bm: tube(bm, neck, 0.3, 0.14, 10), coat)
    k.add(lambda bm: tube(bm, [Vector((0.62, 0, 3.05)), Vector((0.95, 0, 3.75)), Vector((1.28, 0, 4.55)), Vector((1.4, 0, 4.85))], 0.05, 0.035, 4), 'wood')   # mane
    k.add(lambda bm: (sphere(bm, 1.0, (0, 0, 0), (0.34, 0.15, 0.17), 2),
                      bmesh.ops.transform(bm, matrix=Matrix.Translation((1.72, 0, 4.82)) @ Matrix.Rotation(0.45, 4, 'Y'), verts=[v for v in bm.verts if v[k.seen] == 0])), 'terracotta')
    k.add(lambda bm: sphere(bm, 0.1, (1.95, 0, 4.66), (1.0, 1.1, 0.9), 1), 'wood')   # muzzle
    for s in (-1, 1):
        k.add(lambda bm, s=s: tube(bm, [Vector((1.6, s * 0.06, 4.95)), Vector((1.55, s * 0.08, 5.18))], 0.03, 0.025, 5), 'terracotta')   # ossicones
        k.add(lambda bm, s=s: sphere(bm, 0.045, (1.55, s * 0.08, 5.2), sub=1), 'wood_dark')
        k.add(lambda bm, s=s: lathe(bm, [(0.05, 0.0), (0.0, 0.16)], 5, None, 0, Matrix.Translation((1.56, s * 0.12, 4.92)) @ Matrix.Rotation(s * -1.3, 4, 'X')), 'terracotta')   # ears
        k.add(lambda bm, s=s: sphere(bm, 0.03, (1.82, s * 0.12, 4.86), sub=1), 'black')   # eyes
    for (lx, ly, kx) in [(0.62, 0.22, 0.05), (0.62, -0.22, -0.04), (-0.68, 0.2, -0.06), (-0.68, -0.2, 0.05)]:
        k.add(lambda bm, lx=lx, ly=ly, kx=kx: tube(bm, [Vector((lx, ly, 2.25)), Vector((lx + kx, ly, 1.15)), Vector((lx, ly, 0.12))], 0.11, 0.06, 6), coat)
        k.add(lambda bm, lx=lx, ly=ly: lathe(bm, [(0.08, 0.0), (0.065, 0.13), (0.0, 0.13)], 6, None, 0, Matrix.Translation((lx, ly, 0.0))), 'wood_dark')
    k.add(lambda bm: tube(bm, [Vector((-1.0, 0, 2.6)), Vector((-1.12, 0, 2.0)), Vector((-1.1, 0, 1.55))], 0.035, 0.02, 4), 'terracotta')
    k.add(lambda bm: sphere(bm, 0.07, (-1.1, 0, 1.45), (1, 1, 1.8), 1), 'wood_dark')
    return k.obj('Giraffe')

def build_elephant(seed=3381):
    """an elephant on the plain (backdrop): big ears, the trunk hanging and curling, tusks, column legs"""
    rnd = random.Random(seed)
    k = SKit(['ash', 'stone_dark', 'basalt', 'bone', 'bone_dark', 'black', 'khaki'])
    hide = lambda f: 'ash' if f.normal.z > -0.45 else 'basalt'
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 2.05), (1.55, 0.95, 1.05), 2, 0.04, seed, floor=False), hide)
    k.add(lambda bm: blob(bm, 1.0, (1.45, 0, 2.55), (0.72, 0.66, 0.74), 2, 0.03, seed + 1, floor=False), hide)
    for s in (-1, 1):   # ears: big flat flaps, a little away from the head
        M = Matrix.Translation((1.2, s * 0.72, 2.45)) @ Matrix.Rotation(s * 1.35, 4, 'X') @ Matrix.Rotation(0.15, 4, 'Y')
        k.add(lambda bm, M=M: lathe(bm, [(0.0, -0.05), (0.75, -0.03), (0.78, 0.02), (0.0, 0.05)], 14, lambda i, kk, rr, zz: (rr * (1 + 0.12 * math.sin(i * 0.45 + 0.8)), zz), 0, M @ Matrix.Diagonal((0.85, 1.0, 1.0, 1))), hide)
        k.add(lambda bm, s=s: sphere(bm, 0.05, (1.95, s * 0.42, 2.75), sub=1), 'black')
        k.add(lambda bm, s=s: tube(bm, [Vector((1.95, s * 0.3, 1.95)), Vector((2.25, s * 0.38, 1.5)), Vector((2.55, s * 0.4, 1.4)), Vector((2.75, s * 0.38, 1.55))], 0.1, 0.04, 8), 'bone')   # tusks
    trunk = [Vector((2.05, 0, 2.3)), Vector((2.3, 0, 1.6)), Vector((2.35, 0, 0.9)), Vector((2.3, 0, 0.45)), Vector((2.5, 0, 0.3))]
    k.add(lambda bm: tube(bm, trunk, 0.27, 0.09, 10), hide)
    for (lx, ly) in [(0.85, 0.48), (0.85, -0.48), (-0.85, 0.5), (-0.85, -0.5)]:
        k.add(lambda bm, lx=lx, ly=ly: tube(bm, [Vector((lx, ly, 1.7)), Vector((lx, ly, 0.8)), Vector((lx, ly, 0.0))], 0.36, 0.32, 10), hide)
        for j in range(3):
            a = -0.5 + j * 0.5 + (0 if lx > 0 else math.pi)
            k.add(lambda bm, lx=lx, ly=ly, a=a: sphere(bm, 0.07, (lx + math.cos(a) * 0.3, ly + math.sin(a) * 0.3, 0.06), (1, 1, 0.8), 1), 'bone_dark')
    k.add(lambda bm: tube(bm, [Vector((-1.5, 0, 2.3)), Vector((-1.62, 0, 1.6)), Vector((-1.6, 0, 1.3))], 0.05, 0.03, 5), hide)
    k.add(lambda bm: sphere(bm, 0.06, (-1.6, 0, 1.22), (1, 1, 1.6), 1), 'black')
    return k.obj('Elephant')

def ridge_tent(k, x, y, yaw, rnd):
    """a 1960s canvas ridge tent: walls, a ridged roof with a fly sheet, the door flaps tied back, poles, guy ropes"""
    M = Matrix.Translation((x, y, 0)) @ Matrix.Rotation(yaw, 4, 'Z')
    L, W, Hw, Hr = 3.0, 2.4, 1.0, 2.2
    def tent(bm):
        v = lambda p: bm.verts.new(M @ Vector(p))
        a = [v((-L / 2, -W / 2, 0)), v((L / 2, -W / 2, 0)), v((L / 2, W / 2, 0)), v((-L / 2, W / 2, 0))]
        b = [v((-L / 2, -W / 2, Hw)), v((L / 2, -W / 2, Hw)), v((L / 2, W / 2, Hw)), v((-L / 2, W / 2, Hw))]
        r = [v((-L / 2, 0, Hr)), v((L / 2, 0, Hr))]
        bm.faces.new((a[0], a[1], b[1], b[0])); bm.faces.new((a[2], a[3], b[3], b[2]))
        bm.faces.new((b[0], b[1], r[1], r[0])); bm.faces.new((b[2], b[3], r[0], r[1]))
        bm.faces.new((a[1], a[2], b[2], r[1], b[1])); bm.faces.new((a[3], a[0], b[0], r[0], b[3]))
    k.add(tent, canvas)
    k.add(lambda bm: bmesh.ops.create_cube(bm, size=1.0, matrix=M @ Matrix.Translation((-L / 2 - 0.005, 0, 0.75)) @ Matrix.Diagonal((0.02, 0.9, 1.5, 1))), 'wood_dark')   # open door
    for s in (-1, 1):
        k.add(lambda bm, s=s: bmesh.ops.create_cube(bm, size=1.0, matrix=M @ Matrix.Translation((-L / 2 - 0.15, s * 0.6, 0.8)) @ Matrix.Rotation(s * 0.5, 4, 'Z') @ Matrix.Diagonal((0.05, 0.4, 1.4, 1))), 'khaki')   # flaps
        k.add(lambda bm, s=s: tube(bm, [M @ Vector((s * L / 2, 0, 0)), M @ Vector((s * L / 2, 0, Hr + 0.25))], 0.04, 0.04, 5), 'wood_dark')
        for e in (-1, 1):
            k.add(lambda bm, s=s, e=e: tube(bm, [M @ Vector((s * L * 0.3, e * W / 2, Hw)), M @ Vector((s * L * 0.4, e * (W / 2 + 1.0), 0.0))], 0.012, 0.012, 3), 'bone_dark')

def build_safaricamp(seed=3391):
    """an expedition camp out on the plain (backdrop): two canvas ridge tents, a camp table with a lantern, a ring of
    stones with crossed logs, and a boxy 1960s expedition truck (an original design) with a canvas tilt"""
    rnd = random.Random(seed)
    k = SKit(['khaki', 'olive', 'wood_dark', 'wood', 'wood_light', 'bone_dark', 'black', 'metal', 'ice_deep', 'cream',
              'red', 'lamp_warm', 'ash', 'sandstone_dark', 'enamel', 'bone'])
    ridge_tent(k, -4.0, 1.5, 0.15, rnd)
    ridge_tent(k, -1.0, 3.2, -0.25, rnd)
    # table and stools
    k.add(lambda bm: rbox(bm, (1.2, 0.7, 0.05), (-1.6, -0.6, 0.72), 0.01, segs=1), 'wood_light')
    for sx in (-1, 1):
        for sy in (-1, 1):
            k.add(lambda bm, sx=sx, sy=sy: tube(bm, [Vector((-1.6 + sx * 0.5, -0.6 + sy * 0.28, 0.0)), Vector((-1.6 + sx * 0.5, -0.6 + sy * 0.28, 0.72))], 0.025, 0.025, 4), 'wood')
    hurricane_lantern(k, -1.3, -0.5, 0.77, 1.0)
    for (x, y) in [(-2.5, -0.7), (-0.8, -1.1)]:
        k.add(lambda bm, x=x, y=y: rbox(bm, (0.42, 0.42, 0.06), (x, y, 0.42), 0.01, segs=1), 'olive')
        k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x - 0.18, y - 0.18, 0)), Vector((x + 0.18, y + 0.18, 0.42))], 0.02, 0.02, 4), 'wood')
        k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x + 0.18, y - 0.18, 0)), Vector((x - 0.18, y + 0.18, 0.42))], 0.02, 0.02, 4), 'wood')
    # fire ring with crossed logs (unlit: no smoke)
    for j in range(9):
        a = j * 2 * math.pi / 9
        k.add(lambda bm, a=a, j=j: blob(bm, 1.0, (-2.6 + math.cos(a) * 0.55, -2.2 + math.sin(a) * 0.55, 0.0), (0.16, 0.14, 0.12), 1, 0.03, seed + j), 'ash')
    for a in (0.3, 1.4, 2.5):
        d = Vector((math.cos(a), math.sin(a), 0)) * 0.45
        k.add(lambda bm, d=d: tube(bm, [Vector((-2.6, -2.2, 0.08)) - d, Vector((-2.6, -2.2, 0.08)) + d], 0.07, 0.06, 6), 'wood_dark')
    # the expedition truck
    T = Matrix.Translation((3.2, 0.2, 0.0)) @ Matrix.Rotation(-0.5, 4, 'Z')
    body = lambda f: 'khaki' if f.normal.z > -0.3 else 'olive'
    def box(sx, sy, sz, cx, cy, cz, col, r=0.06):
        k.add(lambda bm: (rbox(bm, (sx, sy, sz), (0, 0, 0), r, segs=1), bmesh.ops.transform(bm, matrix=T @ Matrix.Translation((cx, cy, cz)), verts=[v for v in bm.verts if v[k.seen] == 0])), col)
    box(4.1, 1.7, 0.25, 0.0, 0.0, 0.55, 'black', 0.03)                 # chassis
    box(1.3, 1.5, 0.75, 1.35, 0.0, 0.8, body)                          # bonnet
    box(0.9, 1.7, 1.2, 0.35, 0.0, 0.8, body)                           # cab
    box(0.06, 1.5, 0.55, 0.78, 0.0, 1.32, 'ice_deep', 0.01)            # windscreen
    box(2.0, 1.7, 0.6, -1.15, 0.0, 0.8, body)                          # rear tub
    box(1.95, 1.62, 0.85, -1.15, 0.0, 1.38, 'olive', 0.28)            # canvas tilt over the tub
    for (wx, wy) in [(1.35, 0.85), (1.35, -0.85), (-1.25, 0.85), (-1.25, -0.85)]:
        k.add(lambda bm, wx=wx, wy=wy: lathe(bm, [(0.0, -0.14), (0.42, -0.14), (0.45, 0.0), (0.42, 0.14), (0.0, 0.14)], 12, None, 0, T @ Matrix.Translation((wx, wy, 0.42)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), lambda f: 'metal' if abs((T.inverted().to_3x3() @ f.normal).y) > 0.9 else 'black')
    k.add(lambda bm: lathe(bm, [(0.0, -0.12), (0.38, -0.12), (0.4, 0.0), (0.38, 0.12), (0.0, 0.12)], 12, None, 0, T @ Matrix.Translation((1.4, 0.0, 1.67))), 'black')   # spare wheel on the bonnet
    for s in (-1, 1):
        k.add(lambda bm, s=s: lathe(bm, [(0.0, 0.0), (0.12, 0.0), (0.12, 0.06), (0.0, 0.06)], 10, None, 0, T @ Matrix.Translation((2.02, s * 0.5, 1.0)) @ Matrix.Rotation(math.pi / 2, 4, 'Y')), 'cream')   # headlamps
    for j in range(3):   # jerry cans on the tub side
        jx = -1.7 + j * 0.45
        k.add(lambda bm, jx=jx: (rbox(bm, (0.34, 0.16, 0.46), (0, 0, 0), 0.03, segs=1), bmesh.ops.transform(bm, matrix=T @ Matrix.Translation((jx, -0.95, 0.75)), verts=[v for v in bm.verts if v[k.seen] == 0])), 'enamel' if j == 1 else 'olive')
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (6.5, 4.8, 0.06), 3, 0.02, seed), soil)
    return k.obj('SafariCamp')
