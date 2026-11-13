using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILoadoutSlot : MonoBehaviour
{
    public ItemType itemType;
    public Image icon;
    public GameObject emptyObj;
    public Text txtName;
    private PopupInventory popupInventory;
    //public LoadoutSlotsPanel loadoutSlotsPanel;

    public void SetItem(ItemId itemId)
    {
        if (itemId == ItemId.None)
        {
            if (itemType < ItemType.WEAPON)
            {
                if (txtName) txtName.text = LocalizationManager.Localize("cosmetic");
            }
            else
                if (txtName) txtName.text = LocalizationManager.Localize(itemType.ToString());
            icon.gameObject.SetActive(false);
            if (emptyObj)
                emptyObj.gameObject.SetActive(true);
        }
        else
        {
            icon.gameObject.SetActive(true);
            if (emptyObj)
                emptyObj.gameObject.SetActive(false);
            icon.sprite = GUIManager.instan.GetItemIcon(itemId);
            ItemConfig itemConfig = ConfigManager.GetItemConfig(itemId);
            if (itemConfig != null && itemType < ItemType.WEAPON)
            {
                //if (itemType != ItemType.None && itemConfig.slot != itemType && loadoutSlotsPanel.slotsEquipted.Contains(itemType)) loadoutSlotsPanel.slotsEquipted.Remove(itemType);
                itemType = itemConfig.slot;
            }

            if (txtName)
            {

                if (itemConfig != null && itemConfig.catalogItem != null)
                    txtName.text = itemConfig.catalogItem.DisplayName;
                else
                    txtName.text = LocalizationManager.Localize(itemId.ToString());
            }
        }

        //loadoutSlotsPanel.popupLoadout.inventoryPanel.gameObject.SetActive(false);
        //loadoutSlotsPanel.UpdateCharacterSlot(itemType, itemId);
        UpdateCharacterSlot(itemType, itemId);
        if (itemId == ItemId.None && itemType < ItemType.WEAPON)
        {
            itemType = ItemType.None;
        }

        if (popupInventory != null) popupInventory.Dismiss();
    }


    public void SetItemViewOnly(ItemId itemId)
    {
        if (itemId == ItemId.None)
        {
            if (itemType < ItemType.WEAPON)
            {
                itemType = ItemType.None;
                if (txtName) txtName.text = LocalizationManager.Localize("cosmetic");
            }
            else
                if (txtName) txtName.text = LocalizationManager.Localize(itemType.ToString());
            icon.gameObject.SetActive(false);
            if (emptyObj)
                emptyObj.gameObject.SetActive(true);
            return;
        }
        icon.gameObject.SetActive(true); 
        if (emptyObj)
            emptyObj.gameObject.SetActive(false);

        ItemConfig itemConfig = ConfigManager.GetItemConfig(itemId);
        if (itemConfig != null && itemType < ItemType.WEAPON) itemType = itemConfig.slot;
        icon.sprite = GUIManager.instan.GetItemIcon(itemId);
        if (txtName)
        {
           
            if (itemConfig != null && itemConfig.catalogItem != null)
                txtName.text = itemConfig.catalogItem.DisplayName;
            else
                txtName.text = LocalizationManager.Localize(itemId.ToString());
        }
    }

    public void UpdateCharacterSlot(ItemType slot, ItemId itemId)
    {
        if (itemId == ItemId.None)
        {
            if (HomeManager.instance.characterPref.itemLoadOut.ContainsKey(slot))
            {
                HomeManager.instance.characterPref.itemLoadOut.Remove(slot);
            }
            HomeManager.instance.remotePlayer.Unequip(slot);
            //if (slotsEquipted.Contains(slot)) slotsEquipted.Remove(slot);
        }
        else
        {
            if (HomeManager.instance.characterPref.itemLoadOut.ContainsKey(slot))
            {
                HomeManager.instance.characterPref.itemLoadOut[slot] = itemId;
            }
            else
            {
                HomeManager.instance.characterPref.itemLoadOut.Add(slot, itemId);
            }
            HomeManager.instance.remotePlayer.Equipt(itemId, slot);
            //if (slot != ItemType.None && !slotsEquipted.Contains(slot)) slotsEquipted.Add(slot);
        }

        CharacterSave.SaveCharacterPref(HomeManager.instance.characterPref);
    }


    public void OnSlotClick()
    {
        popupInventory = GUIManager.instan.CreatePopup<PopupInventory>();
        popupInventory.inventoryPanel.OpenInventory(HomeManager.instance.characterPref.tF2Class, itemType, SetItem);
        //loadoutSlotsPanel.popupLoadout.inventoryPanel.OpenInventory(loadoutSlotsPanel.tf2Class, itemType, SetItem);
        if (itemType > ItemType.WEAPON)
            HomeManager.instance.remotePlayer.SwitchWeaponItem(itemType);
    }
    
}
