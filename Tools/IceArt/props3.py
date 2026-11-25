import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *

def blob(bm, r, loc, scale=(1, 1, 1), sub=3, jit=0.0, seed=0, floor=True):
    vs = bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=r, matrix=Matrix.Translation(loc) @ Matrix.Diagonal((scale[0], scale[1], scale[2], 1)))['verts']
    if jit: jitter(vs, jit, seed)
    if floor:
        for v in vs:
            if v.co.z < 0: v.co.z = 0
    return vs

def snowc(f): return 3 if f.normal.z > 0.35 else 4

def build_iceblock(seed=41):
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['ice_light', 'ice', 'ice_deep', 'ice_dark', 'snow', 'snow_shade']
    def icec(shift):
        def f(face):
            l = lit(face.normal) + shift
            return 0 if l > 0.5 else (1 if l > 0.05 else (2 if face.normal.z > -0.5 else 3))
        return f
    chunks = [(-0.37, -0.37, 0.98), (0.37, -0.36, 1.12), (-0.36, 0.37, 1.20), (0.38, 0.38, 0.92)]
    for k, (x, y, hgt) in enumerate(chunks):
        def mk(bm, x=x, y=y, hgt=hgt, k=k):
            vs = box(bm, (0.70, 0.70, hgt), (x + rnd.uniform(-0.02, 0.02), y + rnd.uniform(-0.02, 0.02), hgt / 2), Matrix.Rotation(rnd.uniform(-0.06, 0.06), 4, 'Z'))
            jitter(vs, 0.035, seed + k)
        part(bm, mk, icec([0.0, -0.18, 0.12, -0.08][k]), bevel=0.045)
    def top(bm):
        vs = box(bm, (1.36, 1.30, 0.36), (rnd.uniform(-0.05, 0.05), rnd.uniform(-0.05, 0.05), 1.36), Matrix.Rotation(0.05, 4, 'Z')); jitter(vs, 0.04, seed + 9)
    part(bm, top, icec(0.08), bevel=0.05)
    part(bm, lambda bm: blob(bm, 1.0, (0.05, 0.0, 1.55), (0.60, 0.56, 0.13), 3, 0.015, seed, floor=False),
         lambda f: 4 if f.normal.z > 0.35 else 5)
    return bm_to_obj(bm, 'IceBlock', C)

def build_fence():
    bm = bmesh.new(); C = ['wood', 'wood_dark', 'wood_light', 'snow', 'snow_shade']
    woodc = lambda f: 2 if lit(f.normal) > 0.5 else (0 if lit(f.normal) > -0.1 else 1)
    def posts(bm):
        for x in (-0.66, 0.66):
            box(bm, (0.16, 0.16, 1.0), (x, 0, 0.5))
            add_cone(bm, 4, 0.115, 0.0, 0.16, Matrix.Translation((x, 0, 1.08)) @ Matrix.Rotation(math.pi / 4, 4, 'Z'))
    part(bm, posts, lambda f: 1 if lit(f.normal) < 0.2 else 0, bevel=0.015)
    def rails(bm):
        for z in (0.34, 0.74): box(bm, (1.50, 0.08, 0.14), (0, -0.05, z))
    part(bm, rails, woodc, bevel=0.015)
    def snow(bm):
        rnd = random.Random(2)
        for z in (0.41, 0.81):
            vs = box(bm, (1.36, 0.12, 0.07), (0, -0.05, z + 0.035)); jitter(vs, 0.012, 3)
            for k in range(3):
                blob(bm, 0.07, (rnd.uniform(-0.55, 0.55), -0.05, z + 0.06), (1.4, 0.9, 0.6), 2, 0.0, 0, floor=False)
        for x in (-0.66, 0.66): blob(bm, 0.11, (x, 0, 1.14), (1, 1, 0.55), 2, 0.0, 0, floor=False)
    part(bm, snow, snowc)
    return bm_to_obj(bm, 'WoodFence', C)

