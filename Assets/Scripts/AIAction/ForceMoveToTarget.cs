using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ForceMoveToTarget : Action
{
    public SharedVector3 target;
    public NavMeshAgent agent;
    //public BotController botController;

    public override void OnStart()
    {
        agent.stoppingDistance = 0.1f;
        agent.SetDestination(target.Value);
    }

    public override TaskStatus OnUpdate()
    {

        if(Vector3.Distance(transform.position, target.Value) < 0.5f)
        {
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }
}
