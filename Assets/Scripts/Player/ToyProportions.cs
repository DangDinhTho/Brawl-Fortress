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
    [Tooltip("Bones moved in their parent's space after the clip (the hips): the model's legs stand further back than the old skeleton's, and a joint on the front face of a leg crumples it when it bends. The meshes are bound with these offsets (Tools/CharArt export, ToyCharacterBuilder).")]
    public Transform[] offsetBones;
    public Vector3[] offsets;

    [Header("Posture")]
    [Tooltip("0 = the clip's pelvis tilt, 1 = pelvis level as in the rest pose. The clips tip the hips forward; on this figure the pelvis is a big block, so the bend reads as a hunch at the seat and waist. Levelling it stands the lower body up and leaves the curl in the upper back; the legs stay where the clip has them.")]
    [Range(0f, 1f)] public float pelvisLevel = 0.8f;
    public Transform pelvisBone;
    [Tooltip("The pelvis bone's local axis that points up in the rest pose (set by ToyCharacterBuilder).")]
    public Vector3 pelvisUpLocal = Vector3.up;
    public Transform[] hipBones;
    [Tooltip("0 = the clip's bend at the waist (spine 0 and 1), 1 = waist as in the rest pose. The upper back (spine 2 and 3) keeps the clip's curl, so the hunch sits between the shoulders instead of at the belt.")]
    [Range(0f, 1f)] public float waistRelax = 0.7f;
    public Transform[] waistBones;
    public Quaternion[] waistRest;
    [Tooltip("How much of the clip's lean the upper back (from spine 2 up: chest, shoulders, arms) keeps after the waist is stood up. 1 = the chest leans exactly as in the clip, so the whole hunch moves from the belt to between the shoulders; 0 = the chest stands up with the waist.")]
    [Range(0f, 1f)] public float upperBackKeep = 0.75f;
    public Transform upperBackBone;
    [Tooltip("0 = the clip's own lean, 1 = the torso as upright as in the rest pose.")]
    [Range(0f, 1f)] public float upright = 0f;
    public Transform uprightBone;
    public Transform uprightTo;
    [Tooltip("Direction of the torso (uprightBone -> uprightTo) in the rest pose, in this object's space: what 'upright' means for this figure.")]
    public Vector3 uprightRestDir = Vector3.up;
    [Tooltip("0 = the clip's bend of the chest (spine 1-3), 1 = their rest pose. The clips curl the chest forward, which pushes the collar up over the chin.")]
    [Range(0f, 1f)] public float spineRelax = 0f;
    public Transform[] spineBones;
    public Quaternion[] spineRest;
    [Tooltip("0 = the clip's shoulders, 1 = collar bones back in their rest pose (shoulders down and back).")]
    [Range(0f, 1f)] public float shoulderRelax = 0f;
    public Transform[] collarBones;
    public Quaternion[] collarRest;
    [Tooltip("0 = the clip's neck, 1 = the neck in its rest pose (the head where the model has it above the collar).")]
    [Range(0f, 1f)] public float neckRelax = 0.6f;
    public Transform neckBone;
    public Quaternion neckRest = Quaternion.identity;
    [Tooltip("0 = the head pitch / roll of the clip, 1 = head held level (its turning left / right is kept).")]
    [Range(0f, 1f)] public float headUpright = 0.8f;
    public Transform headBone;
    [Tooltip("The head bone's local axis that points up in the rest pose (set by ToyCharacterBuilder).")]
    public Vector3 headUpLocal = Vector3.up;
    [Tooltip("0 = the clip's knee bend, 1 = knees as in the rest pose. The run clips kick the heel up to the seat; on legs this short and thick the trouser leg folds onto itself.")]
    [Range(0f, 1f)] public float kneeRelax = 0f;
    public Transform[] kneeBones;
    public Quaternion[] kneeRest;
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
    [Tooltip("The left arm may grow up to this factor to reach the gun (1 = never). The arm and its sleeve get that much longer and thicker, the hand keeps its size.")]
    [Range(1f, 1.5f)] public float armStretch = 1.25f;
    [Tooltip("Read only: how far the left hand ended from its place on the gun last frame, in metres.")]
    public float lastGripGap;
    [Tooltip("Read only: how far the palm had to turn to face up last frame, in degrees (after 'Palm Up').")]
    public float lastPalmNeed;
    [Tooltip("Read only: distance left hand - grip bone the clip gave last frame (hand units), and how much the hand was put on the gun (0 - 1).")]
    public float lastGripDistanceSeen;
    public float lastGripWeight;
    [Tooltip("The left shoulder joint is pushed towards the gun by this share of the collar bone's length (the whole arm goes with it), so the hand reaches with less stretching.")]
    [Range(0f, 0.8f)] public float shoulderPush = 0.12f;
    public Transform gripCollar;
    [Tooltip("How fast the left hand goes onto / comes off the gun when a clip takes it away (the menu clip lifts it to spin the drum), in full blends per second. The clip's hand crosses the on / off distance within a frame or two, which snapped the arm.")]
    public float gripBlendSpeed = 4f;
    [Tooltip("Distance left hand - grip bone at which the two-handed clips hold the gun on this rig, in the hand's units (measured: stand 0.0073, run 0.0066 - 0.0075, menu idle 0.0075; melee 0.011 - 0.012; the menu clip that spins the drum swings between 0.0057 and 0.0153), how far off that still counts as holding, and over how much more the hold fades out.")]
    public float gripAt = 0.0071f;
    public float gripWindow = 0.0006f;
    public float gripFade = 0.0008f;
    [Tooltip("The hand only counts as holding the gun while that distance changes slower than this (hand units per second; runs measure 0.0015, the jump start 0.0057, the drum spin up to 0.035); three times as fast = not holding.")]
    public float gripStill = 0.006f;
    [Tooltip("Seconds the hand has to stay at the grip before it is put back on the gun.")]
    public float gripDelay = 0.15f;
    [Tooltip("How far the left wrist may bend from its straight rest pose, in degrees. The clips bend it up to 90 on the old long arm; on the short toy arm with its big rigid hand that reads as a broken wrist.")]
    [Range(0f, 90f)] public float wristMax = 45f;
    [Tooltip("The left arm is made long enough to reach the gun with the elbow still bent: 1 = arm fully straight at the grip, lower = more bend (needs more of 'Arm Stretch').")]
    [Range(0.7f, 1f)] public float reachBend = 0.8f;
    [Tooltip("0 = the elbow where the reach leaves it, 1 = the left elbow swung round the shoulder - hand line to point along 'Elbow Pole'.")]
    [Range(0f, 1f)] public float elbowForward = 1f;
    [Tooltip("Where the left elbow points, in this object's space (x right, y up, z forward): down beside the body, a little out and back (Tho, 28 Nov 2026: an elbow flared out and up read as sticking out in front; the elbow stays here and the forearm goes across to the gun).")]
    public Vector3 elbowPole = new Vector3(0.1f, -1f, -0.1f);
    [Tooltip("0 = the hand as the clip turns it, 1 = the left hand straight on the forearm (then turned palm up by 'Palm Up').")]
    [Range(0f, 1f)] public float handStraight = 1f;
    [Tooltip("How far the forearm is twisted so the left palm faces up under the gun: 0 = not at all, 1 = palm flat up.")]
    [Range(0f, 1f)] public float palmUp = 0.85f;
    [Tooltip("The forearm is never twisted further than this for 'Palm Up', in degrees: all of the twist happens at the elbow, and past about 40 the sleeve wrings like a cloth.")]
    [Range(0f, 120f)] public float palmTwistMax = 35f;
    [Tooltip("What the forearm twist leaves over of 'Palm Up' is turned at the wrist itself, up to this many degrees (keep it under 'Wrist Max').")]
    [Range(0f, 90f)] public float wristTwistMax = 35f;
    [Tooltip("The left hand bone's local axis that points out of the palm (set by ToyCharacterBuilder from the model).")]
    public Vector3 palmLocal;
    [Tooltip("The grip point on the gun is where the palm goes, not the wrist: the wrist is held back from it by this much of the forearm's length (the fist is about half a forearm long). 0 = wrist on the grip point, as on the old long-armed model.")]
    [Range(0f, 1f)] public float palmOffset = 0.5f;
    [Tooltip("The left hand is put further along the gun than the clip has it (towards the muzzle, under the fore-end), as a share of the distance right hand - left hand on the gun. Tho, 28 Nov 2026: the hand forward, not the elbow.")]
    [Range(0f, 1f)] public float handForward = 0.22f;

    [Header("Blink (menu)")]
    public SkinnedMeshRenderer blinkRenderer;
    public string blinkShape = "Blink";
    [Tooltip("Seconds between blinks (min, max).")]
    public Vector2 blinkEvery = new Vector2(2.2f, 4.8f);
    public float blinkTime = 0.16f;
    public Transform gripUpperArm;
    public Transform gripLowerArm;
    public Transform gripHand;
    [Tooltip("Two-handed clips: the chest is turned to this angle from where the character faces, left shoulder forward; the head keeps facing where the clip looks and the gun stays where the clip holds it. The clips already turn it about 25. Left hand to its place on the grenade launcher (standing): 0.49 m at 25, 0.35 m at 45, 0.24 m at 55, 0.11 m at 65; from 55 on the right arm is at full stretch.")]
    [Range(0f, 90f)] public float stanceYaw = 0f;
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
    [Tooltip("The part of 'Grip On Weapon' that pointed down in the world when it was measured (the hand sits under the gun): 'Hand Forward' moves the hand along the gun only, this part is scaled by 'Grip Drop'.")]
    public Vector3 gripDown;
    [Tooltip("The same pair for every two-handed clip of the controller, by clip name (set by ToyCharacterBuilder). The gun is made of several bones that every clip poses its own way, so the place of the left hand measured on one clip is off the gun on another (in a match on stand_SECONDARY the palm ended 21 cm beside the grenade launcher). The clips playing on layer 0 are looked up here and blended by weight.")]
    public string[] gripClips;
    public Vector3[] gripClipOnWeapon;
    public Vector3[] gripClipDown;
    [System.NonSerialized] public AnimationClip clipHint;
    [Tooltip("Fallback for the menu clip (selectionmenu_idle) when the per-clip table has no entry. Used while 'Showcase' is on.")]
    public Vector3 gripOnWeaponMenu;
    public Vector3 gripDownMenu;
    [Tooltip("How far under the gun the left palm sits, as a share of where the old character's wrist was.")]
    [Range(0f, 1.5f)] public float gripDrop = 0.75f;

    [Header("Home screen pose")]
    [Tooltip("Set by HomeManager: the character is shown close up in the menu. On the two-handed clips the gun is then held in the right hand alone, muzzle up, and the left arm hangs relaxed (the short arms cannot hold a gun with both hands, and in the menu that shows).")]
    public bool showcase;
    [Tooltip("Off (Tho, 28 Nov 2026): the menu plays its clip as it is. On: the one-handed pose below.")]
    public bool showcasePose = false;
    [Tooltip("How far the line grip - left-hand grip of the gun points up / out to the right in the menu, in degrees (the barrel sits above that line, so the muzzle ends up steeper).")]
    public float showcaseUp = 38f;
    public float showcaseOut = 12f;
    [Tooltip("Right hand in the menu, from the right shoulder, in shoulder half-widths: x to the character's right, y up, z forward.")]
    public Vector3 showcaseHand = new Vector3(0.22f, -0.42f, 0.55f);
    [Tooltip("Rest pose of the left arm (upper arm, forearm, hand), set by ToyCharacterBuilder.")]
    public Quaternion[] leftArmRest;
    [Tooltip("The relaxed left arm is brought this many degrees down from its rest pose (the model's arms stick out at about 40).")]
    public float showcaseArmDown = 22f;
    [Tooltip("Rest pose of the right hand on its forearm, set by ToyCharacterBuilder.")]
    public Quaternion rightHandRest = Quaternion.identity;
    [Tooltip("0 = the wrist bend of the clip, 1 = wrist straight. The gun clips bend the right wrist about 90 degrees, and the hand and the cuff are rigid pieces: the faces between them stretch into streaks.")]
    [Range(0f, 1f)] public float showcaseWrist = 0f;

    private Vector3 gripDownNow;
    private float gripSmooth = -1f;
    private float lastGripDistance;
    private float holdTime;
    private Vector3[] hipPos;
    private Quaternion[] hipRot;
    private Vector3[] offsetWritten;
    private Vector3[] offsetSource;
    private Animator animator;
    private bool animatorLooked;
    private Vector3[] weaponWritten;
    private Vector3[] weaponSource;

    private Quaternion[] kept;

    private int blinkIndex = -2;
    private float nextBlink;
    private float blinkT = -1f;
    private int blinksLeft;

    private void LateUpdate()
    {
        Apply();
        if (showcase) Blink();
    }

    private void Blink()
    {
        if (blinkRenderer == null || blinkRenderer.sharedMesh == null) return;
        if (blinkIndex == -2) blinkIndex = blinkRenderer.sharedMesh.GetBlendShapeIndex(blinkShape);
        if (blinkIndex < 0) return;
        float now = Time.time;
        if (nextBlink <= 0f) nextBlink = now + Random.Range(blinkEvery.x, blinkEvery.y);
        if (blinkT < 0f)
        {
            if (now < nextBlink) return;
            blinkT = 0f;
            // now and then a quick double blink
            if (blinksLeft <= 0) blinksLeft = Random.value < 0.2f ? 2 : 1;
        }
        blinkT += Time.deltaTime;
        float u = blinkTime > 1e-4f ? blinkT / blinkTime : 1f;
        blinkRenderer.SetBlendShapeWeight(blinkIndex, u < 1f ? Mathf.Sin(u * Mathf.PI) * 100f : 0f);
        if (u >= 1f)
        {
            blinkT = -1f; blinksLeft--;
            nextBlink = now + (blinksLeft > 0 ? 0.08f : Random.Range(blinkEvery.x, blinkEvery.y));
        }
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

        ApplyOffsets();

        // where the clip has the left hand on the weapon, measured before anything moves it
        float gripWeight = 0f; Vector3 gripLocal = Vector3.zero; float gripUnit = 1f; Quaternion gripHandRotation = Quaternion.identity;
        if (leftHandGrip > 0f && gripHand != null && gripLowerArm != null && gripUpperArm != null && weaponPivot != null && weaponPivot.parent != null)
        {
            gripUnit = weaponPivot.parent.lossyScale.x;
            if (gripUnit > 1e-6f)
            {
                gripLocal = Quaternion.Inverse(weaponPivot.rotation) * (gripHand.position - weaponPivot.position) / gripUnit;
                float d = gripLocal.magnitude;
                lastGripDistanceSeen = d;
                if (d <= 0.0005f) gripWeight = 0f;
                else if (showcase)
                {
                    // menu: on the gun = at the distance the two-handed clips hold it, and not moving against the gun.
                    // The menu clip swings the hand out and back in past that distance (and close in over the drum)
                    // while it spins it
                    gripWeight = leftHandGrip * Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(gripWindow, gripWindow + gripFade, Mathf.Abs(d - gripAt)));
                    if (Application.isPlaying && Time.deltaTime > 1e-5f)
                    {
                        float rate = lastGripDistance > 0f ? Mathf.Abs(d - lastGripDistance) / Time.deltaTime : 0f;
                        gripWeight *= Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(gripStill, gripStill * 3f, rate));
                    }
                }
                // match: on the gun = anywhere nearer than the melee clips hold the free hand. The strict menu rule
                // dropped the hand whenever the run directions blended or the aim layer moved the arms (Tho, 28 Nov 2026:
                // a wide gap between hand and gun in a match)
                else gripWeight = leftHandGrip * Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(gripNear, gripFar, d));
                lastGripDistance = d;
                gripHandRotation = gripHand.rotation;
                gripDownNow = Vector3.zero;
                Vector3 fromClips, downFromClips;
                if (GripOfPlayingClips(out fromClips, out downFromClips)) { gripLocal = fromClips; gripDownNow = downFromClips; }
                else if (showcase && gripOnWeaponMenu.sqrMagnitude > 0f) { gripLocal = gripOnWeaponMenu; gripDownNow = gripDownMenu; }
                else if (gripOnWeapon.sqrMagnitude > 0f) { gripLocal = gripOnWeapon; gripDownNow = gripDown; }
            }
        }

        if (Application.isPlaying && gripBlendSpeed > 0f)
        {
            // a hand that is off the gun only goes back on once it has been at the grip for a moment: the drum spin
            // passes through the holding distance for two or three frames at a time
            holdTime = gripWeight > 0.5f ? holdTime + Time.deltaTime : 0f;
            if (showcase && gripSmooth >= 0f && gripSmooth <= 0.001f && holdTime < gripDelay) gripWeight = 0f;
            gripSmooth = gripSmooth < 0f ? gripWeight : Mathf.MoveTowards(gripSmooth, gripWeight, Time.deltaTime * gripBlendSpeed);
            gripWeight = gripSmooth;
        }

        lastGripWeight = gripWeight;
        ApplyWeaponScale();

        int k = keepOrientation != null ? keepOrientation.Length : 0;
        if (kept == null || kept.Length != k) kept = new Quaternion[k];
        for (int i = 0; i < k; i++) if (keepOrientation[i] != null) kept[i] = keepOrientation[i].rotation;

        Vector3 up = transform.up;
        if (pelvisLevel > 0f && pelvisBone != null)
        {
            int hn = hipBones != null ? hipBones.Length : 0;
            if (hipPos == null || hipPos.Length != hn) { hipPos = new Vector3[hn]; hipRot = new Quaternion[hn]; }
            for (int i = 0; i < hn; i++) if (hipBones[i] != null) { hipPos[i] = hipBones[i].position; hipRot[i] = hipBones[i].rotation; }
            Straighten(pelvisBone, pelvisBone.rotation * pelvisUpLocal, up, pelvisLevel);
            for (int i = 0; i < hn; i++) if (hipBones[i] != null) { hipBones[i].position = hipPos[i]; hipBones[i].rotation = hipRot[i]; }
            // the hips were just written in world space: the offset guard must see this value next frame
            if (offsetBones != null && offsetWritten != null)
                for (int i = 0; i < offsetBones.Length && i < offsetWritten.Length; i++) if (offsetBones[i] != null) offsetWritten[i] = offsetBones[i].localPosition;
        }
        Quaternion upperBack = upperBackBone != null ? upperBackBone.rotation : Quaternion.identity;
        Relax(waistBones, waistRest, waistRelax);
        if (upperBackBone != null && waistRelax > 0f && upperBackKeep > 0f) upperBackBone.rotation = Quaternion.Slerp(upperBackBone.rotation, upperBack, upperBackKeep);
        Relax(spineBones, spineRest, spineRelax);
        if (upright > 0f && uprightBone != null && uprightTo != null) Straighten(uprightBone, uprightTo.position - uprightBone.position, transform.TransformDirection(uprightRestDir), upright);
        Relax(collarBones, collarRest, shoulderRelax);
        Relax(kneeBones, kneeRest, kneeRelax);
        if (neckRelax > 0f && neckBone != null) neckBone.localRotation = Quaternion.Slerp(neckBone.localRotation, neckRest, neckRelax);
        if (headUpright > 0f && headBone != null) Straighten(headBone, headBone.rotation * headUpLocal, up, headUpright);

        for (int i = 0; i < k; i++) if (keepOrientation[i] != null) keepOrientation[i].rotation = kept[i];

        if (gripWeight > 0.001f)
        {
            if (showcase && showcasePose) ShowcasePose(gripLocal, gripWeight);
            else TwoHandStance(gripLocal, gripUnit, gripHandRotation, gripWeight);
        }
        if (wristMax < 90f && gripHand != null && leftArmRest != null && leftArmRest.Length >= 3)
        {
            Quaternion bend = Quaternion.Inverse(leftArmRest[2]) * gripHand.localRotation;
            float angle = Quaternion.Angle(Quaternion.identity, bend);
            if (angle > wristMax) gripHand.localRotation = leftArmRest[2] * Quaternion.Slerp(Quaternion.identity, bend, wristMax / angle);
        }
    }

    private void ShowcasePose(Vector3 gripLocal, float weight)
    {
        if (holdUpperArm == null || holdLowerArm == null || holdHand == null || gripUpperArm == null) return;
        Vector3 up = transform.up, forward = transform.forward, right = transform.right;
        // left arm: relaxed, a little lower than the model's rest pose
        if (leftArmRest != null && leftArmRest.Length >= 3 && gripLowerArm != null && gripHand != null)
        {
            gripUpperArm.localRotation = Quaternion.Slerp(gripUpperArm.localRotation, leftArmRest[0], weight);
            gripLowerArm.localRotation = Quaternion.Slerp(gripLowerArm.localRotation, leftArmRest[1], weight);
            gripHand.localRotation = Quaternion.Slerp(gripHand.localRotation, leftArmRest[2], weight);
            Vector3 arm = gripHand.position - gripUpperArm.position;
            Vector3 axis = Vector3.Cross(arm, -up);
            if (axis.sqrMagnitude > 1e-10f) gripUpperArm.rotation = Quaternion.AngleAxis(showcaseArmDown * weight, axis.normalized) * gripUpperArm.rotation;
        }
        // right arm: the hand in front of the hip, then the gun turned muzzle up
        Vector3 sr = holdUpperArm.position;
        float half = (sr - gripUpperArm.position).magnitude * 0.5f;
        Vector3 target = sr + (right * showcaseHand.x + up * showcaseHand.y + forward * showcaseHand.z) * half;
        target = Vector3.Lerp(holdHand.position, target, weight);
        // Tho's pick (28 Nov 2026): the hand keeps the orientation the clip gave it and is then turned so the gun points
        // up. Aiming with the forearm and straightening the wrist put the finger on the trigger but tipped the gun
        // backwards, which looked unnatural; the fingers do not have to sit on the trigger.
        Quaternion handRotation = holdHand.rotation;
        Reach(holdUpperArm, holdLowerArm, holdHand, target, target);
        holdHand.rotation = handRotation;
        if (showcaseWrist > 0f) holdHand.localRotation = Quaternion.Slerp(holdHand.localRotation, rightHandRest, showcaseWrist * weight);
        Vector3 barrel = weaponPivot.rotation * gripLocal;
        if (barrel.sqrMagnitude > 1e-12f)
        {
            Vector3 wanted = Quaternion.AngleAxis(showcaseOut, up) * (Quaternion.AngleAxis(-showcaseUp, right) * forward);
            Quaternion turn = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(barrel, wanted), weight);
            holdHand.rotation = turn * holdHand.rotation;
        }
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
        // where the palm goes: the clip's grip point, moved along the gun towards the muzzle
        Vector3 onGun = (gripLocal - gripDownNow) * (1f + handForward) + gripDownNow * gripDrop;
        Vector3 palmAt = weaponPivot.position + weaponPivot.rotation * onGun * (gripUnit * weaponScale);
        palmAt = Vector3.Lerp(gripHand.position, palmAt, weight);
        if (shoulderPush > 0f && gripCollar != null)
        {
            Vector3 reach = gripUpperArm.position - gripCollar.position;
            Vector3 toGun = palmAt - gripUpperArm.position;
            if (reach.sqrMagnitude > 1e-10f && toGun.sqrMagnitude > 1e-10f)
                gripCollar.rotation = Quaternion.FromToRotation(reach, reach + toGun.normalized * (reach.magnitude * shoulderPush * weight)) * gripCollar.rotation;
        }
        Vector3 shoulder = gripUpperArm.position;
        Vector3 back = palmAt - shoulder;
        back = back.sqrMagnitude > 1e-10f ? back.normalized : transform.forward;
        Vector3 target = palmAt;
        // the wrist is held back from that point along the forearm by the palm's length; the forearm's direction
        // depends on where the elbow ends up, so it is solved in a few rounds
        for (int round = 0; round < 3; round++)
        {
            float fore = (gripHand.position - gripLowerArm.position).magnitude;
            target = palmAt - back * (fore * palmOffset * weight);
            if (round == 0 && armStretch > 1f)
            {
                float length = (gripLowerArm.position - shoulder).magnitude + fore;
                float k = length > 1e-5f ? Mathf.Clamp((target - shoulder).magnitude / (length * reachBend), 1f, armStretch) : 1f;
                k = Mathf.Lerp(1f, k, weight);
                if (k > 1.001f)
                {
                    gripUpperArm.localScale *= k;
                    gripHand.localScale /= k;
                }
                fore = (gripHand.position - gripLowerArm.position).magnitude;
                target = palmAt - back * (fore * palmOffset * weight);
            }
            Reach(gripUpperArm, gripLowerArm, gripHand, target, weaponPivot.position);
            // elbow: swung about the shoulder - hand line towards the pole (the hand stays where it is)
            if (elbowForward > 0f)
            {
                Vector3 line = gripHand.position - shoulder;
                if (line.sqrMagnitude > 1e-10f)
                {
                    line.Normalize();
                    Vector3 elbow = Vector3.ProjectOnPlane(gripLowerArm.position - shoulder, line);
                    Vector3 pole = Vector3.ProjectOnPlane(transform.right * elbowPole.x + up * elbowPole.y + transform.forward * elbowPole.z, line);
                    if (elbow.sqrMagnitude > 1e-10f && pole.sqrMagnitude > 1e-10f)
                        gripUpperArm.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(elbow, pole, line) * elbowForward * weight, line) * gripUpperArm.rotation;
                }
            }
            Vector3 along = gripHand.position - gripLowerArm.position;
            if (along.sqrMagnitude > 1e-10f) back = along.normalized;
        }
        Vector3 wanted = target;
        // hand: the clip's orientation, straightened onto the forearm, then the forearm twisted so the palm turns up
        // under the gun (a twist about the elbow - hand line leaves the hand in place)
        gripHand.rotation = leftHandRotation;
        if (handStraight > 0f && leftArmRest != null && leftArmRest.Length >= 3)
            gripHand.localRotation = Quaternion.Slerp(gripHand.localRotation, leftArmRest[2], handStraight * weight);
        if (palmUp > 0f && palmLocal.sqrMagnitude > 0.5f)
        {
            Vector3 fore = gripHand.position - gripLowerArm.position;
            if (fore.sqrMagnitude > 1e-10f)
            {
                fore.Normalize();
                Vector3 palm = Vector3.ProjectOnPlane(gripHand.rotation * palmLocal, fore);
                Vector3 above = Vector3.ProjectOnPlane(up, fore);
                if (palm.sqrMagnitude > 1e-10f && above.sqrMagnitude > 1e-10f)
                {
                    float need = Vector3.SignedAngle(palm, above, fore) * palmUp;
                    float atElbow = Mathf.Clamp(need, -palmTwistMax, palmTwistMax);
                    gripLowerArm.rotation = Quaternion.AngleAxis(atElbow * weight, fore) * gripLowerArm.rotation;
                    float atWrist = Mathf.Clamp(need - atElbow, -wristTwistMax, wristTwistMax);
                    if (Mathf.Abs(atWrist) > 0.5f) gripHand.rotation = Quaternion.AngleAxis(atWrist * weight, fore) * gripHand.rotation;
                    lastPalmNeed = need;
                }
            }
        }
        lastGripGap = (gripHand.position - wanted).magnitude;
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

    private System.Collections.Generic.Dictionary<string, int> gripClipIndex;
    private readonly System.Collections.Generic.List<AnimatorClipInfo> playing = new System.Collections.Generic.List<AnimatorClipInfo>(8);

    /// <summary>Where the left hand holds the gun on the clips playing now (layer 0), blended by their weights.</summary>
    private bool GripOfPlayingClips(out Vector3 onWeapon, out Vector3 down)
    {
        onWeapon = Vector3.zero; down = Vector3.zero;
        if (gripClips == null || gripClipOnWeapon == null || gripClipDown == null || gripClips.Length == 0) return false;
        if (gripClipIndex == null)
        {
            gripClipIndex = new System.Collections.Generic.Dictionary<string, int>();
            for (int i = 0; i < gripClips.Length && i < gripClipOnWeapon.Length && i < gripClipDown.Length; i++) gripClipIndex[gripClips[i]] = i;
        }
        int index;
        if (clipHint != null)
        {
            if (!gripClipIndex.TryGetValue(clipHint.name, out index)) return false;
            onWeapon = gripClipOnWeapon[index]; down = gripClipDown[index];
            return true;
        }
        if (!animatorLooked) { animator = GetComponent<Animator>(); animatorLooked = true; }
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null || !Application.isPlaying) return false;
        playing.Clear();
        animator.GetCurrentAnimatorClipInfo(0, playing);
        float total = 0f;
        for (int i = 0; i < playing.Count; i++)
        {
            AnimationClip clip = playing[i].clip;
            if (clip == null || !gripClipIndex.TryGetValue(clip.name, out index)) continue;
            float w = playing[i].weight;
            onWeapon += gripClipOnWeapon[index] * w; down += gripClipDown[index] * w; total += w;
        }
        if (total < 0.05f) return false;
        onWeapon /= total; down /= total;
        return true;
    }

    private void ApplyOffsets()
    {
        if (offsetBones == null || offsets == null) return;
        int n = Mathf.Min(offsetBones.Length, offsets.Length);
        if (n == 0) return;
        if (!animatorLooked) { animator = GetComponent<Animator>(); animatorLooked = true; }
        // ragdoll (Animator off): physics owns the bones, they keep the offset they already have
        if (Application.isPlaying && animator != null && !animator.enabled) return;
        if (offsetWritten == null || offsetWritten.Length != n) { offsetWritten = new Vector3[n]; offsetSource = new Vector3[n]; for (int i = 0; i < n; i++) offsetWritten[i] = new Vector3(float.NaN, 0, 0); }
        for (int i = 0; i < n; i++)
        {
            Transform b = offsetBones[i];
            if (b == null) continue;
            // a bone no clip wrote this frame still holds what was written here: its source is used again
            Vector3 p = b.localPosition;
            if (p == offsetWritten[i]) p = offsetSource[i]; else offsetSource[i] = p;
            p += offsets[i];
            b.localPosition = p; offsetWritten[i] = p;
        }
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
