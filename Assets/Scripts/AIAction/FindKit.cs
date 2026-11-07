using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FindKit : Action
{
    public BotController botController;
    public SharedVector3 storeTarget;
    private float delayToPickup;

    public override void OnStart()
    {
        delayToPickup = Random.Range(5f, 15f);
        //agent.SetDestination(enemy.Value.position);
    }

    public override TaskStatus OnUpdate()
    {
        if (delayToPickup <= 0)
        {
            //if (botController.weaponType == WeaponType.Mele)
            //{
            //    botController.BuffWeapon(0.5f);
            //}
            if (botController.bot.healthManager.currentHealth <= 50)
                botController.bot.healthManager.Buff(0.5f);
            return TaskStatus.Success;
        }
        delayToPickup -= Time.deltaTime;
        return TaskStatus.Running;
    }
}
