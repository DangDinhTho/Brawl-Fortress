using System;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using UnityEngine;
using UnityEngine.UI;

public class PopupWorkShop : PopupBase
{
    public Transform itemContainer;
    public GameObject itemPref;
    public Dropdown classFilter;
    public Dropdown itemTypeFilter;
    public Text txtPrice;
    public Image currencyIcon;

    private TF2Class tf2Class;
    private ItemType _itemType = ItemType.all;
    private ItemId itemPrevious;
    public Transform highlight;
    public GameObject buyAvailble;
    public GameObject craftAvailble;
    public Text txtItemSelectedName;
    private List<ItemConfig> catalogItems;
    private TF2Class tF2ClassOrg;
    private bool buying;

    public override void OnCreate(params object[] args)
    {
        gameObject.SetActive(true);
        highlight.gameObject.SetActive(false);
        buyAvailble.SetActive(false);
        craftAvailble.SetActive(false);
        itemPrevious = ItemId.None; // the popup instance can be reused
        buying = false;
        _itemType = ItemType.all;

        if (args.Length > 0)
        {
            tf2Class = (TF2Class)args[0];
            if (HomeManager.instance.characterPref.tF2Class != tf2Class)
            {
                HomeManager.instance.SetCharacterClass(tf2Class);
            }
        }
        else
        {
            tf2Class = HomeManager.instance.characterPref.tF2Class;
        }

        tF2ClassOrg = tf2Class;

        txtItemSelectedName.gameObject.SetActive(false);

        // Remove listeners first: OnCreate runs every time a reused popup is opened.
        classFilter.onValueChanged.RemoveListener(OnClassFilterChange);
        classFilter.ClearOptions();
        List<string> classOptions = new List<string>();
        for (int i = 1; i < 10; i++)
        {
            classOptions.Add(LocalizationManager.Localize(((TF2Class)i).ToString()));
        }
        classFilter.AddOptions(classOptions);
        classFilter.SetValueWithoutNotify((int)tf2Class - 1);
        classFilter.onValueChanged.AddListener(OnClassFilterChange);

        itemTypeFilter.onValueChanged.RemoveListener(OnItemFilterChange);
        itemTypeFilter.ClearOptions();
        List<string> itemOptions = new List<string>();
        itemOptions.Add(LocalizationManager.Localize(ItemType.all.ToString()));
        for (int i = 1; i <= 5; i++)
        {
            itemOptions.Add(LocalizationManager.Localize(((ItemType)i).ToString()));
        }
        for (int i = 21; i < 24; i++)
        {
            itemOptions.Add(LocalizationManager.Localize(((ItemType)i).ToString()));
        }
        itemOptions.Add(LocalizationManager.Localize(ItemType.taunt.ToString()));
        itemTypeFilter.AddOptions(itemOptions);
        itemTypeFilter.SetValueWithoutNotify(0);
        itemTypeFilter.onValueChanged.AddListener(OnItemFilterChange);

        catalogItems = ConfigManager.itemConfigs.Values.ToList();
        catalogItems.Sort((a, b) => GetItemPoint(b).CompareTo(GetItemPoint(a)));

        UpdateListItem();
    }

    private long GetItemPoint(ItemConfig item)
    {
        switch (item.slot)
        {
            case ItemType.taunt: return 9;
            case ItemType.Primary: return 8;
            case ItemType.Secondary: return 7;
            case ItemType.Mele: return 6;
            case ItemType.slot_0: return 5;
            case ItemType.slot_1: return 4;
            case ItemType.slot_2: return 3;
            case ItemType.slot_3: return 2;
            case ItemType.slot_4: return 1;
            default: return 0;
        }
    }

    public void UpdateListItem()
    {
        highlight.gameObject.SetActive(false);
        buyAvailble.SetActive(false);

        for (int i = 0; i < itemContainer.childCount; i++)
        {
            itemContainer.GetChild(i).gameObject.SetActive(false);
        }

        int index = 0;
        foreach (var item in catalogItems)
        {
            if (item.itemId < ItemId.IgnoreShow && (_itemType == ItemType.all || item.slot == _itemType) && (item.tf2Classes != null && item.tf2Classes.Contains(tf2Class)))
            {
                UIWorkShopItem loadoutItem;
                if (index < itemContainer.childCount)
                {
                    loadoutItem = itemContainer.GetChild(index).GetComponent<UIWorkShopItem>();
                }
                else
                {
                    loadoutItem = Instantiate(itemPref, itemContainer).GetComponent<UIWorkShopItem>();
                }
                loadoutItem.gameObject.SetActive(true);
                loadoutItem.SetItem(item);
                index++;
            }
        }
    }

    /// <summary>Equipment / taunts can only be owned once: no point buying them again.</summary>
    private static bool IsAlreadyOwned(ItemConfig itemConfig)
    {
        bool unique = itemConfig.slot < ItemType.NonEquipt || itemConfig.slot == ItemType.taunt;
        return unique && GameManager.instan.userData.GetUserItem(itemConfig.itemId) != null;
    }

