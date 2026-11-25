import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from props3 import blob
from common import *

GC = ['snow', 'glacier', 'ice_light', 'glacier_deep', 'ice', 'ice_deep']
def glacier_col(f):
    """glacier faces like the reference: white slabs and pale-blue grooves alternating across the face (by which way each
    vertical facet turns), white flat tops, deeper blue in the crevices and under overhangs"""
    n = f.normal
    if n.z > 0.65: return 0                      # flat tops: snow
    if n.z < -0.4: return 4                      # undersides
    turn = n.x * 0.85 - n.y * 0.35               # facets turned to the right/front read white, to the left blue
    if turn > 0.35: return 0
    if turn > 0.0: return 1
    if turn > -0.45: return 3
    return 4

def column(bm, cx, cy, half_w, depth, h, rnd, lean=0.0):
    """an irregular vertical ice prism (6-8 sided), slanted broken top"""
    n = rnd.choice([6, 7, 8])
    pts = []
    for i in range(n):
        a = 2 * math.pi * i / n + rnd.uniform(-0.25, 0.25)
        pts.append((cx + math.cos(a) * half_w * rnd.uniform(0.85, 1.1), cy + math.sin(a) * depth * rnd.uniform(0.85, 1.1)))
    tilt_x, tilt_y = rnd.uniform(-0.18, 0.18), rnd.uniform(-0.25, 0.05)
    bot = [bm.verts.new((px, py, 0.0)) for px, py in pts]
    top = [bm.verts.new((px + lean * h * 0.0, py - lean * h, h + (px - cx) * tilt_x + (py - cy) * tilt_y)) for px, py in pts]
    bm.faces.new(list(reversed(bot))); bm.faces.new(top)
    for i in range(n): bm.faces.new((bot[i], bot[(i + 1) % n], top[(i + 1) % n], top[i]))
    # a chipped chunk on some tops: a smaller prism sitting a bit lower
    if rnd.random() < 0.4:
        k = 5; r = half_w * 0.6; zc = h + rnd.uniform(-0.6, -0.1)
        p2 = [(cx + math.cos(2 * math.pi * i / k) * r + rnd.uniform(-0.1, 0.1), cy - depth * 0.35 + math.sin(2 * math.pi * i / k) * r * 0.8) for i in range(k)]
        b2 = [bm.verts.new((px, py, zc - 0.9)) for px, py in p2]; t2 = [bm.verts.new((px, py, zc + rnd.uniform(0.2, 0.6))) for px, py in p2]
        bm.faces.new(list(reversed(b2))); bm.faces.new(t2)
        for i in range(k): bm.faces.new((b2[i], b2[(i + 1) % k], t2[(i + 1) % k], t2[i]))

def build_glacier(seed, name, L=7.4, D=2.5, Hmin=6.0, Hmax=8.2):
    """a giant glacier face: vertical ice columns side by side, broken ice at its foot (front = -Y)"""
    rnd = random.Random(seed)
    bm = bmesh.new()
    def cols(bm):
        # solid core: nothing can be seen through the wall
        vs = box(bm, (L, D * 1.4, Hmin * 0.92), (0, D * 0.35, Hmin * 0.46)); jitter(vs, 0.1, seed)
        x = -L / 2 - 0.3
        while x < L / 2 + 0.3:
            w = rnd.uniform(0.9, 1.8)
            h = rnd.uniform(Hmin, Hmax)
            column(bm, x + w / 2, rnd.uniform(-0.35, 0.1), w * 0.7, D * 0.75, h, rnd)
            # a narrower column in front of some, for vertical grooves and depth
            if rnd.random() < 0.45: column(bm, x + w * rnd.uniform(0.3, 0.7), -D * 0.55, w * 0.32, D * 0.35, h * rnd.uniform(0.55, 0.9), rnd)
            x += w * rnd.uniform(0.70, 0.85)
    part(bm, cols, glacier_col)
    def foot(bm):   # fallen ice blocks along the front
        for k in range(6):
            x = rnd.uniform(-L / 2 + 0.4, L / 2 - 0.4); s = rnd.uniform(0.35, 0.8)
            vs = box(bm, (s * 1.3, s, s * 0.9), (x, -D - s * 0.2, s * 0.35), Matrix.Rotation(rnd.uniform(0, 3), 4, 'Z') @ Matrix.Rotation(rnd.uniform(-0.4, 0.4), 4, 'X'))
            jitter(vs, s * 0.15, rnd.randint(0, 999))
            for v in vs:
                if v.co.z < 0: v.co.z = 0
    part(bm, foot, glacier_col)
    return bm_to_obj(bm, name, GC)

