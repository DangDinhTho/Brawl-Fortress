# Chibi pirate powder-man: a replacement body for the Demoman rig (original design, same skeleton).
# Run inside Blender with Demoman.fbx imported (armature "demo.qc_skeleton"):
#   exec(open(r"...\Tools\CharArt\pirate_demo.py").read())
# Builds the mesh "PirateDemo" skinned to the existing bones. Nothing is exported yet (preview only).
# Space: +x = the character's left, -y = front, +z = up, rest pose about 84 units tall.
# In the game RemotePlayer scales bip_spine_1 by 1.5, so everything from the chest up is modelled SMALL here
# (narrow chest, the head only ~10 wide) and reads chibi once the bone is scaled.
import bpy, math
from mathutils import Vector

ARM = bpy.data.objects["demo.qc_skeleton"]
X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))

PALETTE = [  # name, red team, blue team (None = same)
    ("team",       (0.82, 0.16, 0.13), (0.15, 0.40, 0.84)),
    ("team_dark",  (0.50, 0.08, 0.09), (0.08, 0.22, 0.55)),
    ("cream",      (0.96, 0.90, 0.76), None),
    ("gold",       (0.98, 0.74, 0.14), None),
    ("brass",      (0.80, 0.56, 0.16), None),
    ("leather",    (0.46, 0.27, 0.14), None),
    ("leather_dk", (0.27, 0.15, 0.09), None),
    ("skin",       (0.60, 0.39, 0.27), None),
    ("skin_dk",    (0.50, 0.30, 0.21), None),
    ("hair",       (0.20, 0.15, 0.15), None),
    ("patch",      (0.07, 0.06, 0.08), None),
    ("bomb",       (0.15, 0.15, 0.19), None),
    ("hat",        (0.20, 0.18, 0.24), None),
    ("navy",       (0.20, 0.24, 0.38), None),
    ("white",      (0.98, 0.98, 0.96), None),
    ("wood",       (0.66, 0.44, 0.24), None),
    ("iron",       (0.36, 0.37, 0.42), None),
    ("mouth",      (0.38, 0.07, 0.09), None),
    ("rope",       (0.84, 0.74, 0.52), None),
    ("ember",      (1.00, 0.55, 0.08), None),
]
CI = {p[0]: i for i, p in enumerate(PALETTE)}
V, W, F = [], [], []


def bone(n):
    return ARM.data.bones[n].head_local.copy()


def lerp(a, b, t):
    return a + (b - a) * t


def addv(co, w):
    co = Vector(co)
    V.append(co)
    W.append(dict(w(co)) if callable(w) else dict(w))
    return len(V) - 1


def frame(d, hint):
    d = d.normalized()
    u = hint - d * hint.dot(d)
    if u.length < 1e-4:
        u = X - d * X.dot(d)
    u.normalize()
    return u, d.cross(u)


def R(c, d, hint, ru, rv, w):
    u, v = frame(Vector(d), Vector(hint))
    return dict(c=Vector(c), u=u, v=v, ru=ru, rv=rv, w=w)


def zr(z, rx, ry, cy, w, cx=0.0):
    return dict(c=Vector((cx, cy, z)), u=X.copy(), v=Y.copy(), ru=rx, rv=ry, w=w)


def sup(a, sq):
    e = 2.0 / sq
    c, s = math.cos(a), math.sin(a)
    return math.copysign(abs(c) ** e, c), math.copysign(abs(s) ** e, s)


def loft(rings, n, col, sq=2.0, cap0=True, cap1=True, skip=None, phase=0.0, loop=False, capcol=None, flip=False):
    f0 = len(F)
    idx = []
    for r in rings:
        row = []
        for j in range(n):
            px, py = sup(phase + 2 * math.pi * j / n, sq)
            row.append(addv(r["c"] + r["u"] * (r["ru"] * px) + r["v"] * (r["rv"] * py), r["w"]))
        idx.append(row)
    cf = col if callable(col) else (lambda i, j: col)
    m = len(rings)
    for i in range(m if loop else m - 1):
        a, b = idx[i], idx[(i + 1) % m]
        for j in range(n):
            if skip and skip(i, j):
                continue
            k = (j + 1) % n
            F.append(((a[j], a[k], b[k], b[j]), cf(i, j)))
    if not loop:
        if cap0:
            c = addv(rings[0]["c"], rings[0]["w"])
            for j in range(n):
                F.append(((idx[0][(j + 1) % n], idx[0][j], c), capcol or cf(0, j)))
        if cap1:
            c = addv(rings[-1]["c"], rings[-1]["w"])
            for j in range(n):
                F.append(((idx[-1][j], idx[-1][(j + 1) % n], c), capcol or cf(m - 2, j)))
    if flip:
        for i in range(f0, len(F)):
            F[i] = (tuple(reversed(F[i][0])), F[i][1])
    return idx


