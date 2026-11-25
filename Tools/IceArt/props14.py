"""Circus / fairground theme (1960s-70s): a travelling circus and funfair on a field. Striped canvas, painted show
wagons with gilt trim, steamer trunks, hay bales, elephant tubs, a circus cannon, a popcorn cart, a high striker, a
ticket booth, incandescent globe lamps and C9 bulb strings, balloons. Same palette and chunky style as the other themes.
Era check: painted wood and canvas, enamel and brass, incandescent bulbs; no plastic, no LED, no modern rides.

Walls ('#'): CircusWagonA/B, TrunkStack, HayStack.
Small ('x'): StarTubs, PopcornCart, CircusCannon.
Fence ('f'): RopeBarrier.
Tall ('t'): HighStriker, TicketBooth, CarnivalLamp, BalloonPost.
Bush ('b'): FairGrass.
Decor: HayPile, JugglingPins, PopcornSpill.
Border: CircusWallA/B/C (canvas sidewall of the big top). Gate: CircusGate.
Backdrop: BigTop, FerrisWheel, Carousel."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import *
from props3 import blob
from props7 import tube
from props8 import Kit, rbox, sphere, string_lights
import grass

RED, CREAM, GOLD, BLUE = 'xmas_red', 'cream', 'gold', 'lapis'

def paint(col, dark): return lambda f: col if lit(f.normal) > -0.1 else dark
def woodc(f): return 'wood' if lit(f.normal) > -0.15 else 'wood_dark'

def stripes(n, a, b, cx=0.0, cy=0.0, a0=0.0):
    """colour by angle round (cx, cy): n alternating stripes"""
    def c(f):
        p = f.calc_center_median(); t = (math.atan2(p.y - cy, p.x - cx) - a0) % (2 * math.pi)
        return a if int(t / (2 * math.pi / n)) % 2 == 0 else b
    return c

def xstripes(w, a, b, x0=0.0):
    """colour by x position: vertical stripes w wide (flat canvas walls)"""
    return lambda f: a if int(math.floor((f.calc_center_median().x - x0) / w)) % 2 == 0 else b

def pbox(bm, size, loc, yaw=0.0):
    sx, sy, sz = size
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((loc[0], loc[1], loc[2] + sz / 2)) @ Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Diagonal((sx, sy, sz, 1)))

def star(bm, cx, cy, cz, r, axis='Y', depth=0.03, n=5):
    """a five-point star plate facing -axis (a flat extruded star)"""
    pts = []
    for i in range(n * 2):
        a = math.pi / 2 + i * math.pi / n; rr = r if i % 2 == 0 else r * 0.45
        pts.append((math.cos(a) * rr, math.sin(a) * rr))
    if axis == 'Y':
        f = [bm.verts.new((cx + u, cy, cz + v)) for u, v in pts]; b = [bm.verts.new((cx + u, cy + depth, cz + v)) for u, v in pts]
    else:
        f = [bm.verts.new((cx + u, cy + v, cz + depth)) for u, v in pts]; b = [bm.verts.new((cx + u, cy + v, cz)) for u, v in pts]
    bm.faces.new(list(reversed(f)) if axis == 'Y' else f); bm.faces.new(b if axis == 'Y' else list(reversed(b)))
    m = len(pts)
    for i in range(m): bm.faces.new((f[i], f[(i + 1) % m], b[(i + 1) % m], b[i]))

def wheel(k, x, y, z, r, col, axis_y=True, spokes=8, w=0.08):
    """a spoked wooden wheel with an iron tyre; axle along Y"""
    M = Matrix.Translation((x, y, z)) @ Matrix.Rotation(math.pi / 2, 4, 'X') @ Matrix.Translation((0, 0, -w / 2))
    k.add(lambda bm: lathe(bm, [(r - 0.06, 0.0), (r, 0.0), (r, w), (r - 0.06, w)], 16, None, 0, M), 'black')
    k.add(lambda bm: lathe(bm, [(r - 0.12, 0.005), (r - 0.06, 0.005), (r - 0.06, w - 0.005), (r - 0.12, w - 0.005)], 16, None, 0, M), col)
    k.add(lambda bm: lathe(bm, [(0.08, -0.02), (0.08, w + 0.02), (0.0, w + 0.02)], 8, None, 0, M), col)
    for i in range(spokes):
        a = i * 2 * math.pi / spokes
        k.add(lambda bm, a=a: tube(bm, [Vector((x, y, z)), Vector((x + math.cos(a) * (r - 0.1), y, z + math.sin(a) * (r - 0.1)))], 0.022, 0.018, 4), col)

def grass_ring(k, sx, sy, seed, h=0.07):
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (sx, sy, h), 3, 0.02, seed), lambda f: 'khaki' if f.normal.z > 0.4 else 'olive')

# ------------------------------------------------------------------ walls
def build_wagon(kind, seed, name):
    """a painted circus show wagon (a cage wagon): body on a chassis, four spoked wheels, gilt scroll trim, a rounded
    roof with a gilt rail; barred windows on the long sides"""
    rnd = random.Random(seed)
    body, dark, trim = (RED, 'xmas_red_dark', GOLD) if kind == 'A' else (BLUE, 'black', 'yellow')
    k = Kit([body, dark, trim, 'gold_dark', 'black', 'wood', 'wood_dark', 'cream', 'khaki', 'olive'])
    bc = paint(body, dark)
    for x in (-0.48, 0.48):
        for y in (-0.62, 0.62): wheel(k, x, y, 0.36, 0.36, trim)
    k.add(lambda bm: rbox(bm, (1.36, 1.02, 0.10), (0, 0, 0.42), 0.02, segs=1), 'wood_dark')                  # chassis
    k.add(lambda bm: rbox(bm, (1.40, 1.06, 1.15), (0, 0, 0.52), 0.06), bc)                                    # body
    k.add(lambda bm: rbox(bm, (1.52, 1.20, 0.14), (0, 0, 1.67), 0.06), trim)                                  # cornice
    k.add(lambda bm: rbox(bm, (1.44, 1.10, 0.28), (0, 0, 1.81), 0.14, segs=3), bc)                            # rounded roof
    for side in (-1, 1):   # barred window on each long side, gilt frame and corner scrolls
        y = side * 0.535
        k.add(lambda bm, y=y: pbox(bm, (0.95, 0.04, 0.62), (0, y, 0.78)), 'black')
        for i in range(7): k.add(lambda bm, i=i, y=y: tube(bm, [Vector((-0.42 + i * 0.14, y + side * 0.02, 0.78)), Vector((-0.42 + i * 0.14, y + side * 0.02, 1.40))], 0.018, 0.018, 4), trim)
        for (z0, z1) in ((0.74, 0.80), (1.40, 1.46)): k.add(lambda bm, y=y, z0=z0, z1=z1: pbox(bm, (1.05, 0.05, z1 - z0), (0, y + side * 0.01, z0)), trim)
        for x in (-0.6, 0.6): k.add(lambda bm, x=x, y=y: sphere(bm, 0.07, (x, y + side * 0.03, 1.10), (1, 0.4, 1.6), 1), trim)
    for side in (-1, 1): k.add(lambda bm, side=side: pbox(bm, (0.04, 0.70, 0.60), (side * 0.71, 0, 0.80)), trim)   # end panels
    for side in (-1, 1): k.add(lambda bm, side=side: sphere(bm, 0.11, (side * 0.735, 0.0, 1.10), (0.3, 1, 1), 1), body)
    grass_ring(k, 0.85, 0.75, seed)
    return k.obj(name)

def build_trunkstack(seed=1711):
    """steamer trunks and prop boxes stacked high: painted canvas-covered wood, brass corners, leather straps"""
    rnd = random.Random(seed)
    k = Kit([RED, 'xmas_red_dark', 'turquoise', BLUE, 'yellow', 'gold_dark', GOLD, 'wood_dark', 'black', 'khaki', 'olive'])
    def trunk(x, y, z, sx, sy, sz, yaw, col, dark):
        k.add(lambda bm: rbox(bm, (sx, sy, sz), (x, y, z), 0.04, yaw), paint(col, dark))
        for zz in (z + 0.04, z + sz - 0.07): k.add(lambda bm, zz=zz: rbox(bm, (sx + 0.02, sy + 0.02, 0.04), (x, y, zz), 0.01, yaw, segs=1), 'wood_dark')
        R = Matrix.Rotation(yaw, 4, 'Z')
        for (u, v) in [(-1, -1), (1, -1), (1, 1), (-1, 1)]:
            p = R @ Vector((u * sx / 2, v * sy / 2, 0))
            k.add(lambda bm, p=p: sphere(bm, 0.045, (x + p.x, y + p.y, z + sz), sub=1), GOLD)
        p = R @ Vector((0, -sy / 2 - 0.012, 0))
        k.add(lambda bm, p=p: pbox(bm, (0.10, 0.02, 0.10), (x + p.x, y + p.y, z + sz * 0.55), yaw), GOLD)       # lock
    trunk(-0.20, -0.10, 0.0, 1.30, 0.80, 0.62, 0.05, RED, 'xmas_red_dark')
    trunk(0.30, 0.38, 0.0, 0.80, 0.55, 0.55, -0.2, 'turquoise', BLUE)
    trunk(-0.15, -0.05, 0.62, 1.05, 0.70, 0.55, -0.12, 'yellow', 'gold_dark')
    trunk(0.05, 0.05, 1.17, 0.75, 0.55, 0.45, 0.25, BLUE, 'black')
    grass_ring(k, 0.85, 0.8, seed)
    return k.obj('TrunkStack')

def build_haystack(seed=1721):
    """hay bales stacked for the animals, twine bound, loose hay at the foot"""
    rnd = random.Random(seed)
    k = Kit(['khaki', 'olive', 'wood_dark', 'yellow'])
    hayc = lambda f: 'khaki' if lit(f.normal) > -0.15 else 'olive'
    def bale(x, y, z, yaw):
        k.add(lambda bm: rbox(bm, (0.95, 0.48, 0.42), (x, y, z), 0.07, yaw), hayc)
        for u in (-0.22, 0.22):
            R = Matrix.Rotation(yaw, 4, 'Z'); p = R @ Vector((u, 0, 0))
            k.add(lambda bm, p=p: rbox(bm, (0.03, 0.50, 0.44), (x + p.x, y + p.y, z - 0.01), 0.01, yaw, segs=1), 'wood_dark')
    for (x, y) in [(-0.25, -0.40), (-0.25, 0.10), (0.30, -0.15), (0.30, 0.42)]: bale(x, y, 0.0, rnd.uniform(-0.08, 0.08) + (0 if x < 0 else math.pi / 2))
    for (x, y) in [(-0.05, -0.18), (0.10, 0.30)]: bale(x, y, 0.42, rnd.uniform(-0.3, 0.3))
    bale(0.0, 0.05, 0.84, 1.3)
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.85, 0.85, 0.10), 3, 0.04, seed), lambda f: 'khaki' if f.normal.z > 0.5 else 'olive')
    return k.obj('HayStack')

# ------------------------------------------------------------------ small
def build_startubs(seed=1731):
    """elephant tubs: two striped drum pedestals with gold stars, one standing on the other's edge"""
    k = Kit([RED, CREAM, BLUE, GOLD, 'gold_dark', 'khaki', 'olive'])
    for (x, y, r, h, a, b) in [(-0.18, -0.05, 0.50, 0.62, RED, CREAM), (0.38, 0.30, 0.34, 0.95, BLUE, CREAM)]:
        M = Matrix.Translation((x, y, 0))
        k.add(lambda bm, M=M, r=r, h=h: lathe(bm, [(r, 0.0), (r * 0.86, h * 0.5), (r, h), (0.0, h)], 16, None, 0, M), stripes(8, a, b, x, y))
        k.add(lambda bm, x=x, y=y, r=r, h=h: add_cone(bm, 16, r * 0.97, r * 0.97, 0.10, Matrix.Translation((x, y, h + 0.03))), CREAM)   # the painted top (a closed puck)
        for z in (0.0, h - 0.07): k.add(lambda bm, M=M, r=r, z=z: lathe(bm, [(r + 0.02, z), (r + 0.02, z + 0.07), (0.0, z + 0.07)], 16, None, 0, M), GOLD)
        k.add(lambda bm, x=x, y=y, r=r, h=h: star(bm, x, y - r * 0.9 - 0.01, h * 0.5, 0.12, 'Y', 0.02), GOLD)
    grass_ring(k, 0.7, 0.7, seed, h=0.05)
    return k.obj('StarTubs')

