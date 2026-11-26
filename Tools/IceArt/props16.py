"""Swamp theme (a toxic bog and a glowing mushroom wood, 1960s-70s backwoods): dark peat, mossy boulders, cypress roots
and logs, giant mushrooms (red with cream spots, and glowing blue / purple / green ones), an upturned rowboat, rusty oil
drums, a moonshine still, kerosene lanterns on posts, a fishing shack on stilts. Same palette and chunky style as the
other themes; the glowing faces go to material slot 1 (SwampGlow in Unity: unlit, breathing slowly).

Walls ('#'): MossBoulderA/B, ShroomCluster, RootTangle, LogStack (one family per wall).
Small ('x'): Stump, RowBoat, RustyDrums, MoonshineStill.
Fence ('f'): FallenLog.
Tall ('t'): GiantShroomA (blue), GiantShroomB (purple twins), SwampCypress, SwampLantern.
Bush ('b'): BogGrass (reeds: the Brawl leaf clump, narrow, papyrus ramp).
Decor: ShroomPatch, MossClump, Cattails, SwampStones.
Border: SwampBankA/B/C (low mossy banks; the acid lake lies behind them). Gate: SwampGate.
Backdrop: BigCypress, StiltShack (and the giant mushrooms, scaled up).
The slowing bog puddles and the acid lake are made in Unity (SwampThemeSetup)."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import *
from props3 import blob
from props7 import tube
from props8 import Kit, rbox, sphere
import grass


class SKit(Kit):
    """Kit + gradient faces: each vertex takes its own position t on a gradient cell (frost_grad, ice_grad...)"""
    def __init__(self, colours):
        super().__init__(list(colours))
        self.G = []
        self.gi = self.bm.faces.layers.int.new('gi')
        self.gt = self.bm.verts.layers.float.new('gt')

    def add(self, build, colour, glow=False):
        self.mark(); build(self.bm); fs = self.fresh_faces(); self.paint(fs, colour, glow)
        for f in fs: f[self.gi] = 0

    def grad(self, build, cell, t=0.5, glow=False):
        if cell not in self.G: self.G.append(cell)
        gid = self.G.index(cell) + 1
        self.mark(); build(self.bm); fs = self.fresh_faces(); self.bm.normal_update()
        for f in fs:
            f.material_index = self.ci(cell); f[self.gl] = 1 if glow else 0; f[self.gi] = gid
            for v in f.verts: v[self.gt] = t(v) if callable(t) else t

    def obj(self, name):
        ob = super().obj(name)
        me = ob.data
        if 'gi' in me.attributes and 'gt' in me.attributes:
            gi = me.attributes['gi'].data; gt = me.attributes['gt'].data; uv = me.uv_layers['UVMap'].data
            for p in me.polygons:
                g = gi[p.index].value
                if not g: continue
                for li in p.loop_indices: uv[li].uv = grad_uv(self.G[g - 1], gt[me.loops[li].vertex_index].value)
        for a in ('gi', 'gt'):
            if a in me.attributes: me.attributes.remove(me.attributes[a])
        return ob


# ------------------------------------------------------------------ colours
def mossy(f):
    """dark stone, moss on whatever faces up"""
    n = f.normal
    if n.z > 0.62: return 'palm'
    if n.z > 0.35: return 'palm_dark'
    return 'basalt' if lit(n) > -0.15 else 'basalt_dark'

def bark(f):
    """wet dark bark (one colour all round: pieces get turned any way), moss on the top"""
    return 'palm' if f.normal.z > 0.66 else 'wood_dark'

def logc(f):
    """a sawn log: light end grain on the cut ends (faces along the log's axis = local X), bark, moss on top"""
    n = f.normal
    if abs(n.x) > 0.85: return 'wood_light'
    return bark(f)

def moss(f): return 'palm' if f.normal.z > 0.45 else 'palm_dark'

def ground_skirt(k, sx, sy, seed, z=0.0, h=0.07, y=0.0):
    """peat and moss heaped round the foot of a piece (ties it to the dark floor)"""
    k.add(lambda bm: blob(bm, 1.0, (0, y, z), (sx, sy, h), 3, 0.025, seed), lambda f: 'palm_dark' if f.normal.z > 0.5 else 'wood_dark')

def disc(bm, x, y, r, z, rnd, wob=0.22, segs=14):
    """a flat irregular puddle disc"""
    ph = rnd.uniform(0, 6.28); ph2 = rnd.uniform(0, 6.28)
    lathe(bm, [(r, z), (r, z + 0.015), (0.0, z + 0.02)], segs,
          lambda i, kk, rr, zz: (rr * (1 + wob * 0.6 * math.sin(i * 6.28 / segs * 2 + ph) + wob * 0.4 * math.sin(i * 6.28 / segs * 3 + ph2)), zz),
          0, Matrix.Translation((x, y, 0)))

# ------------------------------------------------------------------ mushrooms
STYLES = {
    'red':    dict(cap='xmas_red', spot='cream', gill='bone_dark', glow=False),
    'brown':  dict(cap='terracotta', spot='khaki', gill='bone_dark', glow=False),
    'blue':   dict(cap='frost_grad', spot='frost_white', gill='ice_light', glow=True),
    'cyan':   dict(cap='ice_grad', spot='frost_white', gill='ice_light', glow=True),
    'purple': dict(cap='obsidian_light', spot='frost_white', gill='frost', glow=True),
    'green':  dict(cap='bulb_green', spot='lamp_warm', gill='pine_light', glow=True),
}
GRAD_CAPS = ('frost_grad', 'ice_grad')

def cap_profile(R, h):
    return [(R, h + 0.08 * R), (R * 1.02, h + 0.20 * R), (R * 0.90, h + 0.44 * R), (R * 0.64, h + 0.68 * R), (R * 0.32, h + 0.82 * R), (0.0, h + 0.86 * R)]

def cap_surface(R, h, u):
    """(z, dz/dr) of the cap's top at radius u * R"""
    prof = cap_profile(R, h)[::-1][:-1]           # centre outward (the last, inward-turning rim step dropped)
    r = u * R
    for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
        if r0 <= r <= r1:
            m = (z1 - z0) / max(r1 - r0, 1e-6)
            return z0 + m * (r - r0), m
    return prof[-1][1], -2.0

def shroom(k, x, y, h, R, style, rnd, tilt=(0.0, 0.0), spots=5, z0=0.0, segs=16):
    """a mushroom: a stout stem, gills under a domed cap, round spots on the cap"""
    st = STYLES[style]
    M = Matrix.Translation((x, y, z0)) @ Matrix.Rotation(tilt[0], 4, 'X') @ Matrix.Rotation(tilt[1], 4, 'Y')
    rs = max(0.03, R * 0.24)
    stem = [(rs * 1.35, 0.0), (rs * 1.05, h * 0.12), (rs * 0.92, h * 0.55), (rs * 0.86, h * 0.96), (rs * 0.6, h + 0.02), (0.0, h + 0.03)]
    k.add(lambda bm: lathe(bm, stem, max(6, segs // 2 + 2), None, 0, M), 'bone')
    k.add(lambda bm: lathe(bm, [(rs * 0.9, h - 0.01), (R * 0.97, h + 0.07 * R)], segs, None, 0, M), st['gill'], glow=st['glow'])
    if st['cap'] in GRAD_CAPS:
        Mi = M.inverted()
        def t(v):
            lz = (Mi @ v.co).z
            return min(1.0, max(0.0, 0.15 + 0.85 * (lz - h) / (0.86 * R)))
        k.grad(lambda bm: lathe(bm, cap_profile(R, h), segs, None, 0, M), st['cap'], t, glow=True)
    else:
        k.add(lambda bm: lathe(bm, cap_profile(R, h), segs, None, 0, M), st['cap'], glow=st['glow'])
    R3 = M.to_3x3()
    for j in range(spots):
        a = rnd.uniform(0, 2 * math.pi); u = rnd.uniform(0.12, 0.74)
        z, m = cap_surface(R, h, u)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        n = (R3 @ (Vector((0, 0, 1)) - d * m).normalized()).normalized()
        s = R * rnd.uniform(0.09, 0.15)
        p = M @ (d * u * R + Vector((0, 0, z - s * 0.12)))
        Q = Vector((0, 0, 1)).rotation_difference(n).to_matrix().to_4x4()
        mat = Matrix.Translation(p) @ Q @ Matrix.Diagonal((s, s, s * 0.42, 1))
        k.add(lambda bm, mat=mat: bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1.0, matrix=mat), st['spot'], glow=st['glow'])

def shelf(k, x, y, z, a, r, col='khaki'):
    """a bracket fungus on a trunk: a half disc sticking out, facing direction a"""
    M = Matrix.Translation((x, y, z)) @ Matrix.Rotation(a, 4, 'Z') @ Matrix.Diagonal((1.0, 1.0, 0.45, 1))
    k.add(lambda bm: lathe(bm, [(0.0, -0.05), (r, 0.0), (r * 0.85, 0.08), (0.0, 0.12)], 10, None, 0, M), col)

def strand(k, p, L, rnd, col='olive'):
    """Spanish moss hanging from a branch"""
    q = Vector(p); pts = [q.copy()]
    for i in range(3):
        q = q + Vector((rnd.uniform(-0.05, 0.05), rnd.uniform(-0.05, 0.05), -L / 3)); pts.append(q.copy())
    k.add(lambda bm, pts=pts: tube(bm, pts, 0.07, 0.015, 5), col)

def lantern(k, p, s=1.0):
    """a kerosene hurricane lantern hanging at p (its bail top): tin cap and base, glowing glass globe, wire guard"""
    x, y, z = p
    k.add(lambda bm: tube(bm, [Vector((x, y, z)), Vector((x, y, z - 0.10 * s))], 0.012 * s, 0.012 * s, 4), 'black')
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.10, -0.05), (0.06, -0.08)], 10, None, 0, Matrix.Translation((x, y, z - 0.08 * s)) @ Matrix.Diagonal((s, s, s, 1))), 'xmas_red_dark')
    k.add(lambda bm: sphere(bm, 0.085 * s, (x, y, z - 0.24 * s), (1, 1, 1.35), 2), 'lamp_warm', glow=True)
    k.add(lambda bm: lathe(bm, [(0.11, 0.0), (0.11, 0.06), (0.0, 0.06)], 10, None, 0, Matrix.Translation((x, y, z - 0.42 * s)) @ Matrix.Diagonal((s, s, s, 1))), 'xmas_red_dark')
    for a in range(3):
        ang = a * 2 * math.pi / 3
        dx, dy = math.cos(ang) * 0.10 * s, math.sin(ang) * 0.10 * s
        k.add(lambda bm, dx=dx, dy=dy: tube(bm, [Vector((x + dx, y + dy, z - 0.12 * s)), Vector((x + dx * 1.05, y + dy * 1.05, z - 0.37 * s))], 0.008 * s, 0.008 * s, 3), 'black')

