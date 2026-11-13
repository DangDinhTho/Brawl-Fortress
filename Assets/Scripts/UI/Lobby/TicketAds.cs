using System;

/// <summary>Not enough tickets for a map: opens PopupTicketNotEnough with two choices,
/// watch a rewarded ad for +TicketsPerAd ticket (only when title data can_get_ticket_by_ads allows it) or open the store.</summary>
public static class TicketAds
{
    /// <summary>Tickets granted by one rewarded video, whatever the map costs (must match CloudScript GetTicketByAds).</summary>
    public const int TicketsPerAd = 1;

    static bool busy;

    public static void Offer(Map map, Action onGranted = null)
    {
        int needed = UnityEngine.Mathf.Max(1, ConfigManager.GetTicketCost(map));
        Action watch = ConfigManager.canGetTicketByAds ? (Action)(() => WatchAd(map, onGranted)) : null;
        GUIManager.instan.CreatePopup<PopupTicketNotEnough>(needed, watch, (Action)(() => GUIManager.instan.CreatePopup<PopupStore>()), TicketsPerAd);
    }

    static void WatchAd(Map map, Action onGranted)
    {
        if (busy) return;
        AdsManager.instan.ShowRewardAds(() =>
        {
            if (busy) return;
            busy = true;
            var loading = GUIManager.instan.CreatePopup<PopupLoading>();
            GameManager.instan.RequestTicketByAds(map, () =>
            {
                busy = false;
                if (loading != null) loading.Dismiss();
                ShowTicketReceived(map);
                // callers re-check: with 1 ticket per video the player may still need more for this map
                onGranted?.Invoke();
            }, limit =>
            {
                busy = false;
                if (loading != null) loading.Dismiss();
                GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize(limit ? "ticket_by_ads_limit" : "disconected"));
            });
        });
    }

    /// <summary>"You received" popup (PopupReward, receive button only) for the ticket a rewarded video just granted.
    /// Shows the ticket item the server used for this map (same match order as CloudScript), else hale_ticket.</summary>
    static void ShowTicketReceived(Map map)
    {
        var user = GameManager.instan != null ? GameManager.instan.userData : null;
        var owned = user != null ? user.GetAnyStickerWithMap(map, 1) : null;
        var reward = new RewardData
        {
            virtualCurrncies = new System.Collections.Generic.List<VirtualCurrncy>(),
            itemDatas = new System.Collections.Generic.List<ItemData>
            {
                new ItemData { itemId = owned != null ? owned.itemId : ItemId.hale_ticket, quantity = TicketsPerAd }
            }
        };
        GUIManager.instan.CreatePopup<PopupReward>(reward);
    }
}
