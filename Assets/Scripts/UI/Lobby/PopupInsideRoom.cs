//using System.Collections;
using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Pun.Demo.Asteroids;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PopupInsideRoom : MonoBehaviour
{
    public GameObject uiPlayerItemPref;

    public Transform[] playerContainers;

    private Dictionary<int, GameObject> playerListEntries;
    public GameObject StartGameButton;
    //public GameObject readyButton;
    //public GameObject cancelButton;
    public Toggle readyToggle;

    public GameObject panel;
    public Dropdown teamDropdown;
    private bool ready;

    public GameObject uiChatItemPref;
    public Transform chatContainer;
    private List<GameObject> chatStack = new List<GameObject>();
    public int maxChatCache;
    public InputField chatInput;
    public Text txtRoomName;
    public Image classSelectedAvt;
    public Text txtClassSelectedName;
    private TF2Class tF2Class;
    [HideInInspector]
    public GameMode gameMode = GameMode.Competitive;
    [HideInInspector]
    public Map map = Map.BrawlScene;

    public Text txtGameMap;
    public Image gameModeSelected;

    // Start is called before the first frame update
    void Start()
    {
        //InitData();
        uiPlayerItemPref.SetActive(false);
        panel.SetActive(false);
        uiChatItemPref.SetActive(false);
    }

    public void Open()
    {
        //if (PhotonNetwork.IsMasterClient)
        //{
        //    Hashtable props = new Hashtable
        //    {
        //        {TF2GameProps.GameModeKey, gameMode},
        //        {TF2GameProps.Map, map}
        //    };
        //    PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        //}

        panel.SetActive(true);
        readyToggle.SetIsOnWithoutNotify(false);
        InitData();
        HomeManager.instance.OnCharavterPrefChanged += OnCharacterPrefChange;
    }

    public void Close()
    {
        panel.SetActive(false);
        HomeManager.instance.OnCharavterPrefChanged -= OnCharacterPrefChange;
    }

    public void InitData()
    {
        txtRoomName.text = PhotonNetwork.CurrentRoom.Name;
        if (playerListEntries == null)
        {
            playerListEntries = new Dictionary<int, GameObject>();
        }

        if(PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TF2GameProps.GameModeKey, out var gameModeObj))
        {
            gameMode = (GameMode)gameModeObj;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TF2GameProps.Map, out var gameMapObj))
        {
            map = (Map)gameMapObj;
        }

        OnChangeMap(gameMode, map);

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            Team team = 0;
            if (p.CustomProperties.TryGetValue(TF2GameProps.TeamKey, out var teamObj))
            {
                team = (Team)teamObj;
            }
            {
                GameObject entry = Instantiate(uiPlayerItemPref, playerContainers[(int)team]);
                entry.SetActive(true);
                UIPlayerLobbyItem item = entry.GetComponent<UIPlayerLobbyItem>();
                item.SetData(p, team);

                object isPlayerReady;
                if (p.CustomProperties.TryGetValue(TF2GameProps.PLAYER_READY, out isPlayerReady))
                {
                    item.SetPlayerReady((bool)isPlayerReady);
                }
                else
                {
                    item.SetPlayerReady(false);
                }

                playerListEntries.Add(p.ActorNumber, entry);
            }
        }

        StartGameButton.gameObject.SetActive(CheckPlayersReady());

        Team teamOwner = HomeManager.instance.team;
        if(playerContainers[(int)teamOwner].childCount >= Mathf.RoundToInt((float)PhotonNetwork.CurrentRoom.MaxPlayers / 2))
        {
            teamOwner = ConfigManager.GetEnemyTeam(teamOwner);
            HomeManager.instance.team = teamOwner;
            HomeManager.instance.OnCharavterPrefChanged?.Invoke();
        }
        tF2Class = HomeManager.instance.characterPref.tF2Class;
        Hashtable props = new Hashtable
            {
                {TF2GameProps.PLAYER_LOADED_LEVEL, false},
                {TF2GameProps.ClassKey, tF2Class},
                {TF2GameProps.TeamKey, teamOwner},

            };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdateCharacterSelectedAvt();
        //readyButton.SetActive(true);
        //cancelButton.SetActive(false);
       
        teamDropdown.SetValueWithoutNotify((int)teamOwner);

    }


    public void OnLeftRoom()
    {
        foreach (GameObject entry in playerListEntries.Values)
        {
            Destroy(entry.gameObject);
        }

        playerListEntries.Clear();
        playerListEntries = null;
        Close();
      
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        Team team = 0;
        if (newPlayer.CustomProperties.TryGetValue(TF2GameProps.TeamKey, out var teamObj))
        {
            team = (Team)teamObj;
        }
        

        GameObject entry = Instantiate(uiPlayerItemPref, playerContainers[(int)team]);
        entry.SetActive(true);
        UIPlayerLobbyItem item = entry.GetComponent<UIPlayerLobbyItem>();
        item.SetData(newPlayer, team);
        item.SetPlayerReady(false);

        playerListEntries.Add(newPlayer.ActorNumber, entry);

        StartGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Destroy(playerListEntries[otherPlayer.ActorNumber].gameObject);
        playerListEntries.Remove(otherPlayer.ActorNumber);

        StartGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
        {
            StartGameButton.gameObject.SetActive(CheckPlayersReady());
        }
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (playerListEntries == null)
        {
            playerListEntries = new Dictionary<int, GameObject>();
        }

        GameObject entry;
        if (playerListEntries.TryGetValue(targetPlayer.ActorNumber, out entry))
        {
            UIPlayerLobbyItem item = entry.GetComponent<UIPlayerLobbyItem>();

            object isPlayerReady;
            if (changedProps.TryGetValue(TF2GameProps.PLAYER_READY, out isPlayerReady))
            {
               item.SetPlayerReady((bool)isPlayerReady);
            }
            if (changedProps.TryGetValue(TF2GameProps.TeamKey, out var teamProp))
            {
                Team team = (Team)teamProp;
                if (team != item.team)
                {
                    item.transform.SetParent(playerContainers[(int)team]);
                    item.SetData(targetPlayer, team);
                }
            }
            if (changedProps.TryGetValue(TF2GameProps.ClassKey, out var classProp))
            {
                TF2Class tF2Class = (TF2Class)classProp;

                item.SetClass(tF2Class);
                
            }
        }

        StartGameButton.gameObject.SetActive(CheckPlayersReady());
    }


    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.TryGetValue(TF2GameProps.GameModeKey, out var gameModeObj))
        {
            gameMode = (GameMode)gameModeObj;
        }

        if (propertiesThatChanged.TryGetValue(TF2GameProps.Map, out var gameMapObj))
        {
            map = (Map)gameMapObj;
        }
        OnChangeMap(gameMode, map);
    }

    public void LocalPlayerPropertiesUpdated()
    {
        StartGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    private bool CheckPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return false;
        }

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isPlayerReady;
            if (p.CustomProperties.TryGetValue(TF2GameProps.PLAYER_READY, out isPlayerReady))
            {
                if (!(bool)isPlayerReady)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public void OnCharacterPrefChange()
    {
        if(tF2Class != HomeManager.instance.characterPref.tF2Class)
        {
            tF2Class = HomeManager.instance.characterPref.tF2Class;
            Hashtable props = new Hashtable
            {
                {TF2GameProps.ClassKey, tF2Class}
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        UpdateCharacterSelectedAvt();
    }

    public void UpdateCharacterSelectedAvt()
    {
        string avatarIconName = $"class_{HomeManager.instance.characterPref.tF2Class}{HomeManager.instance.team}";
        classSelectedAvt.sprite = GUIManager.instan.GetAvatarSprite(avatarIconName);
        txtClassSelectedName.text = LocalizationManager.Localize(HomeManager.instance.characterPref.tF2Class.ToString());
    }

    private int CountPlayerOfTeam(int team)
    {
        int count = 0;
        for(int i = 0; i < playerContainers[team].childCount; i++)
        {
            if (playerContainers[team].GetChild(i).gameObject.activeInHierarchy) count++;
        }

        return count;
    }

    public void OnChangeTeam(int _team)
    {
        if(CountPlayerOfTeam(_team) >= Mathf.RoundToInt((float)PhotonNetwork.CurrentRoom.MaxPlayers / 2))
        {
            teamDropdown.SetValueWithoutNotify(_team == 0 ? 1 : 0);
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("max_player_per_team"));
            return;
        }
        Team team = (Team)_team;
        HomeManager.instance.team = team;
        HomeManager.instance.OnCharavterPrefChanged?.Invoke();
        Hashtable props = new Hashtable
            {
                {TF2GameProps.TeamKey, team}
            };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void ChangeReady()
    {
        if (readyToggle.isOn && !ConfigManager.CheckSelectMap(map))
        {
            GUIManager.instan.CreatePopup<PopupConfirm>(
                string.Format(LocalizationManager.Localize("not_enough_ticket")), (Action)(() => {
                    GUIManager.instan.CreatePopup<PopupStore>();
                }));
            readyToggle.SetIsOnWithoutNotify(false);
            ready = false;
            return;
        }
        OnReadyChange(readyToggle.isOn);
    }

    public void OnReadyChange(bool _readdy)
    {
        ready = _readdy;

        //readyButton.SetActive(!ready);
        //cancelButton.SetActive(ready);
        //readyToggle.va

        Hashtable props = new Hashtable
            {
                {TF2GameProps.PLAYER_READY, ready}
            };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void LeaveRoomClick()
    {
        GUIManager.instan.CreatePopup<PopupConfirm>(LocalizationManager.Localize("leave_room_confirm"), (Action)LeaveRoom);
        
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void SendChat()
    {
        if (string.IsNullOrEmpty(chatInput.text)) return;
        LobbyManager.instance.SendChat(chatInput.text);
        chatInput.text = "";
    }

    public void OnReceiveChat(Player sender, string content)
    {
        GameObject chatItem = Instantiate(uiChatItemPref, chatContainer);
        chatItem.SetActive(true);
        chatItem.GetComponent<UIChatItem>().Init(sender, content);
        chatStack.Add(chatItem);
        if(chatStack.Count > maxChatCache)
        {
            Destroy(chatStack[0]);
            chatStack.RemoveAt(0);
        }
    }

    public void OnStartButtonClick()
    {
        CharacterSave.SaveTeam(HomeManager.instance.team);
        CharacterSave.SaveLastCharacter(HomeManager.instance.characterPref.tF2Class);
        
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        PhotonNetwork.LoadLevel("Arena");
    }

    public void ChangeGameModeClick()
    {
        if(!PhotonNetwork.IsMasterClient) return;
        GUIManager.instan.CreatePopup<PopupSelectMap>(gameMode, map);
    }

    public void OnChangeMap(GameMode gameMode, Map map)
    {
        if (!ConfigManager.CheckSelectMap(map))
        {
            readyToggle.SetIsOnWithoutNotify(false);
            ready = false;
            OnReadyChange(false);
        }
        
        this.gameMode = gameMode;
        this.map = map;
        txtGameMap.text = $"{LocalizationManager.Localize($"short_{gameMode}")} - {LocalizationManager.Localize(map.ToString())}";
        gameModeSelected.sprite = GUIManager.instan.iconAtlas.GetSprite($"gametype_{gameMode.ToString().ToLower()}");
    }
}
