import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *

def build_rock(seed=11):
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['stone_light', 'stone', 'stone_dark', 'snow', 'snow_shade']
    for (x, y, s, sx, sy, sz) in [(0, 0, 1.0, 1.0, 0.85, 0.85), (0.95, -0.35, 0.55, 1.0, 0.9, 0.8), (-0.75, 0.45, 0.45, 1.0, 1.0, 0.7)]:
        top = s * sz * 1.6
        def mk(bm, x=x, y=y, s=s, sx=sx, sy=sy, sz=sz):
            vs = bmesh.ops.create_icosphere(bm, subdivisions=3, radius=s, matrix=Matrix.Translation((x, y, s * sz * 0.55)) @ Matrix.Diagonal((sx, sy, sz, 1)))['verts']
            off = Vector((rnd.random() * 10, rnd.random() * 10, rnd.random() * 10))
            for v in vs:
                d = noise.noise((v.co - Vector((x, y, 0))) * 1.3 / s + off)
                v.co += (v.co - Vector((x, y, s * sz * 0.55))).normalized() * (d * 0.20 + 0.06 * noise.noise((v.co + off) * 4.0)) * s
                if v.co.z < 0: v.co.z = 0
        def col(f, top=top):
            n = f.normal; c = f.calc_center_median()
            if n.z > 0.55 and c.z > top * 0.30: return 3 if n.z > 0.7 else 4
            l = lit(n); return 0 if l > 0.45 else (1 if l > 0.0 else 2)
        part(bm, mk, col, bevel=0.035, angle=28)
    return bm_to_obj(bm, 'SnowRock', C)

def build_crate():
    bm = bmesh.new(); C = ['wood', 'wood_dark', 'wood_light', 'snow', 'snow_shade', 'ice_light', 'ice']
    S = 1.2; h = S / 2
    woodc = lambda f: 2 if lit(f.normal) > 0.5 else (0 if lit(f.normal) > -0.1 else 1)
    part(bm, lambda bm: box(bm, (S * 0.94, S * 0.94, S * 0.94), (0, 0, h)), woodc, bevel=0.02)                      # body
    # planks: three slats on each side, slightly proud of the body
    def planks(bm):
        for side in range(4):
            rot = Matrix.Rotation(math.pi / 2 * side, 4, 'Z')
            for k in range(3):
                z = 0.17 + k * 0.33
                box(bm, (S * 0.80, 0.04, 0.27), (rot @ Vector((0, -S * 0.48, z))).to_tuple(), rot)
    part(bm, planks, woodc, bevel=0.012)
    # frame beams on all edges, darker
    def frame(bm):
        b = 0.13
        for sx in (-1, 1):
            for sy in (-1, 1): box(bm, (b, b, S), (sx * (h - b / 2 + 0.01), sy * (h - b / 2 + 0.01), h))
        for z in (b / 2, S - b / 2):
            for sy in (-1, 1): box(bm, (S + 0.02, b, b), (0, sy * (h - b / 2 + 0.01), z))
            for sx in (-1, 1): box(bm, (b, S + 0.02, b), (sx * (h - b / 2 + 0.01), 0, z))
    part(bm, frame, lambda f: 1 if lit(f.normal) < 0.5 else 0, bevel=0.018)
    # thick snow cushion on top, drooping over the edges
    def snow(bm):
        vs = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((0.02, -0.03, S + 0.02)) @ Matrix.Diagonal((0.70, 0.70, 0.16, 1)))['verts']
        rnd = random.Random(5)
        for v in vs:
            if v.co.z < S + 0.02: v.co.z = S + 0.02 - rnd.choice([0.0, 0.03, 0.10, 0.16]) * (math.hypot(v.co.x, v.co.y) > 0.55)
            v.co.x = max(-0.66, min(0.66, v.co.x)); v.co.y = max(-0.66, min(0.66, v.co.y))
    part(bm, snow, lambda f: 3 if f.normal.z > 0.35 else 4)
    # icicles under the top beams, front and right side (the sides the camera sees)
    def icicles(bm):
        rnd = random.Random(9)
        for k in range(6):
            x = -0.45 + k * 0.18 + rnd.uniform(-0.03, 0.03); L = rnd.uniform(0.16, 0.34)
            add_cone(bm, 5, 0.045, 0.0, L, Matrix.Translation((x, -h - 0.02, S - 0.12 - L / 2)) @ Matrix.Rotation(math.pi, 4, 'X'))
        for k in range(4):
            y = -0.35 + k * 0.22; L = rnd.uniform(0.14, 0.28)
            add_cone(bm, 5, 0.04, 0.0, L, Matrix.Translation((h + 0.02, y, S - 0.12 - L / 2)) @ Matrix.Rotation(math.pi, 4, 'X'))
    part(bm, icicles, lambda f: 5 if lit(f.normal) > 0.1 else 6)
    return bm_to_obj(bm, 'SnowCrate', C)

