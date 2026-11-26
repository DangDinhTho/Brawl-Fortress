"""Ocean theme (the sea floor and the ruins of a sunken city): a bright coral reef on rippled sand, the pale aqua stone
of an Atlantis-like ruin with glowing crystals and runes, an old wooden shipwreck with its cannon, anchor and treasure
chest, a brass diving helmet (1960s-70s deep-sea divers), giant clams, sea fans, tall kelp as the hiding bush.
Original designs in the chunky Brawl style, same palette as the other themes; glowing faces go to material slot 1
(OceanGlow in Unity: unlit, breathing slowly).

Walls ('#'): CoralReefA/B, AtlantisWall, ShipHull, BarnacleRock (one family per wall).
Small ('x'): GiantClam, TreasureChest, AnchorRock, DivingHelmet, CannonWreck.
Fence ('f'): CoralRidge.
Tall ('t'): AtlantisColumn, SeaFan, TridentStatue, GlowCrystal, BrokenMast.
Bush ('b'): KelpGrass (the Brawl leaf clump, narrow blades, papyrus ramp; it sways in the game: KelpSway.cs).
Decor: Starfish, SeaShells, SeaUrchins, CoralBits.
Border: ReefWallA/B/C (low reef banks; B with a wreck's bow, C with a ruined arch). Gate: AtlantisGate.
Backdrop: AtlantisTemple, SunkenShip, GiantScallop."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import *
from props3 import blob
from props7 import tube
from props8 import Kit, rbox, sphere
from props11 import shard
from props16 import SKit, disc
import grass

# ------------------------------------------------------------------ colours (by facing, never by the light)
def reefrock(f):
    n = f.normal
    if n.z > 0.6: return 'stone_light'
    return 'stone' if n.z > -0.3 else 'stone_dark'

def aqua(f):
    """the pale sea-green stone of the sunken city"""
    n = f.normal
    return 'ceramic' if n.z > -0.4 else 'pine_light'     # pale jade (the light blue cell read as ice blocks)

def sand(f): return 'sandstone' if f.normal.z > 0.5 else 'khaki'
def wreck(f): return 'olive' if f.normal.z > 0.6 else 'wood_dark'   # wet planks, weed on what faces up

def sand_skirt(k, sx, sy, seed, h=0.06, y=0.0):
    k.add(lambda bm: blob(bm, 1.0, (0, y, 0), (sx, sy, h), 3, 0.02, seed), sand)

# ------------------------------------------------------------------ coral pieces
def brain_coral(k, x, y, z, r, col, seed):
    """a round brain coral: a dome with meandering ridges"""
    ph = (seed % 7) * 0.9
    k.add(lambda bm: lathe(bm, [(r * 0.85, 0.0), (r, r * 0.3), (r * 0.82, r * 0.72), (r * 0.42, r * 0.95), (0.0, r)], 18,
                             lambda i, kk, rr, zz: (rr * (1 + 0.06 * math.sin(i * 2.1 + kk * 1.7 + ph)), zz + (0.03 * r * math.sin(i * 3.3 + ph) if 0 < kk < 4 else 0)),
                             0, Matrix.Translation((x, y, z))), col)

def staghorn(k, x, y, z, h, col, rnd, n=5, r=0.05):
    """branching coral: forked arms rising and spreading, rounded tips"""
    for j in range(n):
        a = rnd.uniform(0, 6.28); lean = rnd.uniform(0.2, 0.55)
        d = Vector((math.cos(a) * lean, math.sin(a) * lean, 1.0)).normalized()
        p0 = Vector((x, y, z)); p1 = p0 + d * h * 0.55
        tip = p1 + (d + Vector((rnd.uniform(-0.3, 0.3), rnd.uniform(-0.3, 0.3), 0.2))).normalized() * h * 0.5
        k.add(lambda bm, p0=p0, p1=p1, tip=tip: tube(bm, [p0, p1, tip], r, r * 0.75, 6), col)
        k.add(lambda bm, tip=tip: sphere(bm, r * 0.85, (tip.x, tip.y, tip.z), sub=1), col)
        b2 = p1 + (d + Vector((rnd.uniform(-0.6, 0.6), rnd.uniform(-0.6, 0.6), 0.0))).normalized() * h * 0.35
        k.add(lambda bm, p1=p1, b2=b2: tube(bm, [p1, b2], r * 0.8, r * 0.6, 5), col)
        k.add(lambda bm, b2=b2: sphere(bm, r * 0.7, (b2.x, b2.y, b2.z), sub=1), col)

def tube_sponges(k, x, y, z, s, col, rnd, n=4):
    """a cluster of tube sponges: open tubes of different heights"""
    for j in range(n):
        a = j * 2 * math.pi / n + rnd.uniform(-0.3, 0.3); d = 0.08 * s if j else 0.0
        h = rnd.uniform(0.25, 0.5) * s; rr = rnd.uniform(0.06, 0.09) * s
        M = Matrix.Translation((x + math.cos(a) * d, y + math.sin(a) * d, z))
        k.add(lambda bm, M=M, h=h, rr=rr: lathe(bm, [(rr * 0.9, 0.0), (rr, h * 0.5), (rr * 1.15, h), (rr * 0.8, h), (rr * 0.7, h * 0.6), (0.0, h * 0.55)], 10, None, 0, M), col)

def sea_fan(k, x, y, z, w, h, yaw, col, rnd):
    """a sea fan: a flat lacy fan on a short stem (a thin double-sided blade with veins)"""
    M = Matrix.Translation((x, y, z)) @ Matrix.Rotation(yaw, 4, 'Z')
    def fan(bm):
        N = 10; pts_f = []; pts_b = []
        for i in range(N + 1):
            a = math.pi * (0.12 + 0.76 * i / N)
            rr = 1.0 + 0.08 * math.sin(i * 2.7)
            pts_f.append(M @ Vector((math.cos(a) * w * rr, -0.025, 0.1 + math.sin(a) * h * rr)))
            pts_b.append(M @ Vector((math.cos(a) * w * rr, 0.025, 0.1 + math.sin(a) * h * rr)))
        c_f = bm.verts.new(M @ Vector((0, -0.025, 0.1))); c_b = bm.verts.new(M @ Vector((0, 0.025, 0.1)))
        vf = [bm.verts.new(p) for p in pts_f]; vb = [bm.verts.new(p) for p in pts_b]
        for i in range(N):
            bm.faces.new((c_f, vf[i + 1], vf[i])); bm.faces.new((c_b, vb[i], vb[i + 1]))
            bm.faces.new((vf[i], vf[i + 1], vb[i + 1], vb[i]))
    k.add(fan, col)
    for j in range(5):   # veins
        a = math.pi * (0.2 + 0.6 * j / 4)
        k.add(lambda bm, a=a: tube(bm, [M @ Vector((0, 0, 0.0)), M @ Vector((math.cos(a) * w * 0.85, 0, 0.1 + math.sin(a) * h * 0.85))], 0.02, 0.012, 4), col)

def anemone(k, x, y, z, s, col, tip, rnd):
    k.add(lambda bm: lathe(bm, [(0.12 * s, 0.0), (0.10 * s, 0.12 * s), (0.13 * s, 0.16 * s), (0.0, 0.15 * s)], 10, None, 0, Matrix.Translation((x, y, z))), col)
    for j in range(9):
        a = j * 2 * math.pi / 9; d = Vector((math.cos(a), math.sin(a), 0))
        p0 = Vector((x, y, z + 0.15 * s)) + d * 0.08 * s; p1 = p0 + d * 0.12 * s + Vector((0, 0, 0.14 * s))
        k.add(lambda bm, p0=p0, p1=p1: tube(bm, [p0, p1], 0.025 * s, 0.012 * s, 4), tip)

def barnacles(k, pts, s=1.0):
    for p in pts:
        k.add(lambda bm, p=p: lathe(bm, [(0.06 * s, 0.0), (0.05 * s, 0.05 * s), (0.025 * s, 0.06 * s), (0.0, 0.05 * s)], 6, None, 0, Matrix.Translation(p)), 'bone')

# ------------------------------------------------------------------ walls
def build_coralreef(kind, seed, name):
    """a reef block: a lumpy rock head grown over with brain coral, branching coral, tube sponges, a sea fan"""
    rnd = random.Random(seed)
    k = SKit(['stone_light', 'stone', 'stone_dark', 'orange', 'red', 'yellow', 'obsidian_light', 'terracotta', 'bulb_green', 'sandstone', 'khaki'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0.55), (0.72, 0.70, 0.72), 2, 0.12, seed), reefrock)
    if kind == 'A':
        brain_coral(k, 0.1, 0.05, 1.0, 0.58, 'orange', seed)
        staghorn(k, -0.38, -0.3, 0.85, 0.95, 'obsidian_light', rnd, n=6, r=0.07)
        tube_sponges(k, 0.42, -0.38, 0.55, 1.6, 'yellow', rnd)
        sea_fan(k, 0.3, 0.42, 0.85, 0.65, 1.0, 0.4, 'red', rnd)
        brain_coral(k, -0.45, 0.4, 0.55, 0.3, 'terracotta', seed + 3)
    else:
        staghorn(k, 0.05, 0.0, 1.0, 1.2, 'red', rnd, n=8, r=0.08)
        brain_coral(k, -0.4, 0.3, 0.7, 0.42, 'terracotta', seed + 1)
        tube_sponges(k, 0.4, 0.35, 0.75, 1.5, 'obsidian_light', rnd)
        sea_fan(k, -0.3, -0.42, 0.65, 0.6, 0.95, -0.5, 'yellow', rnd)
        brain_coral(k, 0.45, -0.35, 0.5, 0.3, 'orange', seed + 5)
    anemone(k, -0.5, -0.55, 0.0, 1.2, 'red', 'bulb_green', rnd)
    sand_skirt(k, 0.85, 0.85, seed)
    return k.obj(name)

def build_atlantiswall(seed=2901):
    """a ruined wall of the sunken city: big pale aqua blocks, a frieze of carved waves, a band of glowing runes,
    coral growing in the joints"""
    rnd = random.Random(seed)
    k = SKit(['ceramic', 'glacier_deep', 'stone', 'gold', 'ice_light', 'turquoise', 'orange', 'obsidian_light', 'sandstone', 'khaki', 'bone'])
    z = 0.0
    for ci, h in enumerate([0.55, 0.55, 0.5]):
        x = -0.72
        while x < 0.7:
            w = min(rnd.uniform(0.5, 0.8), 0.72 - x)
            if ci == 2 and rnd.random() < 0.3: x += w; continue
            k.add(lambda bm, x=x, w=w, z=z, h=h: rbox(bm, (w - 0.03, 1.36 - ci * 0.04, h - 0.02), (x + w / 2, rnd.uniform(-0.02, 0.02), z), 0.05, rnd.uniform(-0.03, 0.03), segs=1), aqua)
            x += w
        z += h
        if ci == 0:   # wave frieze on both faces + glowing runes
            for side in (-1, 1):
                k.add(lambda bm, side=side, z=z: rbox(bm, (1.40, 0.04, 0.07), (0, side * 0.69, z - 0.14), 0.01, segs=1), 'gold')
                for j in range(5):
                    xx = -0.56 + j * 0.28
                    k.add(lambda bm, xx=xx, side=side, z=z: rbox(bm, (0.09, 0.03, 0.14), (xx, side * 0.69, z - 0.4), 0.01, segs=1), 'ice_light', glow=True)
    k.add(lambda bm: blob(bm, 1.0, (0.4, -0.4, 1.1), (0.18, 0.16, 0.12), 1, 0.03, seed, floor=False), 'orange')
    brain_coral(k, -0.35, 0.3, z - 0.02, 0.22, 'orange', seed)
    barnacles(k, [Vector((rnd.uniform(-0.6, 0.6), -0.7, rnd.uniform(0.2, 1.2))) for _ in range(6)])
    sand_skirt(k, 0.85, 0.85, seed)
    return k.obj('AtlantisWall')

def build_shiphull(seed=2911):
    """a broken section of an old wooden ship's hull standing in the sand: curved planks on heavy ribs, barnacles and weed"""
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'wood', 'olive', 'bone', 'metal', 'stone_dark', 'sandstone', 'khaki', 'bulb_green'])
    for i in range(6):   # planks: curved strakes
        z = 0.15 + i * 0.32
        bend = 0.18 * math.sin(i / 5 * math.pi)
        pts = [Vector((-0.72, -0.15 - bend, z)), Vector((0.0, -0.30 - bend, z + 0.02)), Vector((0.72, -0.15 - bend, z))]
        L = rnd.uniform(0.85, 1.0)
        pts = [p if j != 2 else pts[1].lerp(p, L) for j, p in enumerate(pts)]
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.15, 0.15, 4), wreck)
    for x in (-0.6, -0.1, 0.4):   # ribs behind
        k.add(lambda bm, x=x: tube(bm, [Vector((x, -0.15, 0.0)), Vector((x, 0.15, 0.9)), Vector((x, 0.05, 2.0))], 0.11, 0.09, 6), 'wood')
    k.add(lambda bm: rbox(bm, (1.42, 0.6, 0.5), (0, 0.35, 0.0), 0.05, segs=1), 'wood_dark')   # keel timber / ballast block
    barnacles(k, [Vector((rnd.uniform(-0.6, 0.6), -0.48, rnd.uniform(0.2, 1.8))) for _ in range(10)])
    for j in range(4):
        p = Vector((rnd.uniform(-0.5, 0.5), -0.5, rnd.uniform(0.3, 1.6)))
        k.add(lambda bm, p=p: tube(bm, [p, p + Vector((0.02, -0.05, -0.35))], 0.04, 0.01, 4), 'bulb_green')
    sand_skirt(k, 0.85, 0.85, seed)
    return k.obj('ShipHull')

