"""Factory / construction-site theme (1960s-70s): a works yard of red brick, red-oxide riveted steel, timber
scaffolding, concrete pipes, painted drums and a drum cement mixer. Same palette and chunky style as the other themes.
Era check: no hazard stripes, no warning signs, no plastic cones, nothing modern.

Walls ('#'): BrickWallA/B, CrateStack, GirderStack, PipeStack.
Small ('x'): OilDrums, CableReel, CementMixer, BrickPallet.
Fence ('f'): IronFence (corrugated iron sheet on timber posts).
Tall ('t'): GirderColumn, Scaffold, FactoryLamp, Boiler.
Bush ('b'): YardGrass (weeds on the lot: grass.build_frostgrass with the papyrus ramp).
Decor: ScrapPile, Cogwheel, BrickRubble, Wheelbarrow, PlankBundle.
Backdrop: TowerCrane, Chimney (no smoke), WaterTower, FactoryShed."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import *
from props3 import blob
from props7 import tube
from props8 import Kit, rbox, sphere
import grass

def brick(f):
    """brick: the same red on every side (walls are turned any way in the map: a baked dark back face showed as a brown
    block); only undersides darker. FlatKit's own cel shading does the light and shade."""
    return 'wood_dark' if f.normal.z < -0.5 else 'terracotta'
def oxide(f): return 'red' if lit(f.normal) > -0.1 else 'xmas_red_dark'          # red-oxide primer on steel
def conc(f):
    n = f.normal
    return 'stone_light' if lit(n) > -0.1 else 'stone'
def woodc(f): return 'wood' if lit(f.normal) > -0.15 else 'wood_dark'
def metalc(f): return 'metal' if lit(f.normal) > -0.1 else 'stone_dark'
def paint(col, dark): return lambda f: col if lit(f.normal) > -0.1 else dark

def ground_dust(k, sx, sy, seed, h=0.08):
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (sx, sy, h), 3, 0.02, seed), lambda f: 'khaki' if f.normal.z > 0.4 else 'olive')

def ibeam(k, cx, cy, z, L, h, w, yaw, col=oxide, t=0.05):
    """an I-beam lying along its length (yaw around Z): bottom flange, web, top flange"""
    k.add(lambda bm: rbox(bm, (L, w, t), (cx, cy, z), 0.012, yaw, segs=1), col)
    k.add(lambda bm: rbox(bm, (L, t * 0.8, h - 2 * t), (cx, cy, z + t), 0.01, yaw, segs=1), col)
    k.add(lambda bm: rbox(bm, (L, w, t), (cx, cy, z + h - t), 0.012, yaw, segs=1), col)

def column(k, cx, cy, H, d=0.30, w=0.26, col=oxide, t=0.05):
    """a vertical I-beam column"""
    for s in (-1, 1): k.add(lambda bm, s=s: rbox(bm, (w, t, H), (cx, cy + s * (d / 2 - t / 2), 0), 0.012, segs=1), col)
    k.add(lambda bm: rbox(bm, (t * 0.8, d - 2 * t, H), (cx, cy, 0), 0.01, segs=1), col)

def drum(k, x, y, col, dark, lie=None, s=1.0):
    """a 1960s steel oil drum: rolled hoops, a bung on the lid; lie = yaw to lay it on its side"""
    prof = [(0.0, 0.0), (0.29, 0.0), (0.30, 0.04), (0.29, 0.88), (0.30, 0.91), (0.0, 0.91)]
    M = Matrix.Translation((x, y, 0)) @ Matrix.Diagonal((s, s, s, 1))
    if lie is not None:
        M = Matrix.Translation((x, y, 0.30 * s)) @ Matrix.Rotation(lie, 4, 'Z') @ Matrix.Rotation(math.pi / 2, 4, 'Y') @ Matrix.Translation((0, 0, -0.455)) @ Matrix.Diagonal((s, s, s, 1))
    k.add(lambda bm: lathe(bm, prof, 12, None, 0, M), paint(col, dark))
    for z in (0.30, 0.60): k.add(lambda bm, z=z: lathe(bm, [(0.305, z), (0.305, z + 0.035), (0.0, z + 0.035)], 12, None, 0, M), 'stone_dark')
    k.add(lambda bm: lathe(bm, [(0.05, 0.91), (0.05, 0.95), (0.0, 0.95)], 6, None, 0, M @ Matrix.Translation((0.15, 0.05, 0))), 'metal')

def pbox(bm, size, loc, yaw=0.0):
    """a plain box standing on loc (no bevel: 12 triangles)"""
    sx, sy, sz = size
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((loc[0], loc[1], loc[2] + sz / 2)) @ Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Diagonal((sx, sy, sz, 1)))

def bricks_block(k, sx, sy, z0, h, loc=(0, 0), yaw=0.0, rnd=None, proud=True, CH=0.30, BL=0.55, sides=(-1, 1)):
    """a block of chunky brickwork (Brawl-sized bricks, 0.55 x 0.30 m, stretcher bond): the body, thin mortar courses,
    staggered vertical joints on the long faces in 'sides' (-1 = front), a few bricks standing proud and darker"""
    R = Matrix.Rotation(yaw, 4, 'Z')
    k.add(lambda bm: rbox(bm, (sx, sy, h), (loc[0], loc[1], z0), 0.03, yaw, segs=1), brick)
    n = max(1, int(round(h / CH)))
    ch = h / n
    for i in range(n):
        z = z0 + i * ch
        if i > 0: k.add(lambda bm, z=z: pbox(bm, (sx + 0.010, sy + 0.010, 0.016), (loc[0], loc[1], z - 0.008), yaw), 'bone_dark')
        off = (i % 2) * BL / 2
        u = -sx / 2 + off + BL
        while u < sx / 2 - 0.08:
            for side in sides:
                p = R @ Vector((u, side * sy / 2, 0))
                k.add(lambda bm, p=p, z=z: pbox(bm, (0.016, 0.012, ch), (loc[0] + p.x, loc[1] + p.y, z), yaw), 'bone_dark')
            u += BL
    if proud and rnd:
        for j in range(3):   # a few bricks a little proud and darker: the wall is not one flat slab
            i = rnd.randint(0, n - 1); zz = z0 + i * ch + 0.02
            u = -sx / 2 + (i % 2) * BL / 2 + BL * (rnd.randint(0, max(0, int(sx / BL) - 1)) + 0.5)
            if abs(u) > sx / 2 - 0.2: continue
            side = rnd.choice(list(sides)); p = R @ Vector((u, side * (sy / 2 + 0.008), 0))
            k.add(lambda bm, p=p, zz=zz: pbox(bm, (BL - 0.04, 0.03, ch - 0.04), (loc[0] + p.x, loc[1] + p.y, zz), yaw), 'wood_dark')

