"""Volcano theme, second set: more varied rock and things to look at (props11.py has the first set and the helpers).

Walls ('#'), one family per wall in the map builder:  BasaltBouldersA/B (rounded boulders, molten seams),
BasaltSlabsA/B (tilted broken slabs, obsidian between), ObsidianWall (a basalt block split by a big obsidian vein),
TuffRockA/B (layered red-brown volcanic tuff: warm colour among the dark basalt).
Tall ('t'): MagmaPillar (rock pillar with glowing veins and a lava pool on top), BurntPine (charred conifer).
Small ('x'): LavaBombs (round cooled lava bombs), MineCart (a scorched 1960s ore cart full of glowing ore).
Bushes ('b'): ScorchGrassA/B: burnt grass clumps (black leaves, ember tips; grass.build_frostgrass with 'char_grad').
Decor: CooledFlow (a tongue of cooling lava on the floor), EmberTuft (a small charred tuft, not a hiding bush)."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import *
from props3 import blob
from props7 import tube
from props8 import rbox, sphere
from props11 import VKit, bas, obs, hexcol, shard, ground_crack, puddle, ash_heap
import grass

common.FACETED = tuple(common.FACETED) + ('BasaltSlabsA', 'BasaltSlabsB', 'ObsidianWall', 'TuffRockA', 'TuffRockB')

def tuff(f):
    """volcanic tuff: warm red-brown, darker strata on the shaded side, pale ash on top"""
    n = f.normal
    if n.z > 0.75: return 'ash'
    return 'terracotta' if lit(n) > -0.05 else 'wood_dark'

# ------------------------------------------------------------------ walls
def build_boulders(kind, seed, name):
    """rounded basalt boulders piled up, molten rock glowing in the seams between them"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    if kind == 'A':   # two at the foot, one on top
        rocks = [(-0.32, -0.05, 0.40, (0.42, 0.40, 0.44)), (0.33, 0.08, 0.38, (0.40, 0.42, 0.40)), (0.0, 0.0, 1.10, (0.40, 0.38, 0.36)), (0.05, -0.38, 0.22, (0.26, 0.22, 0.22))]
    else:             # one big boulder with a smaller one leaning on it
        rocks = [(0.0, 0.05, 0.62, (0.62, 0.56, 0.62)), (-0.42, -0.35, 0.30, (0.32, 0.30, 0.30)), (0.40, -0.30, 0.22, (0.24, 0.24, 0.22))]
    k.lava(lambda bm: sphere(bm, 0.45, (0, 0, 0.62), (1.2, 1.0, 1.3), 2), 0.62)            # the hot core showing in the seams
    for j, (x, y, z, s) in enumerate(rocks):
        k.add(lambda bm, x=x, y=y, z=z, s=s, j=j: blob(bm, 1.0, (x, y, z), s, 2, 0.06, seed + j), bas)
    for j in range(2):
        a = rnd.uniform(0, 2 * math.pi)
        ground_crack(k, rnd, (math.cos(a) * 0.6, math.sin(a) * 0.6, 0), a, rnd.uniform(0.3, 0.5), 0.08, 3)
    return k.obj(name)

