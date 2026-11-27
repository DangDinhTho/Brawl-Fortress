# Toy-style Demoman after Tho's concept sheet (beanie, eyepatch, jaw beard, black vest with four grenades,
# belt pouches, short legs). Static model in its final proportions: NOT on the old skeleton.
#   exec(open(r"...\Tools\CharArt\toy_demo.py").read())  -> object "ToyDemo" + renders (driver_namespace "toy_*")
# Space: +x = the character's left, -y = front, +z = up, 100 units tall.
import bpy, math, os
from mathutils import Vector, Matrix

_src = open(os.path.join(r"E:\UnityProjects\Brawl Fortress\Tools\CharArt", "pirate_demo.py"), encoding="utf-8").read()
exec(_src.split("# ====================================================================================================")[0], globals())
ns = bpy.app.driver_namespace
TEAM = ns.get("toy_team", 1)

PALETTE = [
    ("shirt",   (0.66, 0.15, 0.15), (0.30, 0.55, 0.80)),
    ("pants",   (0.50, 0.10, 0.11), (0.17, 0.36, 0.62)),
    ("skin",    (0.33, 0.20, 0.145), None),
    ("beard",   (0.15, 0.105, 0.095), None),
    ("line",    (0.05, 0.04, 0.04), None),
    ("hat",     (0.20, 0.215, 0.26), None),
    ("hat_dk",  (0.16, 0.17, 0.21), None),
    ("patch",   (0.19, 0.19, 0.22), None),
    ("vest",    (0.085, 0.085, 0.10), None),
    ("belt",    (0.06, 0.06, 0.07), None),
    ("gren",    (0.27, 0.27, 0.31), None),
    ("gold",    (0.80, 0.60, 0.24), None),
    ("pouch",   (0.52, 0.53, 0.58), None),
    ("pouch_dk", (0.40, 0.41, 0.46), None),
    ("cuff",    (0.92, 0.91, 0.88), None),
    ("boot",    (0.065, 0.06, 0.07), None),
    ("sole",    (0.17, 0.17, 0.19), None),
]
CI = {p[0]: i for i, p in enumerate(PALETTE)}
V.clear(); W.clear(); F.clear()
N = {}                                   # no skinning in this version

# ---------------------------------------------------------------------------------------------------- body shape
TK = [(13.0, 9.5, 7.6, 0.6), (16.0, 12.6, 9.6, 0.6), (20.0, 14.2, 10.8, 0.6), (24.0, 14.9, 11.3, 0.6), (30.0, 15.2, 11.4, 0.7),
      (36.0, 15.0, 11.0, 0.9), (41.0, 13.8, 10.0, 1.1), (44.5, 11.0, 8.4, 1.2), (47.0, 7.0, 6.0, 1.2)]
TSQ = 2.7

# ---------------------------------------------------------------------------------------------------- head shape
HC = Vector((0.0, -0.4, 66.0))
HRX, HRY, HP, HSQ = 27.0, 24.0, 2.5, 2.35


def hrad(z, grow=0.0):
    rz = (30.0 if z > HC.z else 22.6) + grow
    t = min(1.0, abs(z - HC.z) / rz)
    s = (1 - t ** HP) ** (1 / HP)
    return (HRX + grow) * s, (HRY + grow) * s


def hpt(a, z, grow=0.0):
    rx, ry = hrad(z, grow)
    px, py = sup(a, HSQ)
    return Vector((rx * px, HC.y + ry * py, z))


def hfront(x, z, grow=0.0):
    rx, ry = hrad(z, grow)
    t = min(1.0, abs(x) / max(rx, 1e-4))
    return Vector((x, HC.y - ry * (1 - t ** HSQ) ** (1 / HSQ), z))


def hnorm(x, z):
    e = 0.3
    p = hfront(x, z)
    dx = hfront(x + e, z) - hfront(x - e, z)
    dz = hfront(x, z + e) - hfront(x, z - e)
    n = dx.cross(dz).normalized()
    return n if n.y < 0 else -n


