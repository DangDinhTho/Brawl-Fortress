using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILoadoutItem : MonoBehaviour
{
    public Image icon;

    [HideInInspector]
    public ItemConfig itemConfig;
    public InventoryPanel inventoryPanel;
    private ItemId itemId;
    public Text txtName;
    public Text txtQuantity;

    public void SetItem(ItemId itemId)
    {
        this.itemId = itemId;
        icon.gameObject.SetActive(true);
        itemConfig = ConfigManager.GetItemConfig(itemId);
        icon.sprite = GUIManager.instan.GetItemIcon(itemId);
        if (txtName)
        {
            if (itemConfig != null && itemConfig.catalogItem != null)
                txtName.text = itemConfig.catalogItem.DisplayName;
            else
                txtName.text = LocalizationManager.Localize(itemId.ToString());
        }
        if (txtQuantity)
        {
            if(itemConfig != null && itemConfig.catalogItem != null && itemConfig.catalogItem.Consumable != null && itemConfig.catalogItem.Consumable.UsageCount.HasValue)
            {
                UserItem userItem = GameManager.instan.userData.GetUserItem(itemId);
                if(userItem != null)
                {
                    txtQuantity.gameObject.SetActive(true);
                    txtQuantity.text = userItem.itemInstance.RemainingUses.GetValueOrDefault().ToString();
                }
                else
                {
                    txtQuantity.gameObject.SetActive(false);
                }

            }
            else
            {
                txtQuantity.gameObject.SetActive(false);
            }
        }
    }

    public void SetEmptyItem()
    {
        this.itemId = ItemId.None;
        icon.gameObject.SetActive(false);
        itemConfig = null;
        //icon.sprite = GUIManager.instan.GetItemIcon(ItemId.None);
        //if (txtName)
        //{
            //if (itemConfig != null && itemConfig.catalogItem != null)
            //    txtName.text = itemConfig.catalogItem.DisplayName;
            //else
        txtName.text = LocalizationManager.Localize("slot_empty");
        if (txtQuantity) txtQuantity.gameObject.SetActive(false);
        //}
    }



    public void OnClickItem()
    {
        inventoryPanel.OnSelectItem(itemId);
    }
}
