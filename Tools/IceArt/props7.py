"""More ice-age props for Frost Peak (natural pieces only), in the soft chunky style of the v3 GlacierBlock:
IceChunkA/B (small ice blocks, 'x' obstacles), FrozenLog ('x'), IceShardCluster ('t'), and walk-through decor
RibCage, BeastSkull, BonePile, IceHole."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *
from props3 import blob
import props6

def chunk(kind, seed, name):
    return props6.build_glacierblock_v(kind, seed, name)

# ------------------------------------------------------------------ helpers
def tube(bm, pts, r0, r1, segs=8, caps=True):
    """a bent tube through pts, radius r0 -> r1"""
    rings = []
    for i, p in enumerate(pts):
        t = i / (len(pts) - 1); r = r0 + (r1 - r0) * t
        d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        a = d.orthogonal().normalized(); b = d.cross(a).normalized()
        rings.append([bm.verts.new(p + (a * math.cos(2 * math.pi * j / segs) + b * math.sin(2 * math.pi * j / segs)) * max(r, 0.004)) for j in range(segs)])
    for i in range(len(rings) - 1):
        for j in range(segs): bm.faces.new((rings[i][j], rings[i][(j + 1) % segs], rings[i + 1][(j + 1) % segs], rings[i + 1][j]))
    if caps:
        bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])

def bone(bm, a, b, r=0.045):
    """a long bone: a shaft with a knob at each end"""
    a, b = Vector(a), Vector(b)
    tube(bm, [a.lerp(b, t) for t in (0.0, 0.5, 1.0)], r, r * 0.85, 6)
    d = (b - a).normalized(); s = d.orthogonal().normalized() * r * 0.9
    for p in (a, b):
        for o in (s, -s):
            bmesh.ops.create_icosphere(bm, subdivisions=1, radius=r * 1.25, matrix=Matrix.Translation(p + o))

def snow_base(bm, sx, sy, seed, z=0.0, h=0.22):
    blob(bm, 1.0, (0, 0, z), (sx, sy, h), 3, 0.03, seed)

def with_gradient(ob, grad_faces_attr='g', cell='ice_grad'):
    """faces flagged in the int face attribute get a vertical ramp (by height within the flagged geometry)"""
    me = ob.data
    if grad_faces_attr not in me.attributes: return ob
    g = me.attributes[grad_faces_attr].data; uv = me.uv_layers['UVMap'].data
    zs = [me.vertices[vi].co.z for p in me.polygons if g[p.index].value for vi in p.vertices]
    z0, z1 = (min(zs), max(zs)) if zs else (0.0, 1.0)
    for p in me.polygons:
        if not g[p.index].value: continue
        for li in p.loop_indices:
            uv[li].uv = grad_uv(cell, (me.vertices[me.loops[li].vertex_index].co.z - z0) / max(z1 - z0, 1e-3))
    me.attributes.remove(me.attributes[grad_faces_attr])
    return ob

# ------------------------------------------------------------------ obstacles
def build_frozenlog(seed=501):
    """a dead log locked in a long rounded slab of clear ice, its ends sticking out, snow on top"""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['ice_grad', 'wood', 'wood_dark', 'snow', 'snow_shade']
    gl = bm.faces.layers.int.new('g')
    def ice(bm):
        vs = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((0, 0, 0.36)) @ Matrix.Diagonal((1.10, 0.78, 0.72, 1)))['verts']
        edges = list({e for v in vs for e in v.link_edges})
        bmesh.ops.bevel(bm, geom=edges, offset=0.16, offset_type='OFFSET', segments=3, profile=0.5, affect='EDGES', clamp_overlap=True)
    def icec(f): f[gl] = 1; return 0
    part(bm, ice, icec)
    def log(bm):
        pts = [Vector((-0.70 + 1.4 * t, 0.04 * math.sin(t * 5), 0.36 + 0.03 * math.sin(t * 3))) for t in [i / 6 for i in range(7)]]
        tube(bm, pts, 0.21, 0.18, 10)
        # a broken branch stub
        tube(bm, [Vector((0.30, 0.1, 0.50)), Vector((0.42, 0.30, 0.66)), Vector((0.48, 0.40, 0.76))], 0.07, 0.04, 6)
    part(bm, log, lambda f: 2 if abs(f.normal.x) > 0.8 else (1 if lit(f.normal) > -0.1 else 2))
    part(bm, lambda bm: blob(bm, 1.0, (0.05, 0.0, 0.70), (0.52, 0.30, 0.10), 3, 0.02, seed), lambda f: 3 if f.normal.z > -0.3 else 4)
    return with_gradient(bm_to_obj(bm, 'FrozenLog', C))

def build_shardcluster(seed=511):
    """a cluster of clear ice crystals: bevelled hexagonal prisms with soft points, leaning out of a snow mound"""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['ice_grad', 'snow', 'snow_shade']
    gl = bm.faces.layers.int.new('g')
    spec = [(0.0, 0.0, 0.30, 2.2, 0.05, 0.0), (0.42, 0.10, 0.22, 1.5, 0.35, 0.3), (-0.38, 0.18, 0.24, 1.7, -0.30, -0.4),
            (0.10, -0.42, 0.20, 1.2, 0.30, 2.0), (-0.20, -0.30, 0.16, 0.9, -0.40, 3.6), (0.30, 0.40, 0.15, 0.85, 0.45, 1.2)]
    def crystals(bm):
        for (x, y, r, h, tilt, az) in spec:
            prof = [(r, 0.0), (r, h * 0.72), (r * 0.55, h * 0.9), (0.0, h)]
            fs = lathe(bm, prof, 6, None, rnd.uniform(0, 1))
            vs = list({v for f in fs for v in f.verts})
            M = Matrix.Translation((x, y, -0.1)) @ Matrix.Rotation(az, 4, 'Z') @ Matrix.Rotation(tilt, 4, 'X')
            bmesh.ops.transform(bm, matrix=M, verts=vs)
    def icec(f): f[gl] = 1; return 0
    part(bm, crystals, icec, bevel=0.03, angle=40)
    part(bm, lambda bm: blob(bm, 1.0, (0, 0, 0), (0.75, 0.7, 0.25), 3, 0.03, seed), lambda f: 1 if f.normal.z > 0.2 else 2)
    ob = bm_to_obj(bm, 'IceShardCluster', C)
    for v in ob.data.vertices:
        if v.co.z < 0: v.co.z = 0.0
    return with_gradient(ob)

# ------------------------------------------------------------------ decor (walk through)
def build_ribcage(seed=521):
    """the ribs of a great beast arching out of the snow along a half-buried spine"""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['bone', 'bone_dark', 'snow', 'snow_shade']
    def ribs(bm):
        spine = [Vector((0, -1.25 + 2.5 * t, 0.10 + 0.12 * math.sin(t * 3.1))) for t in [i / 8 for i in range(9)]]
        tube(bm, spine, 0.10, 0.06, 8)
        for k in range(6):
            t = 0.12 + k * 0.15; y = -1.25 + 2.5 * t
            size = 1.0 - 0.35 * abs(t - 0.45) * 2
            for s in (-1, 1):
                pts = []
                for i in range(8):
                    u = i / 7
                    pts.append(Vector((s * (0.08 + 0.75 * size * math.sin(u * 1.75)), y + 0.12 * u, 0.12 + 1.35 * size * math.sin(u * 2.1) * (1 - 0.25 * u))))
                tube(bm, pts, 0.065, 0.03, 6)
    part(bm, ribs, lambda f: 0 if lit(f.normal) > -0.05 else 1)
    part(bm, lambda bm: (snow_base(bm, 0.9, 1.6, seed), blob(bm, 0.5, (0.55, 0.9, 0), (1, 1, 0.35), 2, 0.02, seed + 1)), lambda f: 2 if f.normal.z > 0.2 else 3)
    return bm_to_obj(bm, 'RibCage', C)

def build_beastskull(seed=531):
    """a big beast skull lying in the snow, looking forward (-Y): long cranium with a heavy brow, big dark eye
    sockets, a snout with a nose hole, two thick tusks curling out and up"""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['bone', 'bone_dark', 'black', 'snow', 'snow_shade']
    bonec = lambda f: 0 if lit(f.normal) > -0.15 else 1
    def head(bm):
        blob(bm, 0.40, (0, 0.12, 0.30), (0.95, 1.25, 0.78), 3, 0.012, seed, floor=False)          # cranium
        blob(bm, 0.22, (0, -0.24, 0.47), (1.55, 0.62, 0.48), 2, 0.008, seed + 1, floor=False)      # brow ridge
        blob(bm, 0.25, (0, -0.52, 0.24), (0.78, 1.25, 0.62), 2, 0.008, seed + 2, floor=False)      # snout
    part(bm, head, bonec)
    def holes(bm):
        for s in (-1, 1): blob(bm, 0.12, (s * 0.21, -0.36, 0.35), (1.0, 0.75, 1.0), 2, 0.0, 0, floor=False)   # eye sockets
        blob(bm, 0.07, (0, -0.80, 0.30), (1.3, 0.6, 0.8), 2, 0.0, 0, floor=False)                              # nose hole
    part(bm, holes, lambda f: 2)
    def tusks(bm):
        for s in (-1, 1):
            pts = []
            for i in range(9):
                t = i / 8
                pts.append(Vector((s * (0.17 + 0.42 * t), -0.62 - 0.70 * t + 0.25 * t * t, 0.14 - 0.10 * math.sin(math.pi * t) + 0.55 * t * t)))
            tube(bm, pts, 0.085, 0.02, 8)
    part(bm, tusks, bonec)
    part(bm, lambda bm: snow_base(bm, 0.75, 0.85, seed, h=0.16), lambda f: 3 if f.normal.z > 0.2 else 4)
    return bm_to_obj(bm, 'BeastSkull', C)

def build_bonepile(seed=541):
    """a few scattered bones half in the snow"""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['bone', 'bone_dark', 'snow', 'snow_shade']
    def bones(bm):
        for (ax, ay, bx, by, z) in [(-0.45, -0.1, 0.35, 0.15, 0.06), (-0.1, -0.4, 0.15, 0.42, 0.10), (0.2, -0.25, 0.55, -0.05, 0.05), (-0.5, 0.25, -0.15, 0.45, 0.05)]:
            bone(bm, (ax, ay, z), (bx, by, z + rnd.uniform(-0.02, 0.04)), rnd.uniform(0.035, 0.05))
    part(bm, bones, lambda f: 0 if lit(f.normal) > -0.05 else 1)
    part(bm, lambda bm: snow_base(bm, 0.6, 0.55, seed, h=0.08), lambda f: 2 if f.normal.z > 0.2 else 3)
    return bm_to_obj(bm, 'BonePile', C)

def build_icehole(seed=551):
    """a hole in the ice with dark water, a ring of broken ice plates and snow around it (flat, walk over)"""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['ice_dark', 'ice_light', 'ice', 'snow', 'snow_shade']
    part(bm, lambda bm: lathe(bm, [(0.42, 0.012), (0.0, 0.012)], 12), lambda f: 0)                      # water
    def plates(bm):
        n = 9
        for i in range(n):
            a = 2 * math.pi * i / n + rnd.uniform(-0.1, 0.1)
            r = rnd.uniform(0.55, 0.65); w = rnd.uniform(0.26, 0.34)
            c = Vector((math.cos(a) * r, math.sin(a) * r, 0.03))
            vs = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(c) @ Matrix.Rotation(a + rnd.uniform(-0.3, 0.3), 4, 'Z') @ Matrix.Rotation(rnd.uniform(-0.12, 0.12), 4, 'Y') @ Matrix.Diagonal((0.24, w, 0.06, 1)))['verts']
    part(bm, plates, lambda f: 1 if f.normal.z > 0.5 else 2, bevel=0.015)
    def snowring(bm):
        for i in range(7):
            a = 2 * math.pi * i / 7 + 0.3
            blob(bm, 0.22, (math.cos(a) * 0.9, math.sin(a) * 0.9, 0), (1.3, 1.0, 0.35), 2, 0.02, seed + i)
    part(bm, snowring, lambda f: 3 if f.normal.z > 0.2 else 4)
    return bm_to_obj(bm, 'IceHole', C)