def build_popcorncart(seed=1741):
    """a 1960s popcorn cart: a glass case of popcorn under a striped canopy, two spoked wheels, a push handle"""
    k = Kit([RED, CREAM, GOLD, 'black', 'yellow', 'lamp_warm', 'stone_dark', 'xmas_red_dark', 'khaki', 'olive'])
    for s in (-1, 1): wheel(k, -0.15, s * 0.45, 0.30, 0.30, GOLD, spokes=6)
    k.add(lambda bm: rbox(bm, (0.95, 0.75, 0.55), (0.05, 0, 0.30), 0.04), paint(RED, 'xmas_red_dark'))         # cabinet
    k.add(lambda bm: pbox(bm, (0.97, 0.77, 0.05), (0.05, 0, 0.62)), GOLD)
    k.add(lambda bm: pbox(bm, (0.85, 0.65, 0.55), (0.05, 0, 0.85)), 'stone_dark')                              # glass case (dark)
    k.add(lambda bm: blob(bm, 1.0, (0.05, 0, 0.85), (0.38, 0.28, 0.22), 2, 0.04, seed, floor=False), 'cream')  # popcorn
    k.add(lambda bm: sphere(bm, 0.06, (0.05, 0, 1.36), sub=1), 'lamp_warm', glow=True)
    for (x, y) in [(-0.40, -0.35), (0.50, -0.35), (0.50, 0.35), (-0.40, 0.35)]:
        k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, 0.85)), Vector((x, y, 1.45))], 0.02, 0.02, 4), GOLD)
    def canopy(bm):
        lathe(bm, [(0.70, 1.45), (0.66, 1.52), (0.0, 1.85)], 12, None, 0, Matrix.Translation((0.05, 0, 0)))
    k.add(canopy, stripes(12, RED, CREAM, 0.05, 0))
    k.add(lambda bm: sphere(bm, 0.06, (0.05, 0, 1.90), sub=1), GOLD)
    k.add(lambda bm: tube(bm, [Vector((0.55, -0.3, 0.70)), Vector((0.85, -0.3, 0.80)), Vector((0.85, 0.3, 0.80)), Vector((0.55, 0.3, 0.70))], 0.02, 0.02, 5), 'black')
    grass_ring(k, 0.7, 0.6, seed, h=0.05)
    return k.obj('PopcornCart')

