using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FindBombCart : Action
{
    public SharedVector3 storeTarget;
    private Transform bombCart;
    private Vector3 offset;
    public FieldOfView fieldOfView;

    public override void OnStart()
    {
        bombCart = PayloadManager.instance.bombCart;
        float angle = Random.Range(0f, 2 * Mathf.PI);
        offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
    }

    public override TaskStatus OnUpdate()
    {
        float distanceFromBomb = Vector3.Distance(transform.position, bombCart.position);
        if ((distanceFromBomb <= 1.25f) || (distanceFromBomb <= 24f && fieldOfView.canSeePlayer))
        {
            return TaskStatus.Success;
        }

        storeTarget.Value = bombCart.position + offset;
        return TaskStatus.Running;
    }
}
