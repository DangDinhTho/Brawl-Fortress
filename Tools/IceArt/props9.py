"""Egypt theme props: a 1960s archaeological dig among sandstone ruins, in the soft chunky style of the v3 ice pieces.
Sandstone faces run the 'sand_grad' ramp (darker at the foot); painted details use lapis, turquoise, terracotta, gold.

SandBlockA/B/C ('#'), Obelisk / PalmTree / CatStatue / LotusColumn ('t'), DigCrates / Amphorae / Sarcophagus ('x'),
RuinLow ('f'), PapyrusGrass ('b', grass.build_frostgrass with the papyrus ramp), decor BrokenColumn, backdrop Pyramid
and DigTent."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *
from props3 import blob
from props7 import tube, with_gradient
from props8 import Kit, rbox, sphere
import grass

class GKit(Kit):
    """Kit + an int face layer 'g': faces flagged get the vertical sand ramp"""
    def __init__(self, colours):
        super().__init__(['sand_grad'] + list(colours))
        self.g = self.bm.faces.layers.int.new('g')
    def sand(self, build):
        """sandstone geometry: ramp colour"""
        self.mark(); build(self.bm)
        self.bm.normal_update()
        for f in self.fresh_faces():
            f.material_index = self.ci('sand_grad'); f[self.gl] = 0; f[self.g] = 1
    def add(self, build, colour, glow=False):
        self.mark(); build(self.bm); fs = self.fresh_faces(); self.paint(fs, colour, glow)
        for f in fs: f[self.g] = 0
    def obj(self, name):
        ob = super().obj(name)
        return with_gradient(ob, 'g', 'sand_grad')

GLYPH = ['lapis', 'turquoise', 'terracotta', 'gold', 'lapis', 'black']

def glyph_band(k, cx, cy, sx, sy, z0, z1, rnd, yaw=0.0, n=4):
    """a painted band round a block: a darker stripe with little coloured glyph shapes standing a hair proud of it"""
    R = Matrix.Rotation(yaw, 4, 'Z')
    k.add(lambda bm: rbox(bm, (sx + 0.03, sy + 0.03, z1 - z0), (cx, cy, z0), 0.015, yaw, segs=1), 'sandstone_dark')
    zc = (z0 + z1) / 2; h = (z1 - z0) * 0.6
    for side in range(4):
        half_u = (sx if side % 2 == 0 else sy) / 2 - 0.12
        d = (sy if side % 2 == 0 else sx) / 2 + 0.03
        for i in range(n):
            u = -half_u + (i + 0.5) * (2 * half_u / n)
            kind = rnd.randrange(3); col = GLYPH[(side * 3 + i) % len(GLYPH)]
            w, hh = (0.07, h) if kind == 0 else (0.12, h * 0.55) if kind == 1 else (0.10, h * 0.8)
            local = [(u, -d), (d, u), (-u, d), (-d, -u)][side]
            p = R @ Vector((local[0], local[1], 0)); c = Vector((cx + p.x, cy + p.y, zc - hh / 2))
            if kind == 2:   # a round sign (eye / sun)
                k.add(lambda bm, c=c, hh=hh: sphere(bm, hh / 2, (c.x, c.y, c.z + hh / 2), (1.0, 1.0, 1.0), 1), col)
            else:
                k.add(lambda bm, c=c, w=w, hh=hh, side=side: rbox(bm, (w if side % 2 == 0 else 0.02, 0.02 if side % 2 == 0 else w, hh), (c.x, c.y, c.z), 0.008, yaw, segs=1), col)

def sand_heap(k, sx, sy, seed, z=0.0, h=0.18):
    k.add(lambda bm: blob(bm, 1.0, (0, 0, z), (sx, sy, h), 3, 0.03, seed), 'sandstone')

# ------------------------------------------------------------------ walls
def build_sandblock(kind, seed, name):
    rnd = random.Random(seed)
    k = GKit(['sandstone', 'sandstone_dark'])
    if kind == 'A':     # one big carved block
        k.sand(lambda bm: rbox(bm, (1.44, 1.40, 2.05), (0, 0, 0), 0.07, rnd.uniform(-0.04, 0.04)))
        glyph_band(k, 0, 0, 1.44, 1.40, 1.30, 1.62, rnd)
        k.sand(lambda bm: rbox(bm, (1.50, 1.46, 0.14), (0, 0, 2.05), 0.04))          # a cornice
    elif kind == 'B':   # two blocks stacked, the top one a little turned and chipped
        k.sand(lambda bm: rbox(bm, (1.46, 1.42, 1.20), (0, 0, 0), 0.07))
        glyph_band(k, 0, 0, 1.46, 1.42, 0.62, 0.90, rnd)
        k.sand(lambda bm: rbox(bm, (1.20, 1.14, 0.90), (0.05, -0.04, 1.20), 0.07, 0.12))
        k.sand(lambda bm: rbox(bm, (0.40, 0.34, 0.26), (-0.45, 0.42, 2.10), 0.05, 0.6))   # a loose stone on top
    else:               # a block with half a column standing against it
        k.sand(lambda bm: rbox(bm, (1.44, 1.10, 1.70), (0, 0.15, 0), 0.07))
        glyph_band(k, 0, 0.15, 1.44, 1.10, 1.05, 1.32, rnd, n=3)
        k.sand(lambda bm: lathe(bm, [(0.34, 0.0), (0.32, 0.12), (0.26, 0.2), (0.25, 2.0), (0.30, 2.1), (0.30, 2.25), (0.0, 2.25)], 10, None, 0, Matrix.Translation((0.30, -0.42, 0))))
        k.add(lambda bm: lathe(bm, [(0.262, 1.30), (0.262, 1.48), (0.0, 1.48)], 10, None, 0, Matrix.Translation((0.30, -0.42, 0))), 'lapis')
    sand_heap(k, 0.9, 0.85, seed)
    return k.obj(name)

def build_ruinlow(seed=921):
    """a 1.5 m run of low sandstone wall, its top course broken off unevenly, sand drifted against it"""
    rnd = random.Random(seed)
    k = GKit(['sandstone'])
    k.sand(lambda bm: rbox(bm, (1.55, 0.42, 0.55), (0, 0, 0), 0.05))
    x = -0.75
    while x < 0.70:   # top course: blocks of different lengths and heights
        w = rnd.uniform(0.32, 0.5); h = rnd.uniform(0.15, 0.42)
        if rnd.random() < 0.8: k.sand(lambda bm, x=x, w=w, h=h: rbox(bm, (w - 0.03, 0.40, h), (x + w / 2, rnd.uniform(-0.02, 0.02), 0.55), 0.04, rnd.uniform(-0.05, 0.05)))
        x += w
    k.sand(lambda bm: rbox(bm, (0.30, 0.26, 0.22), (0.45, -0.40, 0.0), 0.04, 0.5))   # a fallen block
    sand_heap(k, 0.85, 0.4, seed, h=0.15)
    return k.obj('RuinLow')

# ------------------------------------------------------------------ tall pieces
def build_obelisk(seed=931):
    """a tapered obelisk on a plinth, columns of painted signs down each face, a gold pyramidion"""
    rnd = random.Random(seed)
    k = GKit(['sandstone', 'gold', 'lapis'])
    k.sand(lambda bm: rbox(bm, (0.95, 0.95, 0.35), (0, 0, 0), 0.05))
    def shaft(bm):
        vs = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((0, 0, 0.35 + 1.4)) @ Matrix.Diagonal((0.62, 0.62, 2.8, 1)))['verts']
        for v in vs:
            if v.co.z > 2.0: v.co.x *= 0.62; v.co.y *= 0.62
    k.sand(shaft)
    def tip(bm):
        b = [bm.verts.new((sx * 0.192, sy * 0.192, 3.15)) for sx, sy in [(-1, -1), (1, -1), (1, 1), (-1, 1)]]
        t = bm.verts.new((0, 0, 3.55)); bm.faces.new(list(reversed(b)))
        for i in range(4): bm.faces.new((b[i], b[(i + 1) % 4], t))
    k.add(tip, 'gold')
    for side in range(4):   # painted signs, getting narrower with the shaft
        for i in range(5):
            z = 0.75 + i * 0.45; half = 0.31 - (z - 0.35) / 2.8 * 0.118 + 0.012
            col = GLYPH[(i + side) % 4]
            loc = [(0, -half), (half, 0), (0, half), (-half, 0)][side]
            sz = (0.16, 0.02, 0.26) if side % 2 == 0 else (0.02, 0.16, 0.26)
            k.add(lambda bm, loc=loc, z=z, sz=sz: rbox(bm, sz, (loc[0], loc[1], z), 0.01, segs=1), col)
    sand_heap(k, 0.75, 0.75, seed)
    return k.obj('Obelisk')

def build_palm(seed=941):
    """a date palm: a gently curved ringed trunk, a crown of long arching fronds, a cluster of dates"""
    rnd = random.Random(seed)
    k = Kit(['wood', 'wood_dark', 'palm', 'palm_dark', 'orange', 'sandstone'])
    pts = [Vector((0.35 * math.sin(t * 1.3) * t, 0.0, 3.1 * t)) for t in [i / 8 for i in range(9)]]
    for i in range(8):   # trunk as stacked rings: the ridges read as palm bark
        a, b = pts[i], pts[i + 1]
        k.add(lambda bm, a=a, b=b: tube(bm, [a, a.lerp(b, 0.85), b], 0.17 - i * 0.008, 0.14 - i * 0.008, 7), 'wood' if i % 2 == 0 else 'wood_dark')
    top = pts[-1]
    for j in range(10):
        az = j * 2 * math.pi / 10 + rnd.uniform(-0.2, 0.2); L = rnd.uniform(1.5, 1.9); droop = rnd.uniform(0.7, 1.0)
        d = Vector((math.cos(az), math.sin(az), 0)); side = Vector((-d.y, d.x, 0))
        spine = [top + d * (L * t) + Vector((0, 0, 0.45 * math.sin(t * 2.2) - droop * t * t)) for t in [i / 6 for i in range(7)]]
        def frond(bm, spine=spine, side=side):
            # a leaf strip either side of the spine, widest in the middle, with notched edges
            for s in (-1, 1):
                prev = None
                for i, p in enumerate(spine):
                    t = i / (len(spine) - 1); w = 0.42 * math.sin(math.pi * min(1, t * 1.1)) + 0.03
                    q0 = p; q1 = p + side * s * w + Vector((0, 0, -0.12 * w)); dn = Vector((0, 0, -0.012))
                    a0 = bm.verts.new(q0); a1 = bm.verts.new(q1); b0 = bm.verts.new(q0 + dn); b1 = bm.verts.new(q1 + dn)
                    if prev:   # both sides (own verts for the underside): a single-sided strip vanishes from behind
                        bm.faces.new((prev[0], prev[1], a1, a0)); bm.faces.new((b0, b1, prev[3], prev[2]))
                    prev = (a0, a1, b0, b1)
        k.add(frond, 'palm' if j % 2 else 'palm_dark')
    for j in range(5): k.add(lambda bm, j=j: sphere(bm, 0.07, (top.x + 0.12 * math.cos(j * 1.3), 0.12 * math.sin(j * 1.3), top.z - 0.15 - 0.03 * j), sub=1), 'orange')
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.6, 0.6, 0.14), 3, 0.02, seed), 'sandstone')
    return k.obj('PalmTree')

def build_catstatue(seed=951):
    """a seated cat statue of black stone with a gold collar and earring, on a painted plinth"""
    k = GKit(['black', 'stone_dark', 'gold', 'lapis'])
    stone = 'black'
    k.sand(lambda bm: rbox(bm, (0.95, 1.25, 0.45), (0, 0, 0), 0.05))
    glyph_band(k, 0, 0, 0.95, 1.25, 0.15, 0.32, random.Random(seed), n=2)
    base = 0.45
    k.add(lambda bm: sphere(bm, 0.42, (0, 0.10, base + 0.40), (0.85, 1.05, 1.0), 3), stone)          # haunches
    k.add(lambda bm: sphere(bm, 0.30, (0, -0.12, base + 0.95), (0.75, 0.75, 1.35), 3), stone)        # chest
    k.add(lambda bm: sphere(bm, 0.24, (0, -0.18, base + 1.48), (0.95, 0.9, 0.9), 3), stone)          # head
    k.add(lambda bm: sphere(bm, 0.10, (0, -0.36, base + 1.42), (0.9, 0.8, 0.7), 2), stone)           # muzzle
    for s in (-1, 1):
        k.add(lambda bm, s=s: lathe(bm, [(0.09, 0.0), (0.0, 0.22)], 4, None, 0.78, Matrix.Translation((s * 0.13, -0.16, base + 1.62))), stone)   # ears
        k.add(lambda bm, s=s: tube(bm, [Vector((s * 0.13, -0.25, base + 0.05)), Vector((s * 0.12, -0.32, base + 0.55)), Vector((s * 0.11, -0.26, base + 0.85))], 0.07, 0.06, 6), stone)   # front legs
        k.add(lambda bm, s=s: sphere(bm, 0.035, (s * 0.09, -0.38, base + 1.53), sub=1), 'gold')       # eyes
    pts = [Vector((0.30 * math.sin(a), -0.20 + 0.42 * math.cos(a), base + 0.05)) for a in [i / 8 * math.pi * 1.1 for i in range(9)]]
    k.add(lambda bm: tube(bm, pts, 0.06, 0.04, 6), stone)                                           # tail round the feet
    k.add(lambda bm: tube(bm, [Vector((0.24 * math.cos(a), -0.12 + 0.24 * math.sin(a), base + 1.22)) for a in [i / 12 * 2 * math.pi for i in range(13)]], 0.04, 0.04, 6), 'gold')   # collar
    k.add(lambda bm: sphere(bm, 0.05, (0.0, -0.36, base + 1.15), sub=1), 'lapis')                  # pendant
    k.add(lambda bm: tube(bm, [Vector((0.19, -0.16, base + 1.52 - 0.06 * math.cos(a))) + Vector((0, 0.05 * math.sin(a), 0)) for a in [i / 8 * 2 * math.pi for i in range(9)]], 0.012, 0.012, 4), 'gold')   # earring
    sand_heap(k, 0.8, 0.95, seed, h=0.12)
    return k.obj('CatStatue')

def build_lotuscolumn(seed=961):
    """a free-standing column with a flared lotus capital, painted bands round its neck"""
    k = GKit(['sandstone', 'lapis', 'turquoise', 'terracotta'])
    k.sand(lambda bm: lathe(bm, [(0.50, 0.0), (0.50, 0.18), (0.38, 0.26), (0.34, 0.4), (0.30, 2.30), (0.30, 2.42), (0.0, 2.42)], 12))
    for i, (z, col) in enumerate([(1.95, 'lapis'), (2.05, 'turquoise'), (2.15, 'terracotta')]):
        k.add(lambda bm, z=z: lathe(bm, [(0.312, z), (0.312, z + 0.08), (0.0, z + 0.08)], 12), col)
    k.sand(lambda bm: lathe(bm, [(0.30, 2.40), (0.46, 2.62), (0.58, 2.82), (0.60, 2.92), (0.0, 2.92)], 12, lambda j, kk, r, z: (r * (1.0 + 0.06 * math.cos(j * math.pi / 2 * 2)), z), 0.1))
    k.sand(lambda bm: rbox(bm, (0.95, 0.95, 0.14), (0, 0, 2.92), 0.03))   # abacus
    sand_heap(k, 0.7, 0.7, seed, h=0.12)
    return k.obj('LotusColumn')

# ------------------------------------------------------------------ small pieces
def build_digcrates(seed=971):
    """expedition crates: two stencilled wooden crates and a canvas tarp roped over a third"""
    k = Kit(['wood', 'wood_dark', 'khaki', 'olive', 'black', 'sandstone'])
    woodc = lambda f: 'wood' if lit(f.normal) > -0.15 else 'wood_dark'
    k.add(lambda bm: rbox(bm, (0.80, 0.80, 0.70), (-0.28, -0.10, 0), 0.04), woodc)
    k.add(lambda bm: rbox(bm, (0.62, 0.62, 0.52), (-0.25, -0.08, 0.70), 0.04, 0.3), woodc)
    for z in (0.10, 0.58):   # slats / metal bands
        k.add(lambda bm, z=z: rbox(bm, (0.83, 0.83, 0.05), (-0.28, -0.10, z), 0.01, segs=1), 'wood_dark')
    k.add(lambda bm: rbox(bm, (0.30, 0.02, 0.12), (-0.28, -0.515, 0.32), 0.005, segs=1), 'black')    # stencil mark
    k.add(lambda bm: rbox(bm, (0.70, 0.70, 0.62), (0.42, 0.32, 0), 0.10, 0.2, segs=3), lambda f: 'khaki' if lit(f.normal) > -0.15 else 'olive')
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.85, 0.80, 0.10), 3, 0.02, seed), 'sandstone')
    return k.obj('DigCrates')

def build_amphorae(seed=981):
    """three big clay jars, painted bands, one lying on its side"""
    k = Kit(['terracotta', 'sandstone_dark', 'black', 'sandstone'])
    prof = [(0.0, 0.0), (0.12, 0.02), (0.30, 0.25), (0.34, 0.45), (0.26, 0.75), (0.11, 0.90), (0.10, 1.02), (0.15, 1.06), (0.0, 1.06)]
    for (x, y, s, rot) in [(-0.30, -0.10, 1.0, None), (0.30, 0.20, 0.85, None), (0.25, -0.45, 0.75, 'lie')]:
        M = Matrix.Translation((x, y, 0)) @ Matrix.Diagonal((s, s, s, 1))
        if rot: M = Matrix.Translation((x, y, 0.27 * s)) @ Matrix.Rotation(math.pi / 2, 4, 'Y') @ Matrix.Translation((0, 0, -0.5)) @ Matrix.Diagonal((s, s, s, 1))
        k.add(lambda bm, M=M: lathe(bm, prof, 10, None, 0, M), 'terracotta')
        k.add(lambda bm, M=M: lathe(bm, [(0.345, 0.46), (0.345, 0.54), (0.0, 0.54)], 10, None, 0, M), 'black')
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.70, 0.70, 0.08), 3, 0.02, seed), 'sandstone')
    return k.obj('Amphorae')

def build_sarcophagus(seed=991):
    """a painted coffin lying on a stone base: gold, with lapis stripes and a face mask"""
    k = GKit(['gold', 'gold_dark', 'lapis', 'black', 'turquoise'])
    k.sand(lambda bm: rbox(bm, (0.95, 1.55, 0.35), (0, 0, 0), 0.04))
    def coffin(bm):
        vs = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((0, 0, 0.35 + 0.25)) @ Matrix.Diagonal((0.70, 1.40, 0.50, 1)))['verts']
        for v in vs:   # taper to the feet
            if v.co.y > 0.3: v.co.x *= 0.70
        edges = list({e for v in vs for e in v.link_edges})
        bmesh.ops.bevel(bm, geom=edges, offset=0.12, offset_type='OFFSET', segments=3, profile=0.5, affect='EDGES', clamp_overlap=True)
    k.add(coffin, 'gold')
    for y in (-0.05, 0.15, 0.35, 0.55):
        k.add(lambda bm, y=y: rbox(bm, (0.62 - y * 0.25, 0.07, 0.02), (0, y, 0.85), 0.01, segs=1), 'lapis')
    k.add(lambda bm: sphere(bm, 0.22, (0, -0.48, 0.86), (1.0, 1.0, 0.45), 2), 'gold_dark')          # the mask
    for s in (-1, 1): k.add(lambda bm, s=s: sphere(bm, 0.035, (s * 0.08, -0.55, 0.95), sub=1), 'black')
    k.add(lambda bm: rbox(bm, (0.50, 0.04, 0.30), (0, -0.40, 0.70), 0.02, segs=1), 'turquoise')      # the collar
    sand_heap(k, 0.75, 1.0, seed, h=0.10)
    return k.obj('Sarcophagus')

def build_brokencolumn(seed=995):
    """column drums fallen in the sand (decor, walk through)"""
    rnd = random.Random(seed)
    k = GKit(['sandstone'])
    for (x, y, az, L) in [(-0.40, 0.0, 0.3, 0.60), (0.25, 0.15, 0.1, 0.55), (0.75, -0.25, 1.2, 0.45)]:
        M = Matrix.Translation((x, y, 0.27)) @ Matrix.Rotation(az, 4, 'Z') @ Matrix.Rotation(math.pi / 2, 4, 'Y') @ Matrix.Translation((0, 0, -L / 2))
        k.sand(lambda bm, M=M, L=L: lathe(bm, [(0.27, 0.0), (0.27, L), (0.0, L)], 10, None, 0, M))
    sand_heap(k, 0.9, 0.6, seed, h=0.10)
    return k.obj('BrokenColumn')

def build_pyramid(seed=997):
    """a far pyramid for the backdrop: stepped courses, a gold capstone"""
    k = GKit(['gold', 'sandstone'])
    for i in range(7):
        s = 8.0 - i * 1.1; z = i * 0.85
        k.sand(lambda bm, s=s, z=z: rbox(bm, (s, s, 0.86), (0, 0, z), 0.08))
    def cap(bm):
        b = [bm.verts.new((sx * 0.6, sy * 0.6, 7 * 0.85)) for sx, sy in [(-1, -1), (1, -1), (1, 1), (-1, 1)]]
        t = bm.verts.new((0, 0, 7 * 0.85 + 0.9)); bm.faces.new(list(reversed(b)))
        for i in range(4): bm.faces.new((b[i], b[(i + 1) % 4], t))
    k.add(cap, 'gold')
    return k.obj('Pyramid')

def build_digtent(seed=999):
    """a 1960s expedition ridge tent of khaki canvas on wooden poles"""
    k = Kit(['khaki', 'olive', 'wood_dark', 'sandstone'])
    def tent(bm):
        L, W, H = 1.8, 1.5, 1.3
        pts = [(-W / 2, 0.0), (0.0, H), (W / 2, 0.0)]
        f = [bm.verts.new((x, -L / 2, z)) for x, z in pts]; b = [bm.verts.new((x, L / 2, z)) for x, z in pts]
        bm.faces.new((f[0], f[1], b[1], b[0])); bm.faces.new((f[1], f[2], b[2], b[1]))
        bm.faces.new((b[0], b[1], b[2])); bm.faces.new((f[2], f[1], f[0]))
    k.add(tent, lambda f: 'khaki' if lit(f.normal) > -0.1 else 'olive')
    for y in (-0.92, 0.92): k.add(lambda bm, y=y: tube(bm, [Vector((0, y, 0)), Vector((0, y, 1.42))], 0.03, 0.03, 5), 'wood_dark')
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (1.1, 1.2, 0.08), 3, 0.02, seed), 'sandstone')
    return k.obj('DigTent')

def build_papyrus():
    return grass.build_frostgrass(97, 'PapyrusGrass', 'papyrus_grad', 0.62)

# ------------------------------------------------------------------ border
def build_sandcliff(kind, seed, name):
    """a border cliff of layered sandstone, the size of the old border cliffs (7.6 x 5 x 6 m) so it takes their place;
    its detailed face is the front (-Y here, +Z in Unity) and is turned to the arena by the map builder.
    'A' weathered strata, 'B' strata with a carved temple front, 'C' strata with a slumped step and fallen blocks."""
    rnd = random.Random(seed)
    k = Kit(['sandstone', 'sandstone_dark', 'sand_grad', 'gold', 'lapis', 'turquoise', 'terracotta'])
    L, D, H = 7.6, 5.0, 6.0
    # solid core behind everything: nothing can be seen through the border
    k.add(lambda bm: rbox(bm, (L, D * 0.62, H * 0.96), (0, D * 0.17, 0), 0.25, segs=2), 'sandstone_dark')
    # strata: rounded slabs, each a little set back and of uneven length, light and dark courses alternating
    n = 5; z = 0.0; front = -D / 2
    tiers = []
    for i in range(n):
        h = H / n * rnd.uniform(0.85, 1.15)
        set_back = i * rnd.uniform(0.28, 0.42) + (0.6 if (kind == 'C' and i >= 2) else 0.0)
        y0 = front + set_back
        depth = D / 2 - y0          # from its front back to the rear face of the cliff
        xl = L + rnd.uniform(-0.25, 0.25)
        tiers.append((z, h, y0))
        for seg in range(3):   # each course broken into a few blocks, so its front edge is not one straight line
            if kind == 'B' and i < 3 and seg == 1: continue   # a recess for the temple front (the core still closes it)
            w = xl / 3; x = -xl / 2 + w * (seg + 0.5) + rnd.uniform(-0.08, 0.08)
            yj = y0 + rnd.uniform(-0.12, 0.12)
            k.add(lambda bm, x=x, z=z, h=h, w=w, yj=yj, depth=depth: rbox(bm, (w + 0.06, depth, h + 0.02), (x, yj + depth / 2, z), 0.22, rnd.uniform(-0.02, 0.02), segs=2),
                  'sandstone' if i % 2 == 0 else 'sandstone_dark')
        z += h
    top = z
    k.add(lambda bm: blob(bm, 1.0, (0, 0.3, top - 0.1), (L * 0.42, D * 0.30, 0.35), 3, 0.05, seed), 'sandstone')   # sand drifted on top
    if kind == 'B':   # a carved temple front in the middle of the face
        y = front - 0.05
        k.add(lambda bm: rbox(bm, (3.4, 0.5, 3.6), (0, y + 0.25, 0), 0.06), 'sandstone')                          # wall slab
        k.add(lambda bm: rbox(bm, (1.1, 0.25, 2.3), (0, y - 0.02, 0), 0.04), 'sandstone_dark')                    # doorway (dark recess)
        for x in (-1.25, 1.25):
            k.add(lambda bm, x=x: lathe(bm, [(0.34, 0.0), (0.30, 0.15), (0.28, 3.0), (0.40, 3.25), (0.0, 3.25)], 10, None, 0, Matrix.Translation((x, y - 0.1, 0))), 'sandstone')
            for j, (zz, col) in enumerate([(2.55, 'lapis'), (2.68, 'turquoise'), (2.81, 'terracotta')]):
                k.add(lambda bm, x=x, zz=zz: lathe(bm, [(0.29, zz), (0.29, zz + 0.1), (0.0, zz + 0.1)], 10, None, 0, Matrix.Translation((x, y - 0.1, 0))), col)
        k.add(lambda bm: rbox(bm, (3.7, 0.7, 0.55), (0, y + 0.1, 3.25), 0.05), 'sandstone')                       # lintel
        for i in range(9):   # painted band on the lintel
            k.add(lambda bm, i=i: rbox(bm, (0.16, 0.03, 0.26), (-1.6 + i * 0.4, y - 0.26, 3.39), 0.01, segs=1), GLYPH[i % 4])
        k.add(lambda bm: sphere(bm, 0.30, (0, y - 0.28, 3.55 + 0.45), (1.0, 0.4, 1.0), 2), 'gold')                # sun disc
        for s in (-1, 1):   # its wings
            k.add(lambda bm, s=s: rbox(bm, (0.9, 0.06, 0.18), (s * 0.75, y - 0.25, 3.95), 0.03, s * 0.12), 'lapis')
    # rubble at the foot of the face, and sand drifted against it
    for j in range(5 if kind == 'C' else 3):
        x = rnd.uniform(-L / 2 + 0.6, L / 2 - 0.6); s = rnd.uniform(0.35, 0.75)
        if kind == 'B' and abs(x) < 2.0: x = math.copysign(2.4 + rnd.uniform(0, 0.8), x if x else 1)
        k.add(lambda bm, x=x, s=s: rbox(bm, (s * 1.3, s, s * 0.9), (x, front - 0.45 - s * 0.3, 0), 0.08, rnd.uniform(0, 1.5)), 'sandstone' if j % 2 else 'sandstone_dark')
    k.add(lambda bm: blob(bm, 1.0, (0, front - 0.6, 0), (L * 0.50, 1.1, 0.45), 3, 0.05, seed + 1), 'sandstone')
    return k.obj(name)

def build_templegate(seed=915):
    """the temple gateway behind each team's spawn (in place of the base scene's gate arch, 10.8 x 9.6 m): two tapered
    pylons with painted bands, a lintel with a cavetto cornice and a winged gold sun disc. Front = -Y (+Z in Unity)."""
    rnd = random.Random(seed)
    k = Kit(['sandstone', 'sandstone_dark', 'gold', 'lapis', 'turquoise', 'terracotta'])
    W, D, H = 3.9, 2.4, 8.6
    for s in (-1, 1):
        cx = s * (1.6 + W / 2)
        def pylon(bm, cx=cx):
            vs = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((cx, 0, H / 2)) @ Matrix.Diagonal((W, D, H, 1)))['verts']
            for v in vs:   # battered walls: narrower at the top
                if v.co.z > H / 2: v.co.x = cx + (v.co.x - cx) * 0.78; v.co.y *= 0.8
            edges = list({e for v in vs for e in v.link_edges})
            bmesh.ops.bevel(bm, geom=edges, offset=0.12, offset_type='OFFSET', segments=2, profile=0.5, affect='EDGES', clamp_overlap=True)
        k.add(pylon, 'sandstone')
        k.add(lambda bm, cx=cx: rbox(bm, (W * 0.82, D * 0.86, 0.45), (cx, 0, H), 0.08), 'sandstone_dark')          # cornice
        for j, (z, col) in enumerate([(1.2, 'terracotta'), (1.45, 'turquoise'), (1.7, 'lapis')]):                    # painted bands
            wz = W * (1.0 - 0.22 * z / H) + 0.04
            k.add(lambda bm, cx=cx, z=z, wz=wz: rbox(bm, (wz, 0.05, 0.18), (cx, -D / 2 * (1.0 - 0.2 * z / H) - 0.02, z), 0.01, segs=1), col)
        for r in range(4):   # a column of painted signs on each pylon front
            for c in range(2):
                z = 3.0 + r * 1.1; x = cx + (c - 0.5) * 1.1
                k.add(lambda bm, x=x, z=z: rbox(bm, (0.32, 0.05, 0.55), (x, -D / 2 * (1.0 - 0.2 * z / H) - 0.03, z), 0.02, segs=1), GLYPH[(r + c) % 4])
    k.add(lambda bm: rbox(bm, (3.6, D * 0.85, 1.3), (0, 0, 6.1), 0.08), 'sandstone')                                 # lintel
    k.add(lambda bm: rbox(bm, (3.9, D * 0.95, 0.35), (0, 0, 7.4), 0.08), 'sandstone_dark')                            # its cornice
    k.add(lambda bm: sphere(bm, 0.42, (0, -D * 0.43 - 0.08, 6.75), (1.0, 0.35, 1.0), 2), 'gold')                     # sun disc
    for s in (-1, 1):
        k.add(lambda bm, s=s: rbox(bm, (1.2, 0.07, 0.28), (s * 0.95, -D * 0.43 - 0.06, 6.62), 0.03, s * 0.1), 'lapis')  # wings
        k.add(lambda bm, s=s: rbox(bm, (0.9, 0.07, 0.16), (s * 0.85, -D * 0.43 - 0.06, 6.45), 0.02, s * 0.15), 'turquoise')
    k.add(lambda bm: blob(bm, 1.0, (0, -1.0, 0), (5.5, 1.4, 0.4), 3, 0.05, seed), 'sandstone')                       # sand at the foot
    return k.obj('TempleGate')
