"""Lunar theme (1960s-70s Space Age, retro-futurist): a moon base on the grey regolith. Original designs in the spirit of
the era (no copies of real or fictional craft): white riveted domes with portholes, gold-foil landers, finned rockets,
dish antennas, fuel tanks, cargo canisters, a little buggy, grey cratered boulders, and strange alien fronds as the
hiding bushes. Same palette and chunky style as the other themes.

Walls ('#'): MoonBoulderA/B, CargoStack, FuelTanks.
Small ('x'): MoonBuggy, SupplyPods, RockSampler.
Fence ('f'): RockRidge.
Tall ('t'): RadioDish, MiniRocket, FloodMast, Beacon.
Bush ('b'): StarGrass (alien fronds: grass.build_frostgrass with the frost ramp).
Decor: SmallCrater, MoonPebbles, Footprints.
Border: CraterWallA/B/C. Gate: AirlockGate.
Backdrop: MoonDome, Lander, LaunchRocket, Earth."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import *
from props3 import blob
from props7 import tube
from props8 import Kit, rbox, sphere
import grass

def rock(f):
    """moon rock: pale grey tops, grey sides, darker in the shade (same any way it is turned: the tops carry the light)"""
    n = f.normal
    if n.z > 0.6: return 'ash'                     # neutral pale grey (the stone_* cells are bluish slate)
    return 'metal' if n.z > -0.3 else 'stone'
def whitec(f): return 'snow' if lit(f.normal) > -0.2 else 'snow_shade'
def paint(col, dark): return lambda f: col if lit(f.normal) > -0.1 else dark

def pbox(bm, size, loc, yaw=0.0):
    sx, sy, sz = size
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((loc[0], loc[1], loc[2] + sz / 2)) @ Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Diagonal((sx, sy, sz, 1)))

def dust(k, sx, sy, seed, h=0.06):
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (sx, sy, h), 3, 0.02, seed), lambda f: 'ash' if f.normal.z > 0.4 else 'metal')

def crater_pits(k, rnd, R, z, n=4, seed=0):
    """little round pits on a rock surface: dark discs sunk a hair into the face (reads as craters from above)"""
    for j in range(n):
        a = rnd.uniform(0, 2 * math.pi); rr = rnd.uniform(0.2, R * 0.8); s = rnd.uniform(0.06, 0.14)
        k.add(lambda bm, a=a, rr=rr, s=s: lathe(bm, [(s, 0.0), (0.0, -0.01)], 8, None, 0, Matrix.Translation((math.cos(a) * rr, math.sin(a) * rr, z))), 'stone_dark')

def porthole(k, x, y, z, r, axis='Y'):
    M = Matrix.Translation((x, y, z)) @ (Matrix.Rotation(math.pi / 2, 4, 'X') if axis == 'Y' else Matrix.Rotation(math.pi / 2, 4, 'Y'))
    k.add(lambda bm: lathe(bm, [(r, -0.03), (r * 1.25, -0.03), (r * 1.25, 0.03), (r, 0.03)], 12, None, 0, M), 'metal')
    k.add(lambda bm: lathe(bm, [(r, 0.0), (0.0, 0.0)], 12, None, 0, M), 'ice_deep')

# ------------------------------------------------------------------ walls
def build_moonboulder(kind, seed, name):
    """a big grey moon boulder, lumpy and faceted, a few small craters pocking it, regolith heaped at its foot"""
    rnd = random.Random(seed)
    k = Kit(['ash', 'metal', 'stone'])
    if kind == 'A':
        k.add(lambda bm: blob(bm, 1.0, (0, 0, 0.75), (0.70, 0.65, 0.85), 2, 0.12, seed), rock)
        k.add(lambda bm: blob(bm, 1.0, (0.35, -0.30, 0.35), (0.38, 0.34, 0.38), 1, 0.06, seed + 1), rock)
    else:
        k.add(lambda bm: blob(bm, 1.0, (-0.25, 0.05, 0.55), (0.52, 0.55, 0.62), 2, 0.10, seed), rock)
        k.add(lambda bm: blob(bm, 1.0, (0.32, 0.0, 0.70), (0.42, 0.50, 0.80), 2, 0.10, seed + 2), rock)
    # (no crater pits: placed at a fixed height they floated over the rock)
    dust(k, 0.85, 0.8, seed)
    return k.obj(name)

def build_cargostack(seed=1911):
    """supply canisters of the base, stacked in a frame: white drums with orange bands and stencilled stripes"""
    rnd = random.Random(seed)
    k = Kit(['snow', 'snow_shade', 'orange', 'metal', 'stone_dark', 'black', 'stone_light', 'stone'])
    for x in (-0.62, 0.62):
        for y in (-0.55, 0.55): k.add(lambda bm, x=x, y=y: pbox(bm, (0.08, 0.08, 1.75), (x, y, 0)), 'metal')
    for z in (0.02, 0.86, 1.70): k.add(lambda bm, z=z: pbox(bm, (1.32, 1.18, 0.06), (0, 0, z)), 'metal')
    for z in (0.08, 0.92):
        for y in (-0.28, 0.28):
            M = Matrix.Translation((0, y, z + 0.38)) @ Matrix.Rotation(math.pi / 2, 4, 'Y') @ Matrix.Translation((0, 0, -0.6))
            k.add(lambda bm, M=M: lathe(bm, [(0.0, 0.0), (0.30, 0.03), (0.34, 0.12), (0.34, 1.08), (0.30, 1.17), (0.0, 1.2)], 12, None, 0, M), whitec)
            k.add(lambda bm, M=M: lathe(bm, [(0.345, 0.25), (0.345, 0.38), (0.0, 0.38)], 12, None, 0, M), 'orange')
            k.add(lambda bm, M=M: lathe(bm, [(0.345, 0.82), (0.345, 0.95), (0.0, 0.95)], 12, None, 0, M), 'orange')
    dust(k, 0.85, 0.8, seed)
    return k.obj('CargoStack')

def build_fueltanks(seed=1921):
    """three tall fuel tanks on a skid: white with gold-foil bands, piping and valves between them"""
    k = Kit(['snow', 'snow_shade', 'gold', 'gold_dark', 'metal', 'stone_dark', 'red', 'stone_light', 'stone'])
    k.add(lambda bm: rbox(bm, (1.40, 1.30, 0.18), (0, 0, 0), 0.03), 'stone_dark')
    for (x, y, r, h) in [(-0.33, -0.28, 0.32, 1.9), (0.33, -0.25, 0.30, 1.7), (0.0, 0.32, 0.30, 2.1)]:
        M = Matrix.Translation((x, y, 0.18))
        k.add(lambda bm, M=M, r=r, h=h: lathe(bm, [(0.0, 0.0), (r, 0.05), (r, h - r * 0.6), (r * 0.6, h - 0.1), (0.0, h)], 12, None, 0, M), whitec)
        k.add(lambda bm, M=M, r=r: lathe(bm, [(r + 0.01, 0.5), (r + 0.01, 0.75), (0.0, 0.75)], 12, None, 0, M), paint('gold', 'gold_dark'))
        k.add(lambda bm, x=x, y=y, h=h: tube(bm, [Vector((x, y, h + 0.15)), Vector((x, y, h + 0.35)), Vector((0, 0, h + 0.35))], 0.03, 0.03, 5), 'metal')
    k.add(lambda bm: lathe(bm, [(0.08, 0.0), (0.08, 0.03), (0.0, 0.03)], 8, None, 0, Matrix.Translation((0.0, -0.05, 1.0)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), 'red')
    dust(k, 0.85, 0.8, seed)
    return k.obj('FuelTanks')

# ------------------------------------------------------------------ small
def build_moonbuggy(seed=1931):
    """a little two-seat moon buggy: an open tube frame, four wire-mesh wheels, a dish antenna and a camera mast"""
    k = Kit(['metal', 'stone_dark', 'gold', 'gold_dark', 'snow', 'black', 'stone_light', 'stone'])
    for x in (-0.48, 0.48):
        for y in (-0.48, 0.48):
            M = Matrix.Translation((x, y, 0.24)) @ Matrix.Rotation(math.pi / 2, 4, 'X') @ Matrix.Translation((0, 0, -0.06))
            k.add(lambda bm, M=M: lathe(bm, [(0.16, 0.0), (0.24, 0.0), (0.24, 0.12), (0.16, 0.12)], 12, None, 0, M), 'stone_dark')
            k.add(lambda bm, M=M: lathe(bm, [(0.06, -0.01), (0.06, 0.13), (0.0, 0.13)], 6, None, 0, M), 'gold')
    for y in (-0.48, 0.48): k.add(lambda bm, y=y: tube(bm, [Vector((-0.60, y, 0.30)), Vector((0.60, y, 0.30))], 0.03, 0.03, 5), 'metal')
    k.add(lambda bm: rbox(bm, (1.10, 0.80, 0.08), (0, 0, 0.30), 0.02), paint('gold', 'gold_dark'))            # floor pan (foil)
    for x in (-0.15, 0.25):   # seats
        k.add(lambda bm, x=x: rbox(bm, (0.30, 0.30, 0.06), (x, 0.0, 0.40), 0.02), 'snow')
        k.add(lambda bm, x=x: rbox(bm, (0.06, 0.30, 0.32), (x - 0.15, 0.0, 0.40), 0.02), 'snow')
    k.add(lambda bm: tube(bm, [Vector((0.50, 0.25, 0.38)), Vector((0.50, 0.25, 1.05))], 0.02, 0.02, 4), 'metal')
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.25, 0.10), (0.27, 0.12)], 10, None, 0, Matrix.Translation((0.50, 0.25, 1.05)) @ Matrix.Rotation(0.5, 4, 'Y')), 'snow')   # dish
    k.add(lambda bm: rbox(bm, (0.14, 0.12, 0.10), (-0.50, -0.25, 0.70), 0.02), 'black')
    k.add(lambda bm: tube(bm, [Vector((-0.50, -0.25, 0.38)), Vector((-0.50, -0.25, 0.70))], 0.02, 0.02, 4), 'metal')
    dust(k, 0.75, 0.7, seed, h=0.05)
    return k.obj('MoonBuggy')

def build_supplypods(seed=1941):
    """two round supply pods on short legs and a hard-shell case, dropped from orbit"""
    k = Kit(['snow', 'snow_shade', 'orange', 'metal', 'stone_dark', 'black', 'stone_light', 'stone'])
    for (x, y, r) in [(-0.30, -0.15, 0.42), (0.38, 0.25, 0.32)]:
        for a in range(3):
            ang = a * 2 * math.pi / 3
            k.add(lambda bm, x=x, y=y, r=r, ang=ang: tube(bm, [Vector((x, y, r * 0.9)), Vector((x + math.cos(ang) * r * 1.1, y + math.sin(ang) * r * 1.1, 0.0))], 0.03, 0.025, 4), 'metal')
        k.add(lambda bm, x=x, y=y, r=r: sphere(bm, r, (x, y, r * 1.05), (1, 1, 0.9), 2), whitec)
        k.add(lambda bm, x=x, y=y, r=r: lathe(bm, [(r * 1.01, r * 0.98), (r * 1.01, r * 1.12), (0.0, r * 1.12)], 14, None, 0, Matrix.Translation((x, y, 0))), 'orange')
    k.add(lambda bm: rbox(bm, (0.55, 0.40, 0.32), (0.25, -0.45, 0.0), 0.04, 0.4), paint('orange', 'stone_dark'))
    dust(k, 0.7, 0.65, seed, h=0.05)
    return k.obj('SupplyPods')

def build_rocksampler(seed=1951):
    """a geology station: a tripod drill rig over a core hole, a rack of sample tubes, a few collected rocks"""
    rnd = random.Random(seed)
    k = Kit(['metal', 'stone_dark', 'gold', 'snow', 'orange', 'black', 'stone_light', 'stone'])
    for a in range(3):
        ang = a * 2 * math.pi / 3 + 0.3
        k.add(lambda bm, ang=ang: tube(bm, [Vector((0, 0, 1.30)), Vector((math.cos(ang) * 0.55, math.sin(ang) * 0.55, 0.0))], 0.03, 0.03, 5), 'metal')
    k.add(lambda bm: tube(bm, [Vector((0, 0, 1.35)), Vector((0, 0, 0.05))], 0.04, 0.04, 6), 'gold')
    k.add(lambda bm: rbox(bm, (0.24, 0.24, 0.22), (0, 0, 1.05), 0.03), 'orange')
    k.add(lambda bm: rbox(bm, (0.50, 0.18, 0.40), (0.30, -0.45, 0.0), 0.03), 'snow')
    for i in range(5): k.add(lambda bm, i=i: tube(bm, [Vector((0.12 + i * 0.09, -0.45, 0.40)), Vector((0.12 + i * 0.09, -0.45, 0.62))], 0.03, 0.03, 6), 'metal')
    for j in range(3): k.add(lambda bm, j=j: blob(bm, 1.0, (-0.45 + j * 0.15, 0.40, 0), (0.12, 0.10, 0.10), 1, 0.03, seed + j), rock)
    dust(k, 0.75, 0.7, seed, h=0.05)
    return k.obj('RockSampler')

# ------------------------------------------------------------------ fence
def build_rockridge(seed=1961):
    """a low ridge of regolith and rubble across 1.5 m: crater ejecta (low cover)"""
    rnd = random.Random(seed)
    k = Kit(['ash', 'metal', 'stone'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.78, 0.36, 0.55), 2, 0.06, seed), rock)
    for j in range(4):
        x = -0.6 + j * 0.4
        k.add(lambda bm, x=x, j=j: blob(bm, 1.0, (x, rnd.uniform(-0.15, 0.15), 0.35), (0.22, 0.2, 0.22), 1, 0.05, seed + j), rock)
    return k.obj('RockRidge')

# ------------------------------------------------------------------ tall
def build_radiodish(seed=1971):
    """a dish antenna on a lattice mast: a white parabolic dish tilted at the sky, a feed horn on struts"""
    k = Kit(['snow', 'snow_shade', 'metal', 'stone_dark', 'red', 'lamp_warm', 'stone_light', 'stone'])
    k.add(lambda bm: rbox(bm, (0.80, 0.80, 0.25), (0, 0, 0), 0.04), 'stone_dark')
    for (x, y) in [(-0.25, -0.25), (0.25, -0.25), (0.25, 0.25), (-0.25, 0.25)]:
        k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, 0.25)), Vector((x * 0.3, y * 0.3, 2.2))], 0.03, 0.03, 4), 'metal')
    for z in (0.8, 1.5): k.add(lambda bm, z=z: lathe(bm, [(0.25 - 0.18 * z / 2.2 + 0.01, z), (0.25 - 0.18 * z / 2.2 + 0.01, z + 0.04), (0.0, z + 0.04)], 4, None, 0, Matrix.Rotation(math.pi / 4, 4, 'Z')), 'metal')
    M = Matrix.Translation((0, 0, 2.35)) @ Matrix.Rotation(-0.7, 4, 'X')
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.45, 0.10), (0.85, 0.36), (0.88, 0.40)], 16, None, 0, M), whitec)
    k.add(lambda bm: lathe(bm, [(0.86, 0.39), (0.42, 0.13), (0.0, 0.03)], 16, None, 0, M), 'snow_shade')   # inside of the dish
    for a in range(3):
        ang = a * 2 * math.pi / 3
        p0 = M @ Vector((math.cos(ang) * 0.8, math.sin(ang) * 0.8, 0.36)); p1 = M @ Vector((0, 0, 0.95))
        k.add(lambda bm, p0=p0, p1=p1: tube(bm, [p0, p1], 0.015, 0.015, 4), 'metal')
    p = M @ Vector((0, 0, 0.95)); k.add(lambda bm, p=p: sphere(bm, 0.07, (p.x, p.y, p.z), sub=1), 'red')
    return k.obj('RadioDish')

def build_minirocket(seed=1981):
    """a small sounding rocket on its pad: a slim white body with a red nose and orange fins, a gantry arm"""
    k = Kit(['snow', 'snow_shade', 'red', 'xmas_red_dark', 'orange', 'metal', 'stone_dark', 'black', 'stone_light', 'stone'])
    k.add(lambda bm: rbox(bm, (1.10, 1.10, 0.20), (0, 0, 0), 0.04), 'stone_dark')
    k.add(lambda bm: lathe(bm, [(0.0, 0.25), (0.22, 0.30), (0.24, 0.6), (0.24, 2.4), (0.18, 2.9), (0.07, 3.3), (0.0, 3.4)], 12), lambda f: 'red' if f.calc_center_median().z > 2.45 else whitec(f))
    k.add(lambda bm: lathe(bm, [(0.245, 1.4), (0.245, 1.55), (0.0, 1.55)], 12), 'black')
    for a in range(4):
        ang = a * math.pi / 2
        d = Vector((math.cos(ang), math.sin(ang), 0))
        def fin(bm, d=d):
            p = [Vector((0, 0, 0.25)), Vector((0, 0, 1.0)), Vector((0, 0, 0.25))]
            v = [bm.verts.new(d * 0.22 + Vector((0, 0, 0.30))), bm.verts.new(d * 0.22 + Vector((0, 0, 1.10))), bm.verts.new(d * 0.55 + Vector((0, 0, 0.20)))]
            side = Vector((-d.y, d.x, 0)) * 0.025
            a1 = [bm.verts.new(x.co + side) for x in v]; a2 = [bm.verts.new(x.co - side) for x in v]
            for x in v: bm.verts.remove(x)
            bm.faces.new(a1); bm.faces.new(list(reversed(a2)))
            for i in range(3):
                j = (i + 1) % 3
                bm.faces.new((a1[i], a2[i], a2[j], a1[j]))
        k.add(fin, 'orange')
    k.add(lambda bm: tube(bm, [Vector((0.55, 0.0, 0.2)), Vector((0.55, 0.0, 2.6))], 0.04, 0.04, 5), 'metal')
    k.add(lambda bm: tube(bm, [Vector((0.55, 0.0, 2.2)), Vector((0.25, 0.0, 2.2))], 0.03, 0.03, 5), 'metal')
    return k.obj('MiniRocket')

def build_floodmast(seed=1991):
    """a floodlight mast for the base: a slim pole with a crossbar of four incandescent lamps in reflectors"""
    k = Kit(['metal', 'stone_dark', 'snow', 'lamp_warm', 'black', 'stone_light', 'stone'])
    k.add(lambda bm: rbox(bm, (0.50, 0.50, 0.20), (0, 0, 0), 0.03), 'stone_dark')
    k.add(lambda bm: tube(bm, [Vector((0, 0, 0.2)), Vector((0, 0, 3.1))], 0.07, 0.05, 6), 'metal')
    k.add(lambda bm: rbox(bm, (1.00, 0.08, 0.08), (0, 0, 3.05), 0.01), 'stone_dark')
    for x in (-0.42, -0.14, 0.14, 0.42):
        M = Matrix.Translation((x, -0.06, 3.0)) @ Matrix.Rotation(1.9, 4, 'X')
        k.add(lambda bm, M=M: lathe(bm, [(0.0, 0.0), (0.10, 0.04), (0.13, 0.12)], 8, None, 0, M), 'snow')
        p = M @ Vector((0, 0, 0.08)); k.add(lambda bm, p=p: sphere(bm, 0.07, (p.x, p.y, p.z), sub=1), 'lamp_warm', glow=True)
    return k.obj('FloodMast')

def build_beacon(seed=2001):
    """a landing beacon: a striped orange-and-white post with a red glass lamp on top"""
    k = Kit(['orange', 'snow', 'red', 'metal', 'stone_dark', 'stone_light', 'stone'])
    k.add(lambda bm: lathe(bm, [(0.26, 0.0), (0.26, 0.10), (0.10, 0.18), (0.08, 2.4), (0.0, 2.4)], 10), lambda f: 'orange' if int(f.calc_center_median().z / 0.4) % 2 == 0 else 'snow')
    k.add(lambda bm: lathe(bm, [(0.13, 2.4), (0.13, 2.46), (0.0, 2.46)], 10), 'metal')
    k.add(lambda bm: sphere(bm, 0.14, (0, 0, 2.6), (1, 1, 1.2), 2), 'red', glow=True)
    k.add(lambda bm: lathe(bm, [(0.06, 2.74), (0.06, 2.80), (0.0, 2.80)], 8), 'metal')
    dust(k, 0.45, 0.45, seed, h=0.05)
    return k.obj('Beacon')

def build_stargrass():
    """alien fronds grown in a crater's shade (the hiding bush): the Brawl leaf clump with the blue-lavender frost ramp"""
    return grass.build_frostgrass(181, 'StarGrass', 'frost_grad', 0.9)

