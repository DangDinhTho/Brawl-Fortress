using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class UpdateAimTarget : Action
{
    public SharedBool hasEnemy;
    public SharedTransform enemy;
    public TF2Bot bot;

    //public override TaskStatus OnUpdate()
    //{
    //    if (hasEnemy.Value)
    //    {
    //        bot.targetLook = enemy.Value.position + Vector3.up;
    //    }
    //    else
    //        bot.targetLook = bot.transform.forward * 2 + bot.remotePlayer.tPSLookAt.aim.position;

    //    return TaskStatus.Running;
    //}
}