def build_barnaclerock(seed=2921):
    """a dark reef rock crusted with barnacles, anemones and a starfish"""
    rnd = random.Random(seed)
    k = SKit(['stone_light', 'stone', 'stone_dark', 'bone', 'red', 'bulb_green', 'orange', 'obsidian_light', 'sandstone', 'khaki'])
    k.add(lambda bm: blob(bm, 1.0, (-0.1, 0.0, 0.8), (0.68, 0.66, 0.95), 2, 0.14, seed), reefrock)
    k.add(lambda bm: blob(bm, 1.0, (0.4, -0.35, 0.3), (0.36, 0.3, 0.34), 1, 0.06, seed + 1), reefrock)
    pts = []
    for j in range(14):
        a = rnd.uniform(0, 6.28); zz = rnd.uniform(0.3, 1.4)
        pts.append(Vector((-0.1 + math.cos(a) * 0.62, math.sin(a) * 0.6, zz)))
    barnacles(k, pts, 1.2)
    anemone(k, 0.0, -0.1, 1.68, 1.4, 'obsidian_light', 'yellow', rnd)
    anemone(k, 0.45, -0.4, 0.62, 1.0, 'red', 'bulb_green', rnd)
    sand_skirt(k, 0.85, 0.85, seed)
    return k.obj('BarnacleRock')

