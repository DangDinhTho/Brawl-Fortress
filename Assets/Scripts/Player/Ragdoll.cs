using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    Rigidbody[] rigidBodies;
    CharacterJoint[] characterJoints;
    Animator animator;
    //TPSLookAt tPSLookAt;
    public Rigidbody hipRigidbody;
    public float dieForce = 20;
    private RemotePlayer remotePlayer;

    private void Awake()
    {
        remotePlayer = GetComponent<RemotePlayer>();
        animator = GetComponent<Animator>();
        //tPSLookAt = GetComponent<TPSLookAt>();
        rigidBodies = GetComponentsInChildren<Rigidbody>();
        characterJoints = GetComponentsInChildren<CharacterJoint>();
       // IgnorePlayerCollider();
    }
    // Start is called before the first frame update
    void Start()
    {
        //IgnorePlayerCollider();

    }

   public void DeactivateRagdoll()
    {
        animator.enabled = true;
        foreach (var rigidBody in rigidBodies)
        {
            rigidBody.ResetCenterOfMass();
            rigidBody.isKinematic = true;
        }
        //if (!remotePlayer.player.isMainPlayer)
        //    tPSLookAt.enabled = true;
    }

    public void ActivateRagdoll(Vector3 direction)
    {
        //tPSLookAt.enabled = false;
        foreach (var characterJoin in characterJoints)
        {
            characterJoin.autoConfigureConnectedAnchor = false;
        }
        StartCoroutine(WaitActiveRagdoll(direction));
        
    }

    IEnumerator WaitActiveRagdoll(Vector3 direction)
    {
        yield return new WaitForSeconds(0.1f);
        
        foreach (var rigidBody in rigidBodies)
        {

            //var s = rigidBody.GetComponent<CharacterJoint>();
            //if (s)
            //    s.autoConfigureConnectedAnchor = false;
            rigidBody.isKinematic = false;
        }
        animator.enabled = false;
        ApplyForce(direction);
    }

    public void ApplyForce(Vector3 direc)
    {
        print("AddForce");
        hipRigidbody.AddForce(direc * dieForce, ForceMode.VelocityChange);
    }

    public void IgnorePlayerCollider()
    {

        //var characterJoints = GetComponentsInChildren<CharacterJoint>();
        //if (characterJoints != null && characterJoints.Length > 0)
        //{
        //    for (int i = 0; i < characterJoints.Length; i++)
        //    {
        //        characterJoints[i].autoConfigureConnectedAnchor = false;
        //    }
        //}
        DeactivateRagdoll();

        if(remotePlayer == null) remotePlayer = GetComponent<RemotePlayer>();

        GameObject p = remotePlayer.player.gameObject;
        if (p == null) return;

        var playerColliders = GetComponentsInChildren<Collider>();

        Collider Player = p.GetComponent<CharacterController>();
        Collider boxCollier = p.GetComponent<BoxCollider>();
        if (Player != null)
        {
            for (int i = 0; i < playerColliders.Length; i++)
            {
                var col = playerColliders[i];
                if (col == null) continue;

                Physics.IgnoreCollision(col, Player);
                if(boxCollier != null)
                {
                    Physics.IgnoreCollision(col, boxCollier);
                }
            }
        }

    }
}
