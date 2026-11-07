using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowKothPoint : Action
{
    public SharedVector3 storeTarget;
    private Vector3 offset;
    public TF2Bot bot;
    private KingOfTheHillManager kingOfTheHillManager;
    private float durationTime;
    public SharedBool hasEnemy;
    public FieldOfView fieldOfView;

    public override void OnStart()
    {
        durationTime = 0;
        kingOfTheHillManager = KingOfTheHillManager.instan;
        
    }

    public override TaskStatus OnUpdate()
    {
        float distance = Vector3.Distance(transform.position, kingOfTheHillManager.kothPoint.transform.position);
        if (kingOfTheHillManager.teamOwner != bot.team)
        {
            if (distance < 1.5f)
            {
                return TaskStatus.Success;
            }
            hasEnemy.Value = false;
            storeTarget.Value = kingOfTheHillManager.kothPoint.transform.position;
            return TaskStatus.Running;
        }

        if (distance > 1.5f)
            return TaskStatus.Success;
        else
        {
            hasEnemy.Value = fieldOfView.canSeePlayer;

            if (hasEnemy.Value) return TaskStatus.Success;
            if (durationTime <= 0)
            {
                float angle = Random.Range(0f, 2 * Mathf.PI);
                offset = new Vector3(Mathf.Cos(angle) * 0.5f, 0, Mathf.Sin(angle) * 0.5f);
                storeTarget.Value = kingOfTheHillManager.kothPoint.transform.position + offset;
                durationTime = Random.Range(2f, 5f);
            }

            durationTime -= Time.deltaTime;
        }

        
        return TaskStatus.Running;
    }
}
