using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CratesPanel : MonoBehaviour
{
    public Text txtCrateQuantity;
    public Text goldKeyQuantity;
    private int crateRemaining;
    private bool requesting = false;

    public void OpenCratePanel()
    {
        UpdateData();
    }

    public void UpdateData()
    {
        UserItem crate = GameManager.instan.userData.GetUserItem(ItemId.crate_box);
        if (crate == null)
        {
            crateRemaining = 0;
            txtCrateQuantity.text = "0";
        }
        else
        {
            crateRemaining = crate.itemInstance.RemainingUses.GetValueOrDefault();
            txtCrateQuantity.text = crateRemaining.ToString();
        }

        goldKeyQuantity.text = ConfigManager.pricePerCrate.ToString();
    }

    public void OnCrateClick()
    {
        if (requesting) return;
        if (GameManager.instan.userData.GetUserCurrency(TF2Currency.GK) < ConfigManager.pricePerCrate)
        {
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("crate_not_enough_gold_key"));
            return;
        }
        
        if (crateRemaining <= 0)
        {
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("crate_not_enough_crate"));
            return;
        }

        requesting = true;
        GameManager.instan.RequestCrate(OnCrateResponse, UpdateData);
    }

    public void OnCrateResponse(ItemId itemId)
    {
        requesting = false;
        GUIManager.instan.CreatePopup<PopupOpenCrate>(itemId);
        //UpdateData();
    }
    
}
