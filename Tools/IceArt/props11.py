"""Volcano theme props: a lava island of black columnar basalt, obsidian glass and fire crystals, with a 1960s
volcanology survey camp. Chunky and readable like the other themes: basalt columns get light ash tops (they read like
the snow tops of the ice theme), glowing faces (cracks, crystals, embers, lava) go to the unlit second material slot
and take the 'lava_grad' ramp (deep red -> orange -> yellow-white).

BasaltBlockA/B/C ('#'), BasaltLow ('f'), ObsidianSpire / FireCrystal / CharredTree ('t'), LavaRock / ObsidianChunk /
SurveyCrates ('x'), decor LavaCrack / PumiceRocks, border BasaltRimA/B/C,
backdrop Volcano, VolcanoGate behind each spawn, LavaVent (the base of an eruption vent)."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import *
from props3 import blob
from props7 import tube, with_gradient
from props8 import Kit, rbox, sphere
import grass

# glassy / crystalline pieces keep flat facets
common.FACETED = tuple(common.FACETED) + ('ObsidianSpire', 'ObsidianChunk', 'FireCrystal')

class VKit(Kit):
    """Kit + lava faces (a per-vertex position on the 'lava_grad' ramp, drawn unlit) + ramp faces (the vertical
    'char_grad' ramp by height, like the sand ramp of the Egypt kit)"""
    def __init__(self, colours):
        super().__init__(['lava_grad', 'char_grad'] + list(colours))
        self.lv = self.bm.faces.layers.int.new('lv')
        self.g = self.bm.faces.layers.int.new('g')
        self.lt = self.bm.verts.layers.float.new('lt')
    def add(self, build, colour, glow=False):
        self.mark(); build(self.bm); fs = self.fresh_faces(); self.paint(fs, colour, glow)
        for f in fs: f[self.lv] = 0; f[self.g] = 0
    def lava(self, build, t=0.6, glow=True):
        """glowing lava geometry; t = ramp position (a number, or a function of the vertex)"""
        self.mark(); build(self.bm); fs = self.fresh_faces(); self.bm.normal_update()
        for f in fs:
            f.material_index = self.ci('lava_grad'); f[self.gl] = 1 if glow else 0; f[self.lv] = 1; f[self.g] = 0
            for v in f.verts: v[self.lt] = t(v) if callable(t) else t
    def ramp(self, build):
        self.mark(); build(self.bm); fs = self.fresh_faces()
        for f in fs: f.material_index = self.ci('char_grad'); f[self.gl] = 0; f[self.lv] = 0; f[self.g] = 1
    def obj(self, name):
        ob = super().obj(name)
        me = ob.data
        if 'lv' in me.attributes and 'lt' in me.attributes:
            lv = me.attributes['lv'].data; lt = me.attributes['lt'].data; uv = me.uv_layers['UVMap'].data
            for p in me.polygons:
                if not lv[p.index].value: continue
                for li in p.loop_indices: uv[li].uv = grad_uv('lava_grad', lt[me.loops[li].vertex_index].value)
        for a in ('lv', 'lt'):
            if a in me.attributes: me.attributes.remove(me.attributes[a])
        return with_gradient(ob, 'g', 'char_grad')

def bas(f):
    """basalt: light ash on the flat tops, warm grey sides, darker away from the light"""
    n = f.normal
    if n.z > 0.75: return 'ash'
    return 'basalt' if lit(n) > -0.12 else 'basalt_dark'

def obs(f):
    """obsidian glass: facets towards the light catch a violet sheen"""
    return 'obsidian_light' if lit(f.normal) > 0.28 else 'obsidian'

def hexcol(bm, cx, cy, r, h, rnd, tilt=0.10, sides=6, rot=None, z0=0.0):
    """a basalt column: a slightly irregular hexagonal prism, its top a little tilted, the top edge rounded"""
    a0 = rnd.uniform(0, math.pi / 3) if rot is None else rot
    pts = [(cx + math.cos(a0 + i * 2 * math.pi / sides) * r * rnd.uniform(0.93, 1.05),
            cy + math.sin(a0 + i * 2 * math.pi / sides) * r * rnd.uniform(0.93, 1.05)) for i in range(sides)]
    tx, ty = rnd.uniform(-tilt, tilt), rnd.uniform(-tilt, tilt)
    bot = [bm.verts.new((x, y, z0)) for x, y in pts]
    top = [bm.verts.new((x, y, z0 + h + (x - cx) * tx + (y - cy) * ty)) for x, y in pts]
    bm.faces.new(list(reversed(bot))); ft = bm.faces.new(top)
    for i in range(sides): bm.faces.new((bot[i], bot[(i + 1) % sides], top[(i + 1) % sides], top[i]))
    bmesh.ops.bevel(bm, geom=list(ft.edges), offset=min(0.06, r * 0.16), offset_type='OFFSET', segments=1, profile=0.5, affect='EDGES', clamp_overlap=True)

def strip(bm, pts, w0, w1, z):
    """a flat ribbon on the ground along a polyline, w0 -> w1 wide (a crack)"""
    L = []
    for i, p in enumerate(pts):
        t = i / (len(pts) - 1); w = w0 + (w1 - w0) * t
        d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]); d.z = 0; d.normalize()
        s = Vector((-d.y, d.x, 0)) * w / 2
        L.append((bm.verts.new(Vector((p.x, p.y, z)) + s), bm.verts.new(Vector((p.x, p.y, z)) - s)))
    for i in range(len(L) - 1):
        a, b = L[i], L[i + 1]
        bm.faces.new((a[1], b[1], b[0], a[0]))

def crack_path(rnd, start, ang, length, n=6, wobble=0.45):
    pts = [Vector(start)]
    for i in range(n):
        ang += rnd.uniform(-wobble, wobble)
        pts.append(pts[-1] + Vector((math.cos(ang), math.sin(ang), 0)) * (length / n))
    return pts

def ground_crack(k, rnd, start, ang, length, w=0.10, n=6):
    """a glowing crack in the ground with a dark crust lip"""
    pts = crack_path(rnd, start, ang, length, n)
    k.add(lambda bm: strip(bm, pts, w * 1.9, w * 0.7, 0.008), 'basalt_dark')
    k.lava(lambda bm: strip(bm, pts, w, w * 0.25, 0.016), lambda v: 0.62)
    return pts

def puddle(bm, cx, cy, rx, ry, z, rnd, n=12):
    vs = [bm.verts.new((cx + math.cos(2 * math.pi * i / n) * rx * rnd.uniform(0.75, 1.1), cy + math.sin(2 * math.pi * i / n) * ry * rnd.uniform(0.75, 1.1), z)) for i in range(n)]
    bm.faces.new(vs)

def ash_heap(k, sx, sy, seed, z=0.0, h=0.14):
    k.add(lambda bm: blob(bm, 1.0, (0, 0, z), (sx, sy, h), 3, 0.03, seed), lambda f: 'ash' if f.normal.z > 0.5 else 'basalt')

def shard(bm, x, y, r, h, lean, rnd, sides=5):
    """an obsidian shard: a pointed faceted prism leaning along 'lean'"""
    a0 = rnd.uniform(0, 2 * math.pi)
    ring = lambda rr, z, sh: [bm.verts.new((x + math.cos(a0 + i * 2 * math.pi / sides) * rr * rnd.uniform(0.85, 1.1) + lean[0] * sh,
                                            y + math.sin(a0 + i * 2 * math.pi / sides) * rr * rnd.uniform(0.85, 1.1) + lean[1] * sh, z)) for i in range(sides)]
    b = ring(r, -0.05, 0.0); m = ring(r * 0.78, h * 0.62, h * 0.62)
    tip = bm.verts.new((x + lean[0] * h, y + lean[1] * h, h))
    bm.faces.new(list(reversed(b)))
    for i in range(sides):
        j = (i + 1) % sides
        bm.faces.new((b[i], b[j], m[j], m[i])); bm.faces.new((m[i], m[j], tip))

# ------------------------------------------------------------------ walls
def build_basaltblock(kind, seed, name):
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    if kind == 'A':     # a tight cluster of seven columns, the middle one tallest
        cols = [(0.0, 0.0, 0.27, 2.25)] + [(math.cos(a) * 0.46, math.sin(a) * 0.46, 0.255, rnd.uniform(1.55, 2.05)) for a in [i * math.pi / 3 + 0.5 for i in range(6)]]
        for (x, y, r, h) in cols: k.add(lambda bm, x=x, y=y, r=r, h=h: hexcol(bm, x, y, r, h, rnd), bas)
    elif kind == 'B':   # four thick columns with a fallen one lying across their tops
        tops = []
        for (x, y) in [(-0.34, -0.34), (0.34, -0.34), (-0.34, 0.34), (0.34, 0.34)]:
            h = rnd.uniform(1.35, 1.75); tops.append(h)
            k.add(lambda bm, x=x, y=y, h=h: hexcol(bm, x, y, 0.33, h, rnd, tilt=0.05), bas)
        zt = min(tops) + 0.18
        k.add(lambda bm: tube(bm, [Vector((-0.58, -0.46, zt + 0.06)), Vector((0.58, 0.46, zt + 0.16))], 0.27, 0.27, 6), bas)
    else:               # a stair of columns, split by a glowing fissure
        for i, x in enumerate([-0.50, -0.17, 0.17, 0.50]):
            for y in (-0.30, 0.30):
                h = 0.95 + i * 0.42 + rnd.uniform(-0.08, 0.08)
                k.add(lambda bm, x=x, y=y, h=h: hexcol(bm, x + rnd.uniform(-0.03, 0.03), y, 0.215, h, rnd, tilt=0.06), bas)
        k.lava(lambda bm: rbox(bm, (1.25, 0.05, 1.05), (0, 0, 0.0), 0.01, 0.0, segs=1), lambda v: 0.45 + 0.45 * min(1.0, v.co.z / 1.05))
    # glowing cracks running out from the foot of the block over the ground
    for j in range(3 if kind != 'B' else 2):
        a = rnd.uniform(0, 2 * math.pi)
        ground_crack(k, rnd, (math.cos(a) * 0.55, math.sin(a) * 0.55, 0), a + rnd.uniform(-0.4, 0.4), rnd.uniform(0.35, 0.6), 0.08, 4)
    return k.obj(name)

def build_basaltlow(seed=1121):
    """a 1.5 m run of short basalt columns (low cover), a glowing crack along its foot"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    x = -0.62
    while x <= 0.64:
        h = rnd.uniform(0.62, 0.95)
        k.add(lambda bm, x=x, h=h: hexcol(bm, x, rnd.uniform(-0.06, 0.06), 0.19, h, rnd, tilt=0.08), bas)
        x += 0.31
    for x in (-0.45, 0.0, 0.45):   # a second, shorter row behind
        k.add(lambda bm, x=x: hexcol(bm, x + rnd.uniform(-0.05, 0.05), 0.24, 0.15, rnd.uniform(0.35, 0.55), rnd), bas)
    ground_crack(k, rnd, (-0.75, -0.30, 0), 0.0, 1.5, 0.07, 6)
    return k.obj('BasaltLow')

