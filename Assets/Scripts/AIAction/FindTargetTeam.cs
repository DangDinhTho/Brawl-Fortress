using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class FindTargetTeam : Action
{
    public SharedVector3 storeTarget;
    public BotController botController;


    public override TaskStatus OnUpdate()
    {
        Transform teamPos = MapManager.instan.teamPos[(int)botController.bot.team];
        storeTarget.Value = teamPos.GetChild(Random.Range(0, teamPos.childCount)).position;
        return TaskStatus.Success;
    }

}
