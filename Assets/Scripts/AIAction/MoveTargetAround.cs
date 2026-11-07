using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class MoveTargetAround : Action
{
    public SharedVector3 target;
    public NavMeshAgent agent;
    public float maxTime;
    private float time;
    public float distance = 0.2f;

    public override void OnStart()
    {
        agent.stoppingDistance = distance - 0.1f;
        agent.SetDestination(target.Value);
        time = maxTime;
    }

    public override TaskStatus OnUpdate()
    {
    //    if (hasEnemy.Value)
    //    {
    //        transform.LookAt(enemy.Value);
    //    }

        if(Vector3.Distance(transform.position, target.Value) <= distance || time <= 0)
        {
            return TaskStatus.Success;
        }

        time -= Time.deltaTime;
        return TaskStatus.Running;
        //Debug.Log("success");
    }
}