# ------------------------------------------------------------------ tall pieces
def build_obsidianspire(seed=1131):
    """a tall spire of black volcanic glass: one big leaning shard and two smaller ones, on basalt rubble"""
    rnd = random.Random(seed)
    k = VKit(['obsidian', 'obsidian_light', 'basalt', 'basalt_dark', 'ash'])
    k.add(lambda bm: shard(bm, 0.0, 0.0, 0.46, 3.4, (0.05, 0.04), rnd), obs)
    k.add(lambda bm: shard(bm, 0.36, -0.22, 0.28, 2.1, (0.20, -0.10), rnd), obs)
    k.add(lambda bm: shard(bm, -0.30, 0.26, 0.24, 1.5, (-0.22, 0.14), rnd), obs)
    for j in range(4):
        a = j * math.pi / 2 + 0.6; s = rnd.uniform(0.16, 0.26)
        k.add(lambda bm, a=a, s=s: blob(bm, 1.0, (math.cos(a) * 0.55, math.sin(a) * 0.55, 0), (s * 1.3, s, s * 0.8), 1, s * 0.25, seed + j), bas)
    return k.obj('ObsidianSpire')

def build_firecrystal(seed=1141):
    """a cluster of glowing fire crystals bursting out of a basalt mound"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    k.add(lambda bm: blob(bm, 1.0, (0, 0, 0), (0.72, 0.68, 0.42), 2, 0.06, seed), bas)
    H = 2.5
    for (x, y, r, h, lx, ly) in [(0.0, 0.0, 0.24, 2.5, 0.04, 0.02), (0.30, -0.12, 0.17, 1.7, 0.28, -0.10), (-0.26, -0.18, 0.16, 1.5, -0.25, -0.14),
                                  (-0.08, 0.30, 0.15, 1.3, -0.06, 0.30), (0.22, 0.26, 0.12, 1.0, 0.25, 0.25)]:
        def crystal(bm, x=x, y=y, r=r, h=h, lx=lx, ly=ly):
            sides = 6; a0 = rnd.uniform(0, 1)
            b = [bm.verts.new((x + math.cos(a0 + i * math.pi / 3) * r, y + math.sin(a0 + i * math.pi / 3) * r, 0.1)) for i in range(sides)]
            m = [bm.verts.new((x + lx * h * 0.8 + math.cos(a0 + i * math.pi / 3) * r * 0.95, y + ly * h * 0.8 + math.sin(a0 + i * math.pi / 3) * r * 0.95, h * 0.8)) for i in range(sides)]
            tip = bm.verts.new((x + lx * h, y + ly * h, h))
            bm.faces.new(list(reversed(b)))
            for i in range(sides):
                j = (i + 1) % sides
                bm.faces.new((b[i], b[j], m[j], m[i])); bm.faces.new((m[i], m[j], tip))
        k.lava(crystal, lambda v: 0.35 + 0.65 * min(1.0, max(0.0, v.co.z / H)) ** 0.8)
    ash_heap(k, 0.85, 0.8, seed, h=0.10)
    return k.obj('FireCrystal')

def build_charredtree(seed=1151):
    """a burnt dead tree: black trunk and bare branches with glowing embers in the cracks, standing in ash"""
    rnd = random.Random(seed)
    k = VKit(['black', 'basalt_dark', 'basalt', 'ash'])
    charc = lambda f: 'basalt_dark' if lit(f.normal) > 0.1 else 'black'
    trunk = [Vector((0.08 * math.sin(t * 3.0), 0.05 * math.cos(t * 2.0), 2.6 * t)) for t in [i / 6 for i in range(7)]]
    k.add(lambda bm: tube(bm, trunk, 0.24, 0.07, 7), charc)
    for (zf, az, L, up) in [(0.45, 0.3, 1.0, 0.55), (0.62, 2.4, 0.85, 0.7), (0.78, 4.2, 0.7, 0.8), (0.55, 5.4, 0.6, 0.4)]:
        p0 = trunk[0].lerp(trunk[-1], zf); d = Vector((math.cos(az), math.sin(az), up)).normalized()
        pts = [p0, p0 + d * L * 0.5 + Vector((0, 0, 0.05)), p0 + d * L + Vector((0, 0, -0.08))]
        k.add(lambda bm, pts=pts: tube(bm, pts, 0.08, 0.02, 5), charc)
        tw = pts[-1] + Vector((math.cos(az + 0.8), math.sin(az + 0.8), 0.6)).normalized() * 0.3
        k.add(lambda bm, a=pts[1], tw=tw: tube(bm, [a, a.lerp(tw, 0.5), tw], 0.035, 0.012, 4), charc)
    for (z0, z1, a) in [(0.25, 0.75, 0.4), (1.1, 1.5, 2.6), (0.6, 0.95, 4.4)]:   # glowing fissures in the bark
        r = 0.24 - 0.17 * (z0 / 2.6) + 0.012
        k.lava(lambda bm, z0=z0, z1=z1, a=a, r=r: tube(bm, [Vector((math.cos(a + 0.15 * t) * r, math.sin(a + 0.15 * t) * r, z0 + (z1 - z0) * t)) for t in (0.0, 0.5, 1.0)], 0.03, 0.015, 4), 0.7)
    ash_heap(k, 0.65, 0.6, seed, h=0.16)
    return k.obj('CharredTree')

# ------------------------------------------------------------------ small pieces
def build_lavarock(seed=1161):
    """a basalt boulder split in three, molten rock glowing in the cracks between the pieces"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    k.lava(lambda bm: sphere(bm, 0.50, (0, 0, 0.48), (1.15, 1.0, 0.9), 2), 0.6)
    for j, (a, s) in enumerate([(0.3, (0.52, 0.46, 0.50)), (2.4, (0.48, 0.50, 0.46)), (4.4, (0.46, 0.44, 0.55))]):
        off = Vector((math.cos(a), math.sin(a), 0)) * 0.30
        k.add(lambda bm, off=off, s=s, j=j: blob(bm, 1.0, (off.x, off.y, 0.44 + 0.04 * j), s, 2, 0.05, seed + j), bas)
    ground_crack(k, rnd, (0.55, 0.0, 0), 0.2, 0.35, 0.07, 3)
    return k.obj('LavaRock')

