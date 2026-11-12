using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager instance;

    public PopupLobbyMain LobbyPanel;
    public PopupInsideRoom InsideRoomPanel;

    public GameObject roomListBtn;
    public PhotonView photonView;

    [HideInInspector]
    public bool singlePlayer;
    [HideInInspector]
    public GameMode sigleGameMode;
    [HideInInspector]
    public Map sigleMap;

    public const string RegionPrefKey = "PHOTON_REGION";

    public void Awake()
    {
        instance = this;
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        roomListBtn.SetActive(false);
        PhotonNetwork.EnableCloseConnection = true;
        PhotonNetwork.LocalPlayer.NickName = GameManager.instan.userData.userName;
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
        {
            // Still connected after a match: OnConnectedToMaster will not fire again, so run it now.
            OnConnectedToMaster();
        }
        else
        {
            Connect(); // no-op if a (re)connection is already in progress
        }
    }

    /// <summary>Connects to the region chosen by the player last time, or to the best region.</summary>
    public static void Connect()
    {
        if (PhotonNetwork.IsConnected) return;
        string region = PlayerPrefs.GetString(RegionPrefKey, "");
        if (!string.IsNullOrEmpty(region)) PhotonNetwork.ConnectToRegion(region);
        else PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        roomListBtn.SetActive(ConfigManager.multiplay);
        LobbyPanel.OnConnectedToMaster();
        if (LobbyPanel.isOpen && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // Region switch: connect to the new region once the old connection is fully closed.
        if (LobbyPanel.TryConsumePendingRegion(out var region))
        {
            PhotonNetwork.ConnectToRegion(region);
            return;
        }
        roomListBtn.SetActive(false);
        Debug.LogWarning("Photon disconnected: " + cause);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        LobbyPanel.OnRoomListUpdate(roomList);
    }

    public override void OnJoinedLobby()
    {
        LobbyPanel.OnJoinedLobby();
    }

    public override void OnLeftLobby()
    {
        LobbyPanel.OnLeftLobby();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        GUIManager.instan.CreatePopup<PopupNotify>(message);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        singlePlayer = false;
        LobbyPanel.OnCreateRoomFailed();
        GUIManager.instan.CreatePopup<PopupNotify>(message);
    }

    public override void OnJoinedRoom()
    {
        if (singlePlayer)
        {
            Team team = (Team)Random.Range(0, 2);

            // LocalPlayer properties survive between rooms: reset the per-match ones.
            Hashtable playerProps = new Hashtable
            {
                {TF2GameProps.ClassKey, HomeManager.instance.characterPref.tF2Class},
                {TF2GameProps.TeamKey, team},
                {TF2GameProps.PLAYER_LOADED_LEVEL, false},
                {TF2GameProps.PLAYER_READY, false}
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }
        else
        {
            LobbyPanel.OnJoinedRoom();
            InsideRoomPanel.Open();
        }
    }

    public void OnRoomListButtonClicked()
    {
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby && !PhotonNetwork.InRoom)
        {
            PhotonNetwork.JoinLobby();
        }
        LobbyPanel.Open();
    }

    public override void OnLeftRoom()
    {
        bool wasSingle = singlePlayer;
        singlePlayer = false;
        InsideRoomPanel.OnLeftRoom();
        if (!wasSingle) OnRoomListButtonClicked();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        InsideRoomPanel.OnPlayerEnteredRoom(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        InsideRoomPanel.OnPlayerLeftRoom(otherPlayer);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        InsideRoomPanel.OnMasterClientSwitched(newMasterClient);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        InsideRoomPanel.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        InsideRoomPanel.OnRoomPropertiesUpdate(propertiesThatChanged);
    }

    public const int MaxChatLength = 120;

    public void SendChat(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || !PhotonNetwork.InRoom) return;
        content = content.Trim();
        if (content.Length > MaxChatLength) content = content.Substring(0, MaxChatLength);
        photonView.RPC(nameof(RPC_Chat), RpcTarget.All, content);
    }

    [PunRPC]
    void RPC_Chat(string content, PhotonMessageInfo info)
    {
        if (content != null && content.Length > MaxChatLength) content = content.Substring(0, MaxChatLength);
        InsideRoomPanel.OnReceiveChat(info.Sender, content);
    }
}
