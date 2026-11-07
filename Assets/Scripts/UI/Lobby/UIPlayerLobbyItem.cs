using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerLobbyItem : MonoBehaviour
{
    public Text txtPlayerName;
    public Image imgCharacter;
    public Image bgr;
    public Sprite mineBgr;
    public Sprite otherBgr;

    public Button kickBtn;
    public Image imgReady;

    private bool isPlayerReady;
    [HideInInspector]
    public Team team;
    private Player player;
    public GameObject masterClientObj;

    public void OnEnable()
    {
        PlayerNumbering.OnPlayerNumberingChanged += OnPlayerNumberingChanged;
    }

    public void SetData(Player player, Team team)
    {
        this.player = player;
        this.team = team;
        txtPlayerName.text = player.NickName;

        if (player.CustomProperties.TryGetValue(TF2GameProps.ClassKey, out var classObj))
        {
            TF2Class tF2Class = (TF2Class)classObj;
            SetClass(tF2Class);
        }
        
        SetPlayerData();
    }

    public void SetClass(TF2Class tF2Class)
    {
        imgCharacter.sprite = GUIManager.instan.GetAvatar(tF2Class, team);
    }
    
    public void SetPlayerData()
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber != player.ActorNumber)
        {
            bgr.sprite = otherBgr;
            if (PhotonNetwork.IsMasterClient)
            {
                kickBtn.gameObject.SetActive(true);
            }
        }
        else
        {
            kickBtn.gameObject.SetActive(false);
            bgr.sprite = mineBgr;
            Hashtable initialProps = new Hashtable()
            {
                { TF2GameProps.PLAYER_READY, isPlayerReady },
                //{ TF2GameProps.PLAYER_LIVES, AsteroidsGame.PLAYER_MAX_LIVES }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
            
        }
        
        masterClientObj.SetActive(player.IsMasterClient);

    }

    public void OnDisable()
    {
        PlayerNumbering.OnPlayerNumberingChanged -= OnPlayerNumberingChanged;
    }


    private void OnPlayerNumberingChanged()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == player.ActorNumber)
            {
                //PlayerColorImage.color = AsteroidsGame.GetColor(p.GetPlayerNumber());
            }
        }
    }

    public void SetPlayerReady(bool playerReady)
    {
        imgReady.gameObject.SetActive(playerReady);
        //readyButton.GetComponentInChildren<Text>().text =
        //    playerReady ? LocalizationManager.Localize("ready") : LocalizationManager.Localize("ready?");
        //imgReady.enabled = playerReady;
    }

    public void OnKickClick()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GUIManager.instan.CreatePopup<PopupConfirm>(LocalizationManager.Localize("confirm_kick_player"), (Action)KickPlayer);
        }
    }

    private void KickPlayer()
    {
        PhotonNetwork.CloseConnection(PhotonNetwork.CurrentRoom.GetPlayer(player.ActorNumber));
    }
}
