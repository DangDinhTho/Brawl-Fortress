using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanel : MonoBehaviour
{
    public Transform itemContainer;
    public GameObject itemPref;
    public Dropdown classFilter;
    public Dropdown itemTypeFilter;
    private TF2Class tf2Class = TF2Class.demoman;
    private ItemType itemType = ItemType.all;
    private Action<ItemId> OnItemClick;
    private bool fromLoadout;
    public PopupInventory popupInventory;
    private ItemType backupSlot;

    public void OpenInventory(TF2Class _tf2Class = TF2Class.None, ItemType itemType = ItemType.all, Action<ItemId> _callback = null)
    {
        gameObject.SetActive(true);
        classFilter.ClearOptions();
        itemTypeFilter.ClearOptions();
        if (_tf2Class != TF2Class.None)
        {
            fromLoadout = true;
            tf2Class = _tf2Class;
            if (itemType < ItemType.WEAPON)
            {
                backupSlot = itemType;
                this.itemType = ItemType.all;
            }
            else
                this.itemType = itemType;
            classFilter.interactable = false;
            itemTypeFilter.interactable = false;
            classFilter.AddOptions(new List<string>() { LocalizationManager.Localize(tf2Class.ToString()) });
            classFilter.SetValueWithoutNotify(0);
            itemTypeFilter.AddOptions(new List<string>() { LocalizationManager.Localize("All") });
            itemTypeFilter.SetValueWithoutNotify(0);
        }
        else
        {
            fromLoadout = false;
            classFilter.interactable = true;
            itemTypeFilter.interactable = true;

            
            List<string> classOptions = new List<string>();
            classOptions.Add(LocalizationManager.Localize("All"));
            for (int i = 1; i < 10; i++)
            {
                classOptions.Add(LocalizationManager.Localize(((TF2Class)i).ToString()));
            }
            classFilter.AddOptions(classOptions);
            classFilter.onValueChanged.AddListener(OnClassFilterChange);

            tf2Class = TF2Class.None;
            this.itemType = ItemType.all;
           
            List<string> itemOptions = new List<string>();
            itemOptions.Add(LocalizationManager.Localize((ItemType.all.ToString())));
            itemOptions.Add(LocalizationManager.Localize((ItemType.taunt.ToString())));
            itemOptions.Add(LocalizationManager.Localize((ItemType.ticket.ToString())));
            for (int i = 1; i <= 5; i++)
            {
                itemOptions.Add(LocalizationManager.Localize(((ItemType)i).ToString()));
            }

            for (int i = 21; i < 24; i++)
            {
                itemOptions.Add(LocalizationManager.Localize(((ItemType)i).ToString()));
            }
            itemTypeFilter.AddOptions(itemOptions);
            itemTypeFilter.onValueChanged.AddListener(OnItemFilterChange);
        }



        OnItemClick = _callback;
        UpdateListItem();
    }

    public void UpdateListItem()
    {
        for(int i = 0; i < itemContainer.childCount; i++)
        {
            itemContainer.GetChild(i).gameObject.SetActive(false);
        }

        int index = 0;

        if(fromLoadout && itemType == ItemType.all)
        {
            UILoadoutItem loadoutItem = null;
            if (index < itemContainer.childCount)
            {
                loadoutItem = itemContainer.GetChild(index).GetComponent<UILoadoutItem>();
            }
            else
            {
                loadoutItem = Instantiate(itemPref, itemContainer).GetComponent<UILoadoutItem>();
            }
            loadoutItem.gameObject.SetActive(true);
            loadoutItem.SetEmptyItem();
            index++;
        }

        if (tf2Class != TF2Class.None && itemType > ItemType.WEAPON && itemType < ItemType.Building)
        {
            CharacterConfig characterConfig = ConfigManager.GetCharacterConfig(tf2Class);
            ItemId itemId = ItemId.None;
            switch (itemType)
            {
                case ItemType.Primary:
                    itemId = (ItemId)Enum.Parse(typeof(ItemId), characterConfig.defaultWeapons[0].ToString());
                    break;
                case ItemType.Secondary:
                    itemId = (ItemId)Enum.Parse(typeof(ItemId), characterConfig.defaultWeapons[1].ToString());
                    break;
                case ItemType.Mele:
                    itemId = (ItemId)Enum.Parse(typeof(ItemId), characterConfig.defaultWeapons[2].ToString());
                    break;
                //case ItemType.Building:
                //    if (characterConfig.defaultWeapons.Length > 3)
                //        itemId = (ItemId)Enum.Parse(typeof(ItemId), characterConfig.defaultWeapons[3].ToString());
                //    itemId = bui
                //    break;
                //case ItemType.PDA:
                //    itemId = (ItemId)Enum.Parse(typeof(ItemId), characterConfig.defaultWeapons[4].ToString());
                //    break;
            }

            UILoadoutItem loadoutItem = null;
            if (index < itemContainer.childCount)
            {
                loadoutItem = itemContainer.GetChild(index).GetComponent<UILoadoutItem>();
            }
            else
            {
                loadoutItem = Instantiate(itemPref, itemContainer).GetComponent<UILoadoutItem>();
            }
            loadoutItem.gameObject.SetActive(true);
            loadoutItem.SetItem(itemId);
            index++;
        }

        //for(int i = GameManager.instan.userData.userItems.Count - 1; i >= 0; i--) 
        GameManager.instan.userData.userItems.Sort((a, b) => GetUserItemPoint(b).CompareTo(GetUserItemPoint(a)));
        foreach (var userItem in GameManager.instan.userData.userItems)
        {
            //UserItem userItem = GameManager.instan.userData.userItems[i];
            ItemConfig item = ConfigManager.GetItemConfig(userItem.itemId);
            if (item.slot == ItemType.Pack || item.slot == ItemType.remove_ads) continue;
            if((itemType == ItemType.all || item.slot == itemType) && (tf2Class == TF2Class.None || (item.tf2Classes != null && item.tf2Classes.Contains(tf2Class))))
            {
                if (fromLoadout && itemType == ItemType.all && ((item.slot != backupSlot && popupInventory.slotsEquipted.Contains(item.slot)) || item.slot > ItemType.NonEquipt)) continue;
                UILoadoutItem loadoutItem = null;
                if(index < itemContainer.childCount)
                {
                    loadoutItem = itemContainer.GetChild(index).GetComponent<UILoadoutItem>();
                }
                else
                {
                    loadoutItem = Instantiate(itemPref, itemContainer).GetComponent<UILoadoutItem>();
                }
                loadoutItem.gameObject.SetActive(true);
                loadoutItem.SetItem(item.itemId);
                index++;
            }
        }
    }

    private long GetUserItemPoint(UserItem userItem)
    {
        long point = 0;
        if (userItem.itemInstance.RemainingUses.HasValue) point += 10000000000;
        DateTimeOffset dateTimeOffset = new DateTimeOffset(userItem.itemInstance.PurchaseDate.Value);
        point += dateTimeOffset.ToUnixTimeSeconds();
        return point;
    }

    public void OnSelectItem(ItemId itemId)
    {
        OnItemClick?.Invoke(itemId);
    }

    public void OnClassFilterChange(int value)
    {
        tf2Class = (TF2Class)(value);
        UpdateListItem();
    }

    public void OnItemFilterChange(int value)
    {
        if (value == 0) itemType = ItemType.all;
        else if (value == 1) itemType = ItemType.taunt;
        else if (value == 2) itemType = ItemType.ticket;
        else if (value > 2 && value < 8)
            itemType = (ItemType)(value - 2);
        else
        {
            itemType = (ItemType)(value + 13);
        }
        UpdateListItem();
    }
}
