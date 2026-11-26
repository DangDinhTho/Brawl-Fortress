"""Greek theme (a Mediterranean acropolis by the sea, 1960s-70s): weathered marble ruins of a Doric temple, fallen column
drums and capitals, a headless robed statue, olive trees and slim cypresses, scrub as the hiding bush, a whitewashed
island village with blue doors and shutters, terracotta pots of geraniums, an old stone olive press. Original pieces in
the chunky Brawl style, same palette as the other themes.

Walls ('#'): MarbleBlocksA/B, BrokenColumns, WhiteWall, StoneWall (one family per wall).
Small ('x'): FallenDrums, ColumnCapital, Pithoi, OlivePress.
Fence ('f'): MarbleLow.
Tall ('t'): DoricColumn, MarbleStatue, OliveTree, GreekCypress, BronzeBrazier.
Bush ('b'): MaquisGrass (Mediterranean scrub: the Brawl leaf clump, papyrus ramp).
Decor: MarbleRubble, PotteryShards, GeraniumPot.
Border: GreekCliffA/B/C (low limestone cliffs topped with ruins; the sea lies behind). Gate: GreekGate.
Backdrop: GreekTemple, WhiteVillage, Windmill, Caique."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import *
from props3 import blob
from props7 import tube
from props8 import Kit, rbox, sphere
from props16 import SKit, disc
import grass

# ------------------------------------------------------------------ colours (by facing up/down, never by the light: pieces get turned any way)
def marble(f):
    """weathered marble: pale tops, warm cream sides, a darker weathered foot (by height, not by the light)"""
    n = f.normal
    if n.z > 0.6: return 'cream'
    if n.z < -0.5 or f.calc_center_median().z < 0.28: return 'bone_dark'
    return 'bone'

def lime(f):
    """limestone rock of the acropolis: pale tops, warm sides"""
    n = f.normal
    if n.z > 0.6: return 'bone'
    return 'ash' if n.z > -0.3 else 'khaki'

def white(f): return 'snow_shade' if f.normal.z < -0.5 else 'snow'
def earth(f): return 'khaki' if f.normal.z > 0.5 else 'sandstone_dark'

def ground_skirt(k, sx, sy, seed, z=0.0, h=0.06, y=0.0):
    k.add(lambda bm: blob(bm, 1.0, (0, y, z), (sx, sy, h), 3, 0.02, seed), earth)

def block(k, x, y, z, sx, sy, sz, yaw=0.0, col=marble, r=0.04):
    k.add(lambda bm: rbox(bm, (sx, sy, sz), (x, y, z), r, yaw, segs=1), col)

def doric(k, x, y, h, r, rnd, broken=False, z0=0.0, cap=True, segs=20, tilt=None):
    """a fluted Doric column: tapering shaft (20 flutes), echinus and square abacus; broken: a ragged top"""
    M = Matrix.Translation((x, y, z0)) @ (tilt or Matrix())
    top_jit = [rnd.uniform(0.0, 0.35 * r) for _ in range(segs)]
    prof = [(r, 0.0), (r * 0.97, h * 0.35), (r * 0.91, h * 0.8), (r * 0.87, h)]
    def rim(i, kk, rr, zz):
        rr = rr * (0.89 if i % 2 else 1.0)          # flutes
        if broken and kk == len(prof) - 1: zz = zz - top_jit[i]
        return rr, zz
    k.add(lambda bm: lathe(bm, prof, segs, rim, 0, M), marble)
    if broken or not cap: return
    k.add(lambda bm: lathe(bm, [(r * 0.86, h), (r * 0.92, h + 0.04), (r * 1.18, h + 0.16), (r * 1.2, h + 0.2), (0.0, h + 0.2)], segs, None, 0, M), marble)
    k.add(lambda bm: bmesh.ops.create_cube(bm, size=1.0, matrix=M @ Matrix.Translation((0, 0, h + 0.27)) @ Matrix.Diagonal((r * 2.5, r * 2.5, 0.14, 1))), marble)   # abacus

def drum(k, x, y, z, r, L, yaw, rnd):
    """a fallen column drum lying on its side"""
    M = Matrix.Translation((x, y, z + r * 0.9)) @ Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Rotation(math.pi / 2, 4, 'Y') @ Matrix.Translation((0, 0, -L / 2))
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (r, 0.0), (r * 0.98, L), (0.0, L)], 20, lambda i, kk, rr, zz: (rr * (0.95 if i % 2 else 1.0) if 0 < kk < 3 else rr, zz), 0, M), marble)

def pot(k, x, y, z, s, col='terracotta', flowers=True, rnd=None):
    """a terracotta pot, red geraniums in it"""
    M = Matrix.Translation((x, y, z)) @ Matrix.Diagonal((s, s, s, 1))
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.13, 0.0), (0.18, 0.22), (0.21, 0.26), (0.20, 0.29), (0.0, 0.29)], 10, None, 0, M), col)
    if flowers:
        k.add(lambda bm: blob(bm, 1.0, (x, y, z + 0.27 * s), (0.22 * s, 0.22 * s, 0.17 * s), 2, 0.02 * s, int(x * 100 + y * 10) & 255, floor=False), lambda f: 'palm' if f.normal.z > 0.2 else 'palm_dark')
        rr = rnd or random.Random(int(abs(x) * 97 + abs(y) * 13))
        for j in range(5):
            a = rr.uniform(0, 6.28); d = rr.uniform(0.04, 0.16) * s
            k.add(lambda bm, a=a, d=d: sphere(bm, 0.05 * s, (x + math.cos(a) * d, y + math.sin(a) * d, z + 0.40 * s + rr.uniform(-0.03, 0.03) * s), sub=1), 'red')

# ------------------------------------------------------------------ walls
def build_marbleblocks(kind, seed, name):
    """a ruined wall of big marble ashlars, a carved frieze course (triglyph blocks), chips at its foot"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'sandstone', 'sandstone_dark', 'khaki', 'palm', 'palm_dark'])
    courses = [0.0, 0.52, 1.02, 1.52] if kind == 'A' else [0.0, 0.62, 1.20]
    for ci, z in enumerate(courses):
        h = (courses[ci + 1] - z if ci + 1 < len(courses) else 0.5) - 0.02
        x = -0.72; w0 = rnd.uniform(0.45, 0.8) if ci % 2 else 0.72
        while x < 0.70:
            w = min(w0, 0.72 - x); w0 = rnd.uniform(0.5, 0.85)
            if ci == len(courses) - 1 and rnd.random() < 0.35: x += w; continue      # missing blocks on top
            block(k, x + w / 2 + rnd.uniform(-0.02, 0.02), rnd.uniform(-0.03, 0.03), z, w - 0.03, 1.36 - ci * 0.05, h, rnd.uniform(-0.03, 0.03))
            x += w
    if kind == 'B':   # triglyph frieze on top: blocks with three vertical grooves
        for x in (-0.45, 0.25):
            block(k, x, 0.0, 1.80, 0.55, 1.2, 0.45)
            for g in (-0.15, 0.0, 0.15):
                k.add(lambda bm, x=x, g=g: rbox(bm, (0.05, 1.22, 0.36), (x + g, 0.0, 1.84), 0.01, segs=1), 'bone_dark')
    for j in range(4):
        a = rnd.uniform(0, 6.28); d = rnd.uniform(0.75, 0.9)
        k.add(lambda bm, a=a, d=d, j=j: blob(bm, 1.0, (math.cos(a) * d, math.sin(a) * d * 0.9, 0), (0.12, 0.10, 0.09), 1, 0.03, seed + j), marble)
    ground_skirt(k, 0.85, 0.85, seed)
    return k.obj(name)

