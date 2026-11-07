using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupLeaderboard : PopupBase
{
    public Transform leaderboardContainer;
    public UILeaderboardItem ownerLeaderboard;

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        GameManager.instan.GetLeaderBoard(InitData);
        GameManager.instan.GetMyLeaderBoard(SetOwnerData);
    }

    public void InitData(GetLeaderboardResult result)
    {
        int index = 0;
        foreach (var item in result.Leaderboard)
        {
            UILeaderboardItem it = GUIManager.instan.CreateListItem<UILeaderboardItem>(leaderboardContainer, index);

            it.SetData(item);
            index++;
        }

        GUIManager.instan.CleanListItem(leaderboardContainer, index);
    }

    public void SetOwnerData(PlayerLeaderboardEntry entry)
    {
        ownerLeaderboard.SetData(entry);
    }
}