def tube(pts, radii, ws, n, col, hint=X, sq=2.0, cap0=True, cap1=True, phase=0.0):
    pts = [Vector(p) for p in pts]
    rings = []
    for i, p in enumerate(pts):
        d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)])
        r = radii[i]
        ru, rv = r if isinstance(r, tuple) else (r, r)
        rings.append(R(p, d, hint, ru, rv, ws[i] if isinstance(ws, list) else ws))
    return loft(rings, n, col, sq=sq, cap0=cap0, cap1=cap1, phase=phase)


def ellipsoid(c, rad, w, col, n=12, m=7, lat0=-90.0, lat1=90.0, skip=None, phase=0.0, sq=2.0, flip=False):
    c = Vector(c)
    rings = []
    cap0, cap1 = lat0 <= -89.9, lat1 >= 89.9
    for i in range(m + 1):
        la = math.radians(lerp(lat0, lat1, i / m))
        if cap0 and i == 0:
            la = math.radians(lerp(lat0, lat1, 0.45 / m))
        if cap1 and i == m:
            la = math.radians(lerp(lat0, lat1, 1 - 0.45 / m))
        cr = math.cos(la)
        rings.append(zr(c.z + rad[2] * math.sin(la), rad[0] * cr, rad[1] * cr, c.y, w, c.x))
    return loft(rings, n, col, sq=sq, cap0=cap0, cap1=cap1, skip=skip, phase=phase, flip=flip)


def box(c, hx, hy, hz, w, col, ax=X, ay=Y):
    c = Vector(c)
    ax = Vector(ax).normalized()
    ay = (Vector(ay) - ax * Vector(ay).dot(ax)).normalized()
    az = ax.cross(ay)
    rings = [dict(c=c - az * hz, u=ax, v=ay, ru=hx * 1.2, rv=hy * 1.2, w=w),
             dict(c=c - az * hz * 0.5, u=ax, v=ay, ru=hx * 1.25, rv=hy * 1.25, w=w),
             dict(c=c + az * hz * 0.5, u=ax, v=ay, ru=hx * 1.25, rv=hy * 1.25, w=w),
             dict(c=c + az * hz, u=ax, v=ay, ru=hx * 1.2, rv=hy * 1.2, w=w)]
    return loft(rings, 8, col, sq=5.0)


def grid(rows, col, w, flip=False, closed=True):
    idx = [[addv(p, w) for p in row] for row in rows]
    n = len(rows[0])
    cf = col if callable(col) else (lambda i, j: col)
    for i in range(len(rows) - 1):
        for j in range(n if closed else n - 1):
            k = (j + 1) % n
            f = (idx[i][j], idx[i][k], idx[i + 1][k], idx[i + 1][j])
            F.append((tuple(reversed(f)) if flip else f, cf(i, j)))


def strap(pts, outs, half_w, thick, col, w, loop=True):
    n = len(pts)
    rings = []
    for k in range(n):
        t = (pts[(k + 1) % n] - pts[k - 1]) if loop else (pts[min(k + 1, n - 1)] - pts[max(k - 1, 0)])
        rings.append(R(pts[k], t, outs[k], thick, half_w, w))
    loft(rings, 4, col, sq=8.0, loop=loop, phase=math.radians(45))


def torus(c, axis, rad, r, col, w, n=14, m=6):
    c, axis = Vector(c), Vector(axis).normalized()
    e1, e2 = frame(axis, X if abs(axis.dot(X)) < 0.9 else Y)
    rings = []
    for k in range(n):
        a = 2 * math.pi * k / n
        o = e1 * math.cos(a) + e2 * math.sin(a)
        rings.append(R(c + o * rad, axis.cross(o), o, r, r, w))
    loft(rings, m, col, loop=True)