# ------------------------------------------------------------------ walls
def build_brickwall(kind, seed, name):
    """a 1.5 m section of factory brick wall with a concrete coping; 'B' with a broken top and fallen bricks"""
    rnd = random.Random(seed)
    k = Kit(['terracotta', 'wood_dark', 'bone_dark', 'stone_light', 'stone', 'khaki', 'olive'])
    if kind == 'A':
        bricks_block(k, 1.44, 0.62, 0.0, 1.95, rnd=rnd)
        k.add(lambda bm: rbox(bm, (1.50, 0.72, 0.14), (0, 0, 1.95), 0.03), conc)                  # coping
        for s in (-1, 1): k.add(lambda bm, s=s: rbox(bm, (0.26, 0.72, 2.05), (s * 0.62, 0, 0), 0.03), brick)   # piers
    else:
        bricks_block(k, 1.44, 0.62, 0.0, 1.30, rnd=rnd)
        x = -0.72
        while x < 0.70:   # the broken top: stepped courses of uneven height
            w = rnd.uniform(0.26, 0.42); h = rnd.choice([0.18, 0.36, 0.54])
            bricks_block(k, w - 0.01, 0.62, 1.30, h, (x + w / 2, 0), rnd=None, proud=False)
            x += w
        for j in range(5):   # bricks fallen at the foot
            k.add(lambda bm, j=j: rbox(bm, (0.24, 0.12, 0.07), (rnd.uniform(-0.6, 0.6), rnd.choice([-1, 1]) * rnd.uniform(0.42, 0.62), 0), 0.01, rnd.uniform(0, 3)), brick)
    ground_dust(k, 0.85, 0.55, seed)
    return k.obj(name)

def build_cratestack(seed=1411):
    """a stack of stencilled wooden packing crates, one lid ajar"""
    rnd = random.Random(seed)
    k = Kit(['wood', 'wood_dark', 'wood_light', 'black', 'khaki', 'olive'])
    def crate(x, y, z, s, yaw):
        k.add(lambda bm: rbox(bm, (s, s, s), (x, y, z), 0.03, yaw), woodc)
        for zz in (z + 0.06, z + s - 0.10):   # frame battens
            k.add(lambda bm, zz=zz: rbox(bm, (s + 0.03, s + 0.03, 0.06), (x, y, zz), 0.01, yaw, segs=1), 'wood_dark')
        R = Matrix.Rotation(yaw, 4, 'Z'); p = R @ Vector((0, -s / 2 - 0.016, 0))
        k.add(lambda bm: rbox(bm, (s * 0.4, 0.01, s * 0.14), (x + p.x, y + p.y, z + s * 0.42), 0.003, yaw, segs=1), 'black')   # stencil
    crate(-0.30, -0.25, 0.0, 0.80, 0.05)
    crate(0.36, 0.22, 0.0, 0.72, -0.12)
    crate(-0.28, 0.40, 0.0, 0.60, 0.2)
    crate(-0.22, -0.18, 0.80, 0.62, 0.35)
    crate(0.34, 0.20, 0.72, 0.55, 0.15)
    ground_dust(k, 0.85, 0.85, seed)
    return k.obj('CrateStack')

def build_girderstack(seed=1421):
    """red-oxide steel I-beams stacked on timber bearers, waiting to go up"""
    rnd = random.Random(seed)
    k = Kit(['red', 'xmas_red_dark', 'wood', 'wood_dark', 'khaki', 'olive'])
    for x in (-0.50, 0.50): k.add(lambda bm, x=x: rbox(bm, (0.16, 1.40, 0.16), (x, 0, 0), 0.02, segs=1), woodc)
    z = 0.16
    for layer in range(4):
        n = 3 if layer % 2 == 0 else 2
        for i in range(n):
            y = (i - (n - 1) / 2) * 0.42
            ibeam(k, rnd.uniform(-0.04, 0.04), y, z, 1.45, 0.30, 0.22, rnd.uniform(-0.02, 0.02))
        z += 0.30
        if layer == 1:   # a second pair of bearers halfway up
            for x in (-0.45, 0.45): k.add(lambda bm, x=x, z=z: rbox(bm, (0.12, 1.30, 0.10), (x, 0, z), 0.02, segs=1), woodc)
            z += 0.10
    ground_dust(k, 0.85, 0.85, seed)
    return k.obj('GirderStack')

def build_pipestack(seed=1431):
    """big concrete drainage pipes stacked two and one, chocked with timber"""
    k = Kit(['stone_light', 'stone', 'stone_dark', 'wood', 'wood_dark', 'khaki', 'olive'])
    R, r, L = 0.36, 0.27, 1.40
    def pipe(x, z):
        M = Matrix.Translation((0, x, z)) @ Matrix.Rotation(math.pi / 2, 4, 'Y') @ Matrix.Translation((0, 0, -L / 2))
        k.add(lambda bm: lathe(bm, [(r, 0.0), (R, 0.0), (R, L), (r, L)], 14, None, 0, M), conc)
        k.add(lambda bm: lathe(bm, [(r, L), (r, 0.0)], 14, None, 0, M @ Matrix.Translation((0, 0, 0))), 'stone_dark')   # inside
        k.add(lambda bm: lathe(bm, [(R + 0.04, L - 0.18), (R + 0.04, L), (R, L)], 14, None, 0, M), conc)               # socket end
    pipe(-0.37, R); pipe(0.37, R); pipe(0.0, R + 2 * R * 0.87)
    for x in (-0.6, 0.6): k.add(lambda bm, x=x: rbox(bm, (0.14, 0.2, 0.12), (x, 0.0, 0), 0.02, segs=1), woodc)   # chocks
    ground_dust(k, 0.85, 0.85, 1431)
    return k.obj('PipeStack')

# ------------------------------------------------------------------ small
def build_oildrums(seed=1441):
    """three painted oil drums, one lying on its side"""
    k = Kit(['olive', 'enamel', 'red', 'xmas_red_dark', 'stone_dark', 'metal', 'khaki', 'black'])
    drum(k, -0.28, -0.12, 'olive', 'enamel')
    drum(k, 0.34, 0.18, 'red', 'xmas_red_dark', s=0.95)
    drum(k, 0.10, -0.50, 'enamel', 'black', lie=0.3, s=0.9)
    ground_dust(k, 0.75, 0.75, seed, h=0.06)
    return k.obj('OilDrums')

def build_cablereel(seed=1451):
    """a big wooden cable drum standing on edge, black cable wound on it, a loose end on the ground"""
    k = Kit(['wood', 'wood_dark', 'black', 'stone_dark', 'khaki', 'olive'])
    D, W = 1.10, 0.62
    M = Matrix.Translation((0, 0, D / 2)) @ Matrix.Rotation(math.pi / 2, 4, 'X') @ Matrix.Translation((0, 0, -W / 2))
    for z0 in (0.0, W - 0.06):   # the two flanges
        k.add(lambda bm, z0=z0: lathe(bm, [(D / 2, z0), (D / 2, z0 + 0.06), (0.0, z0 + 0.06)], 16, None, 0, M), woodc)
        k.add(lambda bm, z0=z0: lathe(bm, [(0.0, z0 - 0.002), (D / 2 - 0.002, z0 - 0.002)], 16, None, 0, M), woodc)
    k.add(lambda bm: lathe(bm, [(0.36, 0.06), (0.36, W - 0.06)], 16, None, 0, M), 'black')           # the cable
    for i in range(6):   # wound turns
        z = 0.08 + i * (W - 0.16) / 5
        k.add(lambda bm, z=z: lathe(bm, [(0.37, z), (0.39, z + 0.04), (0.37, z + 0.08)], 16, None, 0, M), 'black')
    k.add(lambda bm: lathe(bm, [(0.07, -0.02), (0.07, W + 0.02), (0.0, W + 0.02)], 8, None, 0, M), 'stone_dark')   # axle hole
    k.add(lambda bm: tube(bm, [Vector((0.30, 0.30, 0.25)), Vector((0.60, 0.45, 0.06)), Vector((0.85, 0.30, 0.04))], 0.045, 0.045, 6), 'black')
    ground_dust(k, 0.7, 0.55, seed, h=0.05)
    return k.obj('CableReel')