def build_slabs(kind, seed, name):
    """platy basalt: thick broken plates of old lava flows stacked a little askew, molten rock glowing in the gaps between
    them, an obsidian shard pushing out of a crack"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash', 'obsidian', 'obsidian_light'])
    z = 0.0; n = 4 if kind == 'A' else 3
    for i in range(n):
        h = rnd.uniform(0.40, 0.55) * (1.2 if kind == 'B' else 1.0)
        sx, sy = 1.38 - i * rnd.uniform(0.04, 0.1), 1.28 - i * rnd.uniform(0.04, 0.1)
        ox, oy, yaw = rnd.uniform(-0.06, 0.06), rnd.uniform(-0.06, 0.06), rnd.uniform(-0.14, 0.14)
        def plate(bm, sx=sx, sy=sy, ox=ox, oy=oy, yaw=yaw, z=z, h=h):
            M = Matrix.Translation((ox, oy, z + h / 2)) @ Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Diagonal((sx, sy, h, 1))
            vs = bmesh.ops.create_cube(bm, size=1.0, matrix=M)['verts']
            for v in vs:   # broken, uneven corners
                if v.co.z > z + h / 2: v.co.z -= rnd.uniform(0.0, 0.16)
            edges = list({e for v in vs for e in v.link_edges})
            bmesh.ops.bevel(bm, geom=edges, offset=0.06, offset_type='OFFSET', segments=1, profile=0.5, affect='EDGES', clamp_overlap=True)
        k.add(plate, bas)
        if i < n - 1:   # the glowing seam: a thinner hot layer, set in, showing in the gap above this plate
            k.lava(lambda bm, sx=sx, sy=sy, ox=ox, oy=oy, z=z, h=h: rbox(bm, (sx * 0.9, sy * 0.9, 0.12), (ox, oy, z + h - 0.12), 0.02, segs=1), 0.62)
        z += h + 0.03
    for (x, y, r, hh, lx, ly) in [(0.62, -0.35, 0.16, 1.1, 0.18, -0.1), (0.66, 0.05, 0.12, 0.75, 0.2, 0.05)] if kind == 'A' else [(-0.6, 0.4, 0.15, 1.0, -0.16, 0.1)]:
        k.add(lambda bm, x=x, y=y, r=r, hh=hh, lx=lx, ly=ly: shard(bm, x, y, r, hh, (lx, ly), rnd), obs)
    ground_crack(k, rnd, (0.0, -0.7, 0), -math.pi / 2 + rnd.uniform(-0.4, 0.4), 0.45, 0.08, 3)
    return k.obj(name)

def build_obsidianwall(seed=1311):
    """a basalt block split open by a big vein of obsidian crystal rising out of it"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash', 'obsidian', 'obsidian_light'])
    for (x, w) in [(-0.45, 0.52), (0.45, 0.52)]:
        k.add(lambda bm, x=x, w=w: rbox(bm, (w, 1.36, 1.25 + rnd.uniform(-0.1, 0.15)), (x, 0, 0), 0.08, rnd.uniform(-0.05, 0.05)), bas)
    for (x, y, r, h, lx, ly) in [(0.0, 0.0, 0.34, 2.35, 0.02, 0.03), (0.05, 0.40, 0.24, 1.75, 0.03, 0.15), (-0.04, -0.42, 0.22, 1.55, -0.02, -0.16), (0.12, 0.10, 0.18, 1.2, 0.2, 0.0)]:
        k.add(lambda bm, x=x, y=y, r=r, h=h, lx=lx, ly=ly: shard(bm, x, y, r, h, (lx, ly), rnd, sides=6), obs)
    k.lava(lambda bm: rbox(bm, (0.36, 1.30, 0.05), (0, 0, 0.0), 0.02, segs=1), 0.65)          # glow at the foot of the split
    return k.obj('ObsidianWall')

