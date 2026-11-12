using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupCrafting : PopupBase
{
    public Transform resourceContainer;
    public Transform currencyContainer;
    public Transform itemContainer;
    public Transform highlight;

    public Dropdown classFilter;
    public Dropdown itemTypeFilter;

    private TF2Class tf2Class = TF2Class.None;
    private ItemType itemType = ItemType.all;

    private ItemId itemIdSelected;

    private Dictionary<TF2Currency, UICrafResourceItem> uiCurrencyResources =
        new Dictionary<TF2Currency, UICrafResourceItem>();

    public Text txtChance;
    private TF2Currency mainCurrency;
    private int mainCurrencyQuantity;
    private int useCurrency;
    private float mainChance;

    private bool canCraft;
    private bool crafting;

    public GameObject hidePanel;
    public GameObject craftInfo;

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);

        craftInfo.SetActive(false);
        itemIdSelected = ItemId.None;
        crafting = false;
        tf2Class = TF2Class.None;
        itemType = ItemType.all;

        // Option 0 = all classes (tf2Class == None), so the dropdown matches the list shown.
        classFilter.onValueChanged.RemoveListener(OnClassFilterChange);
        classFilter.ClearOptions();
        List<string> classOptions = new List<string> { LocalizationManager.Localize(ItemType.all.ToString()) };
        for (int i = 1; i < 10; i++)
        {
            classOptions.Add(LocalizationManager.Localize(((TF2Class)i).ToString()));
        }
        classFilter.AddOptions(classOptions);
        classFilter.SetValueWithoutNotify(0);
        classFilter.onValueChanged.AddListener(OnClassFilterChange);

        itemTypeFilter.onValueChanged.RemoveListener(OnItemFilterChange);
        itemTypeFilter.ClearOptions();
        List<string> itemOptions = new List<string>();
        itemOptions.Add(LocalizationManager.Localize(ItemType.all.ToString()));
        for (int i = 1; i < 4; i++)
        {
            itemOptions.Add(LocalizationManager.Localize(((ItemType)i).ToString()));
        }
        for (int i = 21; i < 25; i++)
        {
            itemOptions.Add(LocalizationManager.Localize(((ItemType)i).ToString()));
        }
        itemTypeFilter.AddOptions(itemOptions);
        itemTypeFilter.SetValueWithoutNotify(0);
        itemTypeFilter.onValueChanged.AddListener(OnItemFilterChange);

        UpdateListItem();
    }

    public void OnClassFilterChange(int value)
    {
        tf2Class = value == 0 ? TF2Class.None : (TF2Class)value;
        UpdateListItem();
    }

    public void OnItemFilterChange(int value)
    {
        if (value == 0) itemType = ItemType.all;
        else if (value > 0 && value < 4)
            itemType = (ItemType)(value);
        else
        {
            itemType = (ItemType)(value + 17);
        }
        UpdateListItem();
    }

    public void UpdateListItem()
    {
        highlight.gameObject.SetActive(false);
        int index = 0;
        foreach (var item in ConfigManager.itemConfigs.Values)
        {
            if (item.itemProperties?.craftData?.resources == null) continue;
            bool typeOk = itemType == ItemType.all || item.slot == itemType;
            bool classOk = tf2Class == TF2Class.None || (item.tf2Classes != null && item.tf2Classes.Contains(tf2Class));
            if (typeOk && classOk)
            {
                UICraftingItem craftingItem = GUIManager.instan.CreateListItem<UICraftingItem>(itemContainer, index);
                craftingItem.SetItem(item);
                index++;
            }
        }

        GUIManager.instan.CleanListItem(itemContainer, index);
    }

    public void OnSelectItem(ItemId itemId)
    {
        ItemConfig itemConfig = ConfigManager.GetItemConfig(itemId);
        var craftData = itemConfig?.itemProperties?.craftData;
        if (craftData == null) return;

        itemIdSelected = itemId;
        uiCurrencyResources = new Dictionary<TF2Currency, UICrafResourceItem>();
        canCraft = true;

        // Reset values of the previous selection
        mainCurrency = craftData.mainCurrency;
        mainChance = craftData.chance;
        mainCurrencyQuantity = 0;
        useCurrency = 0;

        InitListResource(craftData.resources);
        InitListCurrency(craftData.currncies);

        UpdateChance();
        craftInfo.SetActive(true);
    }

    public void InitListResource(ItemData[] resources)
    {
        int index = 0;
        if (resources != null)
        {
            foreach (var item in resources)
            {
                int availbleResource = GameManager.instan.userData.GetUserItems(item.itemId).Count;
                UICrafResourceItem resourceItem = GUIManager.instan.CreateListItem<UICrafResourceItem>(resourceContainer, index);
                resourceItem.SetData(item.itemId, availbleResource, item.quantity);
                if (availbleResource < item.quantity) canCraft = false;
                index++;
            }
        }

        GUIManager.instan.CleanListItem(resourceContainer, index);
    }

    public void InitListCurrency(VirtualCurrncy[] currncies)
    {
        int index = 0;
        if (currncies != null)
        {
            foreach (var currncy in currncies)
            {
                int availbleCurrency = GameManager.instan.userData.GetUserCurrency(currncy.currency);
                int quantityUse = Mathf.Min(availbleCurrency, currncy.quantity);
                UICrafResourceItem resourceItem = GUIManager.instan.CreateListItem<UICrafResourceItem>(currencyContainer, index);
                resourceItem.SetData(currncy.currency, quantityUse, currncy.quantity);

                if (availbleCurrency == 0) canCraft = false;

                uiCurrencyResources[currncy.currency] = resourceItem;
                if (currncy.currency == mainCurrency)
                {
                    mainCurrencyQuantity = currncy.quantity;
                    useCurrency = quantityUse;
                }
                else if (availbleCurrency < currncy.quantity)
                {
                    canCraft = false;
                }

                index++;
            }
        }

        GUIManager.instan.CleanListItem(currencyContainer, index);
    }

    public void UpdateChance()
    {
        float chance = mainCurrencyQuantity > 0 ? useCurrency * 1.0f / mainCurrencyQuantity * mainChance : mainChance;
        txtChance.text = $"{chance:0.#}%";
    }

    public void AddChance()
    {
        if (!uiCurrencyResources.ContainsKey(mainCurrency)) return;
        if (useCurrency >= mainCurrencyQuantity || useCurrency >= GameManager.instan.userData.GetUserCurrency(mainCurrency)) return;
        useCurrency++;
        uiCurrencyResources[mainCurrency].UpdateUseQuantity(useCurrency);
        UpdateChance();
    }

    public void ReduceChance()
    {
        if (!uiCurrencyResources.ContainsKey(mainCurrency)) return;
        if (useCurrency <= 1) return;
        useCurrency--;
        uiCurrencyResources[mainCurrency].UpdateUseQuantity(useCurrency);
        UpdateChance();
    }

    public void OnCraftClick()
    {
        if (itemIdSelected == ItemId.None || crafting) return;

        if (!canCraft)
        {
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("carft_not_enought_resource"));
            return;
        }

        string confirmContent = string.Format(LocalizationManager.Localize("crafting_confirm"),
            LocalizationManager.Localize(itemIdSelected.ToString()));
        GUIManager.instan.CreatePopup<PopupConfirm>(confirmContent, (Action)Craft, GUIManager.instan.GetItemIcon(itemIdSelected));
    }

    private void Craft()
    {
        if (crafting) return;
        crafting = true;
        hidePanel.SetActive(true);
        GameManager.instan.RequestCraft(itemIdSelected, useCurrency, OnCraftResponse, OnCraftError);
    }

    public void OnCraftResponse(bool result)
    {
        crafting = false;
        hidePanel.SetActive(false);
        craftInfo.SetActive(false);
        GUIManager.instan.CreatePopup<PopupOpenCrate>(result ? itemIdSelected : ItemId.None);
    }

    /// <summary>Network / server error: nothing was consumed, keep the selection so the player can retry.</summary>
    private void OnCraftError()
    {
        crafting = false;
        hidePanel.SetActive(false);
        GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("disconected"));
    }
}