def mirror(v0, f0):
    def sw(n):
        if n.endswith("_L"):
            return n[:-2] + "_R"
        if n.endswith("_R"):
            return n[:-2] + "_L"
        return n
    off = len(V) - v0
    for i in range(v0, v0 + off):
        co = V[i]
        V.append(Vector((-co.x, co.y, co.z)))
        W.append({sw(k): x for k, x in W[i].items()})
    for fi, col in F[f0:]:
        F.append((tuple(reversed([i + off for i in fi])), col))


# ---------------------------------------------------------------------------------------------------- torso
WKEYS = [(47.0, "bip_pelvis"), (54.0, "bip_spine_0"), (61.5, "bip_spine_1"), (67.0, "bip_spine_2"), (72.0, "bip_spine_3")]


def wz(co):
    z = co.z if hasattr(co, "z") else co
    if z <= WKEYS[0][0]:
        return {WKEYS[0][1]: 1.0}
    for (z0, b0), (z1, b1) in zip(WKEYS, WKEYS[1:]):
        if z <= z1:
            t = (z - z0) / (z1 - z0)
            return {b0: 1 - t, b1: t}
    return {WKEYS[-1][1]: 1.0}


def torso_w(co):
    w = wz(co)
    if co.z > 65.0 and abs(co.x) > 6.0:
        t = min(1.0, (abs(co.x) - 6.0) / 3.5) * min(1.0, (co.z - 65.0) / 3.0) * 0.6
        w = {k: x * (1 - t) for k, x in w.items()}
        w["bip_collar_L" if co.x > 0 else "bip_collar_R"] = t
    return w


# rest-pose barrel: wide hips and belly, a chest that is narrow because bip_spine_1 is scaled 1.5 in the game
TK = [(40.5, 10.5, 8.6, 0.6), (44.0, 13.0, 10.6, 0.6), (48.0, 14.3, 11.8, 0.6), (52.0, 14.9, 12.4, 0.7), (55.0, 14.7, 12.3, 0.9),
      (58.2, 12.4, 10.4, 1.9), (61.5, 10.4, 8.7, 2.6), (64.5, 10.3, 8.5, 2.7), (67.5, 10.0, 8.1, 2.7), (70.0, 9.2, 7.3, 2.6),
      (72.2, 7.2, 6.0, 2.4), (73.8, 4.6, 4.4, 2.2)]
TSQ = 2.35


def tor(z):
    if z <= TK[0][0]:
        return TK[0][1:]
    for a, b in zip(TK, TK[1:]):
        if z <= b[0]:
            t = (z - a[0]) / (b[0] - a[0])
            t = t * t * (3 - 2 * t)
            return tuple(lerp(a[i], b[i], t) for i in (1, 2, 3))
    return TK[-1][1:]


def tor_pt(a, z, grow=0.0):
    rx, ry, cy = tor(z)
    px, py = sup(a, TSQ)
    return Vector(((rx + grow) * px, cy + (ry + grow) * py, z))


def tor_out(a, z):
    p = tor_pt(a, z)
    rx, ry, cy = tor(z)
    return (p - Vector((0, cy, z))).normalized()


# ---------------------------------------------------------------------------------------------------- head helpers
HEAD = Vector((0.0, -0.5, 84.0))
HR = (10.2, 9.4, 9.2)
HW = {"bip_head": 1}


def hs(x, z, grow=0.0):
    """Front point of the head at (x, z)."""
    rx, ry, rz = HR[0] + grow, HR[1] + grow, HR[2] + grow
    s = 1 - (x / rx) ** 2 - ((z - HEAD.z) / rz) ** 2
    return Vector((x, HEAD.y - ry * math.sqrt(max(0.0, s)), z))


def hring(a, z, grow=0.0):
    s = math.sqrt(max(0.0, 1 - ((z - HEAD.z) / (HR[2] + grow)) ** 2))
    return Vector(((HR[0] + grow) * s * math.cos(a), HEAD.y + (HR[1] + grow) * s * math.sin(a), z))


# ==================================================================================================== left side
v_start, f_start = len(V), len(F)
HIP, KNEE, ANK = bone("bip_hip_L"), bone("bip_knee_L"), bone("bip_foot_L")
SH, EL, WR = bone("bip_upperArm_L"), bone("bip_lowerArm_L"), bone("bip_hand_L")
th = lambda t: lerp(HIP, KNEE, t)
sh = lambda t: lerp(KNEE, ANK, t)
ua = lambda t: lerp(SH, EL, t)
fa = lambda t: lerp(EL, WR, t)
PH, HL, KL, FL, TL = "bip_pelvis", "bip_hip_L", "bip_knee_L", "bip_foot_L", "bip_toe_L"

