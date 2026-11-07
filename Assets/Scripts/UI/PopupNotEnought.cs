using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupNotEnought : PopupBase
{
    public void OnOpenStoreClick()
    {
        GUIManager.instan.CreatePopup<PopupStore>();
        Dismiss();
    }

    public void OnWatchAdsClick()
    {
        GUIManager.instan.CreatePopup<PopupConfirm>(LocalizationManager.Localize("watch_ads_confirm"), (Action)WatchAds,
            GUIManager.instan.iconAtlas.GetSprite("ads"));
        //Dismiss();
    }

    public void WatchAds()
    {
        //Yodo1AdsManager.instan.ShowGoldkeyHomeReward(OnShowRewardAdsDone);
        AdsManager.instan.ShowRewardAds(OnShowRewardAdsDone);
    }

    public void OnShowRewardAdsDone()
    {
        GameManager.instan.AddRewardFromAds();
        Dismiss();
    }
}