def build_brokencolumns(seed=2301):
    """two broken columns on a stepped stylobate block, a drum fallen beside them"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'khaki', 'sandstone_dark'])
    block(k, 0, 0, 0, 1.44, 1.44, 0.22); block(k, 0, 0, 0.22, 1.24, 1.24, 0.2)
    doric(k, -0.30, 0.25, 2.0, 0.30, rnd, broken=True, z0=0.42)
    doric(k, 0.32, -0.25, 1.35, 0.30, rnd, broken=True, z0=0.42)
    drum(k, 0.32, 0.38, 0.42, 0.26, 0.42, 0.6, rnd)
    ground_skirt(k, 0.85, 0.85, seed)
    return k.obj('BrokenColumns')

def build_whitewall(seed=2311):
    """a whitewashed island house corner: thick white walls, a blue door and shuttered window, a stepped parapet,
    geraniums in pots on the ledge"""
    rnd = random.Random(seed)
    k = SKit(['snow', 'snow_shade', 'lapis', 'ice_deep', 'terracotta', 'palm', 'palm_dark', 'red', 'wood_dark', 'khaki', 'sandstone_dark'])
    k.add(lambda bm: rbox(bm, (1.42, 1.42, 2.05), (0, 0, 0), 0.08, segs=2), white)
    k.add(lambda bm: rbox(bm, (1.46, 1.46, 0.16), (0, 0, 2.0), 0.05, segs=2), white)                  # parapet cap
    k.add(lambda bm: rbox(bm, (0.5, 1.46, 0.22), (-0.35, 0, 2.12), 0.06, segs=2), white)               # stepped rise
    k.add(lambda bm: rbox(bm, (0.52, 0.06, 1.25), (-0.25, -0.73, 0.0), 0.03, segs=1), 'lapis')          # door
    k.add(lambda bm: rbox(bm, (0.56, 0.04, 0.08), (-0.25, -0.735, 1.25), 0.01, segs=1), 'snow_shade')
    for sx in (-1, 1):   # window shutters (side +X)
        k.add(lambda bm, sx=sx: rbox(bm, (0.06, 0.24, 0.5), (0.73, sx * 0.13 + 0.2, 1.0), 0.02, segs=1), 'ice_deep')
    k.add(lambda bm: rbox(bm, (0.03, 0.26, 0.5), (0.715, 0.2, 1.0), 0.01, segs=1), 'wood_dark')
    k.add(lambda bm: rbox(bm, (0.12, 0.6, 0.05), (0.75, 0.2, 0.95), 0.01, segs=1), 'snow_shade')      # sill
    pot(k, 0.35, -0.85, 0.0, 1.0, rnd=rnd); pot(k, 0.85, 0.55, 0.0, 0.85, rnd=rnd)
    ground_skirt(k, 0.85, 0.85, seed)
    return k.obj('WhiteWall')

def build_stonewall(seed=2321):
    """a dry-stone field wall of rough limestone, a vine trailing over it, a cap of flat stones"""
    rnd = random.Random(seed)
    k = SKit(['bone', 'sandstone', 'sandstone_dark', 'palm', 'palm_dark', 'olive', 'khaki'])
    for row in range(5):
        z = row * 0.34; x = -0.72 + (0.12 if row % 2 else 0.0)
        while x < 0.68:
            w = rnd.uniform(0.28, 0.45)
            k.add(lambda bm, x=x, w=w, z=z, row=row: rbox(bm, (w - 0.03, 1.36 - row * 0.04, 0.31), (x + w / 2, rnd.uniform(-0.04, 0.04), z), 0.07, rnd.uniform(-0.08, 0.08), segs=1), lime)
            x += w
    for x in (-0.5, -0.05, 0.4):
        k.add(lambda bm, x=x: rbox(bm, (0.5, 1.4, 0.10), (x, 0, 1.70), 0.03, rnd.uniform(-0.1, 0.1), segs=1), lime)
    pts = [Vector((-0.6, -0.7, 1.75)), Vector((-0.2, -0.72, 1.5)), Vector((0.1, -0.73, 1.1)), Vector((0.35, -0.72, 0.5)), Vector((0.4, -0.7, 0.0))]
    k.add(lambda bm: tube(bm, pts, 0.03, 0.025, 4), 'wood_dark')
    for j in range(7):
        p = pts[min(4, j // 2)] + Vector((rnd.uniform(-0.12, 0.12), -0.04, rnd.uniform(-0.12, 0.12)))
        k.add(lambda bm, p=p, j=j: blob(bm, 1.0, (p.x, p.y, p.z), (0.16, 0.08, 0.14), 1, 0.03, seed + 90 + j, floor=False), lambda f: 'palm' if f.normal.z > 0 else 'palm_dark')
    ground_skirt(k, 0.85, 0.85, seed)
    return k.obj('StoneWall')

# ------------------------------------------------------------------ small
def build_fallendrums(seed=2331):
    """two fluted column drums fallen in the grass, one leaning on the other"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'khaki', 'sandstone_dark'])
    drum(k, -0.1, -0.25, 0.0, 0.34, 0.62, 0.3, rnd)
    drum(k, 0.25, 0.32, 0.0, 0.30, 0.55, -0.5, rnd)
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.30, 0.0), (0.29, 0.30), (0.0, 0.30)], 20, lambda i, kk, rr, zz: (rr * (0.95 if i % 2 else 1.0) if 0 < kk < 3 else rr, zz), 0, Matrix.Translation((-0.45, 0.42, 0.0))), marble)
    ground_skirt(k, 0.8, 0.75, seed)
    return k.obj('FallenDrums')

