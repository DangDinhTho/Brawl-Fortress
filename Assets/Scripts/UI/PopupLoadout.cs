using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupLoadout : PopupBase
{
    public InventoryPanel inventoryPanel;
    public LoadoutSlotsPanel loadoutSlotsPanel;
    public CratesPanel cratesPanel;
    //public Image tradingBtn;
    //public Sprite[] tradingIcons;
    public Text txtTimeTradingOpen;
    
    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        SetActivePanel("");
        CheckTradingOpen();
    }

    public void OpenLoadout(TF2Class tF2Class)
    {
        OpenLoadoutClick();
        loadoutSlotsPanel.OpenLoadoutPanel(tF2Class);
    }

    public void OpenInventoryClick()
    {
        //SetActivePanel(inventoryPanel.gameObject.name);
        //inventoryPanel.OpenInventory();
        PopupInventory popupInventory = GUIManager.instan.CreatePopup<PopupInventory>();
        popupInventory.inventoryPanel.OpenInventory();
    }
    
    public void OpenCrateClick()
    {
        SetActivePanel(cratesPanel.gameObject.name);
        cratesPanel.OpenCratePanel();
    }

    public void OpenLoadoutClick()
    {
        SetActivePanel(loadoutSlotsPanel.gameObject.name);
    }

    public void OpenCraft()
    {
        GUIManager.instan.CreatePopup<PopupCrafting>();
    }

    private void SetActivePanel(string panelName)
    {
        inventoryPanel.gameObject.SetActive(panelName == inventoryPanel.gameObject.name);
        loadoutSlotsPanel.gameObject.SetActive(panelName == loadoutSlotsPanel.gameObject.name);
        cratesPanel.gameObject.SetActive(panelName == cratesPanel.gameObject.name);
    }

    public void Back()
    {
        HomeManager.instance.ResetCharacterRotate();

        if (inventoryPanel.gameObject.activeInHierarchy) inventoryPanel.gameObject.SetActive(false);
        else if (loadoutSlotsPanel.gameObject.activeInHierarchy) loadoutSlotsPanel.gameObject.SetActive(false);
        else if (cratesPanel.gameObject.activeInHierarchy) cratesPanel.gameObject.SetActive(false);

        else Dismiss();
    }

    public void OnManncoCatalogClick()
    {
        HomeManager.instance.OnWatchAdsClick();
    }

    public void CheckTradingOpen()
    {
        if (GameManager.instan.today.DayOfWeek == ConfigManager.tradingData.openDay)
        {
            //tradingBtn.sprite = tradingIcons[1];
            txtTimeTradingOpen.text = string.Format(LocalizationManager.Localize("trading_opening"));
        }

        else
        {
            //tradingBtn.sprite = tradingIcons[0];
            txtTimeTradingOpen.text = string.Format(LocalizationManager.Localize("trading_open_time"),
                LocalizationManager.Localize(ConfigManager.tradingData.openDay.ToString()));
        }
    }

    public void OnTradingClick()
    {
        if (GameManager.instan.today.DayOfWeek == ConfigManager.tradingData.openDay)
        {
            GUIManager.instan.CreatePopup<PopupTrading>();
        }
        else
        {
            GUIManager.instan.CreatePopup<PopupNotify>(string.Format(LocalizationManager.Localize("trading_open_time"),
                LocalizationManager.Localize(ConfigManager.tradingData.openDay.ToString())));
        }
    }
}
