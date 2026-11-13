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
    [Tooltip("Left tab bar. The first tab is used when the popup opens (normally 'All').")]
    public MapModeTab[] tabs;
    public ScrollRect mapScroll;
    [Tooltip("Hide tabs that have no map.")]
    public bool hideEmptyTabs = true;
    public float cardStagger = 0.05f;

    private MapModeTab currentTab;
    private readonly List<UIMapIcon> cards = new List<UIMapIcon>();

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
        EnsureUnlockedSelection();
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
        currentTab = null;
        SelectTab(tabs[0], false);
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
        LobbyManager.instance.InsideRoomPanel.OnChangeMap(gameMode, map);
    }

    public void OnDoneClick()
    {
        if (!ModeUnlock.IsUnlocked(gameModeSelected))
        {
            ModeUnlock.ShowLockedPopup(gameModeSelected);
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

    private void OnDisable()
    {
        foreach (var card in cards)
            if (card != null) DOTween.Kill(card);
    }
}