def build_cementmixer(seed=1461):
    """a 1960s drum concrete mixer: a tilted pear-shaped drum on a frame with two spoked wheels and an engine box"""
    k = Kit(['orange', 'wood_dark', 'black', 'metal', 'stone_dark', 'stone_light', 'cream', 'khaki', 'olive'])
    orangec = paint('orange', 'wood_dark')
    # frame and towing bar
    k.add(lambda bm: rbox(bm, (1.20, 0.10, 0.10), (0.0, -0.30, 0.40), 0.01, segs=1), 'stone_dark')
    k.add(lambda bm: rbox(bm, (1.20, 0.10, 0.10), (0.0, 0.30, 0.40), 0.01, segs=1), 'stone_dark')
    k.add(lambda bm: tube(bm, [Vector((0.6, 0, 0.45)), Vector((0.95, 0, 0.30)), Vector((1.0, 0, 0.12))], 0.035, 0.035, 6), 'stone_dark')
    for s in (-1, 1):   # wheels: tyre, hub, spokes
        y = s * 0.42
        M = Matrix.Translation((-0.25, y, 0.30)) @ Matrix.Rotation(math.pi / 2, 4, 'X') @ Matrix.Translation((0, 0, -0.06))
        k.add(lambda bm, M=M: lathe(bm, [(0.22, 0.0), (0.30, 0.0), (0.30, 0.12), (0.22, 0.12)], 14, None, 0, M), 'black')
        k.add(lambda bm, M=M: lathe(bm, [(0.07, 0.02), (0.07, 0.10), (0.0, 0.10)], 8, None, 0, M), orangec)
        for a in range(5):
            ang = a * 2 * math.pi / 5
            k.add(lambda bm, ang=ang, y=y: tube(bm, [Vector((-0.25, y, 0.30)), Vector((-0.25 + math.cos(ang) * 0.22, y, 0.30 + math.sin(ang) * 0.22))], 0.018, 0.018, 4), orangec)
    # engine box with a cream panel
    k.add(lambda bm: rbox(bm, (0.40, 0.46, 0.36), (0.36, 0.0, 0.48), 0.04), orangec)
    k.add(lambda bm: rbox(bm, (0.22, 0.02, 0.16), (0.36, -0.24, 0.58), 0.01, segs=1), 'cream')
    # the drum, tilted towards the back, open mouth
    M = Matrix.Translation((-0.15, 0, 0.62)) @ Matrix.Rotation(-0.75, 4, 'Y')
    k.add(lambda bm: lathe(bm, [(0.10, -0.05), (0.38, 0.12), (0.46, 0.34), (0.40, 0.56), (0.24, 0.78), (0.24, 0.84)], 14, None, 0, M), orangec)
    k.add(lambda bm: lathe(bm, [(0.24, 0.84), (0.20, 0.80), (0.0, 0.60)], 14, None, 0, M), 'stone_dark')      # inside of the mouth
    k.add(lambda bm: lathe(bm, [(0.47, 0.30), (0.47, 0.38), (0.0, 0.38)], 14, None, 0, M), 'stone_dark')      # the gear ring
    ground_dust(k, 0.8, 0.65, seed, h=0.06)
    return k.obj('CementMixer')

def build_brickpallet(seed=1471):
    """a timber pallet stacked with new bricks, a few taken off"""
    rnd = random.Random(seed)
    k = Kit(['terracotta', 'wood_dark', 'bone_dark', 'wood', 'khaki', 'olive'])
    for y in (-0.5, 0.0, 0.5): k.add(lambda bm, y=y: rbox(bm, (1.2, 0.12, 0.10), (0, y, 0), 0.01, segs=1), woodc)
    for x in (-0.5, -0.17, 0.17, 0.5): k.add(lambda bm, x=x: rbox(bm, (0.16, 1.2, 0.04), (x, 0, 0.10), 0.005, segs=1), woodc)
    bricks_block(k, 1.10, 1.05, 0.14, 0.72, rnd=None, proud=False)
    for j in range(4):   # a few bricks lifted off and left on top
        k.add(lambda bm, j=j: rbox(bm, (0.24, 0.12, 0.07), (rnd.uniform(-0.4, 0.4), rnd.uniform(-0.35, 0.35), 0.86), 0.01, rnd.uniform(0, 3)), brick)
    return k.obj('BrickPallet')

# ------------------------------------------------------------------ fence
def build_ironfence(seed=1481):
    """a 1.5 m run of site hoarding: weathered corrugated iron sheets nailed to timber posts and rails"""
    rnd = random.Random(seed)
    k = Kit(['metal', 'stone_dark', 'terracotta', 'wood', 'wood_dark', 'khaki', 'olive'])
    for x in (-0.70, 0.70): k.add(lambda bm, x=x: rbox(bm, (0.10, 0.10, 1.10), (x, 0.06, 0), 0.01), woodc)
    for z in (0.22, 0.82): k.add(lambda bm, z=z: rbox(bm, (1.50, 0.06, 0.08), (0, 0.06, z), 0.01, segs=1), woodc)
    def sheet(bm, x0, x1, top, y=-0.03):
        n = int((x1 - x0) / 0.065)
        ptsb, ptst = [], []
        for i in range(n + 1):
            x = x0 + (x1 - x0) * i / n; yy = y + (0.025 if i % 2 else -0.025)
            ptsb.append(bm.verts.new((x, yy, 0.05))); ptst.append(bm.verts.new((x, yy, top + (x - x0) * 0.02)))
        bb = [bm.verts.new(v.co) for v in ptsb]; bt = [bm.verts.new(v.co) for v in ptst]   # own verts for the back
        for i in range(n):
            bm.faces.new((ptsb[i], ptsb[i + 1], ptst[i + 1], ptst[i]))
            bm.faces.new((bt[i], bt[i + 1], bb[i + 1], bb[i]))   # both sides
    k.add(lambda bm: sheet(bm, -0.74, 0.02, 0.98), lambda f: 'metal' if f.normal.x < 0.4 else 'stone')
    k.add(lambda bm: sheet(bm, -0.02, 0.74, 1.02, -0.045), lambda f: 'terracotta' if f.normal.x < 0.4 else 'wood_dark')   # a rusty sheet
    ground_dust(k, 0.8, 0.35, seed, h=0.06)
    return k.obj('IronFence')

