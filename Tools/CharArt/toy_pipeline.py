"""Toy (chibi) character pipeline: the Blender side.

Run inside Blender (Scripting tab, or the Blender MCP) on a file from Tools/CharArt/blend/. The functions here were
used interactively for the Demoman (Nov 2026); the Python state of a Blender session is not saved in the .blend, so
they live in this file. The per-character numbers (scale table, hip offset, arm rotation, which mesh islands are the
eye / brow) are in the text block "<Name>_notes" inside the .blend.

Steps for a character (see Tools/CharArt/blend/ToyDemoman.blend for a finished one):
 1. Import the Meshy GLB, weld it (remove doubles 1e-5), scale it to 188 units tall with the feet on z = 0 -> "<X>Low".
    Keep the unwelded import, same transform, as "<X>Ref": custom normals are transferred from it.
 2. Pick the scale table S (uniform local bone scales of the OLD skeleton "ToyArm": pelvis, spine_0, hips, collars,
    upper arms, hands, head) so the posed old skeleton fits the new mesh, a rotation of the upper arms towards the
    model's wrists (ROT), and an offset of the hips (armature space) so the leg bones run through the middle of the legs.
 3. Build a bind armature from the posed positions (23 deform bones), automatic weights, then make them clean:
    every loose mesh island rigid on one bone (head, face parts, pouches), hands (skin colour) on the hand, cuffs on
    the forearm, legs in pieces by height (foot / calf / thigh / pelvis), chest on the spine (collars only beyond the
    shoulders), the elbow blend inside the sleeve, accessories with the weights of the body under them.
 4. Split into parts (body, and one part per bodyElement of the old prefab), transfer normals from "<X>Ref".
 5. export_toy(...) -> Tools/CharArt/export/<Name>.json, then Unity: Tools/Characters/Build Toy <Name>.

Unity applies the same S / offsets every frame (ToyProportions), with the old bind poses: the vertices are "unposed"
here into the old mesh's rest space.
"""
import json

import bpy
import numpy as np
from mathutils import Matrix, Vector


def solve_pose(arm, S, ROT=None):
    """Posed armature-space matrices of every bone. S: bone -> uniform local scale; ROT: bone -> armature-space
    Quaternion applied at that bone."""
    ROT = ROT or {}
    out = {}

    def rec(b, Mp, Rp_inv):
        R = b.matrix_local
        L = Matrix.Scale(S.get(b.name, 1.0), 4)
        if b.name in ROT:
            q = ROT[b.name].to_matrix().to_4x4()
            R3 = R.to_3x3().to_4x4()
            L = (R3.inverted() @ q @ R3) @ L
        M = Mp @ (Rp_inv @ R) @ L
        out[b.name] = M
        Ri = R.inverted()
        for c in b.children:
            rec(c, M, Ri)

    for b in arm.data.bones:
        if b.parent is None:
            rec(b, Matrix.Identity(4), Matrix.Identity(4))
    return out


def pose_with_offsets(arm, S, ROT, offsets):
    """solve_pose, then the bones in `offsets` (name -> armature-space Vector) and everything under them are moved."""
    P = solve_pose(arm, S, ROT)

    def desc(b):
        o = [b]
        for c in b.children:
            o += desc(c)
        return o

    for name, d in offsets.items():
        for b in desc(arm.data.bones[name]):
            P[b.name] = Matrix.Translation(Vector(d)) @ P[b.name]
    return P


