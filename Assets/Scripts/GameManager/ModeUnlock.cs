using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Game modes unlock by player level (UserData.Level, from the rank score).
/// Levels: ConfigManager.modeUnlockLevels (PlayFab Title Data "mode_unlock_level"); Competitive is always open.
///  - PopupSelectMap: cards / tabs of a locked mode show a lock (UIMapIcon.lockObj, MapModeTab.lockObj);
///    choosing one opens ShowLockedPopup (required level + current level).
///  - Home: HomeManager calls TakeNewlyUnlocked + ShowUnlockedPopups, one popup per mode unlocked since the last visit.
///    The last level announced is kept in PlayerPrefs per user (SeenKey). On the first run on a device it is set to the
///    current level silently, so existing players do not get a burst of popups after the update.
/// Joining another player's room keeps working whatever its mode; only choosing a mode yourself is locked.
/// </summary>
public static class ModeUnlock
{
    public static int PlayerLevel
    {
        get
        {
            var gm = GameManager.instan;
            return gm != null && gm.userData != null ? gm.userData.Level : 1;
        }
    }

    public static int UnlockLevel(GameMode mode)
    {
        if (mode == GameMode.Competitive) return 1;
        return ConfigManager.modeUnlockLevels.TryGetValue(mode, out var lv) ? Mathf.Max(1, lv) : 1;
    }

    public static bool IsUnlocked(GameMode mode) => PlayerLevel >= UnlockLevel(mode);

    public static string ModeName(GameMode mode) => LocalizationManager.Localize(mode.ToString());

    /// <summary>"Reach Level N to unlock X" + "Your level: M", with the lock icon.</summary>
    public static void ShowLockedPopup(GameMode mode)
    {
        string msg = LocalizationManager.Localize("mode_locked_req", UnlockLevel(mode), ModeName(mode)) + "\n"
                   + LocalizationManager.Localize("mode_locked_current", PlayerLevel);
        var art = ModeUnlockArt.Instance;
        GUIManager.instan.CreatePopup<PopupNotify>(msg, art != null ? art.lockIcon : null);
    }

    static string SeenKey
    {
        get
        {
            var gm = GameManager.instan;
            string id = gm != null && gm.userData != null ? gm.userData.userId : "";
            return "ModeUnlockSeenLevel_" + id;
        }
    }

    /// <summary>Modes unlocked since the last call (sorted by unlock level), and remembers the current level.</summary>
    public static List<GameMode> TakeNewlyUnlocked()
    {
        var list = new List<GameMode>();
        int level = PlayerLevel;
        string key = SeenKey;
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(key, level);
            PlayerPrefs.Save();
            return list;
        }
        int seen = PlayerPrefs.GetInt(key);
        if (level <= seen) return list;
        foreach (GameMode m in Enum.GetValues(typeof(GameMode)))
        {
            int u = UnlockLevel(m);
            if (u > seen && u <= level) list.Add(m);
        }
        list.Sort((a, b) => UnlockLevel(a).CompareTo(UnlockLevel(b)));
        PlayerPrefs.SetInt(key, level);
        PlayerPrefs.Save();
        return list;
    }

    /// <summary>One "new mode unlocked" popup per mode, the next one opening when the previous is closed.</summary>
    public static void ShowUnlockedPopups(List<GameMode> modes, Action done = null)
    {
        if (modes == null || modes.Count == 0) { done?.Invoke(); return; }
        var mode = modes[0];
        modes.RemoveAt(0);
        string msg = LocalizationManager.Localize("mode_unlocked", ModeName(mode));
        var art = ModeUnlockArt.Instance;
        Sprite icon = art != null ? art.GetModeIcon(mode) : null;
        Action next = () => ShowUnlockedPopups(modes, done);
        GUIManager.instan.CreatePopup<PopupNotify>(msg, icon, next);
    }
}
