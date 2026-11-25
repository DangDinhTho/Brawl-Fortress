import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from props3 import blob, snowc
from common import *

def icec_shift(shift):
    def f(face):
        l = lit(face.normal) + shift
        return 0 if l > 0.5 else (1 if l > 0.05 else (2 if face.normal.z > -0.5 else 3))
    return f

def build_icecliff(seed, name):
    """border wall: big stacked ice chunks, ~7.4 long x 6 high x 5.3 deep (the size of the old cliffs)"""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['ice_light', 'ice', 'ice_deep', 'ice_dark', 'snow', 'snow_shade']
    L, D = 7.4, 5.0
    rows = [(0.0, 2.6, 3), (2.4, 2.1, 3), (4.3, 1.5, 2)]
    for r, (z0, h, n) in enumerate(rows):
        w = L / n
        for k in range(n):
            x = -L / 2 + w * (k + 0.5) + (rnd.uniform(-0.25, 0.25) if r else 0) + (w * 0.25 if r == 1 else 0)
            def mk(bm, x=x, z0=z0, h=h, w=w, r=r):
                d = D - r * 0.7 + rnd.uniform(-0.2, 0.2)
                vs = box(bm, (w * rnd.uniform(0.92, 1.02), d, h * rnd.uniform(0.95, 1.1)), (x, rnd.uniform(-0.25, 0.25), z0 + h / 2), Matrix.Rotation(rnd.uniform(-0.07, 0.07), 4, 'Z'))
                jitter(vs, 0.18, rnd.randint(0, 9999))
            part(bm, mk, icec_shift(rnd.choice([0.0, -0.15, 0.12, -0.05])), bevel=0.09)
    # snow on top
    def snow(bm):
        for k in range(3):
            vs = bmesh.ops.create_icosphere(bm, subdivisions=3, radius=1.0, matrix=Matrix.Translation((-2.4 + k * 2.4 + rnd.uniform(-0.3, 0.3), rnd.uniform(-0.3, 0.3), 5.75)) @ Matrix.Diagonal((1.75, 1.9, 0.45, 1)))['verts']
            jitter(vs, 0.05, rnd.randint(0, 999))
    part(bm, snow, lambda f: 4)
    # a few icicles on the front (-Y) and back
    def icicles(bm):
        for side in (-1, 1):
            for k in range(5):
                x = rnd.uniform(-3.2, 3.2); Lc = rnd.uniform(0.35, 0.8)
                add_cone(bm, 5, 0.09, 0.0, Lc, Matrix.Translation((x, side * (D / 2 - 0.45), 4.25 - Lc / 2)) @ Matrix.Rotation(math.pi, 4, 'X'))
    part(bm, icicles, lambda f: 0 if lit(f.normal) > 0.1 else 1)
    return bm_to_obj(bm, name, C)

def build_frostgrass(seed=71):
    """a clump of frozen grass blades (ice blue to white tips) for hiding in"""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['ice_light', 'ice', 'snow', 'snow_shade', 'ice_deep']
    for k in range(16):
        a = rnd.uniform(0, 2 * math.pi); r0 = rnd.uniform(0.0, 0.32)
        x, y = math.cos(a) * r0, math.sin(a) * r0
        h = rnd.uniform(0.85, 1.35); lean = rnd.uniform(0.12, 0.38)
        def blade(bm, x=x, y=y, h=h, a=a, lean=lean):
            # a 3-sided blade bending outwards in 3 steps
            rings = []
            for i, t in enumerate((0.0, 0.4, 0.75, 1.0)):
                wv = 0.09 * (1 - t) + 0.005
                cx = x + math.cos(a) * lean * t * t; cy = y + math.sin(a) * lean * t * t
                ring = [bm.verts.new((cx + math.cos(a + j * 2.094) * wv, cy + math.sin(a + j * 2.094) * wv * 0.55, h * t)) for j in range(3)]
                rings.append(ring)
            for i in range(len(rings) - 1):
                for j in range(3):
                    bm.faces.new((rings[i][j], rings[i][(j + 1) % 3], rings[i + 1][(j + 1) % 3], rings[i + 1][j]))
            bm.faces.new(rings[-1])
        part(bm, blade, lambda f: 2 if f.calc_center_median().z > 0.75 * h else (0 if lit(f.normal) > 0.15 else 1))
    part(bm, lambda bm: blob(bm, 0.45, (0, 0, 0), (1, 1, 0.35), 3, 0.02, seed), lambda f: 2)
    return bm_to_obj(bm, 'FrostGrass', C)

