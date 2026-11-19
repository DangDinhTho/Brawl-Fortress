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
        SetStopDistance();
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
            SetStopDistance();
            botController.Fire();
            attacked = true;
        }

        

        return TaskStatus.Running;

    }

    /// <summary>Normal bots stop at the per-weapon-type distance. A boss is far bigger than a player: it never gets that
    /// close (it only shoves the target), so it attacks from its weapon's attackRange and stops a bit inside it.</summary>
    /// <summary>Called by Behavior Designer whenever this task ends or is aborted (the tree switched to patrolling,
    /// another target, a pickup...): release the trigger, otherwise a flamethrower / minigun keeps firing while walking.</summary>
    public override void OnEnd()
    {
        if (attacked || (botController.currWeapon != null && botController.currWeapon.fire))
            botController.FinishFire();
        attacked = false;
    }

    void SetStopDistance()
    {
        distanceStop = ConfigManager.stopDistances[botController.weaponType];
        if (botController.bot is TF2Boss && botController.currWeapon != null && botController.currWeapon.weaponConfig != null)
        {
            distanceStop = Mathf.Max(distanceStop, botController.currWeapon.weaponConfig.attackRange);
            agent.stoppingDistance = distanceStop * 0.85f;
            return;
        }
        // The per-slot distance (7.5 m for every gun) is too far for short guns: the bot fires up to 1.2 x the stop distance,
        // so a scattergun / shotgun / flamethrower bot stood out of range shooting at nothing. Stop inside this weapon's reach.
        var w = botController.currWeapon;
        var cfg = w != null ? w.weaponConfig : null;
        if (cfg != null && cfg.range > 0f)
        {
            float reach = cfg.range, nozzle = 0f;
            // flamethrower: the flame starts at the nozzle, well in front of the body, and is 'range' long
            if (w is Flamethrower ft && ft.shotPoints != null && ft.shotPoints.Length > 0 && ft.shotPoints[0] != null)
            {
                Vector3 rel = ft.shotPoints[0].position - transform.position; rel.y = 0f;
                nozzle = rel.magnitude;
                reach += nozzle;
            }
            if (cfg.bulletId == BulletId.None) distanceStop = reach * 0.6f;                                    // melee: swing at ~0.72 x reach
            else if (nozzle > 0f) distanceStop = Mathf.Clamp(reach * 0.7f, nozzle + 0.4f, reach * 0.8f);  // target inside the flame
            else distanceStop = Mathf.Min(distanceStop, reach * 0.8f);                                     // guns: fire within 0.96 x range
        }
        agent.stoppingDistance = distanceStop;
    }
}
