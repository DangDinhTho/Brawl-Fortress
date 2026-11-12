using Photon.Pun;
//using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;

public class PopupSelectMap : PopupBase
{
    [HideInInspector]
    public GameMode gameModeSelected = GameMode.Competitive;
    [HideInInspector]
    public Map mapSelected = Map.BrawlScene;

    //public GameObject mapTemPref;

    //public UIGameModeItem[] uIGameModeItems;
    public Transform mapContainer;
    //private Dictionary<GameMode, UIGameModeItem> gameMaps;
    private bool singlePlayer;
    [HideInInspector]
    public UIMapIcon mapSelectedItem;
    public Transform ticketContainer;


    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        //mapTemPref.SetActive(false);
        //if(gameMaps == null)
        //{
        //    gameMaps = new Dictionary<GameMode, UIGameModeItem>();
        //    for(int i = 0; i < uIGameModeItems.Length; i++)
        //    {
        //        gameMaps.Add(uIGameModeItems[i].gameMode, uIGameModeItems[i]);
        //    }
        //}
        if(args.Length > 0)
        {
            gameModeSelected = (GameMode)args[0];
           
        }
        else
        {
            gameModeSelected = GameMode.Competitive;
        }

        if (args.Length > 1)
        {
            mapSelected = (Map)args[1];
        }
        else
        {
            mapSelected = Map.BrawlScene;
        }

        SetTicket();

        OnGameModeUpdate(gameModeSelected);
    }

    public void OnGameModeUpdate(GameMode gameMode)
    {
        //gameModeSelected = gameMode;
        //for(int i = 0; i < uIGameModeItems.Length; i++)
        //{
        //    uIGameModeItems[i].SetSelect(gameMode == uIGameModeItems[i].gameMode);
        //}
        //InitMaps();
    }

    public void InitMaps()
    {
        //for(int i = 1; i < mapContainer.childCount; i++)
        //{
        //    Destroy(mapContainer.GetChild(i).gameObject);
        //}

        //Map[] maps = gameMaps[gameModeSelected].maps;
        //for(int i = 0; i < maps.Length; i++)
        //{
        //    GameObject map = Instantiate(mapTemPref, mapContainer);
        //    map.SetActive(true);
        //    UIMapIcon uIMapIcon = map.GetComponent<UIMapIcon>();
        //    uIMapIcon.Init(maps[i], gameModeSelected, this, maps[i] == mapSelected);
        //    if(maps[i] == mapSelected)
        //    {
        //        mapSelectedItem = uIMapIcon;
        //    }
        //}
    }


    public void SetTicket()
    {
        int index = 0;
        for(ItemId ticketId = ItemId.Ticket + 1; ticketId < ItemId.EndTicket; ticketId++)
        {
            UserItem ticketData = GameManager.instan.userData.GetUserItem(ticketId);
            int quatity = 0;
            if(ticketData != null)
            {
                quatity = ticketData.itemInstance.RemainingUses.GetValueOrDefault();
            }
            var item = GUIManager.instan.CreateListItem<UITicketItem>(ticketContainer, index);
            item.InitTicket(ticketId, quatity);
            index++;
        }
        GUIManager.instan.CleanListItem(ticketContainer, index);
    }

    
    public void SelectedMap(GameMode gameMode, Map map, UIMapIcon uIMapIcon)
    {
        if (mapSelected == map) return;
        if (mapSelectedItem != null && mapSelectedItem.gameObject.activeInHierarchy) mapSelectedItem.Deselected();
        gameModeSelected = gameMode;
        mapSelected = map;
        mapSelectedItem = uIMapIcon;
        LobbyManager.instance.InsideRoomPanel.OnChangeMap(gameMode, map);
    }
 

    public void OnDoneClick()
    {
        if (singlePlayer)
        {
            LobbyManager.instance.sigleGameMode = gameModeSelected;
            LobbyManager.instance.sigleMap = mapSelected;
            GUIManager.instan.CreatePopup<PopupSelectClass>(true);
        }
        else
        {
            Hashtable props = new Hashtable
            {
                {TF2GameProps.GameModeKey, gameModeSelected},
                {TF2GameProps.Map, mapSelected}
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        Dismiss();
    }


    public void SetSinglePlayer()
    {
        singlePlayer = true;

    }
}
