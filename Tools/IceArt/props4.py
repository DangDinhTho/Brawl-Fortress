import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from props3 import *

def build_lamp():
    """1960s rural street light: a wooden utility pole with a cross-arm and glass insulators, and an enamel cone shade
    hanging from a curved iron arm"""
    bm = bmesh.new(); C = ['wood', 'wood_dark', 'wood_light', 'black', 'metal', 'enamel', 'snow', 'snow_shade', 'yellow', 'ceramic']
    woodc = lambda f: 2 if lit(f.normal) > 0.5 else (0 if lit(f.normal) > -0.1 else 1)
    # tapered round pole with a few bands, slightly leaning
    part(bm, lambda bm: lathe(bm, [(0.15, 0.0), (0.135, 1.6), (0.115, 3.55)], 12, None, 0, Matrix.Rotation(0.02, 4, 'X')), lambda f: 0)
    part(bm, lambda bm: (box(bm, (1.10, 0.12, 0.12), (0, 0.0, 3.25)), box(bm, (0.06, 0.05, 0.55), (0.30, 0.0, 3.0), Matrix.Rotation(0.75, 4, 'Y')),
                         box(bm, (0.06, 0.05, 0.55), (-0.30, 0.0, 3.0), Matrix.Rotation(-0.75, 4, 'Y'))), woodc, bevel=0.01)          # cross-arm + braces
    def insulators(bm):
        for x in (-0.46, -0.22, 0.22, 0.46):
            lathe(bm, [(0.035, 3.31), (0.065, 3.34), (0.05, 3.42), (0.03, 3.45)], 7)
            bmesh.ops.translate(bm, vec=(x, 0, 0), verts=[v for v in bm.verts if abs(v.co.x) < 0.08 and 3.30 < v.co.z < 3.46 and abs(v.co.y) < 0.08 and v.is_valid and not v.link_faces[0].material_index == 99] if False else [])
    def insul(bm):
        for x in (-0.46, -0.22, 0.22, 0.46):
            lathe(bm, [(0.035, 0.0), (0.065, 0.03), (0.05, 0.11), (0.03, 0.14)], 7, None, 0, Matrix.Translation((x, 0, 3.31)))
    part(bm, insul, lambda f: 9)
    # curved iron arm (four bent segments) towards the front
    def arm(bm):
        pts = [Vector((0, -0.12, 2.75)), Vector((0, -0.45, 2.88)), Vector((0, -0.75, 2.92)), Vector((0, -0.98, 2.86)), Vector((0, -1.08, 2.74))]
        for a, b in zip(pts, pts[1:]):
            d = b - a; mid = (a + b) / 2
            rot = Matrix.Rotation(math.atan2(d.z, d.y), 4, 'X')
            box(bm, (0.075, d.length + 0.03, 0.075), mid.to_tuple(), rot)
        box(bm, (0.07, 0.07, 0.34), (0, -0.13, 2.62))           # bracket on the pole
        # diagonal brace from lower on the pole to the middle of the arm
        a, b = Vector((0, -0.12, 2.40)), Vector((0, -0.62, 2.88)); d = b - a
        box(bm, (0.055, d.length, 0.055), ((a + b) / 2).to_tuple(), Matrix.Rotation(math.atan2(d.z, d.y), 4, 'X'))
        box(bm, (0.05, 0.05, 0.12), (0, -1.08, 2.70))          # stem down to the shade
    part(bm, arm, lambda f: 3, bevel=0.01)
    # enamel cone shade (green outside, white inside) with a bulb
    def shade(bm):
        lathe(bm, [(0.42, 2.38), (0.30, 2.48), (0.12, 2.62), (0.05, 2.70)], 12, None, 0, Matrix.Translation((0, -1.08, 0)))
    part(bm, shade, lambda f: 5 if f.normal.z > -0.2 else 6, bevel=0.012, angle=25)
    part(bm, lambda bm: lathe(bm, [(0.40, 2.39), (0.29, 2.47), (0.11, 2.60)], 12, None, 0, Matrix.Translation((0, -1.08, 0)) @ Matrix.Diagonal((0.98, 0.98, 1, 1))),
         lambda f: 6)   # white enamel inside (faces point down)
    part(bm, lambda bm: blob(bm, 0.10, (0, -1.08, 2.46), (1, 1, 1.2), 2, floor=False), lambda f: 8)
    # snow on the shade, the cross-arm and the pole top
    def snow(bm):
        lathe(bm, [(0.36, 2.52), (0.22, 2.62), (0.06, 2.72)], 12, None, 0, Matrix.Translation((0, -1.08, 0.03)))
        vs = box(bm, (1.05, 0.15, 0.05), (0, 0.0, 3.335)); jitter(vs, 0.01, 2)
        blob(bm, 0.14, (0, 0, 3.58), (1, 1, 0.5), 2, floor=False)
    part(bm, snow, lambda f: 6 if f.normal.z > 0.3 else 7)
    part(bm, lambda bm: blob(bm, 0.34, (0, 0, 0), (1, 1, 0.35), 2, 0.02, 3), lambda f: 6 if f.normal.z > 0.35 else 7)
    return bm_to_obj(bm, 'LampPost', C)

