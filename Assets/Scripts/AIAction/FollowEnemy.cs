using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowEnemy : Action
{
    public SharedTransform enemy;
    public SharedBool hasEnemy;
    public NavMeshAgent agent;

    public override void OnStart()
    {
        agent.stoppingDistance = 8f;
        agent.SetDestination(enemy.Value.position);
    }

    public override TaskStatus OnUpdate()
    {
        if (!hasEnemy.Value) return TaskStatus.Failure;

        if (Vector3.Distance(transform.position, enemy.Value.position) <= 10f)
        {
            return TaskStatus.Success;
        }

        agent.SetDestination(enemy.Value.position);
        return TaskStatus.Running;
        //Debug.Log("success");
    }
}