def build_columncapital(seed=2341):
    """a big Doric capital fallen on the ground (echinus and abacus), a broken drum by it"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'khaki', 'sandstone_dark'])
    k.add(lambda bm: bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((0.05, 0.0, 0.11)) @ Matrix.Rotation(0.2, 4, 'Z') @ Matrix.Diagonal((1.1, 1.1, 0.22, 1))), marble)
    k.add(lambda bm: lathe(bm, [(0.55, 0.22), (0.50, 0.32), (0.38, 0.46), (0.36, 0.52), (0.0, 0.52)], 20, None, 0, Matrix.Translation((0.05, 0.0, 0.0))), marble)
    k.add(lambda bm: lathe(bm, [(0.36, 0.52), (0.35, 0.68), (0.0, 0.7)], 20, lambda i, kk, rr, zz: (rr * (0.95 if i % 2 else 1.0), zz - (rnd.uniform(0, 0.1) if kk == 2 else 0)), 0, Matrix.Translation((0.05, 0.0, 0.0))), marble)
    ground_skirt(k, 0.75, 0.75, seed)
    return k.obj('ColumnCapital')

def build_pithoi(seed=2351):
    """big terracotta storage jars (pithoi) with rope-pattern bands, one tipped over"""
    rnd = random.Random(seed)
    k = SKit(['terracotta', 'sandstone_dark', 'wood_dark', 'khaki', 'bone'])
    prof = [(0.0, 0.0), (0.16, 0.0), (0.34, 0.25), (0.40, 0.55), (0.34, 0.92), (0.18, 1.05), (0.20, 1.12), (0.0, 1.12)]
    for (x, y, s) in [(-0.30, 0.25, 1.0), (0.35, 0.30, 0.82)]:
        M = Matrix.Translation((x, y, 0)) @ Matrix.Diagonal((s, s, s, 1))
        k.add(lambda bm, M=M: lathe(bm, prof, 14, None, 0, M), 'terracotta')
        for zz in (0.45, 0.70):
            k.add(lambda bm, M=M, zz=zz: lathe(bm, [(0.41 if zz < 0.6 else 0.39, zz), (0.42 if zz < 0.6 else 0.40, zz + 0.05), (0.0, zz + 0.05)], 14, None, 0, M), 'sandstone_dark')
    M = Matrix.Translation((0.05, -0.40, 0.36)) @ Matrix.Rotation(1.2, 4, 'Z') @ Matrix.Rotation(math.pi / 2, 4, 'Y') @ Matrix.Translation((0, 0, -0.55)) @ Matrix.Diagonal((0.85, 0.85, 0.85, 1))
    k.add(lambda bm: lathe(bm, prof, 14, None, 0, M), 'terracotta')
    ground_skirt(k, 0.8, 0.8, seed)
    return k.obj('Pithoi')

def build_olivepress(seed=2361):
    """an old stone olive mill: a round basin, an upright millstone on a wooden beam, a jar and baskets"""
    rnd = random.Random(seed)
    k = SKit(['bone', 'sandstone', 'sandstone_dark', 'wood', 'wood_dark', 'terracotta', 'khaki', 'olive'])
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.68, 0.0), (0.70, 0.38), (0.58, 0.40), (0.56, 0.30), (0.0, 0.30)], 18), lime)
    M = Matrix.Translation((0.15, 0.0, 0.30 + 0.34)) @ Matrix.Rotation(math.pi / 2, 4, 'X') @ Matrix.Translation((0, 0, -0.11))
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.34, 0.0), (0.34, 0.22), (0.0, 0.22)], 16, None, 0, M), lime)
    k.add(lambda bm: tube(bm, [Vector((0, 0, 0.3)), Vector((0, 0, 1.1))], 0.07, 0.06, 6), 'wood_dark')
    k.add(lambda bm: tube(bm, [Vector((-0.6, 0.0, 0.64)), Vector((0.7, 0.0, 0.64))], 0.05, 0.05, 6), 'wood')
    pot(k, -0.65, -0.55, 0.0, 1.2, flowers=False)
    for (x, y) in [(0.6, -0.6), (-0.7, 0.55)]:
        k.add(lambda bm, x=x, y=y: lathe(bm, [(0.0, 0.0), (0.14, 0.0), (0.18, 0.18), (0.0, 0.16)], 10, None, 0, Matrix.Translation((x, y, 0))), 'khaki')
        k.add(lambda bm, x=x, y=y: blob(bm, 1.0, (x, y, 0.15), (0.13, 0.13, 0.06), 1, 0.01, seed, floor=False), 'olive')
    ground_skirt(k, 0.85, 0.85, seed)
    return k.obj('OlivePress')

# ------------------------------------------------------------------ fence
def build_marblelow(seed=2371):
    """a low ruined marble wall across the tile (low cover): a course of blocks, one toppled"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'khaki', 'sandstone_dark'])
    x = -0.74
    while x < 0.72:
        w = min(rnd.uniform(0.42, 0.62), 0.74 - x)
        block(k, x + w / 2, rnd.uniform(-0.03, 0.03), 0, w - 0.03, 0.55, rnd.uniform(0.55, 0.72), rnd.uniform(-0.04, 0.04))
        x += w
    block(k, 0.2, -0.55, 0, 0.5, 0.35, 0.3, 0.5)
    ground_skirt(k, 0.85, 0.5, seed)
    return k.obj('MarbleLow')

