using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class ConfigManager
{
    public static int ver = 1;
    public static int maxPlayerPerRoom = 10;
    public static int timeToSwitchCamOnDeath = 6;
    /// <summary>Respawn wait (s): base_death_time at the start of a match, growing to max_death_time at the end, never
    /// above it (Title Data keys "base_death_time" / "max_death_time", whole seconds).</summary>
    public static int baseDeathTime = 8;
    public static int maxDeathTime = 12;
    public static int timeFlagBackHome = 30;
    public static int pricePerCrate = 5;

    public static string landingPage;
    public static string landingCharacter;

    public static List<string> reciptCodeAccept;
    public static string discordLink;
    public static int minKeyForAdsBase = 3;
    public static int maxKeyForAdsBase = 10;
    public static bool iap = true;
    public static int maxVideoPerDay = 5;

    public static bool canGetTicketByAds = true;
    public static bool showBanner = false;
    public static bool showInterBackHome = true;
    public static bool showInterDie = false;
    /// <summary>MREC (300x250 native banner) switches, Title Data "show_mrec_scoreboard" / "show_mrec_medal" / "show_mrec_die".</summary>
    public static bool showMrecScoreboard = false; // off unless Title Data show_mrec_scoreboard = true
    public static bool showMrecMedal = false;      // off unless Title Data show_mrec_medal = true
    public static bool showMrecDie = true;
    public static bool showMrecMatching = true; // MREC on the "matching" wait before a solo match (Title Data show_mrec_matching)

    public static bool multiplay = false;


    public static Dictionary<WeaponType, float> stopDistances = new Dictionary<WeaponType, float>()
        { { WeaponType.Primary, 7.5f }, { WeaponType.Secondary, 7.5f }, { WeaponType.Mele, 0.65f } };

    public static Dictionary<ItemId, ItemConfig> itemConfigs = new Dictionary<ItemId, ItemConfig>();
    public static Dictionary<TF2Class, CharacterConfig> characterConfigs = new Dictionary<TF2Class, CharacterConfig>();
    public static Dictionary<Taunt, TauntConfig> tauntConfigs = new Dictionary<Taunt, TauntConfig>();

    public static Dictionary<TF2Class, CharacterStats> characterStats = new Dictionary<TF2Class, CharacterStats>();
    public static Dictionary<WeaponId, WeaponStats> weaponStats = new Dictionary<WeaponId, WeaponStats>();

    public static Dictionary<Medal, MedalExp> medals = new Dictionary<Medal, MedalExp>();
    public static TradingData tradingData;

    public static Dictionary<TF2Class, ItemType> classMenuWeapon = new Dictionary<TF2Class, ItemType>() {
        { TF2Class.demoman, ItemType.Primary} ,
        { TF2Class.engineer, ItemType.Mele} ,
        { TF2Class.heavy, ItemType.Primary} ,
        { TF2Class.medic, ItemType.Secondary} ,
        { TF2Class.pyro, ItemType.Primary} ,
        { TF2Class.scout, ItemType.Primary} ,
        { TF2Class.sniper, ItemType.Primary} ,
        { TF2Class.soldier, ItemType.Primary} ,
        { TF2Class.spy, ItemType.Mele}
    };

    public static Dictionary<Map, int> battleTimes;
    /// <summary>Match length (s) for a map missing from Title Data "battle_map_times"; Title Data "default_battle_time" overrides it.</summary>
    public static int defaultBattleTime = 180;

    /// <summary>Match length (s) of maps not listed in Title Data battle_map_times. Payload: 3.5 min, 4 min on the
    /// long-track original map; Mann vs Machine: 6 min; other modes: default_battle_time.</summary>
    public static readonly Dictionary<Map, int> defaultMapBattleTimes = new Dictionary<Map, int>
    {
        { Map.Map_Brawl_Payload, 240 },          // ~106 m of track
        { Map.Map_Brawl_MineExpress, 210 },      // ~34 m
        { Map.Map_Brawl_MoonRail, 210 },
        { Map.Map_Brawl_BambooLine, 210 },
        { Map.Map_Brawl_DynamiteTrail, 210 },
        { Map.Map_Brawl_FrontierConvoy, 210 },
    };
    public static readonly Dictionary<GameMode, int> defaultModeBattleTimes = new Dictionary<GameMode, int>
    {
        { GameMode.Payload, 210 },
        { GameMode.MannVsMachine, 360 },
    };

    /// <summary>Match length for a map: Title Data battle_map_times first, then the defaults above.</summary>
    public static int BattleTimeOf(Map map, GameMode mode)
    {
        if (battleTimes != null && battleTimes.TryGetValue(map, out var t) && t > 0) return t;
        if (defaultMapBattleTimes.TryGetValue(map, out t)) return t;
        if (defaultModeBattleTimes.TryGetValue(mode, out t)) return t;
        return defaultBattleTime;
    }
    public static Dictionary<Map, MapConfig> mapConfigs = new Dictionary<Map, MapConfig>();
    public static List<DailyReward> dailyRewards;
    public static List<TF2Class> robotCanHoldBomb = new List<TF2Class>() { TF2Class.demoman, TF2Class.heavy, TF2Class.pyro, TF2Class.scout, TF2Class.sniper, TF2Class.soldier };
    public static List<QCData> QcDatas;
    public static List<string> randomBotName = new List<string> {"Hello", "Titi_VIP", "My_Love", "fist_player", "Perter179", "ha_phuong", "alex128",
        "prefer110", "RichHunter", "botbot", "NoName", "hahahaha", "vclNhan", "athur", "dragon", "noname", "ImWinner",
        "BuonCuaAnh", "Hunter", "KingMan", "Nhan0804", "LetGo", "hao3", "LoveYou", "KingDragon", "KingHorse", "LionKing",
        "NoBody", "WanaCry", "HiGuy", "NotProblem", "Wolf", "Horse", "Deer", "Tiger", "Dragon", "BoxGaming", "MixiGaming",
        "Beast", "ImBot", "DontKillMe", "Im_nood", "IronMan", "Hurt", "SpiderMan", "BatMan", "SuperMan", "nhabao", "DontShoot",
        "KKK_K", "VaiNoi", "DontAskMe", "KillNow", "Killer", "nobody", "please", "new_player", "doggy", "Kiril", "Greg", "Tadic",
        "Perter", "Liverpool", "Arsenal", "ManC", "Violet", "Vanhel", "LeeJHon", "vltrym", "last_player", "nonono", "bomman",
        "DucThang", "KhanhHuyen", "TramTram", "Haizz", "okBabe", "manasd", "ndndnd", "doitoicodon", "emra", "wolfman", "noproblem",
        "byebye", "good_bye", "noname", "foxx", "noman", "please", "ricky", "star", "katy", "trang", "huyen", "hihi", "dont", "fire",
        "slower", "theflash", "GoHome", "manofUSA", "Ineeddola", "hensome", "wibu", "Doraemon", "Nobita", "Xuka", "Xizuka", "Chaien",
        "Suneo", "Conan", "smasam", "sony", "sonic", "RioMan", "stop", "let_go", "Thoia", "Thiago", "Messi", "Ronaldo", "PickQue", "Congvinh", "HuyenTrang", "Lookso", "FFFF", "OMG141", "Simon72", "Napoleon10", "Misisi", "lalalall", "khnsu", "muoscy", "nanci", "Exo99", "BTS1975", "1977Vlog", "Botbotoo", "Frienfly", "Hehehe", "hoho", "hahah", "vclMl", "Lobanhmy", "traxanh", "vloggly", "huhu", "Tramy", "TraMy99", "Antmannn", "Summer", "Spring", "Auth", "realMan", "Winter", "SoHot", "ItMe", "Vclll",
        "Haizz", "Loki1441", "Tankkkk", "meoMeomao", "LmaoLmao"
    };

    public static TauntConfig GetTauntConfig(Taunt taunt)
    {
        if (tauntConfigs.TryGetValue(taunt, out var tauntConfig))
        {
            return tauntConfig;
        }
        else
        {
            tauntConfig = Resources.Load<TauntConfig>($"Config/Taunts/{taunt}");
            tauntConfigs.Add(taunt, tauntConfig);
            return tauntConfig;
        }
    }
    
    /// <summary>Stats for the boss / zombie attacks until Title Data weapon_stats has them (do not add them before the
    /// build with lenient title-data parsing is on the store). Cloned from Fists so every field is set like a normal melee.</summary>
    public static WeaponStats DefaultAiWeaponStats(WeaponId id)
    {
        WeaponStats s = null;
        if (weaponStats != null && weaponStats.TryGetValue(WeaponId.Fists, out var fists))
            s = Newtonsoft.Json.JsonConvert.DeserializeObject<WeaponStats>(Newtonsoft.Json.JsonConvert.SerializeObject(fists));
        if (s == null) s = new WeaponStats { interval = 1f };
        switch (id)
        {
            case WeaponId.HalePunch: s.minDamage = 50; s.maxDamage = 80; s.interval = 0.9f; break;
            case WeaponId.YetiPunch: s.minDamage = 60; s.maxDamage = 90; s.interval = 1.0f; break;
            case WeaponId.Zombie_Attack: s.minDamage = 20; s.maxDamage = 30; s.interval = 1f; break;
        }
        s.damage = (s.minDamage + s.maxDamage) / 2f;
        return s;
    }

    /// <summary>Boss stats used until Title Data character_stats has an entry for the boss (do not add one before
    /// the build with lenient title-data parsing is on the store). Speed: Heavy's + 25%, already converted like the others.</summary>
    public static CharacterStats DefaultBossStats(TF2Class tf2Class)
    {
        float heavySpeed = characterStats != null && characterStats.TryGetValue(TF2Class.heavy, out var h) ? h.speed / 1.75f : 3f;
        return new CharacterStats
        {
            characterClass = tf2Class,
            maxHealth = tf2Class == TF2Class.yeti ? 12000 : 10000,
            speed = heavySpeed * 1.25f, // aggressive: clearly faster than a Heavy
        };
    }

    public static CharacterConfig GetCharacterConfig(TF2Class tf2Class)
    {
        if (characterConfigs.TryGetValue(tf2Class, out var characterConfig))
        {
            return characterConfig;
        }
        else
        {
            characterConfig = Resources.Load<CharacterConfig>($"Config/Characters/{tf2Class}");
            // bosses (hale, yeti) are not in Title Data character_stats: keep the stats of their config asset
            if (characterStats != null && characterStats.TryGetValue(tf2Class, out var stats))
            {
                characterConfig.characterStats = stats;
                characterConfig.characterStats.speed = stats.speed / 1.75f;
            }
            else if (characterConfig.characterStats == null)
            {
                // stats are not serialized in the config asset: bosses without Title Data entry get code defaults
                characterConfig.characterStats = DefaultBossStats(tf2Class);
            }
            characterConfigs.Add(tf2Class, characterConfig);
            return characterConfig;
        }
    }

    public static Team GetEnemyTeam(Team team)
    {
        Team refTeam;
        refTeam = team == Team.red ? Team.blue : Team.red;
        return refTeam;
    }

    public static ItemConfig GetItemConfig(ItemId itemId)
    {
        if (itemConfigs.TryGetValue(itemId, out var itemConfig))
        {
            return itemConfig;
        }
        else
        {
            return null;
        }
    }
    
    public static List<ItemConfig> GetItemConfigs(ItemType itemType)
    {
        List<ItemConfig> listItems = new List<ItemConfig>();
        foreach (var itemConfig in itemConfigs)
        {
            if(itemConfig.Value.slot == itemType) listItems.Add(itemConfig.Value);
        }

        return listItems;
    }
    
    public static List<ItemConfig> GetItemConfigsByClassAndType(ItemType itemType, TF2Class tf2Class)
    {
        List<ItemConfig> listItems = new List<ItemConfig>();
        foreach (var itemConfig in itemConfigs)
        {
            if(itemConfig.Value.slot == itemType && itemConfig.Value.tf2Classes.Contains(tf2Class)) listItems.Add(itemConfig.Value);
        }
        return listItems;
    }

    public static UserItem CreateUserItem(ItemInstance itemInstance)
    {
        UserItem item = new UserItem()
        {
            itemId = (ItemId)Enum.Parse(typeof(ItemId), itemInstance.ItemId),
            itemInstance = itemInstance
        };

        return item;
    }

    public static Taunt GetTauntFromItemId(ItemId itemId)
    {
        if (!itemId.ToString().StartsWith("taunt")) return Taunt.None;
        string s = itemId.ToString().Remove(0, 6);
        Taunt taunt = (Taunt)Enum.Parse(typeof(Taunt), s);
        return taunt;
    }

    public static CharacterPref GetBotPref(TF2Class tf2Class)
    {

        CharacterConfig characterConfig = GetCharacterConfig(tf2Class);
        Dictionary<ItemType, ItemId> items = new Dictionary<ItemType, ItemId>();
        for (int i = 0; i < characterConfig.defaultWeapons.Length; i++)
        {
            if (characterConfig.defaultWeapons[i] != WeaponId.None)
            {
                ItemType itemType = (ItemType)Enum.Parse(typeof(ItemType), ((WeaponType)(i + 1)).ToString());
                ItemId itemId = (ItemId)Enum.Parse(typeof(ItemId), characterConfig.defaultWeapons[i].ToString());
                items.Add(itemType, itemId);
            }
        }

        return new CharacterPref()
        {
            tF2Class = tf2Class,
            itemLoadOut = items
        };

    }

    /// <summary>Co-op modes against AI (robots, boss, zombies): every player is on the red team, no bots fill the
    /// enemy team from the room size, and only half the room size can join.</summary>
    public static bool IsCoopMode(GameMode m) => m == GameMode.MannVsMachine || m == GameMode.JungleBoss || m == GameMode.Zombie;

    public static bool CheckSelectMap(Map map)
    {
        int cost = GetTicketCost(map);
        if (cost <= 0) return true;
        var user = GameManager.instan != null ? GameManager.instan.userData : null;
        return user != null && user.GetAnyStickerWithMap(map, cost) != null;
    }

    /// <summary>Tickets needed to play a map (0 = free), from mapConfigs (Title Data "map_config" or the defaults below).</summary>
    public static int GetTicketCost(Map map)
    {
        if (mapConfigs.TryGetValue(map, out var cfg) && cfg != null)
            return cfg.requireStick ? Mathf.Max(1, cfg.stickPerPlay) : 0;
        foreach (var d in DefaultTicketCosts) if (d.map == map) return d.cost;
        return 0;
    }

    /// <summary>Default ticket price per map: in each game mode the first map is free (Competitive: the first 3),
    /// the next ones cost 1 to 5 tickets, in the order of the cards in PopupSelectMap.
    /// Title Data "map_config" overrides a map; maps it does not list keep these defaults (ApplyMapTicketDefaults).</summary>
    /// <summary>The first map of a game mode (card order) that is in this build; a room's map that cannot be loaded falls
    /// back to it (rooms made with the old default Map_ARN_Tumble, no longer in the build, hung on "Waiting for other players").</summary>
    public static Map FirstMapOf(GameMode mode)
    {
        foreach (var e in DefaultTicketCosts) if (e.mode == mode && Application.CanStreamedLevelBeLoaded(e.map.ToString())) return e.map;
        foreach (var e in DefaultTicketCosts) if (Application.CanStreamedLevelBeLoaded(e.map.ToString())) return e.map;
        return Map.BrawlScene;
    }

    static readonly (GameMode mode, Map map, int cost)[] DefaultTicketCosts =
    {
        (GameMode.Competitive, Map.BrawlScene, 0),
        (GameMode.Competitive, Map.BrawlScene_2, 0),
        (GameMode.Competitive, Map.Map_Brawl_ShowDown, 0),
        (GameMode.Competitive, Map.Map_Brawl_Cavern, 1),
        (GameMode.Competitive, Map.Map_Brawl_FrontierRuins, 2),
        (GameMode.Competitive, Map.Map_Brawl_FrostPeak, 3),
        (GameMode.Competitive, Map.Map_Brawl_JingleSquare, 4),
        (GameMode.Competitive, Map.Map_Brawl_LanternEve, 5),
        (GameMode.Competitive, Map.Map_Brawl_SunTemple, 6),
        (GameMode.Competitive, Map.Map_Brawl_MagmaRift, 7),
        (GameMode.Competitive, Map.Map_Brawl_SteelYard, 8),
        (GameMode.Competitive, Map.Map_Brawl_CircusRing, 9),
        (GameMode.CaptureTheFlag, Map.Map_Brawl_CTF, 0),
        (GameMode.CaptureTheFlag, Map.Map_Brawl_GemHeist, 1),
        (GameMode.CaptureTheFlag, Map.Map_Brawl_BambooRaid, 2),
        (GameMode.CaptureTheFlag, Map.Map_Brawl_GoldRush, 3),
        (GameMode.CaptureTheFlag, Map.Map_Brawl_BorderSiege, 4),
        (GameMode.CaptureTheFlag, Map.Map_Brawl_FrostVault, 5),
        (GameMode.CaptureTheFlag, Map.Map_Brawl_PresentHeist, 6),
        (GameMode.CaptureTheFlag, Map.Map_Brawl_MidnightGifts, 7),
        (GameMode.CaptureTheFlag, Map.Map_Brawl_ScarabVault, 8),
        (GameMode.CaptureTheFlag, Map.Map_Brawl_ObsidianKeep, 9),
        (GameMode.CaptureTheFlag, Map.Map_Brawl_BigTopRaid, 10),
        (GameMode.KingOfTheHill, Map.Map_Brawl_Koth, 0),
        (GameMode.KingOfTheHill, Map.Map_Brawl_CrystalCrown, 1),
        (GameMode.KingOfTheHill, Map.Map_Brawl_MoonAltar, 2),
        (GameMode.KingOfTheHill, Map.Map_Brawl_SheriffSquare, 3),
        (GameMode.KingOfTheHill, Map.Map_Brawl_CrossroadRuins, 4),
        (GameMode.KingOfTheHill, Map.Map_Brawl_GlacierThrone, 5),
        (GameMode.KingOfTheHill, Map.Map_Brawl_TreeTopHill, 6),
        (GameMode.KingOfTheHill, Map.Map_Brawl_StarlightHill, 7),
        (GameMode.KingOfTheHill, Map.Map_Brawl_ObeliskHill, 8),
        (GameMode.KingOfTheHill, Map.Map_Brawl_CalderaCrown, 9),
        (GameMode.KingOfTheHill, Map.Map_Brawl_RingmasterHill, 10),
        (GameMode.Payload, Map.Map_Brawl_Payload, 0),
        (GameMode.Payload, Map.Map_Brawl_MoonRail, 1),
        (GameMode.Payload, Map.Map_Brawl_BambooLine, 2),
        (GameMode.Payload, Map.Map_Brawl_DynamiteTrail, 3),
        (GameMode.Payload, Map.Map_Brawl_FrontierConvoy, 4),
        (GameMode.Payload, Map.Map_Brawl_FrozenRail, 5),
        (GameMode.Payload, Map.Map_Brawl_SleighRide, 6),
        (GameMode.Payload, Map.Map_Brawl_NightExpress, 7),
        (GameMode.Payload, Map.Map_Brawl_NileExpress, 8),
        (GameMode.Payload, Map.Map_Brawl_LavaLine, 9),
        (GameMode.Payload, Map.Map_Brawl_CircusTrain, 10),
        (GameMode.MannVsMachine, Map.Map_Brawl_MvM, 0),
        (GameMode.MannVsMachine, Map.Map_Brawl_IronGrove, 1),
        (GameMode.MannVsMachine, Map.Map_Brawl_CrystalFactory, 2),
        (GameMode.MannVsMachine, Map.Map_Brawl_MoonbaseSiege, 3),
        (GameMode.MannVsMachine, Map.Map_Brawl_LastStand, 4),
        (GameMode.MannVsMachine, Map.Map_Brawl_IcebreakerPass, 5),
        (GameMode.MannVsMachine, Map.Map_Brawl_ToyWorkshop, 6),
        (GameMode.MannVsMachine, Map.Map_Brawl_CandleWorks, 7),
        (GameMode.MannVsMachine, Map.Map_Brawl_DuneFoundry, 8),
        (GameMode.MannVsMachine, Map.Map_Brawl_CinderWorks, 9),
        (GameMode.MannVsMachine, Map.Map_Brawl_ClockworkFair, 10),
        (GameMode.JungleBoss, Map.Map_Brawl_HaleJungle, 0),
        (GameMode.JungleBoss, Map.Map_Brawl_RedRockPit, 1),
        (GameMode.JungleBoss, Map.Map_Brawl_LunarColossus, 2),
        (GameMode.JungleBoss, Map.Map_Brawl_YetiGlacier, 3),
        (GameMode.JungleBoss, Map.Map_Brawl_SnowGlobe, 4),
        (GameMode.JungleBoss, Map.Map_Brawl_YetiEve, 5),
        (GameMode.JungleBoss, Map.Map_Brawl_PharaohsCourt, 6),
        (GameMode.JungleBoss, Map.Map_Brawl_EmberThrone, 7),
        (GameMode.JungleBoss, Map.Map_Brawl_StrongmanArena, 8),
        (GameMode.Zombie, Map.Map_Brawl_ZombieYard, 0),
        (GameMode.Zombie, Map.Map_Brawl_MoonGraveyard, 1),
        (GameMode.Zombie, Map.Map_Brawl_FrostbiteHollow, 2),
        (GameMode.Zombie, Map.Map_Brawl_CandyCaneAlley, 3),
        (GameMode.Zombie, Map.Map_Brawl_SilentNight, 4),
        (GameMode.Zombie, Map.Map_Brawl_MummyValley, 5),
        (GameMode.Zombie, Map.Map_Brawl_AshenHollow, 6),
        (GameMode.Zombie, Map.Map_Brawl_HauntedFair, 7),
        // retired from the map selection (Nov 2026, one map per art theme per mode; scenes still in the build so rooms
        // made by older clients load): Oasis, DustyCanyon, BambooTemple, MoonMaze, LunarVault, BambooShrine, MineExpress,
        // RobotRidge, HaleMesa, DustDevil, FrontierOutpost, BigfootGulch, StompingGrounds, YetiHollow, MoonlitDen,
        // GhoulMine, CryptTunnels, LastLight, DeadCrater (see BrawlMapBuilder.Retired)
    };

    /// <summary>The boss each Boss Battle (JungleBoss) map is played against: 5 Hale maps, 5 Yeti maps.</summary>
    public static readonly Dictionary<Map, TF2Class> bossOfMap = new Dictionary<Map, TF2Class>
    {
        { Map.Map_Brawl_HaleJungle, TF2Class.hale },
        { Map.Map_Brawl_HaleMesa, TF2Class.hale },
        { Map.Map_Brawl_DustDevil, TF2Class.hale },
        { Map.Map_Brawl_RedRockPit, TF2Class.hale },
        { Map.Map_Brawl_FrontierOutpost, TF2Class.hale },
        { Map.Map_Brawl_LunarColossus, TF2Class.yeti },
        { Map.Map_Brawl_YetiHollow, TF2Class.yeti },
        { Map.Map_Brawl_BigfootGulch, TF2Class.yeti },
        { Map.Map_Brawl_MoonlitDen, TF2Class.yeti },
        { Map.Map_Brawl_StompingGrounds, TF2Class.yeti },
        { Map.Map_Brawl_YetiGlacier, TF2Class.yeti },
        { Map.Map_Brawl_SnowGlobe, TF2Class.yeti },
        { Map.Map_Brawl_YetiEve, TF2Class.yeti },
        { Map.Map_Brawl_PharaohsCourt, TF2Class.hale },
        { Map.Map_Brawl_EmberThrone, TF2Class.hale },
        { Map.Map_Brawl_StrongmanArena, TF2Class.hale },
    };

    public static TF2Class BossOfMap(Map map) => bossOfMap.TryGetValue(map, out var b) ? b : TF2Class.hale;

    /// <summary>Adds the default ticket config for every map mapConfigs does not have yet (called after Title Data is read).</summary>
    public static void ApplyMapTicketDefaults()
    {
        foreach (var d in DefaultTicketCosts)
        {
            if (mapConfigs.ContainsKey(d.map)) continue;
            mapConfigs[d.map] = new MapConfig { gameMode = d.mode, map = d.map, requireStick = d.cost > 0, stickPerPlay = d.cost };
        }
    }
    
    // public static ItemConfig GetStickerWithMap(Map map)
    // {
    //     foreach (var itConfig in itemConfigs.Values)
    //     {
    //         if (itConfig != null && itConfig.slot == ItemType.sticker && itConfig.catalogItem.Tags != null && itConfig.catalogItem.Tags.Contains(map.ToString()))
    //         {
    //             return itConfig;
    //         }
    //     }
    //     return null;
    // }

    public static Medal GetMedal(int score)
    {
        Medal medal = Medal.Newbie;
        int best = int.MinValue;
        foreach (var md in medals)
        {
            if (score >= md.Value.exp && md.Value.exp >= best)
            {
                best = md.Value.exp;
                medal = md.Key;
            }
        }
        return medal;
    }

    /// <summary>Player level: every scorePerLevel points of the rank score (userData.score, the same score as medals) = 1 level.
    /// 50 = about 2 matches: a match gives (win 3 / draw 2 / lose 1) x maxPlayer (10) + (kills + damage/100)/2 - deaths, about 20-30 points on average.
    /// Overridable from PlayFab Title Data key "score_per_level".</summary>
    public static int scorePerLevel = 50;

    /// <summary>Level at which each game mode unlocks (ModeUnlock). Competitive is always open.
    /// Defaults with scorePerLevel 50 (about 20-25 points a match for a new player): CTF Lv 2 = about 3 matches,
    /// then one mode every ~10 matches (~250 points = 5 levels): KOTH Lv 7, Payload Lv 12, MvM Lv 17.
    /// Overridable from PlayFab Title Data key "mode_unlock_level", e.g. {"CaptureTheFlag":2,"KingOfTheHill":7,"Payload":12,"MannVsMachine":17,"JungleBoss":22,"Zombie":27}.</summary>
    public static Dictionary<GameMode, int> modeUnlockLevels = new Dictionary<GameMode, int>
    {
        { GameMode.Competitive, 1 },
        { GameMode.CaptureTheFlag, 2 },
        { GameMode.KingOfTheHill, 7 },
        { GameMode.Payload, 12 },
        { GameMode.MannVsMachine, 17 },
        // one mode every ~10 matches (~5 levels) after MvM
        { GameMode.JungleBoss, 22 },
        { GameMode.Zombie, 27 },
    };

    /// <summary>Level from a rank score; level 1 at 0 points.</summary>
    public static int GetLevel(int score) => Mathf.Max(0, score) / Mathf.Max(1, scorePerLevel) + 1;

    /// <summary>Points needed to reach a level (level 1 = 0).</summary>
    public static int GetLevelScore(int level) => Mathf.Max(0, level - 1) * Mathf.Max(1, scorePerLevel);

    /// <summary>Progress 0..1 inside the current level.</summary>
    public static float GetLevelProgress(int score)
    {
        int per = Mathf.Max(1, scorePerLevel);
        return (Mathf.Max(0, score) % per) / (float)per;
    }

    public static string GetRandomBotName()
    {
        return randomBotName[Random.Range(0, randomBotName.Count)];
    }

}


public class MedalExp
{
    public Medal medal;
    public int exp;
}

public enum Medal: int
{
    Newbie = 0,
    iron_ugc,
    iron_1,
    iron_2,
    iron_3,

    steel_ugc,
    steel_1,
    steel_2,
    steel_3,

    silver_ugc,
    silver_1,
    silver_2,
    silver_3,

    gold_ugc,
    gold_1,
    gold_2,
    gold_3,

    platinum_ugc,
    platinum_1,
    platinum_2,
    platinum_3,
}

public class MapConfig
{
    public GameMode gameMode;
    public Map map;
    public int maxPerDay;
    public bool requireStick;
    public int stickPerPlay;
}


public class QCData
{
    public TF2Class tf2Class;
    public string imageUrl;
    public string content;
    public string direction;
}