def build_glacierblock(seed=301):
    """one map tile (1.5 m) of glacier: a cluster of vertical ice columns"""
    rnd = random.Random(seed)
    bm = bmesh.new()
    def cols(bm):
        for (x, y, w, h) in [(-0.38, -0.30, 0.42, 2.3), (0.32, -0.36, 0.45, 2.6), (-0.30, 0.34, 0.45, 2.5), (0.36, 0.30, 0.40, 2.2), (0.0, 0.0, 0.40, 2.7)]:
            column(bm, x, y, w, w * 1.05, h * rnd.uniform(0.95, 1.05), rnd)
    part(bm, cols, glacier_col)
    return bm_to_obj(bm, 'GlacierBlock', GC)

def build_glacierblock_v(kind, seed, name):
    """one map tile (1.5 m) of glacier wall in a chunky, soft style: rounded ice blocks (a smooth deep-to-light blue
    ramp up their sides, the same whichever way the builder turns them) under a thick rounded snow slab with a few
    drips over the edge. Four kinds: 'cols' = split block, 'block' = one block, 'stack' = two stacked, 'step' = block
    with icicles."""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['ice_grad', 'snow', 'snow_shade', 'ice_light', 'snow']
    kindl = bm.faces.layers.int.new('kind')       # 0 ice (ramp), 1 snow, 2 snow underside, 3 icicle, 4 crack
    tops = []   # (cx, cy, sx, sy, top z) of every block, for the snow
    seen = bm.verts.layers.int.new('seen')      # 1 = existed before the current step (v.tag is used by bmesh ops)
    def mark():
        for v in bm.verts: v[seen] = 1
    def fresh(): return [v for v in bm.verts if v[seen] == 0]
    def cracks(sx, sy, h, radius):
        """thin white zigzag cracks (with a branch now and then) lying just off the flat part of the side faces"""
        faces = []
        for side in range(4):
            if rnd.random() > 0.65: continue
            if side in (0, 2): half_u, half_d, sgn = sx / 2 - radius - 0.05, sy / 2, (-1 if side == 0 else 1)
            else:              half_u, half_d, sgn = sy / 2 - radius - 0.05, sx / 2, (-1 if side == 3 else 1)
            def P(u, z, d=0.006):
                return Vector((u, sgn * (half_d + d), z)) if side in (0, 2) else Vector((sgn * (half_d + d), u, z))
            outward = P(0, 0, 1.0) - P(0, 0, 0.0)
            def ribbon(u, z, ang, n, w):
                pts = [(u, z)]
                for _ in range(n):
                    ang += rnd.uniform(-0.7, 0.7)
                    L = rnd.uniform(0.12, 0.26)
                    u = max(-half_u, min(half_u, u + math.sin(ang) * L)); z = max(radius + 0.1, min(h - 0.15, z - math.cos(ang) * L))
                    pts.append((u, z))
                for i in range(len(pts) - 1):
                    (u0, z0_), (u1, z1) = pts[i], pts[i + 1]
                    du, dz = u1 - u0, z1 - z0_; ln = math.hypot(du, dz) or 1.0
                    nu, nz = -dz / ln, du / ln
                    w0 = w * (1 - i / len(pts)); w1 = w * (1 - (i + 1) / len(pts)) + 0.004
                    q = [P(u0 + nu * w0, z0_ + nz * w0), P(u1 + nu * w1, z1 + nz * w1), P(u1 - nu * w1, z1 - nz * w1), P(u0 - nu * w0, z0_ - nz * w0)]
                    f = bm.faces.new([bm.verts.new(c) for c in q]); f.normal_update()
                    if f.normal.dot(outward) < 0: f.normal_flip()
                    faces.append(f)
                return pts
            # start near the top or a side edge and run down into the face
            u = rnd.uniform(-half_u * 0.7, half_u * 0.7); z = rnd.uniform(h * 0.55, h - 0.2)
            pts = ribbon(u, z, rnd.uniform(-0.6, 0.6), rnd.randint(4, 6), 0.028)
            if len(pts) > 3 and rnd.random() < 0.7:
                bu, bz = pts[rnd.randint(1, 2)]
                ribbon(bu, bz, rnd.choice([-1, 1]) * rnd.uniform(0.8, 1.3), rnd.randint(2, 3), 0.018)
        return faces
    def block(sx, sy, h, x, y, z0=0.0, yaw=0.0, taper=0.94, radius=0.14):
        mark()
        vs = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((0, 0, h / 2)) @ Matrix.Diagonal((sx, sy, h, 1)))['verts']
        edges = list({e for v in vs for e in v.link_edges})
        bmesh.ops.bevel(bm, geom=edges, offset=radius, offset_type='OFFSET', segments=3, profile=0.5, affect='EDGES', clamp_overlap=True)
        crack_faces = cracks(sx, sy, h, radius)                        # drawn in the block's own space, moved with it
        new = fresh()
        R = Matrix.Rotation(yaw, 4, 'Z')
        for v in new:
            k = 1.0 - (1.0 - taper) * (v.co.z / h)                       # narrower towards the top
            p = R @ Vector((v.co.x * k, v.co.y * k, 0))
            v.co = Vector((x + p.x, y + p.y, z0 + v.co.z))
        for f in {f for v in new for f in v.link_faces}: f[kindl] = 0
        for f in crack_faces: f[kindl] = 4
        tops.append((x, y, sx * taper, sy * taper, z0 + h, yaw))
        return z0 + h
    def snow(x, y, sx, sy, top, yaw, drips):
        mark()
        th = 0.30
        vs = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((0, 0, th / 2)) @ Matrix.Diagonal((sx + 0.08, sy + 0.08, th, 1)))['verts']
        edges = list({e for v in vs for e in v.link_edges})
        bmesh.ops.bevel(bm, geom=edges, offset=0.13, offset_type='OFFSET', segments=3, profile=0.5, affect='EDGES', clamp_overlap=True)
        R = Matrix.Rotation(yaw, 4, 'Z')
        z0 = top - 0.12
        def place(v):
            p = R @ Vector((v.co.x, v.co.y, 0)); v.co = Vector((x + p.x, y + p.y, z0 + v.co.z))
        new = fresh()
        for v in new: place(v)
        bm.normal_update()
        for f in {f for v in new for f in v.link_faces}: f[kindl] = 1 if f.normal.z > -0.35 else 2
        # drips: soft snow lobes hanging over the edge (create_icosphere invalidates every python vert reference,
        # so each piece is finished right after its op)
        for _ in range(drips):
            side = rnd.randrange(4); t = rnd.uniform(-0.3, 0.3)
            hx, hy = (sx + 0.08) / 2, (sy + 0.08) / 2
            px, py = [(t * sx, -hy), (hx, t * sy), (t * sx, hy), (-hx, t * sy)][side]
            along = rnd.uniform(0.20, 0.28)               # a flat lobe lying along the edge, hugging the slab
            ax, ay = (along, 0.09) if side in (0, 2) else (0.09, along)
            mark()
            bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((px * 0.985, py * 0.985, 0.04)) @ Matrix.Diagonal((ax, ay, 0.17, 1)))
            d = fresh()
            for v in d: place(v)
            bm.normal_update()
            for f in {f for v in d for f in v.link_faces}: f[kindl] = 1 if f.normal.z > -0.35 else 2
    def icicles(x, y, sx, sy, top, yaw, n):
        R = Matrix.Rotation(yaw, 4, 'Z')
        for _ in range(n):
            side = rnd.randrange(4); t = rnd.uniform(-0.35, 0.35)
            hx, hy = sx / 2 + 0.02, sy / 2 + 0.02
            px, py = [(t * sx, -hy), (hx, t * sy), (t * sx, hy), (-hx, t * sy)][side]
            L = rnd.uniform(0.25, 0.45)
            p = R @ Vector((px, py, 0))
            mark()
            bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=6, radius1=0.0, radius2=rnd.uniform(0.06, 0.09), depth=L,
                                  matrix=Matrix.Translation((x + p.x, y + p.y, top - 0.12 - L / 2 + 0.03)))
            for f in {f for v in fresh() for f in v.link_faces}: f[kindl] = 3
    if kind == 'cols':        # a block split by a crack: two halves of slightly different height
        block(0.76, 1.42, 2.10, -0.35, 0.0, radius=0.12)
        block(0.66, 1.38, 1.85, 0.38, 0.02, radius=0.12)
    elif kind == 'block':     # one big block
        block(1.44, 1.40, 2.05, 0, 0)
    elif kind == 'stack':     # a wide low block with a smaller one sitting on it
        z = block(1.46, 1.42, 1.45, 0, 0)
        block(0.96, 0.92, 0.72, -0.08, 0.06, z0=z + 0.12, yaw=rnd.uniform(0.15, 0.3), taper=0.9, radius=0.12)
    elif kind == 'chunk':     # a small block ('x' obstacle)
        block(1.02, 0.94, 0.95, 0, 0, radius=0.13)
    elif kind == 'chunk2':    # two small chunks leaning together
        block(0.80, 0.74, 0.92, -0.17, -0.08, radius=0.12)
        block(0.58, 0.52, 0.55, 0.32, 0.26, yaw=0.5, radius=0.11)
    else:                     # 'step': one block, icicles under the snow
        block(1.44, 1.40, 2.10, 0, 0)
    for i, (x, y, sx, sy, top, yaw) in enumerate(tops):
        snow(x, y, sx, sy, top, yaw, rnd.randint(2, 3) if i == len(tops) - 1 else 2)
    if kind == 'step':
        x, y, sx, sy, top, yaw = tops[0]; icicles(x, y, sx, sy, top, yaw, 5)
    bm.normal_update()
    ob = bm_to_obj(bm, name, C)                     # face colours below are set from the 'kind' layer
    me = ob.data
    kinds = me.attributes['kind'].data; uv = me.uv_layers['UVMap'].data
    ice_top = max((me.vertices[vi].co.z for p in me.polygons if kinds[p.index].value == 0 for vi in p.vertices), default=1.0)
    for p in me.polygons:
        k = kinds[p.index].value
        for li in p.loop_indices:
            if k == 0: uv[li].uv = grad_uv('ice_grad', me.vertices[me.loops[li].vertex_index].co.z / ice_top)
            else: uv[li].uv = cell_uv(C[k])
    me.attributes.remove(me.attributes['kind'])
    if 'seen' in me.attributes: me.attributes.remove(me.attributes['seen'])
    return ob

