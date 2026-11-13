using System;
using UnityEngine;

/// <summary>
/// Sprites used by the game-mode lock / unlock popups (ModeUnlock). Asset: Resources/ModeUnlockArt.asset.
/// </summary>
[CreateAssetMenu(menuName = "Brawl Fortress/Mode Unlock Art", fileName = "ModeUnlockArt")]
public class ModeUnlockArt : ScriptableObject
{
    [Serializable]
    public struct ModeIcon
    {
        public GameMode mode;
        public Sprite icon;
    }

    [Tooltip("Shown in the 'mode locked' popup")]
    public Sprite lockIcon;
    [Tooltip("Shown in the 'new mode unlocked' popup")]
    public ModeIcon[] modeIcons;

    static ModeUnlockArt instance;
    static bool loaded;

    public static ModeUnlockArt Instance
    {
        get
        {
            if (!loaded) { instance = Resources.Load<ModeUnlockArt>("ModeUnlockArt"); loaded = true; }
            return instance;
        }
    }

    public Sprite GetModeIcon(GameMode mode)
    {
        if (modeIcons != null)
            foreach (var e in modeIcons)
                if (e.mode == mode) return e.icon;
        return null;
    }
}