# ------------------------------------------------------------------ decor
def build_smallcrater(seed=2011):
    """decor: a small impact crater: a raised rim round a shallow dark bowl"""
    k = Kit(['ash', 'metal', 'stone'])
    k.add(lambda bm: lathe(bm, [(0.95, 0.0), (0.75, 0.10), (0.62, 0.12), (0.50, 0.03)], 16), rock)
    k.add(lambda bm: add_cone(bm, 16, 0.52, 0.52, 0.04, Matrix.Translation((0, 0, 0.02))), 'stone')   # the bowl (a closed disc)
    return k.obj('SmallCrater')

def build_moonpebbles(seed=2021):
    """decor: a scatter of grey stones"""
    rnd = random.Random(seed)
    k = Kit(['ash', 'metal', 'stone'])
    for j in range(6):
        x, y = rnd.uniform(-0.55, 0.55), rnd.uniform(-0.45, 0.45); s = rnd.uniform(0.08, 0.2)
        k.add(lambda bm, x=x, y=y, s=s, j=j: blob(bm, 1.0, (x, y, 0), (s * 1.2, s, s * 0.8), 1, s * 0.25, seed + j), rock)
    return k.obj('MoonPebbles')

def build_footprints(seed=2031):
    """decor: a trail of boot prints pressed into the dust"""
    k = Kit(['stone_dark', 'stone'])
    for i in range(6):
        x = -0.8 + i * 0.32; y = 0.12 if i % 2 else -0.12
        k.add(lambda bm, x=x, y=y: rbox(bm, (0.26, 0.11, 0.012), (x, y, 0.0), 0.04, 0.0, segs=1), 'stone_dark')
    return k.obj('Footprints')