def build_tent():
    """canvas wall tent of the 60s: khaki canvas, olive flaps, wooden poles, guy ropes"""
    bm = bmesh.new(); C = ['khaki', 'olive', 'black', 'wood', 'wood_dark', 'snow', 'snow_shade', 'wood_light']
    W, L, H, Wall = 2.2, 2.6, 1.85, 0.55
    def canvas(bm):
        def v(x, y, z): return bm.verts.new((x, y, z))
        n = 4
        lb, lt, rb, rt, rg = [], [], [], [], []
        for i in range(n + 1):
            y = -L / 2 + L * i / n; sag = 0.05 * math.sin(math.pi * i / n)
            lb.append(v(-W / 2, y, 0.0)); lt.append(v(-W / 2 + 0.03, y, Wall - sag * 0.5))
            rb.append(v(W / 2, y, 0.0)); rt.append(v(W / 2 - 0.03, y, Wall - sag * 0.5))
            rg.append(v(0, y, H - sag))
        for i in range(n):
            bm.faces.new((lb[i], lb[i + 1], lt[i + 1], lt[i])); bm.faces.new((lt[i], lt[i + 1], rg[i + 1], rg[i]))
            bm.faces.new((rb[i + 1], rb[i], rt[i], rt[i + 1])); bm.faces.new((rt[i + 1], rt[i], rg[i], rg[i + 1]))
        bm.faces.new((rb[n], rt[n], rg[n], lt[n], lb[n]))            # back
        # front with a doorway split in two flaps
        d0, d1, dt = v(-0.40, -L / 2, 0.0), v(0.40, -L / 2, 0.0), v(0, -L / 2, 1.30)
        bm.faces.new((lb[0], lt[0], rg[0], dt, d0)); bm.faces.new((rg[0], rt[0], rb[0], d1, dt))
    part(bm, canvas, lambda f: 0 if lit(f.normal) > 0.2 else 1)
    def door(bm):
        a = bm.verts.new((-0.40, -L / 2 + 0.04, 0.0)); b = bm.verts.new((0.40, -L / 2 + 0.04, 0.0)); c = bm.verts.new((0, -L / 2 + 0.04, 1.30))
        bm.faces.new((a, c, b))
    part(bm, door, lambda f: 2)
    def flaps(bm):   # rolled-up flaps either side of the doorway
        for s in (-1, 1): box(bm, (0.12, 0.10, 1.10), (s * 0.52, -L / 2 - 0.05, 0.62), Matrix.Rotation(-s * 0.33, 4, 'Y'))
    part(bm, flaps, lambda f: 1, bevel=0.03)
    def poles(bm):
        for y in (-L / 2 - 0.06, L / 2 + 0.06): box(bm, (0.08, 0.08, H + 0.35), (0, y, (H + 0.35) / 2))
        box(bm, (0.06, L + 0.30, 0.06), (0, 0, H + 0.02))
        for y in (-L / 2 + 0.2, L / 2 - 0.2):             # guy ropes to pegs
            for s in (-1, 1):
                a, b = Vector((s * (W / 2 - 0.05), y, Wall)), Vector((s * (W / 2 + 0.75), y, 0.05)); d = b - a
                box(bm, (0.025, 0.025, d.length), ((a + b) / 2).to_tuple(), Matrix.Rotation(math.atan2(d.x, d.z), 4, 'Y'))
                box(bm, (0.06, 0.06, 0.18), (b.x, b.y, 0.06))
    part(bm, poles, lambda f: 3 if lit(f.normal) > 0.3 else 4, bevel=0.008)
    def snow(bm):
        rnd = random.Random(6)
        vs = box(bm, (0.40, L * 0.94, 0.10), (0, 0, H - 0.02)); jitter(vs, 0.02, 4)
        for k in range(5): blob(bm, 0.24, (rnd.choice([-1, 1]) * rnd.uniform(1.15, 1.45), rnd.uniform(-1.1, 1.1), 0.0), (1.2, 1, 0.5), 2, 0.02, k)
    part(bm, snow, lambda f: 5 if f.normal.z > 0.35 else 6)
    return bm_to_obj(bm, 'Tent', C)

if __name__ == '__main__':
    for fn, name in ((build_lamp, 'LampPost'), (build_tent, 'Tent')):
        reset(); ob = fn(); tris = finish(ob, name); export_fbx(ob, name)
        render_preview([ob], f'{PREVIEW}/{name}.png', res=360); d = ob.dimensions
        print(name, tris, round(d.x, 2), round(d.y, 2), round(d.z, 2))
