using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PopupLobbyMain : MonoBehaviour
{
    private Dictionary<string, RoomInfo> cachedRoomList;
    private Dictionary<string, GameObject> roomListEntries;

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

    private void Awake()
    {
        cachedRoomList = new Dictionary<string, RoomInfo>();
        roomListEntries = new Dictionary<string, GameObject>();
        uiRoomItemPref.SetActive(false);
    }

    private void Start()
    {
        panel.SetActive(false);
        regions.Add("asia", "Asia");
        regions.Add("au", "Australia");
        regions.Add("cae", "Canada, East");
        //regions.Add("cn", "Chinese Mainland");
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

        dropDownRegions.options.Clear();
        foreach (var re in regions)
        {
            Dropdown.OptionData o = new Dropdown.OptionData(re.Value);
            dropDownRegions.options.Add(o);
        }
       

        dropDownRegions.onValueChanged.AddListener((i) =>
        {
            PhotonNetwork.Disconnect();
            cachedRoomList.Clear();
            ClearRoomListView();
            UpdateRoomListView();
            if (PhotonNetwork.InLobby)
            {
                PhotonNetwork.LeaveLobby();
            }
            var region = regions.Where(e => e.Value == dropDownRegions.options[i].text).First();
            PhotonNetwork.ConnectToRegion(region.Key);
            //PhotonNetwork.BestRegionSummaryInPreferences = region.Key;

        });
    }

    public void OnConnectedToMaster()
    {
        string re = PhotonNetwork.CloudRegion;
        if (re == null) return;
        if (re.Contains("/"))
        {
            re = re.Split('/')[0];
        }
        if (!string.IsNullOrEmpty(PhotonNetwork.CloudRegion) && (regions.ContainsKey(re)))
        {
            dropDownRegions.value = dropDownRegions.options.FindIndex(e => e.text == regions[re]);
        }
    }


    public void Open()
    {
        panel.SetActive(true);
        isOpen = true;
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void Close()
    {
        panel.SetActive(false);
        isOpen = false;
    }


    public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        ClearRoomListView();

        UpdateCachedRoomList(roomList);
        UpdateRoomListView();
    }

    public void OnJoinedLobby()
    {
        // whenever this joins a new lobby, clear any previous room lists
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
        // joining (or entering) a room invalidates any cached lobby room list (even if LeaveLobby was not called due to just joining a room)
        cachedRoomList.Clear();
        Close();


    }



    private void ClearRoomListView()
    {
        foreach (GameObject entry in roomListEntries.Values)
        {
            Destroy(entry.gameObject);
        }

        roomListEntries.Clear();
    }

    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // Remove room from cached room list if it got closed, became invisible or was marked as removed
            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList.Remove(info.Name);
                }

                continue;
            }

            // Update cached room info
            if (cachedRoomList.ContainsKey(info.Name))
            {
                cachedRoomList[info.Name] = info;
            }
            // Add new room info to cache
            else
            {
                cachedRoomList.Add(info.Name, info);
            }
        }
    }

    private void UpdateRoomListView()
    {
        foreach (RoomInfo info in cachedRoomList.Values)
        {
            GameObject entry = Instantiate(uiRoomItemPref, roomListContainer);
            entry.SetActive(true);
            entry.GetComponent<UIRoomItem>().SetData(info);
            roomListEntries.Add(info.Name, entry);
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
        print(PhotonNetwork.NetworkClientState);
        string roomName = roomNameInput.text;
        roomName = (roomName.Equals(string.Empty)) ? "Room " + Random.Range(1000, 10000) : roomName;

        byte maxPlayers;
        byte.TryParse(maxPlayerInput.text, out maxPlayers);
        maxPlayers = (byte)Mathf.Clamp(maxPlayers, 1, 12);
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                {TF2GameProps.GameModeKey, GameMode.Competitive},
                {TF2GameProps.Map, Map.Map_ARN_Tumble}
            };

        string[] customPropertiesForLobby = new string[] { TF2GameProps.GameModeKey, TF2GameProps.Map };
        RoomOptions options = new RoomOptions { MaxPlayers = maxPlayers, PlayerTtl = 20000, CleanupCacheOnLeave = false, CustomRoomProperties = props, CustomRoomPropertiesForLobby = customPropertiesForLobby };

        PhotonNetwork.CreateRoom(roomName, options, null);
        createRoomPanel.SetActive(false);
    }
}
