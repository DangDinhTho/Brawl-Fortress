using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// args[0] = RewardData, args[1] (optional) = double source ("endgame", "daily"...).
/// When a source is given, a "Watch ad: x2" button is shown (rewarded ad, opt-in),
/// on the same row as the receive button (the receive button is centred when x2 is not offered).
/// </summary>
public class PopupReward : PopupBase
{
    private RewardData rewardData;
    public Transform itemContainer;
    public GameObject itemPref;

    [Tooltip("Button 'Watch ad: x2 reward'. Hidden when the reward cannot be doubled.")]
    public GameObject doubleBtn;
    [Tooltip("Receive / close button, on the same row as doubleBtn")]
    public RectTransform receiveBtn;
    [Tooltip("Horizontal gap between the two buttons")]
    public float buttonGap = 30f;

    private string doubleSource;
    private bool doubling;

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        rewardData = args.Length > 0 ? args[0] as RewardData : null;
        doubleSource = args.Length > 1 ? args[1] as string : null;
        doubling = false;

        if (doubleBtn != null)
        {
            doubleBtn.SetActive(!string.IsNullOrEmpty(doubleSource) && HasAnything(rewardData));
            SetButtonLabel(doubleBtn, LocalizationManager.Localize("x2_reward_btn"));
        }
        LayoutButtons();
        SetReward();
    }

    /// <summary>Both buttons side by side, centred as a pair; the receive button alone is centred.</summary>
    void LayoutButtons()
    {
        if (receiveBtn == null) return;
        bool both = doubleBtn != null && doubleBtn.activeSelf;
        if (!both)
        {
            receiveBtn.anchoredPosition = new Vector2(0f, receiveBtn.anchoredPosition.y);
            return;
        }
        var d = (RectTransform)doubleBtn.transform;
        float wd = d.rect.width, wr = receiveBtn.rect.width;
        float total = wd + buttonGap + wr;
        d.anchoredPosition = new Vector2(-total * 0.5f + wd * 0.5f, receiveBtn.anchoredPosition.y);
        receiveBtn.anchoredPosition = new Vector2(total * 0.5f - wr * 0.5f, receiveBtn.anchoredPosition.y);
    }

    static bool HasAnything(RewardData r)
    {
        return r != null && ((r.virtualCurrncies != null && r.virtualCurrncies.Count > 0) || (r.itemDatas != null && r.itemDatas.Count > 0));
    }

    static void SetButtonLabel(GameObject btn, string text)
    {
        var tmp = btn.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) { tmp.text = text; return; }
        var legacy = btn.GetComponentInChildren<Text>(true);
        if (legacy != null) legacy.text = text;
    }

    /// <summary>Hooked to doubleBtn.onClick in the prefab.</summary>
    public void OnDoubleClick()
    {
        if (doubling || string.IsNullOrEmpty(doubleSource)) return;
        // The rewarded callback only fires when the reward is earned; closing the ad early keeps the button.
        AdsManager.instan.ShowRewardAds(() =>
        {
            if (doubling) return;
            doubling = true;
            GameManager.instan.RequestDoubleReward(doubleSource, OnDoubled, OnDoubleFailed);
        });
    }

    private void OnDoubled(RewardData extra)
    {
        doubleSource = null;
        if (doubleBtn != null) doubleBtn.SetActive(false);
        LayoutButtons();
        rewardData = Merge(rewardData, extra);
        SetReward();
    }

    private void OnDoubleFailed()
    {
        doubling = false;
        doubleSource = null; // already claimed / expired: don't offer it again
        if (doubleBtn != null) doubleBtn.SetActive(false);
        LayoutButtons();
        GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("disconected"));
    }

    /// <summary>Adds quantities of b into a (same currency / item = one entry).</summary>
    static RewardData Merge(RewardData a, RewardData b)
    {
        var res = new RewardData
        {
            virtualCurrncies = new List<VirtualCurrncy>(),
            itemDatas = new List<ItemData>()
        };
        foreach (var src in new[] { a, b })
        {
            if (src == null) continue;
            if (src.virtualCurrncies != null)
                foreach (var c in src.virtualCurrncies)
                {
                    var found = res.virtualCurrncies.Find(x => x.currency == c.currency);
                    if (found != null) found.quantity += c.quantity;
                    else res.virtualCurrncies.Add(new VirtualCurrncy { currency = c.currency, quantity = c.quantity });
                }
            if (src.itemDatas != null)
                foreach (var it in src.itemDatas)
                {
                    var found = res.itemDatas.Find(x => x.itemId == it.itemId);
                    if (found != null) found.quantity += it.quantity;
                    else res.itemDatas.Add(new ItemData { itemId = it.itemId, quantity = it.quantity });
                }
        }
        return res;
    }

    public void SetReward()
    {
        for (int i = 0; i < itemContainer.childCount; i++)
        {
            itemContainer.GetChild(i).gameObject.SetActive(false);
        }

        if (rewardData == null) return;
        int index = 0;

        if (rewardData.virtualCurrncies != null)
            foreach (var currncy in rewardData.virtualCurrncies)
            {
                GetItem(index++).SetCurrency(currncy);
            }

        if (rewardData.itemDatas != null)
            foreach (var item in rewardData.itemDatas)
            {
                GetItem(index++).SetItem(item);
            }
    }

    private UIItem GetItem(int index)
    {
        UIItem uiItem = index < itemContainer.childCount
            ? itemContainer.GetChild(index).GetComponent<UIItem>()
            : Instantiate(itemPref, itemContainer).GetComponent<UIItem>();
        uiItem.gameObject.SetActive(true);
        return uiItem;
    }
}
