using Photon.Pun;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public class PopupSelectMap : PopupBase
{
    [HideInInspector]
    public GameMode gameModeSelected = GameMode.Competitive;
    [HideInInspector]
    public Map mapSelected = Map.BrawlScene;

    public Transform mapContainer;
    private bool singlePlayer;
    [HideInInspector]
    public UIMapIcon mapSelectedItem;
    public Transform ticketContainer;

    [Header("Mode tabs")]
    [Tooltip("Left tab bar, one tab per game mode. The popup opens on the tab of the selected map's mode.")]
    public MapModeTab[] tabs;
    public ScrollRect mapScroll;
    [Tooltip("Hide tabs that have no map.")]
    public bool hideEmptyTabs = true;
    public float cardStagger = 0.05f;

    [Header("Map info")]
    [Tooltip("Icon of the \"i\" button each card adds (UIMapIcon.CreateInfoButton), opening PopupMapInfo")]
    public Sprite infoIcon;

    [Header("Card size")]
    [Tooltip("The map cards are zoomed so that this many columns fill the width of the list")]
    public int columns = 3;
    [Tooltip("Most the cards are zoomed (very wide screens)")]
    public float maxCardScale = 1.3f;
    [Tooltip("Below this zoom the list uses one column less (narrow screens, tablets)")]
    public float minCardScale = 0.85f;

    private MapModeTab currentTab;
    private readonly List<UIMapIcon> cards = new List<UIMapIcon>();
    private readonly Dictionary<UIMapIcon, int> cardOrder = new Dictionary<UIMapIcon, int>();
    private float fittedWidth = -1f;
    private bool autoPicked;   // the selected map was picked by a tab switch and costs tickets: confirm on Done

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        gameModeSelected = args.Length > 0 ? (GameMode)args[0] : GameMode.Competitive;
        mapSelected = args.Length > 1 ? (Map)args[1] : Map.BrawlScene;

        SetTicket();
        InitTabs();
    }

    /// <summary>The previous choice (last room map / default) may belong to a mode still locked by level: fall back to Competitive.</summary>
    private void EnsureUnlockedSelection()
    {
        bool locked = !ModeUnlock.IsUnlocked(gameModeSelected);
        foreach (var c in cards)
            if (c != null && c.map == mapSelected && c.IsLocked) locked = true;
        if (!locked) return;
        gameModeSelected = GameMode.Competitive;
        mapSelected = Map.BrawlScene;
        foreach (var c in cards)
            if (c != null && c.gameMode == GameMode.Competitive) { mapSelected = c.map; break; }
    }

    private void InitTabs()
    {
        cards.Clear();
        if (mapContainer) mapContainer.GetComponentsInChildren(true, cards);
        SortCards();
        EnsureUnlockedSelection();
        FitCards();
        if (tabs == null || tabs.Length == 0) return;

        foreach (var tab in tabs)
        {
            if (tab == null) continue;
            tab.popup = this;
            int n = 0;
            foreach (var c in cards) if (tab.Matches(c)) n++;
            tab.SetCount(n);
            tab.gameObject.SetActive(!hideEmptyTabs || tab.isAll || n > 0);
        }
        // open on the selected map's mode
        MapModeTab start = null;
        foreach (var tab in tabs)
            if (tab != null && !tab.isAll && tab.gameMode == gameModeSelected && tab.gameObject.activeSelf) { start = tab; break; }
        if (start == null)
            foreach (var tab in tabs)
                if (tab != null && tab.gameObject.activeSelf) { start = tab; break; }
        currentTab = null;
        if (start == null) return;
        SelectTab(start, false);
        RevealTab(start);
    }

    /// <summary>Free maps first in each mode: the maps Title Data "free_map" makes free (events), then the maps that are
    /// free by default, then the paid ones in card order (= by price).</summary>
    private void SortCards()
    {
        foreach (var c in cards)
            if (c != null && !cardOrder.ContainsKey(c)) cardOrder[c] = cardOrder.Count; // prefab order, kept if the popup is reused
        cards.RemoveAll(c => c == null);
        cards.Sort((a, b) =>
        {
            int ra = FreeRank(a.map), rb = FreeRank(b.map);
            return ra != rb ? ra.CompareTo(rb) : cardOrder[a].CompareTo(cardOrder[b]);
        });
        for (int i = 0; i < cards.Count; i++) cards[i].transform.SetSiblingIndex(i);
    }

    private static int FreeRank(Map map) => ConfigManager.IsFreeByEvent(map) ? 0 : ConfigManager.GetTicketCost(map) <= 0 ? 1 : 2;

    /// <summary>Zooms the card grid so that the columns fill the width of the list (the cards were laid out for 16:9 and
    /// left an empty strip on wider phones; on 4:3 they ran off the right edge). The grid keeps its own layout in
    /// unscaled units: the content is the viewport's width divided by the zoom, anchored top-left.</summary>
    private void FitCards()
    {
        var content = mapContainer as RectTransform;
        var grid = content ? content.GetComponent<GridLayoutGroup>() : null;
        var viewport = mapScroll ? (mapScroll.viewport ? mapScroll.viewport : (RectTransform)mapScroll.transform) : null;
        if (grid == null || viewport == null) return;
        float width = viewport.rect.width;
        if (width <= 1f) return;
        fittedWidth = width;

        int cols = Mathf.Max(1, columns);
        float scale = 1f;
        for (; cols >= 1; cols--)
        {
            scale = width / (grid.padding.horizontal + cols * grid.cellSize.x + (cols - 1) * grid.spacing.x);
            if (scale >= minCardScale || cols == 1) break;
        }
        scale = Mathf.Min(scale, maxCardScale);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0f, 1f);
        content.localScale = new Vector3(scale, scale, 1f);
        content.sizeDelta = new Vector2(width / scale, content.sizeDelta.y);
        content.anchoredPosition = new Vector2(0f, content.anchoredPosition.y);
    }

    private void LateUpdate()
    {
        // screen size changed (editor Game view, split screen): fit the cards again
        var viewport = mapScroll ? mapScroll.viewport : null;
        if (viewport != null && cards.Count > 0 && Mathf.Abs(viewport.rect.width - fittedWidth) > 0.5f)
        {
            FitCards();
            if (mapContainer is RectTransform rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    /// <summary>Scrolls the tab bar so that the given tab is in view (the popup can open on a tab at the bottom).</summary>
    private void RevealTab(MapModeTab tab)
    {
        var sr = tab ? tab.GetComponentInParent<ScrollRect>() : null;
        if (sr == null || sr == mapScroll || sr.content == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(sr.content);
        var view = sr.viewport ? sr.viewport : (RectTransform)sr.transform;
        float contentH = sr.content.rect.height, viewH = view.rect.height;
        if (contentH <= viewH + 0.5f) return;
        var corners = new Vector3[4];
        ((RectTransform)tab.transform).GetWorldCorners(corners);
        // distances from the top of the content, in its own units
        float contentTop = sr.content.rect.yMax;
        float top = contentTop - sr.content.InverseTransformPoint(corners[1]).y;
        float bottom = contentTop - sr.content.InverseTransformPoint(corners[0]).y;
        float range = contentH - viewH;
        float offset = (1f - sr.verticalNormalizedPosition) * range;
        offset = Mathf.Clamp(offset, bottom - viewH, top);
        sr.verticalNormalizedPosition = 1f - Mathf.Clamp01(offset / range);
    }

    /// <summary>Called by MapModeTab when it is clicked.</summary>
    public void SelectTab(MapModeTab tab)
    {
        SelectTab(tab, true);
    }

    public void SelectTab(MapModeTab tab, bool animateTabs)
    {
        if (tab == null || tab == currentTab) return;
        currentTab = tab;
        foreach (var t in tabs)
            if (t != null) t.SetSelected(t == tab, animateTabs);
        ShowCards();
        if (animateTabs) SelectFirstMapOf(tab); // the player switched mode: pick its first map
    }

    /// <summary>A mode tab was clicked: when the selected map belongs to another mode, select the first map of this
    /// mode (only if the mode is unlocked; a locked mode keeps the previous choice).</summary>
    private void SelectFirstMapOf(MapModeTab tab)
    {
        if (tab == null || tab.isAll) return;
        UIMapIcon first = null;
        foreach (var c in cards)
        {
            if (c == null || !tab.Matches(c)) continue;
            if (c.map == mapSelected && c.gameMode == gameModeSelected) return; // already one of this mode's maps
            if (first == null) first = c;
        }
        if (first == null || first.IsLocked) return;
        SelectedMap(first.gameMode, first.map, first);
        first.SetSelect();
        // Boss Battle and Zombie have no free map: the price of a map the player did not tap is checked on Done
        autoPicked = ConfigManager.GetTicketCost(first.map) > 0;
    }

    private void ShowCards()
    {
        int i = 0;
        foreach (var card in cards)
        {
            if (card == null) continue;
            bool show = currentTab.Matches(card);
            DOTween.Kill(card);
            card.gameObject.SetActive(show);
            if (!show) continue;

            var cg = card.GetComponent<CanvasGroup>();
            if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();
            var tr = card.transform;
            float delay = i * cardStagger;
            cg.alpha = 0f;
            tr.localScale = Vector3.one * 0.8f;
            cg.DOFade(1f, 0.25f).SetDelay(delay).SetUpdate(true).SetTarget(card);
            tr.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(delay).SetUpdate(true).SetTarget(card);
            i++;
        }

        FitCards();
        if (mapContainer is RectTransform rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        if (mapScroll)
        {
            mapScroll.StopMovement();
            mapScroll.verticalNormalizedPosition = 1f;
        }
    }

    public void OnGameModeUpdate(GameMode gameMode)
    {
    }

    /// <summary>Re-reads lock and ticket state on every card (after tickets were granted / spent).</summary>
    public void RefreshCards()
    {
        foreach (var c in cards) if (c != null) c.Refresh();
    }

    public void SetTicket()
    {
        int index = 0;
        for(ItemId ticketId = ItemId.Ticket + 1; ticketId < ItemId.EndTicket; ticketId++)
        {
            UserItem ticketData = GameManager.instan.userData.GetUserItem(ticketId);
            int quatity = 0;
            if(ticketData != null)
            {
                quatity = ticketData.itemInstance.RemainingUses.GetValueOrDefault();
            }
            var item = GUIManager.instan.CreateListItem<UITicketItem>(ticketContainer, index);
            item.InitTicket(ticketId, quatity);
            index++;
        }
        GUIManager.instan.CleanListItem(ticketContainer, index);
    }

    public void SelectedMap(GameMode gameMode, Map map, UIMapIcon uIMapIcon)
    {
        if (mapSelected == map) return;
        if (mapSelectedItem != null && mapSelectedItem.gameObject.activeInHierarchy) mapSelectedItem.Deselected();
        else if (mapSelectedItem != null) mapSelectedItem.selectedObj.SetActive(false);
        gameModeSelected = gameMode;
        mapSelected = map;
        mapSelectedItem = uIMapIcon;
        autoPicked = false;
        LobbyManager.instance.InsideRoomPanel.OnChangeMap(gameMode, map);
    }

    public void OnDoneClick()
    {
        if (!ModeUnlock.IsUnlocked(gameModeSelected))
        {
            ModeUnlock.ShowLockedPopup(gameModeSelected);
            return;
        }
        int cost = ConfigManager.GetTicketCost(mapSelected);
        if (cost > 0 && autoPicked)
        {
            // a paid map selected by a tab switch, not tapped (tapping a card asks the same): the player cannot pay
            // for it yet -> ad / store offer; else tell the price (tickets are spent when the match starts)
            if (!ConfigManager.CheckSelectMap(mapSelected))
                TicketAds.Offer(mapSelected, () => { if (this == null) return; SetTicket(); RefreshCards(); });
            else
                GUIManager.instan.CreatePopup<PopupConfirm>(string.Format(LocalizationManager.Localize("using_ticket_confirm"), cost),
                    (System.Action)(() =>
                    {
                        if (this == null) return;
                        autoPicked = false;
                        OnDoneClick();
                    }));
            return;
        }
        if (singlePlayer)
        {
            LobbyManager.instance.sigleGameMode = gameModeSelected;
            LobbyManager.instance.sigleMap = mapSelected;
            GUIManager.instan.CreatePopup<PopupSelectClass>(true);
        }
        else
        {
            Hashtable props = new Hashtable
            {
                {TF2GameProps.GameModeKey, gameModeSelected},
                {TF2GameProps.Map, mapSelected}
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        Dismiss();
    }

    public void SetSinglePlayer()
    {
        singlePlayer = true;
    }

    private void OnEnable()
    {
        GameManager.OnInventoryUpdated += OnInventoryUpdated;
    }

    private void OnDisable()
    {
        GameManager.OnInventoryUpdated -= OnInventoryUpdated;
        foreach (var card in cards)
            if (card != null) DOTween.Kill(card);
    }

    /// <summary>Tickets bought in the store (or won from an ad / gift) while this popup is open: refresh the ticket counts
    /// and the map cards' ticket price colours (red when not affordable).</summary>
    void OnInventoryUpdated()
    {
        SetTicket();
        foreach (var card in cards)
            if (card != null) card.RefreshTicket();
    }
}
