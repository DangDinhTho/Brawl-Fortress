using System;
using System.Collections;
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
    private Coroutine countdown;

    public void SetData(ItemConfig packConfig)
    {
        this.packConfig = packConfig;
        var packData = packConfig.itemProperties?.packData;

        // Item is reused by CreateListItem: stop the countdown of the previous pack.
        if (countdown != null)
        {
            StopCoroutine(countdown);
            countdown = null;
        }

        if (packData == null || (packData.expirationDate > 0 && packData.expirationDate <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
        {
            gameObject.SetActive(false);
            return;
        }

        txtPackName.text = packConfig.catalogItem.DisplayName;
        txtPrice.text = GetPriceText();

        int index = 0;
        if (packData.currncies != null)
            foreach (var currency in packData.currncies)
            {
                var elementItem = GUIManager.instan.CreateListItem<UIPackElementItem>(itemContainer, index);
                elementItem.SetCurrencyData(currency);
                index++;
            }

        if (packData.itemDatas != null)
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
            countdown = StartCoroutine(CountTime(packData.expirationDate));
        }
        else
        {
            txtTimeRemaining.gameObject.SetActive(false);
        }
    }

    /// <summary>Store price in the player's currency; falls back to the PlayFab "RM" price (cents, USD).</summary>
    private string GetPriceText()
    {
        string local = IAPManager.instan != null ? IAPManager.instan.GetLocalizedPrice(packConfig.itemId.ToString()) : null;
        if (!string.IsNullOrEmpty(local)) return local;

        var prices = packConfig.catalogItem.VirtualCurrencyPrices;
        if (prices != null && prices.TryGetValue("RM", out var cents))
            return $"${cents / 100f:0.##}";
        return "";
    }

    public void OnBuyClick()
    {
        if (IAPManager.instan == null || !IAPManager.instan.IsInitialized)
        {
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("disconected"));
            return;
        }
        IAPManager.instan.BuyProductID(packConfig.itemId.ToString());
    }

    private void OnEnable()
    {
        // The localized price may arrive after the store popup was built.
        if (packConfig != null && txtPrice != null) txtPrice.text = GetPriceText();
    }

    IEnumerator CountTime(long expirationUnix)
    {
        var wait = new WaitForSecondsRealtime(1);
        while (true)
        {
            long remaining = expirationUnix - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (remaining <= 0) break;
            var t = TimeSpan.FromSeconds(remaining);
            txtTimeRemaining.text = string.Format(LocalizationManager.Localize("date_exp_day"), t.Days, t.Hours, t.Minutes, t.Seconds);
            yield return wait;
        }
        countdown = null;
        gameObject.SetActive(false);
    }
}