# ------------------------------------------------------------------ small
def build_giantclam(seed=2931):
    """a giant clam gaping open: two ridged shells with a wavy lip, a glowing pearl inside"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'obsidian_light', 'frost', 'frost_white', 'sandstone', 'khaki', 'turquoise'])
    def shell(M):
        k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.35, 0.02), (0.58, 0.12), (0.66, 0.26)], 16,
                                 lambda i, kk, rr, zz: (rr * (1 + 0.1 * abs(math.sin(i * math.pi / 4))), zz + (0.05 * math.sin(i * math.pi / 2) if kk == 3 else 0)),
                                 0, M), lambda f: 'cream' if f.normal.z < 0.3 else 'bone')
    shell(Matrix.Translation((0, 0, 0.0)) @ Matrix.Diagonal((1.0, 0.8, 1.0, 1)))
    shell(Matrix.Translation((0, 0.42, 0.25)) @ Matrix.Rotation(-2.3, 4, 'X') @ Matrix.Diagonal((1.0, 0.8, 1.0, 1)))
    k.add(lambda bm: blob(bm, 1.0, (0, 0.05, 0.12), (0.45, 0.3, 0.08), 2, 0.02, seed, floor=False), 'turquoise')   # mantle
    k.add(lambda bm: sphere(bm, 0.14, (0, 0.05, 0.28), sub=2), 'frost_white', glow=True)
    sand_skirt(k, 0.75, 0.7, seed)
    return k.obj('GiantClam')

def build_treasurechest(seed=2941):
    """a pirate's treasure chest half sunk in the sand: wooden staves, gold bands, the lid open, gold coins spilling"""
    rnd = random.Random(seed)
    k = SKit(['wood', 'wood_dark', 'gold', 'gold_dark', 'yellow', 'red', 'sandstone', 'khaki', 'olive'])
    k.add(lambda bm: rbox(bm, (1.0, 0.65, 0.5), (0, 0, 0), 0.04, 0.2, segs=1), 'wood')
    for x in (-0.36, 0.36):
        k.add(lambda bm, x=x: bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Rotation(0.2, 4, 'Z') @ Matrix.Translation((x, 0, 0.26)) @ Matrix.Diagonal((0.07, 0.68, 0.53, 1))), 'gold')
    M = Matrix.Rotation(0.2, 4, 'Z') @ Matrix.Translation((0, 0.33, 0.5)) @ Matrix.Rotation(-1.9, 4, 'X')
    k.add(lambda bm: bmesh.ops.create_cube(bm, size=1.0, matrix=M @ Matrix.Translation((0, -0.32, 0.06)) @ Matrix.Diagonal((1.0, 0.65, 0.12, 1))), 'wood_dark')
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0.45), (0.45, 0.28, 0.14), 2, 0.03, seed, floor=False), lambda f: 'gold' if f.normal.z > 0.3 else 'gold_dark')
    for j in range(12):
        x, y = rnd.uniform(-0.75, 0.75), rnd.uniform(-0.65, -0.3)
        k.add(lambda bm, x=x, y=y: lathe(bm, [(0.0, 0.0), (0.06, 0.0), (0.06, 0.015), (0.0, 0.015)], 8, None, 0, Matrix.Translation((x, y, 0.02))), 'gold')
    k.add(lambda bm: sphere(bm, 0.07, (0.1, 0.0, 0.62), sub=1), 'red')
    sand_skirt(k, 0.8, 0.7, seed)
    return k.obj('TreasureChest')

