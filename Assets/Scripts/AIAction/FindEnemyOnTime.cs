using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindEnemyOnTime : Conditional
{
    public FieldOfView fieldOfView;
    public SharedBool hasEnemy;
    public SharedTransform enemy;
    private float time;
    public float minTime;
    public float maxTime;


    public override void OnStart()
    {
        time = Random.Range(minTime, maxTime);
    }


    public override TaskStatus OnUpdate()
    {
        if (time <= 0) return TaskStatus.Success;


        hasEnemy.Value = fieldOfView.canSeePlayer;
        if (fieldOfView.canSeePlayer)
        {
            enemy.Value = fieldOfView.targetInField;
        }


        time -= Time.deltaTime;
        return TaskStatus.Running;
    }
}