def build_circuscannon(seed=1751):
    """the human-cannonball cannon: a fat red barrel with gold bands and a star, raised on a two-wheeled carriage"""
    k = Kit([RED, 'xmas_red_dark', GOLD, 'gold_dark', BLUE, 'black', 'khaki', 'olive'])
    for s in (-1, 1): wheel(k, 0.0, s * 0.52, 0.45, 0.45, GOLD, spokes=10, w=0.10)
    k.add(lambda bm: rbox(bm, (0.95, 0.80, 0.22), (-0.15, 0, 0.25), 0.04), paint(BLUE, 'black'))                 # carriage
    M = Matrix.Translation((0.05, 0, 0.62)) @ Matrix.Rotation(-0.55, 4, 'Y') @ Matrix.Rotation(math.pi / 2, 4, 'Y') @ Matrix.Translation((0, 0, -0.55))
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.30, 0.04), (0.33, 0.30), (0.26, 1.35), (0.30, 1.45), (0.22, 1.45), (0.18, 1.2)], 14, None, 0, M), paint(RED, 'xmas_red_dark'))
    for z in (0.30, 0.80, 1.38): k.add(lambda bm, z=z: lathe(bm, [(0.32 - z * 0.04, z), (0.32 - z * 0.04, z + 0.06), (0.0, z + 0.06)], 14, None, 0, M), GOLD)
    k.add(lambda bm: lathe(bm, [(0.17, 1.44), (0.0, 1.44)], 14, None, 0, M), 'black')                                  # the muzzle
    grass_ring(k, 0.75, 0.7, seed, h=0.05)
    return k.obj('CircusCannon')

# ------------------------------------------------------------------ fence
def build_ropebarrier(seed=1761):
    """a 1.5 m crowd barrier: brass-topped red posts with a thick red rope hanging between them"""
    k = Kit([RED, 'xmas_red_dark', GOLD, 'gold_dark', 'khaki', 'olive'])
    posts = [-0.70, 0.0, 0.70]
    for x in posts:
        M = Matrix.Translation((x, 0, 0))
        k.add(lambda bm, M=M: lathe(bm, [(0.16, 0.0), (0.16, 0.05), (0.06, 0.10), (0.05, 0.95), (0.0, 0.95)], 10, None, 0, M), paint(RED, 'xmas_red_dark'))
        k.add(lambda bm, x=x: sphere(bm, 0.075, (x, 0, 0.98), sub=1), GOLD)
        k.add(lambda bm, M=M: lathe(bm, [(0.17, 0.0), (0.17, 0.03), (0.0, 0.03)], 10, None, 0, M), GOLD)
    for a, b in zip(posts, posts[1:]):
        pts = [Vector((a + (b - a) * t, 0, 0.88 - 0.22 * math.sin(math.pi * t))) for t in [i / 8 for i in range(9)]]
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.04, 0.04, 6), RED)
    grass_ring(k, 0.8, 0.30, seed, h=0.05)
    return k.obj('RopeBarrier')

