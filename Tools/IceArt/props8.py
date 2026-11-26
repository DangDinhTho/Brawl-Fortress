"""Xmas theme props (1960s-70s Christmas: big C9 incandescent bulbs, wrapped presents, painted iron, wood, deep snow),
in the soft chunky style of the v3 ice pieces. Faces flagged 'glow' (bulbs, lamp glass, the star) go to a second
material slot that IceThemeSetup makes unlit, so they read as lit.

XmasTree, GiftStackA, GiftStackB, GiftBox, LightFence, XmasLamp, CandyCane, Snowman2, Sleigh, BigBauble
(PineGrass, the snowy pine bush, is grass.build_frostgrass with the pine gradient)."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *
from props3 import blob
from props7 import tube
import grass

BULBS = ['bulb_red', 'bulb_green', 'bulb_blue', 'bulb_yellow', 'bulb_orange']

class Kit:
    """one bmesh with a colour (palette index) and a glow flag per face"""
    def __init__(self, colours):
        self.bm = bmesh.new(); self.C = list(colours)
        self.gl = self.bm.faces.layers.int.new('glow')
        self.seen = self.bm.verts.layers.int.new('seen')
    def ci(self, name):
        if name not in self.C: self.C.append(name)
        return self.C.index(name)
    def mark(self):
        for v in self.bm.verts: v[self.seen] = 1
    def fresh_faces(self):
        vs = [v for v in self.bm.verts if v[self.seen] == 0]
        return list({f for v in vs for f in v.link_faces})
    def paint(self, faces, colour, glow=False):
        """colour: palette name, or a function face -> palette name"""
        self.bm.normal_update()
        for f in faces:
            f.material_index = self.ci(colour(f) if callable(colour) else colour)
            f[self.gl] = 1 if glow else 0
    def add(self, build, colour, glow=False):
        """build() adds geometry with bmesh ops; every new face gets the colour (python refs are not kept across ops)"""
        self.mark(); build(self.bm); self.paint(self.fresh_faces(), colour, glow)
    def obj(self, name):
        for v in self.bm.verts: v[self.seen] = 0
        bm = self.bm
        # glow faces last: Unity numbers the submeshes in the order their materials first appear in the face list, so
        # a mesh whose first face glowed got its two materials swapped (Volcano boulders, the candy cake banks)
        bm.faces.sort(key=lambda f: f[self.gl]); bm.faces.index_update()
        bm.verts.layers.int.remove(self.seen)
        ob = bm_to_obj(bm, name, self.C)
        return apply_glow(ob)

def rbox(bm, size, loc, radius=0.06, yaw=0.0, segs=2):
    """rounded box standing on loc (its bottom at loc.z)"""
    sx, sy, sz = size
    M = Matrix.Translation((loc[0], loc[1], loc[2] + sz / 2)) @ Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Diagonal((sx, sy, sz, 1))
    vs = bmesh.ops.create_cube(bm, size=1.0, matrix=M)['verts']
    edges = list({e for v in vs for e in v.link_edges})
    bmesh.ops.bevel(bm, geom=edges, offset=radius, offset_type='OFFSET', segments=segs, profile=0.5, affect='EDGES', clamp_overlap=True)

def sphere(bm, r, loc, scale=(1, 1, 1), sub=2):
    bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1.0, matrix=Matrix.Translation(loc) @ Matrix.Diagonal((r * scale[0], r * scale[1], r * scale[2], 1)))

def c9(bm, pos, down=Vector((0, 0, -1)), s=1.0):
    """a C9 bulb hanging from pos along 'down': a teardrop of glass (returns nothing; colour it with Kit.add)"""
    d = down.normalized()
    rot = Vector((0, 0, -1)).rotation_difference(d).to_matrix().to_4x4()
    M = Matrix.Translation(pos) @ rot @ Matrix.Diagonal((s, s, s, 1))
    fs = lathe(bm, [(0.0, 0.0), (0.045, -0.04), (0.055, -0.09), (0.0, -0.16)], 6)
    bmesh.ops.transform(bm, matrix=M, verts=list({v for f in fs for v in f.verts}))

def socket(bm, pos, down=Vector((0, 0, -1)), s=1.0):
    d = down.normalized()
    rot = Vector((0, 0, -1)).rotation_difference(d).to_matrix().to_4x4()
    fs = lathe(bm, [(0.028, 0.03), (0.03, 0.0), (0.0, 0.0)], 4)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(pos) @ rot @ Matrix.Diagonal((s, s, s, 1)), verts=list({v for f in fs for v in f.verts}))

def string_lights(k, pts, every=0.2, s=1.0, start=0):
    """a dark green wire through pts with C9 bulbs hanging from it every 'every' metres, colours cycling"""
    k.add(lambda bm: tube(bm, pts, 0.012, 0.012, 5), 'xmas_green_dark')
    # walk the polyline
    seg = [(pts[i], pts[i + 1]) for i in range(len(pts) - 1)]
    total = sum((b - a).length for a, b in seg)
    n = max(1, int(total / every)); i = start
    for j in range(n):
        t = (j + 0.5) / n * total
        for a, b in seg:
            L = (b - a).length
            if t <= L:
                p = a.lerp(b, t / L); break
            t -= L
        k.add(lambda bm, p=p: socket(bm, p, s=s), 'xmas_green_dark')
        k.add(lambda bm, p=p: c9(bm, p + Vector((0, 0, -0.03 * s)), s=s), BULBS[i % len(BULBS)], glow=True)
        i += 1

def sag(a, b, depth, n=8):
    a, b = Vector(a), Vector(b)
    return [a.lerp(b, t) + Vector((0, 0, -depth * 4 * t * (1 - t))) for t in [i / n for i in range(n + 1)]]

def snow_on(bm, sx, sy, z, loc=(0, 0), th=0.12, yaw=0.0, seed=0):
    """a soft snow pillow lying on a flat top"""
    vs = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((loc[0], loc[1], z)) @ Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Diagonal((sx / 2, sy / 2, th, 1)))['verts']
    for v in vs:
        if v.co.z < z - 0.02: v.co.z = z - 0.02

def snowc(f): return 'snow' if f.normal.z > -0.3 else 'snow_shade'

# ------------------------------------------------------------------ presents
def present(k, size, loc, paper, ribbon, yaw=0.0, bow=True, snow=True, seed=0):
    sx, sy, sz = size
    k.add(lambda bm: rbox(bm, size, loc, 0.05, yaw), paper)
    R = Matrix.Rotation(yaw, 4, 'Z')
    # ribbon bands across the top and down the sides (a hair proud of the paper)
    k.add(lambda bm: rbox(bm, (sx + 0.02, 0.12, sz + 0.02), (loc[0], loc[1], loc[2] - 0.01), 0.03, yaw), ribbon)
    k.add(lambda bm: rbox(bm, (0.12, sy + 0.02, sz + 0.02), (loc[0], loc[1], loc[2] - 0.01), 0.03, yaw), ribbon)
    top = loc[2] + sz
    if bow:
        for s in (-1, 1):   # two loops and a knot
            c = Vector(loc) + R @ Vector((s * 0.11, 0, 0)) + Vector((0, 0, top - loc[2] + 0.08))
            pts = [c + R @ Vector((s * 0.09 * math.sin(a), 0.0, 0.07 * math.cos(a) - 0.0)) for a in [i / 10 * 2 * math.pi for i in range(11)]]
            k.add(lambda bm, pts=pts: tube(bm, pts, 0.03, 0.03, 6), ribbon)
        k.add(lambda bm: sphere(bm, 0.05, (loc[0], loc[1], top + 0.04)), ribbon)
    elif snow:
        k.add(lambda bm: snow_on(bm, sx * 0.85, sy * 0.85, top, (loc[0], loc[1]), 0.07, yaw), snowc)

def build_giftstack(kind, seed, name):
    rnd = random.Random(seed)
    k = Kit(['snow'])
    if kind == 'A':   # three presents stacked, each a little turned
        present(k, (1.30, 1.25, 0.80), (0, 0, 0), 'xmas_red', 'gold', 0.05, bow=False, snow=False)
        present(k, (0.95, 0.90, 0.62), (0.05, -0.04, 0.82), 'xmas_green', 'xmas_red', 0.35, bow=False, snow=False)
        present(k, (0.55, 0.55, 0.42), (-0.02, 0.02, 1.46), 'gold', 'xmas_red', -0.25)
        k.add(lambda bm: snow_on(bm, 1.05, 0.9, 0.80, (-0.15, 0.25), 0.08, seed=seed), snowc)
    else:             # two big ones side by side, a small one on top
        present(k, (0.72, 1.30, 1.05), (-0.36, 0, 0), 'xmas_green', 'gold', 0.0, bow=False, snow=True)
        present(k, (0.70, 1.20, 0.80), (0.37, 0.02, 0), 'cream', 'xmas_red', 0.04, bow=False, snow=False)
        present(k, (0.50, 0.48, 0.40), (0.38, 0.0, 0.81), 'xmas_red', 'gold', 0.4)
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.85, 0.80, 0.10), 3, 0.02, seed), snowc)   # snow drifted at the foot
    return k.obj(name)

def build_giftbox(seed=811):
    k = Kit(['snow'])
    present(k, (0.85, 0.80, 0.70), (0, 0, 0), 'xmas_red', 'gold', 0.1)
    present(k, (0.42, 0.40, 0.36), (0.45, -0.38, 0), 'xmas_green', 'xmas_red', 0.6)
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.70, 0.65, 0.08), 3, 0.02, seed), snowc)
    return k.obj('GiftBox')

# ------------------------------------------------------------------ tree
def build_xmastree(seed=821):
    """a decorated pine: four wavy tiers with snow on their shoulders, a string of C9 bulbs round each tier, baubles,
    a glowing gold star, on a snow mound"""
    rnd = random.Random(seed)
    k = Kit(['xmas_green', 'xmas_green_dark', 'snow'])
    tiers = [(1.05, 0.55, 1.15), (0.85, 1.15, 0.95), (0.64, 1.70, 0.80), (0.42, 2.20, 0.75)]   # (radius, base z, height)
    k.add(lambda bm: lathe(bm, [(0.14, 0.0), (0.13, 0.6), (0.0, 0.6)], 8), 'wood_dark')
    for i, (R, z0, h) in enumerate(tiers):
        rim = lambda j, kk, r, z, i=i: (r * (1.0 + 0.10 * math.sin(j * 2 * math.pi / 14 * 7 + i)), z - (0.06 * (j % 2) if kk == 0 else 0.0))
        k.add(lambda bm: lathe(bm, [(0.0, z0 - 0.02), (R, z0), (R * 0.82, z0 + h * 0.18), (R * 0.36, z0 + h * 0.72), (0.0, z0 + h)], 14, rim, i * 0.2),
              'xmas_green')   # one colour: the cel shading gives the form (two tones by facing read as blotches)
        # snow on the shoulders, drippy lower edge
        zs = z0 + h * 0.42
        rs = R * 0.82 + (R * 0.36 - R * 0.82) * ((h * 0.42 - h * 0.18) / (h * 0.54)) + 0.03
        srim = lambda j, kk, r, z: (r, z - (0.035 * (0.5 + 0.5 * math.sin(j * 2.3)) if kk == 0 else 0.0))
        k.add(lambda bm: lathe(bm, [(rs, zs), (R * 0.36 + 0.04, z0 + h * 0.72), (0.0, z0 + h + 0.03)], 14, srim, i * 0.2), 'snow')
        # bulbs round the tier's lower rim
        n = 6 + 2 * (3 - i)
        pts = []
        for j in range(n + 1):
            a = 2 * math.pi * j / n + i * 0.7
            pts.append(Vector((math.cos(a) * (R * 0.86 + 0.06), math.sin(a) * (R * 0.86 + 0.06), z0 + h * 0.16 + 0.05 * math.sin(a * 3))))
        string_lights(k, pts, every=0.38, s=1.8, start=i)
        for j in range(3):   # baubles
            a = rnd.uniform(0, 6.28); r = R * 0.62
            c = Vector((math.cos(a) * r, math.sin(a) * r, z0 + h * 0.33))
            k.add(lambda bm, c=c: sphere(bm, 0.08, c), 'xmas_red' if (i + j) % 2 else 'gold')
    # the star: a thick five-pointed star facing -Y
    def star(bm):
        pts = []
        for j in range(10):
            a = math.pi / 2 + j * math.pi / 5; r = 0.28 if j % 2 == 0 else 0.12
            pts.append((math.cos(a) * r, math.sin(a) * r))
        top = 2.95 + 0.26
        f = [bm.verts.new((x, -0.05, top + y)) for x, y in pts]; b = [bm.verts.new((x, 0.05, top + y)) for x, y in pts]
        c0 = bm.verts.new((0, -0.09, top)); c1 = bm.verts.new((0, 0.09, top))
        for j in range(10):
            jn = (j + 1) % 10
            bm.faces.new((c0, f[jn], f[j])); bm.faces.new((c1, b[j], b[jn])); bm.faces.new((f[j], f[jn], b[jn], b[j]))
    k.add(star, 'bulb_yellow', glow=True)
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (1.0, 0.95, 0.28), 3, 0.03, seed), snowc)
    return k.obj('XmasTree')

# ------------------------------------------------------------------ fence with lights
def build_lightfence(seed=831):
    """a 1.5 m run of painted wooden fence (posts, two rails), snow on top, a string of C9 bulbs sagging between posts"""
    k = Kit(['wood', 'wood_dark', 'snow'])
    posts = [-0.70, 0.0, 0.70]
    for x in posts:
        k.add(lambda bm, x=x: rbox(bm, (0.14, 0.14, 1.05), (x, 0, 0), 0.03), lambda f: 'wood' if lit(f.normal) > -0.1 else 'wood_dark')
        k.add(lambda bm, x=x: snow_on(bm, 0.2, 0.2, 1.05, (x, 0), 0.07), snowc)
    for z in (0.35, 0.75):
        k.add(lambda bm, z=z: rbox(bm, (1.55, 0.06, 0.12), (0, 0, z), 0.02), lambda f: 'xmas_red' if lit(f.normal) > -0.1 else 'xmas_red_dark')
    k.add(lambda bm: snow_on(bm, 1.5, 0.12, 0.87, (0, 0), 0.05), snowc)
    pts = sag((-0.70, -0.08, 1.00), (0.0, -0.08, 1.00), 0.16) + sag((0.0, -0.08, 1.00), (0.70, -0.08, 1.00), 0.16)[1:]
    string_lights(k, pts, every=0.24, s=1.5)
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.85, 0.25, 0.10), 3, 0.02, seed), snowc)
    return k.obj('LightFence')

# ------------------------------------------------------------------ street lamp
def build_xmaslamp(seed=841):
    """a period cast-iron street lamp painted bottle green: fluted post, four-paned lantern glowing warm, snow on the
    roof, a wreath with a red bow on the post"""
    k = Kit(['enamel', 'black', 'snow'])
    ironc = lambda f: 'enamel' if lit(f.normal) > -0.15 else 'black'
    k.add(lambda bm: lathe(bm, [(0.26, 0.0), (0.24, 0.12), (0.16, 0.2), (0.14, 0.45), (0.09, 0.55), (0.08, 2.35), (0.12, 2.42), (0.0, 2.42)], 8), ironc)
    k.add(lambda bm: rbox(bm, (0.42, 0.42, 0.06), (0, 0, 2.42), 0.02), ironc)
    k.add(lambda bm: rbox(bm, (0.34, 0.34, 0.50), (0, 0, 2.48), 0.03), 'lamp_warm', glow=True)          # the glass
    for x, y in [(-0.18, -0.18), (0.18, -0.18), (-0.18, 0.18), (0.18, 0.18)]:                             # frame bars
        k.add(lambda bm, x=x, y=y: rbox(bm, (0.05, 0.05, 0.52), (x, y, 2.47), 0.01), ironc)
    k.add(lambda bm: lathe(bm, [(0.32, 2.98), (0.28, 3.04), (0.06, 3.22), (0.0, 3.26)], 8, None, math.pi / 8), ironc)   # roof
    k.add(lambda bm: lathe(bm, [(0.30, 3.02), (0.22, 3.10), (0.0, 3.16)], 8, None, math.pi / 8), 'snow')
    k.add(lambda bm: sphere(bm, 0.05, (0, 0, 3.30)), 'gold')
    # wreath on the post, facing -Y, with a red bow
    def wreath(bm):
        pts = [Vector((math.cos(a) * 0.20, -0.11, 1.55 + math.sin(a) * 0.20)) for a in [i / 16 * 2 * math.pi for i in range(17)]]
        tube(bm, pts, 0.075, 0.075, 8)
    k.add(wreath, 'xmas_green')
    for a in range(6):
        ang = a / 6 * 2 * math.pi + 0.3
        k.add(lambda bm, ang=ang: sphere(bm, 0.035, (math.cos(ang) * 0.2, -0.18, 1.55 + math.sin(ang) * 0.2)), 'xmas_red')
    for s in (-1, 1):
        k.add(lambda bm, s=s: sphere(bm, 0.07, (s * 0.07, -0.17, 1.33), (1.2, 0.5, 0.8)), 'xmas_red')
        k.add(lambda bm, s=s: tube(bm, [Vector((s * 0.03, -0.17, 1.30)), Vector((s * 0.08, -0.17, 1.18)), Vector((s * 0.12, -0.16, 1.08))], 0.03, 0.025, 6), 'xmas_red')
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.45, 0.45, 0.12), 3, 0.02, seed), snowc)
    return k.obj('XmasLamp')

# ------------------------------------------------------------------ candy cane
def build_candycane(seed=851):
    """a giant candy cane stuck in the snow, red and white spiral stripes"""
    k = Kit(['cream', 'xmas_red', 'snow'])
    pts = [Vector((0, 0, z)) for z in (0.0, 0.5, 1.0, 1.5, 1.85)]
    for a in [i / 8 * math.pi for i in range(1, 9)]:
        pts.append(Vector((0.28 - 0.28 * math.cos(a), 0, 1.85 + 0.28 * math.sin(a))))
    def cane(bm):
        segs = 14; r = 0.12
        rings = []
        # resample densely so the stripes are smooth
        dense = []
        for i in range(len(pts) - 1):
            for t in range(8): dense.append(pts[i].lerp(pts[i + 1], t / 8))
        dense.append(pts[-1])
        s_acc = [0.0]
        for i in range(1, len(dense)): s_acc.append(s_acc[-1] + (dense[i] - dense[i - 1]).length)
        for i, p in enumerate(dense):
            d = (dense[min(i + 1, len(dense) - 1)] - dense[max(i - 1, 0)]).normalized()
            a = Vector((0, 1, 0)).cross(d).normalized() if abs(d.y) < 0.99 else Vector((1, 0, 0)); b = d.cross(a).normalized()
            rings.append([bm.verts.new(p + (a * math.cos(2 * math.pi * j / segs) + b * math.sin(2 * math.pi * j / segs)) * r) for j in range(segs)])
        for i in range(len(rings) - 1):
            for j in range(segs):
                f = bm.faces.new((rings[i][j], rings[i][(j + 1) % segs], rings[i + 1][(j + 1) % segs], rings[i + 1][j]))
                stripe = int((s_acc[i] * 3.2 + j / segs) * 2) % 2     # a helix: position along + angle round
                f.material_index = 1 if stripe else 0
        bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    k.mark(); cane(k.bm)
    for f in k.fresh_faces(): f[k.gl] = 0      # material_index already set per stripe (0 cream, 1 red)
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.45, 0.42, 0.14), 3, 0.02, seed), snowc)
    return k.obj('CandyCane')

# ------------------------------------------------------------------ snowman
def build_snowman2(seed=861):
    """a jolly snowman: three snowballs, coal eyes and buttons, a carrot nose, a red knitted scarf, a black top hat
    with a red band and a sprig of holly, twig arms"""
    k = Kit(['snow', 'snow_shade'])
    snowb = 'snow'
    k.add(lambda bm: sphere(bm, 0.55, (0, 0, 0.45), (1, 1, 0.9), 3), snowb)
    k.add(lambda bm: sphere(bm, 0.40, (0, 0, 1.18), sub=3), snowb)
    k.add(lambda bm: sphere(bm, 0.30, (0, 0, 1.75), sub=3), snowb)
    for x in (-0.10, 0.10): k.add(lambda bm, x=x: sphere(bm, 0.04, (x, -0.27, 1.82)), 'black')
    for z in (1.30, 1.15, 0.98): k.add(lambda bm, z=z: sphere(bm, 0.045, (0, -0.38, z)), 'black')
    k.add(lambda bm: lathe(bm, [(0.05, 0.0), (0.0, 0.26)], 6, None, 0, Matrix.Translation((0, -0.27, 1.74)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), 'orange')
    # scarf: a ring round the neck and a hanging end
    pts = [Vector((math.cos(a) * 0.30, math.sin(a) * 0.30, 1.50)) for a in [i / 14 * 2 * math.pi for i in range(15)]]
    k.add(lambda bm: tube(bm, pts, 0.07, 0.07, 8), 'xmas_red')
    k.add(lambda bm: rbox(bm, (0.16, 0.06, 0.42), (0.16, -0.30, 1.08), 0.03, 0.2), 'xmas_red')
    # top hat
    k.add(lambda bm: lathe(bm, [(0.32, 1.98), (0.32, 2.02), (0.20, 2.02), (0.19, 2.38), (0.0, 2.38)], 12), 'black')
    k.add(lambda bm: lathe(bm, [(0.205, 2.04), (0.20, 2.12), (0.0, 2.12)], 12), 'xmas_red')
    for a in (0.0, 0.7): k.add(lambda bm, a=a: sphere(bm, 0.05, (0.15 * math.cos(a - 1.9), 0.15 * math.sin(a - 1.9), 2.13), (1.6, 0.6, 0.5)), 'xmas_green')
    k.add(lambda bm: sphere(bm, 0.03, (-0.05, -0.19, 2.15)), 'xmas_red')
    for s in (-1, 1):   # twig arms
        k.add(lambda bm, s=s: tube(bm, [Vector((s * 0.35, 0, 1.25)), Vector((s * 0.65, 0, 1.45)), Vector((s * 0.85, 0, 1.68))], 0.03, 0.015, 5), 'wood_dark')
        k.add(lambda bm, s=s: tube(bm, [Vector((s * 0.65, 0, 1.45)), Vector((s * 0.8, 0, 1.42))], 0.018, 0.01, 5), 'wood_dark')
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.70, 0.65, 0.10), 3, 0.02, seed), snowc)
    return k.obj('Snowman2')

# ------------------------------------------------------------------ sleigh
def build_sleigh(seed=871):
    """a red wooden sleigh with gold trim and curling gold runners, a sack of presents in the back"""
    k = Kit(['xmas_red', 'xmas_red_dark', 'gold', 'snow'])
    redc = lambda f: 'xmas_red' if lit(f.normal) > -0.15 else 'xmas_red_dark'
    k.add(lambda bm: rbox(bm, (1.20, 0.80, 0.45), (0.05, 0, 0.28), 0.10, segs=3), redc)        # body
    k.add(lambda bm: rbox(bm, (0.30, 0.80, 0.55), (0.52, 0, 0.55), 0.10, segs=3), redc)        # raised back
    k.add(lambda bm: rbox(bm, (0.28, 0.80, 0.30), (-0.50, 0, 0.55), 0.10, segs=3), redc)       # front lip
    for y in (-0.41, 0.41):
        k.add(lambda bm, y=y: tube(bm, [Vector((-0.62, y, 0.74)), Vector((-0.3, y, 0.76)), Vector((0.3, y, 0.76)), Vector((0.67, y, 1.08))], 0.025, 0.025, 6), 'gold')
        run = [Vector((0.62, y, 0.05)), Vector((0.0, y, 0.0)), Vector((-0.55, y, 0.02)), Vector((-0.72, y, 0.14)), Vector((-0.74, y, 0.30)), Vector((-0.64, y, 0.36))]
        k.add(lambda bm, run=run: tube(bm, run, 0.04, 0.035, 6), 'gold')
        for x in (-0.30, 0.30): k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, 0.02)), Vector((x, y, 0.30))], 0.025, 0.025, 6), 'gold')
    k.add(lambda bm: blob(bm, 0.36, (0.25, 0.0, 0.88), (1.0, 1.0, 0.85), 2, 0.03, seed), lambda f: 'khaki' if lit(f.normal) > -0.1 else 'olive')
    present(k, (0.26, 0.24, 0.22), (0.05, -0.18, 0.72), 'xmas_green', 'gold', 0.3, bow=False, snow=False)
    present(k, (0.22, 0.22, 0.20), (0.10, 0.20, 0.72), 'gold', 'xmas_red', -0.4, bow=False, snow=False)
    k.add(lambda bm: snow_on(bm, 0.5, 0.6, 0.73, (-0.2, 0.0), 0.05), snowc)
    return k.obj('Sleigh')

# ------------------------------------------------------------------ big bauble (decor)
def build_bigbauble(seed=881):
    """two giant glass baubles fallen in the snow"""
    k = Kit(['xmas_red', 'xmas_red_dark', 'gold', 'gold_dark', 'snow'])
    k.add(lambda bm: sphere(bm, 0.42, (0, 0, 0.36), sub=3), 'xmas_red')
    k.add(lambda bm: lathe(bm, [(0.09, 0.0), (0.09, 0.10), (0.0, 0.10)], 8, None, 0, Matrix.Translation((0.28, 0, 0.62)) @ Matrix.Rotation(-0.9, 4, 'Y')), 'gold')
    k.add(lambda bm: sphere(bm, 0.24, (0.62, -0.30, 0.18), sub=3), 'gold')
    k.add(lambda bm: lathe(bm, [(0.06, 0.0), (0.06, 0.07), (0.0, 0.07)], 8, None, 0, Matrix.Translation((0.62, -0.30, 0.40))), 'xmas_red')
    k.add(lambda bm: blob(bm, 1.0, (0.2, -0.1, 0), (0.95, 0.80, 0.16), 3, 0.02, seed), snowc)
    return k.obj('BigBauble')

def build_pinegrass():
    return grass.build_frostgrass(83, 'PineGrass', 'pine_grad', 0.82)