def build_obsidianchunk(seed=1171):
    """a low cluster of obsidian shards on a basalt slab"""
    rnd = random.Random(seed)
    k = VKit(['obsidian', 'obsidian_light', 'basalt', 'basalt_dark', 'ash'])
    k.add(lambda bm: rbox(bm, (1.15, 1.0, 0.22), (0, 0, 0), 0.08, 0.3), bas)
    for (x, y, r, h, lx, ly) in [(0.0, 0.05, 0.30, 1.25, 0.05, 0.0), (0.32, -0.20, 0.22, 0.85, 0.25, -0.12), (-0.30, -0.15, 0.20, 0.75, -0.22, -0.1), (-0.10, 0.34, 0.18, 0.62, -0.08, 0.25)]:
        k.add(lambda bm, x=x, y=y, r=r, h=h, lx=lx, ly=ly: shard(bm, x, y, r, h, (lx, ly), rnd), obs)
    return k.obj('ObsidianChunk')

def build_surveycrates(seed=1181):
    """1960s volcanology kit: a stencilled crate with a drum seismograph on it, an army-surplus oil drum"""
    k = VKit(['wood', 'wood_dark', 'enamel', 'cream', 'black', 'olive', 'metal', 'ash', 'basalt'])
    woodc = lambda f: 'wood' if lit(f.normal) > -0.15 else 'wood_dark'
    k.add(lambda bm: rbox(bm, (0.82, 0.72, 0.62), (-0.22, -0.05, 0), 0.04, 0.12), woodc)
    for z in (0.08, 0.52): k.add(lambda bm, z=z: rbox(bm, (0.85, 0.75, 0.05), (-0.22, -0.05, z), 0.01, 0.12, segs=1), 'wood_dark')
    k.add(lambda bm: rbox(bm, (0.30, 0.02, 0.12), (-0.18, -0.43, 0.30), 0.005, 0.12, segs=1), 'black')     # stencil
    k.add(lambda bm: rbox(bm, (0.50, 0.36, 0.22), (-0.24, -0.06, 0.62), 0.03, 0.12), 'enamel')           # seismograph case
    k.add(lambda bm: tube(bm, [Vector((-0.42, -0.07, 0.92)), Vector((-0.06, 0.0, 0.92))], 0.10, 0.10, 10), 'cream')   # paper drum
    k.add(lambda bm: tube(bm, [Vector((-0.20, -0.20, 0.84)), Vector((-0.24, -0.16, 1.02))], 0.012, 0.012, 4), 'black')   # stylus
    prof = [(0.0, 0.0), (0.27, 0.0), (0.28, 0.05), (0.27, 0.85), (0.28, 0.88), (0.0, 0.88)]
    k.add(lambda bm: lathe(bm, prof, 12, None, 0, Matrix.Translation((0.40, 0.25, 0))), lambda f: 'olive' if lit(f.normal) > -0.15 else 'enamel')   # an army-surplus oil drum
    for z in (0.28, 0.58): k.add(lambda bm, z=z: lathe(bm, [(0.29, z), (0.29, z + 0.04), (0.0, z + 0.04)], 12, None, 0, Matrix.Translation((0.40, 0.25, 0))), 'metal')   # rolling hoops
    k.add(lambda bm: lathe(bm, [(0.06, 0.88), (0.06, 0.93), (0.0, 0.93)], 6, None, 0, Matrix.Translation((0.48, 0.30, 0))), 'metal')
    ash_heap(k, 0.8, 0.7, seed, h=0.08)
    return k.obj('SurveyCrates')

