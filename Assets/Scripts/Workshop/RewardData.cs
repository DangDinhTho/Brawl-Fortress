using System.Collections;
using System.Collections.Generic;
using PlayFab.ClientModels;
using UnityEditor;
using UnityEngine;

public class RewardData
{
    public List<ItemData> itemDatas;
    public List<VirtualCurrncy> virtualCurrncies;
}

public class ItemData
{
    public ItemId itemId;
    public int quantity;
}


public class VirtualCurrncy
{
    public TF2Currency currency;
    public int quantity;
}