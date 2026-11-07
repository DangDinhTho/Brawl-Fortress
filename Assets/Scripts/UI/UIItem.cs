using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIItem : MonoBehaviour
{
    public Image icon;
    public Text txtQuantity;
    
    public void SetItem(ItemData itemData)
    {
        icon.sprite = GUIManager.instan.GetItemIcon(itemData.itemId);
        if (itemData.quantity > 0 && txtQuantity)
        {
            txtQuantity.text = itemData.quantity.ToString();
        }
    }
    
    public void SetCurrency(VirtualCurrncy currncy)
    {
        icon.sprite = GUIManager.instan.iconAtlas.GetSprite(currncy.currency.ToString());
        if (currncy.quantity > 0 && txtQuantity)
        {
            txtQuantity.text = currncy.quantity.ToString();
        }
    }
}