def build_spire(seed=311):
    """a serac: a tall leaning ice spire with a smaller one at its side"""
    rnd = random.Random(seed)
    bm = bmesh.new()
    def mk(bm):
        for (x, y, r, h, tilt) in [(0, 0, 0.55, 3.6, 0.10), (0.55, 0.25, 0.32, 2.1, 0.22), (-0.45, 0.2, 0.28, 1.5, -0.25)]:
            rot = Matrix.Rotation(tilt, 4, 'X') @ Matrix.Rotation(rnd.uniform(0, 1), 4, 'Z')
            lathe(bm, [(r, 0.0), (r * 0.92, h * 0.45), (r * 0.62, h * 0.78), (0.0, h)], 6, None, rnd.uniform(0, 1), Matrix.Translation((x, y, 0)) @ rot)
    part(bm, mk, glacier_col)
    part(bm, lambda bm: blob(bm, 0.9, (0.05, 0.1, 0), (1, 0.85, 0.3), 3, 0.03, seed), lambda f: 0)
    return bm_to_obj(bm, 'IceSpire', GC)

def build_ridge(seed=321):
    """pressure ridge: tilted ice plates pushed up in a row (1.5 m long, ~1 m high)"""
    rnd = random.Random(seed)
    bm = bmesh.new()
    def mk(bm):
        x = -0.75
        while x < 0.75:
            w = rnd.uniform(0.35, 0.55); h = rnd.uniform(0.85, 1.25)
            rot = Matrix.Rotation(rnd.uniform(-0.2, 0.2), 4, 'Z') @ Matrix.Rotation(rnd.choice([-1, 1]) * rnd.uniform(0.15, 0.45), 4, 'X')
            vs = box(bm, (w, 0.24, h), (x + w / 2, rnd.uniform(-0.12, 0.12), h * 0.4), rot)
            jitter(vs, 0.04, rnd.randint(0, 999))
            for v in vs:
                if v.co.z < 0: v.co.z = 0
            x += w * 0.62
    part(bm, mk, glacier_col)
    part(bm, lambda bm: blob(bm, 1.0, (0, 0, 0), (0.9, 0.35, 0.22), 3, 0.02, seed), lambda f: 0)
    return bm_to_obj(bm, 'PressureRidge', GC)

