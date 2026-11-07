using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldBomb : Action
{
    public TF2Bot bot;
    public SharedVector3 storeTarget;
    private Transform target;
    private AtomBomb bomb;

    public override void OnStart()
    {
        target = ManvsMachineManager.instan.target;
        bomb = ManvsMachineManager.instan.bomb;
    }

    public override TaskStatus OnUpdate()
    {
        if (bomb.owner != null && bomb.owner.viewID == bot.viewID)
        {
            storeTarget.Value = target.position;
            return TaskStatus.Success;
        }

        return TaskStatus.Failure;
    }
}