def build_tuft(seed=51):
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['pine', 'pine_dark', 'pine_light', 'snow', 'snow_shade']
    for (x, y, z, r) in [(0, 0, 0.55, 0.42), (0.22, 0.12, 0.95, 0.30), (-0.2, 0.05, 0.9, 0.30), (0.0, -0.05, 1.22, 0.24)]:
        part(bm, lambda bm, x=x, y=y, z=z, r=r: blob(bm, r, (x, y, z), (1, 1, 0.9), 3, r * 0.05, rnd.randint(0, 999)), lambda f: 0)
        part(bm, lambda bm, x=x, y=y, z=z, r=r: blob(bm, 1.0, (x, y, z + r * 0.50), (r * 0.84, r * 0.84, r * 0.40), 3, r * 0.025, rnd.randint(0, 999), floor=False), lambda f: 3)
    return bm_to_obj(bm, 'SnowTuft', C)

def build_snowman():
    bm = bmesh.new(); C = ['snow', 'snow_shade', 'snow_deep', 'black', 'orange', 'red', 'wood_dark', 'wood']
    sc = lambda f: 0
    part(bm, lambda bm: (blob(bm, 0.56, (0, 0, 0.50), (1, 1, 0.92), 3), blob(bm, 0.42, (0, 0, 1.20), (1, 1, 0.95), 3, floor=False), blob(bm, 0.32, (0, 0, 1.78), (1, 1, 1), 3, floor=False)), sc)
    def coal(bm):
        for p in [(-0.12, -0.29, 1.86), (0.12, -0.29, 1.86), (0, -0.41, 1.32), (0, -0.42, 1.14), (0, -0.40, 0.96)]:
            blob(bm, 0.045, p, (1, 1, 1), 2, floor=False)
    part(bm, coal, lambda f: 3)
    part(bm, lambda bm: add_cone(bm, 6, 0.06, 0.0, 0.34, Matrix.Translation((0, -0.45, 1.76)) @ Matrix.Rotation(math.pi / 2, 4, 'X')), lambda f: 4)
    def scarf(bm):
        lathe(bm, [(0.36, 0.0), (0.38, 0.06), (0.36, 0.13)], 14, None, 0, Matrix.Translation((0, 0, 1.50)))
        box(bm, (0.14, 0.05, 0.42), (0.22, -0.30, 1.30), Matrix.Rotation(0.25, 4, 'Y'))
    part(bm, scarf, lambda f: 5, bevel=0.015)
    def hat(bm):
        lathe(bm, [(0.34, 0.0), (0.34, 0.04)], 12, None, 0, Matrix.Translation((0, 0, 2.03)))
        lathe(bm, [(0.21, 0.0), (0.22, 0.36)], 12, None, 0, Matrix.Translation((0, 0, 2.06)))
    part(bm, hat, lambda f: 3, bevel=0.012)
    def arms(bm):
        def stick(a, b, t):
            a, b = Vector(a), Vector(b); d = b - a
            ang = math.atan2(d.z, math.hypot(d.x, d.y)); yaw = math.atan2(d.y, d.x)
            box(bm, (d.length, t, t), ((a + b) / 2).to_tuple(), Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Rotation(-ang, 4, 'Y'))
        for s in (-1, 1):
            stick((s * 0.30, 0, 1.22), (s * 0.86, 0, 1.52), 0.055)
            stick((s * 0.70, 0, 1.44), (s * 0.80, 0, 1.70), 0.04)    # twig
    part(bm, arms, lambda f: 6 if lit(f.normal) < 0.3 else 7)
    return bm_to_obj(bm, 'Snowman', C)