# ------------------------------------------------------------------ tall
def build_girdercolumn(seed=1491):
    """a red-oxide steel column bolted to a concrete footing, a short beam on top with a chain and hook hanging"""
    k = Kit(['red', 'xmas_red_dark', 'stone_light', 'stone', 'stone_dark', 'black', 'metal', 'khaki', 'olive'])
    k.add(lambda bm: rbox(bm, (0.70, 0.70, 0.30), (0, 0, 0), 0.04), conc)
    k.add(lambda bm: rbox(bm, (0.46, 0.46, 0.04), (0, 0, 0.30), 0.01, segs=1), oxide)
    for x in (-0.16, 0.16):
        for y in (-0.16, 0.16): k.add(lambda bm, x=x, y=y: lathe(bm, [(0.03, 0.34), (0.03, 0.38), (0.0, 0.38)], 6, None, 0, Matrix.Translation((x, y, 0))), 'stone_dark')
    for s in (-1, 1): k.add(lambda bm, s=s: rbox(bm, (0.26, 0.05, 2.75), (0, s * 0.125, 0.34), 0.012, segs=1), oxide)
    k.add(lambda bm: rbox(bm, (0.04, 0.20, 2.75), (0, 0, 0.34), 0.01, segs=1), oxide)
    ibeam(k, 0.45, 0, 3.09, 1.30, 0.26, 0.20, 0.0)
    pts = [Vector((0.95, 0, 3.09 - 0.06 * i)) for i in range(10)]
    for i in range(9): k.add(lambda bm, i=i: tube(bm, [pts[i], pts[i + 1]], 0.018, 0.018, 4), 'black')
    k.add(lambda bm: tube(bm, [Vector((0.95, 0, 2.55)), Vector((0.95, 0, 2.45)), Vector((1.02, 0, 2.38)), Vector((1.06, 0, 2.46))], 0.03, 0.025, 6), 'metal')
    return k.obj('GirderColumn')

def build_scaffold(seed=1501):
    """a timber scaffold tower: four poles, lashed ledgers and braces, two plank decks and a ladder"""
    rnd = random.Random(seed)
    k = Kit(['wood', 'wood_dark', 'wood_light', 'black', 'khaki', 'olive'])
    P = 0.55
    for x in (-P, P):
        for y in (-P, P): k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, 0)), Vector((x + rnd.uniform(-0.02, 0.02), y, 3.0))], 0.05, 0.045, 6), woodc)
    for z in (0.95, 1.95, 2.85):
        for (a, b) in [((-P, -P), (P, -P)), ((-P, P), (P, P)), ((-P, -P), (-P, P)), ((P, -P), (P, P))]:
            k.add(lambda bm, a=a, b=b, z=z: tube(bm, [Vector((a[0], a[1], z)), Vector((b[0], b[1], z + rnd.uniform(-0.03, 0.03)))], 0.035, 0.035, 5), 'wood_dark')
    for (a, b, z0, z1) in [((-P, -P), (P, -P), 0.1, 0.95), ((P, P), (-P, P), 0.95, 1.95), ((-P, P), (-P, -P), 1.95, 2.85)]:
        k.add(lambda bm, a=a, b=b, z0=z0, z1=z1: tube(bm, [Vector((a[0], a[1], z0)), Vector((b[0], b[1], z1))], 0.03, 0.03, 5), 'wood_dark')
    for z in (1.0, 2.0):   # plank decks
        for i in range(5):
            k.add(lambda bm, i=i, z=z: rbox(bm, (1.30, 0.22, 0.05), (rnd.uniform(-0.03, 0.03), -0.48 + i * 0.24, z), 0.01, rnd.uniform(-0.02, 0.02), segs=1), 'wood_light' if i % 2 else 'wood')
    for x in (-0.18, 0.18): k.add(lambda bm, x=x: tube(bm, [Vector((x, -P - 0.12, 0)), Vector((x, -P - 0.02, 2.2))], 0.025, 0.025, 5), 'wood_dark')   # ladder
    for i in range(8):
        z = 0.25 + i * 0.26; yy = -P - 0.12 + 0.10 * (z / 2.2)
        k.add(lambda bm, z=z, yy=yy: tube(bm, [Vector((-0.18, yy, z)), Vector((0.18, yy, z))], 0.018, 0.018, 4), 'wood')
    ground_dust(k, 0.8, 0.8, seed)
    return k.obj('Scaffold')

def build_factorylamp(seed=1511):
    """a cast-iron yard lamp: fluted post on a plinth, a swan-neck arm, an enamel shade over an incandescent bulb"""
    k = Kit(['enamel', 'black', 'stone_dark', 'cream', 'lamp_warm', 'stone_light', 'stone', 'khaki', 'olive'])
    k.add(lambda bm: rbox(bm, (0.50, 0.50, 0.25), (0, 0, 0), 0.04), conc)
    k.add(lambda bm: lathe(bm, [(0.14, 0.25), (0.12, 0.40), (0.07, 0.55), (0.06, 3.1), (0.08, 3.18), (0.0, 3.18)], 8), paint('black', 'stone_dark'))
    k.add(lambda bm: tube(bm, [Vector((0, 0, 3.1)), Vector((0.20, 0, 3.32)), Vector((0.50, 0, 3.36)), Vector((0.70, 0, 3.25))], 0.035, 0.03, 6), 'black')
    M = Matrix.Translation((0.72, 0, 3.0))
    k.add(lambda bm: lathe(bm, [(0.40, 0.0), (0.38, 0.04), (0.20, 0.16), (0.06, 0.24), (0.0, 0.26)], 12, None, 0, M), paint('enamel', 'black'))
    k.add(lambda bm: lathe(bm, [(0.0, 0.03), (0.37, 0.025)], 12, None, 0, M), 'cream')                       # white inside
    k.add(lambda bm: sphere(bm, 0.09, (0.72, 0, 2.98), (1, 1, 1.15), 1), 'lamp_warm', glow=True)            # the bulb
    return k.obj('FactoryLamp')

