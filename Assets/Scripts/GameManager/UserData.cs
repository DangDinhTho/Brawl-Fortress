using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UserData
{
    public string userId;
    public string userName;
    public List<UserItem> userItems = new List<UserItem>();
    public UserCurrency currency = new UserCurrency();
    public int dayLast;
    public int mocDaNhan;
    public int oldScore;
    public int score;
    public int matchCount;
    /// <summary>Player level from the rank score (ConfigManager.scorePerLevel points per level).</summary>
    public int Level => ConfigManager.GetLevel(score);
    /// <summary>Progress 0..1 towards the next level.</summary>
    public float LevelProgress => ConfigManager.GetLevelProgress(score);
    public bool ads_removed = false;

    
    public UserItem GetUserItem(ItemId itemId)
    {
        UserItem item = userItems.Find(e => e.itemId == itemId);
        return item;
    }
    
    public List<UserItem> GetUserItems(ItemId itemId = ItemId.None)
    {
        List<UserItem> res = new List<UserItem>();
        foreach (var item in userItems)
        {
            if (itemId == ItemId.None || item.itemId == itemId)
            {
                res.Add(item);
            }
        }

        return res;
    }

    public List<UserItem> GetUserItemsWithType(ItemType type)
    {
        List<UserItem> res = new List<UserItem>();
        foreach (var item in userItems)
        {
            ItemConfig itConfig = ConfigManager.GetItemConfig(item.itemId);
            if (itConfig != null && itConfig.slot == type)
            {
                res.Add(item);
            }
        }

        return res;
    }
    
    public List<UserItem> GetUserStickerWithMap(Map map)
    {
        List<UserItem> res = new List<UserItem>();
        foreach (var item in userItems)
        {
            ItemConfig itConfig = ConfigManager.GetItemConfig(item.itemId);
            if (itConfig != null && itConfig.slot == ItemType.ticket && itConfig.catalogItem.Tags != null && itConfig.catalogItem.Tags.Contains(map.ToString()))
            {
                res.Add(item);
            }
        }
        return res;
    }
    
    
    public UserItem GetAnyStickerWithMap(Map map, int numTickets)
    {
        foreach (var item in userItems)
        {
            ItemConfig itConfig = ConfigManager.GetItemConfig(item.itemId);
            if (itConfig != null && itConfig.slot == ItemType.ticket && itConfig.catalogItem.Tags != null && itConfig.catalogItem.Tags.Contains(map.ToString()))
            {
                if (item.itemInstance.RemainingUses.GetValueOrDefault() >= numTickets) return item;
            }
        }
        return null;
    }

    public bool UseItem(ItemId itemId, int quantity)
    {
        UserItem item = GetUserItem(itemId);
        if (item == null) return false;
        item.itemInstance.RemainingUses -= quantity;
        return true;
    }


    public List<UserItem> GetUserStickerWithGameMode(GameMode gameMode)
    {
        List<UserItem> res = new List<UserItem>();
        foreach (var item in userItems)
        {
            ItemConfig itConfig = ConfigManager.GetItemConfig(item.itemId);
            if (itConfig != null && itConfig.slot == ItemType.ticket && itConfig.catalogItem.Tags != null && itConfig.catalogItem.Tags.Contains(gameMode.ToString()))
            {
                res.Add(item);
            }
        }
        return res;
    }

    public List<UserItem> GetUserTaunts()
    {
        List<UserItem> res = new List<UserItem>();
        foreach (var item in userItems)
        {
            ItemConfig itConfig = ConfigManager.GetItemConfig(item.itemId);
            if (itConfig != null && itConfig.slot == ItemType.taunt)
            {
                //Taunt taunt = ConfigManager.GetTauntFromItemId(item.itemId);
                res.Add(item);
            }
        }

        return res;
    }

    public void RemoveUserItem(ItemId itemId)
    {
        int indexToRemove = userItems.FindIndex(e => e.itemId == itemId);
        if (indexToRemove >= 0)
        {
            userItems.RemoveAt(indexToRemove);
        }
    }


    public int GetUserCurrency(TF2Currency _currency)
    {
        switch (_currency)
        {
            case TF2Currency.GK:
                return currency.goldKey;
            case TF2Currency.CM:
                return currency.scrapMetal;
            case TF2Currency.MM:
                return currency.reclaimedMetal;
            default: return currency.goldKey;
        }
    }
    
    public bool IsReduceableCurrency(VirtualCurrncy _currency)
    {
        switch (_currency.currency)
        {
            case TF2Currency.GK:
                return currency.goldKey >= _currency.quantity;
            case TF2Currency.CM:
                return currency.scrapMetal >= _currency.quantity;
            case TF2Currency.MM:
                return currency.reclaimedMetal >= _currency.quantity;
            default: return false;
        }
    }
}

public class UserCurrency
{
    public int goldKey;
    public int scrapMetal;
    public int reclaimedMetal;

    public Action OnValueChange;

    public void UpdateVitualCurrency(int goldKey, int scMetal, int reMetal)
    {
        this.goldKey = goldKey;
        this.scrapMetal = scMetal;
        this.reclaimedMetal = reMetal;
        OnValueChange?.Invoke();
    }

    public void UpdateGoldKey(int goldKey)
    {
        this.goldKey = goldKey;
        OnValueChange?.Invoke();
    }

    public void Spend(int goldKey, int scMetal, int reMetal)
    {
        this.goldKey -= goldKey;
        this.scrapMetal -= scMetal;
        this.reclaimedMetal -= reMetal;
        OnValueChange?.Invoke();
    }

    public void AddCurrency(int goldKey, int scMetal, int reMetal)
    {
        this.goldKey += goldKey;
        this.scrapMetal += scMetal;
        this.reclaimedMetal += reMetal;
        OnValueChange?.Invoke();
    }
    
    public void AddCurrency(VirtualCurrncy _currency)
    {
        switch (_currency.currency)
        {
            case TF2Currency.GK:
                 goldKey += _currency.quantity;
                break;
            case TF2Currency.CM:
                scrapMetal += _currency.quantity;
                break;
            case TF2Currency.MM:
                reclaimedMetal += _currency.quantity;
                break;
        }
        OnValueChange?.Invoke();
    }
    
    public void SpendCurrency(VirtualCurrncy _currency)
    {
        switch (_currency.currency)
        {
            case TF2Currency.GK:
                goldKey -= _currency.quantity;
                break;
            case TF2Currency.CM:
                scrapMetal -= _currency.quantity;
                break;
            case TF2Currency.MM:
                reclaimedMetal -= _currency.quantity;
                break;
        }
        OnValueChange?.Invoke();
    }

}

[Serializable]
public class ListScoreLvl
{
    public int[] mocScores;
}