def build_lavacrack(seed=1201):
    """decor: a branching glowing crack in the ground (walk over it)"""
    rnd = random.Random(seed)
    k = VKit(['basalt_dark'])
    main = ground_crack(k, rnd, (-0.9, -0.1, 0), 0.1, 1.8, 0.13, 7)
    for i in (2, 4):
        ground_crack(k, rnd, tuple(main[i]), rnd.choice([-1, 1]) * rnd.uniform(0.8, 1.3), rnd.uniform(0.45, 0.7), 0.08, 4)
    return k.obj('LavaCrack')

def build_pumicerocks(seed=1211):
    """decor: a few loose basalt stones"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    for j in range(4):
        x, y = rnd.uniform(-0.5, 0.5), rnd.uniform(-0.4, 0.4); s = rnd.uniform(0.10, 0.22)
        k.add(lambda bm, x=x, y=y, s=s, j=j: blob(bm, 1.0, (x, y, 0), (s * 1.3, s, s * 0.8), 1, s * 0.25, seed + j), bas)
    return k.obj('PumiceRocks')

# ------------------------------------------------------------------ border, gate, backdrop
def build_basaltrim(kind, seed, name):
    """a border cliff of columnar basalt in place of the old border cliffs (7.6 m long, 5 m deep): columns stepping up
    from the arena side (front, -Y here = +Z in Unity, turned to the arena by the builder) to a solid back, about 4 m
    tall: low enough to see the lava lake behind it. 'A' plain steps, 'B' split by a glowing fissure, 'C' with
    columns toppled at its foot."""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    L, D = 7.6, 5.0
    k.add(lambda bm: rbox(bm, (L, D * 0.55, 3.3), (0, D * 0.18, 0), 0.25, segs=2), bas)   # solid core: nothing seen through
    rows = [(-1.75, 0.50, 1.4, 2.1), (-0.85, 0.52, 2.2, 2.9), (0.10, 0.55, 3.0, 3.7), (1.15, 0.58, 3.4, 4.2), (2.0, 0.55, 3.0, 3.8)]
    for ri, (y, r, h0, h1) in enumerate(rows):
        x = -L / 2 + r * 0.9 + (ri % 2) * r * 0.85
        while x < L / 2 - r * 0.6:
            if kind == 'B' and ri < 3 and abs(x) < 0.75: x += r * 1.7; continue   # the fissure
            h = rnd.uniform(h0, h1) * (0.75 if kind == 'C' and ri == 0 else 1.0)
            k.add(lambda bm, x=x, y=y, r=r, h=h: hexcol(bm, x + rnd.uniform(-0.06, 0.06), y + rnd.uniform(-0.08, 0.08), r * rnd.uniform(0.9, 1.05), h, rnd), bas)
            x += r * 1.7
    if kind == 'B':   # molten rock in the fissure, running out onto the ground in front
        k.lava(lambda bm: rbox(bm, (0.9, 2.6, 0.05), (0, -1.2, 0.02), 0.02, segs=1), 0.65)
        k.lava(lambda bm: rbox(bm, (0.12, 1.6, 2.4), (0, -0.4, 0.0), 0.02, segs=1), lambda v: 0.5 + 0.4 * min(1.0, v.co.z / 2.4))
        k.lava(lambda bm: puddle(bm, 0.0, -2.7, 0.75, 0.42, 0.02, rnd), 0.7)
    if kind == 'C':   # columns toppled at the foot
        for (x, a) in [(-2.2, 0.25), (1.6, -0.35), (2.9, 0.15)]:
            p = Vector((x, -2.35, 0.32)); d = Vector((math.cos(a), math.sin(a), 0.0))
            k.add(lambda bm, p=p, d=d: tube(bm, [p - d * 0.9, p + d * 0.9], 0.32, 0.32, 6), bas)
    for j in range(3):   # glowing cracks at the foot of the face
        x = rnd.uniform(-L / 2 + 0.8, L / 2 - 0.8)
        if kind == 'B' and abs(x) < 1.2: x = math.copysign(1.8, x if x else 1)
        ground_crack(k, rnd, (x, -2.25, 0), -math.pi / 2 + rnd.uniform(-0.5, 0.5), rnd.uniform(0.5, 0.9), 0.09, 4)
    return k.obj(name)

def build_volcanogate(seed=1221):
    """the gateway behind each spawn (in place of the base scene's gate arch, 10.8 x 9.6 m): two pillars of basalt
    columns bridged by one huge fallen column, molten rock glowing in its cracks and dripping from it. Front = -Y."""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    for s in (-1, 1):
        for (dx, dy, r, h) in [(0.0, 0.0, 0.62, 8.2), (0.95, 0.35, 0.50, 7.0), (-0.85, 0.30, 0.52, 7.5), (0.35, -0.80, 0.45, 5.6), (-0.45, 0.95, 0.48, 6.3), (0.9, -0.55, 0.38, 3.2)]:
            k.add(lambda bm, s=s, dx=dx, dy=dy, r=r, h=h: hexcol(bm, s * 3.7 + dx * s, dy, r, h * rnd.uniform(0.96, 1.04), rnd, tilt=0.08), bas)
        k.lava(lambda bm, s=s: puddle(bm, s * 3.2, -1.25, 0.8, 0.45, 0.02, rnd), 0.65)
    # the bridge: a lying column with two glowing cracks round it, drips of lava below
    a, b = Vector((-4.3, 0.0, 7.0)), Vector((4.3, 0.0, 7.25))
    k.add(lambda bm: tube(bm, [a, a.lerp(b, 0.5) + Vector((0, 0, 0.12)), b], 0.78, 0.74, 6), bas)
    for t in (0.36, 0.62):
        c = a.lerp(b, t) + Vector((0, 0, 0.06))
        k.lava(lambda bm, c=c: tube(bm, [c + Vector((-0.05, 0, 0)), c + Vector((0.05, 0, 0))], 0.80, 0.80, 6), 0.7)
    for j in range(6):
        x = rnd.uniform(-2.6, 2.6); c = a.lerp(b, (x + 4.3) / 8.6)
        L = rnd.uniform(0.25, 0.6)
        k.lava(lambda bm, c=c, L=L: lathe(bm, [(0.0, 0.0), (0.07, -0.05), (0.06, -L * 0.6), (0.0, -L)], 6, None, 0, Matrix.Translation((c.x, -0.55 + rnd.uniform(-0.1, 0.1), c.z - 0.6))), lambda v, c=c: 0.5 + 0.4 * min(1.0, (c.z - 0.6 - v.co.z) / 0.6))
    for j in range(4):
        x = rnd.uniform(-2.4, 2.4)
        ground_crack(k, rnd, (x, -0.9, 0), -math.pi / 2 + rnd.uniform(-0.6, 0.6), rnd.uniform(0.6, 1.1), 0.1, 4)
    return k.obj('VolcanoGate')

def build_volcano(seed=1231):
    """a far volcano for the backdrop: a rugged basalt cone, lava running down from a glowing crater"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    R, Hc, rc = 17.0, 15.0, 3.6
    prof = lambda z: rc + (R - rc) * (1.0 - z / Hc) ** 1.35   # concave flanks
    def cone(bm):
        rings = []; NZ, NA = 9, 20
        for i in range(NZ + 1):
            z = Hc * i / NZ; r = prof(z)
            rings.append([bm.verts.new((math.cos(2 * math.pi * j / NA) * r * rnd.uniform(0.9, 1.08), math.sin(2 * math.pi * j / NA) * r * rnd.uniform(0.9, 1.08), z + (rnd.uniform(-0.4, 0.4) if 0 < i < NZ else 0))) for j in range(NA)])
        inner = [bm.verts.new((math.cos(2 * math.pi * j / NA) * rc * 0.7, math.sin(2 * math.pi * j / NA) * rc * 0.7, Hc - 1.2)) for j in range(NA)]
        for i in range(NZ):
            for j in range(NA): bm.faces.new((rings[i][j], rings[i][(j + 1) % NA], rings[i + 1][(j + 1) % NA], rings[i + 1][j]))
        for j in range(NA): bm.faces.new((rings[NZ][j], rings[NZ][(j + 1) % NA], inner[(j + 1) % NA], inner[j]))
    k.add(cone, bas)
    k.lava(lambda bm: lathe(bm, [(rc * 0.72, Hc - 1.15), (0.0, Hc - 1.15)], 16), 0.85)   # the crater's lava lake
    for j in range(4):   # lava rivers down the flanks
        a = j * math.pi / 2 + rnd.uniform(-0.3, 0.3); pts = []
        for i in range(10):
            z = Hc - 0.3 - i * (Hc - 0.3) / 9; a += rnd.uniform(-0.08, 0.08)
            pts.append(Vector((math.cos(a) * (prof(z) + 0.35), math.sin(a) * (prof(z) + 0.35), z + 0.1)))
        def river(bm, pts=pts):
            L = []
            for i, p in enumerate(pts):
                t = i / (len(pts) - 1); w = 0.7 + 1.4 * t
                d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized(); out = Vector((p.x, p.y, 0)).normalized()
                s = d.cross(out).normalized() * w / 2
                L.append((bm.verts.new(p + s), bm.verts.new(p - s)))
            for i in range(len(L) - 1): bm.faces.new((L[i][0], L[i + 1][0], L[i + 1][1], L[i][1]))
        k.lava(river, lambda v: 0.55 + 0.35 * min(1.0, v.co.z / Hc))
    return k.obj('Volcano')

def build_lavavent(seed=1241):
    """the base of an eruption vent (EruptionVent in the game): a low cracked basalt cone round a glowing pit; its glow
    faces get a material the vent script brightens before it erupts"""
    rnd = random.Random(seed)
    k = VKit(['basalt', 'basalt_dark', 'ash'])
    prof = [(1.30, 0.0), (1.18, 0.10), (0.85, 0.22), (0.55, 0.20), (0.44, 0.06), (0.40, 0.02)]
    k.add(lambda bm: lathe(bm, prof, 14), bas)
    k.lava(lambda bm: lathe(bm, [(0.42, 0.03), (0.0, 0.03)], 14), 0.8)
    def zr(r):   # height of the cone surface at radius r
        for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
            if r1 <= r <= r0: return z0 + (z1 - z0) * (r0 - r) / (r0 - r1)
        return 0.0
    for j in range(7):   # radial glowing cracks over the cone
        a = j * 2 * math.pi / 7 + rnd.uniform(-0.2, 0.2); pts = []
        for i in range(6):
            r = 0.42 + 0.95 * i / 5; a += rnd.uniform(-0.12, 0.12)
            pts.append(Vector((math.cos(a) * r, math.sin(a) * r, zr(r) + 0.012)))
        def crack(bm, pts=pts):
            L = []
            for i, p in enumerate(pts):
                t = i / (len(pts) - 1); w = 0.11 * (1 - t) + 0.025
                d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]); d.z = 0; d.normalize()
                s = Vector((-d.y, d.x, 0)) * w / 2
                L.append((bm.verts.new(p + s), bm.verts.new(p - s)))
            for i in range(len(L) - 1): bm.faces.new((L[i][1], L[i + 1][1], L[i + 1][0], L[i][0]))
        k.lava(crack, 0.6)
    return k.obj('LavaVent')
