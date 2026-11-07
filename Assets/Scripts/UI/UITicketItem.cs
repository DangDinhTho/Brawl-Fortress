using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITicketItem : MonoBehaviour
{
    public Image icon;
    public Text txtQuantity;

    public void InitTicket(ItemId ticketId, int quantity)
    {
        icon.sprite = GUIManager.instan.GetItemIcon(ticketId);
        txtQuantity.text = quantity.ToString();
    }

    public void OnClickTicket()
    {
        GUIManager.instan.CreatePopup<PopupStore>();
    }
}