# baggy breeches
tube([th(-0.06), th(0.10), th(0.35), th(0.62), th(0.86), th(1.0), sh(0.10), sh(0.16)],
     [5.8, 6.9, 7.4, 7.5, 7.0, 6.4, 5.6, 4.6],
     [{PH: .65, HL: .35}, {PH: .25, HL: .75}, {HL: 1}, {HL: 1}, {HL: .8, KL: .2}, {HL: .5, KL: .5}, {HL: .15, KL: .85}, {KL: 1}],
     14, lambda i, j: "team_dark" if j in (0, 13) else "navy", hint=X)
# bucket-top boot
tube([sh(0.07), sh(0.09), sh(0.16), sh(0.30), sh(0.36), sh(0.40), sh(0.62), sh(0.86), sh(1.03)],
     [5.4, 7.3, 7.9, 7.7, 6.9, 5.5, 5.2, 5.1, 5.4],
     [{KL: 1}] * 7 + [{KL: .75, FL: .25}, {KL: .35, FL: .65}],
     14, lambda i, j: "leather" if i < 1 else ("leather_dk" if i < 5 else "leather"), hint=X)
torus(sh(0.72), sh(1) - sh(0), 5.35, 0.75, "leather_dk", {KL: 1}, n=14, m=6)          # ankle strap
box(sh(0.72) + Vector((0, -5.6, 0)), 1.5, 0.6, 1.3, {KL: 1}, "gold")                    # its buckle
foot = []
for y, cx, hw, h, w in [(8.4, 8.0, 2.6, 5.0, {FL: 1}), (7.0, 8.0, 4.5, 8.2, {FL: 1}), (3.0, 8.1, 5.1, 8.8, {FL: 1}),
                        (-1.0, 8.6, 5.4, 7.6, {FL: .7, TL: .3}), (-4.6, 9.3, 5.7, 6.8, {FL: .2, TL: .8}),
                        (-8.4, 9.9, 5.4, 6.4, {TL: 1}), (-11.0, 10.2, 4.0, 5.0, {TL: 1}), (-11.9, 10.3, 2.2, 3.0, {TL: 1})]:
    foot.append(dict(c=Vector((cx, y, h / 2)), u=X.copy(), v=Z.copy(), ru=hw, rv=h / 2, w=w))
loft(foot, 14, lambda i, j: "leather_dk" if 8 <= j <= 13 else "leather", sq=2.8)

# coat sleeve, big turned-back cuff with gold trim, shirt frill, bare fist
CL, UL, LL, HDL = "bip_collar_L", "bip_upperArm_L", "bip_lowerArm_L", "bip_hand_L"
tube([ua(-0.16), ua(-0.02), ua(0.22), ua(0.6), ua(0.86), ua(1.0), fa(0.16), fa(0.48)],
     [3.6, 5.2, 5.4, 5.0, 4.8, 4.7, 4.8, 4.9],
     [{CL: .7, UL: .3}, {CL: .3, UL: .7}, {UL: 1}, {UL: 1}, {UL: .8, LL: .2}, {UL: .5, LL: .5}, {UL: .15, LL: .85}, {LL: 1}],
     14, "team", hint=Y)
tube([fa(0.40), fa(0.41), fa(0.47), fa(0.53), fa(0.74), fa(0.80), fa(0.82)],
     [4.9, 6.6, 7.0, 7.1, 7.3, 6.9, 5.0],
     {LL: 1}, 14, lambda i, j: "gold" if i in (1, 2) else "team_dark", hint=Y)
for k in range(3):                                                                       # cuff buttons
    p = fa(0.52 + 0.1 * k)
    ellipsoid(p + Vector((2.0, -6.9, 0.0)).normalized() * 7.2, (0.95, 0.95, 0.95), {LL: 1}, "gold", n=8, m=4)
tube([fa(0.78), fa(0.84), fa(0.92), fa(1.0)], [5.4, 5.6, 4.9, 4.2], [{LL: 1}, {LL: 1}, {LL: .8, HDL: .2}, {LL: .5, HDL: .5}],
     12, "cream", hint=Y)
