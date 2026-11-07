using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime;

public class FindRandomTarget : Action
{
    public SharedVector3 storeTarget;
    public SharedBool hasEnemy;

    public override TaskStatus OnUpdate()
    {
        if(hasEnemy.Value) return TaskStatus.Failure;
        storeTarget.Value = MapManager.instan.PathPoints[Random.Range(0, MapManager.instan.PathPoints.Length)].position;
        return TaskStatus.Success;
    }

}
