using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LoadoutSlotsPanel : MonoBehaviour
{
    public PopupLoadout popupLoadout;
    public UILoadoutSlot[] loadoutSlots;
    [HideInInspector]
    public List<UILoadoutSlot> uiLoadoutSlots = new List<UILoadoutSlot>();
    public Dictionary<ItemType, UILoadoutSlot> weaponSlots = new Dictionary<ItemType, UILoadoutSlot>(); 
    public TF2Class tf2Class;
    public CharacterPref characterPref;
    public Transform slotContainer;
    [HideInInspector]
    public List<ItemType> slotsEquipted = new List<ItemType>();
    public Text txtCharacterName;
    private bool inited;

    private void Awake()
    {
        
    }

    private void Init()
    {
        for (int i = 0; i < loadoutSlots.Length; i++)
        {
            UILoadoutSlot slot = loadoutSlots[i];

            if (slot.itemType > ItemType.WEAPON && slot.itemType < ItemType.END_WEAPON)
            {
                weaponSlots.Add(slot.itemType, slot);
            }
            else
            {
                uiLoadoutSlots.Add(slot);
            }
        }
        inited = true;
    }

    public void OpenLoadoutPanel(TF2Class _tf2Class)
    {
        if (!inited) Init();
        this.tf2Class = _tf2Class;
        characterPref = CharacterSave.GetCharacterPref(_tf2Class);
        HomeManager.instance.SetCharacterClass(_tf2Class);

        foreach(var uiItem in loadoutSlots)
        {
            uiItem.SetItemViewOnly(ItemId.None);
        }

        slotsEquipted.Clear();

        //if (characterPref.itemLoadOut != null)
        //{
        int slotIndex = 0;
        for (int i = 0; i < characterPref.itemLoadOut.Count; i++)
        {
            var equiptData = characterPref.itemLoadOut.ElementAt(i);
            if (equiptData.Value != ItemId.None)
            {
                if(equiptData.Key > ItemType.WEAPON && equiptData.Key < ItemType.END_WEAPON)
                {
                    if (weaponSlots.ContainsKey(equiptData.Key))
                        weaponSlots[equiptData.Key].SetItemViewOnly(equiptData.Value);
                    
                }
                else
                {
                    uiLoadoutSlots[slotIndex].SetItemViewOnly(equiptData.Value);
                    slotIndex++;
                }
                slotsEquipted.Add(equiptData.Key);
            }
        }
            //foreach (var loadoutItem in characterPref.itemLoadOut)
            //{
            //    if (uiLoadoutSlots.ContainsKey(loadoutItem.Key))
            //        uiLoadoutSlots[loadoutItem.Key].SetItemViewOnly(loadoutItem.Value);
            //}

        //}

        txtCharacterName.text = ">>  " + LocalizationManager.Localize(_tf2Class.ToString());
        HomeManager.instance.characterContainer.localRotation = Quaternion.Euler(0, 0, 0);
    }
    
    public void UpdateCharacterSlot(ItemType slot, ItemId itemId)
    {
        if(itemId == ItemId.None)
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