def build_boiler(seed=1521):
    """a vertical riveted boiler / air receiver: a domed tank on legs, a pressure gauge, a valve and pipes"""
    k = Kit(['metal', 'stone_dark', 'black', 'cream', 'red', 'xmas_red_dark', 'stone_light', 'stone', 'khaki', 'olive'])
    for a in range(3):
        ang = a * 2 * math.pi / 3 + 0.4
        k.add(lambda bm, ang=ang: tube(bm, [Vector((math.cos(ang) * 0.42, math.sin(ang) * 0.42, 0)), Vector((math.cos(ang) * 0.38, math.sin(ang) * 0.38, 0.45))], 0.05, 0.05, 6), 'black')
    k.add(lambda bm: lathe(bm, [(0.0, 0.35), (0.40, 0.45), (0.50, 0.65), (0.50, 2.20), (0.40, 2.45), (0.15, 2.58), (0.0, 2.60)], 14), metalc)
    for z in (0.75, 1.42, 2.10):   # riveted seams
        k.add(lambda bm, z=z: lathe(bm, [(0.512, z), (0.512, z + 0.05), (0.0, z + 0.05)], 14), 'stone_dark')
        for i in range(14):
            a = i * 2 * math.pi / 14
            k.add(lambda bm, a=a, z=z: sphere(bm, 0.022, (math.cos(a) * 0.52, math.sin(a) * 0.52, z + 0.025), sub=1), 'stone_dark')
    k.add(lambda bm: lathe(bm, [(0.11, 0.0), (0.11, 0.04), (0.0, 0.04)], 12, None, 0, Matrix.Translation((0.0, -0.52, 1.75)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), 'black')   # gauge rim
    k.add(lambda bm: lathe(bm, [(0.09, 0.0), (0.0, 0.0)], 12, None, 0, Matrix.Translation((0.0, -0.565, 1.75)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), 'cream')               # gauge face
    k.add(lambda bm: tube(bm, [Vector((0.48, 0, 1.2)), Vector((0.75, 0, 1.2)), Vector((0.75, 0, 0.05))], 0.06, 0.06, 8), 'stone_dark')   # pipe down
    k.add(lambda bm: lathe(bm, [(0.10, 0.0), (0.10, 0.03), (0.0, 0.03)], 8, None, 0, Matrix.Translation((0.75, 0, 1.38))), 'red')            # valve wheel
    k.add(lambda bm: tube(bm, [Vector((0.75, 0, 1.2)), Vector((0.75, 0, 1.38))], 0.03, 0.03, 6), 'black')
    return k.obj('Boiler')

def build_yardgrass():
    """weeds grown over the yard (the hiding bush): the Brawl leaf clump with the papyrus green ramp, narrower leaves"""
    return grass.build_frostgrass(151, 'YardGrass', 'papyrus_grad', 0.78)

# ------------------------------------------------------------------ decor
def build_scrappile(seed=1531):
    """decor: a heap of scrap steel: bent bars, an offcut of plate, a short beam end, bolts"""
    rnd = random.Random(seed)
    k = Kit(['metal', 'stone_dark', 'terracotta', 'red', 'xmas_red_dark', 'black', 'khaki', 'olive'])
    for j in range(5):
        a = rnd.uniform(0, 3); L = rnd.uniform(0.6, 1.0)
        p0 = Vector((rnd.uniform(-0.3, 0.3), rnd.uniform(-0.3, 0.3), 0.05 + j * 0.03))
        d = Vector((math.cos(a), math.sin(a), 0))
        k.add(lambda bm, p0=p0, d=d, L=L: tube(bm, [p0 - d * L / 2, p0 + d * 0.1 + Vector((0, 0, 0.06)), p0 + d * L / 2 + Vector((0, 0.08, 0))], 0.025, 0.025, 5), rnd.choice(['terracotta', 'metal', 'stone_dark']))
    k.add(lambda bm: rbox(bm, (0.5, 0.35, 0.03), (0.15, -0.1, 0.0), 0.005, 0.4, segs=1), 'terracotta')
    ibeam(k, -0.2, 0.15, 0.0, 0.45, 0.2, 0.15, 1.1)
    ground_dust(k, 0.6, 0.5, seed, h=0.05)
    return k.obj('ScrapPile')

def build_cogwheel(seed=1541):
    """decor: a big cast-iron gear wheel lying in the dirt"""
    k = Kit(['stone_dark', 'black', 'metal', 'terracotta', 'khaki', 'olive'])
    teeth = 16; R = 0.55
    def gear(bm):
        pts = []
        for i in range(teeth * 4):
            a = i * 2 * math.pi / (teeth * 4); rr = R if (i % 4) in (1, 2) else R - 0.08
            pts.append((math.cos(a) * rr, math.sin(a) * rr))
        b = [bm.verts.new((x, y, 0.0)) for x, y in pts]; t = [bm.verts.new((x, y, 0.10)) for x, y in pts]
        n = len(pts)
        for i in range(n): bm.faces.new((b[i], b[(i + 1) % n], t[(i + 1) % n], t[i]))
        bm.faces.new(t); bm.faces.new(list(reversed(b)))
    k.add(gear, metalc)
    k.add(lambda bm: lathe(bm, [(0.14, 0.10), (0.14, 0.16), (0.07, 0.16), (0.07, 0.10)], 10), 'black')     # hub
    for i in range(5):   # spoke holes painted dark
        a = i * 2 * math.pi / 5
        k.add(lambda bm, a=a: lathe(bm, [(0.08, 0.101), (0.0, 0.101)], 8, None, 0, Matrix.Translation((math.cos(a) * 0.30, math.sin(a) * 0.30, 0))), 'black')
    return k.obj('Cogwheel')

def build_brickrubble(seed=1551):
    """decor: broken bricks and a lump of old mortar"""
    rnd = random.Random(seed)
    k = Kit(['terracotta', 'wood_dark', 'bone_dark', 'stone_light', 'stone', 'khaki', 'olive'])
    for j in range(9):
        k.add(lambda bm, j=j: rbox(bm, (rnd.choice([0.24, 0.14]), 0.12, 0.07), (rnd.uniform(-0.45, 0.45), rnd.uniform(-0.35, 0.35), rnd.choice([0.0, 0.0, 0.06])), 0.01, rnd.uniform(0, 3)), brick)
    k.add(lambda bm: blob(bm, 1.0, (0.1, 0.05, 0), (0.18, 0.14, 0.08), 1, 0.03, seed), conc)
    ground_dust(k, 0.6, 0.5, seed, h=0.04)
    return k.obj('BrickRubble')

def build_wheelbarrow(seed=1561):
    """decor: a steel builder's wheelbarrow with timber handles"""
    k = Kit(['enamel', 'black', 'stone_dark', 'wood', 'wood_dark', 'stone_light', 'stone'])
    def tray(bm):
        vs = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((0.05, 0, 0.45)) @ Matrix.Diagonal((0.70, 0.55, 0.26, 1)))['verts']
        for v in vs:
            if v.co.z > 0.45: v.co.x *= 1.3; v.co.y *= 1.15
            if v.co.x > 0.2 and v.co.z > 0.45: v.co.x += 0.12
    k.add(tray, paint('enamel', 'black'))
    k.add(lambda bm: blob(bm, 1.0, (0.05, 0, 0.58), (0.38, 0.28, 0.06), 1, 0.02, seed, floor=False), conc)    # set concrete in it
    for s in (-1, 1): k.add(lambda bm, s=s: tube(bm, [Vector((0.45, s * 0.14, 0.18)), Vector((-0.2, s * 0.24, 0.40)), Vector((-0.75, s * 0.28, 0.55))], 0.025, 0.025, 5), woodc)
    M = Matrix.Translation((0.55, 0, 0.18)) @ Matrix.Rotation(math.pi / 2, 4, 'X') @ Matrix.Translation((0, 0, -0.05))
    k.add(lambda bm: lathe(bm, [(0.10, 0.0), (0.18, 0.0), (0.18, 0.10), (0.10, 0.10)], 12, None, 0, M), 'black')
    for s in (-1, 1): k.add(lambda bm, s=s: tube(bm, [Vector((-0.2, s * 0.22, 0.40)), Vector((-0.22, s * 0.22, 0.0))], 0.02, 0.02, 4), 'stone_dark')
    return k.obj('Wheelbarrow')

def build_plankbundle(seed=1571):
    """decor: a bundle of sawn planks on two bearers, roped"""
    rnd = random.Random(seed)
    k = Kit(['wood', 'wood_dark', 'wood_light', 'khaki', 'olive'])
    for x in (-0.5, 0.5): k.add(lambda bm, x=x: rbox(bm, (0.10, 0.6, 0.08), (x, 0, 0), 0.01, segs=1), 'wood_dark')
    for row in range(3):
        for i in range(4):
            k.add(lambda bm, row=row, i=i: rbox(bm, (1.5 + rnd.uniform(-0.1, 0.1), 0.12, 0.04), (rnd.uniform(-0.05, 0.05), -0.20 + i * 0.13, 0.08 + row * 0.045), 0.005, rnd.uniform(-0.02, 0.02), segs=1), rnd.choice(['wood', 'wood_light', 'wood']))
    for x in (-0.35, 0.35): k.add(lambda bm, x=x: rbox(bm, (0.04, 0.58, 0.16), (x, 0, 0.07), 0.01, segs=1), 'khaki')
    return k.obj('PlankBundle')

# ------------------------------------------------------------------ backdrop
def build_towercrane(seed=1581):
    """a 1960s lattice tower crane (yellow enamel): square mast, slewing cab, long jib with trolley and hook, counter-jib
    with concrete counterweights"""
    k = Kit(['yellow', 'gold_dark', 'black', 'stone_light', 'stone', 'cream', 'metal'])
    yc = paint('yellow', 'gold_dark')
    H, S = 16.0, 0.9
    corners = [(-S, -S), (S, -S), (S, S), (-S, S)]
    for (x, y) in corners: k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, 0)), Vector((x, y, H))], 0.07, 0.07, 6), yc)
    z = 0.0
    while z < H - 0.1:   # lattice: rings and diagonals
        for i in range(4):
            a, b = corners[i], corners[(i + 1) % 4]
            k.add(lambda bm, a=a, b=b, z=z: tube(bm, [Vector((a[0], a[1], z)), Vector((b[0], b[1], z))], 0.04, 0.04, 4), yc)
            k.add(lambda bm, a=a, b=b, z=z: tube(bm, [Vector((a[0], a[1], z)), Vector((b[0], b[1], z + 1.8))], 0.035, 0.035, 4), yc)
        z += 1.8
    k.add(lambda bm: rbox(bm, (2.6, 2.6, 0.6), (0, 0, 0), 0.05), conc)                     # base
    k.add(lambda bm: rbox(bm, (1.4, 1.2, 1.2), (0.6, -1.2, H), 0.05), yc)                   # cab
    k.add(lambda bm: rbox(bm, (1.0, 0.04, 0.6), (0.6, -1.81, H + 0.4), 0.01, segs=1), 'cream')
    L1, L2 = 22.0, 8.0
    for (y, zz) in [(-0.6, H + 0.1), (0.6, H + 0.1), (0.0, H + 1.4)]:   # triangular jib
        k.add(lambda bm, y=y, zz=zz: tube(bm, [Vector((-L2, y * 0.8, zz)), Vector((L1, y * 0.5, zz))], 0.06, 0.05, 5), yc)
    x = -L2
    while x < L1:
        k.add(lambda bm, x=x: tube(bm, [Vector((x, -0.5, H + 0.1)), Vector((x + 1.2, 0, H + 1.4))], 0.03, 0.03, 4), yc)
        k.add(lambda bm, x=x: tube(bm, [Vector((x, 0.5, H + 0.1)), Vector((x + 1.2, 0, H + 1.4))], 0.03, 0.03, 4), yc)
        x += 2.4
    k.add(lambda bm: rbox(bm, (2.6, 1.6, 1.5), (-L2 + 1.5, 0, H - 1.0), 0.05), conc)        # counterweights
    k.add(lambda bm: tube(bm, [Vector((0, 0, H + 1.4)), Vector((0, 0, H + 4.0))], 0.08, 0.06, 6), yc)   # A-frame
    for xx in (-L2 + 1, L1 - 2): k.add(lambda bm, xx=xx: tube(bm, [Vector((0, 0, H + 4.0)), Vector((xx, 0, H + 1.4))], 0.025, 0.025, 4), 'black')
    k.add(lambda bm: tube(bm, [Vector((14, 0, H)), Vector((14, 0, 6.0))], 0.02, 0.02, 4), 'black')    # hook rope
    k.add(lambda bm: rbox(bm, (0.5, 0.4, 0.6), (14, 0, 5.4), 0.05), 'black')
    return k.obj('TowerCrane')

