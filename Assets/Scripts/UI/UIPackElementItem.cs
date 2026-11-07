using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPackElementItem : MonoBehaviour
{
    public Image icon;
    public Text txtQuantity;

    public void SetCurrencyData(VirtualCurrncy currncy)
    {
        icon.sprite = GUIManager.instan.iconAtlas.GetSprite(currncy.currency.ToString());
        txtQuantity.text = currncy.quantity.ToString();
    }

    public void SetItemData(ItemData itemData)
    {
        icon.sprite = GUIManager.instan.GetItemIcon(itemData.itemId);
        txtQuantity.text = itemData.quantity.ToString();
    }
}
