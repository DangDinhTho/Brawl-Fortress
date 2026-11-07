using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class WaitStartGame : Action
{
    public SharedInt caseAction;
    
    public override TaskStatus OnUpdate()
    {
        if (ArenaManager.Instance.gameState == GameState.Started) return TaskStatus.Failure;

        caseAction.Value = Random.Range(0, 10);
        return TaskStatus.Success;
    }
}
