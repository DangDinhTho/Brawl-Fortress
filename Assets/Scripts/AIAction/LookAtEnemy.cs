using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtEnemy : Action
{
    public SharedBool hasEnemy;
    public SharedTransform enemy;
    private Vector3 target;

    public override TaskStatus OnUpdate()
    {
        if (hasEnemy.Value)
        {
            target = enemy.Value.position;
            target.y = transform.position.y;
            transform.LookAt(target);
            return TaskStatus.Running;
        }
        else
        {
            return TaskStatus.Failure;
        }
        
        //Debug.Log("success");
    }
}
