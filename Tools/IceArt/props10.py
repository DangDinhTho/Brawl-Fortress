"""Gates behind each team's spawn, where the base scene's gate arch stood (10.8 x 9.6 m; the gap in the border there is
walled by the invisible VitualWall, the gate closes it for the eye). Front = -Y here (+Z in Unity), turned to the arena.

IceGate  - a natural glacier arch (ice age theme: nothing man-made)
XmasGate - a 1960s-70s village gate: timber posts and arch, pine garland, C9 bulb string, wreath, lanterns, snow
(TempleGate, the Egypt one, is in props9.py.)"""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *
from props3 import blob
from props6 import column, glacier_col, GC
from props7 import tube
from props8 import Kit, rbox, sphere, string_lights, socket, c9

def build_icegate(seed=1001):
    rnd = random.Random(seed)
    k = Kit(list(GC))
    icec = lambda f: GC[glacier_col(f)]
    for s in (-1, 1):   # two pillars: clusters of faceted ice columns, like the glacier walls
        def pillar(bm, s=s):
            for (dx, dy, w, h) in [(0.0, 0.0, 0.95, 8.6), (0.9, 0.35, 0.75, 7.4), (-0.85, 0.3, 0.8, 7.9), (0.3, -0.75, 0.6, 6.2), (-0.4, 0.95, 0.7, 6.8)]:
                column(bm, s * 3.6 + dx * s, dy, w, w * 0.95, h * rnd.uniform(0.95, 1.05), rnd)
        k.add(pillar, icec)
        k.add(lambda bm, s=s: blob(bm, 1.0, (s * 3.6, 0.1, 8.1), (1.2, 1.1, 0.35), 3, 0.04, seed + (s > 0)), 'snow')   # snow on top
    # the ice bridge: one continuous faceted band swept along an arc from pillar to pillar (a rectangular section)
    n = 12
    C = Vector((0.0, 0.0, 5.4))
    def outward(t, p):   # normal of the arc in its plane, away from the arch centre (the top side of the band)
        r = t.cross(Vector((0, 1, 0))).normalized()
        return r if r.dot(p - C) >= 0 else -r
    arc = [Vector((-math.cos(a) * 3.0, 0.0, 5.4 + math.sin(a) * 2.0)) for a in [math.pi * (i / n) for i in range(n + 1)]]
    def bridge(bm):
        rings = []
        for i, p in enumerate(arc):
            t = (arc[min(i + 1, n)] - arc[max(i - 1, 0)]).normalized()
            r = outward(t, p)
            w = 1.0 + 0.12 * math.sin(i * 1.7); th = 0.62 + 0.08 * math.cos(i * 2.3)   # uneven: broken, faceted ice
            ring = [bm.verts.new(p + r * th + Vector((0, -w, 0))), bm.verts.new(p + r * th + Vector((0, w, 0))),
                    bm.verts.new(p - r * th + Vector((0, w, 0))), bm.verts.new(p - r * th + Vector((0, -w, 0)))]
            rings.append(ring)
        for i in range(n):
            a, b = rings[i], rings[i + 1]
            for j in range(4): bm.faces.new((a[j], a[(j + 1) % 4], b[(j + 1) % 4], b[j]))
        bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    k.add(bridge, icec)
    # snow lying along the top of the bridge
    def snowtop(bm):
        pts = []
        for i, p in enumerate(arc[1:-1], 1):
            t = (arc[i + 1] - arc[i - 1]).normalized(); r = outward(t, p)
            pts.append(p + r * 0.72)
        tube(bm, pts, 0.32, 0.32, 8)
    k.add(snowtop, 'snow')
    for j in range(6):   # broken ice at the feet
        x = rnd.choice([-1, 1]) * rnd.uniform(2.2, 5.0); sz = rnd.uniform(0.4, 0.8)
        def chunk(bm, x=x, sz=sz):
            vs = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((x, -1.4 - sz * 0.3, sz * 0.35)) @ Matrix.Rotation(rnd.uniform(0, 3), 4, 'Z') @ Matrix.Diagonal((sz * 1.3, sz, sz * 0.9, 1)))['verts']
            jitter(vs, sz * 0.15, rnd.randint(0, 999))
            for v in vs:
                if v.co.z < 0: v.co.z = 0
        k.add(chunk, icec)
    k.add(lambda bm: blob(bm, 1.0, (0, -0.8, 0), (5.6, 1.6, 0.35), 3, 0.05, seed + 4), 'snow')
    return k.obj('IceGate')

