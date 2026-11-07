using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTFMoveIgnoreEnemy : Action
{
    public BotController botController;
    private FlagController enemyFlag;

    private float time;
    public float minTime;
    public float maxTime;

    private int enemyTeam;

    public override void OnStart()
    {
        enemyTeam = (int)ConfigManager.GetEnemyTeam(botController.bot.team);
        enemyFlag = CaptureTheFlagManager.instan.flagControllers[enemyTeam];
        time = Random.Range(minTime, maxTime);
        
    }

    public override TaskStatus OnUpdate()
    {
        if (time <= 0) return TaskStatus.Success;

        if (enemyFlag.state == FlagState.moving)
        {

            return TaskStatus.Success;

        }

        time -= Time.deltaTime;
        return TaskStatus.Running;
    }
}