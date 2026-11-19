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
        RefreshRoomListButton();
        // back from a solo match (offline mode): go online again for multiplayer rooms
        if (PhotonNetwork.OfflineMode && !PhotonNetwork.InRoom) PhotonNetwork.OfflineMode = false;
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

    /// <summary>"Find a game" shows when multiplayer is on (Title Data "multiplay") and Photon is connected (not offline).
    /// Called when Photon connects / disconnects and when Title Data arrives, whichever comes last.</summary>
    public static void RefreshRoomListButton()
    {
        var lm = instance;
        if (lm == null || lm.roomListBtn == null) return;
        lm.roomListBtn.SetActive(true);   // always shown: when not connected, a click offers to reconnect
    }

    // ---- reconnect: on a phone the connection drops while an ad is shown / the app is in the background or the network
    // blinks; nothing connected again, so "Find a game" stayed hidden until the game was restarted
    int reconnectAttempt;
    Coroutine reconnecting;

    void ScheduleReconnect()
    {
        if (reconnecting != null) return;
        float delay = Mathf.Min(30f, 2f * Mathf.Pow(2f, reconnectAttempt));
        reconnectAttempt++;
        reconnecting = StartCoroutine(ReconnectAfter(delay));
    }

    System.Collections.IEnumerator ReconnectAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        reconnecting = null;
        if (!PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode) Connect();
    }

    void OnApplicationPause(bool paused)
    {
        // back from an ad / the home screen: reconnect at once if the connection was lost meanwhile
        if (paused || PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode) return;
        reconnectAttempt = 0;
        if (reconnecting != null) { StopCoroutine(reconnecting); reconnecting = null; }
        Connect();
    }

    public override void OnConnectedToMaster()
    {
        reconnectAttempt = 0;
        RefreshRoomListButton();
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
        Debug.LogWarning("Photon disconnected: " + cause);
        // lost without asking for it (not: going offline for a solo match, region switch): try again
        if (cause != DisconnectCause.DisconnectByClientLogic && !PhotonNetwork.OfflineMode) ScheduleReconnect();
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

    static bool IsOnline => PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.OfflineMode;

    /// <summary>Not connected: a popup with Retry (waiting screen, up to RetryTimeout s to connect, then the room list or this
    /// popup again) and Close.</summary>
    void ShowNotConnected()
    {
        GUIManager.instan.CreatePopup<PopupConfirm>(LocalizationManager.Localize("online_not_connected"), (System.Action)RetryConnect, null, null,
            LocalizationManager.Localize("retry_connect_btn"), LocalizationManager.Localize("close_btn"));
    }

    public const float RetryTimeout = 5f;
    Coroutine retryRoutine;

    void RetryConnect()
    {
        if (retryRoutine != null) return;
        retryRoutine = StartCoroutine(RetryConnectRoutine());
    }

    System.Collections.IEnumerator RetryConnectRoutine()
    {
        var loading = GUIManager.instan.CreatePopup<PopupLoading>(LocalizationManager.Localize("connecting"));
        float t = 0f;
        if (PhotonNetwork.OfflineMode && !PhotonNetwork.InRoom) PhotonNetwork.OfflineMode = false;
        reconnectAttempt = 0;
        if (reconnecting != null) { StopCoroutine(reconnecting); reconnecting = null; }
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsConnectedAndReady)
        {
            // stuck half way from an earlier attempt: start a clean one
            PhotonNetwork.Disconnect();
            while (PhotonNetwork.IsConnected && t < 1.5f) { t += Time.unscaledDeltaTime; yield return null; }
            if (reconnecting != null) { StopCoroutine(reconnecting); reconnecting = null; }
        }
        if (!PhotonNetwork.IsConnected) Connect();
        while (t < RetryTimeout && !IsOnline) { t += Time.unscaledDeltaTime; yield return null; }
        if (loading != null) loading.Dismiss();
        retryRoutine = null;
        if (IsOnline) OnRoomListButtonClicked();
        else ShowNotConnected();
    }

    public void OnRoomListButtonClicked()
    {
        if (!IsOnline) { ShowNotConnected(); return; }
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
