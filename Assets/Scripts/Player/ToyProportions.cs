using UnityEngine;

/// <summary>
/// Toy (chibi) characters keep the old skeleton and the old animation clips; their proportions (big head, short legs,
/// wide shoulders) come from a table of uniform local bone scales applied after the Animator every frame. The skinned
/// meshes are bound against the skeleton with these scales, so the table must stay as generated (Tools/CharArt,
/// ToyCharacterBuilder).
/// The old clips were made for a tall hunched figure: spine and neck lean forward, the collar bones swing the
/// shoulders up and forward. On a torso three times as large and a head six times as large that reads as huge
/// shoulders with the face sunk into the chest, so the posture block below takes part of it back out. The upper arms
/// keep the orientation the clip gave them: the weapon still points where the clip aims.
/// </summary>
[DefaultExecutionOrder(10000)]
public class ToyProportions : MonoBehaviour
{
    public Transform skeletonRoot;
    public Vector3 skeletonRootScale = Vector3.one;
    public Transform[] bones;
    public float[] scales;

    [Header("Posture")]
    [Tooltip("0 = the clip's own lean, 1 = the torso as upright as in the rest pose.")]
    [Range(0f, 1f)] public float upright = 0.85f;
    public Transform uprightBone;
    public Transform uprightTo;
    [Tooltip("Direction of the torso (uprightBone -> uprightTo) in the rest pose, in this object's space: what 'upright' means for this figure.")]
    public Vector3 uprightRestDir = Vector3.up;
    [Tooltip("0 = the clip's bend of the chest (spine 1-3), 1 = their rest pose. The clips curl the chest forward, which pushes the collar up over the chin.")]
    [Range(0f, 1f)] public float spineRelax = 0.8f;
    public Transform[] spineBones;
    public Quaternion[] spineRest;
    [Tooltip("0 = the clip's shoulders, 1 = collar bones back in their rest pose (shoulders down and back).")]
    [Range(0f, 1f)] public float shoulderRelax = 0.75f;
    public Transform[] collarBones;
    public Quaternion[] collarRest;
    [Tooltip("0 = the clip's neck, 1 = the neck in its rest pose (the head where the model has it above the collar).")]
    [Range(0f, 1f)] public float neckRelax = 0.85f;
    public Transform neckBone;
    public Quaternion neckRest = Quaternion.identity;
    [Tooltip("0 = the head pitch / roll of the clip, 1 = head held level (its turning left / right is kept).")]
    [Range(0f, 1f)] public float headUpright = 0.85f;
    public Transform headBone;
    [Tooltip("The head bone's local axis that points up in the rest pose (set by ToyCharacterBuilder).")]
    public Vector3 headUpLocal = Vector3.up;
    [Tooltip("Bones that keep the world orientation the clip gave them (upper arms).")]
    public Transform[] keepOrientation;

    [Header("Weapons")]
    [Tooltip("Size of everything held in the hands, on top of the hand's own scale. 1.34 = the size the weapons had on the old characters.")]
    public float weaponScale = 1.6f;
    [Tooltip("The grip bone (weapon_bone): the other weapon bones of the same hand are spread out from it.")]
    public Transform weaponPivot;
    [Tooltip("weapon_bone and its sibling bones (weapon_bone_1..3): the clips key each of them in the hand's space, so their offsets from the grip are scaled together with their size.")]
    public Transform[] weaponBones;
    [Tooltip("Bones scaled about themselves (weapon_bone_L, weapon_targe).")]
    public Transform[] weaponBonesOther;