def build_iceslab(seed=81):
    """a flat cracked ice plate lying on the ground, slightly raised"""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['ice_light', 'ice', 'ice_deep', 'snow']
    for k in range(3):     # three plates split by cracks
        def mk(bm, k=k):
            n = 7; pts = []
            cx, cy = [(-0.35, 0.1), (0.4, 0.25), (0.05, -0.45)][k]
            for i in range(n):
                a = 2 * math.pi * i / n + rnd.uniform(-0.2, 0.2); r = rnd.uniform(0.38, 0.55)
                pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
            bot = [bm.verts.new((px, py, 0.0)) for px, py in pts]
            top = [bm.verts.new((px * 0.96 + cx * 0.04, py * 0.96 + cy * 0.04, 0.06 + rnd.uniform(0, 0.02))) for px, py in pts]
            bm.faces.new(top)
            for i in range(n): bm.faces.new((bot[i], bot[(i + 1) % n], top[(i + 1) % n], top[i]))
        part(bm, mk, lambda f: (0 if k == 0 else 1) if f.normal.z > 0.5 else 2, bevel=0.015)
    return bm_to_obj(bm, 'IceSlab', C)

def build_snowdrift(seed=91):
    """a low, wind-blown snow drift (long and smooth)"""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['snow']
    def mk(bm):
        vs = bmesh.ops.create_icosphere(bm, subdivisions=3, radius=1.0, matrix=Matrix.Diagonal((1.6, 0.6, 0.32, 1)))['verts']
        for v in vs:
            v.co.x += 0.25 * v.co.z * (1 if v.co.y > 0 else 0)          # windward slope
            if v.co.z < 0: v.co.z = 0
        jitter(vs, 0.02, seed)
        vs2 = bmesh.ops.create_icosphere(bm, subdivisions=3, radius=1.0, matrix=Matrix.Translation((0.9, 0.35, 0)) @ Matrix.Diagonal((0.7, 0.4, 0.22, 1)))['verts']
        for v in vs2:
            if v.co.z < 0: v.co.z = 0
    part(bm, mk, lambda f: 0)
    return bm_to_obj(bm, 'SnowDrift', C)

def build_iceshards(seed=101):
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['ice_light', 'ice', 'ice_deep', 'ice_dark']
    for k in range(6):
        x, y = rnd.uniform(-0.4, 0.4), rnd.uniform(-0.4, 0.4); s = rnd.uniform(0.12, 0.25)
        def mk(bm, x=x, y=y, s=s):
            vs = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=s, matrix=Matrix.Translation((x, y, s * 0.4)) @ Matrix.Rotation(rnd.uniform(0, 3), 4, 'Z') @ Matrix.Diagonal((1, 0.8, 1.4, 1)))['verts']
            jitter(vs, s * 0.2, rnd.randint(0, 999))
            for v in vs:
                if v.co.z < 0: v.co.z = 0
        part(bm, mk, lambda f: 0 if lit(f.normal) > 0.4 else (1 if lit(f.normal) > -0.1 else 2))
    return bm_to_obj(bm, 'IceShards', C)

if __name__ == '__main__':
    import common
    common.FACETED = common.FACETED + ('IceShards', 'FrostGrass')
    jobs = [(lambda: build_icecliff(201, 'IceCliffA'), 'IceCliffA'), (lambda: build_icecliff(202, 'IceCliffB'), 'IceCliffB'), (lambda: build_icecliff(203, 'IceCliffC'), 'IceCliffC'),
            (build_frostgrass, 'FrostGrass'), (build_iceslab, 'IceSlab'), (build_snowdrift, 'SnowDrift'), (build_iceshards, 'IceShards')]
    for fn, name in jobs:
        reset(); ob = fn(); tris = finish(ob, name); export_fbx(ob, name)
        render_preview([ob], f'{PREVIEW}/{name}.png', res=300); d = ob.dimensions
        print('BUILT', name, tris, round(d.x, 2), round(d.y, 2), round(d.z, 2))
