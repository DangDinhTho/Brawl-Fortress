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


def bake_clip(anim, action, rig, rest, gun_scale, two_handed=True, hold=0.0, reach=0.97, lift=1.5, posture=None):
    """Returns (frames, stats): frames is float32 [frame, exported bone, 12] = the bone's 3x4 matrix in the RAW
    armature space, in the order of export_bones(rig)."""
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

    for fi, f in enumerate(range(f0, f1 + 1)):
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
        if low < foot_floor:
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
        if two_handed and "weapon_bone" in O:
            Rg = R["weapon_bone"]
            pw_old = O["weapon_bone"][:3, 3]
            k_old = {s: np.mean([O[x % s][:3, 3] for x in FINGERS_OLD], axis=0) for s in "LR"}
            g = {s: Rg.T @ (k_old[s] - pw_old) for s in "LR"}
            w = 1.0 if hold <= 0 else 1.0 - _smooth(np.linalg.norm(g["L"]) / hold, 1.25, 1.6)

            def knuckles(s):
                return np.mean([pos[x % s] for x in FINGERS_NEW], axis=0)

            def solve(side, wrist_target):
                ua, la, hd = "bip_upperArm_" + side, "bip_lowerArm_" + side, "bip_hand_" + side
                S, E0, H0 = pos[ua], pos[la], pos[hd]
                a, b = arm_len[side]
                E, H = _ik(S, E0, wrist_target, a, b)
                R[ua] = _from_to(E0 - S, E - S) @ R[ua]
                R[la] = _from_to(H0 - E0, H - E) @ R[la]
                fixed[la] = E
                fixed[hd] = H
                fk(fixed)
                return np.linalg.norm(H - wrist_target)

            def left_target():
                pivot = knuckles("R") - Rg @ (g["R"] * gun_scale)
                return pivot + Rg @ (g["L"] * gun_scale) - (knuckles("L") - pos["bip_hand_L"])

            if posture:
                # the gun back where the clip holds it: the right arm reaches from the straightened shoulder
                solve("R", pos0["bip_hand_R"] + bump)
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
                if w > 0.99:
                    stats["left_miss_max"] = max(stats["left_miss_max"], float(miss))
            pivot = knuckles("R") - Rg @ (g["R"] * gun_scale)
            fixed["weapon_bone"] = pivot
            for n in bones:
                if n.startswith("weapon_bone") and n != "weapon_bone" and n in O and parent[n] == "bip_hand_R":
                    fixed[n] = pivot + (O[n][:3, 3] - pw_old) * gun_scale
            fk(fixed)
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
