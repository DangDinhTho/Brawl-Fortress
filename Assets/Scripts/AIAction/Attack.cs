using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Attack : Action
{
    public SharedBool hasEnemy;
    public SharedTransform enemy;
    public NavMeshAgent agent;
    public BotController botController;
    private bool attacked;
    private float distanceStop;
    private Vector3 target;

    public override void OnStart()
    {
        distanceStop = ConfigManager.stopDistances[botController.weaponType];
        agent.stoppingDistance = distanceStop;
        attacked = false;
    }

    public override TaskStatus OnUpdate()
    {
        //target = enemy.Value.position;
        //target.y = transform.position.y;
        //transform.LookAt(target);

        if (!hasEnemy.Value)
        {
            if (botController.currWeapon.fire || attacked)
            {
                attacked = false;
                botController.FinishFire();
            }
            return TaskStatus.Failure;
        }

        if (botController.fpsState == PlayerFPState.Reloading)
        {
            if (botController.currWeapon.fire || attacked)
            {
                attacked = false;
                botController.FinishFire();
            }
            return TaskStatus.Running;
        }

        if (Vector3.Distance(transform.position, enemy.Value.position) > distanceStop  * 1.2f)
        {
            if (botController.currWeapon.fire || attacked)
            {
                attacked = false;
                botController.FinishFire();
            }
            return TaskStatus.Failure;
        }

        if (botController.blockAttack && (botController.currWeapon.fire || attacked))
        {
            attacked = false;
            botController.FinishFire();
            return TaskStatus.Running;
        }

        if (!botController.currWeapon.fire && !botController.blockAttack && !attacked)
        {
            distanceStop = ConfigManager.stopDistances[botController.weaponType];
            agent.stoppingDistance = distanceStop;
            botController.Fire();
            attacked = true;
        }

        

        return TaskStatus.Running;

    }
}