def build_boulder(seed=331):
    """a worn ice boulder, faceted like the glacier; snow lies on its upward faces"""
    rnd = random.Random(seed)
    bm = bmesh.new()
    def mk(bm):
        vs = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.75, matrix=Matrix.Translation((0, 0, 0.5)) @ Matrix.Diagonal((1.15, 0.95, 0.75, 1)))['verts']
        off = Vector((rnd.random() * 9, rnd.random() * 9, rnd.random() * 9))
        for v in vs: v.co += (v.co - Vector((0, 0, 0.5))).normalized() * noise.noise(v.co * 1.4 + off) * 0.18
        for v in vs:
            if v.co.z < 0: v.co.z = 0
    part(bm, mk, lambda f: 0 if f.normal.z > 0.62 else glacier_col(f), bevel=0.02, angle=28)
    return bm_to_obj(bm, 'IceBoulder', GC)

def build_bones(seed=341):
    """mammoth remains in the snow: two great curved tusks, the top of a skull, a few ribs"""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['bone', 'bone_dark', 'snow']
    def tube(bm, pts, r0, r1, segs=8):
        rings = []
        for i, p in enumerate(pts):
            t = i / (len(pts) - 1); r = r0 + (r1 - r0) * t
            d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
            a = d.orthogonal().normalized(); b = d.cross(a).normalized()
            rings.append([bm.verts.new(p + (a * math.cos(2 * math.pi * j / segs) + b * math.sin(2 * math.pi * j / segs)) * max(r, 0.004)) for j in range(segs)])
        for i in range(len(rings) - 1):
            for j in range(segs): bm.faces.new((rings[i][j], rings[i][(j + 1) % segs], rings[i + 1][(j + 1) % segs], rings[i + 1][j]))
        bm.faces.new(list(reversed(rings[0])))
    def tusks(bm):
        for s in (-1, 1):
            pts = [Vector((s * 0.25 + s * 0.55 * math.sin(t * 1.4) * t, -0.2 - 1.1 * t, 0.05 + 1.1 * math.sin(t * 2.4) * (1 - 0.35 * t))) for t in [i / 9 for i in range(10)]]
            tube(bm, pts, 0.11, 0.025)
    part(bm, tusks, lambda f: 0 if lit(f.normal) > 0.0 else 1)
    def skull(bm):
        vs = blob(bm, 0.45, (0, 0.25, 0.0), (1.0, 1.15, 0.85), 3, 0.03, seed)
    part(bm, skull, lambda f: 0)
    def ribs(bm):
        for k in range(4):
            y = 0.85 + k * 0.32
            for s in (-1, 1):
                pts = [Vector((s * (0.12 + 0.55 * math.sin(t * 1.5)), y, 0.75 * math.sin(t * 2.6) * (1 - 0.15 * k))) for t in [i / 7 for i in range(8)]]
                tube(bm, pts, 0.05, 0.03, 6)
    part(bm, ribs, lambda f: 0 if lit(f.normal) > 0.0 else 1)
    part(bm, lambda bm: (blob(bm, 1.0, (0, 0.6, 0), (0.85, 1.4, 0.22), 3, 0.03, seed + 2), blob(bm, 0.5, (0.3, -0.4, 0), (1, 1, 0.3), 3, 0.02, seed + 3)), lambda f: 2)
    return bm_to_obj(bm, 'MammothBones', C)

if __name__ == '__main__':
    import common; common.GROUND_RGBA = (0.42, 0.55, 0.72, 1); common.SUN_ENERGY = 2.0
    jobs = [(lambda: build_glacier(401, 'GlacierWallA'), 'GlacierWallA'), (lambda: build_glacier(402, 'GlacierWallB'), 'GlacierWallB'), (lambda: build_glacier(403, 'GlacierWallC'), 'GlacierWallC'),
            (build_glacierblock, 'GlacierBlock'), (build_spire, 'IceSpire'), (build_ridge, 'PressureRidge'), (build_boulder, 'IceBoulder'), (build_bones, 'MammothBones')]
    only = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    for fn, name in jobs:
        if only and name not in only: continue
        reset(); ob = fn(); tris = finish(ob, name); export_fbx(ob, name)
        render_preview([ob], f'{PREVIEW}/{name}.png', res=320); d = ob.dimensions
        print('BUILT', name, tris, round(d.x, 2), round(d.y, 2), round(d.z, 2))
