using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PopupLobbyMain : MonoBehaviour
{
    private readonly Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private readonly Dictionary<string, UIRoomItem> roomListEntries = new Dictionary<string, UIRoomItem>();
    private readonly Stack<UIRoomItem> pooledEntries = new Stack<UIRoomItem>();

    public GameObject uiRoomItemPref;
    public Transform roomListContainer;

    public GameObject panel;
    public GameObject createRoomPanel;
    public InputField roomNameInput;
    public InputField maxPlayerInput;
    [HideInInspector]
    public bool isOpen;
    [HideInInspector]
    public Dictionary<string, string> regions = new Dictionary<string, string>();

    public Dropdown dropDownRegions;

    private string pendingRegion;
    private bool creatingRoom;

    private void Awake()
    {
        uiRoomItemPref.SetActive(false);
    }

    private void Start()
    {
        panel.SetActive(false);
        regions.Clear();
        regions.Add("asia", "Asia");
        regions.Add("au", "Australia");
        regions.Add("cae", "Canada, East");
        regions.Add("eu", "Europe");
        regions.Add("in", "India");
        regions.Add("jp", "Japan");
        regions.Add("ru", "Russia");
        regions.Add("rue", "Russia, East");
        regions.Add("za", "South Africa");
        regions.Add("sa", "South America");
        regions.Add("kr", "South Korea");
        regions.Add("tr", "Turkey");
        regions.Add("us", "USA, East");
        regions.Add("usw", "USA, West");

        dropDownRegions.ClearOptions();
        dropDownRegions.AddOptions(regions.Values.ToList());
        dropDownRegions.onValueChanged.AddListener(OnRegionSelected);
    }

    private void OnRegionSelected(int index)
    {
        var region = regions.ElementAt(index).Key;
        if (CurrentRegionCode() == region) return;

        PlayerPrefs.SetString(LobbyManager.RegionPrefKey, region);
        cachedRoomList.Clear();
        ClearRoomListView();

        if (PhotonNetwork.IsConnected)
        {
            // Connect to the new region in LobbyManager.OnDisconnected, once this connection is closed.
            pendingRegion = region;
            PhotonNetwork.Disconnect();
        }
        else
        {
            PhotonNetwork.ConnectToRegion(region);
        }
    }

    public bool TryConsumePendingRegion(out string region)
    {
        region = pendingRegion;
        pendingRegion = null;
        return !string.IsNullOrEmpty(region);
    }

    private static string CurrentRegionCode()
    {
        string re = PhotonNetwork.CloudRegion;
        if (string.IsNullOrEmpty(re)) return null;
        return re.Contains("/") ? re.Split('/')[0] : re;
    }

    public void OnConnectedToMaster()
    {
        creatingRoom = false;
        string re = CurrentRegionCode();
        if (re != null && regions.ContainsKey(re))
        {
            int index = regions.Keys.ToList().IndexOf(re);
            // Without notify: changing the value must not trigger a disconnect/reconnect.
            dropDownRegions.SetValueWithoutNotify(index);
        }
    }

    public void Open()
    {
        panel.SetActive(true);
        isOpen = true;
        if (!PhotonNetwork.IsConnected)
        {
            LobbyManager.Connect();
        }
    }

    public void Close()
    {
        panel.SetActive(false);
        isOpen = false;
    }

    public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
        UpdateRoomListView();
    }

    public void OnJoinedLobby()
    {
        cachedRoomList.Clear();
        ClearRoomListView();
    }

    // note: when a client joins / creates a room, OnLeftLobby does not get called, even if the client was in a lobby before
    public void OnLeftLobby()
    {
        cachedRoomList.Clear();
        ClearRoomListView();
    }

    public void OnJoinedRoom()
    {
        creatingRoom = false;
        cachedRoomList.Clear();
        ClearRoomListView();
        Close();
    }

    private void ClearRoomListView()
    {
        foreach (var entry in roomListEntries.Values)
        {
            entry.gameObject.SetActive(false);
            pooledEntries.Push(entry);
        }
        roomListEntries.Clear();
    }

    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
            {
                cachedRoomList.Remove(info.Name);
                continue;
            }
            cachedRoomList[info.Name] = info;
        }
    }

    /// <summary>Only creates/removes the entries that changed (no Destroy/Instantiate of the whole list).</summary>
    private void UpdateRoomListView()
    {
        foreach (var name in roomListEntries.Keys.Where(n => !cachedRoomList.ContainsKey(n)).ToList())
        {
            var entry = roomListEntries[name];
            entry.gameObject.SetActive(false);
            pooledEntries.Push(entry);
            roomListEntries.Remove(name);
        }

        foreach (RoomInfo info in cachedRoomList.Values)
        {
            if (!roomListEntries.TryGetValue(info.Name, out var entry))
            {
                entry = pooledEntries.Count > 0
                    ? pooledEntries.Pop()
                    : Instantiate(uiRoomItemPref, roomListContainer).GetComponent<UIRoomItem>();
                entry.gameObject.SetActive(true);
                roomListEntries.Add(info.Name, entry);
            }
            entry.SetData(info);
        }
    }

    public void OpenCreateRoomPanel()
    {
        roomNameInput.text = PhotonNetwork.LocalPlayer.NickName + "'s room";
        maxPlayerInput.text = ConfigManager.maxPlayerPerRoom.ToString();
        createRoomPanel.SetActive(true);
    }

    public void CreateRoomClick()
    {
        if (creatingRoom) return;
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("disconected"));
            return;
        }

        string roomName = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(roomName)) roomName = "Room " + Random.Range(1000, 10000);

        byte.TryParse(maxPlayerInput.text, out byte maxPlayers);
        maxPlayers = (byte)Mathf.Clamp(maxPlayers, 2, 12);
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            {TF2GameProps.GameModeKey, GameMode.Competitive},
            {TF2GameProps.Map, Map.Map_ARN_Tumble}
        };

        string[] customPropertiesForLobby = new string[] { TF2GameProps.GameModeKey, TF2GameProps.Map };
        // No rejoin support: a leaving player is removed at once (PlayerTtl 0) and their character/buildables are
        // cleaned up (CleanupCacheOnLeave). Bots are room objects, so they are not affected.
        RoomOptions options = new RoomOptions { MaxPlayers = maxPlayers, PlayerTtl = 0, CleanupCacheOnLeave = true, CustomRoomProperties = props, CustomRoomPropertiesForLobby = customPropertiesForLobby };

        creatingRoom = PhotonNetwork.CreateRoom(roomName, options, null);
        createRoomPanel.SetActive(false);
    }

    public void OnCreateRoomFailed()
    {
        creatingRoom = false;
    }
}