def smoothstep(x, a, b):
    t = min(1.0, max(0.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def leg_weights(z, x, side, k0, k1, h0, h1, ankle=(7, 11), crotch=(44, 50)):
    """Weights of a leg vertex by height: foot below the ankle band, calf up to the knee band (k0..k1), thigh up to
    the hip band (h0..h1), pelvis above; above the crotch the inside of the thigh goes to the pelvis."""
    sx = 1 - (1 - smoothstep(abs(x), 2, 5)) * smoothstep(z, *crotch)
    foot = 1 - smoothstep(z, *ankle)
    calf = smoothstep(z, *ankle) * (1 - smoothstep(z, k0, k1))
    thigh = smoothstep(z, k0, k1) * (1 - smoothstep(z, h0, h1))
    pel = smoothstep(z, h0, h1)
    return {"bip_foot_" + side: foot * sx, "bip_knee_" + side: calf * sx, "bip_hip_" + side: thigh * sx,
            "bip_pelvis": pel * sx + (1 - sx)}


def bend_fingers(co, hand, lower, back_of_hand, t0=12.5, radius=6.5, max_angle=2.0, mid=-1.0, thumb_side=-5.6,
                 width_hint=(0.21, 0.98, -0.04)):
    """Bends the fused fingers of an open mitt into a fist. co: (n, 3) vertices of ONE hand (armature space);
    hand / lower: joint positions; returns the new positions and the bend angle per vertex (rotate the normals by it
    about the hand's width axis). Vertices on the thumb's side (u < thumb_side) are left alone."""
    a = (hand - lower) / np.linalg.norm(hand - lower)
    n = np.array(width_hint, dtype=float)
    n -= a * (n @ a)
    n /= np.linalg.norm(n)
    b = np.array(back_of_hand, dtype=float)
    out = co.copy()
    angle = np.zeros(len(co))
    for i, p in enumerate(co - hand):
        t, u, w = p @ a, p @ n, p @ b
        if t <= t0 or u < thumb_side:
            continue
        s = t - t0
        th = min(s / radius, max_angle)
        extra = max(0.0, s - radius * max_angle)
        d = max(0.8 - radius, w - mid)
        t2 = t0 + (radius + d) * np.sin(th) + extra * np.cos(th)
        w2 = (mid - radius) + (radius + d) * np.cos(th) - extra * np.sin(th)
        out[i] = hand + a * t2 + n * u + b * w2
        angle[i] = th
    return out, angle


def export_toy(arm, P, S, offsets, parts, outfile, shapes=None, hand_axes=None):
    """parts: [(object name, part name)]. shapes: {part name: {shape name: {vertex index: armature-space delta}}}.
    Writes the json ToyCharacterBuilder reads: vertices in the old mesh's rest space, weights by bone name."""
    skinM = {b.name: np.array(P[b.name] @ b.matrix_local.inverted()) for b in arm.data.bones}
    data = {"scales": S, "offsets": {k: list(v) for k, v in offsets.items()},
            "bones_rest": {b.name: [round(v, 5) for v in b.head_local] for b in arm.data.bones}, "parts": []}
    if hand_axes:
        data["hand_axes"] = hand_axes
    stats = {}
    for obj_name, part_name in parts:
        ob = bpy.data.objects[obj_name]
        me = ob.data
        me.update()
        cn = np.array([c.vector[:] for c in me.corner_normals])
        gnames = [g.name for g in ob.vertex_groups]
        nv = len(me.vertices)
        co = np.array([v.co[:] for v in me.vertices], dtype=np.float64)
        W = []
        rest = np.zeros((nv, 3))
        BT = np.zeros((nv, 3, 3))
        Binv = np.zeros((nv, 3, 3))
        for i, v in enumerate(me.vertices):
            gs = sorted(((g.weight, gnames[g.group]) for g in v.groups if g.weight > 1e-4), reverse=True)[:4]
            if not gs:
                gs = [(1.0, "bip_pelvis")]
            t = sum(w for w, _ in gs)
            gs = [(w / t, n) for w, n in gs]
            B = sum(w * skinM[n] for w, n in gs)
            Bi = np.linalg.inv(B)
            rest[i] = (Bi @ np.append(co[i], 1.0))[:3]
            BT[i] = B[:3, :3].T
            Binv[i] = Bi[:3, :3]
            W.append(gs)
        uvl = me.uv_layers.active.data
        key, verts, tris, nout = {}, [], [], []
        for p in me.polygons:
            idx = []
            for li in p.loop_indices:
                vi = me.loops[li].vertex_index
                uv = uvl[li].uv
                nn = BT[vi] @ cn[li]
                ln = np.linalg.norm(nn)
                nn = nn / ln if ln > 1e-9 else np.array([0, 0, 1.0])
                k = (vi, round(uv.x, 5), round(uv.y, 5), round(float(nn[0]), 2), round(float(nn[1]), 2), round(float(nn[2]), 2))
                j = key.get(k)
                if j is None:
                    j = len(verts)
                    key[k] = j
                    verts.append(k)
                    nout.append(nn)
                idx.append(j)
            tris.append(idx)
        bones = sorted({n for gs in W for _, n in gs})
        bi = {n: i for i, n in enumerate(bones)}
        part = {"name": part_name, "bones": bones,
                "pos": [round(float(x), 4) for k in verts for x in rest[k[0]]],
                "nrm": [round(float(x), 4) for nn in nout for x in nn],
                "uv": [x for k in verts for x in (k[1], k[2])],
                "tri": [i for t in tris for i in t],
                "bw": [[(bi[n], round(w, 4)) for w, n in W[k[0]]] for k in verts]}
        for shape, deltas in (shapes or {}).get(part_name, {}).items():
            idx, dl = [], []
            for j, k in enumerate(verts):
                d = deltas.get(k[0])
                if d is None:
                    continue
                idx.append(j)
                dl += [round(float(x), 5) for x in Binv[k[0]] @ np.array(d)]
            part.setdefault("shapes", {})[shape] = {"idx": idx, "d": dl}
        data["parts"].append(part)
        stats[part_name] = (len(verts), len(tris))
    with open(outfile, "w") as f:
        json.dump(data, f)
    return stats