def trunk(k, x, y, h, r, rnd, flare=1.8, segs=12, lean=(0.0, 0.0)):
    """a bald cypress trunk: a fluted buttress foot flaring out on the ground, tapering up"""
    ph = rnd.uniform(0, 6.28)
    prof = [(r * flare, 0.0), (r * (1 + (flare - 1) * 0.45), h * 0.07), (r * 1.08, h * 0.2), (r, h * 0.45), (r * 0.82, h * 0.8), (r * 0.6, h)]
    def rim(i, kk, rr, zz):
        f = max(0.0, 1.0 - zz / (h * 0.35))
        return rr * (1 + 0.28 * f * math.sin(i * 6.28 / segs * 5 + ph)), zz
    M = Matrix.Translation((x, y, 0)) @ Matrix.Shear('XY', 4, (lean[0], lean[1]))
    k.add(lambda bm: lathe(bm, prof, segs, rim, 0, M), bark)
    return Vector((x + lean[0] * h, y + lean[1] * h, h))

# ------------------------------------------------------------------ walls
def build_mossboulder(kind, seed, name):
    """a big dark boulder furred with moss on top, a smaller stone against it, ferny moss at its foot"""
    k = SKit(['palm', 'palm_dark', 'basalt', 'basalt_dark', 'wood_dark'])
    if kind == 'A':
        k.add(lambda bm: blob(bm, 1.0, (0, 0, 0.8), (0.70, 0.66, 0.95), 2, 0.12, seed), mossy)
        k.add(lambda bm: blob(bm, 1.0, (0.40, -0.32, 0.30), (0.36, 0.32, 0.36), 1, 0.06, seed + 1), mossy)
    else:
        k.add(lambda bm: blob(bm, 1.0, (-0.22, 0.10, 0.62), (0.56, 0.56, 0.72), 2, 0.10, seed), mossy)
        k.add(lambda bm: blob(bm, 1.0, (0.30, -0.08, 0.82), (0.44, 0.50, 0.92), 2, 0.10, seed + 2), mossy)
    rnd = random.Random(seed)
    shroom(k, -0.48, -0.50, 0.22, 0.13, 'cyan' if kind == 'A' else 'green', rnd, spots=2, segs=10)
    shroom(k, -0.30, -0.60, 0.14, 0.09, 'cyan' if kind == 'A' else 'green', rnd, spots=1, segs=8)
    ground_skirt(k, 0.85, 0.80, seed)
    return k.obj(name)

def build_shroomcluster(seed=2101):
    """a wall of giant mushrooms on a mossy mound: a big red one with cream spots, a glowing blue one, small ones"""
    rnd = random.Random(seed)
    k = SKit(['palm', 'palm_dark', 'basalt', 'basalt_dark', 'wood_dark', 'bone'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.72, 0.70, 0.55), 2, 0.06, seed), moss)
    shroom(k, -0.22, 0.18, 1.45, 0.78, 'red', rnd, tilt=(-0.10, -0.12), spots=7)
    shroom(k, 0.36, -0.22, 1.95, 0.56, 'blue', rnd, tilt=(0.12, 0.10), spots=5)
    shroom(k, -0.40, -0.45, 0.75, 0.32, 'red', rnd, tilt=(0.25, -0.15), spots=3, segs=12)
    shroom(k, 0.52, 0.42, 0.62, 0.26, 'blue', rnd, tilt=(-0.2, 0.25), spots=2, segs=12)
    ground_skirt(k, 0.82, 0.80, seed)
    return k.obj('ShroomCluster')

