using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITransitionItem : MonoBehaviour
{
    public UIItem sourceItem;
    public UIItem targetItem;
    public GameObject adsItem;
    private Transition transition;
    private bool traing;

    public void SetTransitionData(Transition transition)
    {
        this.transition = transition;
        if (transition.sourceCurrency != null)
        {
            sourceItem.SetCurrency(transition.sourceCurrency);
        }
        else if (transition.sourceItem != null)
        {
            sourceItem.SetItem(transition.sourceItem);
        }
        
        targetItem.SetCurrency(transition.targetCurrency);
        
        adsItem.SetActive(!GameManager.instan.userData.ads_removed);
        
        traing = false;
    }

    public void TradingClick()
    {
        if(traing) return;
        if (transition.sourceCurrency != null)
        {
            if (!GameManager.instan.userData.IsReduceableCurrency(transition.sourceCurrency))
            {
                GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("not_enough_resource"));
                return;
            }
        }
        else if (transition.sourceItem != null)
        {
            if (GameManager.instan.userData.GetUserItem(transition.sourceItem.itemId) == null)
            {
                GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("not_enough_resource"));
                return;
            }
        }
        GUIManager.instan.CreatePopup<PopupConfirm>(LocalizationManager.Localize("trading_confirm"), (Action)ConfirmTrading);
    }

    public void ConfirmTrading()
    {
        
        traing = true;
        //Yodo1AdsManager.instan.ShowTradingReward(Trading);
        AdsManager.instan.ShowRewardAds(Trading);
    }

    public void Trading()
    {
        GameManager.instan.RequestTransition(transition, OnTrasitionResponse);
    }

    public void OnTrasitionResponse(RewardData rewardData)
    {
        traing = false;
        GUIManager.instan.CreatePopup<PopupReward>(rewardData);
    }
}
