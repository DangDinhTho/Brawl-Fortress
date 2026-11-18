using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossMoveToTarget : Action
{
    public SharedVector3 target;
    public SharedBool hasEnemy;
    private NavMeshAgent agent;
    private BotController botController;
    private float time;
    private float maxTime;
    private bool rolled;

    public override void OnAwake()
    {
        base.OnAwake();
        botController = GetComponent<BotController>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStart()
    {
        agent.stoppingDistance = 0.1f;
        agent.SetDestination(target.Value);
        maxTime = Random.Range(5f, 15f);
        time = maxTime;
        rolled = false;
    }

    public override TaskStatus OnUpdate()
    {
        if (hasEnemy.Value) return TaskStatus.Failure;

        float distance = Vector3.Distance(transform.position, target.Value);
        if (distance < 0.5f || time <= 0)
        {
            return TaskStatus.Success;
        }

        if(!rolled && distance > 6f && time < maxTime - 1)
        {
            rolled = true;
            botController.Roll();
            
        }

        time -= Time.deltaTime;
        return TaskStatus.Running;
        //Debug.Log("success");
    }
}