def build_roottangle(seed=2111):
    """a knot of cypress roots and an old broken stump: thick roots arching out over the ground, moss and mushrooms"""
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'palm', 'palm_dark', 'wood_light', 'bone'])
    top = trunk(k, 0.0, 0.05, 1.7, 0.42, rnd, flare=1.5)
    k.add(lambda bm: lathe(bm, [(0.26, 1.69), (0.0, 1.73)], 10, None, 0, Matrix.Translation((0, 0.05, 0))), 'wood_light')   # broken top
    for j in range(7):
        a = j * 2 * math.pi / 7 + rnd.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0))
        h = rnd.uniform(0.9, 1.6)
        pts = [d * 0.15 + Vector((0, 0.05, h)), d * 0.48 + Vector((0, 0.05, h * 0.85)), d * 0.68 + Vector((0, 0.05, h * 0.4)), d * 0.74 + Vector((0, 0.05, -0.05))]
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.15, 0.09, 7), bark)
    k.add(lambda bm: blob(bm, 1.0, (0.05, 0.05, 1.55), (0.36, 0.34, 0.20), 2, 0.05, seed), moss)
    shelf(k, 0.30, -0.25, 1.0, -0.7, 0.18)
    shelf(k, 0.36, -0.15, 0.78, -0.4, 0.14)
    shroom(k, -0.55, -0.42, 0.30, 0.17, 'purple', rnd, spots=2, segs=10)
    shroom(k, -0.38, -0.58, 0.20, 0.11, 'purple', rnd, spots=1, segs=8)
    ground_skirt(k, 0.82, 0.80, seed)
    return k.obj('RootTangle')

def build_logstack(seed=2121):
    """sawn cypress logs stacked between stakes, 3-2-1 high, moss on top, little mushrooms growing on the bark"""
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'wood_light', 'palm', 'palm_dark', 'bone'])
    R = 0.27; L = 1.42
    rows = [(-2 * R * 1.02, 0), (0, 0), (2 * R * 1.02, 0), (-R * 1.02, 1), (R * 1.02, 1), (0, 2)]
    for (y, row) in rows:
        z = R + row * R * 1.72
        x0 = -L / 2 + rnd.uniform(-0.04, 0.04); x1 = L / 2 + rnd.uniform(-0.04, 0.04)
        rr = R * rnd.uniform(0.92, 1.04)
        k.add(lambda bm, y=y, z=z, x0=x0, x1=x1, rr=rr: tube(bm, [Vector((x0, y, z)), Vector((x1, y, z))], rr, rr, 10), logc)
        for xe in (x0 - 0.002, x1 + 0.002):   # growth rings on the cut ends
            M = Matrix.Translation((xe, y, z)) @ Matrix.Rotation(math.pi / 2, 4, 'Y')
            k.add(lambda bm, M=M, rr=rr: lathe(bm, [(rr * 0.5, -0.004), (rr * 0.56, -0.004), (rr * 0.56, 0.004), (rr * 0.5, 0.004)], 10, None, 0, M), 'wood_dark')
    for y in (-0.80, 0.80):
        for x in (-0.45, 0.45): k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, 0.0)), Vector((x, y * 0.97, 1.05))], 0.05, 0.04, 5), 'wood_dark')
    k.add(lambda bm: blob(bm, 1.0, (0.0, 0.0, R * 2 + R * 3.44 - 0.06), (0.55, 0.18, 0.10), 2, 0.03, seed), moss)
    shroom(k, 0.30, -0.42, 0.14, 0.10, 'green', rnd, tilt=(1.0, 0.0), z0=0.62, spots=1, segs=8)
    shroom(k, 0.05, -0.30, 0.12, 0.08, 'green', rnd, tilt=(0.9, 0.0), z0=1.05, spots=1, segs=8)
    shelf(k, -0.40, -0.55, 0.30, -1.57, 0.12)
    ground_skirt(k, 0.85, 0.85, seed)
    return k.obj('LogStack')

# ------------------------------------------------------------------ small
def build_stump(seed=2131):
    """a sawn cypress stump with its roots, bracket fungi on the side, small glowing mushrooms at its foot"""
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'wood_light', 'palm', 'palm_dark', 'bone', 'khaki'])
    trunk(k, 0, 0, 0.85, 0.42, rnd, flare=1.6)
    k.add(lambda bm: lathe(bm, [(0.35, 0.84), (0.0, 0.86)], 12), 'wood_light')
    for rr in (0.12, 0.22): k.add(lambda bm, rr=rr: lathe(bm, [(rr, 0.861), (rr + 0.02, 0.861), (rr + 0.02, 0.866), (rr, 0.866)], 12), 'wood_dark')
    shelf(k, 0.32, -0.22, 0.55, -0.6, 0.17)
    shelf(k, 0.38, -0.08, 0.38, -0.2, 0.13, 'orange')
    for j, (x, y) in enumerate([(-0.50, -0.30), (-0.38, -0.48), (0.48, 0.40)]):
        shroom(k, x, y, rnd.uniform(0.16, 0.28), rnd.uniform(0.09, 0.15), 'blue', rnd, spots=2, segs=10)
    k.add(lambda bm: blob(bm, 1.0, (-0.05, 0.05, 0.86), (0.18, 0.15, 0.06), 1, 0.02, seed), moss)
    ground_skirt(k, 0.75, 0.70, seed)
    return k.obj('Stump')