# ------------------------------------------------------------------ border and gate
def build_craterwall(kind, seed, name):
    """a border piece in place of the old border cliffs (7.6 m long, 5 m deep, ~5 m tall): the rugged rim of a big crater,
    grey rock in rounded masses, pocked with small craters. Front = -Y (+Z in Unity). 'A' plain rim, 'B' with a base
    outpost (a dome half set into the rock, a dish), 'C' with a lander's landing pad and beacons on a ledge."""
    rnd = random.Random(seed)
    k = Kit(['stone_light', 'stone', 'stone_dark', 'snow', 'snow_shade', 'metal', 'ice_deep', 'gold', 'gold_dark', 'red', 'orange', 'lamp_warm'])
    L, D, H = 7.6, 5.0, 4.8
    k.add(lambda bm: rbox(bm, (L, D * 0.6, H * 0.85), (0, D * 0.15, 0), 0.4, segs=2), rock)                   # solid core
    for j in range(7):   # rounded rock masses along the face
        x = -L / 2 + 0.5 + j * (L - 1.0) / 6 + rnd.uniform(-0.2, 0.2)
        s = rnd.uniform(0.9, 1.3); h = rnd.uniform(H * 0.55, H * 1.0)
        if kind == 'B' and abs(x - 1.2) < 1.5: continue   # leave the outpost dome in view
        k.add(lambda bm, x=x, s=s, h=h, j=j: blob(bm, 1.0, (x, -D / 2 + 1.5 + rnd.uniform(-0.2, 0.3), h * 0.5), (s, 1.4, h * 0.55), 2, 0.12, seed + j), rock)
    for j in range(6):   # boulders at the foot
        x = rnd.uniform(-L / 2 + 0.4, L / 2 - 0.4); s = rnd.uniform(0.25, 0.5)
        k.add(lambda bm, x=x, s=s, j=j: blob(bm, 1.0, (x, -D / 2 + 0.4 + rnd.uniform(0, 0.3), 0), (s * 1.2, s, s * 0.9), 1, s * 0.2, seed + 10 + j), rock)
    if kind == 'B':   # a little outpost dome half set into the rim, with a porthole and a dish
        k.add(lambda bm: sphere(bm, 1.4, (1.2, -1.0, 0.0), (1, 1, 0.9), 2), whitec)
        for i in range(10):
            a = i * 2 * math.pi / 10
            k.add(lambda bm, a=a: tube(bm, [Vector((1.2 + math.cos(a) * 1.42, -1.0 + math.sin(a) * 1.42, 0.0)), Vector((1.2 + math.cos(a) * 0.3, -1.0 + math.sin(a) * 0.3, 1.27))], 0.025, 0.025, 4), 'metal')
        porthole(k, 1.2, -2.42, 0.7, 0.22)
        k.add(lambda bm: tube(bm, [Vector((-1.8, -1.2, 3.0)), Vector((-1.8, -1.2, 4.2))], 0.04, 0.04, 5), 'metal')
        k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.40, 0.10), (0.55, 0.22)], 12, None, 0, Matrix.Translation((-1.8, -1.2, 4.2)) @ Matrix.Rotation(-0.6, 4, 'X')), whitec)
    if kind == 'C':   # a landing pad on a ledge with beacons
        k.add(lambda bm: lathe(bm, [(1.3, 0.0), (1.3, 0.12), (0.0, 0.12)], 16, None, 0, Matrix.Translation((-0.8, -1.4, 2.0))), 'stone_dark')
        k.add(lambda bm: lathe(bm, [(1.0, 0.125), (1.1, 0.125), (1.1, 0.13), (1.0, 0.13)], 16, None, 0, Matrix.Translation((-0.8, -1.4, 2.0))), 'orange')
        for a in range(4):
            ang = a * math.pi / 2 + 0.4
            p = Vector((-0.8 + math.cos(ang) * 1.25, -1.4 + math.sin(ang) * 1.25, 2.12))
            k.add(lambda bm, p=p: sphere(bm, 0.08, (p.x, p.y, p.z + 0.08), sub=1), 'red', glow=True)
    k.add(lambda bm: blob(bm, 1.0, (0, -D / 2 + 0.5, 0), (L * 0.48, 0.8, 0.10), 3, 0.02, seed), lambda f: 'ash' if f.normal.z > 0.4 else 'metal')
    return k.obj(name)

