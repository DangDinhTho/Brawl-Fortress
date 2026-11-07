using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class FindTargetAround : Action
{
    private int WALKABLE_MASK = 1 << NavMesh.GetAreaFromName("Walkable");
    public SharedVector3 storeTarget;
    public NavMeshAgent agent;
    //public bool checkEnemy;
    //public SharedBool hasEnemy;
    
    public override TaskStatus OnUpdate()
    {
        //if (checkEnemy && !hasEnemy.Value)
        //{
        //    return TaskStatus.Failure;
        //}
        
        Vector3 target = transform.position;
        float x = Random.Range(-2f, 2f);
        float z = Random.Range(-2f, 2f);
        target.x += x;
        target.z += z;
        if (CheckPoint(ref target))
        {
            storeTarget.Value = target;
        }
        return TaskStatus.Success;
    }

    public bool CheckPoint(ref Vector3 point)
    {
        var filter = new NavMeshQueryFilter() { agentTypeID = agent.agentTypeID, areaMask = WALKABLE_MASK };
        NavMeshHit hit;
        if (NavMesh.SamplePosition(point, out hit, 0.3f, filter))
        {
            point = hit.position;
            return true;
        }

        return false;
    }
}