# ------------------------------------------------------------------ tall
def build_doriccolumn(seed=2381):
    """a standing Doric column with its capital and a fragment of architrave on top, on a square plinth"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'khaki', 'sandstone_dark'])
    block(k, 0, 0, 0, 0.95, 0.95, 0.22)
    doric(k, 0, 0, 3.0, 0.34, rnd, z0=0.22)
    block(k, 0.12, 0.0, 3.56, 1.25, 0.8, 0.36)
    for j in range(3): k.add(lambda bm, j=j: blob(bm, 1.0, (0.55 - j * 0.2, -0.55 + j * 0.15, 0), (0.10, 0.09, 0.08), 1, 0.03, seed + j), marble)
    ground_skirt(k, 0.6, 0.6, seed)
    return k.obj('DoricColumn')

def build_marblestatue(seed=2391):
    """a weathered marble statue on a plinth (an original robed figure, the head long gone): a draped body with folds,
    one arm raised, an urn at its feet"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'khaki', 'sandstone_dark'])
    block(k, 0, 0, 0, 0.95, 0.95, 0.85); block(k, 0, 0, 0.85, 1.05, 1.05, 0.12); block(k, 0, 0, -0.0, 1.05, 1.05, 0.14)
    z0 = 0.97
    k.add(lambda bm: lathe(bm, [(0.30, 0.0), (0.30, 0.05), (0.25, 0.6), (0.22, 1.0), (0.26, 1.25), (0.22, 1.4), (0.10, 1.48), (0.0, 1.5)], 16,
                             lambda i, kk, rr, zz: (rr * (1 + 0.08 * math.sin(i * 6.28 / 16 * 4)) if kk < 3 else rr, zz), 0, Matrix.Translation((0, 0, z0))), marble)   # robe with folds
    k.add(lambda bm: tube(bm, [Vector((0.22, 0, z0 + 1.30)), Vector((0.36, -0.05, z0 + 1.55)), Vector((0.40, -0.08, z0 + 1.85))], 0.07, 0.06, 6), marble)   # raised arm
    k.add(lambda bm: tube(bm, [Vector((-0.22, 0, z0 + 1.30)), Vector((-0.30, -0.05, z0 + 1.0)), Vector((-0.25, -0.12, z0 + 0.8))], 0.07, 0.06, 6), marble)
    k.add(lambda bm: tube(bm, [Vector((0.0, 0, z0 + 1.48)), Vector((0.0, 0, z0 + 1.58))], 0.06, 0.06, 6), marble)            # broken neck
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.07, 0.0), (0.12, 0.10), (0.06, 0.2), (0.08, 0.24), (0.0, 0.24)], 10, None, 0, Matrix.Translation((0.3, -0.3, z0))), marble)
    ground_skirt(k, 0.65, 0.65, seed)
    return k.obj('MarbleStatue')

def build_olivetree(seed=2401, name='OliveTree', H=3.0):
    """an old olive tree: a gnarled, twisting, split trunk and a broad silvery-green crown in clumps"""
    rnd = random.Random(seed)
    k = SKit(['wood_dark', 'wood', 'olive', 'khaki', 'palm_dark', 'sandstone_dark'])
    s = H / 3.0
    for j in range(3):   # the trunk splits in twisting stems
        a = j * 2.1 + rnd.uniform(-0.3, 0.3)
        d = Vector((math.cos(a), math.sin(a), 0))
        pts = [Vector((0, 0, 0)) + d * 0.12 * s, d * 0.05 * s + Vector((0, 0, 0.6 * s)), d * 0.35 * s + Vector((0.1, 0, 1.2 * s)), d * 0.75 * s + Vector((0, 0.1, 1.8 * s))]
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.17 * s, 0.07 * s, 7), lambda f: 'wood_dark' if f.normal.z < 0.5 else 'wood')
    k.add(lambda bm: lathe(bm, [(0.42 * s, 0.0), (0.25 * s, 0.25 * s), (0.0, 0.3 * s)], 8, lambda i, kk, rr, zz: (rr * (1 + 0.3 * math.sin(i * 2.4)), zz)), 'wood_dark')
    crown = lambda f: 'olive' if f.normal.z > 0.1 else ('khaki' if f.normal.z > -0.3 else 'palm_dark')
    for j in range(6):
        a = j * 1.05 + rnd.uniform(-0.2, 0.2); r = rnd.uniform(0.45, 0.9) * s
        c = Vector((math.cos(a) * r, math.sin(a) * r, rnd.uniform(1.85, 2.5) * s))
        k.add(lambda bm, c=c, j=j: blob(bm, 1.0, (c.x, c.y, c.z), (0.65 * s, 0.6 * s, 0.42 * s), 2, 0.08 * s, seed + j, floor=False), crown)
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 2.65 * s), (0.75 * s, 0.7 * s, 0.4 * s), 2, 0.08 * s, seed + 9, floor=False), crown)
    ground_skirt(k, 0.6 * s, 0.6 * s, seed)
    return k.obj(name)

