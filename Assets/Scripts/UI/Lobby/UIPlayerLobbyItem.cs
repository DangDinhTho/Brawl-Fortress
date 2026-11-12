using System;
using Photon.Pun;
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

    [HideInInspector]
    public Team team;
    private Player player;
    public GameObject masterClientObj;

    public Player Player => player;

    public void SetData(Player player, Team team)
    {
        this.player = player;
        this.team = team;
        txtPlayerName.text = player.NickName;

        if (player.CustomProperties.TryGetValue(TF2GameProps.ClassKey, out var classObj))
        {
            SetClass((TF2Class)classObj);
        }

        RefreshMasterState();
    }

    public void SetClass(TF2Class tF2Class)
    {
        imgCharacter.sprite = GUIManager.instan.GetAvatar(tF2Class, team);
    }

    /// <summary>
    /// Background, kick button and master icon. Must be refreshed when the master client changes.
    /// (Does not touch the player's READY property anymore: changing team used to reset it to false.)
    /// </summary>
    public void RefreshMasterState()
    {
        if (player == null) return;
        bool isMine = player.IsLocal;
        bgr.sprite = isMine ? mineBgr : otherBgr;
        kickBtn.gameObject.SetActive(!isMine && PhotonNetwork.IsMasterClient);
        masterClientObj.SetActive(player.IsMasterClient);
    }

    public void SetPlayerReady(bool playerReady)
    {
        imgReady.gameObject.SetActive(playerReady);
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
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null || player == null) return;
        var target = PhotonNetwork.CurrentRoom.GetPlayer(player.ActorNumber);
        if (target != null) PhotonNetwork.CloseConnection(target);
    }
}
