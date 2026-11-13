using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class KothMoveFree : Action
{
    public SharedVector3 storeTarget;
    public SharedBool hasEnemy;
    public TF2Bot bot;
    private KingOfTheHillManager kingOfTheHillManager;
    public FieldOfView fieldOfView;

    public override void OnStart()
    {
        kingOfTheHillManager = KingOfTheHillManager.instan;
        storeTarget.Value = MapManager.instan.GetPatrolPoint();
    }

    public override TaskStatus OnUpdate()
    {
        if (fieldOfView.canSeePlayer)
        {
            hasEnemy.Value = true;
            storeTarget.Value = fieldOfView.targetInField.position;
            return TaskStatus.Success;
        }

        hasEnemy.Value = fieldOfView.canSeePlayer;
        if (KingOfTheHillManager.instan.teamOwner != bot.team)
        {
            storeTarget.Value = kingOfTheHillManager.kothPoint.transform.position;
            return TaskStatus.Failure;
        }
        

        if (Vector3.Distance(transform.position, storeTarget.Value) < 1)
        {
            storeTarget.Value = MapManager.instan.GetPatrolPoint();
            
        }
        
        return TaskStatus.Running;
    }
}