# ------------------------------------------------------------------ tall
def build_highstriker(seed=1771):
    """the strongman's high striker: a tall painted board with coloured bands, a brass bell on top, the striking pad and
    a big wooden mallet leaning on it"""
    k = Kit([RED, CREAM, 'yellow', 'xmas_green', BLUE, GOLD, 'gold_dark', 'wood', 'wood_dark', 'black', 'khaki', 'olive'])
    k.add(lambda bm: rbox(bm, (0.90, 0.80, 0.25), (0, 0, 0), 0.04), woodc)                                     # base
    k.add(lambda bm: rbox(bm, (0.40, 0.30, 0.10), (0.15, -0.10, 0.25), 0.03), 'black')                        # pad
    k.add(lambda bm: rbox(bm, (0.34, 0.14, 3.0), (-0.15, 0.15, 0.25), 0.03), CREAM)                           # board
    cols = ['xmas_green', 'xmas_green', 'yellow', 'yellow', 'orange', RED, RED]
    for i, c in enumerate(cols): k.add(lambda bm, i=i, c=c: pbox(bm, (0.36, 0.02, 0.32), (-0.15, 0.07, 0.45 + i * 0.38)), c)
    k.add(lambda bm: tube(bm, [Vector((-0.15, 0.04, 0.4)), Vector((-0.15, 0.04, 3.1))], 0.015, 0.015, 4), 'black')   # the wire
    k.add(lambda bm: pbox(bm, (0.10, 0.06, 0.12), (-0.15, 0.03, 1.2)), BLUE)                                   # the puck
    k.add(lambda bm: lathe(bm, [(0.20, 0.0), (0.17, 0.08), (0.08, 0.20), (0.0, 0.22)], 12, None, 0, Matrix.Translation((-0.15, 0.10, 3.2))), GOLD)   # bell
    k.add(lambda bm: star(bm, -0.15, 0.06, 3.0, 0.14, 'Y', 0.02), RED)
    k.add(lambda bm: tube(bm, [Vector((0.45, -0.30, 0.0)), Vector((0.35, -0.25, 1.0))], 0.03, 0.03, 6), woodc)   # mallet
    k.add(lambda bm: rbox(bm, (0.36, 0.18, 0.18), (0.33, -0.25, 1.0), 0.03, 0.4), 'wood_dark')
    return k.obj('HighStriker')

def build_ticketbooth(seed=1781):
    """a little painted ticket booth: panelled walls, a window with a brass grille, a striped pointed roof and a pennant"""
    k = Kit([RED, CREAM, GOLD, 'gold_dark', BLUE, 'black', 'stone_dark', 'lamp_warm', 'xmas_red_dark', 'khaki', 'olive'])
    k.add(lambda bm: rbox(bm, (1.20, 1.20, 2.10), (0, 0, 0), 0.05), xstripes(0.2, RED, CREAM, -0.6))
    k.add(lambda bm: pbox(bm, (0.70, 0.04, 0.55), (0, -0.60, 1.05)), 'stone_dark')                           # window
    for i in range(5): k.add(lambda bm, i=i: tube(bm, [Vector((-0.28 + i * 0.14, -0.63, 1.05)), Vector((-0.28 + i * 0.14, -0.63, 1.6))], 0.012, 0.012, 4), GOLD)
    k.add(lambda bm: rbox(bm, (0.90, 0.25, 0.06), (0, -0.70, 1.0), 0.02, segs=1), GOLD)                      # counter
    k.add(lambda bm: pbox(bm, (1.30, 1.30, 0.14), (0, 0, 2.10)), GOLD)
    k.add(lambda bm: lathe(bm, [(0.95, 2.24), (0.88, 2.30), (0.0, 3.05)], 12), stripes(12, RED, CREAM))
    k.add(lambda bm: tube(bm, [Vector((0, 0, 3.0)), Vector((0, 0, 3.5))], 0.02, 0.02, 4), GOLD)
    def flag(bm):
        a = bm.verts.new((0, 0, 3.48)); b = bm.verts.new((0.35, 0, 3.40)); c = bm.verts.new((0, 0, 3.30))
        bm.faces.new((a, b, c)); a2 = bm.verts.new((0, 0.001, 3.48)); b2 = bm.verts.new((0.35, 0.001, 3.40)); c2 = bm.verts.new((0, 0.001, 3.30)); bm.faces.new((c2, b2, a2))
    k.add(flag, BLUE)
    k.add(lambda bm: sphere(bm, 0.06, (0, -0.55, 1.75), sub=1), 'lamp_warm', glow=True)
    grass_ring(k, 0.75, 0.75, seed, h=0.05)
    return k.obj('TicketBooth')

def build_carnivallamp(seed=1791):
    """a fairground lamp standard: a striped post with a crown of five incandescent globes"""
    k = Kit([RED, CREAM, GOLD, 'gold_dark', 'lamp_warm', 'black', 'khaki', 'olive'])
    k.add(lambda bm: lathe(bm, [(0.22, 0.0), (0.20, 0.12), (0.09, 0.30), (0.07, 3.0), (0.10, 3.05), (0.0, 3.05)], 10), lambda f: RED if int(f.calc_center_median().z / 0.35) % 2 == 0 else CREAM)
    for i in range(5):
        a = i * 2 * math.pi / 5
        p = Vector((math.cos(a) * 0.36, math.sin(a) * 0.36, 3.15))
        k.add(lambda bm, p=p: tube(bm, [Vector((0, 0, 3.0)), Vector((p.x * 0.7, p.y * 0.7, 3.12)), p], 0.025, 0.02, 5), GOLD)
        k.add(lambda bm, p=p: sphere(bm, 0.12, (p.x, p.y, p.z + 0.12), sub=2), 'lamp_warm', glow=True)
    k.add(lambda bm: sphere(bm, 0.14, (0, 0, 3.35), sub=2), 'lamp_warm', glow=True)
    grass_ring(k, 0.5, 0.5, seed, h=0.05)
    return k.obj('CarnivalLamp')