def build_airlockgate(seed=2041):
    """the base gateway behind each spawn (in place of the base scene's gate arch, 10.8 x 9.6 m): two white cylindrical
    towers with portholes and red tips, a truss bridge between them carrying a round emblem and beacons"""
    k = Kit(['snow', 'snow_shade', 'metal', 'stone_dark', 'ice_deep', 'red', 'orange', 'gold', 'gold_dark', 'lamp_warm', 'lapis', 'stone_light', 'stone'])
    X = 4.2
    for s in (-1, 1):
        M = Matrix.Translation((s * X, 0, 0))
        k.add(lambda bm, M=M: lathe(bm, [(0.95, 0.0), (0.95, 0.3), (0.80, 0.45), (0.75, 5.8), (0.55, 6.6), (0.0, 6.9)], 14, None, 0, M), lambda f: 'red' if f.calc_center_median().z > 5.9 else whitec(f))
        for z in (1.6, 3.4): porthole(k, s * X, -0.76, z, 0.22)
        k.add(lambda bm, M=M: lathe(bm, [(0.77, 2.4), (0.77, 2.6), (0.0, 2.6)], 14, None, 0, M), 'orange')
        k.add(lambda bm, s=s: sphere(bm, 0.12, (s * X, 0, 7.0), sub=1), 'red', glow=True)
    for (y, z) in [(-0.35, 5.0), (0.35, 5.0), (-0.35, 5.8), (0.35, 5.8)]:   # the truss
        k.add(lambda bm, y=y, z=z: tube(bm, [Vector((-X, y, z)), Vector((X, y, z))], 0.06, 0.06, 6), 'metal')
    x = -X + 0.6
    while x < X - 0.5:
        for y in (-0.35, 0.35): k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, 5.0)), Vector((x + 0.6, y, 5.8))], 0.03, 0.03, 4), 'metal')
        x += 0.6
    M = Matrix.Translation((0, -0.45, 5.4)) @ Matrix.Rotation(math.pi / 2, 4, 'X')
    k.add(lambda bm: lathe(bm, [(0.9, -0.05), (1.0, -0.05), (1.0, 0.08), (0.0, 0.08)], 20, None, 0, M), 'lapis')   # emblem disc
    k.add(lambda bm: lathe(bm, [(0.5, 0.09), (0.0, 0.09)], 16, None, 0, M @ Matrix.Translation((0.25, 0.0, 0.0))), 'snow')   # a moon on it
    k.add(lambda bm: lathe(bm, [(1.02, 0.0), (1.12, 0.0), (1.12, 0.06), (1.02, 0.06)], 20, None, 0, M), 'gold')
    k.add(lambda bm: blob(bm, 1.0, (0, -1.0, 0), (5.4, 1.4, 0.08), 3, 0.02, seed), lambda f: 'ash' if f.normal.z > 0.4 else 'metal')
    return k.obj('AirlockGate')

