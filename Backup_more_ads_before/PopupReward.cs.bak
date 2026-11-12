using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupReward : PopupBase
{
    private RewardData rewardData;
    public Transform itemContainer;
    public GameObject itemPref;

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        if (args.Length > 0)
        {
            rewardData = (RewardData)args[0];
        }
        SetReward();
    }

    public void SetReward()
    {
        for (int i = 0; i < itemContainer.childCount; i++)
        {
            itemContainer.GetChild(i).gameObject.SetActive(false);
        }

        if (rewardData == null) return;
        int index = 0;
        
        if(rewardData.virtualCurrncies != null)
            foreach (var currncy in rewardData.virtualCurrncies)
            {
                UIItem uiItem = null;
                if (index < itemContainer.childCount)
                {
                    uiItem = itemContainer.GetChild(index).GetComponent<UIItem>();
                }
                else
                {
                    uiItem = Instantiate(itemPref, itemContainer).GetComponent<UIItem>();
                }

                uiItem.gameObject.SetActive(true);
                uiItem.SetCurrency(currncy);
                index++;
            }
        
        if(rewardData.itemDatas != null)
            foreach (var item in rewardData.itemDatas)
            {
                UIItem uiItem = null;
                if (index < itemContainer.childCount)
                {
                    uiItem = itemContainer.GetChild(index).GetComponent<UIItem>();
                }
                else
                {
                    uiItem = Instantiate(itemPref, itemContainer).GetComponent<UIItem>();
                }

                uiItem.gameObject.SetActive(true);
                uiItem.SetItem(item);
                index++;
            }

        
    }
}
