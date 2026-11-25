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

    [Tooltip("Patrol points inside bushes (baked by Tools/Bush/Build Bush Areas). Bots pick one of these instead of a PathPoint with probability bushPatrolChance, so they often hide in bushes.")]
    public Transform[] BushPoints;
    [Range(0f, 1f)] public float bushPatrolChance = 0.45f;

    /// <summary>Random patrol destination for bots: a bush point (bushPatrolChance) or a normal path point.</summary>
    public Vector3 GetPatrolPoint()
    {
        bool hasBush = BushPoints != null && BushPoints.Length > 0;
        bool hasPath = PathPoints != null && PathPoints.Length > 0;
        if (hasBush && (!hasPath || Random.value < bushPatrolChance))
        {
            var b = BushPoints[Random.Range(0, BushPoints.Length)];
            if (b) return b.position;
        }
        if (hasPath)
        {
            var p = PathPoints[Random.Range(0, PathPoints.Length)];
            if (p) return p.position;
        }
        return transform.position;
    }

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
    JungleBoss,   // ported from TF2_AI: team vs the Hale boss
    Zombie,       // ported from TF2_AI: survive zombie waves

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
    Map_Brawl_Oasis,
    Map_Brawl_Cavern,
    Map_Brawl_MoonMaze,
    Map_Brawl_BambooTemple,
    Map_Brawl_DustyCanyon,
    Map_Brawl_FrontierRuins,
    Map_Brawl_GemHeist,
    Map_Brawl_LunarVault,
    Map_Brawl_BambooRaid,
    Map_Brawl_GoldRush,
    Map_Brawl_BorderSiege,
    Map_Brawl_CrystalCrown,
    Map_Brawl_MoonAltar,
    Map_Brawl_BambooShrine,
    Map_Brawl_SheriffSquare,
    Map_Brawl_CrossroadRuins,
    Map_Brawl_MineExpress,
    Map_Brawl_MoonRail,
    Map_Brawl_BambooLine,
    Map_Brawl_DynamiteTrail,
    Map_Brawl_FrontierConvoy,
    Map_Brawl_RobotRidge,
    Map_Brawl_IronGrove,
    Map_Brawl_CrystalFactory,
    Map_Brawl_MoonbaseSiege,
    Map_Brawl_LastStand,
    Map_Brawl_HaleJungle,   // JungleBoss (ported mode)
    Map_Brawl_ZombieYard,   // Zombie (ported mode)
    Map_Brawl_HaleMesa,
    Map_Brawl_DustDevil,
    Map_Brawl_RedRockPit,
    Map_Brawl_FrontierOutpost,
    Map_Brawl_LunarColossus,
    Map_Brawl_GhoulMine,
    Map_Brawl_MoonGraveyard,
    Map_Brawl_CryptTunnels,
    Map_Brawl_DeadCrater,
    Map_Brawl_LastLight,
    Map_Brawl_YetiHollow,      // JungleBoss, Yeti
    Map_Brawl_BigfootGulch,    // JungleBoss, Yeti
    Map_Brawl_MoonlitDen,      // JungleBoss, Yeti
    Map_Brawl_StompingGrounds, // JungleBoss, Yeti
    Map_Brawl_FrostPeak,       // Competitive, Ice theme
    Map_Brawl_FrostVault,      // CaptureTheFlag, Ice
    Map_Brawl_GlacierThrone,   // KingOfTheHill, Ice
    Map_Brawl_FrozenRail,      // Payload, Ice
    Map_Brawl_IcebreakerPass,  // MannVsMachine, Ice
    Map_Brawl_YetiGlacier,     // JungleBoss, Yeti, Ice
    Map_Brawl_FrostbiteHollow, // Zombie, Ice
}