# ------------------------------------------------------------------ backdrop
def build_moondome(seed=2051):
    """a base habitat: a big white ribbed dome with portholes and an entry tunnel"""
    k = Kit(['snow', 'snow_shade', 'metal', 'ice_deep', 'stone_dark', 'orange', 'stone_light', 'stone'])
    k.add(lambda bm: sphere(bm, 4.0, (0, 0, 0), (1, 1, 0.85), 3), whitec)
    for i in range(16):
        a = i * 2 * math.pi / 16
        pts = [Vector((math.cos(a) * 4.02 * math.cos(t), math.sin(a) * 4.02 * math.cos(t), 3.42 * math.sin(t))) for t in [j / 6 * math.pi / 2 for j in range(7)]]
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.05, 0.05, 4), 'metal')
    for i in range(6): porthole(k, math.cos(i) * 3.6, math.sin(i) * 3.6 - 0.2, 1.5, 0.35)
    k.add(lambda bm: tube(bm, [Vector((0, -3.6, 0.8)), Vector((0, -6.0, 0.8))], 0.9, 0.9, 12), whitec)
    k.add(lambda bm: lathe(bm, [(0.92, 0.0), (0.92, 0.2), (0.0, 0.2)], 12, None, 0, Matrix.Translation((0, -6.0, 0.8)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), 'orange')
    return k.obj('MoonDome')

def build_lander(seed=2061):
    """a lander on four splayed legs (an original design): an octagonal gold-foil descent stage, a white cabin with two
    triangular windows, a dish and an antenna"""
    k = Kit(['gold', 'gold_dark', 'snow', 'snow_shade', 'metal', 'stone_dark', 'black', 'ice_deep'])
    k.add(lambda bm: lathe(bm, [(1.6, 1.2), (1.6, 2.4), (0.0, 2.4)], 8, None, 0.39), paint('gold', 'gold_dark'))
    k.add(lambda bm: lathe(bm, [(0.0, 1.15), (1.6, 1.2)], 8, None, 0.39), 'gold_dark')
    for a in range(4):
        ang = a * math.pi / 2 + math.pi / 4
        d = Vector((math.cos(ang), math.sin(ang), 0))
        k.add(lambda bm, d=d: tube(bm, [d * 1.4 + Vector((0, 0, 1.8)), d * 2.6 + Vector((0, 0, 0.15))], 0.07, 0.07, 6), 'metal')
        k.add(lambda bm, d=d: lathe(bm, [(0.35, 0.0), (0.30, 0.10), (0.0, 0.12)], 10, None, 0, Matrix.Translation(d * 2.6)), 'gold')
    k.add(lambda bm: rbox(bm, (2.2, 2.0, 1.6), (0, 0, 2.4), 0.25, segs=2), whitec)
    for s in (-1, 1):
        def win(bm, s=s):
            v = [bm.verts.new((s * 0.5, -1.01, 3.2)), bm.verts.new((s * 0.15, -1.01, 3.6)), bm.verts.new((s * 0.75, -1.01, 3.6))]
            bm.faces.new(v if s < 0 else list(reversed(v)))
        k.add(win, 'black')
    k.add(lambda bm: tube(bm, [Vector((0.6, 0.6, 4.0)), Vector((0.6, 0.6, 4.8))], 0.03, 0.03, 4), 'metal')
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.35, 0.10)], 10, None, 0, Matrix.Translation((-0.7, 0.5, 4.1)) @ Matrix.Rotation(-0.5, 4, 'X')), 'snow')
    return k.obj('Lander')

