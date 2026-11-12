using System;
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
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int index = 0;
        foreach (var packConfig in packConfigs)
        {
            var packData = packConfig.itemProperties?.packData;
            if (packData == null) continue; // pack without CustomData

            if (packConfig.itemId == ItemId.brawl_valley_pack_first_purchase && GameManager.instan.userData.GetUserItem(ItemId.brawl_valley_pack_first_purchase) != null) continue;
            if (packData.expirationDate > 0 && packData.expirationDate <= now) continue;

            var uiPackItem = GUIManager.instan.CreateListItem<UIPackItem>(packContainer, index);
            uiPackItem.SetData(packConfig);
            index++;
        }

        GUIManager.instan.CleanListItem(packContainer, index);
    }
}
