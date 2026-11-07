using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TPSLookAt : MonoBehaviour
{
    public RemotePlayer remotePlayer;
    //public Transform target;
    public Transform aim;
    //private Character character;
    //public Transform bone;

    public HumanBone[] humanBones;
    public int iterations = 8;
    [Range(0, 1)]
    public float weight = 0.1f;

    //public Transform root;
    public Transform[] boneTransforms;

    public float angleLimit = 90.0f;
    public float distanceLimit = 1.5f;

    void Start()
    {
        //boneTransforms = new Transform[4];
        //for(int i = 0; i < 4; i++)
        //{
        //    boneTransforms[i] = FindInArray($"bip_spine_{i}");
        //}
    }

    Transform FindInArray(string name)
    {
        foreach (Transform t in transform)
        {
            if (t.gameObject.name == name)
                return t;
        }
        return null;
    }


    Vector3 GetTargetPosition()
    {
        Vector3 targetDirection = remotePlayer.player.targetLook - aim.position;
        Vector3 aimDirection = aim.forward;
        float blendOut = 0.0f;
        float targetAngle = Vector3.Angle(targetDirection, aimDirection);
        if(targetAngle > angleLimit)
        {
            blendOut += (targetAngle - angleLimit) / 50.0f;
        }

        Vector3 direction = Vector3.Slerp(targetDirection, aimDirection, blendOut);
        return aim.position + direction;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 target = GetTargetPosition();
        for (int i = 0; i < iterations; i++)
        {
            for (int j = 0; j < boneTransforms.Length; j++)
            {
                AimTarget(boneTransforms[j], target, weight);
            }
        }

        //if(character.characterController.alignBones!= null)
        //{
        //    character.characterController.alignBones.FollowBone();
        //}
    }

    //public void UpdateBone(Vector3 target)
    //{
    //    for (int i = 0; i < iterations; i++)
    //    {
    //        for (int j = 0; j < boneTransforms.Length; j++)
    //        {
    //            AimTarget(boneTransforms[j], target, weight);
    //        }
    //    }
    //}

    private void AimTarget(Transform bone, Vector3 targetPosition, float weight)
    {
        Vector3 targetDirection = targetPosition - aim.position;
        Quaternion aimTowards = Quaternion.FromToRotation(aim.forward, targetDirection);
        Quaternion blendedRotation = Quaternion.Slerp(Quaternion.identity, aimTowards, weight);
        bone.rotation = blendedRotation * bone.rotation;
    }
}
