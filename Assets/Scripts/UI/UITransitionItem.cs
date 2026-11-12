using System;
using UnityEngine;

public class UITransitionItem : MonoBehaviour
{
    public UIItem sourceItem;
    public UIItem targetItem;
    public GameObject adsItem;
    private Transition transition;
    private bool trading;

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

        adsItem.SetActive(true); // rewarded ads are opt-in: not affected by remove_ads

        trading = false;
    }

    public void TradingClick()
    {
        if (trading) return;
        if (!HasSource())
        {
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("not_enough_resource"));
            return;
        }
        GUIManager.instan.CreatePopup<PopupConfirm>(LocalizationManager.Localize("trading_confirm"), (Action)ConfirmTrading);
    }

    private bool HasSource()
    {
        if (transition.sourceCurrency != null)
            return GameManager.instan.userData.IsReduceableCurrency(transition.sourceCurrency);
        if (transition.sourceItem != null)
            return GameManager.instan.userData.GetUserItem(transition.sourceItem.itemId) != null;
        return true;
    }

    public void ConfirmTrading()
    {
        if (trading) return;
        // "trading" is only locked once the reward is granted, so closing the ad early never locks the button.
        AdsManager.instan.ShowRewardAds(Trading);
    }

    public void Trading()
    {
        if (trading) return;
        if (!HasSource()) return; // resources may have changed while the ad was playing
        trading = true;
        GameManager.instan.RequestTransition(transition, OnTrasitionResponse, OnTransitionError);
    }

    public void OnTrasitionResponse(RewardData rewardData)
    {
        trading = false;
        GUIManager.instan.CreatePopup<PopupReward>(rewardData);
    }

    private void OnTransitionError()
    {
        trading = false;
        GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("disconected"));
    }
}