def build_tuff(kind, seed, name):
    """a weathered outcrop of red-brown volcanic tuff: a lumpy rock (noise-displaced sphere), flat-topped, its strata
    stepping in as ledges with thin dark bands between, a fallen chunk at the foot"""
    rnd = random.Random(seed)
    k = VKit(['terracotta', 'wood_dark', 'ash', 'sandstone_dark'])
    H = 2.1 if kind == 'A' else 1.8
    NB = 4
    def rock(bm):
        # two lumps of different heights grown together (a single one read as a clay pot)
        bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((-0.14, 0.04, H * 0.5)) @ Matrix.Diagonal((0.62, 0.60, H * 0.62, 1)))
        bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((0.30, -0.12, H * 0.30)) @ Matrix.Diagonal((0.48, 0.50, H * 0.40, 1)))
        for v in bm.verts:
            z = v.co.z
            if z > H * 0.90: v.co.z = H * 0.90 + (v.co.z - H * 0.90) * 0.15 + v.co.x * (0.12 if kind == 'A' else -0.1)   # flat, slanted top
            if z < 0.05: v.co.z = 0.0
            b = min(NB - 1, int(max(0.0, v.co.z) / (H / NB)))
            ledge = 1.0 + (0.13 if b % 2 == 0 else -0.07)
            n = noise.noise(Vector((v.co.x * 1.6 + seed * 0.1, v.co.y * 1.6, v.co.z * 0.9)))
            r = Vector((v.co.x, v.co.y, 0)) * (1.0 - 0.18 * max(0.0, v.co.z) / H)   # broader at the foot
            v.co.x = r.x * ledge * (1 + 0.32 * n); v.co.y = r.y * ledge * (1 + 0.32 * n)
            v.co.x = max(-0.74, min(0.74, v.co.x)); v.co.y = max(-0.74, min(0.74, v.co.y))   # stay inside the tile
    def band_col(f):
        if f.normal.z > 0.70: return 'ash'
        l = lit(f.normal)                                     # the strata read from the ledges' light and shade
        return 'terracotta' if l > -0.05 else ('sandstone_dark' if l > -0.35 else 'wood_dark')
    k.add(rock, band_col)
    for (x, y, s) in ([(0.62, -0.50, 0.26), (-0.55, -0.48, 0.18)] if kind == 'B' else [(0.55, -0.55, 0.22)]):
        k.add(lambda bm, x=x, y=y, s=s: blob(bm, 1.0, (x, y, 0), (s * 1.3, s, s * 0.9), 1, s * 0.2, seed + 7), lambda f: 'terracotta' if lit(f.normal) > -0.1 else 'wood_dark')
    ash_heap(k, 0.85, 0.8, seed, h=0.08)
    return k.obj(name)

