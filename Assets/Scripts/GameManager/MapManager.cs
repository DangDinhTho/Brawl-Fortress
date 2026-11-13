using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager instan;
    public Transform[] teamPos;
    public DoorController[] doorControllers;
    public Map map;

    public Transform[] PathPoints;

    private void Awake()
    {
        instan = this;
    }

    private void Start()
    {
        ArenaManager.Instance.OnGameStarted += StartBattle;
        if(ArenaManager.Instance.gameState == GameState.Started)
        {
            StartBattle();
        }
    }

    public void StartBattle()
    {
        for(int i = 0; i < doorControllers.Length; i++)
        {
            doorControllers[i].Open();
        }
    }
}

public enum GameMode : byte
{
    Competitive = 0,
    CaptureTheFlag,
    MannVsMachine,
    Payload,
    KingOfTheHill,

}

public enum Map: byte
{
    None = 0,
    Map_1,
    Map_ARN_Tumble,
    Map_CTF_Turbine,
    Map_Playload,
    Map_MvM,
    Map_ARN_Bagel,
    Map_MvM_Fastener,
    Map_Koth_Bagel,
    Map_Koth_OffBlast,
    Map_ARN_1,
    Map_ARN_2,
    Map_ARN_3,
    Map_CTF_1,
    Map_CTF_2,
    Map_CTF_3,
    Map_Payload_1,
    Map_Payload_2,
    Map_Payload_3,
    Map_MvM_1,
    Map_MvM_2,
    Map_MvM_3,
    Map_Koth_1,
    Map_Koth_2,
    Map_Koth_3,
    Map_CP_1,
    Map_CP_2,
    Map_CP_3,
    Map_Koth_Sawmill,
    Map_Koth_Lakeside,
    Map_ARN_Sawmill,
    BrawlScene,
    BrawlScene_2,
    Map_Brawl_Payload,
    Map_Brawl_MvM,
    Map_Brawl_ShowDown,
    Map_Brawl_CTF,
    Map_Brawl_Koth,
    Map_Brawl_Oasis
}