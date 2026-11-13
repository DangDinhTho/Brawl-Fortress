using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Not enough tickets" popup used only by TicketAds.Offer (PopupConfirm stays untouched for its other callers).
/// Close (X) in the top-right corner, two choices at the bottom: watch a video for tickets / open the store.
/// args: [0] int tickets needed, [1] Action watch video (null = hide that button), [2] Action open store, [3] int tickets one video gives (default 1).
/// </summary>
public class PopupTicketNotEnough : PopupBase
{
    public Text txtContent;
    public Button closeBtn;
    public Button watchAdBtn;
    public TMP_Text txtWatchAd;
    public Button storeBtn;
    public TMP_Text txtStore;
    [Tooltip("x position of each button when both are shown; a single button is centred")]
    public float buttonSpacingX = 135f;

    private Action onWatchAd;
    private Action onStore;

    private void Awake()
    {
        if (closeBtn) closeBtn.onClick.AddListener(Dismiss);
        if (watchAdBtn) watchAdBtn.onClick.AddListener(() => Choose(onWatchAd));
        if (storeBtn) storeBtn.onClick.AddListener(() => Choose(onStore));
    }

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        int amount = args.Length > 0 && args[0] is int a ? Mathf.Max(1, a) : 1;
        int adReward = args.Length > 3 && args[3] is int r ? Mathf.Max(1, r) : 1;
        onWatchAd = args.Length > 1 ? args[1] as Action : null;
        onStore = args.Length > 2 ? args[2] as Action : null;

        if (txtContent) txtContent.text = LocalizationManager.Localize("ticket_not_enough_msg", amount);
        if (txtWatchAd) txtWatchAd.text = LocalizationManager.Localize("ticket_watch_ad_btn", adReward);
        if (txtStore) txtStore.text = LocalizationManager.Localize("shop");

        bool showAd = onWatchAd != null;
        if (watchAdBtn) watchAdBtn.gameObject.SetActive(showAd);
        if (storeBtn)
        {
            var rt = (RectTransform)storeBtn.transform;
            rt.anchoredPosition = new Vector2(showAd ? buttonSpacingX : 0f, rt.anchoredPosition.y);
        }
        if (watchAdBtn)
        {
            var rt = (RectTransform)watchAdBtn.transform;
            rt.anchoredPosition = new Vector2(-buttonSpacingX, rt.anchoredPosition.y);
        }
    }

    private void Choose(Action action)
    {
        Dismiss();
        action?.Invoke();
    }
}