def build_greekcypress(seed=2411, name='GreekCypress', H=3.8):
    """a slim Mediterranean cypress: a tall dark green flame shape in tufts, a short trunk"""
    rnd = random.Random(seed)
    k = SKit(['pine_dark', 'pine', 'xmas_green_dark', 'wood_dark', 'khaki', 'sandstone_dark'])
    k.add(lambda bm: tube(bm, [Vector((0, 0, 0)), Vector((0, 0, 0.5))], 0.12, 0.1, 6), 'wood_dark')
    ph = rnd.uniform(0, 6.28)
    k.add(lambda bm: lathe(bm, [(0.0, 0.35), (0.38, 0.55), (0.48, H * 0.3), (0.42, H * 0.6), (0.26, H * 0.85), (0.0, H)], 12,
                             lambda i, kk, rr, zz: (rr * (1 + 0.12 * math.sin(i * 2.7 + kk * 1.9 + ph)), zz)), lambda f: 'pine' if f.normal.z > 0.25 else 'pine_dark')
    for j in range(6):   # tufts
        z = rnd.uniform(0.25, 0.8) * H; a = rnd.uniform(0, 6.28); rr = 0.42 * (1 - abs(z / H - 0.35))
        k.add(lambda bm, z=z, a=a, rr=rr, j=j: blob(bm, 1.0, (math.cos(a) * rr, math.sin(a) * rr, z), (0.2, 0.2, 0.32), 1, 0.03, seed + j, floor=False), lambda f: 'pine' if f.normal.z > 0.25 else 'pine_dark')
    ground_skirt(k, 0.5, 0.5, seed)
    return k.obj(name)

def build_bronzebrazier(seed=2421):
    """a bronze tripod brazier with a bowl of glowing coals and flames, on a marble base"""
    rnd = random.Random(seed)
    k = SKit(['gold_dark', 'gold', 'terracotta', 'cream', 'bone', 'bone_dark', 'orange', 'yellow', 'khaki', 'sandstone_dark'])
    block(k, 0, 0, 0, 0.7, 0.7, 0.25)
    for a in range(3):
        ang = a * 2 * math.pi / 3
        k.add(lambda bm, ang=ang: tube(bm, [Vector((math.cos(ang) * 0.28, math.sin(ang) * 0.28, 0.25)), Vector((math.cos(ang) * 0.16, math.sin(ang) * 0.16, 1.2)), Vector((math.cos(ang) * 0.28, math.sin(ang) * 0.28, 1.75))], 0.03, 0.03, 5), 'gold_dark')
    k.add(lambda bm: lathe(bm, [(0.0, 1.65), (0.30, 1.72), (0.42, 1.92), (0.44, 1.96), (0.40, 1.96), (0.0, 1.90)], 14), lambda f: 'gold' if f.normal.z > 0.3 else 'gold_dark')
    k.add(lambda bm: disc(bm, 0, 0, 0.38, 1.90, rnd, 0.1), 'orange', glow=True)
    for j in range(5):
        a = j * 1.26; r = 0.14 if j else 0.0
        k.add(lambda bm, a=a, r=r, j=j: lathe(bm, [(0.10, 0.0), (0.08, 0.15), (0.0, 0.42 - j * 0.04)], 6, None, 0, Matrix.Translation((math.cos(a) * r, math.sin(a) * r, 1.92))), 'yellow' if j == 0 else 'orange', glow=True)
    ground_skirt(k, 0.5, 0.5, seed)
    return k.obj('BronzeBrazier')

def build_maquisgrass():
    """Mediterranean scrub (the hiding bush): the Brawl leaf clump with the papyrus ramp (dark green, sun-dried tips;
    the palette has no purple ramp for lavender)"""
    return grass.build_frostgrass(201, 'MaquisGrass', 'papyrus_grad', 0.75)

# ------------------------------------------------------------------ decor
def build_marblerubble(seed=2431):
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark'])
    for j in range(6):
        x, y = rnd.uniform(-0.5, 0.5), rnd.uniform(-0.4, 0.4); s = rnd.uniform(0.08, 0.2)
        k.add(lambda bm, x=x, y=y, s=s, j=j: rbox(bm, (s * 1.6, s * 1.1, s * 0.8), (x, y, 0), 0.02, rnd.uniform(0, 3), segs=1), marble)
    return k.obj('MarbleRubble')

def build_potteryshards(seed=2441):
    rnd = random.Random(seed)
    k = SKit(['terracotta', 'sandstone_dark', 'black'])
    for j in range(7):
        x, y = rnd.uniform(-0.4, 0.4), rnd.uniform(-0.3, 0.3); s = rnd.uniform(0.06, 0.14)
        k.add(lambda bm, x=x, y=y, s=s: rbox(bm, (s * 1.4, s, 0.025), (x, y, 0), 0.01, rnd.uniform(0, 3), segs=1), 'terracotta' if j % 3 else 'black')
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.10, 0.0), (0.14, 0.12), (0.12, 0.2), (0.0, 0.2)], 10, None, 0, Matrix.Translation((0.25, 0.15, 0)) @ Matrix.Rotation(1.3, 4, 'Y')), 'terracotta')
    return k.obj('PotteryShards')

def build_geraniumpot(seed=2451):
    rnd = random.Random(seed)
    k = SKit(['terracotta', 'palm', 'palm_dark', 'red', 'snow'])
    pot(k, -0.15, 0.0, 0.0, 1.0, rnd=rnd); pot(k, 0.22, 0.12, 0.0, 0.75, col='snow', rnd=rnd)
    return k.obj('GeraniumPot')

