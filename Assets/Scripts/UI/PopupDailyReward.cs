using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupDailyReward : PopupBase
{
    public UIDailyRewardItem[] dailyRewardItems;

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        for (int i = 0; i < 7; i++)
        {
            dailyRewardItems[i].Init(ConfigManager.dailyRewards[i], GameManager.instan.userData.mocDaNhan);
        }
    }
}
