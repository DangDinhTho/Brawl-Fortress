using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindMachineBomb : Action
{
    public SharedVector3 storeTarget;
    private Transform currBomb;
    private Vector3 offset;

    public override void OnStart()
    {
        currBomb = ManvsMachineManager.instan.currentBomb;
        float angle = Random.Range(0f, 2 * Mathf.PI);
        offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
    }

    public override TaskStatus OnUpdate()
    {
        if (ManvsMachineManager.instan.wave == 1)
        {
            if (ManvsMachineManager.instan.bomb.state == FlagState.home)
            {
                if (Vector3.Distance(transform.position, storeTarget.Value) < 1f)
                {
                    return TaskStatus.Success;
                }
                else
                    return TaskStatus.Running;
            }


            if (ManvsMachineManager.instan.bomb.state == FlagState.dropped)
            {
                if (Vector3.Distance(transform.position, currBomb.position) < 1.25f)
                {
                    return TaskStatus.Success;
                }
                storeTarget.Value = currBomb.position;
                return TaskStatus.Running;
            }
        }


        if (Vector3.Distance(transform.position, currBomb.position) < 8f)
        {
            return TaskStatus.Success;
        }

        storeTarget.Value = currBomb.position + offset;
        return TaskStatus.Running;
    }
}