def build_balloonpost(seed=1801):
    """a balloon seller's post: a striped pole with a bunch of rubber balloons on strings"""
    rnd = random.Random(seed)
    k = Kit([RED, CREAM, 'yellow', BLUE, 'turquoise', 'orange', 'xmas_green', GOLD, 'black', 'khaki', 'olive'])
    k.add(lambda bm: lathe(bm, [(0.20, 0.0), (0.16, 0.10), (0.05, 0.18), (0.04, 1.9), (0.0, 1.9)], 8), lambda f: RED if int(f.calc_center_median().z / 0.3) % 2 == 0 else CREAM)
    cols = [RED, 'yellow', BLUE, 'turquoise', 'orange', 'xmas_green', RED, 'yellow']
    for i, c in enumerate(cols):
        a = i * 2 * math.pi / len(cols) + rnd.uniform(-0.2, 0.2); rr = rnd.uniform(0.25, 0.45)
        p = Vector((math.cos(a) * rr, math.sin(a) * rr, 2.6 + rnd.uniform(-0.2, 0.4)))
        k.add(lambda bm, p=p: tube(bm, [Vector((0, 0, 1.9)), p - Vector((0, 0, 0.25))], 0.006, 0.006, 3), 'black')
        k.add(lambda bm, p=p: sphere(bm, 0.22, (p.x, p.y, p.z), (1, 1, 1.25), 2), c)
        k.add(lambda bm, p=p: lathe(bm, [(0.03, 0.0), (0.0, -0.06)], 5, None, 0, Matrix.Translation((p.x, p.y, p.z - 0.27))), c)
    grass_ring(k, 0.45, 0.45, seed, h=0.05)
    return k.obj('BalloonPost')

def build_fairgrass():
    """long field grass round the fairground (the hiding bush): the Brawl leaf clump with the papyrus ramp"""
    return grass.build_frostgrass(171, 'FairGrass', 'papyrus_grad', 0.85)

# ------------------------------------------------------------------ decor
def build_haypile(seed=1811):
    """decor: loose hay spilled on the grass"""
    rnd = random.Random(seed)
    k = Kit(['khaki', 'yellow', 'olive'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.75, 0.55, 0.16), 2, 0.05, seed), lambda f: 'khaki' if f.normal.z > 0.5 else 'olive')   # (bright yellow read as paint)
    for j in range(10):
        a = rnd.uniform(0, 3.14); p = Vector((rnd.uniform(-0.6, 0.6), rnd.uniform(-0.45, 0.45), 0.03)); d = Vector((math.cos(a), math.sin(a), 0)) * 0.18
        k.add(lambda bm, p=p, d=d: tube(bm, [p - d, p + d], 0.01, 0.01, 3), 'yellow')
    return k.obj('HayPile')

def build_jugglingpins(seed=1821):
    """decor: juggling clubs and balls dropped on the grass"""
    rnd = random.Random(seed)
    k = Kit([RED, CREAM, BLUE, 'yellow', GOLD, 'turquoise'])
    for i, (x, y, a, c) in enumerate([(-0.25, 0.0, 0.3, RED), (0.15, 0.2, 1.6, BLUE), (0.2, -0.25, 2.4, 'yellow')]):
        M = Matrix.Translation((x, y, 0.08)) @ Matrix.Rotation(a, 4, 'Z') @ Matrix.Rotation(math.pi / 2, 4, 'Y') @ Matrix.Translation((0, 0, -0.25))
        k.add(lambda bm, M=M: lathe(bm, [(0.0, 0.0), (0.04, 0.02), (0.03, 0.18), (0.08, 0.36), (0.07, 0.46), (0.0, 0.50)], 8, None, 0, M), c)
        k.add(lambda bm, M=M: lathe(bm, [(0.085, 0.33), (0.085, 0.37), (0.0, 0.37)], 8, None, 0, M), CREAM)
    for (x, y, c) in [(-0.35, -0.3, 'turquoise'), (0.4, 0.05, RED)]:
        k.add(lambda bm, x=x, y=y, c=c: sphere(bm, 0.08, (x, y, 0.08), sub=2), c)
    return k.obj('JugglingPins')

def build_popcornspill(seed=1831):
    """decor: a tipped striped popcorn box and popcorn on the grass"""
    rnd = random.Random(seed)
    k = Kit([RED, CREAM, 'yellow'])
    M = Matrix.Translation((0, 0, 0.12)) @ Matrix.Rotation(math.pi / 2 - 0.2, 4, 'Y') @ Matrix.Translation((0, 0, -0.15))
    k.add(lambda bm: lathe(bm, [(0.0, 0.0), (0.09, 0.0), (0.13, 0.30), (0.12, 0.30), (0.08, 0.02), (0.0, 0.02)], 8, None, 0, M), stripes(8, RED, CREAM, 0, 0))
    for j in range(14):
        k.add(lambda bm, j=j: sphere(bm, 0.035, (0.2 + rnd.uniform(0, 0.35), rnd.uniform(-0.2, 0.2), 0.02), sub=1), 'cream' if j % 4 else 'yellow')
    return k.obj('PopcornSpill')

