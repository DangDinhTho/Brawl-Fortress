using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftResultController : MonoBehaviour
{
    public Image icon;
    public TMP_Text txtName;


    public void SetData(ItemId _itemId)
    {
        
        txtName.gameObject.SetActive(true);

        GUIManager.instan.GetItemIcon(_itemId);
        if (_itemId == ItemId.None)
        {
            icon.gameObject.SetActive(false);
            //icon.sprite = GUIManager.instan.iconAtlas.GetSprite("crate_empty");
            txtName.text = LocalizationManager.Localize("crafting_fail");
        }
        else
        {
            icon.gameObject.SetActive(true);
            icon.sprite = GUIManager.instan.GetItemIcon(_itemId);
            txtName.text = ConfigManager.GetItemConfig(_itemId).catalogItem.DisplayName;
        }
    }

    public void Hide()
    {
        icon.gameObject.SetActive(false);
        txtName.gameObject.SetActive(false);
    }

}
