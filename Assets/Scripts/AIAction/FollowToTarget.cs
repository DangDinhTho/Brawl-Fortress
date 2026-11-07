using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowToTarget : Action
{
    public SharedVector3 target;
    public SharedBool hasEnemy;
    public NavMeshAgent agent;
    public BotController botController;
    public float stopDistance;

    public override void OnStart()
    {
        agent.stoppingDistance = 0.1f;
        agent.SetDestination(target.Value);
    }

    public override TaskStatus OnUpdate()
    {
        if (hasEnemy.Value) return TaskStatus.Failure;

        //if (Vector3.Distance(transform.position, target.Value) < stopDistance)
        //{
        //    if (botController.weaponType == WeaponType.Mele)
        //    {
        //        int r = Random.Range(0, 2);
        //        if (r == 0)
        //        {
        //            botController.BuffWeapon(0.5f);
        //            botController.SwitchWeapon(botController.bot.characterConfig.defaultWeapon);
        //        }
        //    }
        //    return TaskStatus.Success;
        //}

        agent.SetDestination(target.Value);
        return TaskStatus.Running;
        //Debug.Log("success");
    }
}