def build_chimney(seed=1591):
    """a tall brick factory chimney, tapering, with iron bands and a corbelled top (no smoke)"""
    k = Kit(['terracotta', 'wood_dark', 'cream', 'black', 'stone_light', 'stone'])
    k.add(lambda bm: rbox(bm, (3.2, 3.2, 2.4), (0, 0, 0), 0.06), brick)
    k.add(lambda bm: lathe(bm, [(1.35, 2.4), (0.85, 20.0), (0.0, 20.0)], 12), brick)
    for z in (6.0, 10.0, 14.0, 18.0):
        r = 1.35 - (z - 2.4) / 17.6 * 0.5 + 0.03
        k.add(lambda bm, z=z, r=r: lathe(bm, [(r, z), (r, z + 0.18), (0.0, z + 0.18)], 12), 'black')
    k.add(lambda bm: lathe(bm, [(0.86, 19.6), (1.05, 20.0), (1.05, 20.6), (0.70, 20.6), (0.70, 20.3), (0.0, 20.3)], 12), brick)
    k.add(lambda bm: lathe(bm, [(1.06, 20.1), (1.06, 20.25), (0.0, 20.25)], 12), 'cream')
    return k.obj('Chimney')

def build_watertower(seed=1601):
    """a riveted steel water tank on four lattice legs, a ladder up the side"""
    k = Kit(['enamel', 'black', 'stone_dark', 'metal', 'stone_light', 'stone'])
    tc = 'enamel'   # one colour all round (a baked dark side turned the tank black)
    for (x, y) in [(-1.6, -1.6), (1.6, -1.6), (1.6, 1.6), (-1.6, 1.6)]:
        k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, 0)), Vector((x * 0.75, y * 0.75, 9.0))], 0.10, 0.08, 6), 'stone_dark')
        k.add(lambda bm, x=x, y=y: rbox(bm, (0.6, 0.6, 0.5), (x, y, 0), 0.04), conc)
    for z in (3.0, 6.0):
        for i, ((x0, y0), (x1, y1)) in enumerate([((-1, -1), (1, -1)), ((1, -1), (1, 1)), ((1, 1), (-1, 1)), ((-1, 1), (-1, -1))]):
            s = 1.6 - 0.4 * z / 9
            k.add(lambda bm, x0=x0, y0=y0, x1=x1, y1=y1, z=z, s=s: tube(bm, [Vector((x0 * s, y0 * s, z)), Vector((x1 * s, y1 * s, z))], 0.05, 0.05, 4), 'stone_dark')
    k.add(lambda bm: lathe(bm, [(0.0, 8.8), (2.1, 9.0), (2.2, 9.2), (2.2, 12.0), (1.4, 12.8), (0.0, 13.0)], 16), tc)
    for z in (9.9, 11.0): k.add(lambda bm, z=z: lathe(bm, [(2.22, z), (2.22, z + 0.1), (0.0, z + 0.1)], 16), 'black')
    for x in (-0.2, 0.2): k.add(lambda bm, x=x: tube(bm, [Vector((x, -2.3, 9.0)), Vector((x, -2.3, 12.0))], 0.03, 0.03, 4), 'black')
    return k.obj('WaterTower')