# ==================================================================================================== left side
v_start, f_start = len(V), len(F)

# trouser leg, boot with a rolled top, foot on a thick sole
LX = 7.0
tube([(LX - 0.4, 0.4, 21.5), (LX, 0.4, 17.0), (LX, 0.4, 12.0), (LX, 0.4, 8.5)], [6.3, 6.7, 6.6, 6.0], N, 18, "pants", hint=X)
tube([(LX, 0.5, 11.6), (LX, 0.5, 11.2), (LX, 0.5, 9.4), (LX, 0.5, 8.8), (LX, 0.5, 4.0)], [6.0, 7.2, 7.3, 6.5, 6.3], N, 18, "boot", hint=X)
rows = [(8.0, 2.8, 5.2), (6.6, 5.1, 7.8), (2.0, 5.8, 8.4), (-3.5, 6.1, 7.6), (-8.0, 6.0, 6.8), (-10.8, 4.8, 5.6), (-11.9, 2.6, 3.2)]
loft([dict(c=Vector((LX + 0.2, y, 1.4 + h / 2)), u=X.copy(), v=Z.copy(), ru=hw, rv=h / 2, w=N) for y, hw, h in rows], 18, "boot", sq=2.6)
loft([dict(c=Vector((LX + 0.2, y, 1.1)), u=X.copy(), v=Z.copy(), ru=hw + 0.55, rv=1.15, w=N) for y, hw, h in rows], 18, "sole", sq=3.2)
ellipsoid((LX + 0.2, -6.4, 5.6), (5.2, 4.6, 3.6), N, "boot", n=14, m=8)                        # round toe cap

# arm: shirt sleeve out of the vest's armhole, white cuff, short wrist, fist
tube([(12.6, 0.6, 42.0), (15.6, 0.5, 42.6), (18.6, 0.3, 40.4), (20.4, 0.0, 36.0), (21.3, -0.5, 31.4), (21.5, -0.7, 29.6)],
     [5.0, 5.9, 6.2, 5.8, 5.5, 5.0], N, 18, "shirt", hint=Y)
tube([(21.3, -0.6, 30.9), (21.4, -0.7, 30.5), (21.6, -1.0, 28.6), (21.8, -1.3, 26.9), (21.9, -1.4, 26.5)],
     [5.2, 5.95, 6.05, 5.9, 5.1], N, 18, "cuff", hint=Y)
tube([(21.8, -1.3, 27.2), (22.1, -1.9, 23.2)], [4.3, 4.2], N, 14, "skin", hint=Y, cap0=False, cap1=False)
HN = Vector((22.4, -2.6, 20.4))
ellipsoid(HN, (4.5, 5.0, 4.9), N, "skin", n=16, m=10)
ellipsoid(HN + Vector((-2.2, -3.6, 1.2)), (1.9, 2.3, 2.7), N, "skin", n=10, m=6)                # thumb
for k in range(3):                                                                               # knuckle grooves
    y = -4.6 + 2.3 * k
    tube([HN + Vector((3.4, y, -3.4)), HN + Vector((1.2, y, -4.75)), HN + Vector((-1.6, y, -4.4))], [0.16, 0.2, 0.16], N, 5, "line", hint=Y)

# ear
ellipsoid((HRX - 0.6, 3.0, 59.5), (3.2, 4.3, 5.6), N, "skin", n=12, m=8)
ellipsoid((HRX + 1.1, 2.4, 59.5), (1.6, 2.6, 3.6), N, "beard", n=10, m=6)

