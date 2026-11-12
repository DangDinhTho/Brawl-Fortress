using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class UIRoomItem : MonoBehaviour
{
    public Text txtRoomName;
    public Text txtPlayerCount;
    public Text txtGameMode;
    public Text txtMap;
    //public Image imgMap;
    //public Image imgGameMode;
    private RoomInfo roomInfo;
    public Image[] gameModeIcons;
    
    public void SetData(RoomInfo _roomInfo)
    {
        this.roomInfo = _roomInfo;
        txtRoomName.text = _roomInfo.Name;
        txtPlayerCount.text = $"{_roomInfo.PlayerCount}/{_roomInfo.MaxPlayers}";

        if (_roomInfo.CustomProperties.TryGetValue(TF2GameProps.Map, out var mapObj))
        {
            txtMap.text = LocalizationManager.Localize(((Map)mapObj).ToString());
        }
        
        if (_roomInfo.CustomProperties.TryGetValue(TF2GameProps.GameModeKey, out var gameModeObj))
        {
            GameMode gameMode = (GameMode)gameModeObj;
            txtGameMode.text = LocalizationManager.Localize(gameMode.ToString());
            //imgGameMode.sprite = GUIManager.instan.iconAtlas.GetSprite(((GameMode)gameModeObj).ToString());
            for(int i = 0; i < gameModeIcons.Length; i++)
            {
                gameModeIcons[i].gameObject.SetActive(i == (int)gameMode);
            }
        }

        
    }

    public void JoinRoomClick()
    {
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        PhotonNetwork.JoinRoom(roomInfo.Name);
    }
}
