using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindEnemyUntillEmpty : Conditional
{
    public FieldOfView fieldOfView;
    public SharedBool hasEnemy;
    public SharedTransform enemy;
    public BotController botController;

    public override TaskStatus OnUpdate()
    {

        if (botController.bot.healthManager.currentHealth <= botController.bot.healthManager.maxHealth * 0.3f)
        {
            hasEnemy.Value = false;
            return TaskStatus.Failure;
        }

        hasEnemy.Value = fieldOfView.canSeePlayer;
        if (fieldOfView.canSeePlayer)
        {
            enemy.Value = fieldOfView.targetInField;
            return TaskStatus.Running;
        }
        else
        {
            return TaskStatus.Success;
        }

        
    }
}