# ------------------------------------------------------------------ tall
def build_magmapillar(seed=1321):
    """a rough rock pillar veined with glowing magma, a little lava pool brimming on its top"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    H = 2.7; S = 7
    def pillar(bm):
        rings = []
        for i in range(6):
            z = H * i / 5; r = 0.55 - 0.12 * i / 5 + (0.06 if i == 5 else 0)
            a0 = rnd.uniform(0, 0.3)
            rings.append([bm.verts.new((math.cos(a0 + j * 2 * math.pi / S) * r * rnd.uniform(0.88, 1.08), math.sin(a0 + j * 2 * math.pi / S) * r * rnd.uniform(0.88, 1.08), z)) for j in range(S)])
        for i in range(5):
            for j in range(S): bm.faces.new((rings[i][j], rings[i][(j + 1) % S], rings[i + 1][(j + 1) % S], rings[i + 1][j]))
        bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    k.add(pillar, bas)
    for v in range(3):   # veins winding up the sides
        a0 = v * 2.1 + 0.4
        pts = [Vector((math.cos(a0 + t * 1.6) * (0.56 - 0.1 * t), math.sin(a0 + t * 1.6) * (0.56 - 0.1 * t), 0.15 + t * (H - 0.4))) for t in [i / 6 for i in range(7)]]
        k.lava(lambda bm, pts=pts: tube(bm, pts, 0.045, 0.03, 4), lambda vv: 0.5 + 0.35 * min(1.0, vv.co.z / H))
    k.lava(lambda bm: lathe(bm, [(0.40, H + 0.01), (0.0, H + 0.01)], 10), 0.85)              # lava pool on top
    for j in range(3):
        a = j * 2.2; s = rnd.uniform(0.18, 0.28)
        k.add(lambda bm, a=a, s=s, j=j: blob(bm, 1.0, (math.cos(a) * 0.62, math.sin(a) * 0.62, 0), (s * 1.3, s, s * 0.8), 1, s * 0.25, seed + j), bas)
    return k.obj('MagmaPillar')

def build_burntpine(seed=1331):
    """a charred conifer: a black trunk and tiers of broken drooping branches, a few embers still glowing"""
    rnd = random.Random(seed)
    k = VKit(['black', 'basalt_dark', 'ash'])
    charc = lambda f: 'basalt_dark' if lit(f.normal) > 0.1 else 'black'
    H = 3.1
    k.add(lambda bm: tube(bm, [Vector((0, 0, 0)), Vector((0.03, 0.02, H * 0.5)), Vector((0.0, 0.05, H))], 0.20, 0.03, 7), charc)
    for tier in range(5):
        z = 0.9 + tier * 0.45; L = 0.85 - tier * 0.13
        for j in range(5):
            if rnd.random() < 0.25: continue   # broken off
            az = j * 2 * math.pi / 5 + tier * 0.6 + rnd.uniform(-0.2, 0.2)
            d = Vector((math.cos(az), math.sin(az), 0))
            Lj = L * rnd.uniform(0.6, 1.0)
            pts = [Vector((0, 0, z)), Vector((0, 0, z)) + d * Lj * 0.55 + Vector((0, 0, -0.06)), Vector((0, 0, z)) + d * Lj + Vector((0, 0, -0.22))]
            k.add(lambda bm, pts=pts: tube(bm, pts, 0.05, 0.012, 4), charc)
            if rnd.random() < 0.3:
                k.lava(lambda bm, p=pts[-1]: sphere(bm, 0.035, (p.x, p.y, p.z), sub=1), 0.8)
    k.lava(lambda bm: tube(bm, [Vector((0.19, 0, 0.3)), Vector((0.17, 0.03, 0.75))], 0.025, 0.02, 4), 0.7)   # a glowing split in the bark
    ash_heap(k, 0.6, 0.55, seed, h=0.14)
    return k.obj('BurntPine')

# ------------------------------------------------------------------ small
def build_lavabombs(seed=1341):
    """three round lava bombs fallen from an eruption: cracked dark crust over a glowing core"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    for (x, y, r) in [(-0.28, -0.12, 0.42), (0.36, 0.10, 0.34), (0.0, 0.42, 0.26)]:
        k.lava(lambda bm, x=x, y=y, r=r: sphere(bm, r * 0.94, (x, y, r * 0.9), (1, 1, 0.92), 2), 0.62)
        for j in range(6):   # crust plates, gaps between them glow
            a = j * math.pi / 3 + rnd.uniform(-0.2, 0.2); e = rnd.uniform(-0.6, 0.9)
            c = Vector((math.cos(a) * math.cos(e), math.sin(a) * math.cos(e), math.sin(e))) * r * 0.62
            k.add(lambda bm, x=x, y=y, r=r, c=c, j=j: blob(bm, 1.0, (x + c.x, y + c.y, r * 0.9 + c.z), (r * 0.62, r * 0.62, r * 0.55), 1, r * 0.08, seed + j, floor=False), bas)
        k.add(lambda bm, x=x, y=y, r=r: blob(bm, 1.0, (x, y, r * 1.45), (r * 0.55, r * 0.55, r * 0.35), 1, r * 0.06, seed + 9, floor=False), bas)   # cap
    ground_crack(k, rnd, (0.6, -0.3, 0), -0.4, 0.4, 0.07, 3)
    return k.obj('LavaBombs')