# two grenades on this side of the chest: dark tube, gold nose pointing outwards, a loop of webbing
for gz, gx, tilt in ((39.4, 7.0, -0.20), (31.6, 7.3, -0.12)):
    c = tor_pt(math.radians(270 + 24), gz, 4.2)
    c.x = gx
    ax = Vector((1.0, 0.16, tilt)).normalized()
    hint = Z
    L = 5.0
    loft([R(c - ax * L, ax, hint, 1.9, 1.9, N), R(c - ax * (L - 0.5), ax, hint, 3.2, 3.2, N), R(c - ax * 1.0, ax, hint, 3.3, 3.3, N),
          R(c + ax * (L - 1.6), ax, hint, 3.3, 3.3, N), R(c + ax * (L - 1.4), ax, hint, 2.8, 2.8, N)], 14, "gren")
    loft([R(c + ax * (L - 1.5), ax, hint, 2.9, 2.9, N), R(c + ax * (L - 0.9), ax, hint, 3.25, 3.25, N), R(c + ax * (L + 0.6), ax, hint, 2.9, 2.9, N),
          R(c + ax * (L + 1.5), ax, hint, 1.7, 1.7, N)], 14, "gold")
    torus(c - ax * 0.6, ax, 3.45, 0.6, "vest", N, n=16, m=6)

# belt pouches: a pale one beside the buckle line, a dark one on the hip
pa = math.radians(270 + 19)
pn = tor_out(pa, 21.5)
pt = Vector((-pn.y, pn.x, 0))
box(tor_pt(pa, 20.8, 2.3), 3.7, 2.2, 4.2, N, "pouch", ax=pt, ay=pn)
box(tor_pt(pa, 23.2, 2.9), 3.9, 2.0, 1.9, N, "pouch_dk", ax=pt, ay=pn)
ellipsoid(tor_pt(pa, 21.6, 4.7), (0.7, 0.5, 0.7), N, "vest", n=8, m=4)
pa2 = math.radians(270 + 66)
pn2 = tor_out(pa2, 21.5)
box(tor_pt(pa2, 21.2, 1.8), 3.0, 1.7, 3.3, N, "vest", ax=Vector((-pn2.y, pn2.x, 0)), ay=pn2)

# half of the goatee: moustache running down to the chin beard
g = [hfront(0.0, 59.6, 0.25), hfront(3.6, 59.3, 0.25), hfront(6.6, 57.6, 0.25), hfront(7.6, 54.6, 0.25), hfront(7.4, 51.6, 0.2)]
tube(g, [(0.5, 1.2), (0.55, 1.35), (0.55, 1.3), (0.5, 1.2), (0.3, 0.9)], N, 8, "beard", hint=Y, cap0=False)

mirror(v_start, f_start)

# ==================================================================================================== centre
NT = 32
PHT = math.radians(360 / NT / 2)
ZT = [lerp(13.0, 47.0, i / 24) for i in range(25)]
loft([zr(z, *tor(z), N) for z in ZT], NT, lambda i, j: "pants" if ZT[i] < 21.0 else "shirt", phase=PHT, sq=TSQ)
# vest: one thick shell from the belt to the shoulders, raised collar edge
VZ = [(24.4, 0.15), (24.7, 1.25), (27.0, 1.45), (31.0, 1.5), (35.0, 1.5), (39.0, 1.45), (42.4, 1.35), (44.6, 1.2), (45.6, 0.75), (45.9, 0.1)]
loft([zr(z, tor(z)[0] + gw, tor(z)[1] + gw, tor(z)[2], N) for z, gw in VZ], NT, "vest", phase=PHT, sq=TSQ, cap0=False, cap1=False)
loft([zr(z, rx, ry, 1.2, N) for z, rx, ry in ((44.8, 8.4, 7.2), (45.4, 9.6, 8.3), (46.9, 9.4, 8.1), (47.5, 8.0, 6.9))], NT, "vest", phase=PHT, sq=TSQ,
     cap0=False, cap1=False)
tube([tor_pt(math.radians(270), z, 1.7) for z in (25.0, 30.0, 36.0, 42.0, 45.0)], [(0.45, 0.35)] * 5, N, 6, "gren", hint=X)       # zip
# belt
loft([zr(z, tor(z)[0] + gw, tor(z)[1] + gw, tor(z)[2], N) for z, gw in ((19.9, 0.2), (20.2, 1.55), (22.0, 1.75), (23.8, 1.55), (24.1, 0.2))],
     NT, "belt", phase=PHT, sq=TSQ, cap0=False, cap1=False)

