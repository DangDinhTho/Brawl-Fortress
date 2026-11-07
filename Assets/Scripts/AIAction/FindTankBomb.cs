using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindTankBomb : Action
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
        if (Vector3.Distance(transform.position, currBomb.position) < 12f)
        {
            return TaskStatus.Success;
        }

        storeTarget.Value = currBomb.position + offset;
        return TaskStatus.Running;
    }
}
