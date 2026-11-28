"""Toy characters: retarget the old clips onto a skeleton with the model's own proportions (Nov 2026).

Run inside Blender on Tools/CharArt/blend/Toy<Name>.blend after toy_pipeline has produced the posed matrices:
    g = {};  exec(open(".../toy_retarget.py").read(), g)
    frames, stats = g["bake_clip"](anim_armature, action, rig, rest, gun_scale, two_handed=True, hold=...)
    g["write_clips"](path_without_extension, bone_names, {clip name: frames})

The new rig ("ToyRig") has every bone where it sits on the model (the posed matrices P of toy_pipeline, without
scale). Its bone frames correspond one to one to the old skeleton's, so:
  - every bone takes the old bone's rotation in world space (not the rotation relative to rest);
  - positions come from the new rig's own bone offsets (forward kinematics), the clip's animated offsets scaled;
  - the gun keeps the clip's orientation and sits with its handle at the right hand's knuckles: the place the old
    right hand's knuckles had in the gun's space, scaled with the gun;
  - while the clip holds the gun with both hands, the left hand goes to the place on the gun the old left hand's
    knuckles had, by two-bone IK with the clip's elbow as the pole; when the short arm cannot reach, the right
    arm brings the gun closer (level), by IK too. "Holding" = the old left knuckles no farther from the grip
    bone than about 1.25 x `hold` (their distance on the weapon's stand clip); it fades out up to 1.6 x.

The clips of the game's FBX files pose the skeleton in the armature object's own (rotated) space; the object's
world matrix W brings a pose upright. Baking works upright; the result is written back in the raw space
(inverse W), which is what Unity plays under the prefab's rotated skeleton node.
"""
import json

import bpy
import numpy as np
from mathutils import Matrix

FINGERS_NEW = ("bip_index_0_%s", "bip_middle_0_%s", "bip_ring_0_%s")
FINGERS_OLD = ("bip_index_0_%s", "bip_middle_0_%s", "bip_ring_0_%s", "bip_pinky_0_%s")
UP = np.array([0, 0, 1.0])


def _np(m):
    return np.array(m, dtype=np.float64)


def _from_to(a, b):
    a = a / (np.linalg.norm(a) + 1e-12)
    b = b / (np.linalg.norm(b) + 1e-12)
    v = np.cross(a, b)
    c = float(a @ b)
    s = np.linalg.norm(v)
    if s < 1e-9:
        return np.eye(3)
    k = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]])
    return np.eye(3) + k + k @ k * ((1 - c) / (s * s))


def _ik(S, E0, T, a, b):
    """Elbow and hand positions of a two-bone arm from S reaching for T, the elbow kept on E0's side."""
    d = np.linalg.norm(T - S)
    dirn = (T - S) / (d + 1e-12)
    d = min(max(d, abs(a - b) + 1e-4), a + b - 1e-4)
    pole = E0 - S
    pole = pole - dirn * (pole @ dirn)
    if np.linalg.norm(pole) < 1e-6:
        pole = np.cross(dirn, [0, 0, 1.0])
    pole /= np.linalg.norm(pole)
    cos_a = (a * a + d * d - b * b) / (2 * a * d)
    sin_a = np.sqrt(max(0.0, 1 - cos_a * cos_a))
    return S + a * (cos_a * dirn + sin_a * pole), S + d * dirn