fingers = ["index", "middle", "ring", "pinky"]
kn = sum((bone("bip_%s_0_L" % f) for f in fingers), Vector()) / 4
tube([lerp(WR, kn, -0.12), lerp(WR, kn, 0.25), lerp(WR, kn, 0.7), lerp(WR, kn, 1.05)], [(2.6, 3.4), (3.0, 4.4), (3.0, 4.6), (2.5, 4.3)],
     {HDL: 1}, 10, "skin", hint=X, sq=2.6)
for f in fingers + ["thumb"]:
    p = [bone("bip_%s_%d_L" % (f, i)) for i in range(3)]
    tip = p[2] + (p[2] - p[1]).normalized() * (2.3 if f != "thumb" else 2.0)
    r = 1.55 if f != "thumb" else 1.75
    b = ["bip_%s_%d_L" % (f, i) for i in range(3)]
    start = lerp(p[0], WR, 0.3) if f == "thumb" else lerp(p[0], kn, 0.2)
    tube([start, lerp(p[0], p[1], .5), p[1], lerp(p[1], p[2], .5), p[2], lerp(p[2], tip, .7), tip],
         [r, r * 1.05, r, r, r * .97, r * .9, r * .45],
         [{HDL: .5, b[0]: .5}, {b[0]: 1}, {b[0]: .5, b[1]: .5}, {b[1]: 1}, {b[1]: .5, b[2]: .5}, {b[2]: 1}, {b[2]: 1}],
         8, "skin", hint=X)

# head, left half: ear, beard clumps along the jaw, sideburn, moustache half
ellipsoid((9.9, 1.0, 83.2), (1.5, 2.2, 2.6), HW, "skin", n=10, m=6)
for a_deg, z, r in ((352, 81.6, 2.5), (338, 79.0, 2.9), (318, 76.9, 3.1), (299, 75.0, 3.0), (283, 74.2, 2.9),
                    (345, 76.4, 2.8), (322, 74.2, 2.9), (300, 72.9, 2.7)):
    p = hring(math.radians(a_deg), z)
    ellipsoid(p, (r, r, r), HW, "hair", n=10, m=6)
m_pts = [hs(0.3, 79.9) + Vector((0, -1.3, 0)), hs(2.8, 79.6) + Vector((0, -1.5, 0)), hs(5.4, 78.9) + Vector((0, -1.3, 0)),
         hs(7.2, 77.6) + Vector((0, -1.0, 0)), hs(7.9, 76.0) + Vector((0, -0.7, 0))]
tube(m_pts, [1.5, 1.95, 1.75, 1.2, 0.45], HW, 9, "hair", hint=Z, cap0=False)

mirror(v_start, f_start)

# ==================================================================================================== centre
NT = 24
PHT = math.radians(7.5)                    # a face sits on the centre line
FRONT = (16, 17, 18)                       # the open coat front


def body_col(i, j):
    z = lerp(40.5, 73.8, (i + 0.5) / 26)
    if z < 50.0:
        return "navy"
    if j in FRONT and z < 71.0:
        return "team_dark" if int((z - 50.0) / 1.9) % 2 else "cream"      # striped jersey
    return "team"


loft([zr(z, *tor(z), torso_w) for z in [lerp(40.5, 73.8, i / 26) for i in range(27)]], NT, body_col, phase=PHT, sq=TSQ)
# lapels with gold edge, buttons
for sgn in (-1, 1):
    a = math.radians(270 + sgn * 24.5)
    zs = [53.5 + 2.2 * k for k in range(9)]
    tube([tor_pt(a, z, 0.55) for z in zs], [(1.5, 0.75)] * 9, torso_w, 6, "gold", hint=X)
    a2 = math.radians(270 + sgn * 37)
    tube([tor_pt(a2, z, 0.5) for z in zs[:-1]], [(1.6, 0.6)] * 8, torso_w, 6, "team_dark", hint=X)
    for z in (57.0, 62.0, 67.0):
        ellipsoid(tor_pt(math.radians(270 + sgn * 37), z, 1.1), (1.05, 1.05, 1.05), torso_w, "gold", n=8, m=4)
# tall coat collar
loft([zr(71.2, 8.2, 7.4, 2.5, torso_w), zr(73.0, 9.0, 8.2, 2.7, torso_w), zr(76.4, 9.6, 8.8, 3.1, {"bip_spine_3": 1}),
      zr(76.8, 9.0, 8.2, 3.1, {"bip_spine_3": 1}), zr(73.4, 8.2, 7.5, 2.7, torso_w)],
     NT, lambda i, j: "gold" if i == 2 else "team_dark", phase=PHT, cap0=False, cap1=False,
     skip=lambda i, j: j in (15, 16, 17, 18, 19))