def build_anchorrock(seed=2951):
    """an old iron ship's anchor wedged against a rock, its chain trailing into the sand"""
    rnd = random.Random(seed)
    k = SKit(['black', 'stone_dark', 'terracotta', 'stone_light', 'stone', 'wood_dark', 'sandstone', 'khaki', 'olive'])
    k.add(lambda bm: blob(bm, 1.0, (0.35, 0.25, 0.3), (0.42, 0.38, 0.42), 2, 0.08, seed), reefrock)
    T = Matrix.Translation((-0.1, -0.1, 0.25)) @ Matrix.Rotation(-0.9, 4, 'Y') @ Matrix.Rotation(0.3, 4, 'Z')
    iron = lambda f: 'terracotta' if f.normal.z > 0.5 else 'black'
    k.add(lambda bm: tube(bm, [T @ Vector((0, 0, -0.6)), T @ Vector((0, 0, 0.7))], 0.07, 0.07, 6), iron)       # shank
    k.add(lambda bm: tube(bm, [T @ Vector((-0.5, 0, -0.35)), T @ Vector((-0.3, 0, -0.6)), T @ Vector((0, 0, -0.66)), T @ Vector((0.3, 0, -0.6)), T @ Vector((0.5, 0, -0.35))], 0.07, 0.07, 6), iron)   # arms
    for s in (-1, 1):
        k.add(lambda bm, s=s: lathe(bm, [(0.0, 0.0), (0.14, 0.12), (0.0, 0.3)], 4, None, 0, T @ Matrix.Translation((s * 0.5, 0, -0.35)) @ Matrix.Rotation(s * -0.6, 4, 'Y')), iron)   # flukes
    k.add(lambda bm: tube(bm, [T @ Vector((0, -0.42, 0.55)), T @ Vector((0, 0.42, 0.55))], 0.05, 0.05, 6), 'wood_dark')   # stock
    k.add(lambda bm: lathe(bm, [(0.10, -0.03), (0.14, 0.0), (0.10, 0.03), (0.06, 0.0)], 10, None, 0, T @ Matrix.Translation((0, 0, 0.82)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), iron)   # ring
    p = T @ Vector((0, 0, 0.82))
    for j in range(7):   # chain links trailing away
        q = p + Vector((-0.12 - j * 0.12, -0.15 - j * 0.05, -min(0.75, 0.12 * j + 0.1)))
        k.add(lambda bm, q=q, j=j: lathe(bm, [(0.05, -0.015), (0.065, 0.0), (0.05, 0.015), (0.035, 0.0)], 8, None, 0, Matrix.Translation(q) @ Matrix.Rotation(j * 1.57, 4, 'Z') @ Matrix.Rotation(math.pi / 2, 4, 'X')), 'black')
    sand_skirt(k, 0.8, 0.75, seed)
    return k.obj('AnchorRock')

def build_divinghelmet(seed=2961):
    """an old brass diving helmet (deep-sea diver's) on a sea chest: round faceplate and side ports, bolts, a breastplate"""
    rnd = random.Random(seed)
    k = SKit(['gold', 'gold_dark', 'ice_deep', 'black', 'wood', 'wood_dark', 'metal', 'sandstone', 'khaki', 'olive'])
    k.add(lambda bm: rbox(bm, (0.9, 0.6, 0.45), (0, 0, 0), 0.05, 0.3, segs=1), 'wood')
    brass = lambda f: 'gold' if f.normal.z > -0.2 else 'gold_dark'
    k.add(lambda bm: lathe(bm, [(0.0, 0.45), (0.36, 0.47), (0.40, 0.52), (0.30, 0.60), (0.0, 0.62)], 16), brass)   # breastplate
    k.add(lambda bm: sphere(bm, 0.30, (0, 0, 0.88), (1, 1, 1.05), 2), brass)
    for (a, r, z) in [(0.0, 0.13, 0.88), (math.pi / 2, 0.09, 0.9), (-math.pi / 2, 0.09, 0.9)]:
        d = Vector((math.sin(a), -math.cos(a), 0))
        M = Matrix.Translation(Vector((0, 0, z)) + d * 0.29) @ d.to_track_quat('Z', 'Y').to_matrix().to_4x4()
        k.add(lambda bm, M=M, r=r: lathe(bm, [(r, -0.02), (r * 1.3, -0.02), (r * 1.3, 0.04), (r, 0.04)], 12, None, 0, M), 'gold_dark')
        k.add(lambda bm, M=M, r=r: lathe(bm, [(r, 0.0), (0.0, 0.0)], 12, None, 0, M), 'ice_deep')
    for j in range(8):
        a = j * 2 * math.pi / 8
        k.add(lambda bm, a=a: sphere(bm, 0.03, (math.cos(a) * 0.37, math.sin(a) * 0.37, 0.56), sub=1), 'metal')
    k.add(lambda bm: tube(bm, [Vector((0.1, 0.25, 1.05)), Vector((0.3, 0.45, 1.0)), Vector((0.5, 0.6, 0.5)), Vector((0.55, 0.65, 0.02))], 0.035, 0.035, 6), 'black')   # air hose
    sand_skirt(k, 0.7, 0.65, seed)
    return k.obj('DivingHelmet')

def build_cannonwreck(seed=2971):
    """a ship's cannon still on its broken wooden carriage, crusted, a few cannonballs by it"""
    rnd = random.Random(seed)
    k = SKit(['black', 'stone_dark', 'wood', 'wood_dark', 'olive', 'terracotta', 'sandstone', 'khaki', 'bone'])
    k.add(lambda bm: rbox(bm, (0.9, 0.55, 0.3), (0, 0, 0.12), 0.04, 0.0, segs=1), wreck)
    for x in (-0.3, 0.3):
        for y in (-0.3, 0.3):
            k.add(lambda bm, x=x, y=y: lathe(bm, [(0.0, -0.04), (0.14, -0.04), (0.14, 0.04), (0.0, 0.04)], 10, None, 0, Matrix.Translation((x, y, 0.14)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), 'wood_dark')
    M = Matrix.Translation((-0.55, 0, 0.55)) @ Matrix.Rotation(math.pi / 2 - 0.12, 4, 'Y')
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.2, 0.0), (0.22, 0.1), (0.18, 0.2), (0.15, 1.0), (0.17, 1.1), (0.17, 1.2), (0.09, 1.2), (0.09, 1.0)], 12, None, 0, M), lambda f: 'terracotta' if f.normal.z > 0.6 else 'black')
    for j, (x, y) in enumerate([(0.45, -0.45), (0.6, -0.3), (0.52, -0.6)]):
        k.add(lambda bm, x=x, y=y: sphere(bm, 0.1, (x, y, 0.1), sub=2), 'black')
    barnacles(k, [Vector((rnd.uniform(-0.3, 0.4), rnd.uniform(-0.1, 0.1), 0.72)) for _ in range(5)], 0.8)
    sand_skirt(k, 0.8, 0.7, seed)
    return k.obj('CannonWreck')

# ------------------------------------------------------------------ fence
def build_coralridge(seed=2981):
    """a low ridge of reef rock across the tile with small corals on it (low cover)"""
    rnd = random.Random(seed)
    k = SKit(['stone_light', 'stone', 'stone_dark', 'orange', 'red', 'yellow', 'obsidian_light', 'bulb_green', 'sandstone', 'khaki'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.8, 0.36, 0.5), 2, 0.07, seed), reefrock)
    brain_coral(k, -0.35, 0.0, 0.42, 0.2, 'orange', seed)
    staghorn(k, 0.3, 0.05, 0.4, 0.35, 'obsidian_light', rnd, n=3, r=0.035)
    tube_sponges(k, 0.0, -0.1, 0.45, 0.7, 'yellow', rnd, n=3)
    sand_skirt(k, 0.85, 0.45, seed)
    return k.obj('CoralRidge')

# ------------------------------------------------------------------ tall
def build_atlantiscolumn(seed=2991):
    """a column of the sunken city: banded pale aqua shaft with gold rings, a shell-shaped capital holding a glowing orb"""
    rnd = random.Random(seed)
    k = SKit(['ceramic', 'glacier_deep', 'stone', 'gold', 'gold_dark', 'ice_light', 'frost_white', 'orange', 'sandstone', 'khaki'])
    k.add(lambda bm: rbox(bm, (0.9, 0.9, 0.3), (0, 0, 0), 0.05, segs=1), aqua)
    k.add(lambda bm: lathe(bm, [(0.34, 0.3), (0.30, 1.2), (0.32, 1.3), (0.30, 1.4), (0.28, 2.6), (0.0, 2.6)], 16, lambda i, kk, rr, zz: (rr * (0.93 if i % 2 else 1.0), zz)), aqua)
    for z in (0.6, 1.35, 2.1): k.add(lambda bm, z=z: lathe(bm, [(0.33, z), (0.33, z + 0.08), (0.0, z + 0.08)], 16), 'gold')
    k.add(lambda bm: lathe(bm, [(0.28, 2.55), (0.5, 2.85), (0.56, 2.95), (0.0, 2.9)], 12, lambda i, kk, rr, zz: (rr * (1.12 if i % 2 else 1.0), zz + (0.06 if i % 2 and kk == 2 else 0)), 0), 'ceramic')
    k.grad(lambda bm: sphere(bm, 0.3, (0, 0, 3.2), sub=2), 'ice_grad', lambda v: min(1.0, max(0.0, (v.co.z - 2.9) / 0.6)), glow=True)
    brain_coral(k, 0.32, -0.3, 0.3, 0.16, 'orange', seed)
    sand_skirt(k, 0.6, 0.6, seed)
    return k.obj('AtlantisColumn')