def build_launchrocket(seed=2071):
    """a tall moon rocket on its pad with a launch tower (an original design): a white body with black roll bands, a red
    nose cone, four swept fins"""
    k = Kit(['snow', 'snow_shade', 'black', 'red', 'xmas_red_dark', 'metal', 'stone_dark', 'orange', 'stone_light', 'stone'])
    k.add(lambda bm: rbox(bm, (6.0, 6.0, 0.6), (0, 0, 0), 0.1), 'stone_dark')
    k.add(lambda bm: lathe(bm, [(0.0, 0.6), (1.2, 0.8), (1.3, 2.0), (1.3, 12.0), (1.1, 13.5), (0.6, 15.5), (0.0, 16.5)], 16), lambda f: 'red' if f.calc_center_median().z > 12.5 else whitec(f))
    for z in (4.0, 8.0): k.add(lambda bm, z=z: lathe(bm, [(1.31, z), (1.31, z + 0.6), (0.0, z + 0.6)], 16), 'black')
    for a in range(4):
        ang = a * math.pi / 2 + math.pi / 4
        d = Vector((math.cos(ang), math.sin(ang), 0)); side = Vector((-d.y, d.x, 0)) * 0.12
        def fin(bm, d=d, side=side):
            pts = [d * 1.2 + Vector((0, 0, 1.0)), d * 1.2 + Vector((0, 0, 4.5)), d * 3.0 + Vector((0, 0, 0.6)), d * 2.6 + Vector((0, 0, 0.6))]
            a1 = [bm.verts.new(p + side) for p in pts]; a2 = [bm.verts.new(p - side) for p in pts]
            bm.faces.new(a1); bm.faces.new(list(reversed(a2)))
            for i in range(4):
                j = (i + 1) % 4
                bm.faces.new((a1[i], a2[i], a2[j], a1[j]))
        k.add(fin, 'red')
    for x in (3.0, 4.2):   # launch tower (lattice)
        for y in (-0.6, 0.6): k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, 0.6)), Vector((x, y, 15.0))], 0.08, 0.08, 4), 'orange')
    z = 1.0
    while z < 15:
        k.add(lambda bm, z=z: tube(bm, [Vector((3.0, -0.6, z)), Vector((4.2, 0.6, z + 1.2))], 0.04, 0.04, 4), 'orange')
        z += 1.2
    k.add(lambda bm: tube(bm, [Vector((3.0, 0, 11.0)), Vector((1.3, 0, 11.0))], 0.07, 0.07, 4), 'orange')
    return k.obj('LaunchRocket')

def build_earth(seed=2081):
    """the Earth hanging in the black sky: a blue sphere with green-brown continents and white cloud bands (faceted)"""
    rnd = random.Random(seed)
    k = Kit(['ice_deep', 'ice', 'xmas_green', 'khaki', 'snow'])
    def globe(bm):
        bmesh.ops.create_icosphere(bm, subdivisions=3, radius=1.0)
    def col(f):
        p = f.calc_center_median()
        n = noise.noise(p * 1.8 + Vector((seed * 0.1, 0, 0)))
        if abs(p.z) > 0.86: return 'snow'
        if noise.noise(p * 3.5 + Vector((5, 1, 2))) > 0.35: return 'snow'          # clouds
        if n > 0.18: return 'xmas_green' if n < 0.45 else 'khaki'
        return 'ice_deep' if n < -0.1 else 'ice'
    k.add(globe, col)
    return k.obj('Earth')