def build_factoryshed(seed=1611):
    """a brick workshop with a sawtooth north-light roof, tall iron-framed windows and a big timber door"""
    k = Kit(['terracotta', 'wood_dark', 'cream', 'stone_light', 'stone', 'stone_dark', 'black', 'metal', 'wood', 'lamp_warm'])
    W, D, H = 14.0, 8.0, 5.0
    k.add(lambda bm: rbox(bm, (W, D, H), (0, 0, 0), 0.08), brick)
    for i in range(4):   # sawtooth roof: slope + glazed vertical
        x0 = -W / 2 + i * W / 4; x1 = x0 + W / 4
        def tooth(bm, x0=x0, x1=x1):
            a = [bm.verts.new((x0, y, H)) for y in (-D / 2, D / 2)]; b = [bm.verts.new((x1, y, H)) for y in (-D / 2, D / 2)]
            t = [bm.verts.new((x1, y, H + 1.8)) for y in (-D / 2, D / 2)]
            bm.faces.new((a[0], b[0], t[0])); bm.faces.new((a[1], t[1], b[1]))
            bm.faces.new((a[0], t[0], t[1], a[1]))
            bm.faces.new((b[0], b[1], t[1], t[0]))
        k.add(tooth, lambda f: 'stone_dark' if f.normal.x > 0.5 else ('metal' if f.normal.z > 0.2 else 'terracotta'))
    for i in range(5):   # windows on the long front
        x = -W / 2 + 1.6 + i * 2.7
        k.add(lambda bm, x=x: rbox(bm, (1.4, 0.06, 2.4), (x, -D / 2 - 0.02, 1.4), 0.02, segs=1), 'stone_dark')
        k.add(lambda bm, x=x: rbox(bm, (1.5, 0.08, 0.12), (x, -D / 2 - 0.03, 3.8), 0.02, segs=1), 'cream')
    k.add(lambda bm: rbox(bm, (2.6, 0.1, 3.4), (W / 2 - 2.2, -D / 2 - 0.04, 0), 0.03), woodc)
    return k.obj('FactoryShed')

# ------------------------------------------------------------------ border and gate
def window(k, x, y, z, w, h):
    """a tall iron-framed factory window set in brickwork: dark glass, cream sill and lintel, iron glazing bars"""
    k.add(lambda bm: pbox(bm, (w, 0.06, h), (x, y - 0.01, z)), 'stone_dark')
    for i in range(1, 3): k.add(lambda bm, i=i: pbox(bm, (0.04, 0.05, h), (x - w / 2 + i * w / 3, y - 0.03, z)), 'black')
    for j in range(1, 4): k.add(lambda bm, j=j: pbox(bm, (w, 0.05, 0.04), (x, y - 0.03, z + j * h / 4)), 'black')
    k.add(lambda bm: rbox(bm, (w + 0.16, 0.16, 0.10), (x, y - 0.06, z - 0.10), 0.02, segs=1), 'cream')
    k.add(lambda bm: rbox(bm, (w + 0.20, 0.10, 0.16), (x, y - 0.03, z + h), 0.02, segs=1), 'cream')