def build_seafan(seed=3001):
    """a tall purple sea fan and a red one behind it, on a reef rock"""
    rnd = random.Random(seed)
    k = SKit(['stone_light', 'stone', 'stone_dark', 'obsidian_light', 'red', 'yellow', 'sandstone', 'khaki', 'bulb_green'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.5, 0.45, 0.4), 2, 0.06, seed), reefrock)
    k.add(lambda bm: tube(bm, [Vector((0, 0, 0.3)), Vector((0, 0, 0.7))], 0.06, 0.05, 6), 'obsidian_light')
    sea_fan(k, 0.0, 0.0, 0.6, 1.0, 2.2, 0.0, 'obsidian_light', rnd)
    sea_fan(k, 0.15, 0.25, 0.4, 0.6, 1.5, 0.5, 'red', rnd)
    tube_sponges(k, -0.3, -0.25, 0.25, 0.8, 'yellow', rnd, n=3)
    sand_skirt(k, 0.6, 0.55, seed)
    return k.obj('SeaFan')

def build_tridentstatue(seed=3011):
    """a statue of the sunken city (an original sea-king figure): a merman with a curling tail on a plinth, raising a
    gold trident, an orb glowing at its tip"""
    rnd = random.Random(seed)
    k = SKit(['ceramic', 'glacier_deep', 'stone', 'gold', 'gold_dark', 'ice_light', 'frost_white', 'orange', 'sandstone', 'khaki'])
    k.add(lambda bm: rbox(bm, (1.0, 1.0, 0.75), (0, 0, 0), 0.06, segs=1), aqua)
    k.add(lambda bm: rbox(bm, (1.1, 1.1, 0.1), (0, 0, 0.75), 0.03, segs=1), 'gold')
    z0 = 0.85
    tail = [Vector((0.25, 0.1, z0 + 0.05)), Vector((0.0, 0.0, z0 + 0.2)), Vector((-0.12, 0.0, z0 + 0.6)), Vector((0.0, 0.0, z0 + 1.0))]
    k.add(lambda bm: tube(bm, tail, 0.12, 0.26, 10), aqua)
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.18, 0.06), (0.0, 0.12)], 6, None, 0, Matrix.Translation(tail[0]) @ Matrix.Rotation(1.3, 4, 'Y')), aqua)   # fluke
    k.add(lambda bm: lathe(bm, [(0.26, 0.0), (0.30, 0.25), (0.24, 0.5), (0.12, 0.6), (0.0, 0.62)], 14, None, 0, Matrix.Translation((0, 0, z0 + 1.0))), aqua)   # torso
    k.add(lambda bm: sphere(bm, 0.17, (0, 0, z0 + 1.78), sub=2), aqua)   # head
    k.add(lambda bm: lathe(bm, [(0.16, 0.0), (0.18, 0.08), (0.12, 0.1), (0.15, 0.2), (0.0, 0.12)], 10, lambda i, kk, rr, zz: (rr, zz + (0.06 if i % 2 and kk == 3 else 0)), 0, Matrix.Translation((0, 0, z0 + 1.88))), 'gold')   # crown
    k.add(lambda bm: tube(bm, [Vector((0.28, 0, z0 + 1.45)), Vector((0.42, -0.05, z0 + 1.7))], 0.06, 0.05, 6), aqua)   # arm
    k.add(lambda bm: tube(bm, [Vector((0.45, -0.06, z0 + 0.6)), Vector((0.45, -0.06, z0 + 2.5))], 0.03, 0.03, 6), 'gold')   # trident
    k.add(lambda bm: tube(bm, [Vector((0.30, -0.06, z0 + 2.55)), Vector((0.30, -0.06, z0 + 2.45)), Vector((0.60, -0.06, z0 + 2.45)), Vector((0.60, -0.06, z0 + 2.55))], 0.025, 0.025, 4), 'gold')
    for x in (0.30, 0.45, 0.60):
        k.add(lambda bm, x=x: lathe(bm, [(0.03, 0.0), (0.0, 0.18)], 4, None, 0, Matrix.Translation((x, -0.06, z0 + 2.5))), 'gold')
    k.add(lambda bm: sphere(bm, 0.08, (0.45, -0.06, z0 + 2.75), sub=1), 'frost_white', glow=True)
    sand_skirt(k, 0.65, 0.65, seed)
    return k.obj('TridentStatue')

def build_glowcrystal(seed=3021):
    """a cluster of the sunken city's glowing crystals on a rock: tall hexagonal spires lit from within"""
    rnd = random.Random(seed)
    k = SKit(['stone_light', 'stone', 'stone_dark', 'gold', 'sandstone', 'khaki'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.55, 0.5, 0.35), 2, 0.06, seed), reefrock)
    for j, (x, y, r, h, lx, ly) in enumerate([(0.0, 0.0, 0.2, 2.6, 0.02, 0.0), (0.25, 0.15, 0.14, 1.7, 0.15, 0.08), (-0.22, 0.12, 0.13, 1.5, -0.15, 0.05), (0.05, -0.25, 0.12, 1.2, 0.03, -0.15), (-0.15, -0.15, 0.09, 0.8, -0.1, -0.1)]):
        k.grad(lambda bm, x=x, y=y, r=r, h=h, lx=lx, ly=ly: shard(bm, x, y, r, h, (lx, ly), rnd, 6), 'ice_grad', lambda v, h=h: min(1.0, max(0.0, v.co.z / h)), glow=True)
    sand_skirt(k, 0.6, 0.55, seed)
    return k.obj('GlowCrystal')

