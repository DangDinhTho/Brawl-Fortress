using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIWorkShopItem : MonoBehaviour
{
    public Image icon;
    public Text txtName;
    public Text txtPrice;
    public Image currncyIcon;
    public Image statusIcon;

    [HideInInspector]
    public ItemConfig itemConfig;
    public PopupWorkShop popupWorkShop;

    public void SetItem(ItemConfig itemConfig)
    {
        this.itemConfig = itemConfig;
        icon.sprite = GUIManager.instan.GetItemIcon(itemConfig.itemId);
        txtName.text = itemConfig.catalogItem.DisplayName;
        if (itemConfig.catalogItem.VirtualCurrencyPrices.TryGetValue("GK", out var price))
        {
            txtPrice.gameObject.SetActive(true);
            txtPrice.text = price.ToString();
        }
        else
        {
            txtPrice.gameObject.SetActive(false);
        }

        if (string.IsNullOrEmpty(itemConfig.catalogItem.Description))
        {
            statusIcon.gameObject.SetActive(false);
        }
        else
        {
            statusIcon.gameObject.SetActive(true);
            statusIcon.sprite = GUIManager.instan.iconAtlas.GetSprite(itemConfig.catalogItem.Description);
        }
        
    }

    public void SetWeapon(WeaponId weaponId)
    {
        //itemConfig = ConfigManager.GetItemConfig(itemId);
        icon.sprite = GUIManager.instan.iconAtlas.GetSprite(weaponId.ToString().ToLower());
    }

    public void OnClickItem()
    {
        popupWorkShop.OnSelectItem(itemConfig.itemId);
        popupWorkShop.highlight.gameObject.SetActive(true);
        popupWorkShop.highlight.position = transform.position;
        popupWorkShop.highlight.parent = this.transform;
    }
}