    [Header("Left hand on the weapon")]
    [Tooltip("0 = off, 1 = on the two-handed clips the left hand is put back on the weapon (shoulders and weapon size moved it off).")]
    [Range(0f, 1f)] public float leftHandGrip = 1f;
    public Transform gripUpperArm;
    public Transform gripLowerArm;
    public Transform gripHand;
    [Tooltip("Two-handed clips: the chest is turned to this angle from where the character faces, left shoulder forward; the head keeps facing where the clip looks and the gun stays where the clip holds it. The clips already turn it about 25. Left hand to its place on the grenade launcher (standing): 0.49 m at 25, 0.35 m at 45, 0.24 m at 55, 0.11 m at 65; from 55 on the right arm is at full stretch.")]
    [Range(0f, 90f)] public float stanceYaw = 50f;
    [Tooltip("Off (Tho's choice): only the torso turns a little and the left hand reaches towards the gun as far as the short arm goes; the right arm and the gun stay where the clip has them. On: the right arm also brings the gun in front of the turned chest so both hands hold it (needs a turn of about 75 degrees).")]
    public bool stanceMovesWeapon = false;
    [Tooltip("How far in front of the turned chest the gun is held, in shoulder half-widths.")]
    public float stanceFront = 0.86f;
    [Tooltip("How far under the shoulders the hands hold the gun, in shoulder half-widths.")]
    public float stanceDrop = 0.6f;
    public Transform holdUpperArm;
    public Transform holdLowerArm;
    public Transform holdHand;
    [Tooltip("Distance left hand - grip bone in the clip on this rig, in the hand's own units: at or under 'near' the clip holds the weapon with both hands (the gun clips measure about 0.0076), from 'far' on the hand is free (melee: about 0.0135).")]
    public float gripNear = 0.0095f;
    public float gripFar = 0.0115f;
    [Tooltip("Where the left hand holds the gun, in the grip bone's space and the hand's units, measured on the OLD character's two-handed stand clip by ToyCharacterBuilder (on the toy rig the clip leaves the hands much further apart). Zero = use the clip as it is.")]
    public Vector3 gripOnWeapon;

    private Vector3[] weaponWritten;
    private Vector3[] weaponSource;

    private Quaternion[] kept;

    private void LateUpdate()
    {
        Apply();
    }

    public void Apply()
    {
        if (skeletonRoot != null) skeletonRoot.localScale = skeletonRootScale;
        if (bones != null && scales != null)
        {
            int n = Mathf.Min(bones.Length, scales.Length);
            for (int i = 0; i < n; i++)
            {
                if (bones[i] != null) bones[i].localScale = Vector3.one * scales[i];
            }
        }

        // where the clip has the left hand on the weapon, measured before anything moves it
        float gripWeight = 0f; Vector3 gripLocal = Vector3.zero; float gripUnit = 1f; Quaternion gripHandRotation = Quaternion.identity;
        if (leftHandGrip > 0f && gripHand != null && gripLowerArm != null && gripUpperArm != null && weaponPivot != null && weaponPivot.parent != null)
        {
            gripUnit = weaponPivot.parent.lossyScale.x;
            if (gripUnit > 1e-6f)
            {
                gripLocal = Quaternion.Inverse(weaponPivot.rotation) * (gripHand.position - weaponPivot.position) / gripUnit;
                float d = gripLocal.magnitude;
                if (d > 0.0005f) gripWeight = leftHandGrip * Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(gripNear, gripFar, d));
                gripHandRotation = gripHand.rotation;
                if (gripOnWeapon.sqrMagnitude > 0f) gripLocal = gripOnWeapon;
            }
        }

        ApplyWeaponScale();

        int k = keepOrientation != null ? keepOrientation.Length : 0;
        if (kept == null || kept.Length != k) kept = new Quaternion[k];
        for (int i = 0; i < k; i++) if (keepOrientation[i] != null) kept[i] = keepOrientation[i].rotation;