def build_brokenmast(seed=3031, name='BrokenMast'):
    """the broken mast of a wreck stuck in the sand at a slant: a yard with a torn sail, rigging, a crow's nest"""
    rnd = random.Random(seed)
    k = SKit(['wood', 'wood_dark', 'cream', 'bone', 'khaki', 'olive', 'sandstone'])
    T = Matrix.Rotation(0.18, 4, 'X') @ Matrix.Rotation(0.1, 4, 'Y')
    k.add(lambda bm: tube(bm, [T @ Vector((0, 0, 0)), T @ Vector((0, 0, 3.6))], 0.14, 0.09, 8), wreck)
    k.add(lambda bm: tube(bm, [T @ Vector((-0.9, 0, 2.6)), T @ Vector((0.9, 0, 2.6))], 0.06, 0.05, 6), 'wood_dark')   # yard
    def sail(bm):
        v = [T @ Vector((-0.85, -0.04, 2.55)), T @ Vector((0.85, -0.04, 2.55)), T @ Vector((0.6, -0.12, 1.4)), T @ Vector((-0.2, -0.1, 1.7)), T @ Vector((-0.75, -0.08, 1.2))]
        f = [bm.verts.new(p) for p in v]; b = [bm.verts.new(p + Vector((0, 0.02, 0))) for p in v]
        bm.faces.new(f); bm.faces.new(list(reversed(b)))
    k.add(sail, 'cream')
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.32, 0.0), (0.34, 0.3), (0.3, 0.32), (0.3, 0.05), (0.0, 0.05)], 12, None, 0, T @ Matrix.Translation((0, 0, 3.15))), 'wood')   # crow's nest
    for (a, b) in [((0, 0, 3.4), (1.0, 0.3, 0.0)), ((0, 0, 3.4), (-0.9, 0.4, 0.0))]:
        k.add(lambda bm, a=a, b=b: tube(bm, [T @ Vector(a), Vector(b)], 0.015, 0.015, 3), 'khaki')
    sand_skirt(k, 0.6, 0.55, seed)
    return k.obj(name)

def build_kelpgrass():
    """tall kelp (the hiding bush): the Brawl leaf clump, narrow blades, the papyrus ramp (dark olive to gold tips)"""
    return grass.build_frostgrass(221, 'KelpGrass', 'papyrus_grad', 0.5)

# ------------------------------------------------------------------ decor
def build_starfish(seed=3041):
    rnd = random.Random(seed)
    k = SKit(['orange', 'red', 'yellow', 'terracotta'])
    for (x, y, s, col, a0) in [(-0.2, 0.05, 1.0, 'orange', 0.3), (0.3, -0.15, 0.7, 'red', 1.2)]:
        def star(bm, x=x, y=y, s=s, a0=a0):
            c_t = bm.verts.new((x, y, 0.07 * s)); c_b = bm.verts.new((x, y, 0.0))
            pts = []
            for i in range(10):
                a = a0 + i * math.pi / 5; r = (0.25 if i % 2 == 0 else 0.09) * s
                pts.append((bm.verts.new((x + math.cos(a) * r, y + math.sin(a) * r, 0.025 * s)), bm.verts.new((x + math.cos(a) * r, y + math.sin(a) * r, 0.0))))
            for i in range(10):
                j = (i + 1) % 10
                bm.faces.new((c_t, pts[i][0], pts[j][0])); bm.faces.new((c_b, pts[j][1], pts[i][1]))
                bm.faces.new((pts[i][0], pts[i][1], pts[j][1], pts[j][0]))
        k.add(star, col)
    return k.obj('Starfish')

def build_seashells(seed=3051):
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'terracotta', 'frost', 'sandstone'])
    for j in range(5):
        x, y = rnd.uniform(-0.4, 0.4), rnd.uniform(-0.3, 0.3); s = rnd.uniform(0.08, 0.15)
        k.add(lambda bm, x=x, y=y, s=s: lathe(bm, [(0.0, 0.0), (s, 0.01), (s * 1.1, s * 0.35), (0.0, s * 0.45)], 10, lambda i, kk, rr, zz: (rr * (1 + 0.12 * abs(math.sin(i * math.pi / 2.5))), zz), rnd.uniform(0, 6), Matrix.Translation((x, y, 0))), rnd.choice(['cream', 'bone', 'terracotta', 'frost']))
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.08, 0.05), (0.06, 0.15), (0.03, 0.24), (0.0, 0.28)], 10, None, 0, Matrix.Translation((0.25, 0.2, 0.05)) @ Matrix.Rotation(1.4, 4, 'Y')), 'cream')
    return k.obj('SeaShells')

def build_seaurchins(seed=3061):
    rnd = random.Random(seed)
    k = SKit(['obsidian', 'obsidian_light', 'black'])
    for (x, y, r) in [(-0.2, 0.0, 0.14), (0.15, 0.15, 0.1), (0.25, -0.2, 0.12)]:
        k.add(lambda bm, x=x, y=y, r=r: sphere(bm, r, (x, y, r * 0.6), (1, 1, 0.8), 1), 'obsidian')
        for j in range(14):
            u = rnd.uniform(-0.2, 1.0); a = rnd.uniform(0, 6.28); s = math.sqrt(max(0, 1 - u * u))
            d = Vector((s * math.cos(a), s * math.sin(a), u))
            p0 = Vector((x, y, r * 0.6)) + d * r * 0.8
            k.add(lambda bm, p0=p0, d=d, r=r: tube(bm, [p0, p0 + d * r * 1.3], 0.012, 0.004, 3), 'obsidian_light')
    return k.obj('SeaUrchins')

def build_coralbits(seed=3071):
    rnd = random.Random(seed)
    k = SKit(['orange', 'red', 'yellow', 'obsidian_light', 'stone', 'stone_light'])
    brain_coral(k, -0.25, 0.0, 0.0, 0.14, 'orange', seed)
    staghorn(k, 0.2, 0.1, 0.0, 0.3, 'red', rnd, n=3, r=0.03)
    tube_sponges(k, 0.05, -0.25, 0.0, 0.5, 'yellow', rnd, n=3)
    return k.obj('CoralBits')

# ------------------------------------------------------------------ border, gate, backdrop
def ruined_arch(k, cx, cy, cz, s, rnd):
    for sx in (-1, 1):
        k.add(lambda bm, sx=sx: lathe(bm, [(0.35 * s, 0.0), (0.3 * s, 2.4 * s), (0.0, 2.4 * s)], 12, lambda i, kk, rr, zz: (rr * (0.93 if i % 2 else 1.0), zz), 0, Matrix.Translation((cx + sx * 1.1 * s, cy, cz))), aqua)
    pts = [Vector((cx - 1.1 * s, cy, cz + 2.4 * s)), Vector((cx - 0.6 * s, cy, cz + 3.1 * s)), Vector((cx, cy, cz + 3.3 * s)), Vector((cx + 0.5 * s, cy, cz + 3.15 * s))]
    k.add(lambda bm: tube(bm, pts, 0.28 * s, 0.26 * s, 8), aqua)   # the arch, broken off on one side
    k.add(lambda bm: sphere(bm, 0.18 * s, (cx, cy - 0.2 * s, cz + 3.0 * s), sub=2), 'ice_light', glow=True)

