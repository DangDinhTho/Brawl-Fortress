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

    public void Awake()
    {
        instance = this;
        PhotonNetwork.AutomaticallySyncScene = true;
 
        //PlayerNameInput.text = "Player " + Random.Range(1000, 10000);
    }

    private void Start()
    {
        roomListBtn.SetActive(false);
        PhotonNetwork.EnableCloseConnection = true;
        PhotonNetwork.LocalPlayer.NickName = GameManager.instan.userData.userName;
        //if (ConfigManager.multiplay)
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        //if (!ConfigManager.multiplay) return;
        roomListBtn.SetActive(ConfigManager.multiplay);
        LobbyPanel.OnConnectedToMaster();
        if (LobbyPanel.isOpen & !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
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

    public override void OnJoinedRoom()
    {
        if (singlePlayer)
        {
            //Hashtable props = new Hashtable
            //{
            //    {TF2GameProps.GameModeKey, sigleGameMode},
            //    {TF2GameProps.Map, sigleMap}

            //};
            //PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            Team team = (Team)Random.Range(0, 2);

            Hashtable playerProps = new Hashtable
            {
                {TF2GameProps.ClassKey, HomeManager.instance.characterPref.tF2Class},
                {TF2GameProps.TeamKey, team}

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
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        //InsideRoomPanel.Close();
        LobbyPanel.Open();
    }

    public override void OnLeftRoom()
    {
        InsideRoomPanel.OnLeftRoom();

        OnRoomListButtonClicked();
        
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

    public void SendChat(string content)
    {
        photonView.RPC(nameof(RPC_Chat), RpcTarget.All, content);
    }

    [PunRPC]
    void RPC_Chat(string content, PhotonMessageInfo info)
    {
        InsideRoomPanel.OnReceiveChat(info.Sender, content);
    }

}
