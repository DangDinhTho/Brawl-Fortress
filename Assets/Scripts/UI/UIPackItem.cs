using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPackItem : MonoBehaviour
{
    public Transform itemContainer;
    public TMP_Text txtPackName;
    public Text txtTimeRemaining;
    public TMP_Text txtPrice;
    private ItemConfig packConfig;
        
    public void SetData(ItemConfig packConfig)
    {
        this.packConfig = packConfig;
        var packData = packConfig.itemProperties.packData;

        if (packData.expirationDate > 0 && packData.expirationDate < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            gameObject.SetActive(false);
            return;
        }

        txtPackName.text = packConfig.catalogItem.DisplayName;
        txtPrice.text = $"${packConfig.catalogItem.VirtualCurrencyPrices["RM"] / 100f}";

        int index = 0;
        if(packData.currncies != null)
            foreach (var currency in packData.currncies)
            {
                var elementItem = GUIManager.instan.CreateListItem<UIPackElementItem>(itemContainer, index);
                elementItem.SetCurrencyData(currency);
                index++;
            }
        
        if(packData.itemDatas != null)
            foreach (var itemData in packData.itemDatas)
            {
                var elementItem = GUIManager.instan.CreateListItem<UIPackElementItem>(itemContainer, index);
                elementItem.SetItemData(itemData);
                index++;
            }
        
        GUIManager.instan.CleanListItem(itemContainer, index);

        

        if (packData.expirationDate > 0)
        {
            txtTimeRemaining.gameObject.SetActive(true);
            var timeRemaining = packData.expirationDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            StartCoroutine(CountTime(timeRemaining));
        }
        else
        {
            txtTimeRemaining.gameObject.SetActive(false);
        }
    }

    public void OnBuyClick()
    {
        IAPManager.instan.BuyProductID(packConfig.itemId.ToString());
    }



    IEnumerator CountTime(long totalTime)
    {
        while (totalTime > 0)
        {
            var timeRemaining = TimeSpan.FromSeconds(totalTime);
            txtTimeRemaining.text = string.Format(LocalizationManager.Localize("date_exp_day"), timeRemaining.Days, timeRemaining.Hours, timeRemaining.Minutes, timeRemaining.Seconds);
            yield return new WaitForSeconds(1);
            totalTime--;
        }

        gameObject.SetActive(false);
    }
}
