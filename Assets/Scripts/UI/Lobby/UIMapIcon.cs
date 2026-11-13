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
    public Text txtStickRequire;

    [Header("Mode lock (ModeUnlock)")]
    [Tooltip("Overlay shown while this card's game mode is locked by level")]
    public GameObject lockObj;
    [Tooltip("'Level N' on the lock overlay")]
    public TMP_Text txtLockLevel;

    public bool IsLocked => !ModeUnlock.IsUnlocked(gameMode);

    //public void Init(Map map, GameMode gameMode, PopupSelectMap popup, bool selected)
    //{
    //    this.gameMode = gameMode;
    //    popupSelectMap = popup;
    //    this.map = map;
    //    mapName.text = LocalizationManager.Localize(map.ToString());
    //    mapIcon.sprite = GUIManager.instan.GetMapIcon(map);
    //    selectedObj.SetActive(selected);
    //    if (ConfigManager.mapConfigs.TryGetValue(map, out var mapConfig))
    //    {
    //        if (mapConfig.requireStick)
    //        {
    //            txtStickRequire.gameObject.SetActive(true);
    //            if (mapConfig.stickPerPlay == 1)
    //                txtStickRequire.text =
    //                    string.Format(LocalizationManager.Localize("ticket_required"), mapConfig.stickPerPlay);
    //            else if(mapConfig.stickPerPlay > 1)
    //                txtStickRequire.text =
    //                    string.Format(LocalizationManager.Localize("tickets_required"), mapConfig.stickPerPlay);
    //        }
    //        else
    //        {
    //            txtStickRequire.gameObject.SetActive(false);
    //        }
    //    }
    //    else
    //    {
    //        txtStickRequire.gameObject.SetActive(false);
    //    }
    //}

    private void Start()
    {
        selectedObj.SetActive(map == popupSelectMap.mapSelected);
        if (map == popupSelectMap.mapSelected) popupSelectMap.mapSelectedItem = this;
        RefreshLock();
    }

    private void OnEnable()
    {
        RefreshLock();
    }

    public void RefreshLock()
    {
        bool locked = IsLocked;
        if (lockObj) lockObj.SetActive(locked);
        if (locked && txtLockLevel) txtLockLevel.text = LocalizationManager.Localize("level", ModeUnlock.UnlockLevel(gameMode));
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
            TicketAds.Offer(map);
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

        if (ConfigManager.mapConfigs.TryGetValue(map, out var mapConfig) && mapConfig.requireStick)
        {
            GUIManager.instan.CreatePopup<PopupConfirm>(
                string.Format(string.Format(LocalizationManager.Localize("using_ticket_confirm"), mapConfig.stickPerPlay)), (Action)SetSelect);
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