tube([(0, 2.2, 72.0), (0, 1.2, 77.5)], [4.4, 4.4], {"bip_neck": 1}, 10, "skin", hint=X, cap0=False, cap1=False)

# coat skirt (open in front, dark lining), wide belt with a big buckle
def PW(co):
    t = max(0.0, min(1.0, (44.0 - co.z) / 12.0)) * 0.5 * min(1.0, abs(co.x) / 5.0)
    return {"bip_pelvis": 1 - t, ("bip_hip_L" if co.x > 0 else "bip_hip_R"): t} if t > 0.01 else {"bip_pelvis": 1}
SK = [(51.5, 15.3, 12.8), (47.0, 16.3, 13.9), (42.0, 17.4, 15.1), (37.0, 18.4, 16.2), (32.0, 19.2, 17.0), (30.8, 19.3, 17.1)]
sk_skip = lambda i, j: j in (15, 16, 17, 18, 19)
loft([zr(z, rx, ry, 0.7, PW) for z, rx, ry in reversed(SK)], NT, lambda i, j: "gold" if i == 0 else "team", phase=PHT, sq=TSQ,
     cap0=False, cap1=False, skip=sk_skip)
loft([zr(z, rx - 0.5, ry - 0.5, 0.7, PW) for z, rx, ry in reversed(SK)], NT, "team_dark", phase=PHT, sq=TSQ,
     cap0=False, cap1=False, skip=sk_skip, flip=True)
loft([zr(z, tor(z)[0] + g, tor(z)[1] + g, tor(z)[2], wz) for z, g in ((49.4, 0.2), (49.7, 1.0), (51.6, 1.15), (53.5, 1.0), (53.8, 0.2))],
     NT, "leather_dk", phase=PHT, sq=TSQ, cap0=False, cap1=False)
bk = tor_pt(math.radians(270), 51.6, 1.3)
box(bk, 3.3, 0.75, 2.9, wz, "gold")
box(bk + Vector((0, -0.35, 0)), 2.0, 0.8, 1.6, wz, "leather_dk")

# bandolier from the left shoulder to the right hip, three round bombs on the chest
NB = 32
b_pts, b_out = [], []
for k in range(NB):
    a = 2 * math.pi * k / NB
    z = 63.2 + 9.2 * math.cos(a)
    b_pts.append(tor_pt(a, z, 0.75))
    b_out.append(tor_out(a, z))
strap(b_pts, b_out, 1.7, 0.6, "leather", torso_w)


def bomb(c, r, w, up=Z, fuse=True):
    c, up = Vector(c), Vector(up).normalized()
    ellipsoid(c, (r, r, r), w, "bomb", n=12, m=8)
    ellipsoid(c + Vector((-0.35, -0.5, 0.45)) * r, (r * 0.22, r * 0.16, r * 0.22), w, "white", n=6, m=3)   # shine
    tube([c + up * r * 0.8, c + up * r * 1.25], [r * 0.42, r * 0.42], w, 8, "brass", hint=X)
    if fuse:
        side = frame(up, X)[0]
        tube([c + up * r * 1.2, c + up * r * 1.7 + side * r * 0.25, c + up * r * 2.0 + side * r * 0.7], [r * 0.14] * 3, w, 5, "rope", hint=Y)
        ellipsoid(c + up * r * 2.05 + side * r * 0.78, (r * 0.3,) * 3, w, "ember", n=6, m=4)


for a_deg in (232, 262, 292):
    a = math.radians(a_deg)
    z = 63.2 + 9.2 * math.cos(a)
    bomb(tor_pt(a, z, 2.6), 2.25, torso_w, up=(0.25, -0.25, 1))

# powder keg across the back
BW = {"bip_spine_2": 1}
KC = Vector((0.0, 15.4, 64.6))
kx = [-6.6, -6.2, -4.4, -2.2, 0.0, 2.2, 4.4, 6.2, 6.6]
kr = [3.6, 4.3, 4.9, 5.2, 5.3, 5.2, 4.9, 4.3, 3.6]
loft([R(KC + X * x, X, Z, r, r, BW) for x, r in zip(kx, kr)], 14,
     lambda i, j: "iron" if i in (1, 6) else ("leather_dk" if j % 2 else "wood"), capcol="wood")
