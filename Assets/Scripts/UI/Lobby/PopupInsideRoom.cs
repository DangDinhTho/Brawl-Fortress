using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PopupInsideRoom : MonoBehaviour
{
    public GameObject uiPlayerItemPref;

    public Transform[] playerContainers;

    private readonly Dictionary<int, UIPlayerLobbyItem> playerListEntries = new Dictionary<int, UIPlayerLobbyItem>();
    public GameObject StartGameButton;
    public Toggle readyToggle;

    public GameObject panel;
    public Dropdown teamDropdown;
    private bool ready;
    private bool starting;
    private bool subscribed;

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

    void Start()
    {
        uiPlayerItemPref.SetActive(false);
        panel.SetActive(false);
        uiChatItemPref.SetActive(false);
        if (chatInput != null) chatInput.characterLimit = LobbyManager.MaxChatLength;
    }

    public void Open()
    {
        panel.SetActive(true);
        starting = false;
        ready = false;
        readyToggle.SetIsOnWithoutNotify(false);
        InitData();
        if (!subscribed)
        {
            HomeManager.instance.OnCharavterPrefChanged += OnCharacterPrefChange;
            subscribed = true;
        }
    }

    public void Close()
    {
        panel.SetActive(false);
        if (subscribed)
        {
            HomeManager.instance.OnCharavterPrefChanged -= OnCharacterPrefChange;
            subscribed = false;
        }
    }

    private void OnDestroy()
    {
        if (subscribed && HomeManager.instance != null)
            HomeManager.instance.OnCharavterPrefChanged -= OnCharacterPrefChange;
    }

    private static Team GetTeam(Player p)
    {
        return p.CustomProperties.TryGetValue(TF2GameProps.TeamKey, out var teamObj) ? (Team)teamObj : Team.red;
    }

    /// <summary>Counts players of a team from the room state (not from the UI hierarchy).</summary>
    private static int CountPlayersInTeam(Team team, bool excludeLocal)
    {
        int count = 0;
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (excludeLocal && p.IsLocal) continue;
            if (GetTeam(p) == team) count++;
        }
        return count;
    }

    private static int MaxPerTeam => Mathf.Max(1, Mathf.RoundToInt(PhotonNetwork.CurrentRoom.MaxPlayers / 2f));

    private UIPlayerLobbyItem AddOrUpdateEntry(Player p)
    {
        Team team = GetTeam(p);
        if (!playerListEntries.TryGetValue(p.ActorNumber, out var item))
        {
            GameObject entry = Instantiate(uiPlayerItemPref, playerContainers[(int)team]);
            entry.SetActive(true);
            item = entry.GetComponent<UIPlayerLobbyItem>();
            playerListEntries.Add(p.ActorNumber, item);
        }
        else if (item.team != team)
        {
            item.transform.SetParent(playerContainers[(int)team], false);
        }
        item.SetData(p, team);
        item.SetPlayerReady(p.CustomProperties.TryGetValue(TF2GameProps.PLAYER_READY, out var r) && (bool)r);
        return item;
    }

    private void ClearEntries()
    {
        foreach (var entry in playerListEntries.Values)
        {
            if (entry != null) Destroy(entry.gameObject);
        }
        playerListEntries.Clear();
    }

    public void InitData()
    {
        txtRoomName.text = PhotonNetwork.CurrentRoom.Name;
        ClearEntries();

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TF2GameProps.GameModeKey, out var gameModeObj))
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
            AddOrUpdateEntry(p);
        }

        // Pick a team that still has room (other players only: our own props are not set yet).
        Team teamOwner = HomeManager.instance.team;
        if (CountPlayersInTeam(teamOwner, true) >= MaxPerTeam)
        {
            teamOwner = ConfigManager.GetEnemyTeam(teamOwner);
            HomeManager.instance.team = teamOwner;
            HomeManager.instance.OnCharavterPrefChanged?.Invoke();
        }
        tF2Class = HomeManager.instance.characterPref.tF2Class;
        Hashtable props = new Hashtable
        {
            {TF2GameProps.PLAYER_LOADED_LEVEL, false},
            {TF2GameProps.PLAYER_READY, false},
            {TF2GameProps.ClassKey, tF2Class},
            {TF2GameProps.TeamKey, teamOwner},
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdateCharacterSelectedAvt();
        teamDropdown.SetValueWithoutNotify((int)teamOwner);
        StartGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public void OnLeftRoom()
    {
        ClearEntries();
        foreach (var c in chatStack) if (c != null) Destroy(c);
        chatStack.Clear();
        Close();
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!panel.activeSelf) return;
        AddOrUpdateEntry(newPlayer);
        StartGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (playerListEntries.TryGetValue(otherPlayer.ActorNumber, out var item))
        {
            if (item != null) Destroy(item.gameObject);
            playerListEntries.Remove(otherPlayer.ActorNumber);
        }
        StartGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        // Kick buttons and the master icon depend on who the master is.
        foreach (var item in playerListEntries.Values)
        {
            if (item != null) item.RefreshMasterState();
        }
        StartGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!panel.activeSelf) return;

        if (playerListEntries.TryGetValue(targetPlayer.ActorNumber, out var item))
        {
            if (changedProps.TryGetValue(TF2GameProps.PLAYER_READY, out var isPlayerReady))
            {
                item.SetPlayerReady((bool)isPlayerReady);
            }
            if (changedProps.TryGetValue(TF2GameProps.TeamKey, out var teamProp))
            {
                Team team = (Team)teamProp;
                if (team != item.team)
                {
                    item.transform.SetParent(playerContainers[(int)team], false);
                    item.SetData(targetPlayer, team);
                }
            }
            if (changedProps.TryGetValue(TF2GameProps.ClassKey, out var classProp))
            {
                item.SetClass((TF2Class)classProp);
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
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null || starting)
        {
            return false;
        }

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!(p.CustomProperties.TryGetValue(TF2GameProps.PLAYER_READY, out var isPlayerReady) && (bool)isPlayerReady))
            {
                return false;
            }
        }
        return true;
    }

    public void OnCharacterPrefChange()
    {
        if (!PhotonNetwork.InRoom) return;
        if (tF2Class != HomeManager.instance.characterPref.tF2Class)
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

    public void OnChangeTeam(int _team)
    {
        Team team = (Team)_team;
        if (team == HomeManager.instance.team) return;
        if (CountPlayersInTeam(team, true) >= MaxPerTeam)
        {
            teamDropdown.SetValueWithoutNotify((int)HomeManager.instance.team);
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("max_player_per_team"));
            return;
        }
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
            // after the ad grants the tickets, mark the player ready (what they were trying to do)
            TicketAds.Offer(map, () =>
            {
                if (this == null || !PhotonNetwork.InRoom || !ConfigManager.CheckSelectMap(map)) return;
                readyToggle.SetIsOnWithoutNotify(true);
                OnReadyChange(true);
            });
            readyToggle.SetIsOnWithoutNotify(false);
            ready = false;
            return;
        }
        OnReadyChange(readyToggle.isOn);
    }

    public void OnReadyChange(bool _readdy)
    {
        ready = _readdy;
        if (!PhotonNetwork.InRoom) return;
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
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
    }

    public void SendChat()
    {
        if (string.IsNullOrWhiteSpace(chatInput.text)) return;
        LobbyManager.instance.SendChat(chatInput.text);
        chatInput.text = "";
    }

    public void OnReceiveChat(Player sender, string content)
    {
        GameObject chatItem = Instantiate(uiChatItemPref, chatContainer);
        chatItem.SetActive(true);
        chatItem.GetComponent<UIChatItem>().Init(sender, content);
        chatStack.Add(chatItem);
        if (chatStack.Count > maxChatCache)
        {
            Destroy(chatStack[0]);
            chatStack.RemoveAt(0);
        }
    }

    public void OnStartButtonClick()
    {
        // Re-check: a player may have un-readied or left since the button was shown.
        if (starting || !CheckPlayersReady()) return;
        starting = true;
        StartGameButton.gameObject.SetActive(false);

        CharacterSave.SaveTeam(HomeManager.instance.team);
        CharacterSave.SaveLastCharacter(HomeManager.instance.characterPref.tF2Class);

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        PhotonNetwork.LoadLevel("Arena");
    }

    public void ChangeGameModeClick()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        GUIManager.instan.CreatePopup<PopupSelectMap>(gameMode, map);
    }

    public void OnChangeMap(GameMode gameMode, Map map)
    {
        if (!ConfigManager.CheckSelectMap(map) && ready)
        {
            readyToggle.SetIsOnWithoutNotify(false);
            OnReadyChange(false);
        }

        this.gameMode = gameMode;
        this.map = map;
        txtGameMap.text = $"{LocalizationManager.Localize($"short_{gameMode}")} - {LocalizationManager.Localize(map.ToString())}";
        gameModeSelected.sprite = GUIManager.instan.iconAtlas.GetSprite($"gametype_{gameMode.ToString().ToLower()}");
    }
}