# head: skin with the jaw beard painted on (no seam), smile
HN_, HM_ = 48, 26
HZ = [lerp(43.6, 94.5, i / HM_) for i in range(HM_ + 1)]
loft([dict(c=Vector((0, HC.y, z)), u=X.copy(), v=Y.copy(), ru=hrad(z)[0], rv=hrad(z)[1], w=N) for z in HZ], HN_, "skin", sq=HSQ)


def jaw_z(a):
    side = abs(math.cos(a))
    return 51.6 + 13.0 * side ** 2.0 - 2.2 * side ** 8                 # low under the mouth, up to the ears at the sides


BA0, BA1, BN, BK = math.pi - 0.36, 2 * math.pi + 0.36, 56, 9
brow = []
for k in range(BK + 1):
    t = k / BK
    row = []
    for j in range(BN + 1):
        a = lerp(BA0, BA1, j / BN)
        edge = min(1.0, min(j, BN - j) / 3.0)
        z = lerp(44.6, jaw_z(a), t)
        gw = 0.42 * edge * (1.0 if k < BK else 0.0) + (0.0 if k < BK else -0.05)
        if k == BK - 1:
            z = jaw_z(a) - 0.5
        row.append(hpt(a, z, gw))
    brow.append(row)
grid(brow, "beard", N, closed=False)
tube([hfront(x, 55.6 - 0.05 * x * x, 0.3) for x in (-3.4, -1.8, 0.0, 1.8, 3.4)], [0.3, 0.55, 0.6, 0.55, 0.3], N, 6, "line", hint=Z)
# closed eye on the character's right, eyepatch on the left with its strap
EX = -9.6
tube([hfront(EX + dx, 67.2 + 2.1 * (1 - (dx / 3.4) ** 2), 0.25) for dx in (-3.4, -2.0, 0.0, 2.0, 3.4)], [0.3, 0.7, 0.8, 0.7, 0.3], N, 6, "line", hint=Z)
PX, PZ = 9.8, 66.6
pn_ = hnorm(PX, PZ)
ptan = Vector((1, 0, 0)) - pn_ * pn_.x
box(hfront(PX, PZ) + pn_ * 0.5, 4.9, 1.0, 4.6, N, "patch", ax=ptan, ay=pn_)
NS = 40
s_pts, s_out = [], []
for k in range(NS):
    a = 2 * math.pi * k / NS
    z = PZ
    for _ in range(4):
        p = hpt(a, z, 0.35)
        z = max(48.0, min(90.0, PZ + 0.56 * (PX - p.x)))
    p = hpt(a, z, 0.35)
    s_pts.append(p)
    s_out.append((p - Vector((0, HC.y, z))).normalized())
strap(s_pts, s_out, 0.75, 0.4, "patch", N)

# beanie: dome and a thick rolled band, wider than the head
DZ = 84.0
ellipsoid((0, HC.y, DZ), (27.8, 24.9, 16.6), N, "hat", n=40, m=12, lat0=0.0, sq=HSQ)
loft([zr(z, rx, ry, HC.y, N) for z, rx, ry in ((75.4, 25.4, 22.4), (75.8, 28.8, 25.8), (77.6, 30.6, 27.6), (81.0, 31.2, 28.2), (84.4, 30.6, 27.6),
                                              (86.4, 29.0, 26.0), (87.0, 27.6, 24.7))], 40, "hat_dk", sq=HSQ, cap0=False, cap1=False)

# ---------------------------------------------------------------------------------------------------- mesh
for nm in ("ToyDemo", "ChibiDemo", "PirateDemo"):
    old = bpy.data.objects.get(nm)
    if old:
        me_old = old.data
        bpy.data.objects.remove(old)
        bpy.data.meshes.remove(me_old)
used = sorted({i for f, _ in F for i in f})
remap = {o: n for n, o in enumerate(used)}
me = bpy.data.meshes.new("ToyDemo")
me.from_pydata([V[i] for i in used], [], [tuple(remap[i] for i in f) for f, _ in F])
me.update()
for p in me.polygons:
    p.use_smooth = True