    public void OnSelectItem(ItemId itemId)
    {
        ItemConfig itemConfig = ConfigManager.GetItemConfig(itemId);
        if (itemConfig == null) return;

        if (itemPrevious != ItemId.None && itemConfig.slot != ItemType.taunt)
        {
            if (itemId == itemPrevious) return;
            HomeManager.instance.remotePlayer.Unequip(itemPrevious);
        }

        if (itemConfig.slot < ItemType.NonEquipt)
        {
            HomeManager.instance.remotePlayer.Equipt(itemId);
            HomeManager.instance.remotePlayer.EndTaunt();
            if (itemConfig.slot > ItemType.WEAPON)
                HomeManager.instance.remotePlayer.SwitchWeaponItem(itemConfig.slot);
            else if (itemPrevious != ItemId.None)
            {
                ItemConfig itemPreviousConfig = ConfigManager.GetItemConfig(itemPrevious);
                if (itemPreviousConfig != null && itemPreviousConfig.slot > ItemType.WEAPON)
                {
                    HomeManager.instance.remotePlayer.SwitchWeaponItem(ConfigManager.classMenuWeapon[HomeManager.instance.characterPref.tF2Class]);
                }
            }
        }
        else if (itemConfig.slot == ItemType.taunt)
        {
            HomeManager.instance.remotePlayer.PlayTaunt(ConfigManager.GetTauntFromItemId(itemId));
        }

        txtItemSelectedName.gameObject.SetActive(true);
        txtItemSelectedName.text = itemConfig.catalogItem.DisplayName;

        var prices = itemConfig.catalogItem.VirtualCurrencyPrices;
        if (prices != null && prices.TryGetValue("GK", out var price) && !IsAlreadyOwned(itemConfig))
        {
            txtPrice.gameObject.SetActive(true);
            txtPrice.text = price.ToString();
            buyAvailble.SetActive(true);
        }
        else
        {
            txtPrice.gameObject.SetActive(false);
            buyAvailble.SetActive(false);
        }

        craftAvailble.SetActive(itemConfig.itemProperties != null && itemConfig.itemProperties.craftData != null);

        itemPrevious = itemId;
    }

    public void OnClassFilterChange(int value)
    {
        tf2Class = (TF2Class)(value + 1);
        UpdateListItem();
        HomeManager.instance.SetCharacterClass(tf2Class);
    }

    public void OnItemFilterChange(int value)
    {
        if (value == 0) _itemType = ItemType.all;
        else if (value > 0 && value <= 5)
            _itemType = (ItemType)(value);
        else if (value < itemTypeFilter.options.Count - 1)
        {
            _itemType = (ItemType)(value + 15);
        }
        else _itemType = ItemType.taunt;
        UpdateListItem();
    }

    public void OnBuyClick()
    {
        if (buying || itemPrevious == ItemId.None) return;
        ItemConfig itemConfig = ConfigManager.GetItemConfig(itemPrevious);
        var prices = itemConfig?.catalogItem?.VirtualCurrencyPrices;
        if (prices == null || !prices.TryGetValue("GK", out var price)) return;

        if (GameManager.instan.userData.currency.goldKey < price)
        {
            GUIManager.instan.CreatePopup<PopupNotEnought>();
            return;
        }

        string confirmContent = string.Format(LocalizationManager.Localize("work_shop_buy_confirm"),
            itemConfig.catalogItem.DisplayName, price);
        GUIManager.instan.CreatePopup<PopupConfirm>(confirmContent, (Action)Buy, GUIManager.instan.GetItemIcon(itemPrevious));
    }

    private void Buy()
    {
        if (buying) return; // double tap on the confirm button
        buying = true;
        GameManager.instan.BuyItem(itemPrevious, OnBuySuccess, OnBuyFailed);
    }

    public void OnBuySuccess(ItemId itemId)
    {
        buying = false;
        RewardData reward = new RewardData();
        reward.itemDatas = new List<ItemData>() { new ItemData() { itemId = itemId, quantity = 1 } };
        GUIManager.instan.CreatePopup<PopupReward>(reward);

        // Refresh the selection (hides the Buy button for owned equipment)
        if (itemId == itemPrevious)
        {
            var cfg = ConfigManager.GetItemConfig(itemId);
            if (cfg != null && IsAlreadyOwned(cfg))
            {
                buyAvailble.SetActive(false);
                txtPrice.gameObject.SetActive(false);
            }
        }
    }

    private void OnBuyFailed(PlayFabError error)
    {
        buying = false;
        if (error != null && error.Error == PlayFabErrorCode.InsufficientFunds)
            GUIManager.instan.CreatePopup<PopupNotEnought>();
        else
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("disconected"));
    }

    public override void OnDismiss()
    {
        HomeManager.instance.SetCharacterClass(tF2ClassOrg);
        base.OnDismiss();
    }
}