# ------------------------------------------------------------------ border and gate
def build_circuswall(kind, seed, name):
    """a border piece in place of the old border cliffs (7.6 m long, 5 m deep): the side wall of the big top. A striped
    canvas sidewall between tall king poles with pennants, a scalloped valance, the striped roof sloping up behind it
    (nothing seen through). Front = -Y (+Z in Unity). 'A' plain canvas, 'B' with painted sideshow banners hung on it,
    'C' with a show wagon parked against it."""
    rnd = random.Random(seed)
    k = Kit([RED, CREAM, GOLD, BLUE, 'yellow', 'turquoise', 'wood', 'wood_dark', 'black', 'xmas_red_dark', 'khaki', 'olive'])
    L, D, H = 7.6, 5.0, 4.2
    front = -D / 2 + 0.8
    k.add(lambda bm: rbox(bm, (L, D - 1.4, H), (0, 0.7, 0), 0.05, segs=1), RED)                            # solid core
    def wall(bm):   # the sidewall: a strip with a slight wave, vertical stripes
        n = 38
        for i in range(n):
            x0 = -L / 2 + L * i / n; x1 = x0 + L / n
            y0 = front + 0.06 * math.sin(i * 0.9); y1 = front + 0.06 * math.sin((i + 1) * 0.9)
            v = [bm.verts.new((x0, y0, 0.0)), bm.verts.new((x1, y1, 0.0)), bm.verts.new((x1, y1, H)), bm.verts.new((x0, y0, H))]
            bm.faces.new((v[0], v[1], v[2], v[3]))   # facing -Y (the arena); the reverse was culled in Unity
            w = [bm.verts.new(p.co) for p in v]; bm.faces.new((w[1], w[0], w[3], w[2]))   # and the back
    k.add(wall, xstripes(L / 19, RED, CREAM, -L / 2))
    def roof(bm):   # one quad per stripe (a single quad took one colour)
        n = 10
        for i in range(n):
            x0 = -L / 2 + L * i / n; x1 = x0 + L / n
            v = [bm.verts.new((x0, front - 0.1, H)), bm.verts.new((x1, front - 0.1, H)), bm.verts.new((x1, D / 2, H + 2.6)), bm.verts.new((x0, D / 2, H + 2.6))]
            bm.faces.new(v)
            w = [bm.verts.new(p.co) for p in v]; bm.faces.new(list(reversed(w)))   # both sides
    k.add(roof, xstripes(L / 10, RED, CREAM, -L / 2))
    for i in range(16):   # scalloped valance along the eave
        x = -L / 2 + (i + 0.5) * L / 16
        def scallop(bm, x=x):
            vs = [bm.verts.new((x + 0.24 * math.cos(a), front - 0.12, H - 0.02 - 0.24 * math.sin(a) * 0.9)) for a in [j / 6 * math.pi for j in range(7)]]
            c = bm.verts.new((x, front - 0.12, H));
            for j in range(6): bm.faces.new((c, vs[j + 1], vs[j]))
        k.add(scallop, BLUE if i % 2 == 0 else GOLD)
    for x in (-L / 2 + 0.2, -L / 6, L / 6, L / 2 - 0.2):   # poles with pennants
        k.add(lambda bm, x=x: tube(bm, [Vector((x, front - 0.15, 0)), Vector((x, front - 0.15, H + 1.6))], 0.07, 0.06, 6), woodc)
        k.add(lambda bm, x=x: sphere(bm, 0.09, (x, front - 0.15, H + 1.65), sub=1), GOLD)
        def pennant(bm, x=x):
            a = bm.verts.new((x, front - 0.15, H + 1.55)); b = bm.verts.new((x + 0.6, front - 0.15, H + 1.40)); c = bm.verts.new((x, front - 0.15, H + 1.25))
            bm.faces.new((a, b, c)); a2 = bm.verts.new((x, front - 0.149, H + 1.55)); b2 = bm.verts.new((x + 0.6, front - 0.149, H + 1.40)); c2 = bm.verts.new((x, front - 0.149, H + 1.25))
            bm.faces.new((c2, b2, a2))
        k.add(pennant, rnd.choice([BLUE, 'yellow', 'turquoise', RED]))
    string_lights(k, [Vector((-L / 2 + L * t, front - 0.25, H - 0.35 - 0.25 * math.sin(math.pi * ((t * 3) % 1)))) for t in [i / 24 for i in range(25)]], every=0.45, s=1.6)
    if kind == 'B':   # painted sideshow banners: framed boards with simple pictures (a star, a moon, a ring)
        for j, x in enumerate((-2.4, 0.0, 2.4)):
            k.add(lambda bm, x=x: pbox(bm, (1.7, 0.06, 1.9), (x, front - 0.12, 1.2)), GOLD)
            k.add(lambda bm, x=x, j=j: pbox(bm, (1.5, 0.06, 1.7), (x, front - 0.15, 1.3)), [BLUE, 'turquoise', RED][j])
            if j == 0: k.add(lambda bm, x=x: star(bm, x, front - 0.20, 2.15, 0.5, 'Y', 0.03), 'yellow')
            if j == 1: k.add(lambda bm, x=x: sphere(bm, 0.45, (x, front - 0.20, 2.15), (1, 0.15, 1), 2), 'cream')
            if j == 2: k.add(lambda bm, x=x: lathe(bm, [(0.45, -0.03), (0.55, -0.03), (0.55, 0.03), (0.45, 0.03)], 16, None, 0, Matrix.Translation((x, front - 0.22, 2.15)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), 'yellow')
    if kind == 'C':   # a show wagon parked against the canvas
        k.add(lambda bm: rbox(bm, (2.6, 1.2, 1.6), (0.6, front - 0.75, 0.6), 0.06), paint(RED, 'xmas_red_dark'))
        k.add(lambda bm: rbox(bm, (2.75, 1.32, 0.16), (0.6, front - 0.75, 2.2), 0.05), GOLD)
        k.add(lambda bm: rbox(bm, (2.65, 1.25, 0.28), (0.6, front - 0.75, 2.36), 0.14, segs=3), paint(RED, 'xmas_red_dark'))
        for x in (-0.3, 1.5): wheel(k, x, front - 1.40, 0.45, 0.45, GOLD)
        k.add(lambda bm: star(bm, 0.6, front - 1.36, 1.4, 0.35, 'Y', 0.03), GOLD)
    k.add(lambda bm: blob(bm, 1.0, (0, front - 0.6, 0), (L * 0.48, 0.8, 0.08), 3, 0.02, seed), lambda f: 'khaki' if f.normal.z > 0.4 else 'olive')
    return k.obj(name)

def build_circusgate(seed=1841):
    """the fairground entrance behind each spawn (in place of the base scene's gate arch, 10.8 x 9.6 m): two striped
    towers with gold cupolas, an arched board between them edged with glowing bulbs, a star on top, pennants"""
    rnd = random.Random(seed)
    k = Kit([RED, CREAM, GOLD, 'gold_dark', BLUE, 'yellow', 'turquoise', 'lamp_warm', 'xmas_red_dark', 'khaki', 'olive'])
    X = 4.2
    for s in (-1, 1):
        M = Matrix.Translation((s * X, 0, 0))
        k.add(lambda bm, M=M: lathe(bm, [(0.75, 0.0), (0.75, 0.4), (0.62, 0.5), (0.58, 5.6), (0.0, 5.6)], 12, None, 0, M), stripes(12, RED, CREAM, s * X, 0))
        k.add(lambda bm, M=M: lathe(bm, [(0.80, 5.6), (0.80, 5.8), (0.0, 5.8)], 12, None, 0, M), GOLD)
        k.add(lambda bm, M=M: lathe(bm, [(0.72, 5.8), (0.55, 6.3), (0.15, 6.9), (0.0, 7.0)], 12, None, 0, M), stripes(12, BLUE, GOLD, s * X, 0))
        k.add(lambda bm, s=s: tube(bm, [Vector((s * X, 0, 6.9)), Vector((s * X, 0, 7.6))], 0.03, 0.03, 4), GOLD)
        def pen(bm, s=s):
            a = bm.verts.new((s * X, 0, 7.55)); b = bm.verts.new((s * X + 0.7, 0, 7.4)); c = bm.verts.new((s * X, 0, 7.25))
            bm.faces.new((a, b, c)); a2 = bm.verts.new((s * X, 0.001, 7.55)); b2 = bm.verts.new((s * X + 0.7, 0.001, 7.4)); c2 = bm.verts.new((s * X, 0.001, 7.25)); bm.faces.new((c2, b2, a2))
        k.add(pen, 'yellow')
    # the arched board: a band of segments along an arc
    n = 14
    arc = [(-X + 0.5 + (2 * X - 1.0) * t, 5.0 + 1.3 * math.sin(math.pi * t)) for t in [i / n for i in range(n + 1)]]
    def board(bm, y, h, depth):
        for i in range(n):
            (x0, z0), (x1, z1) = arc[i], arc[i + 1]
            v = [bm.verts.new((x0, y, z0 - h / 2)), bm.verts.new((x1, y, z1 - h / 2)), bm.verts.new((x1, y, z1 + h / 2)), bm.verts.new((x0, y, z0 + h / 2)),
                 bm.verts.new((x0, y + depth, z0 - h / 2)), bm.verts.new((x1, y + depth, z1 - h / 2)), bm.verts.new((x1, y + depth, z1 + h / 2)), bm.verts.new((x0, y + depth, z0 + h / 2))]
            bm.faces.new((v[0], v[1], v[2], v[3])); bm.faces.new((v[5], v[4], v[7], v[6]))
            bm.faces.new((v[3], v[2], v[6], v[7])); bm.faces.new((v[1], v[0], v[4], v[5]))
    k.add(lambda bm: board(bm, -0.10, 1.10, 0.20), GOLD)
    k.add(lambda bm: board(bm, -0.13, 0.85, 0.05), RED)
    for i in range(n + 1):   # bulbs along the top and bottom edge of the board
        x, z = arc[i]
        for dz in (0.52, -0.52): k.add(lambda bm, x=x, z=z, dz=dz: sphere(bm, 0.07, (x, -0.18, z + dz), sub=1), 'lamp_warm', glow=True)
    k.add(lambda bm: star(bm, 0.0, -0.20, 6.85, 0.55, 'Y', 0.06), 'yellow')
    for x in (-1.6, 1.6): k.add(lambda bm, x=x: star(bm, x, -0.19, 5.0 + 1.3 * math.sin(math.pi * (x + X - 0.5) / (2 * X - 1.0)), 0.25, 'Y', 0.03), 'cream')
    k.add(lambda bm: blob(bm, 1.0, (0, -1.0, 0), (5.4, 1.4, 0.08), 3, 0.02, seed), lambda f: 'khaki' if f.normal.z > 0.4 else 'olive')
    return k.obj('CircusGate')

# ------------------------------------------------------------------ backdrop
def build_bigtop(seed=1851):
    """the big top: a round striped tent, scalloped valance, a peaked roof on two king poles, flags on top"""
    k = Kit([RED, CREAM, GOLD, BLUE, 'yellow', 'wood_dark'])
    R, Hw = 12.0, 4.0
    k.add(lambda bm: lathe(bm, [(R, 0.0), (R, Hw)], 32), stripes(32, RED, CREAM))
    k.add(lambda bm: lathe(bm, [(R + 0.3, Hw), (R * 0.45, Hw + 4.5), (1.2, Hw + 7.5), (0.0, Hw + 7.8)], 32), stripes(32, RED, CREAM))
    for i in range(32):
        a = (i + 0.5) * 2 * math.pi / 32
        k.add(lambda bm, a=a, i=i: sphere(bm, 0.75, (math.cos(a) * (R + 0.35), math.sin(a) * (R + 0.35), Hw - 0.1), (1, 1, 0.6), 1), BLUE if i % 2 else GOLD)
    k.add(lambda bm: tube(bm, [Vector((0, 0, Hw + 7.5)), Vector((0, 0, Hw + 10.0))], 0.12, 0.10, 6), 'wood_dark')
    def flag(bm):
        a = bm.verts.new((0, 0, Hw + 9.9)); b = bm.verts.new((1.8, 0, Hw + 9.4)); c = bm.verts.new((0, 0, Hw + 8.9)); bm.faces.new((a, b, c))
        a2 = bm.verts.new((0, 0.01, Hw + 9.9)); b2 = bm.verts.new((1.8, 0.01, Hw + 9.4)); c2 = bm.verts.new((0, 0.01, Hw + 8.9)); bm.faces.new((c2, b2, a2))
    k.add(flag, 'yellow')
    return k.obj('BigTop')

def build_ferriswheel(seed=1861):
    """a 1960s Ferris wheel: two lattice rims on spokes, twelve swinging gondolas, an A-frame on each side, bulbs on the rim"""
    k = Kit([CREAM, RED, BLUE, 'yellow', 'turquoise', 'orange', GOLD, 'stone_dark', 'lamp_warm', 'black'])
    R, Hc = 9.0, 10.5
    for y in (-0.7, 0.7):
        pts = [Vector((math.cos(a) * R, y, Hc + math.sin(a) * R)) for a in [i / 24 * 2 * math.pi for i in range(25)]]
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.12, 0.12, 6), CREAM)
        for i in range(12):
            a = i * 2 * math.pi / 12
            k.add(lambda bm, a=a, y=y: tube(bm, [Vector((0, y, Hc)), Vector((math.cos(a) * R, y, Hc + math.sin(a) * R))], 0.05, 0.05, 4), CREAM)
        for s in (-1, 1):   # A-frame legs
            k.add(lambda bm, y=y, s=s: tube(bm, [Vector((0, y * 1.6, Hc)), Vector((s * 5.5, y * 3.0, 0))], 0.15, 0.18, 6), RED)
    k.add(lambda bm: tube(bm, [Vector((0, -1.4, Hc)), Vector((0, 1.4, Hc))], 0.25, 0.25, 8), GOLD)
    cols = [RED, BLUE, 'yellow', 'turquoise', 'orange', RED, BLUE, 'yellow', 'turquoise', 'orange', RED, BLUE]
    for i in range(12):
        a = i * 2 * math.pi / 12 + 0.13
        p = Vector((math.cos(a) * R, 0, Hc + math.sin(a) * R))
        k.add(lambda bm, p=p: tube(bm, [Vector((p.x, -0.7, p.z)), Vector((p.x, 0.7, p.z))], 0.04, 0.04, 4), 'stone_dark')
        k.add(lambda bm, p=p, c=cols[i]: rbox(bm, (1.2, 1.1, 0.9), (p.x, 0, p.z - 1.5), 0.15), cols[i])
        k.add(lambda bm, p=p: lathe(bm, [(0.8, 0.0), (0.0, 0.3)], 8, None, 0, Matrix.Translation((p.x, 0, p.z - 0.55))), GOLD)
        k.add(lambda bm, a=a: sphere(bm, 0.12, (math.cos(a + 0.26) * R, -0.85, Hc + math.sin(a + 0.26) * R), sub=1), 'lamp_warm', glow=True)
    return k.obj('FerrisWheel')

