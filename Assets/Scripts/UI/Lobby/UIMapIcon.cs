using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMapIcon : MonoBehaviour
{
    public Image mapIcon;
    //public Text mapName;
    public GameObject selectedObj;
    public GameMode gameMode;
    public Map map;
    public PopupSelectMap popupSelectMap;
    [Tooltip("'N Tickets' price of this map (ConfigManager.GetTicketCost); hidden on free maps")]
    public Text txtStickRequire;
    [Tooltip("Price text colour when the player does not own enough tickets")]
    public Color notEnoughTicketColor = new Color(1f, 0.45f, 0.4f);
    [Tooltip("Badge on the map picture: ticket icon + 'xN' (shown on paid maps)")]
    public GameObject ticketBadge;
    [Tooltip("'xN' text of the ticket badge")]
    public TMP_Text txtTicketCost;

    [Header("Mode lock (ModeUnlock)")]
    [Tooltip("Overlay shown while this card's game mode is locked by level")]
    public GameObject lockObj;
    [Tooltip("'Level N' on the lock overlay")]
    public TMP_Text txtLockLevel;

    public bool IsLocked => !ModeUnlock.IsUnlocked(gameMode);

    private Color ticketColor = Color.white;
    private bool ticketColorCached;

    private void Start()
    {
        selectedObj.SetActive(map == popupSelectMap.mapSelected);
        if (map == popupSelectMap.mapSelected) popupSelectMap.mapSelectedItem = this;
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        RefreshLock();
        RefreshTicket();
    }

    public void RefreshLock()
    {
        bool locked = IsLocked;
        if (lockObj) lockObj.SetActive(locked);
        if (locked && txtLockLevel) txtLockLevel.text = LocalizationManager.Localize("level", ModeUnlock.UnlockLevel(gameMode));
    }

    /// <summary>Shows the ticket price on paid maps (red when the player cannot afford it); hidden on free or level-locked maps.
    /// Uses the badge on the map picture (ticketBadge: ticket icon + "x3") when assigned, else the old text line.</summary>
    public void RefreshTicket()
    {
        int cost = ConfigManager.GetTicketCost(map);
        bool show = cost > 0 && !IsLocked;
        bool enough = !show || ConfigManager.CheckSelectMap(map);

        if (ticketBadge != null)
        {
            if (txtStickRequire) txtStickRequire.gameObject.SetActive(false);
            ticketBadge.SetActive(show);
            if (show && txtTicketCost)
            {
                if (!ticketColorCached) { ticketColor = txtTicketCost.color; ticketColorCached = true; }
                txtTicketCost.text = "x" + cost;
                txtTicketCost.color = enough ? ticketColor : notEnoughTicketColor;
            }
            return;
        }

        if (txtStickRequire == null) return;
        if (!ticketColorCached) { ticketColor = txtStickRequire.color; ticketColorCached = true; }
        txtStickRequire.gameObject.SetActive(show);
        if (!show) return;
        txtStickRequire.text = LocalizationManager.Localize(cost == 1 ? "ticket_required" : "tickets_required", cost);
        txtStickRequire.color = enough ? ticketColor : notEnoughTicketColor;
    }

    public void OnSelectMapClick()
    {
        if (IsLocked)
        {
            ModeUnlock.ShowLockedPopup(gameMode);
            return;
        }

        if (!ConfigManager.CheckSelectMap(map))
        {
            // after the ad grants the tickets, refresh the counters and pick the map straight away
            TicketAds.Offer(map, () =>
            {
                if (this == null || popupSelectMap == null) return;
                popupSelectMap.SetTicket();
                popupSelectMap.RefreshCards();
                SetSelect();
            });
            return;
        }

        if (gameMode == GameMode.MannVsMachine)
        {
            if (PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.MaxPlayers > ConfigManager.maxPlayerPerRoom / 2)
            {
                GUIManager.instan.CreatePopup<PopupNotify>(
                     string.Format(LocalizationManager.Localize("select_mvm_fail_out_of_player"), ConfigManager.maxPlayerPerRoom / 2));
                return;
            }
        }

        int cost = ConfigManager.GetTicketCost(map);
        if (cost > 0 && popupSelectMap.mapSelected != map)
        {
            // tickets are only spent when the match starts; this just tells the price
            GUIManager.instan.CreatePopup<PopupConfirm>(
                string.Format(LocalizationManager.Localize("using_ticket_confirm"), cost), (Action)SetSelect);
            return;
        }

        SetSelect();
    }

    public void SetSelect()
    {
        selectedObj.SetActive(true);
        popupSelectMap.SelectedMap(gameMode, map, this);
    }

    public void Deselected()
    {
        selectedObj.SetActive(false);
    }
}
