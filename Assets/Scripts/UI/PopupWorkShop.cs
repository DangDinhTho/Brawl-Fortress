using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public override void OnCreate(params object[] args)
    {
        gameObject.SetActive(true);
        highlight.gameObject.SetActive(false);
        buyAvailble.SetActive(false);
        if (args.Length > 0)
        {
            tf2Class = (TF2Class)args[0];
            
            if(HomeManager.instance.characterPref.tF2Class != tf2Class)
            {
                HomeManager.instance.SetCharacterClass(tf2Class);
            }
            
        }
        else
        {
            tf2Class = HomeManager.instance.characterPref.tF2Class;
        }

        tF2ClassOrg = tf2Class;

        if (args.Length > 1)
        {

        }
        txtItemSelectedName.gameObject.SetActive(false);
        classFilter.ClearOptions();
        List<string> classOptions = new List<string>();
        for(int i = 1; i < 10; i++)
        {
            classOptions.Add(LocalizationManager.Localize(((TF2Class)i).ToString()));
        }
        classFilter.AddOptions(classOptions);
        classFilter.onValueChanged.AddListener(OnClassFilterChange);
        classFilter.SetValueWithoutNotify((int)tf2Class - 1);

        itemTypeFilter.ClearOptions();
        List<string> itemOptions = new List<string>();
        itemOptions.Add(LocalizationManager.Localize((ItemType.all.ToString())));
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
        itemTypeFilter.onValueChanged.AddListener(OnItemFilterChange);

        catalogItems = ConfigManager.itemConfigs.Values.ToList();
        catalogItems.Sort((a, b) => GetItemPoint(b).CompareTo(GetItemPoint(a)));

        UpdateListItem();
    }

    private long GetItemPoint(ItemConfig item)
    {
        long point = 0;
        switch (item.slot)
        {
            case ItemType.taunt:
                point += 1000000000009;
                break;
            case ItemType.Primary:
                point += 1000000000008;
                break;
            case ItemType.Secondary:
                point += 1000000000007;
                break;
            case ItemType.Mele:
                point += 1000000000006;
                break;
            case ItemType.slot_0:
                point += 1000000000005;
                break;
            case ItemType.slot_1:
                point += 1000000000004;
                break;
            case ItemType.slot_2:
                point += 1000000000003;
                break;
            case ItemType.slot_3:
                point += 1000000000002;
                break;
            case ItemType.slot_4:
                point += 1000000000001;
                break;

        }

        return point;
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
            if(item.itemId < ItemId.IgnoreShow && (_itemType == ItemType.all || item.slot == _itemType) && (item.tf2Classes != null && item.tf2Classes.Contains(tf2Class)))
            {
                UIWorkShopItem loadoutItem = null;
                if(index < itemContainer.childCount)
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

    public void OnSelectItem(ItemId itemId)
    {
        ItemConfig itemConfig = ConfigManager.GetItemConfig(itemId);
        if (itemPrevious != ItemId.None && itemConfig.slot != ItemType.taunt)
        {
            if (itemId == itemPrevious) return;
            HomeManager.instance.remotePlayer.Unequip(itemPrevious);
            //HomeManager.instance.remotePlayer.EndTaunt();
        }

       
        
        
        if (itemConfig.slot < ItemType.NonEquipt)
        {
            HomeManager.instance.remotePlayer.Equipt(itemId);
            HomeManager.instance.remotePlayer.EndTaunt();
            if (itemConfig.slot > ItemType.WEAPON)
                HomeManager.instance.remotePlayer.SwitchWeaponItem(itemConfig.slot);
            else if(itemPrevious != ItemId.None)
            {
                ItemConfig itemPreviousConfig = ConfigManager.GetItemConfig(itemPrevious);
                if (itemPreviousConfig.slot > ItemType.WEAPON)
                {
                    HomeManager.instance.remotePlayer.SwitchWeaponItem(ConfigManager.classMenuWeapon[HomeManager.instance.characterPref.tF2Class]);
                   // HomeManager.instance.remotePlayer.playerAnimator.Animator.SetTrigger("Menu");
                }
            }
        }
        else if (itemConfig.slot == ItemType.taunt)
        {
            HomeManager.instance.remotePlayer.PlayTaunt(ConfigManager.GetTauntFromItemId(itemId));
        }

        txtItemSelectedName.gameObject.SetActive(true);
        txtItemSelectedName.text = itemConfig.catalogItem.DisplayName;

        
        if (itemConfig.catalogItem.VirtualCurrencyPrices.TryGetValue("GK", out var price))
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

        if(itemConfig.itemProperties != null && itemConfig.itemProperties.craftData != null)
        {
            craftAvailble.SetActive(true);
        }
        else
        {
            craftAvailble.SetActive(false);
        }

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
        ItemConfig itemConfig = ConfigManager.GetItemConfig(itemPrevious);
        if(GameManager.instan.userData.currency.goldKey < itemConfig.catalogItem.VirtualCurrencyPrices["GK"])
        {
            GUIManager.instan.CreatePopup<PopupNotEnought>();
            return;
        }

        string confirmContent = string.Format(LocalizationManager.Localize("work_shop_buy_confirm"),
            itemConfig.catalogItem.DisplayName, txtPrice.text);
        GUIManager.instan.CreatePopup<PopupConfirm>(confirmContent, (Action)Buy, GUIManager.instan.GetItemIcon(itemPrevious));
    }

    private void Buy()
    {
        GameManager.instan.BuyItem(itemPrevious, OnBuySuccess);
    }

    public void OnBuySuccess(ItemId itemId)
    {
        RewardData reward = new RewardData();
        ItemData itemPurcharsed = new ItemData()
        {
            itemId = itemId,
            quantity = 1
        };
        reward.itemDatas = new List<ItemData>() { itemPurcharsed };
        GUIManager.instan.CreatePopup<PopupReward>(reward);
        Debug.Log("success");
    }

    public override void OnDismiss()
    {
        //HomeManager.instance.CreateCharacter();
        HomeManager.instance.SetCharacterClass(tF2ClassOrg);
        base.OnDismiss();
    }
}
