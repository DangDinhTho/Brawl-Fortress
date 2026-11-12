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
    private RoomInfo roomInfo;
    public Image[] gameModeIcons;

    public void SetData(RoomInfo _roomInfo)
    {
        roomInfo = _roomInfo;
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
            for (int i = 0; i < gameModeIcons.Length; i++)
            {
                gameModeIcons[i].gameObject.SetActive(i == (int)gameMode);
            }
        }
    }

    public void JoinRoomClick()
    {
        if (roomInfo == null) return;
        // Joining a room leaves the lobby automatically; calling LeaveLobby first makes JoinRoom fail
        // because the client is still in the "Leaving" state.
        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom ||
            PhotonNetwork.NetworkClientState == ClientState.Joining) return;

        if (roomInfo.MaxPlayers > 0 && roomInfo.PlayerCount >= roomInfo.MaxPlayers)
        {
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("max_player_per_team"));
            return;
        }

        PhotonNetwork.JoinRoom(roomInfo.Name);
    }
}
