using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class PlayTaunt : Action
{
    public BotController botController;
    private bool endTaunt;
    
    public override void OnStart()
    {
        endTaunt = false;
        List<Taunt> taunts = new List<Taunt>();
        for (Taunt taunt = Taunt.None; taunt < Taunt.Max; taunt++)
        {
            if (taunt.ToString().StartsWith(botController.bot.tF2Class.ToString()))
            {
                taunts.Add(taunt);
            }
        }
        int ranTaunt = Random.Range(0, taunts.Count);
        botController.bot.remotePlayer.PlayTaunt(taunts[ranTaunt], (System.Action)OnEndTaunt);
        TauntConfig config = ConfigManager.GetTauntConfig(taunts[ranTaunt]);
        if (config.loop)
        {
            StartCoroutine(WaitEndTaunt(Random.Range(8f, 15f)));
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (endTaunt)
        {
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }

    IEnumerator WaitEndTaunt(float time)
    {
        yield return new WaitForSeconds(time);
        botController.bot.remotePlayer.EndTaunt();
    }

    public void OnEndTaunt()
    {
        endTaunt = true;
    }
}