tube([KC + Vector((6.5, 0, 2.0)), KC + Vector((8.2, 0.4, 4.0)), KC + Vector((8.0, 1.0, 6.4)), KC + Vector((9.4, 1.2, 8.0))], [0.42] * 4, BW, 5, "rope", hint=Y)
ellipsoid(KC + Vector((9.6, 1.2, 8.3)), (0.9, 0.9, 0.9), BW, "ember", n=6, m=4)
for x in (-3.3, 3.3):                                                                    # keg straps
    torus(KC + X * x, X, 5.45, 0.5, "leather", BW, n=14, m=4)

# ---------------------------------------------------------------------------------------------------- head
ellipsoid(HEAD, HR, HW, "skin", n=24, m=14)
nose = hs(0, 81.5)
ellipsoid(nose + Vector((0, -1.5, 0)), (2.6, 2.5, 2.3), HW, "skin_dk", n=12, m=7)
# one good eye (the character's left), eyepatch on the right
ec = hs(4.0, 84.3)
ellipsoid(ec + Vector((0, 0.15, 0)), (2.9, 1.1, 3.0), HW, "white", n=14, m=8)
ellipsoid(ec + Vector((-0.5, -0.75, -0.2)), (1.4, 0.5, 1.6), HW, "patch", n=10, m=6)
ellipsoid(ec + Vector((0.0, -1.15, 0.5)), (0.5, 0.22, 0.55), HW, "white", n=6, m=4)
pc = hs(-4.0, 84.3)
ellipsoid(pc + Vector((0, 0.1, 0)), (3.3, 1.0, 3.2), HW, "patch", n=14, m=8, sq=2.6)
NS = 28
s_pts, s_out = [], []
for k in range(NS):
    a = 2 * math.pi * k / NS
    x = (HR[0] + 0.3) * math.cos(a)
    z = 86.0 + 0.42 * x
    p = hring(a, z, 0.3)
    s_pts.append(p)
    s_out.append((p - Vector((0, HEAD.y, z))).normalized())
strap(s_pts, s_out, 0.6, 0.32, "patch", HW)
# angry brows
box(hs(4.2, 88.0) + Vector((0, -0.7, 0)), 3.2, 0.8, 0.95, HW, "hair", ax=(1, 0.2, 0.34), ay=Y)
box(hs(-4.2, 88.1) + Vector((0, -0.7, 0)), 3.2, 0.8, 0.95, HW, "hair", ax=(1, -0.2, -0.34), ay=Y)
# grin: dark mouth, a row of teeth, one gold tooth
mc = hs(0, 77.6)
ellipsoid(mc + Vector((0, -0.5, 0)), (4.6, 1.2, 2.2), HW, "mouth", n=12, m=6)
box(mc + Vector((0, -1.35, 0.7)), 3.7, 0.55, 0.95, HW, "white")
box(mc + Vector((2.0, -1.55, 0.7)), 0.8, 0.55, 1.0, HW, "gold")
ellipsoid(hring(math.radians(270), 73.2), (3.2, 2.8, 2.6), HW, "hair", n=12, m=7)        # chin clump
torus((10.9, 1.2, 80.0), Y, 1.75, 0.42, "gold", HW, n=12, m=6)                             # earring, left ear

# bandana under the hat, knot and tails behind the right ear
BLAT = math.degrees(math.asin((87.2 - HEAD.z) / (HR[2] + 0.55)))
ellipsoid(HEAD, (HR[0] + 0.55, HR[1] + 0.55, HR[2] + 0.55), HW, "team", n=24, m=8, lat0=BLAT)
loft([zr(z, *(lambda p: (p.x, hring(math.pi / 2, z, g).y - HEAD.y))(hring(0, z, g)), HEAD.y, HW) for z, g in ((86.5, 0.3), (86.7, 1.05), (88.0, 1.1), (88.3, 0.5))],
     24, "team_dark", cap0=False, cap1=False)
kp = hring(math.radians(140), 87.4, 1.2)
ellipsoid(kp, (1.9, 1.9, 1.7), HW, "team", n=10, m=6)
tube([kp, kp + Vector((-1.5, 2.6, -3.0)), kp + Vector((-2.2, 3.8, -7.0))], [(1.5, 0.6), (2.0, 0.5), (2.4, 0.3)], HW, 8, "team", hint=X, sq=2.6)
tube([kp, kp + Vector((1.2, 3.2, -2.4)), kp + Vector((1.8, 5.0, -5.6))], [(1.4, 0.6), (1.8, 0.5), (2.1, 0.3)], HW, 8, "team_dark", hint=X, sq=2.6)

