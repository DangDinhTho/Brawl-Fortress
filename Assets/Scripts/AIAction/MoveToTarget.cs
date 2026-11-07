using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveToTarget : Action
{
    public SharedVector3 target;
    public SharedBool hasEnemy;
    public NavMeshAgent agent;
    public BotController botController;
    private float time;

    public override void OnStart()
    {
        agent.stoppingDistance = 0.1f;
        agent.SetDestination(target.Value);
        time = Random.Range(5f, 15f);
    }

    public override TaskStatus OnUpdate()
    {
        if (hasEnemy.Value) return TaskStatus.Failure;

        if(Vector3.Distance(transform.position, target.Value) < 0.5f || time <= 0)
        {
            if(botController.weaponType == WeaponType.Mele)
            {
                int r = Random.Range(0, 2);
                if(r == 0)
                {
                    botController.BuffWeapon(0.5f);
                    botController.SwitchWeapon(botController.bot.characterConfig.defaultWeapon);
                }
            }
            return TaskStatus.Success;
        }

        time -= Time.deltaTime;
        return TaskStatus.Running;
        //Debug.Log("success");
    }
}