# ------------------------------------------------------------------ border, gate, backdrop
def build_greekcliff(kind, seed, name):
    """a border piece in place of the old border cliffs (7.6 m long, 5 m deep, ~4 m tall: low, the sea shows behind it):
    a layered limestone cliff edge of the acropolis. Front = -Y (+Z in Unity). 'A' a ruined colonnade on top, 'B' a
    whitewashed chapel with a blue dome, 'C' olive trees and a cypress."""
    rnd = random.Random(seed)
    k = SKit(['bone', 'sandstone', 'sandstone_dark', 'cream', 'bone_dark', 'khaki', 'olive', 'palm_dark', 'snow', 'snow_shade', 'lapis', 'ice_deep'])
    L, D = 7.6, 5.0
    k.add(lambda bm: rbox(bm, (L, D * 0.55, 2.6), (0, D * 0.18, 0), 0.3, segs=2), lime)
    for t in range(3):   # big overlapping rock masses along the face, stepping back as they rise
        n = 5 - t
        for j in range(n):
            x = -L / 2 + 0.8 + j * (L - 1.6) / max(1, n - 1) + rnd.uniform(-0.35, 0.35)
            z = t * 0.9; s = rnd.uniform(0.95, 1.25)
            k.add(lambda bm, x=x, z=z, s=s, t=t, j=j: blob(bm, 1.0, (x, -D / 2 + 1.35 + t * 0.25, z + 0.5), (s, 0.75, 0.62 + 0.1 * (t == 0)), 2, 0.12, seed + j * 5 + t, floor=False), lime)
    k.add(lambda bm: blob(bm, 1.0, (0, -D / 2 + 0.55, 0), (L * 0.49, 0.7, 0.10), 3, 0.025, seed), earth)
    top = 2.6
    if kind == 'A':
        block(k, 0, 0.5, top, L * 0.9, 1.6, 0.25)
        for j, x in enumerate([-3.0, -1.5, 0.0, 1.5, 3.0]):
            if j == 3: drum(k, x, 0.3, top + 0.25, 0.3, 0.5, 0.4, rnd); continue
            h = 2.6 if j in (1, 2) else rnd.uniform(1.0, 1.7)
            doric(k, x, 0.5, h, 0.32, rnd, broken=(h < 2.0), z0=top + 0.25)
        block(k, -0.75, 0.5, top + 0.25 + 2.6 + 0.34, 2.4, 0.8, 0.4)
    if kind == 'B':
        cx = -1.0
        k.add(lambda bm: rbox(bm, (2.2, 1.8, 1.9), (cx, 0.4, top), 0.1, segs=2), white)
        k.add(lambda bm: lathe(bm, [(0.75, 0.0), (0.75, 0.25), (0.0, 0.25)], 14, None, 0, Matrix.Translation((cx, 0.4, top + 1.9))), white)
        k.add(lambda bm: lathe(bm, [(0.72, 0.0), (0.70, 0.25), (0.5, 0.6), (0.0, 0.75)], 14, None, 0, Matrix.Translation((cx, 0.4, top + 2.15))), 'lapis')
        k.add(lambda bm: tube(bm, [Vector((cx, 0.4, top + 2.9)), Vector((cx, 0.4, top + 3.25))], 0.03, 0.03, 4), 'snow')
        k.add(lambda bm: rbox(bm, (0.24, 0.04, 0.04), (cx, 0.4, top + 3.08), 0.01, segs=1), 'snow')
        k.add(lambda bm: rbox(bm, (0.6, 0.06, 1.0), (cx, -0.52, top), 0.03, segs=1), 'lapis')                                   # door
        k.add(lambda bm: rbox(bm, (0.5, 0.5, 1.2), (cx + 1.5, 0.3, top), 0.06, segs=2), white)                                    # bell gable
        k.add(lambda bm: sphere(bm, 0.12, (cx + 1.5, 0.05, top + 0.85), sub=1), 'gold_dark')
        build_olive_inline(k, 2.2, 0.4, top, rnd, 0.75)
    if kind == 'C':
        build_olive_inline(k, -2.2, 0.3, top, rnd, 1.0)
        build_olive_inline(k, 0.6, 0.6, top, rnd, 0.85)
        k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.45, 0.2), (0.52, 1.4), (0.40, 2.6), (0.22, 3.5), (0.0, 4.0)], 12,
                                 lambda i, kk, rr, zz: (rr * (1 + 0.12 * math.sin(i * 2.7 + kk)), zz), 0, Matrix.Translation((2.6, 0.5, top))), lambda f: 'pine' if f.normal.z > 0.25 else 'pine_dark')
    return k.obj(name)

def build_olive_inline(k, x, y, z, rnd, s):
    for j in range(2):
        a = j * 2.6 + 0.4; d = Vector((math.cos(a), math.sin(a), 0))
        pts = [Vector((x, y, z)), Vector((x, y, z)) + d * 0.1 * s + Vector((0, 0, 0.8 * s)), Vector((x, y, z)) + d * 0.5 * s + Vector((0, 0, 1.5 * s))]
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.15 * s, 0.07 * s, 6), 'wood_dark')
    crown = lambda f: 'olive' if f.normal.z > 0.1 else ('khaki' if f.normal.z > -0.3 else 'palm_dark')
    for j in range(4):
        a = j * 1.6 + rnd.uniform(-0.3, 0.3); r = 0.55 * s
        k.add(lambda bm, a=a, r=r, j=j: blob(bm, 1.0, (x + math.cos(a) * r, y + math.sin(a) * r, z + 1.75 * s), (0.6 * s, 0.55 * s, 0.4 * s), 2, 0.07 * s, j + 7, floor=False), crown)

