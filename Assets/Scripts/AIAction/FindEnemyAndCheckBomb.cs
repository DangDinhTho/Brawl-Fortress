using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class FindEnemyAndCheckBomb : Action
{
    public FieldOfView fieldOfView;
    public SharedBool hasEnemy;
    public SharedTransform enemy;
    public BotController botController;
    private AtomBomb atomBomb;

    public override void OnStart()
    {
        atomBomb = ManvsMachineManager.instan.bomb;
    }

    public override TaskStatus OnUpdate()
    {
        if (ManvsMachineManager.instan.wave == 2) return TaskStatus.Failure;

        if(botController.bot.healthManager.currentHealth <= botController.bot.healthManager.maxHealth * 0.2f)
        {
            hasEnemy.Value = false;
            return TaskStatus.Success;
        }

        if (ManvsMachineManager.instan.wave == 1 && atomBomb.state == FlagState.moving)
        {
            if (Vector3.Distance(transform.position, atomBomb.transform.position) < 100)
            {
                hasEnemy.Value = true;
                enemy.Value = ManvsMachineManager.instan.currentBomb;
                return TaskStatus.Running;
            }
            
        }


        hasEnemy.Value = fieldOfView.canSeePlayer;
        if (fieldOfView.canSeePlayer)
        {
            enemy.Value = fieldOfView.targetInField;
        }

        return TaskStatus.Running;
    }
}
