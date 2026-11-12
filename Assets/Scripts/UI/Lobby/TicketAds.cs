using System;

/// <summary>Offers 'watch a rewarded ad to get tickets' when the player lacks tickets for a map.
/// Falls back to the store when the feature is disabled (title data can_get_ticket_by_ads).</summary>
public static class TicketAds
{
    static bool busy;

    public static void Offer(Map map, Action onGranted = null)
    {
        if (!ConfigManager.canGetTicketByAds)
        {
            GUIManager.instan.CreatePopup<PopupConfirm>(
                LocalizationManager.Localize("not_enough_ticket"), (Action)(() => GUIManager.instan.CreatePopup<PopupStore>()));
            return;
        }

        int amount = 1;
        if (ConfigManager.mapConfigs.TryGetValue(map, out var cfg) && cfg.stickPerPlay > 0) amount = cfg.stickPerPlay;

        GUIManager.instan.CreatePopup<PopupConfirm>(
            string.Format(LocalizationManager.Localize("ticket_by_ads_confirm"), amount), (Action)(() =>
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
                        onGranted?.Invoke();
                    }, limit =>
                    {
                        busy = false;
                        if (loading != null) loading.Dismiss();
                        GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize(limit ? "ticket_by_ads_limit" : "disconected"));
                    });
                });
            }));
    }
}
