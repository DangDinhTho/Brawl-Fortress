using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentryLookAt : MonoBehaviour
{
    public Transform target;
    public Transform aim;
    //public Transform bone;

    public int iterations = 8;
    [Range(0, 1)]
    public float weight = 0.1f;

    public Transform boneTransformsX;
    public Transform boneTransformsY;

    public Transform targetTruc, aimTruc;
    public Transform[] trucBone;
    [Range(0, 1)]
    public float weight2;
    //public GameObject[] offWhenBuiled;
    //public Transform[] shootPoints;
    public bool isSentryLevel1;

    public float angleLimit = 70.0f;
    public float distanceLimit = 0.5f;



    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 _target = GetTargetPosition();
        AimTargetX(boneTransformsX, _target, weight);
        //if (Vector3.Distance(aim.position, target) < 0.2f) return;
        for (int i = 0; i < iterations; i++)
        {

            AimTargetY(boneTransformsY, _target, weight);

            if (isSentryLevel1)
            {
                float w = weight2 * Mathf.Clamp(aim.position.y - _target.y, 1, 5);
                AimTargetTruc(trucBone[0], aimTruc, targetTruc.position, w);
                AimTargetTruc(trucBone[1], aimTruc, targetTruc.position, weight);
            }
        }


    }

    Vector3 GetTargetPosition()
    {
        float hight = Mathf.Abs(target.position.y - aim.position.y);
        float distance = Vector3.Distance(target.position, aim.position);
        float blendOut = 0.0f;
        float targetAngle = Mathf.Asin(hight / distance) * Mathf.Rad2Deg;
        if (targetAngle > angleLimit)
        {
            blendOut += (targetAngle - angleLimit) / 50.0f;
        }
        Vector3 targetDirection = target.position - aim.position;
        Vector3 aimDirection = aim.forward;
        Vector3 direction = Vector3.Slerp(targetDirection, aimDirection, blendOut);
        return aim.position + direction;
    }

    private void AimTargetX(Transform boneX, Vector3 targetPosition, float weight)
    {
        targetPosition.y = boneX.position.y;
        boneX.LookAt(targetPosition);
        //Vector3 targetDirection = targetPosition - aim.position;
        //Quaternion aimTowards = Quaternion.FromToRotation(aim.forward, targetDirection);
        //Quaternion blendedRotation = Quaternion.Slerp(Quaternion.identity, aimTowards, weight);
        //boneX.rotation = blendedRotation * boneX.rotation;
    }

    private void AimTargetY(Transform boneY, Vector3 targetPosition, float weight)
    {
        Vector3 targetDirection = targetPosition - aim.position;
        Quaternion aimTowards = Quaternion.FromToRotation(aim.forward, targetDirection);
        Quaternion blendedRotation = Quaternion.Slerp(Quaternion.identity, aimTowards, weight);
        boneY.rotation = blendedRotation * boneY.rotation;
    }

    private void AimTargetTruc(Transform bone, Transform aimTruc, Vector3 targetPosition, float weight)
    {
        Vector3 targetDirection = targetPosition - aimTruc.position;
        Quaternion aimTowards = Quaternion.FromToRotation(aimTruc.forward, targetDirection);
        Quaternion blendedRotation = Quaternion.Slerp(Quaternion.identity, aimTowards, weight);
        bone.rotation = blendedRotation * bone.rotation;
    }
}