def build_factorywall(kind, seed, name):
    """a border wall of the works in place of the old border cliffs (7.6 m long, 5 m deep, ~5.4 m tall, stretched by the
    builder to the old cliff's top). Front = -Y (+Z in Unity), turned to the arena. 'A' a brick workshop wall with
    pilasters and tall windows, 'B' a corrugated-iron shed on a brick plinth with a big timber door, 'C' a brick wall
    with pipework and a riveted tank against it. A pitched roof behind closes the view."""
    rnd = random.Random(seed)
    k = Kit(['terracotta', 'wood_dark', 'bone_dark', 'cream', 'stone_light', 'stone', 'stone_dark', 'black', 'metal', 'wood', 'khaki', 'olive'])
    L, D, H = 7.6, 5.0, 5.4
    front = -D / 2 + 0.6
    # the body and a pitched corrugated roof behind (nothing can be seen through the border)
    k.add(lambda bm: rbox(bm, (L, D - 1.0, H - 0.2), (0, 0.5, 0), 0.05, segs=1), brick)
    def roof(bm):
        a = [bm.verts.new((x, front + 0.2, H)) for x in (-L / 2, L / 2)]; b = [bm.verts.new((x, D / 2, H)) for x in (-L / 2, L / 2)]
        r = [bm.verts.new((x, (front + D / 2) / 2 + 0.3, H + 1.6)) for x in (-L / 2, L / 2)]
        bm.faces.new((a[0], a[1], r[1], r[0])); bm.faces.new((r[0], r[1], b[1], b[0]))
        bm.faces.new((a[0], r[0], b[0])); bm.faces.new((a[1], b[1], r[1]))
    k.add(roof, lambda f: 'metal' if f.normal.z > 0.3 else 'terracotta')
    if kind in ('A', 'C'):
        bricks_block(k, L, 0.6, 0.0, H, (0, front + 0.3), rnd=rnd, CH=0.40, BL=0.80, sides=(-1,))
        k.add(lambda bm: rbox(bm, (L + 0.1, 0.75, 0.22), (0, front + 0.3, H), 0.03, segs=1), conc)             # coping
        k.add(lambda bm: pbox(bm, (L + 0.02, 0.66, 0.14), (0, front + 0.3, 3.1)), 'cream')                       # string course
        for x in (-L / 2 + 0.25, -L / 6, L / 6, L / 2 - 0.25):                                                    # pilasters
            k.add(lambda bm, x=x: rbox(bm, (0.5, 0.28, H), (x, front - 0.1, 0), 0.03, segs=1), brick)
        if kind == 'A':
            for x in (-L / 3, 0.0, L / 3): window(k, x, front, 1.2, 1.5, 1.7)
            for x in (-L / 3, 0.0, L / 3): window(k, x, front, 3.5, 1.5, 1.0)
        else:
            window(k, -L / 3, front, 3.5, 1.5, 1.0); window(k, L / 3, front, 3.5, 1.5, 1.0)
            for (z, r) in [(2.6, 0.16), (2.2, 0.12)]:   # pipework along the face, on brackets
                k.add(lambda bm, z=z, r=r: tube(bm, [Vector((-L / 2, front - 0.35, z)), Vector((L / 2, front - 0.35, z))], r, r, 8), metalc)
                for x in (-2.6, 0.0, 2.6): k.add(lambda bm, x=x, z=z: pbox(bm, (0.08, 0.36, 0.06), (x, front - 0.18, z - 0.2)), 'black')
            k.add(lambda bm: tube(bm, [Vector((1.6, front - 0.35, 2.6)), Vector((1.6, front - 0.35, 0.4)), Vector((1.9, front - 0.6, 0.15))], 0.14, 0.14, 8), metalc)
            def tank(bm):   # a riveted tank lying against the wall
                M = Matrix.Translation((-1.6, front - 0.75, 0.7)) @ Matrix.Rotation(math.pi / 2, 4, 'Y') @ Matrix.Translation((0, 0, -1.1))
                lathe(bm, [(0.0, 0.0), (0.55, 0.12), (0.62, 0.3), (0.62, 1.9), (0.55, 2.08), (0.0, 2.2)], 12, None, 0, M)
            k.add(tank, metalc)
            for x in (-2.4, -0.8): k.add(lambda bm, x=x: rbox(bm, (0.16, 1.1, 0.45), (x, front - 0.7, 0), 0.02, segs=1), conc)   # saddles
    else:
        k.add(lambda bm: rbox(bm, (L, 0.6, 0.9), (0, front + 0.3, 0), 0.03, segs=1), brick)                     # brick plinth
        def corrugated(bm, x0, x1, z0, z1, y):
            n = int((x1 - x0) / 0.12)
            bot = []; top = []
            for i in range(n + 1):
                x = x0 + (x1 - x0) * i / n; yy = y + (0.04 if i % 2 else -0.04)
                bot.append(bm.verts.new((x, yy, z0))); top.append(bm.verts.new((x, yy, z1)))
            for i in range(n): bm.faces.new((bot[i], bot[i + 1], top[i + 1], top[i]))
        k.add(lambda bm: corrugated(bm, -L / 2, L / 2, 0.9, H, front + 0.1), lambda f: 'metal' if f.normal.x < 0.4 else 'stone')
        k.add(lambda bm: rbox(bm, (L, 0.5, 0.2), (0, front + 0.3, H - 0.1), 0.02, segs=1), 'stone_dark')       # eaves
        k.add(lambda bm: rbox(bm, (2.8, 0.12, 3.4), (0.8, front - 0.02, 0), 0.03), woodc)                       # sliding door
        for z in (0.6, 1.7, 2.8): k.add(lambda bm, z=z: pbox(bm, (2.8, 0.05, 0.10), (0.8, front - 0.10, z)), 'wood_dark')
        k.add(lambda bm: tube(bm, [Vector((-0.5, front - 0.12, 0.3)), Vector((2.1, front - 0.12, 3.1))], 0.04, 0.04, 5), 'wood_dark')   # brace
        k.add(lambda bm: pbox(bm, (3.4, 0.10, 0.10), (0.8, front - 0.06, 3.45)), 'black')                        # door rail
        k.add(lambda bm: tube(bm, [Vector((-L / 2 + 0.3, front - 0.15, H)), Vector((-L / 2 + 0.3, front - 0.15, 0.1))], 0.06, 0.06, 6), 'stone_dark')   # downpipe
    for j in range(3):   # a few bricks at the foot
        x = rnd.uniform(-L / 2 + 0.6, L / 2 - 0.6)
        k.add(lambda bm, x=x: rbox(bm, (0.24, 0.12, 0.07), (x, front - 0.6, 0), 0.01, rnd.uniform(0, 3)), brick)
    k.add(lambda bm: blob(bm, 1.0, (0, front - 0.6, 0), (L * 0.48, 0.8, 0.10), 3, 0.02, seed), lambda f: 'khaki' if f.normal.z > 0.4 else 'olive')
    return k.obj(name)

def build_factorygate(seed=1621):
    """the works entrance behind each spawn (in place of the base scene's gate arch, 10.8 x 9.6 m): two brick piers with
    stone caps and lamps, a riveted red-oxide girder across them, wrought-iron gates folded back against the piers"""
    rnd = random.Random(seed)
    k = Kit(['terracotta', 'wood_dark', 'bone_dark', 'cream', 'stone_light', 'stone', 'red', 'xmas_red_dark', 'black', 'stone_dark', 'enamel', 'lamp_warm', 'khaki', 'olive'])
    X = 4.1
    for s in (-1, 1):
        bricks_block(k, 1.3, 1.3, 0.0, 6.4, (s * X, 0), rnd=rnd, CH=0.40, BL=0.65)
        k.add(lambda bm, s=s: rbox(bm, (1.55, 1.55, 0.30), (s * X, 0, 6.4), 0.04), conc)
        k.add(lambda bm, s=s: lathe(bm, [(0.28, 6.7), (0.22, 6.85), (0.0, 6.9)], 8, None, 0, Matrix.Translation((s * X, 0, 0))), conc)
        k.add(lambda bm, s=s: tube(bm, [Vector((s * X, -0.66, 4.6)), Vector((s * X, -1.0, 4.8)), Vector((s * X, -1.15, 4.65))], 0.03, 0.03, 5), 'black')   # lamp bracket
        k.add(lambda bm, s=s: lathe(bm, [(0.26, 0.0), (0.13, 0.12), (0.0, 0.16)], 10, None, 0, Matrix.Translation((s * X, -1.15, 4.45))), paint('enamel', 'black'))
        k.add(lambda bm, s=s: sphere(bm, 0.07, (s * X, -1.15, 4.42), sub=1), 'lamp_warm', glow=True)
        x0 = s * (X - 0.66)   # the wrought-iron gate leaf, swung open against the inside of the pier
        for i in range(9):
            y = -0.55 - i * 0.24
            k.add(lambda bm, x0=x0, y=y, i=i: tube(bm, [Vector((x0, y, 0.1)), Vector((x0, y, 2.6 + 0.25 * math.sin(i / 8 * math.pi)))], 0.025, 0.025, 4), 'black')
        for z in (0.4, 1.4, 2.4): k.add(lambda bm, x0=x0, z=z: tube(bm, [Vector((x0, -0.55, z)), Vector((x0, -2.47, z))], 0.03, 0.03, 4), 'black')
    ibeam(k, 0.0, 0.0, 5.6, 2 * X + 1.0, 0.70, 0.45, 0.0, t=0.08)   # the girder across the top, riveted
    for i in range(15):
        x = -X + 0.3 + i * (2 * X - 0.6) / 14
        k.add(lambda bm, x=x: sphere(bm, 0.035, (x, -0.06, 6.22), sub=1), 'xmas_red_dark')
    k.add(lambda bm: blob(bm, 1.0, (0, -1.0, 0), (5.4, 1.4, 0.10), 3, 0.02, seed), lambda f: 'khaki' if f.normal.z > 0.4 else 'olive')
    return k.obj('FactoryGate')
