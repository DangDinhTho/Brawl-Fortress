using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Death ragdoll. Stable setup (no twisting / jittering bodies):
///  - bones collide with each other and the world (only directly jointed bones ignore each other, Unity default);
///  - bones ignore every other collider of their own character (bot capsule, hit boxes, CharacterController...);
///  - joints use projection so they cannot stretch, rigidbodies interpolate, more solver iterations, limited
///    depenetration speed;
///  - the death push is applied to every bone (the body flies as one piece instead of the pelvis being yanked away).
/// </summary>
public class Ragdoll : MonoBehaviour
{
    Rigidbody[] rigidBodies;
    CharacterJoint[] characterJoints;
    Collider[] boneColliders;
    Animator animator;
    public Rigidbody hipRigidbody;
    [Tooltip("Death push (velocity change, scaled by the hit direction length)")]
    public float dieForce = 20;
    [Tooltip("Share of dieForce given to every bone (the whole body moves together)")]
    [Range(0f, 1f)] public float pushScale = 0.5f;
    [Tooltip("Max sideways speed of the death push (m/s)")] public float maxPushSide = 6f;
    [Tooltip("Max upward speed of the death push (m/s): the body must not fly off the screen")] public float maxPushUp = 2.5f;
    const float SettleTime = 1.5f, MaxBoneSpeed = 10f, MaxBoneRise = 4f;
    float activatedAt;
    Vector3[] bindPos; Quaternion[] bindRot;
    private RemotePlayer remotePlayer;
    /// <summary>True while the body is simulated: nothing may write the bone transforms then.</summary>
    public bool IsActive { get; private set; }

    private void Awake()
    {
        remotePlayer = GetComponent<RemotePlayer>();
        animator = GetComponent<Animator>();
        rigidBodies = GetComponentsInChildren<Rigidbody>(true);
        characterJoints = GetComponentsInChildren<CharacterJoint>(true);
        var cols = new List<Collider>();
        foreach (var rb in rigidBodies) cols.AddRange(rb.GetComponents<Collider>());
        boneColliders = cols.ToArray();
        bindPos = new Vector3[rigidBodies.Length]; bindRot = new Quaternion[rigidBodies.Length];
        for (int i = 0; i < rigidBodies.Length; i++) { bindPos[i] = rigidBodies[i].transform.localPosition; bindRot[i] = rigidBodies[i].transform.localRotation; }

        foreach (var j in characterJoints)
        {
            j.enableProjection = true;
            j.projectionDistance = 0.05f;
            j.projectionAngle = 10f;
            j.enablePreprocessing = false;
        }
        foreach (var rb in rigidBodies)
        {
            // NO interpolation while alive: a kinematic interpolated body writes its old physics pose over the
            // animated bone, so after a respawn / teleport the ragdoll bones stayed behind and stretched the skin
            rb.interpolation = RigidbodyInterpolation.None;
            rb.solverIterations = 12;
            rb.solverVelocityIterations = 4;
            rb.maxDepenetrationVelocity = 3f;
            rb.angularDamping = Mathf.Max(rb.angularDamping, 0.5f);
        }
    }

    public void DeactivateRagdoll()
    {
        IsActive = false;
        // respawn: put every simulated bone back where the skeleton expects it (no stretched first frame)
        if (bindPos != null)
            for (int i = 0; i < rigidBodies.Length; i++) { rigidBodies[i].transform.localPosition = bindPos[i]; rigidBodies[i].transform.localRotation = bindRot[i]; }
        if (animator) animator.enabled = true;
        foreach (var rigidBody in rigidBodies)
        {
            rigidBody.interpolation = RigidbodyInterpolation.None;
            rigidBody.isKinematic = true;
            rigidBody.ResetCenterOfMass();
            // the physics pose follows the bone again (it would otherwise keep the place of death)
            rigidBody.position = rigidBody.transform.position;
            rigidBody.rotation = rigidBody.transform.rotation;
        }
        Physics.SyncTransforms();
    }

    public void ActivateRagdoll(Vector3 direction)
    {
        IgnoreOwnColliders(); // the owner may have gained colliders since spawn (weapons, pickups)
        StartCoroutine(WaitActiveRagdoll(direction));
    }

    IEnumerator WaitActiveRagdoll(Vector3 direction)
    {
        yield return new WaitForSeconds(0.1f);
        if (animator) animator.enabled = false;
        IsActive = true;
        activatedAt = Time.time;
        RebaseJoints();
        foreach (var rigidBody in rigidBodies)
        {
            rigidBody.position = rigidBody.transform.position; // start the simulation from the pose of death
            rigidBody.rotation = rigidBody.transform.rotation;
            rigidBody.isKinematic = false; // velocities can only be set on dynamic bodies
            rigidBody.interpolation = RigidbodyInterpolation.Interpolate; // smooth only while simulated
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }
        ApplyForce(direction);
    }

    /// <summary>Joints skip bones (spine_2 hangs on the pelvis over spine_0/1, the arms on spine_2 over the clavicles), so
    /// the anchors stored at spawn do not match an animated pose: the body was yanked back to the spawn pose when
    /// physics started. Recompute every anchor and the joint frame from the pose at the moment of death.</summary>
    void RebaseJoints()
    {
        foreach (var j in characterJoints)
        {
            var cb = j.connectedBody;
            if (cb == null) continue;
            j.autoConfigureConnectedAnchor = false;
            j.connectedAnchor = cb.transform.InverseTransformPoint(j.transform.TransformPoint(j.anchor));
            j.connectedBody = null; // re-assigning re-initialises the joint frame (limits) from the current pose
            j.connectedBody = cb;
        }
    }

    public void ApplyForce(Vector3 direc)
    {
        Vector3 v = direc * dieForce * pushScale;
        // damage sources pass directions of very different lengths (explosions...): cap the push
        Vector3 side = new Vector3(v.x, 0f, v.z);
        if (side.magnitude > maxPushSide) side = side.normalized * maxPushSide;
        v = side + Vector3.up * Mathf.Clamp(v.y, 0f, maxPushUp);
        foreach (var rb in rigidBodies)
            if (!rb.isKinematic) rb.AddForce(v, ForceMode.VelocityChange);
    }

    /// <summary>Safety net for the first moments: no bone flies off (huge hit, depenetration from the ground...).</summary>
    void FixedUpdate()
    {
        if (!IsActive || Time.time - activatedAt > SettleTime) return;
        foreach (var rb in rigidBodies)
        {
            if (rb.isKinematic) continue;
            var vel = rb.linearVelocity;
            if (vel.y > MaxBoneRise) vel.y = MaxBoneRise;
            if (vel.magnitude > MaxBoneSpeed) vel = vel.normalized * MaxBoneSpeed;
            rb.linearVelocity = vel;
        }
    }

    /// <summary>Called when the character is set up: ragdoll off, bones ignore the character's own colliders.</summary>
    public void IgnorePlayerCollider()
    {
        DeactivateRagdoll();
        IgnoreOwnColliders();
    }

    void IgnoreOwnColliders()
    {
        if (remotePlayer == null) remotePlayer = GetComponent<RemotePlayer>();
        if (remotePlayer == null || remotePlayer.player == null) return;
        var bones = new HashSet<Collider>(boneColliders);
        foreach (var other in remotePlayer.player.GetComponentsInChildren<Collider>(true))
        {
            if (other == null || bones.Contains(other)) continue;
            for (int i = 0; i < boneColliders.Length; i++)
                if (boneColliders[i] != null) Physics.IgnoreCollision(boneColliders[i], other, true);
        }
    }
}