def build_greekgate(seed=2461):
    """the gateway behind each spawn (in place of the base scene's gate arch, ~10.8 m wide): a propylon of four Doric
    columns on steps, an architrave with a triglyph frieze and a pediment, potted olive trees at the feet"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'lapis', 'terracotta', 'khaki', 'sandstone_dark', 'olive', 'palm_dark', 'wood_dark', 'red', 'palm'])
    block(k, 0, 0, 0, 11.0, 2.6, 0.25); block(k, 0, 0.1, 0.25, 10.4, 2.2, 0.22)
    for x in (-4.4, -2.9, 2.9, 4.4):
        doric(k, x, 0, 5.0, 0.42, rnd, z0=0.47)
    zt = 0.47 + 5.0 + 0.34
    block(k, 0, 0, zt, 10.4, 1.3, 0.55)                                         # architrave
    k.add(lambda bm: rbox(bm, (10.42, 1.32, 0.12), (0, 0, zt + 0.55), 0.01, segs=1), 'lapis')   # painted band
    block(k, 0, 0, zt + 0.67, 10.4, 1.3, 0.6)                                   # frieze
    x = -4.9
    while x < 5.0:
        for g in (-0.1, 0.0, 0.1):
            k.add(lambda bm, x=x, g=g: rbox(bm, (0.04, 1.32, 0.5), (x + g, 0, zt + 0.72), 0.005, segs=1), 'bone_dark')
        x += 0.9
    block(k, 0, 0, zt + 1.27, 10.8, 1.5, 0.18)                                  # cornice
    def ped(bm):   # pediment: a triangular prism
        zb = zt + 1.45; W = 5.3; Hh = 1.4; y0, y1 = -0.6, 0.6
        v = [bm.verts.new((-W, y0, zb)), bm.verts.new((W, y0, zb)), bm.verts.new((0, y0, zb + Hh)),
             bm.verts.new((-W, y1, zb)), bm.verts.new((W, y1, zb)), bm.verts.new((0, y1, zb + Hh))]
        bm.faces.new((v[0], v[1], v[2])); bm.faces.new((v[5], v[4], v[3]))
        bm.faces.new((v[0], v[3], v[4], v[1])); bm.faces.new((v[1], v[4], v[5], v[2])); bm.faces.new((v[2], v[5], v[3], v[0]))
    k.add(ped, marble)
    k.add(lambda bm: lathe(bm, [(0.55, 0.0), (0.55, 0.08), (0.0, 0.08)], 16, None, 0, Matrix.Translation((0, -0.62, zt + 1.95)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), 'lapis')   # painted roundel
    for s in (-1, 1):
        pot(k, s * 5.4, -1.0, 0.0, 2.2, flowers=False)
        build_olive_inline(k, s * 5.4, -1.0, 0.6, rnd, 0.7)
    k.add(lambda bm: blob(bm, 1.0, (0, -1.1, 0), (5.8, 1.2, 0.06), 3, 0.02, seed), earth)
    return k.obj('GreekGate')

def build_greektemple(seed=2471):
    """a great ruined Doric temple on its stepped base (an original design): a peristyle of columns, some broken, an
    unbroken run of entablature with a pediment at one end, the roof long gone"""
    rnd = random.Random(seed)
    k = SKit(['cream', 'bone', 'bone_dark', 'khaki', 'sandstone', 'sandstone_dark'])
    L, W = 22.0, 10.0
    k.add(lambda bm: blob(bm, 1.0, (0, 0, -0.1), (L * 0.78, W * 1.05, 4.0), 2, 0.5, seed, floor=False), lime)   # the rock it stands on
    for st in range(3): block(k, 0, 0, st * 0.45, L + 2.4 - st * 0.8, W + 2.4 - st * 0.8, 0.45)
    z0 = 1.35; H = 6.0; r = 0.55
    xs = [-L / 2 + i * L / 9 for i in range(10)]; ys = [-W / 2 + i * W / 4 for i in range(5)]
    cols = [(x, -W / 2) for x in xs] + [(x, W / 2) for x in xs] + [(xs[0], y) for y in ys[1:-1]] + [(xs[-1], y) for y in ys[1:-1]]
    for (x, y) in cols:
        broken = (y > 0 and 2 < xs.index(x) < 7 and rnd.random() < 0.7) if x in xs else False
        h = H if not broken else rnd.uniform(1.5, 4.0)
        doric(k, x, y, h, r, rnd, broken=broken, z0=z0, segs=16)
    zt = z0 + H + 0.42
    block(k, 0, -W / 2, zt, L + 1.2, 1.3, 0.9)                                   # front entablature (whole)
    for x in (-L / 4 - 1.0, L / 2 - 2.5): block(k, x, W / 2, zt, 6.0, 1.3, 0.9)   # broken bits on the back
    for x in (-L / 2, L / 2): block(k, x, 0, zt, 1.3, W + 1.2, 0.9)
    def ped(bm):
        zb = zt + 0.9; Hh = 2.2; x0, x1 = L / 2 - 0.6, L / 2 + 0.6; Wd = W / 2 + 0.6
        v = [bm.verts.new((x0, -Wd, zb)), bm.verts.new((x0, Wd, zb)), bm.verts.new((x0, 0, zb + Hh)),
             bm.verts.new((x1, -Wd, zb)), bm.verts.new((x1, Wd, zb)), bm.verts.new((x1, 0, zb + Hh))]
        bm.faces.new((v[2], v[1], v[0])); bm.faces.new((v[3], v[4], v[5]))
        bm.faces.new((v[0], v[1], v[4], v[3])); bm.faces.new((v[1], v[2], v[5], v[4])); bm.faces.new((v[2], v[0], v[3], v[5]))
    k.add(ped, marble)
    for j in range(8):   # fallen drums round it
        a = rnd.uniform(0, 6.28); d = rnd.uniform(0.75, 0.95)
        drum(k, math.cos(a) * (L / 2 + 2.5) * d, math.sin(a) * (W / 2 + 2.5) * d, 0.0, 0.5, 0.9, rnd.uniform(0, 3), rnd)
    return k.obj('GreekTemple')

def build_whitevillage(seed=2481):
    """a cluster of whitewashed island houses stepping up a slope: cubes with blue doors and shutters, a blue-domed
    chapel, terraces, an outside stair"""
    rnd = random.Random(seed)
    k = SKit(['snow', 'snow_shade', 'lapis', 'ice_deep', 'terracotta', 'gold_dark', 'bone', 'sandstone', 'sandstone_dark', 'khaki', 'palm', 'palm_dark', 'red'])
    k.add(lambda bm: blob(bm, 1.0, (0, 1.5, 0), (8.0, 5.0, 2.5), 2, 0.2, seed), lime)   # the slope
    houses = [(-5.5, -1.5, 0.0, 2.6, 2.2, 2.2), (-2.6, -2.0, 0.0, 2.4, 2.4, 2.6), (0.4, -1.2, 0.4, 3.0, 2.2, 2.4), (3.6, -1.6, 0.0, 2.2, 2.6, 2.2),
              (-4.0, 1.4, 1.3, 2.6, 2.4, 2.4), (-0.8, 1.8, 1.8, 2.8, 2.6, 2.2), (2.6, 1.4, 1.4, 2.4, 2.2, 2.6), (5.4, 0.8, 0.6, 2.2, 2.4, 2.0)]
    for i, (x, y, z, w, d, h) in enumerate(houses):
        k.add(lambda bm, x=x, y=y, z=z, w=w, d=d, h=h: rbox(bm, (w, d, h + z), (x, y, 0), 0.1, segs=2), white)
        k.add(lambda bm, x=x, y=y, z=z, d=d: rbox(bm, (0.6, 0.06, 1.2), (x - 0.3, y - d / 2 - 0.02, z), 0.03, segs=1), 'lapis' if i % 2 else 'ice_deep')
        k.add(lambda bm, x=x, y=y, z=z, d=d, h=h: rbox(bm, (0.5, 0.06, 0.55), (x + 0.6, y - d / 2 - 0.02, z + h * 0.5), 0.02, segs=1), 'ice_deep' if i % 2 else 'lapis')
    cx, cy, cz = 0.4, -1.2, 0.4 + 2.4
    k.add(lambda bm: lathe(bm, [(0.95, 0.0), (0.95, 0.3), (0.0, 0.3)], 14, None, 0, Matrix.Translation((cx, cy, cz))), white)
    k.add(lambda bm: lathe(bm, [(0.92, 0.0), (0.9, 0.3), (0.62, 0.8), (0.0, 0.98)], 14, None, 0, Matrix.Translation((cx, cy, cz + 0.3))), 'lapis')
    k.add(lambda bm: tube(bm, [Vector((cx, cy, cz + 1.25)), Vector((cx, cy, cz + 1.7))], 0.04, 0.04, 4), 'snow')
    k.add(lambda bm: rbox(bm, (0.3, 0.05, 0.05), (cx, cy, cz + 1.5), 0.01, segs=1), 'snow')
    for j in range(6): pot(k, rnd.uniform(-6, 6), -3.3 + rnd.uniform(-0.2, 0.2), 0.0, 1.4, rnd=rnd)
    return k.obj('WhiteVillage')

def build_windmill(seed=2491):
    """an island windmill: a round whitewashed tower, a conical thatch-and-timber cap, a wheel of spars with
    triangular canvas sails"""
    rnd = random.Random(seed)
    k = SKit(['snow', 'snow_shade', 'wood', 'wood_dark', 'cream', 'lapis', 'khaki', 'sandstone_dark'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, -0.2), (4.2, 3.8, 1.6), 2, 0.3, seed, floor=False), lime)   # its rocky islet
    k.add(lambda bm: lathe(bm, [(1.7, 0.0), (1.55, 5.5), (1.6, 5.7), (0.0, 5.7)], 16), white)
    k.add(lambda bm: lathe(bm, [(1.7, 5.6), (1.2, 6.6), (0.0, 7.4)], 16), lambda f: 'wood' if f.normal.z > 0.3 else 'wood_dark')
    k.add(lambda bm: rbox(bm, (0.8, 0.08, 1.6), (0, -1.6, 0.0), 0.03, segs=1), 'lapis')
    for z in (2.2, 3.8): k.add(lambda bm, z=z: rbox(bm, (0.45, 0.08, 0.5), (0.4, -1.58, z), 0.02, segs=1), 'lapis')
    hub = Vector((0, -1.9, 6.3))
    k.add(lambda bm: tube(bm, [Vector((0, -0.8, 6.3)), hub], 0.12, 0.12, 6), 'wood_dark')
    for j in range(8):
        a = j * math.pi / 4 + 0.2
        d = Vector((math.cos(a), 0, math.sin(a)))
        tip = hub + d * 4.2
        k.add(lambda bm, tip=tip: tube(bm, [hub, tip], 0.05, 0.04, 4), 'wood')
        side = Vector((-d.z, 0, d.x))
        def sail(bm, d=d, side=side, tip=tip):
            v = [bm.verts.new(hub + d * 0.8), bm.verts.new(tip), bm.verts.new(hub + d * 3.6 + side * 1.0)]
            bm.faces.new(v); bm.faces.new([bm.verts.new(x.co + Vector((0, 0.02, 0))) for x in reversed(v)])
        k.add(sail, 'cream')
    return k.obj('Windmill')

def build_caique(seed=2501):
    """a small wooden fishing caique (as it lies on the sea): a double-ended painted hull, a short mast, a little
    wheelhouse"""
    rnd = random.Random(seed)
    k = SKit(['snow', 'lapis', 'ice_deep', 'red', 'wood', 'wood_dark', 'wood_light', 'gold', 'cream'])
    L, W, H = 6.0, 2.0, 1.1
    NS, NA = 14, 8
    P = []
    for i in range(NS + 1):
        x = -L / 2 + L * i / NS; u = abs(x) / (L / 2)
        w = max(0.04, W / 2 * (1 - u ** 2.2) ** 0.6); hh = H * (1 + 0.35 * u ** 2)
        row = []
        for j in range(NA + 1):
            a = -math.pi / 2 + math.pi * j / NA
            row.append(Vector((x, w * math.sin(a), hh * (1.0 - max(0.0, math.cos(a)) ** 0.6))))
        P.append(row)
    def hull(bm, j0, j1):
        V = [[bm.verts.new(P[i][j]) for j in range(NA + 1)] for i in range(NS + 1)]
        for i in range(NS):
            for j in range(j0, j1): bm.faces.new((V[i][j], V[i][j + 1], V[i + 1][j + 1], V[i + 1][j]))
    for j in range(NA):
        col = 'lapis' if j in (0, NA - 1) else ('red' if j in (1, NA - 2) else 'snow')
        k.add(lambda bm, j=j: hull(bm, j, j + 1), col)
    def deck(bm):
        vs = [bm.verts.new(P[i][0]) for i in range(NS + 1)] + [bm.verts.new(P[i][NA]) for i in reversed(range(NS + 1))]
        bm.faces.new(vs)
    k.add(deck, 'wood_light')
    k.add(lambda bm: rbox(bm, (1.2, 1.1, 0.9), (-1.0, 0, H), 0.06, segs=1), 'snow')
    k.add(lambda bm: rbox(bm, (1.3, 1.2, 0.08), (-1.0, 0, H + 0.9), 0.02, segs=1), 'ice_deep')
    k.add(lambda bm: tube(bm, [Vector((0.8, 0, H)), Vector((0.8, 0, H + 2.6))], 0.06, 0.05, 6), 'wood')
    k.add(lambda bm: tube(bm, [Vector((0.8, 0, H + 2.0)), Vector((2.4, 0, H + 0.8))], 0.04, 0.04, 5), 'wood')
    return k.obj('Caique')
