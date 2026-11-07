using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class FindFlag : Action
{
    public SharedVector3 storeTarget;
    private int caseAction;
    
    public override void OnStart()
    {
        caseAction = Random.Range(0, 3);
    }

    public override TaskStatus OnUpdate()
    {
        
        switch (caseAction)
        {
            case 0:
                storeTarget.Value = MapManager.instan.PathPoints[Random.Range(0, MapManager.instan.PathPoints.Length)].position;
                return TaskStatus.Success;
        }

        return TaskStatus.Success;
    }
}