        Vector3 up = transform.up;
        Relax(spineBones, spineRest, spineRelax);
        if (upright > 0f && uprightBone != null && uprightTo != null) Straighten(uprightBone, uprightTo.position - uprightBone.position, transform.TransformDirection(uprightRestDir), upright);
        Relax(collarBones, collarRest, shoulderRelax);
        if (neckRelax > 0f && neckBone != null) neckBone.localRotation = Quaternion.Slerp(neckBone.localRotation, neckRest, neckRelax);
        if (headUpright > 0f && headBone != null) Straighten(headBone, headBone.rotation * headUpLocal, up, headUpright);

        for (int i = 0; i < k; i++) if (keepOrientation[i] != null) keepOrientation[i].rotation = kept[i];

        if (gripWeight > 0.001f) TwoHandStance(gripLocal, gripUnit, gripHandRotation, gripWeight);
    }

    private void TwoHandStance(Vector3 gripLocal, float gripUnit, Quaternion leftHandRotation, float weight)
    {
        Vector3 up = transform.up;
        bool rightArm = holdUpperArm != null && holdLowerArm != null && holdHand != null;
        if (rightArm && stanceYaw > 0f && uprightBone != null)
        {
            // pivot -> left grip on the gun, and the gun's orientation: both stay what the clip made them
            Quaternion gunRotation = weaponPivot.rotation;
            Quaternion rightHandRotation = holdHand.rotation;
            Vector3 handToPivot = weaponPivot.position - holdHand.position;
            Vector3 rightHandPosition = holdHand.position;
            Vector3 toGrip = gunRotation * gripLocal * (gripUnit * weaponScale);
            Quaternion headRotation = headBone != null ? headBone.rotation : Quaternion.identity;

            // turn to the stance angle, counted from where this object faces (the clips already turn the chest a little)
            Vector3 chest = Vector3.Cross(holdUpperArm.position - gripUpperArm.position, up);
            float turned = chest.sqrMagnitude > 1e-10f ? Vector3.SignedAngle(transform.forward, chest, up) : 0f;
            uprightBone.rotation = Quaternion.AngleAxis((stanceYaw - turned) * weight, up) * uprightBone.rotation;
            if (headBone != null) headBone.rotation = headRotation;

            Vector3 sr = holdUpperArm.position, sl = gripUpperArm.position;
            float half = (sr - sl).magnitude * 0.5f;
            Vector3 front = Vector3.Cross(sr - sl, up).normalized;
            Vector3 along = Vector3.ProjectOnPlane(toGrip, up);
            float length = along.magnitude;
            if (!stanceMovesWeapon)
            {
                // the gun stays where the clip holds it while the chest turns under it: the right arm follows the gun,
                // the left shoulder comes forward towards it
                Reach(holdUpperArm, holdLowerArm, holdHand, rightHandPosition, rightHandPosition);
                holdHand.rotation = rightHandRotation;
            }
            else if (half > 1e-5f && length > 1e-5f)
            {
                along /= length;
                Vector3 centre = (sr + sl) * 0.5f + front * (stanceFront * half) - up * (stanceDrop * half);
                // the hands sit either side of the centre along the gun, the right one at the grip bone
                Vector3 pivot = centre - along * (length * 0.5f) - up * (Vector3.Dot(toGrip, up) * 0.5f);
                Vector3 handTarget = Vector3.Lerp(holdHand.position, pivot - handToPivot, weight);
                Reach(holdUpperArm, holdLowerArm, holdHand, handTarget, handTarget);
                holdHand.rotation = rightHandRotation;
            }
        }
        Vector3 target = weaponPivot.position + weaponPivot.rotation * gripLocal * (gripUnit * weaponScale);
        target = Vector3.Lerp(gripHand.position, target, weight);
        Reach(gripUpperArm, gripLowerArm, gripHand, target, weaponPivot.position);
        gripHand.rotation = leftHandRotation;
    }

    /// <summary>Two-bone IK. A target out of reach is slid towards <paramref name="slideTo"/> (along the gun, to the grip
    /// bone) until the short arm reaches it.</summary>
    private static void Reach(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 slideTo)
    {
        Vector3 s = upper.position, e = lower.position, h = hand.position;
        float a = (e - s).magnitude, b = (h - e).magnitude;
        if (a < 1e-5f || b < 1e-5f) return;
        float reach = (a + b) * 0.985f;
        if ((target - s).magnitude > reach)
        {
            if ((slideTo - s).magnitude <= reach)
            {
                float lo = 0f, hi = 1f;
                for (int i = 0; i < 12; i++)
                {
                    float mid = (lo + hi) * 0.5f;
                    if ((Vector3.Lerp(target, slideTo, mid) - s).magnitude > reach) lo = mid; else hi = mid;
                }
                target = Vector3.Lerp(target, slideTo, hi);
            }
            else target = s + (target - s).normalized * reach;
        }
        float c = Mathf.Clamp((target - s).magnitude, Mathf.Abs(a - b) + 1e-4f, a + b - 1e-4f);
        // elbow: open or close it to the length needed
        Vector3 toS = s - e, toH = h - e;
        Vector3 axis = Vector3.Cross(toS, toH);
        if (axis.sqrMagnitude < 1e-12f) axis = lower.right; else axis.Normalize();
        float current = Vector3.Angle(toS, toH);
        float wanted = Mathf.Acos(Mathf.Clamp((a * a + b * b - c * c) / (2f * a * b), -1f, 1f)) * Mathf.Rad2Deg;
        lower.rotation = Quaternion.AngleAxis(wanted - current, axis) * lower.rotation;
        // shoulder: swing the whole arm onto the target
        Vector3 nowH = hand.position - s;
        if (nowH.sqrMagnitude > 1e-10f) upper.rotation = Quaternion.FromToRotation(nowH, target - s) * upper.rotation;
    }

    private void ApplyWeaponScale()
    {
        if (Mathf.Approximately(weaponScale, 1f)) return;
        Vector3 size = Vector3.one * weaponScale;
        if (weaponBonesOther != null)
        {
            for (int i = 0; i < weaponBonesOther.Length; i++) if (weaponBonesOther[i] != null) weaponBonesOther[i].localScale = size;
        }
        if (weaponBones == null || weaponPivot == null) return;
        int n = weaponBones.Length;
        if (weaponWritten == null || weaponWritten.Length != n) { weaponWritten = new Vector3[n]; weaponSource = new Vector3[n]; for (int i = 0; i < n; i++) weaponWritten[i] = new Vector3(float.NaN, 0, 0); }
        // the position the clip gave each bone this frame; a bone no clip wrote still holds what was written here last
        // frame, and its own source is used again instead of scaling it a second time
        Vector3 pivot = weaponPivot.localPosition;
        for (int i = 0; i < n; i++)
        {
            Transform b = weaponBones[i];
            if (b == null) continue;
            Vector3 p = b.localPosition;
            if (p == weaponWritten[i]) p = weaponSource[i]; else weaponSource[i] = p;
            if (b == weaponPivot) pivot = p;
        }
        for (int i = 0; i < n; i++)
        {
            Transform b = weaponBones[i];
            if (b == null) continue;
            Vector3 p = pivot + (weaponSource[i] - pivot) * weaponScale;
            b.localPosition = p; weaponWritten[i] = p;
            b.localScale = size;
        }
    }

    private static void Relax(Transform[] list, Quaternion[] rest, float amount)
    {
        if (amount <= 0f || list == null || rest == null) return;
        int n = Mathf.Min(list.Length, rest.Length);
        for (int i = 0; i < n; i++)
        {
            if (list[i] != null) list[i].localRotation = Quaternion.Slerp(list[i].localRotation, rest[i], amount);
        }
    }

    private static void Straighten(Transform bone, Vector3 current, Vector3 up, float amount)
    {
        if (current.sqrMagnitude < 1e-10f) return;
        Quaternion c = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(current, up), amount);
        bone.rotation = c * bone.rotation;
    }
}
