using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICrafResourceItem : MonoBehaviour
{
    public Image icon;
    private ItemId itemId;
    public Text txtQuantity;
    private int quatityRequired;
    
    public void SetData(ItemId _itemId, int quatityAvailble, int quatityRequired)
    {
        this.quatityRequired = quatityRequired;
        icon.sprite = GUIManager.instan.GetItemIcon(_itemId);
        txtQuantity.text = $"{quatityAvailble}/{quatityRequired}";
    } 
    
    public void SetData(TF2Currency currency, int quatityAvailble, int quatityRequired)
    {
        this.quatityRequired = quatityRequired;
        icon.sprite = GUIManager.instan.iconAtlas.GetSprite(currency.ToString());
        txtQuantity.text = $"{quatityAvailble}/{quatityRequired}";
    }

    public void UpdateUseQuantity(int quantity)
    {
        txtQuantity.text = $"{quantity}/{quatityRequired}";
    }
}