def build_lamp():
    bm = bmesh.new(); C = ['black', 'metal', 'stone_dark', 'stone', 'yellow', 'snow', 'snow_shade']
    part(bm, lambda bm: box(bm, (0.46, 0.46, 0.32), (0, 0, 0.16)), lambda f: 3 if lit(f.normal) > 0.2 else 2, bevel=0.03)
    part(bm, lambda bm: (lathe(bm, [(0.10, 0.32), (0.07, 2.55)], 8), lathe(bm, [(0.13, 0.32), (0.13, 0.48)], 8)), lambda f: 1 if lit(f.normal) > 0.4 else 0, bevel=0.01)
    def lantern(bm):
        box(bm, (0.34, 0.34, 0.06), (0, 0, 2.58))
        for sx in (-1, 1):
            for sy in (-1, 1): box(bm, (0.05, 0.05, 0.42), (sx * 0.15, sy * 0.15, 2.82))
        add_cone(bm, 4, 0.30, 0.04, 0.24, Matrix.Translation((0, 0, 3.14)) @ Matrix.Rotation(math.pi / 4, 4, 'Z'))
    part(bm, lantern, lambda f: 1 if lit(f.normal) > 0.4 else 0, bevel=0.01)
    part(bm, lambda bm: box(bm, (0.27, 0.27, 0.40), (0, 0, 2.82)), lambda f: 4)                 # glowing glass
    part(bm, lambda bm: blob(bm, 0.24, (0, 0, 3.24), (1, 1, 0.42), 2, floor=False), lambda f: 5 if f.normal.z > 0.3 else 6)
    part(bm, lambda bm: blob(bm, 0.30, (0, 0, 0.33), (1, 1, 0.25), 2, floor=False), lambda f: 5 if f.normal.z > 0.3 else 6)
    return bm_to_obj(bm, 'LampPost', C)

def build_sled():
    bm = bmesh.new(); C = ['wood_light', 'wood', 'wood_dark', 'red', 'metal', 'black', 'snow']
    def runners(bm):
        for x in (-0.30, 0.30):
            box(bm, (0.05, 1.30, 0.05), (x, 0, 0.03))
            box(bm, (0.05, 0.28, 0.05), (x, -0.74, 0.14), Matrix.Rotation(-0.9, 4, 'X'))   # front curl
            for y in (-0.45, 0.0, 0.45): box(bm, (0.04, 0.04, 0.20), (x, y, 0.14))
    part(bm, runners, lambda f: 4 if lit(f.normal) > 0.3 else 5, bevel=0.008)
    def deck(bm):
        for k in range(6): box(bm, (0.74, 0.17, 0.05), (0, -0.50 + k * 0.20, 0.26))
        for x in (-0.36, 0.36): box(bm, (0.05, 1.18, 0.07), (x, 0, 0.30))
    part(bm, deck, lambda f: 0 if lit(f.normal) > 0.45 else (1 if lit(f.normal) > 0.0 else 2), bevel=0.012)
    part(bm, lambda bm: box(bm, (0.60, 0.50, 0.10), (0, 0.25, 0.34)), lambda f: 3, bevel=0.03)   # red cushion
    part(bm, lambda bm: blob(bm, 0.18, (-0.1, -0.35, 0.30), (1.3, 1, 0.45), 2, floor=False), lambda f: 6)
    return bm_to_obj(bm, 'Sled', C)

def build_flag():
    bm = bmesh.new(); C = ['metal', 'black', 'yellow', 'red', 'snow', 'snow_shade']
    part(bm, lambda bm: lathe(bm, [(0.05, 0.0), (0.04, 3.2)], 10), lambda f: 0)
    part(bm, lambda bm: blob(bm, 0.09, (0, 0, 3.26), (1, 1, 1), 2, floor=False), lambda f: 2)
    def cloth(bm):
        # a waving pennant: a strip of quads bent along a sine
        n = 6; L = 1.05; top, bot = 3.12, 2.42
        cols = []
        for i in range(n + 1):
            u = i / n; x = 0.05 + u * L; y = math.sin(u * math.pi * 1.5) * 0.12
            h = (top - bot) * (1 - u) * 0.5
            mid = (top + bot) / 2
            cols.append((bm.verts.new((x, y - 0.02, mid + h)), bm.verts.new((x, y - 0.02, mid - h)), bm.verts.new((x, y + 0.02, mid + h)), bm.verts.new((x, y + 0.02, mid - h))))
        for i in range(n):
            a, b = cols[i], cols[i + 1]
            if i < n - 1 or True:
                bm.faces.new((a[0], a[1], b[1], b[0])); bm.faces.new((a[2], b[2], b[3], a[3]))
                bm.faces.new((a[0], b[0], b[2], a[2])); bm.faces.new((a[1], a[3], b[3], b[1]))
        bm.faces.new((cols[0][0], cols[0][2], cols[0][3], cols[0][1]))
    part(bm, cloth, lambda f: 3)
    part(bm, lambda bm: blob(bm, 0.40, (0, 0, 0), (1, 1, 0.45), 3, 0.02, 5), lambda f: 4 if f.normal.z > 0.35 else 5)
    return bm_to_obj(bm, 'Flag', C)

