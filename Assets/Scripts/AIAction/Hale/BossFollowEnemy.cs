using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossFollowEnemy : Action
{
    public SharedTransform enemy;
    public SharedBool hasEnemy;
    private NavMeshAgent agent;
    private BotController botController;
    private float time;
    private float maxTime;
    private bool rolled;
    private float attackRange;

    public override void OnAwake()
    {
        base.OnAwake();
        botController = GetComponent<BotController>();
        agent = GetComponent<NavMeshAgent>();
       
    }

    public override void OnStart()
    {
        attackRange = botController.currWeapon.weaponConfig.attackRange;
        agent.stoppingDistance = Mathf.Max(0.5f, attackRange - 1.2f); // walk well inside punching range: the big boss body must not stop it short
        agent.SetDestination(enemy.Value.position);
        maxTime = Random.Range(3f, 8f); // shorter chase cycles: more aggressive
        time = maxTime;
        rolled = false;
    }

    public override TaskStatus OnUpdate()
    {
        if (!hasEnemy.Value) return TaskStatus.Failure;

        float distance = Vector3.Distance(transform.position, enemy.Value.position);
        if (distance <= attackRange)
        {
            return TaskStatus.Success;
        }

        if (!rolled && distance - attackRange > 8f && time < maxTime - 0.5f)
        {
            rolled = true;
            botController.Roll();
            
        }

        time -= Time.deltaTime;

        agent.SetDestination(enemy.Value.position);
        return TaskStatus.Running;
        //Debug.Log("success");
    }
}