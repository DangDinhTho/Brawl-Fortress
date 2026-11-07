using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PopupTrading : PopupBase
{
    public Transform container;
    private TradingData tradingData;
    
    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        tradingData = ConfigManager.tradingData;
        for (int i = 0; i < tradingData.transitions.Length; i++)
        {
            var item = GUIManager.instan.CreateListItem<UITransitionItem>(container, i);
            item.SetTransitionData(tradingData.transitions[i]);
        }
        GUIManager.instan.CleanListItem(container, tradingData.transitions.Length);
        CheckTime();
    }

    public void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            CheckTime();
        }
    }
    public void CheckTime()
    {
        GameManager.instan.GetServerTime(CheckTimeResponse);
    }

    public void CheckTimeResponse(DateTime time)
    {
        if (time.DayOfWeek != ConfigManager.tradingData.openDay)
        {
            Dismiss();
        }
    }

}



public class TradingData
{
    public DayOfWeek openDay;
    public Transition[] transitions;
}

public class Transition
{
    public int ID;
    public VirtualCurrncy sourceCurrency;
    public ItemData sourceItem;
    public VirtualCurrncy targetCurrency;
}
