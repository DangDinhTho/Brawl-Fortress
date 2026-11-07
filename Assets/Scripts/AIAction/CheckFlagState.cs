using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class CheckFlagState : Action
{
    public BotController botController;
    public SharedVector3 storeTarget;
    public SharedBool hasEnemy;
    private FlagController teamFlag;
    private FlagController enemyFlag;
    public FieldOfView fieldOfView;
    public SharedTransform enemy;
    public SharedBool igroneEnemy;
    
    private int team;
    private int enemyTeam;
    private Vector3 homePosition;

    public override void OnStart()
    {
        team = (int)botController.bot.team;
        enemyTeam = (int)ConfigManager.GetEnemyTeam(botController.bot.team);
        teamFlag = CaptureTheFlagManager.instan.flagControllers[team];
        enemyFlag = CaptureTheFlagManager.instan.flagControllers[enemyTeam];
        homePosition = CaptureTheFlagManager.instan.homePositions[team];
        hasEnemy.Value = false;
    }

    public override TaskStatus OnUpdate()
    {
        if (enemyFlag.state == FlagState.moving)
        {
            if (enemyFlag.owner.viewID == botController.bot.viewID)
            {
                storeTarget.Value = homePosition;
                hasEnemy.Value = false;
                return TaskStatus.Running;
            }
            else
            {
                
                if (teamFlag.state == FlagState.home || teamFlag.state == FlagState.dropped)
                {
                    //if(botController.bot.healthManager.currentHealth <= botController.bot.healthManager.maxHealth * 0.3f)
                    //{
                    //    storeTarget.Value = MapManager.instan.PathPoints[Random.Range(0, MapManager.instan.PathPoints.Length)].position;
                    //    igroneEnemy.Value = false;
                    //    return TaskStatus.Success;
                    //}

                    hasEnemy.Value = fieldOfView.canSeePlayer;
                    if (fieldOfView.canSeePlayer)
                    {
                        enemy.Value = fieldOfView.targetInField;
                        igroneEnemy.Value = false;
                        return TaskStatus.Success;
                    }

                    storeTarget.Value = enemyFlag.transform.position;
                    return TaskStatus.Running;
                }
            }
            
        }

        if (teamFlag.state == FlagState.moving)
        {
            float distanceToTeamFlag = Vector3.Distance(transform.position, teamFlag.transform.position);
            if (distanceToTeamFlag <
                Vector3.Distance(transform.position, enemyFlag.transform.position))
            {
                storeTarget.Value = teamFlag.transform.position;
                if (distanceToTeamFlag < 20f)
                {
                    igroneEnemy.Value = false;
                    return TaskStatus.Success;
                }

                else
                {
                    hasEnemy.Value = true;
                    enemy.Value = teamFlag.owner.transform;
                    return TaskStatus.Running;
                }
            }
            
        }
        
        if(botController.bot.healthManager.currentHealth <= botController.bot.healthManager.maxHealth * 0.3f)
        {
            storeTarget.Value = enemyFlag.transform.position;
            hasEnemy.Value = false;
            return TaskStatus.Running;
        }


        int rand = Random.Range(0, 3);
        if (rand == 0)
        {
            storeTarget.Value = MapManager.instan.PathPoints[Random.Range(0, MapManager.instan.PathPoints.Length)].position;
            igroneEnemy.Value = false;
            return TaskStatus.Success;
        }
        
        storeTarget.Value = enemyFlag.transform.position;
        hasEnemy.Value = false;
        igroneEnemy.Value = true;
        return TaskStatus.Success;

    }
}
