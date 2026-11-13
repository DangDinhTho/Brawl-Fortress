using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICraftingItem : MonoBehaviour
{
    public Image icon;
    public Text txtName;

    [HideInInspector]
    public ItemConfig itemConfig;
    public PopupCrafting popupCrafting;

    public void SetItem(ItemConfig itemConfig)
    {
        this.itemConfig = itemConfig;
        icon.sprite = GUIManager.instan.GetItemIcon(itemConfig.itemId);
        txtName.text = itemConfig.catalogItem.DisplayName;
    }


    public void OnClickItem()
    {
        popupCrafting.OnSelectItem(itemConfig.itemId);
        popupCrafting.highlight.gameObject.SetActive(true);
        popupCrafting.highlight.position = transform.position;
        popupCrafting.highlight.parent = this.transform;
    }
}