# tricorn hat: crown, gold band, brim folded up on three sides, gold edge
HZ = 90.4
HC = Vector((0.0, -0.3, HZ))
ellipsoid(HC, (8.3, 7.8, 6.4), HW, lambda i, j: "gold" if i == 0 else "hat", n=24, m=8, lat0=0.0)
NH, KH = 36, 5
A_FRONT = math.radians(270)


def brim(a, k, under):
    c = ((1 + math.cos(3 * (a - A_FRONT))) / 2) ** 1.3          # 1 at the three points, 0 on the folded sides
    t = k / KH
    rad = lerp(7.6, lerp(11.4, 17.2, c), t)
    up = (1 - c) * 6.2 * t ** 1.25 + 0.8 * t * t
    p = HC + Vector((rad * math.cos(a), rad * 0.95 * math.sin(a), up))
    if under:
        n_in = Vector((-math.cos(a), -math.sin(a), 0)) * (1 - c) * 0.6
        p += Vector((0, 0, -0.85)) * (0.35 + 0.65 * c) + n_in * t
    return p


rows = [[brim(2 * math.pi * j / NH, k, False) for j in range(NH)] for k in range(KH + 1)]
rows += [[brim(2 * math.pi * j / NH, k, True) for j in range(NH)] for k in range(KH, -1, -1)]
grid(rows, lambda i, j: "gold" if i in (KH - 1, KH, KH + 1) else "hat", HW, flip=True)
# feather on the left side of the hat
fb = HC + Vector((7.4, 2.0, 2.2))
tube([fb, fb + Vector((2.2, 2.4, 4.6)), fb + Vector((2.6, 6.0, 8.4)), fb + Vector((1.2, 10.4, 9.8)), fb + Vector((-0.6, 13.6, 8.6))],
     [(0.5, 0.5), (0.6, 2.3), (0.55, 2.9), (0.5, 2.4), (0.25, 0.7)], HW, 8, lambda i, j: "team" if i >= 3 else "cream", hint=X, sq=2.4)
# brass bomb badge on the crown's front
ellipsoid(HC + Vector((0, -7.6, 3.2)), (1.7, 0.7, 1.7), HW, "gold", n=10, m=5)

HEAD_SCALE, HEAD_PIVOT = 1.36, Vector((0.0, 0.8, 75.4))
for i in range(len(V)):
    if len(W[i]) == 1 and "bip_head" in W[i]:
        V[i] = HEAD_PIVOT + (V[i] - HEAD_PIVOT) * HEAD_SCALE

# ---------------------------------------------------------------------------------------------------- mesh
for nm in ("PirateDemo", "FireworkDemo"):
    old = bpy.data.objects.get(nm)
    if old:
        me_old = old.data
        bpy.data.objects.remove(old)
        bpy.data.meshes.remove(me_old)
used = sorted({i for f, _ in F for i in f})
remap = {o: n for n, o in enumerate(used)}
me = bpy.data.meshes.new("PirateDemo")
me.from_pydata([V[i] for i in used], [], [tuple(remap[i] for i in f) for f, _ in F])
me.update()
for p in me.polygons:
    p.use_smooth = True
ob = bpy.data.objects.new("PirateDemo", me)
bpy.context.scene.collection.objects.link(ob)
ob.parent = ARM
for n in sorted({k for i in used for k in W[i]}):
    ob.vertex_groups.new(name=n)
for n_i, o_i in enumerate(used):
    for k, x in W[o_i].items():
        if x > 0.001:
            ob.vertex_groups[k].add([n_i], x, 'REPLACE')
ca = me.color_attributes.new("Col", 'BYTE_COLOR', 'CORNER')
TEAM = bpy.app.driver_namespace.get("pirate_team", 1)
for p, (_, col) in zip(me.polygons, F):
    pal = PALETTE[CI[col]]
    c = pal[TEAM] or pal[1]
    for li in p.loop_indices:
        ca.data[li].color_srgb = (c[0], c[1], c[2], 1.0)

result = {"verts": len(used), "faces": len(F), "bones": len(ob.vertex_groups)}