def build_carousel(seed=1871):
    """a carousel (merry-go-round): a round platform, painted horses on brass poles, a striped canopy with a scalloped
    rim lit by bulbs, a gilt centre drum"""
    rnd = random.Random(seed)
    k = Kit([RED, CREAM, GOLD, 'gold_dark', BLUE, 'yellow', 'turquoise', 'lamp_warm', 'wood', 'wood_dark', 'black'])
    R = 4.0
    k.add(lambda bm: lathe(bm, [(R, 0.0), (R, 0.45), (0.0, 0.45)], 24), lambda f: 'wood' if f.normal.z > 0.5 else GOLD)
    k.add(lambda bm: lathe(bm, [(1.1, 0.45), (1.1, 3.4), (0.0, 3.4)], 16), stripes(16, BLUE, GOLD))
    k.add(lambda bm: lathe(bm, [(R + 0.4, 3.4), (R + 0.4, 3.7), (0.0, 5.6)], 24), stripes(24, RED, CREAM))
    for i in range(24):
        a = (i + 0.5) * 2 * math.pi / 24
        k.add(lambda bm, a=a, i=i: sphere(bm, 0.32, (math.cos(a) * (R + 0.45), math.sin(a) * (R + 0.45), 3.35), (1, 1, 0.7), 1), GOLD if i % 2 else BLUE)
        k.add(lambda bm, a=a: sphere(bm, 0.08, (math.cos(a + 0.13) * (R + 0.5), math.sin(a + 0.13) * (R + 0.5), 3.15), sub=1), 'lamp_warm', glow=True)
    cols = [CREAM, 'yellow', 'turquoise', CREAM, RED, 'yellow', CREAM, BLUE]
    for i in range(8):
        a = i * 2 * math.pi / 8; r = R - 0.8
        x, y = math.cos(a) * r, math.sin(a) * r; h = 1.2 + 0.25 * math.sin(i * 1.7)
        k.add(lambda bm, x=x, y=y: tube(bm, [Vector((x, y, 0.45)), Vector((x, y, 3.4))], 0.04, 0.04, 5), GOLD)
        yaw = a + math.pi / 2; d = Vector((math.cos(yaw), math.sin(yaw), 0))
        k.add(lambda bm, x=x, y=y, h=h, yaw=yaw, c=cols[i]: rbox(bm, (0.9, 0.30, 0.42), (x, y, h), 0.12, yaw), cols[i])                    # body
        k.add(lambda bm, x=x, y=y, h=h, d=d, c=cols[i]: rbox(bm, (0.22, 0.20, 0.45), (x + d.x * 0.45, y + d.y * 0.45, h + 0.30), 0.08, yaw), cols[i])   # neck/head
        for s in (-1, 1): k.add(lambda bm, x=x, y=y, h=h, d=d, s=s: tube(bm, [Vector((x + d.x * 0.3 * s, y + d.y * 0.3 * s, h)), Vector((x + d.x * 0.38 * s, y + d.y * 0.38 * s, h - 0.45))], 0.04, 0.03, 4), 'wood_dark')
        k.add(lambda bm, x=x, y=y, h=h: rbox(bm, (0.32, 0.34, 0.08), (x, y, h + 0.40), 0.02), RED)                                           # saddle
    k.add(lambda bm: tube(bm, [Vector((0, 0, 5.5)), Vector((0, 0, 6.3))], 0.05, 0.05, 4), GOLD)
    k.add(lambda bm: sphere(bm, 0.2, (0, 0, 6.35), sub=1), GOLD)
    return k.obj('Carousel')