def build_igloo(seed=4):
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['snow', 'snow_shade', 'snow_deep', 'ice_deep', 'ice_dark', 'black']
    Rd = 2.0
    # inner shell: the blue showing through the joints between blocks
    part(bm, lambda bm: lathe(bm, [(Rd - 0.12, 0.0)] + [((Rd - 0.12) * math.cos(a), (Rd - 0.12) * math.sin(a)) for a in [math.radians(d) for d in (15, 30, 45, 60, 75)]] + [(0.0, Rd - 0.12)], 20),
         lambda f: 3 if lit(f.normal) > 0.2 else 4)
    blockc = lambda f: 0 if lit(f.normal) > 0.45 else (1 if lit(f.normal) > 0.05 else 2)
    rings = [(0, 14), (16, 12), (32, 10), (48, 8), (64, 5)]
    def blocks(bm):
        for (el0, n) in rings:
            el1 = el0 + 16; elm = math.radians(el0 + 8)
            hgt = (math.radians(16) * Rd) - 0.05
            for i in range(n):
                az = 2 * math.pi * (i + (0.5 if (el0 // 16) % 2 else 0)) / n
                # skip the doorway (facing -Y) on the lowest two rings
                if el0 < 30 and abs(((az - (-math.pi / 2)) + math.pi) % (2 * math.pi) - math.pi) < 0.42: continue
                w = 2 * math.pi * Rd * math.cos(elm) / n - 0.06
                pos = Vector((math.cos(az) * math.cos(elm), math.sin(az) * math.cos(elm), math.sin(elm))) * Rd
                rot = Matrix.Rotation(az - math.pi / 2, 4, 'Z') @ Matrix.Rotation(elm, 4, 'X')
                box(bm, (w, 0.30, hgt), (pos.x, pos.y, pos.z), rot @ Matrix.Rotation(rnd.uniform(-0.03, 0.03), 4, 'Y'))
        # cap
        add_cone(bm, 8, 0.55, 0.30, 0.22, Matrix.Translation((0, 0, Rd - 0.03)))
    part(bm, blocks, blockc, bevel=0.035)
    # entrance tunnel: an arch of blocks sticking out at the front (-Y)
    def tunnel(bm):
        # a solid snow arch (half tube) from inside the dome to 1 m in front of it
        segs, ro, ri = 9, 1.02, 0.70
        y0, y1 = -Rd + 0.45, -Rd - 0.95
        def ring(y, r):
            return [bm.verts.new((math.cos(math.pi * i / segs) * r, y, math.sin(math.pi * i / segs) * r * 1.08)) for i in range(segs + 1)]
        oa, ob_, ia, ib = ring(y0, ro), ring(y1, ro), ring(y0, ri), ring(y1, ri)
        for i in range(segs):
            bm.faces.new((oa[i], oa[i + 1], ob_[i + 1], ob_[i]))      # outside
            bm.faces.new((ib[i], ib[i + 1], ia[i + 1], ia[i]))        # inside
            bm.faces.new((ob_[i], ob_[i + 1], ib[i + 1], ib[i]))     # front rim
            bm.faces.new((ia[i], ia[i + 1], oa[i + 1], oa[i]))        # back rim (hidden in the dome)
        bm.faces.new((oa[0], ob_[0], ib[0], ia[0])); bm.faces.new((ia[segs], ib[segs], ob_[segs], oa[segs]))   # feet
        bmesh.ops.recalc_face_normals(bm, faces=[f for f in bm.faces if any(v in oa or v in ob_ for v in f.verts)])
    part(bm, tunnel, lambda f: 2 if f.normal.y > 0.5 or f.normal.z < -0.3 else blockc(f), bevel=0.05, angle=30)
    # dark doorway
    part(bm, lambda bm: lathe(bm, [(0.62, 0.0)] + [(0.62 * math.cos(math.radians(d)), 0.72 * math.sin(math.radians(d))) for d in (30, 60)] + [(0.0, 0.72)], 10,
                              None, 0, Matrix.Translation((0, -Rd - 0.25, 0.01)) @ Matrix.Rotation(math.pi / 2, 4, 'X') @ Matrix.Diagonal((1.12, 1.15, 0.05, 1))),
         lambda f: 5)
    return bm_to_obj(bm, 'Igloo', C)

def build_bush(seed=21):
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['pine', 'pine_dark', 'pine_light', 'snow', 'snow_shade']
    H = 1.9
    blobs = [(0, 0, 1.05, 0.92), (0.62, 0.22, 0.75, 0.68), (-0.62, 0.18, 0.72, 0.68), (0.30, -0.55, 0.62, 0.60), (-0.32, -0.50, 0.60, 0.58), (0.05, 0.55, 0.80, 0.66), (0.0, 0.05, 1.50, 0.60), (0.45, -0.05, 1.30, 0.50), (-0.42, 0.0, 1.28, 0.50)]
    for (x, y, z, r) in blobs:
        def mk(bm, x=x, y=y, z=z, r=r):
            vs = bmesh.ops.create_icosphere(bm, subdivisions=3, radius=r, matrix=Matrix.Translation((x, y, z)) @ Matrix.Diagonal((1, 1, 0.9, 1)))['verts']
            jitter(vs, r * 0.07, rnd.randint(0, 999))
            for v in vs:
                if v.co.z < 0: v.co.z = 0
        part(bm, mk, lambda f: 0)
        if z + r * 0.6 > 1.1:      # clumps reaching high enough wear a snow cap
            def cap(bm, x=x, y=y, z=z, r=r):
                vs = bmesh.ops.create_icosphere(bm, subdivisions=3, radius=1.0, matrix=Matrix.Translation((x, y, z + r * 0.52)) @ Matrix.Diagonal((r * 0.86, r * 0.86, r * 0.42, 1)))['verts']
                jitter(vs, r * 0.03, rnd.randint(0, 999))
            part(bm, cap, lambda f: 3)
    return bm_to_obj(bm, 'FrostBush', C)

def build_wall(seed=31):
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['ice_light', 'ice', 'ice_deep', 'ice_dark', 'snow', 'snow_shade']
    icec = lambda f: 0 if lit(f.normal) > 0.5 else (1 if lit(f.normal) > 0.05 else (2 if f.normal.z > -0.5 else 3))
    def slabs(bm):
        z = 0.0
        for k, (hh, w) in enumerate([(0.55, 2.0), (0.45, 1.85), (0.38, 1.7)]):
            n = 2 if k != 1 else 3
            for j in range(n):
                seg = w / n
                x = -w / 2 + seg * (j + 0.5) + rnd.uniform(-0.03, 0.03)
                vs = box(bm, (seg - 0.05, 0.95 - k * 0.06, hh - 0.04), (x, rnd.uniform(-0.03, 0.03), z + hh / 2))
                jitter(vs, 0.035, rnd.randint(0, 999))
            z += hh
    part(bm, slabs, icec, bevel=0.04)
    def snow(bm):
        vs = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Translation((0, 0, 1.38)) @ Matrix.Diagonal((0.88, 0.44, 0.14, 1)))['verts']
        for v in vs:
            if v.co.z < 1.38: v.co.z = 1.38 - (0.12 if rnd.random() < 0.25 and abs(v.co.y) > 0.3 else 0.0)
    part(bm, snow, lambda f: 4 if f.normal.z > 0.35 else 5)
    return bm_to_obj(bm, 'IceWall', C)

if __name__ == '__main__':
    res = {}
    for fn, name in ((build_rock, 'SnowRock'), (build_crate, 'SnowCrate'), (build_igloo, 'Igloo'), (build_bush, 'FrostBush'), (build_wall, 'IceWall')):
        reset(); ob = fn(); tris = finish(ob, name); export_fbx(ob, name)
        render_preview([ob], f'{PREVIEW}/{name}.png', res=360)
        dims = ob.dimensions
        res[name] = (tris, round(dims.x, 2), round(dims.y, 2), round(dims.z, 2))
    print(res)