ob = bpy.data.objects.new("ToyDemo", me)
sc = bpy.context.scene
sc.collection.objects.link(ob)
ca = me.color_attributes.new("Col", 'BYTE_COLOR', 'CORNER')
for p, (_, col) in zip(me.polygons, F):
    pal = PALETTE[CI[col]]
    c = pal[TEAM] or pal[1]
    for li in p.loop_indices:
        ca.data[li].color_srgb = (c[0], c[1], c[2], 1.0)

# ---------------------------------------------------------------------------------------------------- preview
for o in sc.objects:
    if o.type in ('MESH', 'LIGHT', 'ARMATURE') and o is not ob:
        o.hide_viewport = True
        o.hide_render = True
m = bpy.data.materials.get("ToyClay")
if m:
    bpy.data.materials.remove(m)
m = bpy.data.materials.new("ToyClay")
m.use_nodes = True
bsdf = m.node_tree.nodes["Principled BSDF"]
vc = m.node_tree.nodes.new("ShaderNodeVertexColor"); vc.layer_name = "Col"
m.node_tree.links.new(vc.outputs["Color"], bsdf.inputs["Base Color"])
bsdf.inputs["Roughness"].default_value = 0.62
if "Sheen Weight" in bsdf.inputs:
    bsdf.inputs["Sheen Weight"].default_value = 0.2
me.materials.append(m)
sub = ob.modifiers.new("Subsurf", 'SUBSURF'); sub.levels = 2; sub.render_levels = 2


def aim(o, pos, target):
    o.location = Vector(pos)
    o.rotation_euler = (Vector(target) - Vector(pos)).to_track_quat('-Z', 'Y').to_euler()


def cam(name, pos, target, lens=125):
    c = bpy.data.objects.get("TCam" + name)
    if not c:
        c = bpy.data.objects.new("TCam" + name, bpy.data.cameras.new("TCam" + name)); sc.collection.objects.link(c)
    aim(c, pos, target)
    c.data.type = 'PERSP'; c.data.lens = lens; c.data.clip_end = 3000
    return c


def light(name, pos, energy, size, colr, target):
    l = bpy.data.objects.get(name)
    if not l:
        l = bpy.data.objects.new(name, bpy.data.lights.new(name, 'AREA')); sc.collection.objects.link(l)
    l.hide_render = False; l.hide_viewport = False
    l.data.energy = energy; l.data.size = size; l.data.color = colr
    aim(l, pos, target)


T = (0, 0, 50)
cams = {"Front": cam("Front", (0, -470, 62), T), "34": cam("34", (-250, -390, 78), T), "34L": cam("34L", (250, -390, 78), T),
        "Back": cam("Back", (190, 420, 90), T), "Top": cam("Top", (0, -270, 390), (0, 0, 46)), "Side": cam("Side", (470, -10, 62), T)}
light("TKey", (-170, -260, 250), 5.6e6, 190, (1.0, 0.98, 0.95), T)
light("TFill", (260, -200, 70), 1.5e6, 240, (0.86, 0.9, 1.0), T)
light("TRim", (120, 280, 210), 3.2e6, 140, (0.9, 0.95, 1.0), T)
w = bpy.data.worlds.get("TWorld") or bpy.data.worlds.new("TWorld")
w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.014, 0.015, 0.02, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
sc.world = w
sc.view_settings.view_transform = 'Standard'
sc.render.resolution_x = ns.get("toy_w", 760); sc.render.resolution_y = ns.get("toy_h", 860)
files = []
for n in ns.get("toy_shots", ("Front", "34")):
    sc.camera = cams[n]
    sc.render.filepath = ns.get("toy_out", "//") + "\\" + ns.get("toy_tag", "toy") + "_" + n + ".png"
    bpy.ops.render.render(write_still=True)
    files.append(sc.render.filepath)
result = {"verts": len(used), "tris": sum(len(f) - 2 for f, _ in F), "files": files}
