using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PopupInventory : PopupBase
{
    [HideInInspector]
    public List<ItemType> slotsEquipted = new List<ItemType>();
    public InventoryPanel inventoryPanel;

    private CharacterPref characterPref;

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        characterPref = HomeManager.instance.characterPref;
        InitData();
    }

    

    public void InitData()
    {
        slotsEquipted.Clear();

        for (int i = 0; i < characterPref.itemLoadOut.Count; i++)
        {
            var equiptData = characterPref.itemLoadOut.ElementAt(i);
            if (equiptData.Value != ItemId.None)
            {
                slotsEquipted.Add(equiptData.Key);
            }
        }

    }

    public void UpdateCharacterSlot(ItemType slot, ItemId itemId)
    {
        if (itemId == ItemId.None)
        {
            if (characterPref.itemLoadOut.ContainsKey(slot))
            {
                characterPref.itemLoadOut.Remove(slot);
            }
            HomeManager.instance.remotePlayer.Unequip(slot);
            if (slotsEquipted.Contains(slot)) slotsEquipted.Remove(slot);
        }
        else
        {
            if (characterPref.itemLoadOut.ContainsKey(slot))
            {
                characterPref.itemLoadOut[slot] = itemId;
            }
            else
            {
                characterPref.itemLoadOut.Add(slot, itemId);
            }
            HomeManager.instance.remotePlayer.Equipt(itemId, slot);
            if (slot != ItemType.None && !slotsEquipted.Contains(slot)) slotsEquipted.Add(slot);
        }

        CharacterSave.SaveCharacterPref(characterPref);
    }

}