def build_reefwall(kind, seed, name):
    """a border piece in place of the old border cliffs (7.6 m long, 5 m deep, ~3.5 m tall: low): a reef bank of
    rock heads grown over with coral, sea fans and kelp tufts. Front = -Y (+Z in Unity). 'A' reef, 'B' with the bow of
    a wreck run aground on it, 'C' with a ruined arch of the sunken city."""
    rnd = random.Random(seed)
    k = SKit(['stone_light', 'stone', 'stone_dark', 'orange', 'red', 'yellow', 'obsidian_light', 'terracotta', 'bulb_green',
              'wood_dark', 'wood', 'olive', 'cream', 'ceramic', 'glacier_deep', 'ice_light', 'sandstone', 'khaki', 'bone'])
    L, D = 7.6, 5.0
    k.add(lambda bm: rbox(bm, (L, D * 0.55, 2.4), (0, D * 0.18, 0), 0.35, segs=2), reefrock)
    for t in range(2):
        n = 6 - t
        for j in range(n):
            x = -L / 2 + 0.7 + j * (L - 1.4) / max(1, n - 1) + rnd.uniform(-0.3, 0.3)
            if kind == 'B' and -2.6 < x < 0.4 and t == 1: continue
            k.add(lambda bm, x=x, t=t, j=j: blob(bm, 1.0, (x, -D / 2 + 1.3 + t * 0.4, 0.6 + t * 1.1), (rnd.uniform(0.85, 1.15), 0.8, 0.7), 2, 0.12, seed + j * 3 + t, floor=False), reefrock)
    cols = ['orange', 'red', 'yellow', 'obsidian_light', 'terracotta']
    for j in range(12):   # corals along the top and the face
        x = -L / 2 + 0.5 + j * (L - 1.0) / 11 + rnd.uniform(-0.2, 0.2)
        if kind == 'B' and -2.8 < x < 0.6: continue
        if kind == 'C' and 0.6 < x < 3.4: continue
        c = cols[j % len(cols)]; what = j % 3
        zc = 1.6 + rnd.uniform(0.0, 0.4)
        if what == 0: brain_coral(k, x, -D / 2 + 0.95, zc, rnd.uniform(0.45, 0.6), c, seed + j)
        elif what == 1: staghorn(k, x, -D / 2 + 0.95, zc, 1.0, c, rnd, n=6, r=0.075)
        else: sea_fan(k, x, -D / 2 + 1.15, zc - 0.1, 0.7, 1.2, rnd.uniform(-0.4, 0.4), c, rnd)
    for j in range(5):   # anemones and sponges at the foot
        x = rnd.uniform(-L / 2 + 0.5, L / 2 - 0.5)
        if j % 2: anemone(k, x, -D / 2 + 0.35, 0.0, 1.3, 'red', 'bulb_green', rnd)
        else: tube_sponges(k, x, -D / 2 + 0.35, 0.0, 1.2, 'yellow', rnd, n=3)
    if kind == 'B':   # the bow of a wreck run aground
        B = Matrix.Translation((-1.2, -1.0, 0.3)) @ Matrix.Rotation(0.25, 4, 'X') @ Matrix.Rotation(-0.35, 4, 'Z')
        for i in range(5):
            z = i * 0.38
            pts = [B @ Vector((-1.2, -0.0, z)), B @ Vector((0.0, -0.75 + i * 0.05, z)), B @ Vector((1.4, -0.2, z + 0.3))]
            k.add(lambda bm, pts=pts: tube(bm, pts, 0.17, 0.12, 4), wreck)
            pts2 = [B @ Vector((-1.2, 0.0, z)), B @ Vector((0.0, 0.75 - i * 0.05, z)), B @ Vector((1.4, 0.2, z + 0.3))]
            k.add(lambda bm, pts2=pts2: tube(bm, pts2, 0.17, 0.12, 4), wreck)
        k.add(lambda bm: tube(bm, [B @ Vector((1.4, 0, 1.9)), B @ Vector((2.6, 0, 2.6))], 0.08, 0.05, 6), 'wood')   # bowsprit
        barnacles(k, [B @ Vector((rnd.uniform(-1.0, 1.0), -0.85, rnd.uniform(0.2, 1.5))) for _ in range(8)])
    if kind == 'C':
        ruined_arch(k, 2.0, -1.3, 0.0, 1.0, rnd)
    k.add(lambda bm: blob(bm, 1.0, (0, -D / 2 + 0.5, 0), (L * 0.49, 0.7, 0.08), 3, 0.02, seed), sand)
    return k.obj(name)

def build_atlantisgate(seed=3081):
    """the gateway behind each spawn (in place of the base scene's gate arch, ~10.8 m wide): two banded pale aqua
    pillars with shell capitals, a lintel crested with carved waves, a big glowing orb, coral and kelp growing on it"""
    rnd = random.Random(seed)
    k = SKit(['ceramic', 'glacier_deep', 'stone', 'gold', 'gold_dark', 'ice_light', 'frost_white', 'orange', 'red', 'obsidian_light', 'yellow', 'sandstone', 'khaki'])
    X = 4.4
    for s in (-1, 1):
        k.add(lambda bm, s=s: rbox(bm, (1.6, 1.6, 0.5), (s * X, 0, 0), 0.08, segs=1), aqua)
        k.add(lambda bm, s=s: lathe(bm, [(0.6, 0.5), (0.55, 5.6), (0.0, 5.6)], 16, lambda i, kk, rr, zz: (rr * (0.92 if i % 2 else 1.0), zz), 0, Matrix.Translation((s * X, 0, 0))), aqua)
        for z in (1.4, 3.0, 4.6): k.add(lambda bm, s=s, z=z: lathe(bm, [(0.6, z), (0.6, z + 0.14), (0.0, z + 0.14)], 16, None, 0, Matrix.Translation((s * X, 0, 0))), 'gold')
        k.add(lambda bm, s=s: lathe(bm, [(0.55, 5.55), (0.95, 5.95), (1.0, 6.1), (0.0, 6.05)], 12, lambda i, kk, rr, zz: (rr * (1.12 if i % 2 else 1.0), zz), 0, Matrix.Translation((s * X, 0, 0))), 'ceramic')
        brain_coral(k, s * X + 0.6, -0.6, 0.5, 0.4, 'orange', seed + s)
        staghorn(k, s * X - 0.5, -0.7, 0.5, 0.8, 'obsidian_light', rnd, n=4, r=0.06)
    k.add(lambda bm: rbox(bm, (10.6, 1.4, 0.9), (0, 0, 6.1), 0.12, segs=2), aqua)
    k.add(lambda bm: rbox(bm, (10.65, 1.45, 0.14), (0, 0, 6.5), 0.02, segs=1), 'gold')
    for j in range(11):   # carved wave crests along the top
        x = -5.0 + j * 1.0
        M = Matrix.Translation((x, 0, 7.0)) @ Matrix.Rotation(math.pi / 2, 4, 'X')
        k.add(lambda bm, M=M: lathe(bm, [(0.3, -0.5), (0.45, -0.5), (0.45, 0.5), (0.3, 0.5)], 12, None, 0, M), 'glacier_deep')
    k.grad(lambda bm: sphere(bm, 0.6, (0, -0.6, 5.6), sub=2), 'ice_grad', lambda v: min(1.0, max(0.0, (v.co.z - 5.0) / 1.2)), glow=True)
    for j in range(8):
        x = -4.0 + j * 1.15
        k.add(lambda bm, x=x: rbox(bm, (0.14, 0.04, 0.2), (x, -0.73, 6.35), 0.01, segs=1), 'ice_light', glow=True)
    k.add(lambda bm: blob(bm, 1.0, (0, -1.0, 0), (5.8, 1.2, 0.06), 3, 0.02, seed), sand)
    return k.obj('AtlantisGate')