def build_minecart(seed=1351):
    """a 1960s ore cart, scorched and abandoned, its tub full of glowing ore, on a stub of rail"""
    rnd = random.Random(seed)
    k = VKit(['metal', 'black', 'terracotta', 'wood_dark', 'basalt', 'basalt_dark', 'ash'])
    ironc = lambda f: 'metal' if lit(f.normal) > 0.25 else ('terracotta' if lit(f.normal) > -0.2 else 'black')   # rusty, burnt
    for y in (-0.36, 0.36):   # rail stub on sleepers
        k.add(lambda bm, y=y: rbox(bm, (1.45, 0.06, 0.07), (0, y, 0.10), 0.01, segs=1), 'metal')
    for x in (-0.5, 0.0, 0.5):
        k.add(lambda bm, x=x: rbox(bm, (0.16, 1.0, 0.10), (x, 0, 0.0), 0.02, segs=1), 'wood_dark')
    for x in (-0.32, 0.32):
        for y in (-0.36, 0.36):
            k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y - 0.04, 0.32)), Vector((x, y + 0.04, 0.32))], 0.15, 0.15, 10), 'black')
    def tub(bm):
        vs = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((0, 0, 0.72)) @ Matrix.Diagonal((1.05, 0.72, 0.55, 1)))['verts']
        for v in vs:
            if v.co.z > 0.72: v.co.x *= 1.15; v.co.y *= 1.12   # flared
        edges = list({e for v in vs for e in v.link_edges})
        bmesh.ops.bevel(bm, geom=edges, offset=0.04, offset_type='OFFSET', segments=1, profile=0.5, affect='EDGES', clamp_overlap=True)
    k.add(tub, ironc)
    k.add(lambda bm: rbox(bm, (1.25, 0.88, 0.05), (0, 0, 0.98), 0.02, segs=1), 'black')        # rim
    k.lava(lambda bm: blob(bm, 1.0, (0, 0, 0.98), (0.52, 0.36, 0.16), 2, 0.03, seed, floor=False), 0.7)   # glowing ore
    for j in range(5):
        x, y = rnd.uniform(-0.4, 0.4), rnd.uniform(-0.25, 0.25)
        k.add(lambda bm, x=x, y=y, j=j: blob(bm, 1.0, (x, y, 1.06), (0.12, 0.10, 0.09), 1, 0.02, seed + j, floor=False), bas)
    return k.obj('MineCart')

def build_cooledflow(seed=1371):
    """decor: a tongue of cooling lava on the floor, lumpy dark crust cracked over a glowing inside, hot at its tip"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    pts = [Vector((-0.9 + i * 0.3, 0.12 * math.sin(i * 1.1), 0)) for i in range(7)]
    k.lava(lambda bm: puddle(bm, 0.0, 0.0, 1.0, 0.36, 0.02, rnd, 14), 0.55)
    for i, p in enumerate(pts):
        s = 0.34 - 0.025 * i
        k.add(lambda bm, p=p, s=s, i=i: blob(bm, 1.0, (p.x, p.y, 0), (s * 1.2, s, 0.13), 2, 0.03, seed + i), lambda f: 'basalt' if f.normal.z > 0.3 else 'basalt_dark')
    k.lava(lambda bm: blob(bm, 1.0, (0.95, 0.05, 0), (0.22, 0.18, 0.10), 2, 0.02, seed + 20), 0.85)   # the hot tip
    for i in (1, 3, 5):
        ground_crack(k, rnd, (pts[i].x, pts[i].y - 0.2, 0.12), math.pi / 2 + rnd.uniform(-0.3, 0.3), 0.4, 0.05, 3)
    return k.obj('CooledFlow')

# ------------------------------------------------------------------ bushes
def build_scorchgrass(kind):
    """burnt grass bush: the Brawl-style leaf clump with the charred ramp (black root, brown, ember tips)"""
    return grass.build_frostgrass(131 if kind == 'A' else 137, 'ScorchGrass' + kind, 'char_grad', 1.0 if kind == 'A' else 0.72)

def build_embertuft():
    """decor: the same charred clump, placed small (0.3 x) as a tuft on the floor; not a hiding bush (no 'grass' in the name)"""
    return grass.build_frostgrass(141, 'EmberTuft', 'char_grad', 0.8)
