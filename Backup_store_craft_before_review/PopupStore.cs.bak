using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupStore : PopupBase
{
    public Transform packContainer;
    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        InitPacks();
    }

    public void InitPacks()
    {
        var packConfigs = ConfigManager.GetItemConfigs(ItemType.Pack);
        int index = 0;
        foreach (var packConfig in packConfigs)
        {
            if(packConfig.itemId == ItemId.brawl_valley_pack_first_purchase && GameManager.instan.userData.GetUserItem(ItemId.brawl_valley_pack_first_purchase) != null) continue;
            
            if (packConfig.itemProperties.packData.expirationDate > 0)
            {
                var timeRemaining = packConfig.itemProperties.packData.expirationDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if(timeRemaining <= 0) continue;
            }
            
            var uiPackItem = GUIManager.instan.CreateListItem<UIPackItem>(packContainer, index);
            uiPackItem.SetData(packConfig);
            index++;
        }
        
        GUIManager.instan.CleanListItem(packContainer, index);
    }
}