def build_xmasgate(seed=1011):
    rnd = random.Random(seed)
    k = Kit(['wood', 'wood_dark', 'snow', 'snow_shade', 'xmas_green', 'xmas_red', 'stone', 'stone_dark'])
    woodc = lambda f: 'wood' if lit(f.normal) > -0.15 else 'wood_dark'
    X = 4.2
    for s in (-1, 1):
        k.add(lambda bm, s=s: rbox(bm, (1.3, 1.3, 1.1), (s * X, 0, 0), 0.10, segs=2), lambda f: 'stone' if lit(f.normal) > -0.15 else 'stone_dark')   # stone base
        k.add(lambda bm, s=s: rbox(bm, (0.75, 0.75, 6.6), (s * X, 0, 1.1), 0.06), woodc)                                                          # post
        k.add(lambda bm, s=s: rbox(bm, (0.95, 0.95, 0.25), (s * X, 0, 7.7), 0.05), woodc)                                                         # cap
        k.add(lambda bm, s=s: blob(bm, 1.0, (s * X, 0, 7.92), (0.55, 0.55, 0.2), 2, 0.02, seed + (s > 0)), 'snow')
        k.add(lambda bm, s=s: blob(bm, 1.0, (s * X, 0, 1.08), (0.75, 0.75, 0.16), 2, 0.02, seed + 5 + (s > 0)), 'snow')
        # a lantern hanging on the front of each post
        k.add(lambda bm, s=s: rbox(bm, (0.36, 0.36, 0.5), (s * X, -0.58, 4.6), 0.03), 'lamp_warm', glow=True)
        k.add(lambda bm, s=s: rbox(bm, (0.44, 0.44, 0.08), (s * X, -0.58, 5.1), 0.02), 'black')
        k.add(lambda bm, s=s: rbox(bm, (0.44, 0.44, 0.06), (s * X, -0.58, 4.56), 0.02), 'black')
        k.add(lambda bm, s=s: rbox(bm, (0.08, 0.30, 0.08), (s * X, -0.42, 5.2), 0.01, segs=1), 'black')
    # the arch: a curved timber beam from post to post, rising to the middle
    pts = [Vector((-X + 2 * X * t, 0.0, 6.9 + 1.5 * math.sin(math.pi * t))) for t in [i / 12 for i in range(13)]]
    k.add(lambda bm: tube(bm, pts, 0.32, 0.32, 8), woodc)
    k.add(lambda bm: tube(bm, [p + Vector((0, 0, -0.85)) for p in pts[2:-2]], 0.14, 0.14, 6), woodc)                   # a lower brace
    for i in range(3, 11, 2):   # struts between the two
        k.add(lambda bm, i=i: tube(bm, [pts[i] + Vector((0, 0, -0.85)), pts[i]], 0.09, 0.09, 6), 'wood_dark')
    # pine garland wound along the front of the arch, bulbs hanging from it, snow lying on top
    gpts = [p + Vector((0, -0.34, -0.05)) for p in pts]
    k.add(lambda bm: tube(bm, gpts, 0.24, 0.24, 8), 'xmas_green')
    string_lights(k, [p + Vector((0, -0.55, -0.2)) for p in pts], every=0.55, s=1.8)
    k.add(lambda bm: tube(bm, [p + Vector((0, 0.02, 0.27)) for p in pts[1:-1]], 0.2, 0.2, 6), 'snow')
    # a wreath with a red bow in the middle, on the front
    top = pts[6]
    def wreath(bm):
        tube(bm, [Vector((math.cos(a) * 0.62, top.y - 0.62, top.z - 0.15 + math.sin(a) * 0.62)) for a in [i / 18 * 2 * math.pi for i in range(19)]], 0.2, 0.2, 8)
    k.add(wreath, 'xmas_green')
    for a in range(8):
        ang = a / 8 * 2 * math.pi + 0.3
        k.add(lambda bm, ang=ang: sphere(bm, 0.08, (math.cos(ang) * 0.62, top.y - 0.84, top.z - 0.15 + math.sin(ang) * 0.62), sub=1), 'xmas_red')
    for s in (-1, 1):
        k.add(lambda bm, s=s: sphere(bm, 0.2, (s * 0.2, top.y - 0.82, top.z - 0.8), (1.2, 0.5, 0.8)), 'xmas_red')
        k.add(lambda bm, s=s: tube(bm, [Vector((s * 0.1, top.y - 0.82, top.z - 0.85)), Vector((s * 0.25, top.y - 0.82, top.z - 1.2)), Vector((s * 0.35, top.y - 0.8, top.z - 1.5))], 0.08, 0.06, 6), 'xmas_red')
    k.add(lambda bm: blob(bm, 1.0, (0, -0.6, 0), (5.4, 1.4, 0.35), 3, 0.05, seed + 9), 'snow')                     # snow at the foot
    return k.obj('XmasGate')