def build_rowboat(seed=2141):
    """an old wooden rowboat pulled up on the bank: cream hull with a green top strake, a dark keel, plank seats and a
    pair of oars inside"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'enamel', 'wood_dark', 'wood', 'wood_light', 'palm_dark'])
    L, W, H = 1.48, 0.78, 0.46
    NS, NA = 12, 8
    def sec(i, inner):
        x = -L / 2 + L * i / NS
        u = (x + L / 2) / L                                  # 0 transom .. 1 bow
        w = max(0.02, W / 2 * (1.0 - u ** 2.6) ** 0.5 * (0.84 + 0.16 * (1 - u)))
        hh = H * (0.90 + 0.22 * u ** 3)                      # the bow sheer rises
        if inner: w = max(0.01, w - 0.045); 
        row = []
        for j in range(NA + 1):
            a = -math.pi / 2 + math.pi * j / NA
            z = hh * (1.0 - max(0.0, math.cos(a)) ** 0.6)
            if inner: z = min(hh, z + 0.05)
            row.append(Vector((x, w * math.sin(a), z)))
        return row
    PO = [sec(i, False) for i in range(NS + 1)]; PI = [sec(i, True) for i in range(NS + 1)]
    def surf(bm, P, j0, j1, flip):
        V = [[bm.verts.new(P[i][j]) for j in range(NA + 1)] for i in range(NS + 1)]
        for i in range(NS):
            for j in range(j0, j1):
                q = (V[i][j], V[i][j + 1], V[i + 1][j + 1], V[i + 1][j])
                bm.faces.new(tuple(reversed(q)) if flip else q)
    strake = lambda j: j == 0 or j == NA - 1
    for j in range(NA):   # outside: green top strakes, cream planking, dark keel strake
        col = 'enamel' if strake(j) else ('wood_dark' if j in (NA // 2 - 1, NA // 2) and False else 'cream')
        k.add(lambda bm, j=j: surf(bm, PO, j, j + 1, False), col)
    k.add(lambda bm: surf(bm, PI, 0, NA, True), 'wood')                 # inside: bare wood
    def rim(bm):
        for side in (0, NA):
            for i in range(NS):
                q = (bm.verts.new(PO[i][side]), bm.verts.new(PO[i + 1][side]), bm.verts.new(PI[i + 1][side]), bm.verts.new(PI[i][side]))
                bm.faces.new(q if side == NA else tuple(reversed(q)))
    k.add(rim, 'wood_dark')
    def transom(bm):
        o = [bm.verts.new(PO[0][j]) for j in range(NA + 1)]
        bm.faces.new(o)
    k.add(transom, 'enamel')
    k.add(lambda bm: tube(bm, [PO[0][NA // 2] + Vector((0, 0, 0.0)), PO[NS][NA // 2] + Vector((0.03, 0, 0.05))], 0.03, 0.025, 5), 'wood_dark')   # keel
    for x in (-0.38, 0.12):   # thwarts
        k.add(lambda bm, x=x: rbox(bm, (0.14, W * 0.84 - (0.12 if x > 0 else 0.02), 0.035), (x, 0, H * 0.62), 0.01, segs=1), 'wood_light')
    for (y, a) in [(-0.10, 0.06), (0.12, -0.05)]:   # oars inside
        p0 = Vector((-0.70, y, H * 0.75)); p1 = Vector((0.45, y + a, H * 0.68))
        k.add(lambda bm, p0=p0, p1=p1: tube(bm, [p0, p1], 0.022, 0.022, 5), 'wood_light')
        k.add(lambda bm, p1=p1: rbox(bm, (0.30, 0.10, 0.02), (p1.x + 0.13, p1.y, p1.z - 0.01), 0.01, segs=1), 'wood_light')
    ground_skirt(k, 0.80, 0.55, seed, h=0.05)
    return k.obj('RowBoat')

def drum(k, x, y, z, col, rot=None, rnd=None):
    M = Matrix.Translation((x, y, z)) @ (rot or Matrix())
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.27, 0.0), (0.28, 0.03), (0.28, 0.85), (0.27, 0.88), (0.0, 0.88)], 12, None, 0, M), col)
    for zz in (0.28, 0.58): k.add(lambda bm, zz=zz: lathe(bm, [(0.287, zz), (0.287, zz + 0.04), (0.0, zz + 0.04)], 12, None, 0, M), 'wood_dark')
    k.add(lambda bm: lathe(bm, [(0.05, 0.881), (0.05, 0.90), (0.0, 0.90)], 6, None, 0, M @ Matrix.Translation((0.15, 0.0, 0.0))), 'metal')

def build_rustydrums(seed=2151):
    """three old oil drums dumped in the bog, faded paint gone to rust, one tipped over leaking a glowing ooze"""
    rnd = random.Random(seed)
    k = SKit(['terracotta', 'olive', 'enamel', 'wood_dark', 'metal', 'bulb_green', 'palm_dark'])
    drum(k, -0.30, 0.28, 0.0, 'terracotta')
    drum(k, 0.30, 0.34, 0.0, 'olive')
    drum(k, 0.05, -0.28, 0.28, 'enamel', Matrix.Rotation(0.3, 4, 'Z') @ Matrix.Rotation(math.pi / 2, 4, 'Y') @ Matrix.Translation((0, 0, -0.44)))
    k.add(lambda bm: disc(bm, 0.38, -0.55, 0.30, 0.0, rnd, 0.3), 'bulb_green', glow=True)
    k.add(lambda bm: disc(bm, 0.62, -0.45, 0.12, 0.0, rnd, 0.3), 'bulb_green', glow=True)
    for j in range(3): k.add(lambda bm, j=j: blob(bm, 1.0, (-0.55 + j * 0.12, -0.45 + j * 0.05, 0), (0.10, 0.08, 0.07), 1, 0.02, seed + j), mossy)
    ground_skirt(k, 0.75, 0.75, seed, h=0.05)
    return k.obj('RustyDrums')

def build_moonshinestill(seed=2161):
    """a backwoods copper still: a domed pot over a stone firebox with glowing embers, the swan neck running into a
    wooden worm tub, stoneware jugs and split wood"""
    rnd = random.Random(seed)
    k = SKit(['terracotta', 'gold', 'gold_dark', 'wood', 'wood_dark', 'wood_light', 'basalt', 'basalt_dark', 'orange', 'cream', 'palm', 'palm_dark'])
    for j in range(9):   # firebox ring of stones
        a = j * 2 * math.pi / 9
        k.add(lambda bm, a=a, j=j: blob(bm, 1.0, (-0.25 + math.cos(a) * 0.36, 0.05 + math.sin(a) * 0.36, 0), (0.14, 0.13, 0.20), 1, 0.03, seed + j), mossy)
    k.add(lambda bm: disc(bm, -0.25, 0.05, 0.26, 0.02, rnd, 0.15), 'orange', glow=True)   # embers
    copper = lambda f: 'gold_dark' if f.normal.z > 0.55 else ('terracotta' if lit(f.normal) > -0.2 else 'wood')
    M = Matrix.Translation((-0.25, 0.05, 0.25))
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.30, 0.02), (0.36, 0.20), (0.36, 0.45), (0.30, 0.58), (0.14, 0.66), (0.08, 0.80), (0.0, 0.80)], 14, None, 0, M), copper)
    k.add(lambda bm: lathe(bm, [(0.365, 0.30), (0.365, 0.34), (0.0, 0.34)], 14, None, 0, M), 'gold_dark')
    k.add(lambda bm: tube(bm, [Vector((-0.25, 0.05, 1.02)), Vector((-0.10, 0.02, 1.14)), Vector((0.20, -0.02, 1.05)), Vector((0.36, -0.05, 0.86))], 0.035, 0.03, 6), copper)
    # worm tub (barrel)
    T = Matrix.Translation((0.42, -0.08, 0.0))
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.24, 0.0), (0.28, 0.40), (0.24, 0.80), (0.0, 0.80)], 12, None, 0, T), 'wood')
    for zz in (0.12, 0.62): k.add(lambda bm, zz=zz: lathe(bm, [(0.27, zz), (0.28, zz + 0.05), (0.0, zz + 0.05)], 12, None, 0, T), 'basalt_dark')
    k.add(lambda bm: tube(bm, [Vector((0.66, -0.08, 0.20)), Vector((0.80, -0.08, 0.20)), Vector((0.80, -0.08, 0.12))], 0.02, 0.02, 4), copper)
    for (x, y) in [(0.55, 0.42), (0.30, 0.52)]:   # stoneware jugs
        k.add(lambda bm, x=x, y=y: lathe(bm, [(0.0, 0.0), (0.11, 0.0), (0.13, 0.12), (0.11, 0.24), (0.04, 0.30), (0.035, 0.36), (0.0, 0.36)], 10, None, 0, Matrix.Translation((x, y, 0))), lambda f: 'cream' if f.calc_center_median().z > 0.16 else 'wood_light')
    for j in range(3):   # split firewood
        p = Vector((-0.55 + j * 0.06, -0.55, 0.06 + (0.10 if j == 1 else 0.0))); d = Vector((0.3, 0.12, 0))
        k.add(lambda bm, p=p, d=d: tube(bm, [p - d, p + d], 0.055, 0.055, 5), logc)
    ground_skirt(k, 0.80, 0.78, seed, h=0.05)
    return k.obj('MoonshineStill')

# ------------------------------------------------------------------ fence
def build_fallenlog(seed=2171):
    """a fallen mossy log across the tile (low cover): broken branch stubs, bracket fungi, a few glowing mushrooms"""
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'wood_light', 'palm', 'palm_dark', 'bone', 'khaki'])
    pts = [Vector((-0.78, 0.02, 0.30)), Vector((-0.25, -0.03, 0.33)), Vector((0.30, 0.03, 0.31)), Vector((0.78, -0.01, 0.27))]
    k.add(lambda bm: tube(bm, pts, 0.31, 0.26, 10), logc)
    for (x, a, z) in [(-0.2, 0.8, 0.5), (0.35, -1.1, 0.45)]:
        d = Vector((0.1, math.cos(a), math.sin(a) + 0.6)).normalized()
        k.add(lambda bm, x=x, d=d, z=z: tube(bm, [Vector((x, 0, z)), Vector((x, 0, z)) + d * 0.35], 0.06, 0.04, 5), bark)
    k.add(lambda bm: blob(bm, 1.0, (0.0, 0.0, 0.52), (0.62, 0.20, 0.10), 2, 0.03, seed), moss)
    shelf(k, -0.45, -0.30, 0.32, -1.57, 0.12); shelf(k, -0.30, -0.30, 0.22, -1.4, 0.09)
    shroom(k, 0.55, -0.34, 0.15, 0.10, 'blue', rnd, spots=1, segs=8)
    shroom(k, 0.42, 0.36, 0.20, 0.12, 'blue', rnd, spots=2, segs=8)
    ground_skirt(k, 0.85, 0.45, seed, h=0.05)
    return k.obj('FallenLog')

# ------------------------------------------------------------------ tall
def build_giantshroom(kind, seed, name):
    """a giant glowing mushroom you walk under: 'A' one tall blue cap, 'B' purple twins leaning apart"""
    rnd = random.Random(seed)
    k = SKit(['palm', 'palm_dark', 'wood_dark', 'bone'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.55, 0.52, 0.22), 2, 0.05, seed), moss)
    if kind == 'A':
        shroom(k, 0, 0, 2.55, 0.95, 'blue', rnd, tilt=(0.06, 0.05), spots=8, segs=18)
        shroom(k, 0.35, -0.30, 0.45, 0.22, 'blue', rnd, tilt=(0.3, 0.2), spots=2, segs=10)
        shroom(k, -0.30, -0.35, 0.30, 0.15, 'cyan', rnd, tilt=(0.2, -0.3), spots=1, segs=8)
    else:
        shroom(k, -0.10, 0.08, 2.40, 0.78, 'purple', rnd, tilt=(-0.10, -0.16), spots=7, segs=16)
        shroom(k, 0.22, -0.14, 1.55, 0.52, 'purple', rnd, tilt=(0.22, 0.28), spots=4, segs=14)
        shroom(k, -0.35, -0.38, 0.32, 0.16, 'green', rnd, spots=1, segs=8)
    return k.obj(name)

def build_swampcypress(seed=2181, name='SwampCypress', H=3.6):
    """a bald cypress: fluted foot, a crooked trunk, a few branches with tufts of needles, Spanish moss hanging, knees"""
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'palm', 'palm_dark', 'pine', 'olive', 'khaki'])
    top = trunk(k, 0, 0, H, 0.28 * H / 3.6, rnd, flare=2.0, lean=(0.03, -0.02))
    canopy = lambda f: 'palm' if f.normal.z > 0.3 else 'palm_dark'
    sc = (H / 3.6) ** 0.8
    nb = 4 if H < 5 else 7
    for j in range(nb):
        a = j * 2 * math.pi / nb + rnd.uniform(-0.3, 0.3)
        z = H * rnd.uniform(0.55, 0.88)
        d = Vector((math.cos(a), math.sin(a), 0))
        base = Vector((0.03 * z, -0.02 * z, z)); tip = base + d * H * rnd.uniform(0.22, 0.32) + Vector((0, 0, H * 0.12))
        k.add(lambda bm, base=base, tip=tip: tube(bm, [base, (base + tip) / 2 + Vector((0, 0, 0.1)), tip], 0.08 * H / 3.6, 0.03, 5), bark)
        k.add(lambda bm, tip=tip, j=j: blob(bm, 1.0, (tip.x, tip.y, tip.z), (0.42 * sc, 0.38 * sc, 0.24 * sc), 2, 0.06, seed + j, floor=False), canopy)
        for m in range(2):
            p = base + (tip - base) * rnd.uniform(0.4, 0.9)
            strand(k, p, H * rnd.uniform(0.18, 0.28), rnd)
    k.add(lambda bm: blob(bm, 1.0, (top.x, top.y, top.z + 0.1), (0.50 * sc, 0.46 * sc, 0.32 * sc), 2, 0.06, seed + 9, floor=False), canopy)
    for j in range(4):   # cypress knees round the foot
        a = rnd.uniform(0, 6.28); r = rnd.uniform(0.55, 0.85) * H / 3.6
        k.add(lambda bm, a=a, r=r: lathe(bm, [(0.08, 0.0), (0.05, 0.22), (0.0, 0.26)], 6, None, 0, Matrix.Translation((math.cos(a) * r, math.sin(a) * r, 0))), 'wood_dark')
    ground_skirt(k, 0.7 * H / 3.6, 0.7 * H / 3.6, seed)
    return k.obj(name)

def build_swamplantern(seed=2191):
    """a rough timber post with a crossarm, a kerosene lantern hanging from it, a coil of rope, glowing mushrooms at the foot"""
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'wood', 'khaki', 'black', 'xmas_red_dark', 'lamp_warm', 'palm', 'palm_dark', 'bone'])
    k.add(lambda bm: rbox(bm, (0.20, 0.20, 2.75), (0, 0, 0), 0.03, rnd.uniform(-0.1, 0.1)), 'wood_dark')
    k.add(lambda bm: rbox(bm, (0.80, 0.12, 0.12), (0.30, 0, 2.45), 0.02), 'wood')
    k.add(lambda bm: tube(bm, [Vector((0.0, 0.0, 2.05)), Vector((0.40, 0, 2.47))], 0.03, 0.03, 4), 'wood')   # brace
    k.add(lambda bm: tube(bm, [Vector((0.62, 0, 2.45)), Vector((0.62, 0, 2.22))], 0.01, 0.01, 3), 'black')
    lantern(k, (0.62, 0.0, 2.22), 1.25)
    k.add(lambda bm: lathe(bm, [(0.13, 0.0), (0.16, 0.03), (0.16, 0.10), (0.13, 0.12)], 10, None, 0, Matrix.Translation((0, 0, 1.0))), 'khaki')   # rope coil on a peg
    shroom(k, -0.25, -0.20, 0.22, 0.12, 'green', rnd, spots=1, segs=8)
    shroom(k, 0.22, -0.28, 0.16, 0.09, 'green', rnd, spots=1, segs=8)
    ground_skirt(k, 0.45, 0.45, seed, h=0.06)
    return k.obj('SwampLantern')

def build_boggrass():
    """reeds round the bog (the hiding bush): the Brawl leaf clump, narrow blades, the papyrus ramp"""
    return grass.build_frostgrass(191, 'BogGrass', 'papyrus_grad', 0.55)

# ------------------------------------------------------------------ decor
def build_shroompatch(seed=2201):
    """decor: a ring of small glowing mushrooms on a cushion of moss"""
    rnd = random.Random(seed)
    k = SKit(['palm', 'palm_dark', 'wood_dark', 'bone'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.45, 0.38, 0.08), 2, 0.02, seed), moss)
    for j in range(6):
        a = j * 2 * math.pi / 6 + rnd.uniform(-0.3, 0.3); r = rnd.uniform(0.12, 0.36)
        shroom(k, math.cos(a) * r, math.sin(a) * r, rnd.uniform(0.10, 0.26), rnd.uniform(0.06, 0.13), rnd.choice(['blue', 'blue', 'green', 'purple']), rnd, spots=1, segs=8, z0=0.03)
    return k.obj('ShroomPatch')

def build_mossclump(seed=2211):
    """decor: low hummocks of moss with a pebble"""
    rnd = random.Random(seed)
    k = SKit(['palm', 'palm_dark', 'basalt', 'basalt_dark', 'wood_dark'])
    for j in range(3):
        x, y = rnd.uniform(-0.3, 0.3), rnd.uniform(-0.25, 0.25)
        k.add(lambda bm, x=x, y=y, j=j: blob(bm, 1.0, (x, y, 0), (rnd.uniform(0.2, 0.32), rnd.uniform(0.18, 0.28), rnd.uniform(0.10, 0.16)), 2, 0.03, seed + j), moss)
    k.add(lambda bm: blob(bm, 1.0, (0.35, -0.2, 0), (0.10, 0.08, 0.08), 1, 0.02, seed + 9), mossy)
    return k.obj('MossClump')

def build_cattails(seed=2221):
    """decor: a few cattails (bulrush) by the water: blades and brown velvet heads on stalks (no 'grass' in the name:
    not a hiding bush)"""
    rnd = random.Random(seed)
    k = SKit(['palm', 'palm_dark', 'pine_light', 'wood_dark', 'khaki'])
    for j in range(9):   # blades
        a = rnd.uniform(0, 6.28); h = rnd.uniform(0.45, 0.85); lean = rnd.uniform(0.1, 0.3)
        b = Vector((math.cos(a) * 0.08, math.sin(a) * 0.08, 0)); d = Vector((math.cos(a), math.sin(a), 0))
        pts = [b, b + d * lean * 0.3 + Vector((0, 0, h * 0.5)), b + d * lean + Vector((0, 0, h))]
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.03, 0.004, 3), lambda f: 'palm' if f.calc_center_median().z > 0.3 else 'palm_dark')
    for j in range(4):   # stalks with heads
        x, y = rnd.uniform(-0.12, 0.12), rnd.uniform(-0.12, 0.12); h = rnd.uniform(0.8, 1.05)
        k.add(lambda bm, x=x, y=y, h=h: tube(bm, [Vector((x, y, 0)), Vector((x * 1.3, y * 1.3, h + 0.15))], 0.012, 0.008, 4), 'pine_light')
        k.add(lambda bm, x=x, y=y, h=h: lathe(bm, [(0.0, 0.0), (0.035, 0.02), (0.04, 0.16), (0.03, 0.19), (0.0, 0.2)], 8, None, 0, Matrix.Translation((x * 1.25, y * 1.25, h - 0.12))), 'wood_dark')
    return k.obj('Cattails')

def build_swampstones(seed=2231):
    """decor: a few mossy stones half sunk in the peat"""
    rnd = random.Random(seed)
    k = SKit(['palm', 'palm_dark', 'basalt', 'basalt_dark'])
    for j in range(5):
        x, y = rnd.uniform(-0.5, 0.5), rnd.uniform(-0.4, 0.4); s = rnd.uniform(0.08, 0.2)
        k.add(lambda bm, x=x, y=y, s=s, j=j: blob(bm, 1.0, (x, y, 0), (s * 1.2, s, s * 0.7), 1, s * 0.25, seed + j), mossy)
    return k.obj('SwampStones')

# ------------------------------------------------------------------ border and gate
def build_stiltshack(seed=2241, name='StiltShack', small=False):
    """a fisherman's shack on stilts: plank walls with battens, a rusty corrugated tin roof, a glowing window, a porch
    with a ladder and a hanging lantern, a stovepipe"""
    rnd = random.Random(seed)
    k = SKit(['wood', 'wood_dark', 'wood_light', 'metal', 'terracotta', 'lamp_warm', 'black', 'xmas_red_dark', 'khaki', 'palm', 'palm_dark'])
    s = 0.62 if small else 1.0
    W, D, Z, Hh = 3.0 * s, 2.6 * s, 1.3 * s, 2.0 * s
    for x in (-W / 2 + 0.15 * s, W / 2 - 0.15 * s):
        for y in (-D / 2 - 0.7 * s, -D / 2 + 0.15 * s, D / 2 - 0.15 * s):
            k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, -0.1)), Vector((x, y, Z))], 0.08 * s, 0.07 * s, 6), 'wood_dark')
    k.add(lambda bm: rbox(bm, (W + 0.2 * s, D + 0.9 * s, 0.12 * s), (0, -0.35 * s, Z), 0.02), 'wood_light')                     # deck
    k.add(lambda bm: rbox(bm, (W, D, Hh), (0, 0, Z + 0.12 * s), 0.03), 'wood')
    for i in range(int(W / (0.42 * s)) + 1):   # battens on the front
        x = -W / 2 + 0.12 * s + i * 0.42 * s
        if x > W / 2 - 0.1 * s: break
        k.add(lambda bm, x=x: rbox(bm, (0.06 * s, 0.04 * s, Hh), (x, -D / 2 - 0.02 * s, Z + 0.12 * s), 0.01, segs=1), 'wood_dark')
    k.add(lambda bm: rbox(bm, (0.62 * s, 0.06 * s, 1.30 * s), (-0.55 * s, -D / 2 - 0.03 * s, Z + 0.12 * s), 0.02, segs=1), 'wood_dark')   # door
    k.add(lambda bm: rbox(bm, (0.62 * s, 0.05 * s, 0.50 * s), (0.65 * s, -D / 2 - 0.035 * s, Z + 0.85 * s), 0.02, segs=1), 'lamp_warm', glow=True)   # window
    k.add(lambda bm: rbox(bm, (0.70 * s, 0.06 * s, 0.06 * s), (0.65 * s, -D / 2 - 0.05 * s, Z + 0.83 * s), 0.01, segs=1), 'wood_dark')
    rz = Z + 0.12 * s + Hh
    for side in (-1, 1):   # two roof slopes of rusty tin, ribbed
        M = Matrix.Translation((0, side * D * 0.27, rz + 0.33 * s)) @ Matrix.Rotation(side * 0.42, 4, 'X')
        def slope(bm, M=M):
            bmesh.ops.create_cube(bm, size=1.0, matrix=M @ Matrix.Diagonal((W + 0.5 * s, D * 0.62, 0.06 * s, 1)))
        k.add(slope, lambda f: 'metal' if f.normal.z > 0.2 else 'terracotta')
        for i in range(7):
            x = -W / 2 - 0.2 * s + i * (W + 0.4 * s) / 6
            k.add(lambda bm, M=M, x=x: bmesh.ops.create_cube(bm, size=1.0, matrix=M @ Matrix.Translation((x, 0, 0.04 * s)) @ Matrix.Diagonal((0.05 * s, D * 0.62, 0.03 * s, 1))), 'terracotta')
    k.add(lambda bm: tube(bm, [Vector((W * 0.3, D * 0.2, rz)), Vector((W * 0.3, D * 0.2, rz + 1.1 * s))], 0.07 * s, 0.07 * s, 6), 'black')
    for x in (-W / 2 - 0.05 * s, W / 2 + 0.05 * s):   # porch rail posts
        k.add(lambda bm, x=x: tube(bm, [Vector((x, -D / 2 - 0.75 * s, Z)), Vector((x, -D / 2 - 0.75 * s, Z + 0.8 * s))], 0.04 * s, 0.04 * s, 4), 'wood_dark')
    k.add(lambda bm: tube(bm, [Vector((-W / 2 - 0.05 * s, -D / 2 - 0.75 * s, Z + 0.78 * s)), Vector((0.2 * s, -D / 2 - 0.75 * s, Z + 0.78 * s))], 0.035 * s, 0.035 * s, 4), 'wood_dark')
    for x in (0.55 * s, 0.95 * s):   # ladder
        k.add(lambda bm, x=x: tube(bm, [Vector((x, -D / 2 - 1.25 * s, 0.0)), Vector((x, -D / 2 - 0.8 * s, Z))], 0.03 * s, 0.03 * s, 4), 'wood')
    for i in range(4):
        t = (i + 0.6) / 4.6
        k.add(lambda bm, t=t: tube(bm, [Vector((0.55 * s, -D / 2 - 1.25 * s + 0.45 * s * t, Z * t)), Vector((0.95 * s, -D / 2 - 1.25 * s + 0.45 * s * t, Z * t))], 0.02 * s, 0.02 * s, 4), 'wood')
    lantern(k, (-W / 2 + 0.25 * s, -D / 2 - 0.08 * s, Z + 1.75 * s), 0.9 * s)
    return k.obj(name)

def build_swampbank(kind, seed, name):
    """a border piece in place of the old border cliffs (7.6 m long, 5 m deep, ~4 m tall: low, the acid lake shows
    behind it): a mossy bank of dark boulders, roots and a fallen log. Front = -Y (+Z in Unity). 'A' rocks, roots and
    mushrooms, 'B' with a fishing shack on stilts, 'C' with a bald cypress and a stand of glowing mushrooms."""
    rnd = random.Random(seed)
    k = SKit(['palm', 'palm_dark', 'basalt', 'basalt_dark', 'wood_dark', 'wood_light', 'bone', 'olive'])
    L, D = 7.6, 5.0
    k.add(lambda bm: rbox(bm, (L, D * 0.55, 3.0), (0, D * 0.18, 0), 0.35, segs=2), mossy)   # solid core: nothing seen through
    for j in range(9):   # hummocks of moss and half-buried stones on top (no flat lawn seen from the game camera)
        x = -L / 2 + 0.4 + j * (L - 0.8) / 8 + rnd.uniform(-0.25, 0.25); yy = D * 0.18 + rnd.uniform(-0.8, 0.8)
        if j % 3 == 1:
            k.add(lambda bm, x=x, yy=yy, j=j: blob(bm, 1.0, (x, yy, 2.85), (rnd.uniform(0.35, 0.55), rnd.uniform(0.3, 0.5), rnd.uniform(0.35, 0.6)), 1, 0.06, seed + 50 + j, floor=False), mossy)
        else:
            k.add(lambda bm, x=x, yy=yy, j=j: blob(bm, 1.0, (x, yy, 2.9), (rnd.uniform(0.6, 0.95), rnd.uniform(0.5, 0.8), rnd.uniform(0.18, 0.32)), 2, 0.05, seed + 60 + j, floor=False), lambda f: 'palm_dark' if (f.index % 3 == 0) else 'palm')
    for j in range(8):   # mossy boulders along the face
        x = -L / 2 + 0.45 + j * (L - 0.9) / 7 + rnd.uniform(-0.2, 0.2)
        if kind == 'B' and -2.6 < x < -0.2: continue        # the shack stands there
        if kind == 'C' and 0.9 < x < 2.9: continue          # the cypress
        s = rnd.uniform(0.7, 1.05); h = rnd.uniform(2.2, 3.7)
        k.add(lambda bm, x=x, s=s, h=h, j=j: blob(bm, 1.0, (x, -D / 2 + 1.35 + rnd.uniform(-0.15, 0.25), h * 0.5), (s, 1.1, h * 0.55), 2, 0.12, seed + j), mossy)
    for j in range(5):   # roots crawling down the face
        x = rnd.uniform(-L / 2 + 0.6, L / 2 - 0.6)
        pts = [Vector((x, -D / 2 + 1.6, 3.2)), Vector((x + 0.2, -D / 2 + 0.75, 2.2)), Vector((x - 0.1, -D / 2 + 0.45, 0.9)), Vector((x + 0.15, -D / 2 + 0.25, -0.05))]
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.13, 0.08, 6), bark)
    lx = -1.2 if kind != 'B' else 1.4   # a fallen log at the foot
    p0 = Vector((lx - 1.4, -D / 2 + 0.55, 0.28)); p1 = Vector((lx + 1.4, -D / 2 + 0.45, 0.25))
    k.add(lambda bm: tube(bm, [p0, p1], 0.30, 0.26, 10), logc)
    k.add(lambda bm: blob(bm, 1.0, ((p0.x + p1.x) / 2, -D / 2 + 0.5, 0.5), (1.1, 0.18, 0.08), 2, 0.03, seed), moss)
    for j in range(4):   # Spanish moss hanging over the bank
        x = rnd.uniform(-L / 2 + 0.5, L / 2 - 0.5)
        strand(k, (x, -D / 2 + 1.0, 3.3), rnd.uniform(0.7, 1.3), rnd)
    # mushrooms at the foot
    for j in range(3):
        x = rnd.uniform(-L / 2 + 0.8, L / 2 - 0.8)
        if kind == 'C' and 0.5 < x < 3.3: continue
        shroom(k, x, -D / 2 + 0.25, rnd.uniform(0.35, 0.6), rnd.uniform(0.18, 0.3), rnd.choice(['red', 'blue', 'blue', 'purple']), rnd, spots=2, segs=12)
    if kind == 'B':
        shack_inline(k, -1.4, -1.0, 0.0, rnd)
    if kind == 'C':
        top = trunk(k, 1.9, -1.2, 4.6, 0.42, rnd, flare=1.9, lean=(0.02, -0.03))
        canopy = lambda f: 'palm' if f.normal.z > 0.3 else 'palm_dark'
        for j in range(3):
            a = rnd.uniform(-1.2, 1.2) + (0 if j % 2 else math.pi)
            base = Vector((1.9, -1.2, 3.4 + j * 0.4)); tip = base + Vector((math.cos(a), math.sin(a) * 0.6 - 0.2, 0.0)) * 1.3 + Vector((0, 0, 0.5))
            k.add(lambda bm, base=base, tip=tip: tube(bm, [base, tip], 0.10, 0.04, 5), bark)
            k.add(lambda bm, tip=tip, j=j: blob(bm, 1.0, (tip.x, tip.y, tip.z), (0.55, 0.45, 0.28), 2, 0.06, seed + 30 + j, floor=False), canopy)
            strand(k, (base + tip) / 2, 1.0, rnd)
        k.add(lambda bm: blob(bm, 1.0, (top.x, top.y, top.z + 0.15), (0.6, 0.5, 0.35), 2, 0.06, seed + 40, floor=False), canopy)
        shroom(k, 0.9, -2.0, 1.25, 0.55, 'blue', rnd, tilt=(0.15, 0.1), spots=5)
        shroom(k, 3.0, -1.9, 0.85, 0.42, 'purple', rnd, tilt=(0.15, -0.2), spots=3)
        shroom(k, 2.6, -2.3, 0.35, 0.18, 'cyan', rnd, spots=1, segs=10)
    k.add(lambda bm: blob(bm, 1.0, (0, -D / 2 + 0.55, 0), (L * 0.49, 0.75, 0.10), 3, 0.025, seed), lambda f: 'palm_dark' if f.normal.z > 0.5 else 'wood_dark')
    return k.obj(name)

def shack_inline(k, cx, cy, cz, rnd):
    """a small fishing shack on stilts, standing on the bank (part of a border piece)"""
    W, D, Z, Hh = 1.9, 1.6, 2.2, 1.35
    for x in (cx - W / 2 + 0.1, cx + W / 2 - 0.1):
        for y in (cy - D / 2 + 0.1, cy + D / 2 - 0.1):
            k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, cz)), Vector((x, y, cz + Z))], 0.06, 0.05, 6), 'wood_dark')
    k.add(lambda bm: rbox(bm, (W + 0.2, D + 0.2, 0.10), (cx, cy, cz + Z), 0.02), 'wood_light')
    k.add(lambda bm: rbox(bm, (W, D, Hh), (cx, cy, cz + Z + 0.1), 0.03), 'wood')
    for i in range(5):
        x = cx - W / 2 + 0.15 + i * 0.4
        k.add(lambda bm, x=x: rbox(bm, (0.05, 0.04, Hh), (x, cy - D / 2 - 0.02, cz + Z + 0.1), 0.01, segs=1), 'wood_dark')
    k.add(lambda bm: rbox(bm, (0.45, 0.05, 0.36), (cx + 0.35, cy - D / 2 - 0.03, cz + Z + 0.6), 0.02, segs=1), 'lamp_warm', glow=True)
    rz = cz + Z + 0.1 + Hh
    for side in (-1, 1):
        M = Matrix.Translation((cx, cy + side * D * 0.27, rz + 0.22)) @ Matrix.Rotation(side * 0.42, 4, 'X')
        k.add(lambda bm, M=M: bmesh.ops.create_cube(bm, size=1.0, matrix=M @ Matrix.Diagonal((W + 0.4, D * 0.62, 0.05, 1))), lambda f: 'metal' if f.normal.z > 0.2 else 'terracotta')
        for i in range(5):
            x = -W / 2 - 0.15 + i * (W + 0.3) / 4
            k.add(lambda bm, M=M, x=x: bmesh.ops.create_cube(bm, size=1.0, matrix=M @ Matrix.Translation((x, 0, 0.035)) @ Matrix.Diagonal((0.05, D * 0.62, 0.03, 1))), 'terracotta')
    lantern(k, (cx - W / 2 + 0.2, cy - D / 2 - 0.08, cz + Z + 1.25), 0.8)

def build_swampgate(seed=2251):
    """the gateway behind each spawn (in place of the base scene's gate arch, ~10.8 m wide): two bald cypress trunks,
    a crossbeam of two lashed logs, three kerosene lanterns hanging from it, Spanish moss, a blank hanging board, glowing
    mushrooms at the feet"""
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'wood_light', 'wood', 'palm', 'palm_dark', 'khaki', 'olive', 'black', 'xmas_red_dark', 'lamp_warm', 'bone', 'enamel'])
    X = 4.4
    for s in (-1, 1):
        trunk(k, s * X, 0.0, 7.4, 0.55, rnd, flare=1.9, segs=14)
        k.add(lambda bm, s=s: blob(bm, 1.0, (s * X, 0.0, 7.3), (0.9, 0.8, 0.5), 2, 0.08, seed + s, floor=False), lambda f: 'palm' if f.normal.z > 0.3 else 'palm_dark')
    for z in (5.55, 6.20):
        k.add(lambda bm, z=z: tube(bm, [Vector((-X - 0.9, 0.0, z)), Vector((0.0, 0.0, z + 0.08)), Vector((X + 0.9, 0.0, z))], 0.30, 0.30, 10), logc)
    for s in (-1, 1):   # rope lashings round the posts
        for z in (5.4, 5.75, 6.05, 6.4):
            k.add(lambda bm, s=s, z=z: lathe(bm, [(0.46, z), (0.47, z + 0.07), (0.0, z + 0.07)], 12, None, 0, Matrix.Translation((s * X, 0, 0))), 'khaki')
    for x in (-2.4, 0.0, 2.4):   # lanterns on ropes
        k.add(lambda bm, x=x: tube(bm, [Vector((x, 0.0, 5.3)), Vector((x, 0.0, 4.75))], 0.015, 0.015, 3), 'khaki')
        lantern(k, (x, 0.0, 4.75), 1.6)
    for j in range(7):
        x = rnd.uniform(-X + 0.6, X - 0.6)
        if min(abs(x - c) for c in (-2.4, 0.0, 2.4)) < 0.35: continue
        strand(k, (x, rnd.uniform(-0.15, 0.15), 5.35), rnd.uniform(0.8, 1.6), rnd)
    # a blank hanging board (no lettering), painted green edges
    for x in (-1.5, -0.6):
        k.add(lambda bm, x=x: tube(bm, [Vector((x, -0.25, 5.3)), Vector((x, -0.25, 4.3))], 0.012, 0.012, 3), 'khaki')
    k.add(lambda bm: rbox(bm, (1.25, 0.07, 0.42), (-1.05, -0.25, 3.95), 0.03, segs=1), 'wood_light')
    for z in (3.97, 4.31): k.add(lambda bm, z=z: rbox(bm, (1.27, 0.08, 0.05), (-1.05, -0.25, z), 0.01, segs=1), 'enamel')
    for s in (-1, 1):
        shroom(k, s * X + s * 0.2, -1.0, 0.75, 0.38, 'blue', rnd, spots=3)
        shroom(k, s * X - s * 0.65, -0.85, 0.45, 0.24, 'purple', rnd, spots=2, segs=12)
    k.add(lambda bm: blob(bm, 1.0, (0, -0.9, 0), (5.6, 1.3, 0.08), 3, 0.02, seed), lambda f: 'palm_dark' if f.normal.z > 0.5 else 'wood_dark')
    return k.obj('SwampGate')

# ------------------------------------------------------------------ backdrop
def build_bigcypress():
    return build_swampcypress(2261, 'BigCypress', 9.0)
