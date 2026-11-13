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
    public static int baseDeathTime = 8;
    public static int maxDeathTime = 30;
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
    
    public static CharacterConfig GetCharacterConfig(TF2Class tf2Class)
    {
        if (characterConfigs.TryGetValue(tf2Class, out var characterConfig))
        {
            return characterConfig;
        }
        else
        {
            characterConfig = Resources.Load<CharacterConfig>($"Config/Characters/{tf2Class}");
            characterConfig.characterStats = characterStats[tf2Class];
            characterConfig.characterStats.speed = characterStats[tf2Class].speed / 1.75f;
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

    public static bool CheckSelectMap(Map map)
    {
        if (mapConfigs.TryGetValue(map, out var mapConfig))
        {
            if (mapConfig.requireStick)
            {
                UserItem ticket = GameManager.instan.userData.GetAnyStickerWithMap(map, mapConfig.stickPerPlay);
                if (ticket != null) return true;
                else
                {
                    return false;
                }
                
            }
        }

        return true;
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
    /// Overridable from PlayFab Title Data key "mode_unlock_level", e.g. {"CaptureTheFlag":2,"KingOfTheHill":7,"Payload":12,"MannVsMachine":17}.</summary>
    public static Dictionary<GameMode, int> modeUnlockLevels = new Dictionary<GameMode, int>
    {
        { GameMode.Competitive, 1 },
        { GameMode.CaptureTheFlag, 2 },
        { GameMode.KingOfTheHill, 7 },
        { GameMode.Payload, 12 },
        { GameMode.MannVsMachine, 17 },
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