def build_atlantistemple(seed=3091):
    """the great temple of the sunken city (an original design): a round colonnade on a stepped base, a pale dome
    banded in gold, a glowing core seen between the columns, a few columns fallen"""
    rnd = random.Random(seed)
    k = SKit(['ceramic', 'glacier_deep', 'stone', 'gold', 'gold_dark', 'ice_light', 'frost_white', 'orange', 'red', 'obsidian_light', 'sandstone', 'khaki'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, -0.3), (13.0, 13.0, 2.0), 2, 0.3, seed, floor=False), sand)
    for st in range(3): k.add(lambda bm, st=st: lathe(bm, [(10.0 - st * 0.9, st * 0.5), (10.0 - st * 0.9, st * 0.5 + 0.5), (0.0, st * 0.5 + 0.5)], 24), aqua)
    z0 = 1.5; R = 7.0; H = 6.0
    for i in range(14):
        a = i * 2 * math.pi / 14
        if i in (3, 4): continue                       # gap: two columns fallen
        x, y = math.cos(a) * R, math.sin(a) * R
        k.add(lambda bm, x=x, y=y: lathe(bm, [(0.5, z0), (0.45, z0 + H), (0.0, z0 + H)], 12, lambda j, kk, rr, zz: (rr * (0.93 if j % 2 else 1.0), zz), 0, Matrix.Translation((x, y, 0))), aqua)
    k.add(lambda bm: lathe(bm, [(R + 0.7, z0 + H), (R + 0.7, z0 + H + 0.8), (0.0, z0 + H + 0.8)], 28), aqua)
    k.add(lambda bm: lathe(bm, [(R + 0.72, z0 + H + 0.5), (R + 0.72, z0 + H + 0.65), (0.0, z0 + H + 0.65)], 28), 'gold')
    k.add(lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=3, radius=1.0, matrix=Matrix.Translation((0, 0, z0 + H + 0.7)) @ Matrix.Diagonal((R, R, 4.5, 1))), lambda f: 'ceramic' if (f.calc_center_median().z - (z0 + H + 0.7)) % 1.6 > 0.25 else 'gold')
    k.grad(lambda bm: sphere(bm, 2.4, (0, 0, z0 + 2.6), sub=2), 'ice_grad', lambda v: min(1.0, max(0.0, (v.co.z - z0) / 5.0)), glow=True)
    for j, a in enumerate([3 * 2 * math.pi / 14, 4 * 2 * math.pi / 14]):
        p = Vector((math.cos(a) * (R + 2.5), math.sin(a) * (R + 2.5), 0.6)); d = Vector((math.cos(a + 1.2), math.sin(a + 1.2), 0))
        k.add(lambda bm, p=p, d=d: tube(bm, [p - d * 2.8, p + d * 2.8], 0.48, 0.45, 12), aqua)
    return k.obj('AtlantisTemple')

def build_sunkenship(seed=3101):
    """an old wooden sailing ship lying on the sea floor, heeled over: hull planks, a broken mast, the stern castle"""
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'wood', 'olive', 'cream', 'bone', 'gold', 'black', 'sandstone', 'khaki', 'bulb_green'])
    T = Matrix.Rotation(0.35, 4, 'X')
    L, W, H = 16.0, 4.6, 3.4
    for i in range(7):
        z = 0.3 + i * H / 7
        w = W / 2 * (0.55 + 0.45 * math.sin((i + 1) / 7 * math.pi / 2))
        for s in (-1, 1):
            pts = [T @ Vector((-L / 2, s * w * 0.75, z + 0.4)), T @ Vector((-L / 4, s * w, z)), T @ Vector((L / 4, s * w, z)), T @ Vector((L / 2, 0.0, z + 0.8))]
            k.add(lambda bm, pts=pts: tube(bm, pts, 0.32, 0.25, 5), wreck)
    k.add(lambda bm: bmesh.ops.create_cube(bm, size=1.0, matrix=T @ Matrix.Translation((-L / 2 + 1.8, 0, H + 1.2)) @ Matrix.Diagonal((3.4, W * 0.85, 2.2, 1))), 'wood')   # stern castle
    for x in (-L / 2 + 1.2, -L / 2 + 2.4):
        k.add(lambda bm, x=x: bmesh.ops.create_cube(bm, size=1.0, matrix=T @ Matrix.Translation((x, -W * 0.43, H + 1.3)) @ Matrix.Diagonal((0.6, 0.05, 0.6, 1))), 'black')
    k.add(lambda bm: tube(bm, [T @ Vector((0.5, 0, H)), T @ Vector((0.5, 0, H + 7.0))], 0.28, 0.2, 8), 'wood')
    k.add(lambda bm: tube(bm, [T @ Vector((-1.8, 0, H + 5.0)), T @ Vector((2.8, 0, H + 5.0))], 0.12, 0.1, 6), 'wood_dark')
    k.add(lambda bm: tube(bm, [T @ Vector((L / 2 - 0.2, 0, H + 0.6)), T @ Vector((L / 2 + 3.0, 0, H + 2.0))], 0.16, 0.08, 6), 'wood')   # bowsprit
    barnacles(k, [T @ Vector((rnd.uniform(-L / 3, L / 3), -W / 2 - 0.1, rnd.uniform(0.5, H))) for _ in range(14)], 2.0)
    return k.obj('SunkenShip')

def build_giantscallop(seed=3111):
    """a huge scallop shell half buried, fanned ribs, standing like a monument of the reef"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'terracotta', 'orange', 'sandstone', 'khaki'])
    def scallop(bm):
        N = 18; R = 3.0; v0f = bm.verts.new((0, -0.05, 0.2)); v0b = bm.verts.new((0, 0.25, 0.2))
        ringf = []; ringb = []
        for i in range(N + 1):
            a = math.pi * (0.08 + 0.84 * i / N); bump = 0.18 if i % 2 else 0.0
            ringf.append(bm.verts.new((math.cos(a) * R, -0.05 - bump - 0.6 * math.sin(a), 0.2 + math.sin(a) * R)))
            ringb.append(bm.verts.new((math.cos(a) * R, 0.25, 0.2 + math.sin(a) * R)))
        for i in range(N):
            bm.faces.new((v0f, ringf[i + 1], ringf[i])); bm.faces.new((v0b, ringb[i], ringb[i + 1]))
            bm.faces.new((ringf[i], ringf[i + 1], ringb[i + 1], ringb[i]))
    k.add(scallop, lambda f: 'cream' if f.normal.y < -0.3 and int(math.degrees(math.atan2(f.calc_center_median().z, f.calc_center_median().x)) / 10) % 2 == 0 else ('terracotta' if f.normal.y < -0.3 else 'bone'))
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (3.2, 1.6, 0.4), 2, 0.05, seed), sand)
    return k.obj('GiantScallop')
