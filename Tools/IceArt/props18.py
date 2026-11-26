"""Candy theme (a candy kingdom in the spirit of a 1960s-70s sweet shop): wafer and layer-cake walls, chocolate bars,
gumdrops, marshmallows, cupcakes, a vintage gumball machine, wrapped sweets, doughnuts, swirl lollipops, ice cream
cones, gumdrop trees, gummy bears, peppermint-stick fences, pink sugar grass as the hiding bush; a pink candy castle,
a gingerbread house and a tiered cake beyond a chocolate lake. Same palette and chunky style as the other themes.

PINK: the 8 x 8 palette is full and has no pink, so candy props put their pink faces in material slot 1 (the 'glow'
flag of the kit). In Unity that slot is CandyPink.mat: the lit FlatKit props material tinted pink (CandyThemeSetup),
so a white cell reads pink, frost reads mauve, cream reads peach. CKit.add takes (cell, True) for such faces.

Walls ('#'): WaferBlock, CakeBlock, ChocoBlock, GumdropStack, MarshmallowStack (one family per wall).
Small ('x'): Cupcake, GumballMachine, WrappedSweets, DonutStack.
Fence ('f'): StickFence.
Tall ('t'): LollipopA (pink swirl), LollipopB (rainbow), IceCreamCone, GumdropTree, GummyBear.
Bush ('b'): SugarGrass (the Brawl leaf clump, frost ramp, in the pink slot = purple to pink).
Decor: Sprinkles, WrappedCandy, ChocoChips.
Border: CandyWallA/B/C (low layer-cake banks; the chocolate lake lies behind). Gate: CandyGate.
Backdrop: CandyCastle, GingerbreadHouse, LayerCake."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import *
from props3 import blob
from props7 import tube
from props8 import Kit, rbox, sphere
from props16 import SKit, disc
import grass

PINK = ('snow', True)          # pink (white cell in the pink slot)
PINK2 = ('frost_white', True)  # pale pink
MAUVE = ('frost', True)        # mauve
PEACH = ('cream', True)        # peach
CANDY = ['red', 'orange', 'yellow', 'bulb_green', 'ice', 'obsidian_light', PINK, 'snow']

class CKit(SKit):
    """SKit whose colours may be (cell, pink) tuples: pink faces go to material slot 1 (CandyPink in Unity)"""
    def paint(self, faces, colour, glow=False):
        self.bm.normal_update()
        for f in faces:
            c = colour(f) if callable(colour) else colour
            g = glow
            if isinstance(c, tuple): c, g = c
            f.material_index = self.ci(c); f[self.gl] = 1 if g else 0
    def obj(self, name):
        ob = super().obj(name); ob['pink_preview'] = 1       # build_all tints the preview after the FBX is written
        return ob

PINK_TINT = (0.98, 0.58, 0.80)   # the look in the game (CandyPink.mat _Color 0.92 / 0.48 / 0.68 under the map's ambient)

def pink_preview(ob):
    """Blender preview only, called by build_all AFTER the FBX is exported: the slot-1 material lit and tinted pink
    like CandyPink in Unity. Changing the material before the export swapped the two submeshes in Unity."""
    if len(ob.data.materials) < 2: return ob
    # keep the slot's name sorting after 'IcePalette': Unity orders the submeshes by material name (a copy named
    # 'CandyPink' came first and swapped the two slots: sponge layers pink, pink frosting white)
    m = ob.data.materials[1].copy(); m.name = 'XmasGlowPink'
    nt = m.node_tree; bsdf = nt.nodes['Principled BSDF']; tex = nt.nodes['Image Texture']
    mix = nt.nodes.new('ShaderNodeMixRGB'); mix.blend_type = 'MULTIPLY'; mix.inputs['Fac'].default_value = 1.0
    mix.inputs['Color2'].default_value = PINK_TINT + (1.0,)
    nt.links.new(tex.outputs['Color'], mix.inputs['Color1']); nt.links.new(mix.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Emission Strength'].default_value = 0.0
    ob.data.materials[1] = m
    return ob

def M_at(x, y, z, s=1.0): return Matrix.Translation((x, y, z)) @ Matrix.Diagonal((s, s, s, 1))

def base_skirt(k, sx, sy, seed, h=0.05):
    """a little heap of sugar round the foot (ties a piece to the floor)"""
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (sx, sy, h), 3, 0.02, seed), 'cream')

def sprinkles(k, pts, rnd, L=0.07, r=0.018):
    for p in pts:
        a = rnd.uniform(0, 6.28); d = Vector((math.cos(a), math.sin(a), rnd.uniform(-0.3, 0.3))).normalized() * L / 2
        k.add(lambda bm, p=p, d=d: tube(bm, [p - d, p + d], r, r, 4), rnd.choice(CANDY))

def gumdrop(k, x, y, z, s, col, M=None):
    prof = [(0.0, 0.0), (0.36, 0.0), (0.37, 0.07), (0.32, 0.30), (0.19, 0.45), (0.0, 0.5)]
    k.add(lambda bm: lathe(bm, [(r * s, zz * s) for r, zz in prof], 12, None, 0, (M or Matrix()) @ Matrix.Translation((x, y, z))), col)

def marshmallow(k, x, y, z, s, col):
    prof = [(0.0, 0.0), (0.27, 0.0), (0.32, 0.05), (0.32, 0.41), (0.27, 0.46), (0.0, 0.46)]
    k.add(lambda bm: lathe(bm, [(r * s, zz * s) for r, zz in prof], 14, None, 0, Matrix.Translation((x, y, z))), col)

def drips(k, x0, x1, y, z, rnd, col=PINK, n=5, axis='x', L=(0.12, 0.3), r=0.07):
    """frosting drips hanging down a face (along x at depth y, or along y at x = y if axis is 'y')"""
    for j in range(n):
        t = (j + 0.5) / n + rnd.uniform(-0.04, 0.04)
        u = x0 + (x1 - x0) * t; l = rnd.uniform(*L)
        p0 = Vector((u, y, z)) if axis == 'x' else Vector((y, u, z))
        p1 = p0 + Vector((0, 0, -l))
        k.add(lambda bm, p0=p0, p1=p1: tube(bm, [p0, p1], r, r * 0.9, 6), col)
        k.add(lambda bm, p1=p1: sphere(bm, r * 0.95, (p1.x, p1.y, p1.z), sub=1), col)

def swirl_disc(k, center, R, t, cols, axis='Y', arms=2, turns=1.4, segs=28, rings=7):
    """a flat round candy (lollipop, peppermint): spiral stripes of the colours in cols, a coin facing along axis"""
    prof = [(0.0, -t)] + [(R * i / rings, -t) for i in range(1, rings + 1)] + [(R, t)] + [(R * i / rings, t) for i in range(rings - 1, 0, -1)] + [(0.0, t)]
    rot = Matrix.Rotation(math.pi / 2, 4, 'X') if axis == 'Y' else Matrix.Rotation(math.pi / 2, 4, 'Y') if axis == 'X' else Matrix()
    M = Matrix.Translation(center) @ rot
    Mi = M.inverted(); n = len(cols)
    def col(f):
        p = Mi @ f.calc_center_median()
        r = math.hypot(p.x, p.y)
        if r > R * 0.96: return cols[0]
        w = math.atan2(p.y, p.x) / (2 * math.pi) + r / R * turns
        return cols[int(math.floor(w * arms * n)) % n]
    k.add(lambda bm: lathe(bm, prof, segs, None, 0, M), col)

def stripe_stick(k, p0, p1, r, cols=('red', 'snow'), segs=10, pitch=0.4):
    """a stick with spiral stripes (peppermint stick)"""
    p0, p1 = Vector(p0), Vector(p1)
    d = p1 - p0; L = d.length; d = d.normalized()
    n = max(6, int(L / pitch * 6))
    pts = [p0 + d * (L * i / n) for i in range(n + 1)]
    a_ = d.orthogonal().normalized(); b_ = d.cross(a_)
    def col(f):
        c = f.calc_center_median() - p0
        s = c.dot(d); q = c - d * s
        v = s / pitch + math.atan2(q.dot(b_), q.dot(a_)) / (2 * math.pi)
        return cols[int(math.floor(v * len(cols))) % len(cols)]
    k.add(lambda bm: tube(bm, pts, r, r, segs), col)

def prof_r(prof, z):
    for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
        if z0 <= z <= z1 and z1 > z0: return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
    return 0.0

# ------------------------------------------------------------------ walls
def build_waferblock(seed=2601):
    """two stacked wafer bars: crisp biscuit sheets with a grid pattern and pink / vanilla cream between them"""
    rnd = random.Random(seed)
    k = CKit(['wood_light', 'sandstone', 'sandstone_dark', 'snow', 'cream'])
    z = 0.0
    for b, (w, d, yaw, fill) in enumerate([(1.44, 1.40, 0.0, PINK), (1.30, 1.26, 0.06, 'cream')]):
        for layer in range(4):
            k.add(lambda bm, z=z, w=w, d=d, yaw=yaw: rbox(bm, (w, d, 0.16), (0, 0, z), 0.03, yaw, segs=1), 'wood_light')
            z += 0.16
            if layer < 3:
                k.add(lambda bm, z=z, w=w, d=d, yaw=yaw: rbox(bm, (w - 0.04, d - 0.04, 0.07), (0, 0, z), 0.02, yaw, segs=1), fill)
                z += 0.07
        # the wafer grid on top: raised ridges
        for i in range(-3, 4):
            k.add(lambda bm, i=i, z=z, w=w, d=d, yaw=yaw: bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Translation((i * w / 7, 0, z + 0.017)) @ Matrix.Diagonal((0.035, d - 0.06, 0.035, 1))), 'sandstone')
            k.add(lambda bm, i=i, z=z, w=w, d=d, yaw=yaw: bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Translation((0, i * d / 7, z + 0.017)) @ Matrix.Diagonal((w - 0.06, 0.035, 0.035, 1))), 'sandstone')
        z += 0.0
    base_skirt(k, 0.85, 0.85, seed)
    return k.obj('WaferBlock')

def build_cakeblock(seed=2611):
    """a square layer cake: sponge and cream layers, pink frosting on top running down the sides, strawberries"""
    rnd = random.Random(seed)
    k = CKit(['sandstone', 'wood', 'snow', 'cream', 'red', 'bulb_green', 'xmas_green'])
    z = 0.0
    for i, (h, col) in enumerate([(0.42, 'sandstone'), (0.10, 'snow'), (0.42, 'wood'), (0.10, 'snow'), (0.42, 'sandstone')]):
        k.add(lambda bm, z=z, h=h: rbox(bm, (1.40, 1.40, h), (0, 0, z), 0.04, segs=1), col)
        z += h
    k.add(lambda bm: rbox(bm, (1.46, 1.46, 0.16), (0, 0, z), 0.07, segs=2), PINK)
    for side in (-1, 1):
        drips(k, -0.6, 0.6, side * 0.72, z + 0.06, rnd, PINK, 5, 'x')
        drips(k, -0.6, 0.6, side * 0.72, z + 0.06, rnd, PINK, 5, 'y')
    zt = z + 0.16
    for (x, y) in [(-0.4, -0.35), (0.35, -0.4), (0.0, 0.3), (0.45, 0.4), (-0.45, 0.42)]:
        k.add(lambda bm, x=x, y=y: sphere(bm, 0.13, (x, y, zt + 0.1), (1, 1, 1.2), 2), 'red')
        k.add(lambda bm, x=x, y=y: lathe(bm, [(0.0, 0.0), (0.1, 0.02), (0.0, 0.05)], 6, None, 0, Matrix.Translation((x, y, zt + 0.24))), 'bulb_green')
    sprinkles(k, [Vector((rnd.uniform(-0.6, 0.6), rnd.uniform(-0.6, 0.6), zt + 0.01)) for _ in range(14)], rnd)
    base_skirt(k, 0.85, 0.85, seed)
    return k.obj('CakeBlock')

def build_chocoblock(seed=2621):
    """two big chocolate bars stood on end, moulded in squares, the lower part still in red paper and gold foil"""
    rnd = random.Random(seed)
    k = CKit(['wood_dark', 'wood', 'red', 'gold', 'gold_dark', 'snow', 'cream'])
    for (y, h, yaw) in [(-0.33, 2.0, 0.03), (0.36, 1.6, -0.05)]:
        R = Matrix.Translation((0, y, 0)) @ Matrix.Rotation(yaw, 4, 'Z')
        k.add(lambda bm, R=R, h=h: bmesh.ops.create_cube(bm, size=1.0, matrix=R @ Matrix.Translation((0, 0, h / 2)) @ Matrix.Diagonal((1.38, 0.5, h, 1))), 'wood_dark')
        for side in (-1, 1):   # the moulded squares on both faces
            for ix in range(3):
                for iz in range(int(h / 0.42)):
                    x = -0.46 + ix * 0.46; zz = 0.62 + iz * 0.42
                    if zz + 0.2 > h: continue
                    k.add(lambda bm, R=R, x=x, zz=zz, side=side: bmesh.ops.create_cube(bm, size=1.0, matrix=R @ Matrix.Translation((x, side * 0.27, zz)) @ Matrix.Diagonal((0.38, 0.06, 0.34, 1))), 'wood')
        k.add(lambda bm, R=R: bmesh.ops.create_cube(bm, size=1.0, matrix=R @ Matrix.Translation((0, 0, 0.26)) @ Matrix.Diagonal((1.42, 0.54, 0.52, 1))), 'red')     # paper wrapper
        k.add(lambda bm, R=R: bmesh.ops.create_cube(bm, size=1.0, matrix=R @ Matrix.Translation((0, 0, 0.55)) @ Matrix.Diagonal((1.40, 0.52, 0.08, 1))), 'gold')     # foil edge
        k.add(lambda bm, R=R: bmesh.ops.create_cube(bm, size=1.0, matrix=R @ Matrix.Translation((0, 0, 0.26)) @ Matrix.Diagonal((0.5, 0.56, 0.18, 1))), 'cream')    # label
    base_skirt(k, 0.85, 0.85, seed)
    return k.obj('ChocoBlock')

def build_gumdropstack(seed=2631):
    """a heap of giant sugared gumdrops, three levels"""
    rnd = random.Random(seed)
    k = CKit(['red', 'orange', 'yellow', 'bulb_green', 'obsidian_light', 'cream', 'snow'])
    cols = ['red', 'orange', 'yellow', 'bulb_green', 'obsidian_light', PINK]
    i = 0
    for (x, y, z, s) in [(-0.36, -0.36, 0, 1.0), (0.36, -0.36, 0, 1.0), (-0.36, 0.36, 0, 1.0), (0.36, 0.36, 0, 1.0),
                         (-0.18, 0.0, 0.42, 1.1), (0.24, 0.05, 0.42, 1.05), (0.0, -0.05, 0.92, 1.25)]:
        gumdrop(k, x + rnd.uniform(-0.03, 0.03), y + rnd.uniform(-0.03, 0.03), z, s * 1.05, cols[i % len(cols)]); i += 1
    base_skirt(k, 0.85, 0.85, seed)
    return k.obj('GumdropStack')

def build_marshmallowstack(seed=2641):
    """columns of stacked marshmallows, pink and white"""
    rnd = random.Random(seed)
    k = CKit(['snow', 'cream'])
    for ci, (x, y, n) in enumerate([(-0.36, -0.36, 4), (0.36, -0.36, 3), (-0.36, 0.36, 3), (0.36, 0.36, 4), (0.0, 0.0, 5)]):
        for j in range(n):
            marshmallow(k, x + rnd.uniform(-0.03, 0.03), y + rnd.uniform(-0.03, 0.03), j * 0.44, 1.05 if ci < 4 else 1.0, PINK if (j + ci) % 2 else 'snow')
    base_skirt(k, 0.85, 0.85, seed)
    return k.obj('MarshmallowStack')

# ------------------------------------------------------------------ small
def build_cupcake(seed=2651):
    """a giant cupcake: a striped paper case, chocolate sponge, a swirl of pink frosting, sprinkles and a cherry"""
    rnd = random.Random(seed)
    k = CKit(['snow', 'wood', 'wood_dark', 'red', 'cream'])
    def case(f):
        a = math.atan2(f.calc_center_median().y, f.calc_center_median().x)
        return PINK if int(math.floor(a / (2 * math.pi) * 12)) % 2 else 'snow'
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.46, 0.0), (0.50, 0.2), (0.55, 0.4), (0.60, 0.6), (0.0, 0.6)], 24,
                             lambda i, kk, rr, zz: (rr * (1.0 if i % 2 else 0.95) if 0 < kk < 5 else rr, zz)), case)
    k.add(lambda bm: lathe(bm, [(0.60, 0.6), (0.62, 0.7), (0.5, 0.8), (0.0, 0.82)], 20), 'wood')
    prof = [(0.56, 0.72), (0.60, 0.84), (0.50, 0.98), (0.52, 1.06), (0.38, 1.2), (0.40, 1.27), (0.24, 1.4), (0.12, 1.5), (0.0, 1.54)]
    k.add(lambda bm: lathe(bm, prof, 20, lambda i, kk, rr, zz: (rr * (1 + 0.07 * math.sin(i * 6.28 / 20 * 4 + kk * 1.3)), zz)), PINK)
    pts = []
    for j in range(16):
        z = rnd.uniform(0.85, 1.42); a = rnd.uniform(0, 6.28); r = prof_r(prof, z) * 1.02
        pts.append(Vector((math.cos(a) * r, math.sin(a) * r, z)))
    sprinkles(k, pts, rnd, L=0.08, r=0.02)
    k.add(lambda bm: sphere(bm, 0.13, (0.0, 0.0, 1.62), (1, 1, 1.0), 2), 'red')
    k.add(lambda bm: tube(bm, [Vector((0.0, 0.0, 1.72)), Vector((0.04, 0.02, 1.86)), Vector((0.1, 0.03, 1.92))], 0.015, 0.012, 4), 'wood_dark')
    base_skirt(k, 0.7, 0.7, seed)
    return k.obj('Cupcake')

def build_gumballmachine(seed=2661):
    """a vintage gumball machine: a red cast-iron stand with a coin slot, a globe brim-full of gumballs, a red cap"""
    rnd = random.Random(seed)
    k = CKit(['red', 'xmas_red_dark', 'metal', 'stone_dark', 'orange', 'yellow', 'bulb_green', 'ice', 'obsidian_light', 'snow'])
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.42, 0.0), (0.42, 0.1), (0.34, 0.16), (0.24, 0.5), (0.32, 0.62), (0.34, 0.72), (0.0, 0.72)], 16), 'red')
    k.add(lambda bm: rbox(bm, (0.24, 0.08, 0.22), (0, -0.30, 0.38), 0.02, segs=1), 'metal')
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.06, 0.0), (0.06, 0.02), (0.0, 0.02)], 10, None, 0, Matrix.Translation((0, -0.35, 0.5)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), 'stone_dark')
    k.add(lambda bm: lathe(bm, [(0.36, 0.72), (0.40, 0.76), (0.0, 0.78)], 16), 'metal')
    c = Vector((0, 0, 1.16))
    k.add(lambda bm: sphere(bm, 0.40, (c.x, c.y, c.z), sub=2), 'red')           # the packed heart of the globe
    for j in range(90):
        u = rnd.uniform(-1, 1); a = rnd.uniform(0, 6.28); s = math.sqrt(1 - u * u)
        p = c + Vector((s * math.cos(a), s * math.sin(a), u)) * 0.37
        k.add(lambda bm, p=p: sphere(bm, 0.105, (p.x, p.y, p.z), sub=1), rnd.choice(['red', 'orange', 'yellow', 'bulb_green', 'ice', 'obsidian_light', PINK, 'snow']))
    k.add(lambda bm: lathe(bm, [(0.30, 1.48), (0.30, 1.56), (0.20, 1.64), (0.0, 1.66)], 16), 'red')
    k.add(lambda bm: sphere(bm, 0.06, (0, 0, 1.72), sub=1), 'metal')
    return k.obj('GumballMachine')

def wrapped(k, M, L, r, body, stripe):
    """a wrapped sweet along local X: a fat body, twisted paper ends"""
    k.add(lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=M @ Matrix.Diagonal((L * 0.5, r, r, 1))), body)
    for s in (-1, 1):
        R = M @ Matrix.Translation((s * L * 0.48, 0, 0)) @ Matrix.Rotation(s * math.pi / 2, 4, 'Y')
        k.add(lambda bm, R=R: lathe(bm, [(r * 0.25, 0.0), (r * 0.15, r * 0.25), (r * 0.7, r * 0.75), (r * 0.8, r * 0.8), (0.0, r * 0.82)], 10,
                                     lambda i, kk, rr, zz: (rr * (1.18 if i % 2 and kk >= 2 else 1.0), zz), 0, R), stripe)

def build_wrappedsweets(seed=2671):
    """a pile of giant wrapped sweets in twisted paper"""
    rnd = random.Random(seed)
    k = CKit(['red', 'yellow', 'bulb_green', 'ice', 'orange', 'snow', 'cream'])
    for (x, y, z, a, b, s) in [(-0.2, -0.25, 0.28, 0.3, 'red', 'yellow'), (0.25, 0.22, 0.28, 1.6, 'ice', 'snow'), (0.0, 0.0, 0.72, 0.9, PINK, 'snow'), (-0.35, 0.4, 0.22, 2.4, 'bulb_green', 'yellow')]:
        wrapped(k, Matrix.Translation((x, y, z)) @ Matrix.Rotation(a, 4, 'Z'), 0.9, 0.3 if z < 0.5 else 0.28, b, s)
    base_skirt(k, 0.75, 0.75, seed)
    return k.obj('WrappedSweets')

def donut(k, x, y, z, R, r, icing, tilt=None, rnd=None):
    M = Matrix.Translation((x, y, z)) @ (tilt or Matrix())
    prof = [(R + r * math.cos(t), r * math.sin(t)) for t in [-math.pi / 2 + i * 2 * math.pi / 12 for i in range(13)]]
    Mi = M.inverted()
    k.add(lambda bm: lathe(bm, prof, 20, None, 0, M), lambda f: icing if (Mi.to_3x3() @ f.normal).z > 0.15 else 'wood_light')
    if rnd:
        pts = []
        for j in range(10):
            a = rnd.uniform(0, 6.28); rr = R + rnd.uniform(-r * 0.6, r * 0.6)
            pts.append(M @ Vector((math.cos(a) * rr, math.sin(a) * rr, r * 0.95)))
        sprinkles(k, pts, rnd, L=0.07, r=0.018)

def build_donutstack(seed=2681):
    """giant ring doughnuts, iced pink, chocolate and white, sprinkled, one leaning on the stack"""
    rnd = random.Random(seed)
    k = CKit(['wood_light', 'wood_dark', 'snow', 'sandstone'])
    donut(k, 0.0, 0.0, 0.18, 0.40, 0.18, PINK, rnd=rnd)
    donut(k, 0.02, 0.03, 0.54, 0.38, 0.18, 'wood_dark', rnd=rnd)
    donut(k, 0.0, 0.0, 0.90, 0.36, 0.17, 'snow', rnd=rnd)
    donut(k, 0.55, -0.15, 0.45, 0.34, 0.16, PINK, Matrix.Rotation(1.2, 4, 'Y'), rnd)
    base_skirt(k, 0.75, 0.75, seed)
    return k.obj('DonutStack')

# ------------------------------------------------------------------ fence
def build_stickfence(seed=2691):
    """a low fence of peppermint sticks between gumdrop-topped posts (low cover)"""
    rnd = random.Random(seed)
    k = CKit(['red', 'snow', 'bulb_green', 'yellow', 'cream'])
    for x in (-0.68, 0.68):
        stripe_stick(k, (x, 0, 0), (x, 0, 0.9), 0.08, ('bulb_green', 'snow'), pitch=0.3)
        gumdrop(k, x, 0, 0.88, 0.5, 'yellow')
    for z in (0.38, 0.72):
        stripe_stick(k, (-0.75, 0, z), (0.75, 0, z), 0.07, ('red', 'snow'), pitch=0.32)
    base_skirt(k, 0.8, 0.35, seed)
    return k.obj('StickFence')

# ------------------------------------------------------------------ tall
def build_lollipop(kind, seed, name):
    """a giant lollipop: a white stick in a sugar heap, a swirl disc on top, a ribbon bow; 'A' pink and white,
    'B' rainbow"""
    rnd = random.Random(seed)
    k = CKit(['snow', 'cream', 'red', 'orange', 'yellow', 'bulb_green', 'ice', 'obsidian_light'])
    k.add(lambda bm: tube(bm, [Vector((0, 0, 0)), Vector((0, 0, 2.3))], 0.07, 0.07, 8), 'snow')
    cols = [PINK, 'snow'] if kind == 'A' else ['red', 'orange', 'yellow', 'bulb_green', 'ice', 'obsidian_light']
    swirl_disc(k, (0, 0, 2.95), 0.82, 0.13, cols, 'Y', arms=1, turns=1.3 if kind == 'B' else 2.2, rings=9, segs=32)
    for s in (-1, 1):   # ribbon bow
        k.add(lambda bm, s=s: lathe(bm, [(0.0, 0.0), (0.12, 0.18), (0.0, 0.22)], 8, None, 0, Matrix.Translation((0, 0, 2.18)) @ Matrix.Rotation(s * 1.9, 4, 'Y')), PINK if kind == 'B' else 'ice')
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.42, 0.42, 0.14), 2, 0.03, seed), 'cream')
    return k.obj(name)

def build_icecreamcone(seed=2701):
    """a giant ice cream cone planted in whipped cream: a waffle cone, chocolate, strawberry and mint scoops, a cherry"""
    rnd = random.Random(seed)
    k = CKit(['sandstone', 'sandstone_dark', 'wood', 'wood_dark', 'ceramic', 'snow', 'red', 'cream'])
    prof = [(0.0, 0.05)] + [(0.48 * i / 9, 0.05 + 1.40 * i / 9) for i in range(1, 10)] + [(0.0, 1.45)]
    def waffle(f):
        p = f.calc_center_median(); a = math.atan2(p.y, p.x) / (2 * math.pi) * 8
        u, v = (a + p.z * 3.0) % 1.0, (a - p.z * 3.0) % 1.0
        return 'sandstone_dark' if u < 0.18 or v < 0.18 else 'sandstone'
    k.add(lambda bm: lathe(bm, prof, 24), waffle)
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.48, 0.48, 0.32), 2, 0.04, seed), 'snow')
    for (z, r, col) in [(1.72, 0.52, 'wood'), (2.36, 0.46, PINK), (2.92, 0.40, 'ceramic')]:
        k.add(lambda bm, z=z, r=r: bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((0, 0, z)) @ Matrix.Diagonal((r, r, r * 0.88, 1))), col)
        k.add(lambda bm, z=z, r=r: lathe(bm, [(r * 1.02, -0.18), (r * 1.07, -0.12), (r * 0.9, 0.0), (0.0, 0.0)], 16,
                                         lambda i, kk, rr, zz: (rr * (1 + 0.06 * math.sin(i * 2.4)), zz - (0.06 if i % 3 == 0 and kk < 2 else 0)), 0, Matrix.Translation((0, 0, z - r * 0.35))), col)
    k.add(lambda bm: sphere(bm, 0.12, (0, 0, 3.36), sub=2), 'red')
    k.add(lambda bm: tube(bm, [Vector((0, 0, 3.45)), Vector((0.05, 0.0, 3.6))], 0.014, 0.012, 4), 'wood_dark')
    return k.obj('IceCreamCone')

def build_gumdroptree(seed=2711):
    """a candy tree: a twisting chocolate trunk, a round crown of mint frosting studded with gumdrops"""
    rnd = random.Random(seed)
    k = CKit(['wood_dark', 'wood', 'ceramic', 'turquoise', 'red', 'orange', 'yellow', 'obsidian_light', 'cream'])
    k.add(lambda bm: tube(bm, [Vector((0, 0, 0)), Vector((0.08, 0, 0.8)), Vector((-0.06, 0.05, 1.6)), Vector((0.0, 0.0, 2.2))], 0.20, 0.12, 8), 'wood_dark')
    k.add(lambda bm: lathe(bm, [(0.42, 0.0), (0.22, 0.2), (0.0, 0.24)], 8, lambda i, kk, rr, zz: (rr * (1 + 0.3 * math.sin(i * 2.4)), zz)), 'wood_dark')
    c = Vector((0, 0, 2.75))
    k.add(lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation(c) @ Matrix.Diagonal((0.95, 0.95, 0.78, 1))), lambda f: 'ceramic' if f.normal.z > -0.2 else 'turquoise')
    cols = ['red', 'orange', 'yellow', 'obsidian_light', PINK]
    for j in range(14):
        u = rnd.uniform(-0.3, 0.95); a = rnd.uniform(0, 6.28); s = math.sqrt(max(0, 1 - u * u))
        n = Vector((s * math.cos(a), s * math.sin(a), u)).normalized()
        p = c + Vector((n.x * 0.95, n.y * 0.95, n.z * 0.78)) - n * 0.04
        Q = Vector((0, 0, 1)).rotation_difference(n).to_matrix().to_4x4()
        gumdrop(k, 0, 0, 0, 0.45, cols[j % len(cols)], Matrix.Translation(p) @ Q)
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.5, 0.5, 0.06), 2, 0.02, seed), 'cream')
    return k.obj('GumdropTree')

def build_gummybear(seed=2721):
    """a giant gummy bear standing on a cookie (a generic bear shape, moulded, no face)"""
    rnd = random.Random(seed)
    k = CKit(['wood_light', 'wood_dark', 'red', 'orange', 'bulb_green', 'cream'])
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.62, 0.0), (0.64, 0.1), (0.6, 0.16), (0.0, 0.16)], 18), 'wood_light')
    for j in range(7):
        a = rnd.uniform(0, 6.28); r = rnd.uniform(0.1, 0.5)
        k.add(lambda bm, a=a, r=r: sphere(bm, 0.06, (math.cos(a) * r, math.sin(a) * r, 0.16), (1, 1, 0.6), 1), 'wood_dark')
    col = rnd.choice(['red', 'orange', 'bulb_green'])
    z0 = 0.16
    for s in (-1, 1):
        k.add(lambda bm, s=s: bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((s * 0.26, -0.05, z0 + 0.22)) @ Matrix.Diagonal((0.2, 0.24, 0.24, 1))), col)   # feet
        k.add(lambda bm, s=s: bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((s * 0.42, -0.08, z0 + 0.95)) @ Matrix.Rotation(s * 0.5, 4, 'Y') @ Matrix.Diagonal((0.14, 0.15, 0.26, 1))), col)   # arms
        k.add(lambda bm, s=s: sphere(bm, 0.12, (s * 0.22, 0.0, z0 + 1.78), (1, 0.8, 1), 2), col)   # ears
    k.add(lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((0, 0, z0 + 0.78)) @ Matrix.Diagonal((0.42, 0.36, 0.55, 1))), col)   # body
    k.add(lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((0, 0, z0 + 1.52)) @ Matrix.Diagonal((0.32, 0.29, 0.29, 1))), col)   # head
    k.add(lambda bm: sphere(bm, 0.12, (0, -0.25, z0 + 1.46), (1, 0.8, 0.8), 2), col)   # snout
    return k.obj('GummyBear')

def build_sugargrass():
    """pink sugar grass (the hiding bush): the Brawl leaf clump with the pale ice ramp; the leaves go to the pink slot
    (purple roots to pink tips; the pale ice ramp read blue-lilac in the game), the hidden core stays in slot 0"""
    ob = grass.build_frostgrass(211, 'SugarGrass', 'frost_grad', 0.8)   # tinted pink: purple roots -> pink tips
    me = ob.data
    me.materials.append(glow_material())
    for p in me.polygons:
        if p.index >= 33: p.material_index = 1       # the first 33 faces are the core (lathe, 8 x 4 + cap)
    ob['pink_preview'] = 1
    return ob

# ------------------------------------------------------------------ decor
def build_sprinkles(seed=2731):
    rnd = random.Random(seed)
    k = CKit(['red', 'yellow', 'bulb_green', 'ice', 'orange', 'snow', 'obsidian_light'])
    sprinkles(k, [Vector((rnd.uniform(-0.55, 0.55), rnd.uniform(-0.45, 0.45), 0.02)) for _ in range(26)], rnd, L=0.12, r=0.025)
    return k.obj('Sprinkles')

def build_wrappedcandy(seed=2741):
    rnd = random.Random(seed)
    k = CKit(['red', 'yellow', 'bulb_green', 'ice', 'snow', 'orange'])
    for (x, y, a, b, s) in [(-0.25, 0.0, 0.4, 'red', 'snow'), (0.2, 0.18, 1.9, 'yellow', 'orange'), (0.12, -0.22, 2.8, PINK, 'snow')]:
        wrapped(k, Matrix.Translation((x, y, 0.08)) @ Matrix.Rotation(a, 4, 'Z'), 0.26, 0.08, b, s)
    return k.obj('WrappedCandy')

def build_chocochips(seed=2751):
    rnd = random.Random(seed)
    k = CKit(['wood_dark', 'wood'])
    for j in range(7):
        x, y = rnd.uniform(-0.45, 0.45), rnd.uniform(-0.35, 0.35); s = rnd.uniform(0.07, 0.14)
        k.add(lambda bm, x=x, y=y, s=s: lathe(bm, [(0.0, 0.0), (s, 0.0), (s * 0.5, s * 0.8), (0.0, s * 1.4)], 8, None, 0, Matrix.Translation((x, y, 0))), 'wood_dark' if j % 3 else 'wood')
    return k.obj('ChocoChips')

# ------------------------------------------------------------------ border, gate, backdrop
def gingerbread(k, cx, cy, cz, s, rnd):
    """a gingerbread cottage: biscuit walls with white icing trim, a chocolate roof with candy buttons, a gumdrop
    ridge, a lit window"""
    W, D, H = 2.4 * s, 2.0 * s, 1.6 * s
    k.add(lambda bm: rbox(bm, (W, D, H), (cx, cy, cz), 0.06 * s, segs=1), 'wood')
    for side in (-1, 1):
        M = Matrix.Translation((cx, cy + side * D * 0.27, cz + H + 0.40 * s)) @ Matrix.Rotation(-side * 0.62, 4, 'X')
        k.add(lambda bm, M=M: bmesh.ops.create_cube(bm, size=1.0, matrix=M @ Matrix.Diagonal((W + 0.3 * s, D * 0.66, 0.12 * s, 1))), 'wood_dark')
        e = Vector((0, side * D * 0.56, 0)); zz = cz + H + 0.02 * s
        k.add(lambda bm, e=e, zz=zz: tube(bm, [Vector((cx - W / 2 - 0.15 * s, cy + e.y, zz)), Vector((cx + W / 2 + 0.15 * s, cy + e.y, zz))], 0.06 * s, 0.06 * s, 6), 'snow')
        for j in range(5):
            x = cx - W / 2 + (j + 0.5) * W / 5
            k.add(lambda bm, x=x, side=side: sphere(bm, 0.09 * s, (x, cy + side * D * 0.3, cz + H + 0.5 * s), sub=1), rnd.choice(['red', 'yellow', 'bulb_green', 'ice', PINK]))
    k.add(lambda bm: tube(bm, [Vector((cx - W / 2 - 0.15 * s, cy, cz + H + 0.82 * s)), Vector((cx + W / 2 + 0.15 * s, cy, cz + H + 0.82 * s))], 0.07 * s, 0.07 * s, 6), 'snow')
    for j in range(4):
        gumdrop(k, cx - W / 2 + (j + 0.5) * W / 4, cy, cz + H + 0.84 * s, 0.5 * s, ['red', 'bulb_green', 'yellow', PINK][j])
    k.add(lambda bm: rbox(bm, (0.5 * s, 0.06 * s, 0.9 * s), (cx - 0.5 * s, cy - D / 2 - 0.02 * s, cz), 0.05 * s, segs=1), 'wood_dark')
    k.add(lambda bm: rbox(bm, (0.6 * s, 0.06 * s, 0.06 * s), (cx - 0.5 * s, cy - D / 2 - 0.03 * s, cz + 0.9 * s), 0.02, segs=1), 'snow')
    k.add(lambda bm: rbox(bm, (0.5 * s, 0.05 * s, 0.45 * s), (cx + 0.55 * s, cy - D / 2 - 0.02 * s, cz + 0.6 * s), 0.03 * s, segs=1), 'yellow')
    for (dx, dz, w, h) in [(0.55, 0.82, 0.04, 0.45), (0.55, 0.6, 0.5, 0.04), (0.55, 1.05, 0.56, 0.05)]:
        k.add(lambda bm, dx=dx, dz=dz, w=w, h=h: rbox(bm, (w * s, 0.07 * s, h * s), (cx + dx * s, cy - D / 2 - 0.035 * s, cz + (dz - h / 2) * s), 0.01, segs=1), 'snow')

def build_candywall(kind, seed, name):
    """a border piece in place of the old border cliffs (7.6 m long, 5 m deep, ~3.6 m tall: low, the chocolate lake
    shows behind it): a long layer cake with pink frosting running down its face. Front = -Y (+Z in Unity). 'A' wafers,
    gumdrops and marshmallows at its foot, 'B' a gingerbread cottage on top, 'C' giant lollipops and an ice cream cone."""
    rnd = random.Random(seed)
    k = CKit(['sandstone', 'wood', 'snow', 'cream', 'wood_light', 'wood_dark', 'red', 'orange', 'yellow', 'bulb_green', 'ice', 'obsidian_light', 'ceramic'])
    L, D = 7.6, 5.0
    z = 0.0
    for h, col in [(0.8, 'sandstone'), (0.16, 'snow'), (0.8, 'wood'), (0.16, 'snow'), (0.7, 'sandstone')]:
        k.add(lambda bm, z=z, h=h: rbox(bm, (L, D * 0.55, h), (0, D * 0.18, z), 0.08, segs=1), col)
        z += h
    k.add(lambda bm: rbox(bm, (L + 0.06, D * 0.55 + 0.08, 0.2), (0, D * 0.18, z), 0.1, segs=2), PINK)
    yf = D * 0.18 - D * 0.275 - 0.04
    drips(k, -L / 2 + 0.3, L / 2 - 0.3, yf, z + 0.08, rnd, PINK, 14, 'x', L=(0.2, 0.7), r=0.11)
    zt = z + 0.2
    for j in range(9):   # cherries along the top
        x = -L / 2 + 0.5 + j * (L - 1.0) / 8
        k.add(lambda bm, x=x: sphere(bm, 0.18, (x, yf + 0.35, zt + 0.15), sub=2), 'red')
    if kind == 'A':
        for j in range(5):
            x = -L / 2 + 0.8 + j * (L - 1.6) / 4
            if j % 2 == 0:
                gumdrop(k, x, -D / 2 + 0.5, 0, 1.4, ['red', 'bulb_green', 'orange'][j // 2])
            else:
                for m in range(3): marshmallow(k, x, -D / 2 + 0.55, m * 0.46, 1.1, PINK if m % 2 == 0 else 'snow')
    if kind == 'B':
        gingerbread(k, -0.8, D * 0.18, zt, 1.0, rnd)
        for x in (-3.0, 2.4): gumdrop(k, x, -D / 2 + 0.5, 0, 1.3, rnd.choice(['red', 'yellow', 'obsidian_light']))
    if kind == 'C':
        for (x, cols) in [(-2.2, [PINK, 'snow']), (1.6, ['red', 'yellow', 'bulb_green', 'ice'])]:
            k.add(lambda bm, x=x: tube(bm, [Vector((x, -D / 2 + 0.7, 0)), Vector((x, -D / 2 + 0.7, 3.4))], 0.08, 0.08, 8), 'snow')
            swirl_disc(k, (x, -D / 2 + 0.7, 4.1), 0.85, 0.13, cols, 'Y', arms=2 if len(cols) == 2 else 1)
        for m in range(3): marshmallow(k, 3.2, -D / 2 + 0.55, m * 0.46, 1.1, PINK if m % 2 else 'snow')
    k.add(lambda bm: blob(bm, 1.0, (0, -D / 2 + 0.5, 0), (L * 0.49, 0.7, 0.08), 3, 0.02, seed), 'cream')
    return k.obj(name)

def build_candygate(seed=2761):
    """the gateway behind each spawn (in place of the base scene's gate arch, ~10.8 m wide): two thick peppermint
    posts crowned with swirl discs, a wafer-bar lintel with pink frosting and gumdrops, a big swirl candy in the middle"""
    rnd = random.Random(seed)
    k = CKit(['red', 'snow', 'wood_light', 'sandstone', 'cream', 'orange', 'yellow', 'bulb_green', 'obsidian_light', 'ice'])
    X = 4.4
    for s in (-1, 1):
        stripe_stick(k, (s * X, 0, 0), (s * X, 0, 6.2), 0.5, ('red', 'snow'), segs=14, pitch=1.2)
        swirl_disc(k, (s * X, 0, 7.2), 1.0, 0.18, ['red', 'snow'], 'Y', arms=2)
        k.add(lambda bm, s=s: lathe(bm, [(0.75, 0.0), (0.7, 0.3), (0.55, 0.42), (0.0, 0.45)], 14, None, 0, Matrix.Translation((s * X, 0, 0))), 'cream')
    for z, col in [(5.2, 'wood_light'), (5.45, PINK), (5.55, 'wood_light'), (5.8, 'cream'), (5.9, 'wood_light')]:
        h = 0.25 if col == 'wood_light' else 0.1
        k.add(lambda bm, z=z, h=h: rbox(bm, (9.4, 1.0, h), (0, 0, z), 0.04, segs=1), col)
    k.add(lambda bm: rbox(bm, (9.5, 1.08, 0.16), (0, 0, 6.15), 0.07, segs=2), PINK)
    drips(k, -4.2, 4.2, -0.56, 6.2, rnd, PINK, 12, 'x', L=(0.2, 0.55), r=0.09)
    for j in range(7):
        gumdrop(k, -3.3 + j * 1.1, 0, 6.3, 0.8, ['red', 'orange', 'yellow', 'bulb_green', 'ice', 'obsidian_light', PINK][j])
    swirl_disc(k, (0, -0.6, 4.4), 0.75, 0.12, [PINK, 'snow', 'yellow'], 'Y', arms=1)
    k.add(lambda bm: blob(bm, 1.0, (0, -1.0, 0), (5.8, 1.2, 0.06), 3, 0.02, seed), 'cream')
    return k.obj('CandyGate')

def build_candycastle(seed=2771):
    """a pink candy castle on a cake island: frosted walls with gumdrop battlements, round towers with mint and
    lilac cone roofs, a keep crowned with an ice cream scoop and a cherry, a chocolate-bar door"""
    rnd = random.Random(seed)
    k = CKit(['snow', 'frost', 'ceramic', 'turquoise', 'obsidian_light', 'yellow', 'red', 'wood_dark', 'wood', 'sandstone', 'cream', 'orange', 'bulb_green', 'ice'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, -0.2), (11.0, 9.0, 1.6), 2, 0.2, seed, floor=False), 'cream')
    W, D, H = 12.0, 9.0, 4.0
    for (x, y, w, d) in [(0, -D / 2, W, 0.9), (0, D / 2, W, 0.9), (-W / 2, 0, 0.9, D), (W / 2, 0, 0.9, D)]:
        k.add(lambda bm, x=x, y=y, w=w, d=d: rbox(bm, (w, d, H), (x, y, 0), 0.12, segs=1), PINK)
    for j in range(11):
        gumdrop(k, -W / 2 + 0.6 + j * (W - 1.2) / 10, -D / 2, H, 0.9, ['red', 'yellow', 'bulb_green', 'ice', 'orange'][j % 5])
    for (x, y, roof) in [(-W / 2, -D / 2, 'ceramic'), (W / 2, -D / 2, 'obsidian_light'), (-W / 2, D / 2, 'obsidian_light'), (W / 2, D / 2, 'ceramic')]:
        k.add(lambda bm, x=x, y=y: lathe(bm, [(1.3, 0.0), (1.3, 6.0), (1.5, 6.2), (1.5, 6.5), (0.0, 6.5)], 16, None, 0, Matrix.Translation((x, y, 0))), PINK2)
        k.add(lambda bm, x=x, y=y, roof=roof: lathe(bm, [(1.6, 6.5), (0.0, 9.0)], 16, None, 0, Matrix.Translation((x, y, 0))), roof)
        k.add(lambda bm, x=x, y=y: sphere(bm, 0.25, (x, y, 9.15), sub=1), 'yellow')
        for z in (2.5, 4.3): k.add(lambda bm, x=x, y=y, z=z: rbox(bm, (0.5, 0.1, 0.8), (x, y - 1.3, z), 0.1, segs=1), 'wood_dark')
    k.add(lambda bm: lathe(bm, [(3.0, 0.0), (3.0, 7.5), (3.2, 7.7), (0.0, 7.7)], 20), PINK)                           # keep
    k.add(lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((0, 0, 8.4)) @ Matrix.Diagonal((3.0, 3.0, 2.4, 1))), 'snow')
    k.add(lambda bm: lathe(bm, [(3.1, -0.4), (3.25, -0.2), (2.9, 0.1), (0.0, 0.1)], 20, lambda i, kk, rr, zz: (rr, zz - (0.5 if i % 3 == 0 and kk < 2 else 0)), 0, Matrix.Translation((0, 0, 7.9))), 'snow')
    k.add(lambda bm: sphere(bm, 0.6, (0, 0, 11.0), sub=2), 'red')
    k.add(lambda bm: rbox(bm, (2.0, 0.3, 2.8), (0, -D / 2 - 0.45, 0), 0.1, segs=1), 'wood_dark')
    for ix in range(2):
        for iz in range(3):
            k.add(lambda bm, ix=ix, iz=iz: rbox(bm, (0.8, 0.1, 0.75), (-0.45 + ix * 0.9, -D / 2 - 0.62, 0.15 + iz * 0.9), 0.06, segs=1), 'wood')
    return k.obj('CandyCastle')

def build_gingerbreadhouse(seed=2781):
    rnd = random.Random(seed)
    k = CKit(['wood', 'wood_dark', 'snow', 'yellow', 'red', 'bulb_green', 'ice', 'cream'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (2.4, 2.1, 0.25), 2, 0.05, seed), 'cream')
    gingerbread(k, 0, 0, 0.0, 1.8, rnd)
    return k.obj('GingerbreadHouse')

def build_layercake(seed=2791):
    """a three-tier celebration cake: sponge tiers with pink and white frosting, drips, cherries, candles"""
    rnd = random.Random(seed)
    k = CKit(['sandstone', 'snow', 'cream', 'red', 'yellow', 'orange', 'bulb_green', 'ice'])
    z = 0.0
    for t, (R, h, top) in enumerate([(3.0, 1.6, PINK), (2.2, 1.4, 'snow'), (1.4, 1.2, PINK)]):
        k.add(lambda bm, R=R, z=z, h=h: lathe(bm, [(0.0, z), (R, z), (R, z + h), (0.0, z + h)], 24), 'sandstone' if t != 1 else 'cream')
        k.add(lambda bm, R=R, z=z, h=h, top=top: lathe(bm, [(R + 0.06, z + h - 0.25), (R + 0.08, z + h), (0.0, z + h + 0.12)], 24), top)
        for j in range(10):
            a = j * 2 * math.pi / 10 + rnd.uniform(-0.1, 0.1); l = rnd.uniform(0.25, 0.6)
            p0 = Vector((math.cos(a) * (R + 0.06), math.sin(a) * (R + 0.06), z + h - 0.15))
            k.add(lambda bm, p0=p0, l=l: tube(bm, [p0, p0 + Vector((0, 0, -l))], 0.12, 0.11, 6), top)
            k.add(lambda bm, p0=p0, l=l: sphere(bm, 0.12, (p0.x, p0.y, p0.z - l), sub=1), top)
        for j in range(8):
            a = j * 2 * math.pi / 8
            k.add(lambda bm, a=a, R=R, z=z, h=h: sphere(bm, 0.2, (math.cos(a) * (R - 0.3), math.sin(a) * (R - 0.3), z + h + 0.25), sub=2), 'red')
        z += h + 0.12
    for j in range(5):   # candles on the top tier
        a = j * 2 * math.pi / 5
        p = Vector((math.cos(a) * 0.6, math.sin(a) * 0.6, z))
        stripe_stick(k, p, p + Vector((0, 0, 0.8)), 0.08, (['ice', 'bulb_green', 'yellow', 'orange', 'red'][j], 'snow'), pitch=0.3)
        k.add(lambda bm, p=p: lathe(bm, [(0.0, 0.0), (0.08, 0.08), (0.0, 0.3)], 8, None, 0, Matrix.Translation((p.x, p.y, p.z + 0.82))), 'yellow')
    return k.obj('LayerCake')