def build_tent():
    bm = bmesh.new(); C = ['orange', 'red', 'black', 'wood', 'wood_dark', 'snow', 'snow_shade']
    W, L, H = 2.0, 2.4, 1.6
    def canvas(bm):
        # A-frame shell with a little sag in the middle of each side
        def v(x, y, z): return bm.verts.new((x, y, z))
        n = 4
        left, right, ridge = [], [], []
        for i in range(n + 1):
            y = -L / 2 + L * i / n; sag = 0.06 * math.sin(math.pi * i / n)
            left.append(v(-W / 2, y, 0.0)); right.append(v(W / 2, y, 0.0)); ridge.append(v(0, y, H - sag))
        for i in range(n):
            bm.faces.new((left[i], left[i + 1], ridge[i + 1], ridge[i]))
            bm.faces.new((right[i + 1], right[i], ridge[i], ridge[i + 1]))
        bm.faces.new((right[n], ridge[n], left[n]))                       # back wall
        # front: two flaps leaving a dark doorway
        f1 = v(-0.35, -L / 2 - 0.02, 0.0); f2 = v(0.35, -L / 2 - 0.02, 0.0); f3 = v(0, -L / 2 - 0.02, 1.05)
        bm.faces.new((left[0], ridge[0], f3, f1)); bm.faces.new((ridge[0], right[0], f2, f3))
    part(bm, canvas, lambda f: 0 if lit(f.normal) > 0.25 else 1)
    def door(bm):
        a = bm.verts.new((-0.35, -L / 2 + 0.03, 0.0)); b = bm.verts.new((0.35, -L / 2 + 0.03, 0.0)); c = bm.verts.new((0, -L / 2 + 0.03, 1.05))
        bm.faces.new((a, c, b))
    part(bm, door, lambda f: 2)
    def poles(bm):
        for y in (-L / 2 - 0.05, L / 2 + 0.05): box(bm, (0.07, 0.07, H + 0.25), (0, y, (H + 0.25) / 2))
        box(bm, (0.06, L + 0.25, 0.06), (0, 0, H + 0.02))
    part(bm, poles, lambda f: 3 if lit(f.normal) > 0.3 else 4, bevel=0.01)
    def snow(bm):
        vs = box(bm, (0.46, L * 0.92, 0.10), (0, 0, H - 0.02)); jitter(vs, 0.02, 4)
        rnd = random.Random(6)
        for k in range(4): blob(bm, 0.22, (rnd.choice([-0.55, 0.55]) * rnd.uniform(0.6, 1.0), rnd.uniform(-0.9, 0.9), 0.0), (1.2, 1, 0.5), 2, 0.02, k)
    part(bm, snow, lambda f: 5 if f.normal.z > 0.35 else 6)
    return bm_to_obj(bm, 'Tent', C)

def build_mound(seed=61):
    bm = bmesh.new(); C = ['snow', 'snow_shade', 'snow_deep']
    part(bm, lambda bm: (blob(bm, 0.8, (0, 0, 0), (1, 0.8, 0.55), 3, 0.04, seed), blob(bm, 0.45, (0.55, 0.3, 0), (1, 1, 0.75), 3, 0.03, seed + 1)),
         lambda f: 0)
    return bm_to_obj(bm, 'SnowMound', C)

BUILDERS = [(build_iceblock, 'IceBlock'), (build_fence, 'WoodFence'), (build_tuft, 'SnowTuft'), (build_snowman, 'Snowman'),
            (build_lamp, 'LampPost'), (build_sled, 'Sled'), (build_flag, 'Flag'), (build_tent, 'Tent'), (build_mound, 'SnowMound')]

if __name__ == '__main__':
    res = {}
    for fn, name in BUILDERS:
        reset(); ob = fn(); tris = finish(ob, name); export_fbx(ob, name)
        render_preview([ob], f'{PREVIEW}/{name}.png', res=300)
        d = ob.dimensions; res[name] = (tris, round(d.x, 2), round(d.y, 2), round(d.z, 2))
    print(res)