def _smooth(x, a, b):
    t = min(1.0, max(0.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def set_action(ob, action):
    ob.animation_data_create()
    ob.animation_data.action = action
    if hasattr(ob.animation_data, "action_slot") and getattr(action, "slots", None):
        try:
            ob.animation_data.action_slot = action.slots[0]
        except Exception:
            pass


def export_bones(rig):
    return [b.name for b in rig.data.bones if not b.name.endswith("_end")]


def hold_distance(anim, action, frame=None):
    """Distance of the old left hand's knuckles from the grip bone on a frame of a two-handed stand clip."""
    set_action(anim, action)
    bpy.context.scene.frame_set(int(round(action.frame_range[0])) if frame is None else frame)
    k = np.mean([_np(anim.pose.bones[x % "L"].matrix)[:3, 3] for x in FINGERS_OLD], axis=0)
    return float(np.linalg.norm(k - _np(anim.pose.bones["weapon_bone"].matrix)[:3, 3]))


POSTURE = {"pelvis_level": 0.8, "waist_relax": 0.7, "upper_back_keep": 0.75, "neck_relax": 0.6, "head_upright": 0.8}


def _slerp(a, b, t):
    """Rotation matrix between a and b."""
    qa = Matrix(a.tolist()).to_quaternion()
    qb = Matrix(b.tolist()).to_quaternion()
    return _np(qa.slerp(qb, t).to_matrix())


def torso_profile(parts, z0=44.0, z1=100.0, dz=4.0, nt=36, bones=("bip_pelvis", "bip_spine_0", "bip_spine_1", "bip_spine_2", "bip_spine_3")):
    """The torso (with what it wears: bandolier, vest) as a table of radii around the vertical line through its
    middle, by height and direction, in the rest pose. parts: mesh objects bound to the rig. Arms are kept out of it
    by bake_clip(torso=...): the toy bodies are wider than their shoulders, and a clip made for a thin body hangs the
    arms inside the belly."""
    pts = []
    for ob in parts:
        gi = {g.index for g in ob.vertex_groups if g.name in bones}
        for v in ob.data.vertices:
            if z0 < v.co.z < z1 and sum(g.weight for g in v.groups if g.group in gi) > 0.7:
                pts.append(v.co[:])
    pts = np.array(pts)
    cy = float(np.median(pts[:, 1]))
    nz = int(round((z1 - z0) / dz))
    prof = np.zeros((nz, nt))
    zi = np.clip(((pts[:, 2] - z0) / dz).astype(int), 0, nz - 1)
    ti = ((np.arctan2(pts[:, 1] - cy, pts[:, 0]) + np.pi) / (2 * np.pi) * nt).astype(int) % nt
    np.maximum.at(prof, (zi, ti), np.hypot(pts[:, 0], pts[:, 1] - cy))
    for _ in range(nz + nt):  # holes take their neighbours' mean
        holes = np.argwhere(prof == 0)
        if not len(holes):
            break
        new = prof.copy()
        for a, b in holes:
            nb = [prof[a, (b - 1) % nt], prof[a, (b + 1) % nt]] + ([prof[a - 1, b]] if a > 0 else []) + ([prof[a + 1, b]] if a < nz - 1 else [])
            nb = [x for x in nb if x > 0]
            if nb:
                new[a, b] = float(np.mean(nb))
        prof = new
    smooth = prof.copy()  # never smaller than the surface: the larger of the cell and its neighbours' mean
    for a in range(nz):
        for b in range(nt):
            nb = [prof[a, (b - 1) % nt], prof[a, (b + 1) % nt], prof[max(a - 1, 0), b], prof[min(a + 1, nz - 1), b]]
            smooth[a, b] = max(prof[a, b], float(np.mean(nb)))
    return {"profile": smooth, "z0": z0, "z1": z1, "dz": dz, "cy": cy}


def bake_clip(anim, action, rig, rest, gun_scale, two_handed=True, hold=0.0, reach=0.97, lift=1.5, posture=None,
              additive=False, torso=None, arm_radius=(10.0, 9.0, 8.5, 7.0), max_push=(16.0, 18.0), max_gun=6.0,
              hold_reach=0.93):
    """Returns (frames, stats): frames is float32 [frame, exported bone, 12] = the bone's 3x4 matrix in the RAW
    armature space, in the order of export_bones(rig).

    additive: a clip played on an Additive animator layer (attack, reload, jump land). Unity takes only what changes
    against the clip's first frame, bone by bone in the parent's space, so the bake must leave those differences as
    the old clip has them: the old local rotations, the old local moves scaled to the new bone lengths, and nothing
    that depends on the pose (no posture, no hand IK, no gun placement, no floor guard). Its absolute pose means
    nothing, in the old clip either."""
    if additive:
        two_handed = False
        posture = None
        torso = None
    scene = bpy.context.scene
    set_action(anim, action)
    W = _np(anim.matrix_world)
    Winv = np.linalg.inv(W)
    bones = [b.name for b in rig.data.bones]
    out_bones = export_bones(rig)
    parent = {b.name: (b.parent.name if b.parent else None) for b in rig.data.bones}
    Nr = {n: _np(rest[n]) for n in bones}
    Or = {b.name: _np(b.matrix_local) for b in anim.data.bones}
    l_new, s_of, l_rest_old = {}, {}, {}
    for n in bones:
        p = parent[n]
        if p is None:
            continue
        l_new[n] = Nr[p][:3, :3].T @ (Nr[n][:3, 3] - Nr[p][:3, 3])
        if n in Or and p in Or:
            lo = (np.linalg.inv(Or[p]) @ Or[n][:, 3])[:3]
            l_rest_old[n] = lo
            lr = np.linalg.norm(lo)
            s_of[n] = (np.linalg.norm(l_new[n]) / lr) if lr > 1e-4 else 1.0
    arm_len = {}
    for side in "LR":
        ua, la, hd = "bip_upperArm_" + side, "bip_lowerArm_" + side, "bip_hand_" + side
        arm_len[side] = (np.linalg.norm(Nr[la][:3, 3] - Nr[ua][:3, 3]), np.linalg.norm(Nr[hd][:3, 3] - Nr[la][:3, 3]))
    f0, f1 = int(round(action.frame_range[0])), int(round(action.frame_range[1]))
    stats = {"frames": f1 - f0 + 1, "pull_max": 0.0, "left_miss_max": 0.0, "foot_min": 1e9, "held": 0}
    frames = np.zeros((f1 - f0 + 1, len(out_bones), 12), dtype=np.float32)
    anim_bones = [pb.name for pb in anim.pose.bones]
    # posture (what ToyProportions did at run time on the scaled rig): the pelvis levelled, the waist's hunch
    # relaxed towards rest while the upper back keeps most of its lean, neck relaxed, head upright. The arms keep
    # the clip's world rotations and the right hand goes back to where the clip had it, so the gun still points
    # where the clip points it.
    children = {n: [] for n in bones}
    for n in bones:
        if parent[n]:
            children[parent[n]].append(n)

    def subtree(n, stop=()):
        out = [n]
        for c in children[n]:
            if c not in stop:
                out += subtree(c, stop)
        return out

    up_rest = Nr["bip_head"][:3, 3] - Nr["bip_pelvis"][:3, 3]
    up_rest /= np.linalg.norm(up_rest)
    pelvis_up = Nr["bip_pelvis"][:3, :3].T @ up_rest
    head_up = Nr["bip_head"][:3, :3].T @ up_rest
    rest_local = {n: Nr[parent[n]][:3, :3].T @ Nr[n][:3, :3] for n in bones if parent[n]}
    hips = ("bip_hip_L", "bip_hip_R")
    arms = [n for side in "LR" for n in subtree("bip_upperArm_" + side)]
    tree_pelvis = subtree("bip_pelvis", hips)
    trees = {n: subtree(n) for n in ("bip_spine_0", "bip_spine_1", "bip_spine_2", "bip_neck", "bip_head")}
    foot_floor = min(Nr["bip_foot_L"][:3, 3] @ UP, Nr["bip_foot_R"][:3, 3] @ UP) - 0.3

    # The arms are cleared of the torso in two passes: the first measures the pushes frame by frame, they are then
    # smoothed over time (a push found frame by frame jumps where an arm enters or leaves the torso), and the second
    # pass applies them.
    count = f1 - f0 + 1
    rec = {(side, kind): np.zeros((count, 3)) for side in "LR" for kind in "EHK"} if torso else None
    schedule = [(pas, fi, f) for pas in ((0, 1) if torso else (1,)) for fi, f in enumerate(range(f0, f1 + 1))]
    smoothed = [False]
    for pas, fi, f in schedule:
        if torso and pas == 1 and not smoothed[0]:
            smoothed[0] = True
            kernel = np.exp(-0.5 * (np.arange(-6, 7) / 2.2) ** 2)
            kernel /= kernel.sum()
            for key, arr in rec.items():
                loops = count > 12 and np.linalg.norm(arr[0] - arr[-1]) < 2.0
                padded = np.pad(arr, ((6, 6), (0, 0)), mode="wrap" if loops else "edge")
                rec[key] = np.stack([np.convolve(padded[:, k], kernel, mode="valid") for k in range(3)], axis=1)
            stats.update({"pull_max": 0.0, "left_miss_max": 0.0, "held": 0})
        cleared = set()
        scene.frame_set(f)
        O = {n: W @ _np(anim.pose.bones[n].matrix) for n in anim_bones}
        Oinv = {}
        R = {}
        for n in bones:
            if n in O:
                r = O[n][:3, :3]
                R[n] = r / np.linalg.norm(r, axis=0)
            else:
                p = parent[n]
                R[n] = R[p] @ (Nr[p][:3, :3].T @ Nr[n][:3, :3]) if p else Nr[n][:3, :3]
        off = {}
        for n in bones:
            p = parent[n]
            if p is None:
                continue
            o = l_new[n]
            if n in O and p in O and n in l_rest_old:
                if p not in Oinv:
                    Oinv[p] = np.linalg.inv(O[p])
                o = o + ((Oinv[p] @ O[n][:, 3])[:3] - l_rest_old[n]) * s_of.get(n, 1.0)
            off[n] = o
        pos = {}
        bumped = [None]

        def fk(fixed):
            for n in bones:
                if n in fixed:
                    pos[n] = fixed[n]
                    continue
                p = parent[n]
                if p is None:
                    pos[n] = (O[n][:3, 3] + UP * lift) if n in O else Nr[n][:3, 3].copy()
                    if bumped[0] is not None:
                        pos[n] = pos[n] + bumped[0]
                else:
                    pos[n] = pos[p] + R[p] @ off[n]

        fk({})
        pos0 = dict(pos)
        # a foot under the floor (landing crouch: the new legs are longer against the hips' height): the root is raised
        low = min(pos["bip_foot_L"] @ UP, pos["bip_foot_R"] @ UP)
        if low < foot_floor and not additive:
            bumped[0] = UP * (foot_floor - low)
        bump = bumped[0] if bumped[0] is not None else np.zeros(3)
        fixed = {}
        if posture:
            R0 = dict(R)

            def turn(names, m):
                for x in names:
                    R[x] = m @ R[x]

            keep_back = R["bip_spine_2"].copy()
            if posture.get("pelvis_level", 0) > 0:
                c = _slerp(np.eye(3), _from_to(R["bip_pelvis"] @ pelvis_up, UP), posture["pelvis_level"])
                turn(tree_pelvis, c)
                for h in hips:
                    fixed[h] = pos0[h] + bump
            for b in ("bip_spine_0", "bip_spine_1"):
                if posture.get("waist_relax", 0) > 0:
                    local = R[parent[b]].T @ R[b]
                    new = R[parent[b]] @ _slerp(local, rest_local[b], posture["waist_relax"])
                    turn(trees[b], new @ R[b].T)
            if posture.get("upper_back_keep", 0) > 0 and posture.get("waist_relax", 0) > 0:
                new = _slerp(R["bip_spine_2"], keep_back, posture["upper_back_keep"])
                turn(trees["bip_spine_2"], new @ R["bip_spine_2"].T)
            if posture.get("neck_relax", 0) > 0:
                local = R[parent["bip_neck"]].T @ R["bip_neck"]
                new = R[parent["bip_neck"]] @ _slerp(local, rest_local["bip_neck"], posture["neck_relax"])
                turn(trees["bip_neck"], new @ R["bip_neck"].T)
            if posture.get("head_upright", 0) > 0:
                c = _slerp(np.eye(3), _from_to(R["bip_head"] @ head_up, UP), posture["head_upright"])
                turn(trees["bip_head"], c)
            for x in arms:
                R[x] = R0[x]
        fk(fixed)
        def knuckles(s):
            return np.mean([pos[x % s] for x in FINGERS_NEW], axis=0)

        def solve(side, wrist_target, pole=None):
            """pole: a point the elbow bends towards instead of where it is now."""
            ua, la, hd = "bip_upperArm_" + side, "bip_lowerArm_" + side, "bip_hand_" + side
            S, E0, H0 = pos[ua], pos[la], pos[hd]
            a, b = arm_len[side]
            E, H = _ik(S, E0 if pole is None else pole, wrist_target, a, b)
            R[ua] = _from_to(E0 - S, E - S) @ R[ua]
            R[la] = _from_to(H0 - E0, H - E) @ R[la]
            fixed[la] = E
            fixed[hd] = H
            fk(fixed)
            return np.linalg.norm(H - wrist_target)

        def inside(p, radius):
            """How deep the point (a ball of that radius) is in the torso, and the way out, in the clip's space."""
            c = "bip_spine_1"
            q = Nr[c][:3, :3] @ (R[c].T @ (p - pos[c])) + Nr[c][:3, 3]
            z0, z1, dz, cy, prof = torso["z0"], torso["z1"], torso["dz"], torso["cy"], torso["profile"]
            if not (z0 < q[2] < z1):
                return 0.0, None
            nz, nt = prof.shape
            rad = float(np.hypot(q[0], q[1] - cy))
            a = (q[2] - z0) / dz - 0.5
            b = (np.arctan2(q[1] - cy, q[0]) + np.pi) / (2 * np.pi) * nt - 0.5
            a0, b0 = int(np.floor(a)), int(np.floor(b))
            fa, fb = a - a0, b - b0
            cell = lambda x, y: prof[min(max(x, 0), nz - 1), y % nt]
            need = (cell(a0, b0) * (1 - fa) + cell(a0 + 1, b0) * fa) * (1 - fb) + (cell(a0, b0 + 1) * (1 - fa) + cell(a0 + 1, b0 + 1) * fa) * fb
            # nothing near the shoulders (the arm starts inside the chest) nor at the legs
            fade = (1.0 - _smooth(q[2], z1 - 14.0, z1 - 2.0)) * _smooth(q[2], z0, z0 + 6.0)
            depth = (need + radius - rad) * fade
            if depth <= 0 or rad < 1e-4:
                return 0.0, None
            out = np.array([q[0], q[1] - cy, 0.0]) / rad
            return float(depth), R[c] @ (Nr[c][:3, :3].T @ out)

        def bend_out(side, way):
            """The elbow turned about the shoulder - wrist line towards `way` (a vector, its length 0..1 = how
            much), the hand where it is."""
            ua, la, hd = "bip_upperArm_" + side, "bip_lowerArm_" + side, "bip_hand_" + side
            k = float(np.linalg.norm(way))
            if k < 0.02:
                return
            S, T = pos[ua].copy(), pos[hd].copy()
            line = (T - S) / np.linalg.norm(T - S)
            now = pos[la] - S
            now = now - line * (now @ line)
            want = way - line * (way @ line)
            if np.linalg.norm(want) < 1e-6:
                return
            want /= np.linalg.norm(want)
            now = now / np.linalg.norm(now) if np.linalg.norm(now) > 1e-6 else want
            bend = now * (1 - min(k, 1.0)) + want * min(k, 1.0)
            if np.linalg.norm(bend) < 1e-6:
                bend = want
            solve(side, T, pole=S + bend * 10.0 + line * 5.0)

        def clear(side):
            """Arm out of the torso: the elbow first (the whole arm turns about the shoulder), then the forearm and
            the hand (the wrist moves, the elbow stays on its side). Returns how far the wrist ended from where it
            was. Pass 0 finds the pushes and records them in the chest's frame; pass 1 applies the smoothed ones,
            once per frame."""
            ua, la, hd = "bip_upperArm_" + side, "bip_lowerArm_" + side, "bip_hand_" + side
            chest = R["bip_spine_1"]
            start = pos[hd].copy()
            S, E0 = pos[ua].copy(), pos[la].copy()

            def elbow_out(amount):
                """A pushed arm bends with its elbow away from the body and down, never towards the belly: a hand
                moved outwards past the elbow would leave the elbow on the inner side of the arm's line."""
                k = _smooth(amount, 0.5, 6.0)
                if k <= 0:
                    return
                rest_chest = Nr["bip_spine_1"][:3, :3]
                side_way = chest @ (rest_chest.T @ np.array([1.0 if side == "L" else -1.0, 0.0, 0.0]))
                want = side_way - UP * 0.7
                want /= np.linalg.norm(want)
                T = pos[hd].copy()
                line = (T - S) / np.linalg.norm(T - S)
                now = pos[la] - S
                now = now - line * (now @ line)
                now = now / np.linalg.norm(now) if np.linalg.norm(now) > 1e-6 else want
                want = want - line * (want @ line)
                if np.linalg.norm(want) < 1e-6:
                    return
                want /= np.linalg.norm(want)
                bend = now * (1 - k) + want * k
                if np.linalg.norm(bend) < 1e-6:
                    bend = want
                solve(side, T, pole=S + bend * 10.0 + line * 5.0)

            def elbow_to(E):
                E = S + (E - S) * (np.linalg.norm(E0 - S) / np.linalg.norm(E - S))
                turn_arm = _from_to(E0 - S, E - S)
                for x in subtree(ua):
                    R[x] = turn_arm @ R[x]
                for x in (la, hd):
                    fixed.pop(x, None)
                fk(fixed)
                solve(side, start)  # the hand back where it was, the elbow now outside
                return pos[la] - E0

            if pas == 1:
                if side in cleared:
                    return np.zeros(3)
                cleared.add(side)
                dE, dH = chest @ rec[(side, "E")][fi], chest @ rec[(side, "H")][fi]
                if np.linalg.norm(dE) > 0.05:
                    elbow_to(E0 + dE)
                if np.linalg.norm(dH) > 0.05:
                    solve(side, start + dH)
                elbow_out(float(np.linalg.norm(dE) + np.linalg.norm(dH)))
                return pos[hd] - start
            depth, way = inside(E0, arm_radius[0])
            if depth > 0:
                rec[(side, "E")][fi] += chest.T @ elbow_to(E0 + way * min(depth, max_push[0]))
            moved = 0.0
            for _ in range(3):
                E, H = pos[la], pos[hd]
                worst, push = 0.0, None
                for frac, radius in ((0.6, arm_radius[1]), (1.0, arm_radius[2])):
                    depth, way = inside(E + (H - E) * frac, radius)
                    if depth / frac > worst:
                        worst, push = depth / frac, way
                depth, way = inside(knuckles(side), arm_radius[3])
                if depth > worst:
                    worst, push = depth, way
                if worst < 0.2 or moved >= max_push[1]:
                    break
                step = min(worst, max_push[1] - moved)
                solve(side, H + push * step)
                moved += step
            rec[(side, "H")][fi] += chest.T @ (pos[hd] - start)
            return pos[hd] - start

        if two_handed and "weapon_bone" in O:
            Rg = R["weapon_bone"]
            pw_old = O["weapon_bone"][:3, 3]
            k_old = {s: np.mean([O[x % s][:3, 3] for x in FINGERS_OLD], axis=0) for s in "LR"}
            g = {s: Rg.T @ (k_old[s] - pw_old) for s in "LR"}
            w = 1.0 if hold <= 0 else 1.0 - _smooth(np.linalg.norm(g["L"]) / hold, 1.25, 1.6)

            def left_target():
                pivot = knuckles("R") - Rg @ (g["R"] * gun_scale)
                return pivot + Rg @ (g["L"] * gun_scale) - (knuckles("L") - pos["bip_hand_L"])

            if posture:
                # the gun back where the clip holds it: the right arm reaches from the straightened shoulder
                solve("R", pos0["bip_hand_R"] + bump)
            if torso:
                clear("R")
                if w <= 0.5:
                    clear("L")
            if w > 0.001:
                stats["held"] += 1
                wrist_l = left_target()
                S = pos["bip_upperArm_L"]
                can = sum(arm_len["L"]) * reach
                away = wrist_l - S
                far = np.linalg.norm(away)
                if far > can:
                    rise = away @ UP
                    level = away - UP * rise
                    flat = np.linalg.norm(level)
                    slide = level * (1 - np.sqrt(can * can - rise * rise) / flat) if (can > abs(rise) and flat > 1e-6) else away * (1 - can / far)
                    slide = slide * w
                    solve("R", pos["bip_hand_R"] - slide)
                    stats["pull_max"] = max(stats["pull_max"], float(np.linalg.norm(slide)))
                    wrist_l = left_target()
                wrist_l = pos["bip_hand_L"] + (wrist_l - pos["bip_hand_L"]) * w
                miss = solve("L", wrist_l)
                if torso and w > 0.5:
                    # The left hand is on the gun. The gun stays where the right hand holds it: pushed out for the
                    # left arm's sake it drags the right hand into the body and leaves the left arm straight
                    # (tried). So: the left elbow turns outwards with the hand in place, and the gun gives way only
                    # a little (max_gun), never past the left arm's comfortable reach nor into the torso.
                    la, hd = "bip_lowerArm_L", "bip_hand_L"
                    total = sum(arm_len["L"])
                    regrip = lambda: solve("L", pos[hd] + (left_target() - pos[hd]) * w)
                    if pas == 0:
                        E, H = pos[la], pos[hd]
                        worst, push = 0.0, None
                        for point, radius in ((E + (H - E) * 0.6, arm_radius[1]), (H, arm_radius[2]), (knuckles("L"), arm_radius[3])):
                            depth, way = inside(point, radius)
                            if depth > worst:
                                worst, push = depth, way
                        if worst > 0.2:
                            limit = max(np.linalg.norm(pos[hd] - pos["bip_upperArm_L"]) / total, hold_reach)
                            right_in = inside(pos["bip_hand_R"], arm_radius[3])[0]
                            for part in (1.0, 0.6, 0.3):
                                keep = (dict(R), dict(pos), dict(fixed))
                                shift = push * min(worst, max_gun) * part
                                solve("R", pos["bip_hand_R"] + shift)
                                regrip()
                                fine = (np.linalg.norm(pos[hd] - pos["bip_upperArm_L"]) / total <= limit + 0.004
                                        and inside(pos["bip_hand_R"], arm_radius[3])[0] <= right_in + 0.3)
                                if fine:
                                    rec[("L", "H")][fi] += R["bip_spine_1"].T @ shift
                                    break
                                for now, was in zip((R, pos, fixed), keep):
                                    now.clear()
                                    now.update(was)
                        depth, way = inside(pos[la], arm_radius[0])
                        if depth > 0:  # the elbow leaves the belly the shortest way
                            rec[("L", "K")][fi] = R["bip_spine_1"].T @ (way * _smooth(depth, 0.5, 6.0))
                    else:
                        shift = R["bip_spine_1"] @ rec[("L", "H")][fi]
                        if np.linalg.norm(shift) > 0.05:
                            solve("R", pos["bip_hand_R"] + shift)
                            regrip()
                        bend_out("L", R["bip_spine_1"] @ rec[("L", "K")][fi])
                        cleared.add("L")
                    miss = float(np.linalg.norm(pos[hd] - (pos[hd] + (left_target() - pos[hd]) * w)))
                if w > 0.99:
                    stats["left_miss_max"] = max(stats["left_miss_max"], float(miss))
            pivot = knuckles("R") - Rg @ (g["R"] * gun_scale)
            fixed["weapon_bone"] = pivot
            for n in bones:
                if n.startswith("weapon_bone") and n != "weapon_bone" and n in O and parent[n] == "bip_hand_R":
                    fixed[n] = pivot + (O[n][:3, 3] - pw_old) * gun_scale
            fk(fixed)
        elif torso:
            clear("R")
            clear("L")
        if torso:
            stats["clear_max"] = max(stats.get("clear_max", 0.0), float(np.linalg.norm(pos["bip_hand_R"] - (pos0["bip_hand_R"] + bump))))
        stats["foot_min"] = min(stats["foot_min"], float(min(pos["bip_foot_L"] @ UP, pos["bip_foot_R"] @ UP)))
        for bi, n in enumerate(out_bones):
            m = np.eye(4)
            m[:3, :3] = R[n]
            m[:3, 3] = pos[n]
            frames[fi, bi] = (Winv @ m)[:3, :].reshape(-1)
    return frames, stats


def write_clips(path, bone_names, clips, fps=24):
    """clips: {name: frames array}. Writes <path>.clips.bin (float32) and <path>.clips.json (the index)."""
    index = {"bones": bone_names, "fps": fps, "clips": []}
    offset = 0
    with open(path + ".clips.bin", "wb") as f:
        for name, frames in clips.items():
            data = np.ascontiguousarray(frames, dtype="<f4")
            f.write(data.tobytes())
            index["clips"].append({"name": name, "frames": int(data.shape[0]), "offset": offset})
            offset += data.size
    with open(path + ".clips.json", "w") as f:
        json.dump(index, f)
    return offset
