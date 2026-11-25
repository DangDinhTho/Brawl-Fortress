import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *

PART_TRUNK, PART_LEAF, PART_SNOWBASE = 0, 1, 2

def build_pine(seed=3):
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['wood_dark', 'wood', 'pine', 'pine_dark', 'pine_light', 'snow', 'snow_shade']
    lathe(bm, [(0.30, 0.0), (0.26, 0.6), (0.20, 1.3)], 8)                      # trunk
    tiers = [(1.75, 1.45, 0.85), (1.40, 1.35, 1.75), (1.05, 1.25, 2.60), (0.68, 1.15, 3.40)]
    segs = 12
    lines = []
    for t, (R, H, Z) in enumerate(tiers):
        rot = rnd.uniform(0, 1)
        snowline = [rnd.choice([0.08, 0.13, 0.18, 0.24]) for _ in range(segs)]
        lines.append((R, H, Z, rot, snowline))
        def rim(i, k, r, z, R=R, H=H):
            if k == 0:
                tip = i % 2 == 0
                return (r * (1.0 if tip else 0.80), z - (0.22 if tip else 0.02) * (1 + 0.3 * rnd.random()))
            return (r * (1.0 + (0.05 if i % 2 == 0 else -0.02)), z)
        prof = [(R, 0.0), (R * 0.93, 0.10 * H), (R * 0.74, 0.33 * H), (R * 0.50, 0.58 * H), (R * 0.26, 0.82 * H), (0.0, H)]
        lathe(bm, prof, segs, rim, rot, Matrix.Translation((0, 0, Z)))
    bevel_bm(bm, 0.03, 30)
    # colour by geometry only (faces made by the bevel carry no reliable index)
    for f in bm.faces:
        c = f.calc_center_median(); n = f.normal
        rad = math.hypot(c.x, c.y)
        if c.z < 1.25 and rad < 0.34:                                        # trunk
            a = math.atan2(c.y, c.x)
            f.material_index = 0 if int((a + math.pi) / (2 * math.pi) * 8) % 2 == 0 else 1
            continue
        if n.z < -0.55: f.material_index = 3; continue                       # underside of a tier
        # owner tier: the one whose leaf profile passes closest to this face (tiers overlap)
        best, tier = 1e9, None
        for (R, H, Z, rot, snowline) in lines:
            h = (c.z - Z) / H
            if h < -0.3 or h > 1.02: continue
            hp = max(0.0, h)
            pr = [(0.0, 1.0), (0.10, 0.93), (0.33, 0.74), (0.58, 0.50), (0.82, 0.26), (1.0, 0.0)]
            for (h0, r0), (h1, r1) in zip(pr, pr[1:]):
                if h0 <= hp <= h1: expect = R * (r0 + (r1 - r0) * (hp - h0) / (h1 - h0)); break
            d = abs(rad - expect)
            if d < best: best, tier = d, (R, H, Z, rot, snowline)
        if tier is None: f.material_index = 2; continue
        R, H, Z, rot, snowline = tier
        h = (c.z - Z) / H
        i = int(((math.atan2(c.y, c.x) - rot) % (2 * math.pi)) / (2 * math.pi) * segs) % segs
        edge = snowline[i]
        if h >= edge and n.z > 0.3:
            f.material_index = 5 if n.z > 0.45 else 6      # snow on the tier's shoulder
        else:
            f.material_index = 4 if (n.x * 0.6 - n.y * 0.8) > 0.25 else 2   # green fringe of drooping tips
    return bm_to_obj(bm, 'IcePine', C)

def build_crystals(seed=7):
    bm = bmesh.new(); C = ['ice_light', 'ice', 'ice_deep', 'ice_dark', 'snow', 'snow_shade', 'snow_deep']
    spec = [(0.00, 0.00, 0.34, 1.55, 0.0, 0.0), (0.42, 0.18, 0.24, 1.05, 24, 30), (-0.38, 0.25, 0.22, 0.90, 28, 140),
            (0.10, -0.42, 0.20, 0.80, 30, 250), (-0.20, -0.20, 0.15, 0.55, 35, 200), (0.30, -0.10, 0.13, 0.45, 40, 330)]
    for i, (x, y, r, h, tilt, yaw) in enumerate(spec):
        rot = Matrix.Rotation(math.radians(yaw), 4, 'Z') @ Matrix.Rotation(math.radians(tilt), 4, 'X')
        prof = [(r, 0.0), (r * 0.95, h * 0.55), (r * 0.9, h), (0.0, h + r * 1.7)]
        faces = lathe(bm, prof, 6, None, 0.0, Matrix.Translation((x, y, 0.05)) @ rot)
        for f in faces: f.material_index = 10
    mound = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=Matrix.Diagonal((0.95, 0.85, 0.32, 1)))['verts']
    jitter(mound, 0.05, seed)
    for v in mound:
        if v.co.z < 0: v.co.z = 0
    for f in faces_of(bm, mound): f.material_index = 20
    bevel_bm(bm, 0.025, 25)
    for f in bm.faces:
        n = f.normal
        if f.material_index == 20 or f.calc_center_median().z < 0.06 and f.material_index != 10:
            f.material_index = 4 if n.z > 0.45 else (5 if n.z > -0.2 else 6)
        else:
            lit = n.x * 0.55 - n.y * 0.6 + n.z * 0.55
            f.material_index = 0 if lit > 0.45 else (1 if lit > -0.05 else (2 if n.z > -0.6 else 3))
    return bm_to_obj(bm, 'IceCrystals', C)

if __name__ == '__main__':
    results = {}
    for fn, name in ((build_pine, 'IcePine'), (build_crystals, 'IceCrystals')):
        reset(); ob = fn(); tris = finish(ob, name); export_fbx(ob, name)
        render_preview([ob], f'{PREVIEW}/{name}.png', res=420)
        results[name] = tris
    print(results)
