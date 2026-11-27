using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What the map info popup (PopupMapInfo, "i" button on a map card) tells about a map: its art theme and the mood of
/// it, the mode's objective and the map's special terrain. Texts are Main.csv keys:
/// theme_&lt;Theme&gt;, theme_desc_&lt;Theme&gt; (MapInfoTable.Theme), mode_title_&lt;GameMode&gt;, mode_desc_&lt;GameMode&gt;, feat_*.
/// </summary>
public static class MapInfo
{
    public struct Feature
    {
        public string text;
        public Color color;
        public Feature(string text, Color color) { this.text = text; this.color = color; }
    }

    public static string ThemeKey(Map map) => MapInfoTable.Theme.TryGetValue(map, out var t) ? t : null;

    public static string ThemeName(Map map)
    {
        var t = ThemeKey(map);
        return t == null ? "" : LocalizationManager.Localize("theme_" + t);
    }

    public static string ThemeDescription(Map map)
    {
        var t = ThemeKey(map);
        return t == null ? "" : LocalizationManager.Localize("theme_desc_" + t);
    }

    public static string ModeName(GameMode mode) => LocalizationManager.Localize("mode_title_" + mode);
    public static string ModeObjective(GameMode mode) => LocalizationManager.Localize("mode_desc_" + mode);

    /// <summary>Accent colour of a theme (title of the theme line, image frame glow).</summary>
    public static Color ThemeColor(Map map)
    {
        switch (ThemeKey(map))
        {
            case "Ice": return new Color(0.55f, 0.85f, 1f);
            case "Xmas": return new Color(1f, 0.35f, 0.35f);
            case "XmasNight": return new Color(1f, 0.82f, 0.4f);
            case "Egypt": return new Color(1f, 0.82f, 0.45f);
            case "Volcano": return new Color(1f, 0.45f, 0.15f);
            case "Factory": return new Color(0.95f, 0.55f, 0.35f);
            case "Circus": return new Color(1f, 0.35f, 0.4f);
            case "Lunar": return new Color(0.7f, 0.75f, 1f);
            case "Swamp": return new Color(0.45f, 1f, 0.55f);
            case "Greek": return new Color(0.45f, 0.7f, 1f);
            case "Candy": return new Color(1f, 0.55f, 0.8f);
            case "Ocean": return new Color(0.3f, 0.9f, 0.95f);
            case "Savanna": return new Color(1f, 0.7f, 0.35f);
            case "Japan": return new Color(1f, 0.55f, 0.65f);
            case "Western": return new Color(1f, 0.65f, 0.4f);
            case "Bamboo": return new Color(0.55f, 0.95f, 0.4f);
            case "Cave": return new Color(0.5f, 0.75f, 1f);
            case "Moon": return new Color(0.75f, 0.6f, 1f);
            default: return new Color(1f, 0.85f, 0.35f);
        }
    }

    /// <summary>Special terrain and rules of the map, most important first.</summary>
    public static List<Feature> Features(GameMode mode, Map map)
    {
        var list = new List<Feature>();
        switch (ThemeKey(map))
        {
            case "Volcano": list.Add(new Feature(LocalizationManager.Localize("feat_vents"), new Color(1f, 0.45f, 0.15f))); break;
            case "Swamp": list.Add(new Feature(LocalizationManager.Localize("feat_bog"), new Color(0.55f, 0.85f, 0.3f))); break;
            case "Ocean": list.Add(new Feature(LocalizationManager.Localize("feat_current"), new Color(0.3f, 0.9f, 1f))); break;
            case "Savanna": list.Add(new Feature(LocalizationManager.Localize("feat_waterhole"), new Color(0.4f, 1f, 0.6f))); break;
            case "Japan": list.Add(new Feature(LocalizationManager.Localize("feat_torii"), new Color(1f, 0.35f, 0.3f))); break;
            case "XmasNight":
                list.Add(new Feature(LocalizationManager.Localize("feat_gifts"), new Color(1f, 0.86f, 0.35f)));
                list.Add(new Feature(LocalizationManager.Localize("feat_night"), new Color(1f, 0.82f, 0.4f)));
                break;
            // Nov 2026 mechanics (Assets/Scripts/Map: GiftDrop, HumanCannon, MeteorShower, SlickIce, JellyWall, DartTrap)
            case "Xmas": list.Add(new Feature(LocalizationManager.Localize("feat_gifts"), new Color(1f, 0.86f, 0.35f))); break;
            case "Circus": list.Add(new Feature(LocalizationManager.Localize("feat_cannon"), new Color(1f, 0.4f, 0.35f))); break;
            case "Lunar": list.Add(new Feature(LocalizationManager.Localize("feat_meteor"), new Color(1f, 0.55f, 0.25f))); break;
            case "Ice": list.Add(new Feature(LocalizationManager.Localize("feat_slickice"), new Color(0.6f, 0.9f, 1f))); break;
            case "Candy": list.Add(new Feature(LocalizationManager.Localize("feat_jelly"), new Color(1f, 0.55f, 0.8f))); break;
            case "Egypt": list.Add(new Feature(LocalizationManager.Localize("feat_darts"), new Color(1f, 0.75f, 0.35f))); break;
        }
        if (mode == GameMode.JungleBoss)
        {
            string boss = LocalizationManager.Localize(ConfigManager.BossOfMap(map) == TF2Class.yeti ? "boss_name_yeti" : "boss_name_hale");
            list.Add(new Feature(LocalizationManager.Localize("feat_boss", boss), new Color(0.8f, 0.55f, 1f)));
        }
        if (mode == GameMode.Payload || mode == GameMode.MannVsMachine)
            list.Add(new Feature(LocalizationManager.Localize("feat_track"), new Color(0.85f, 0.85f, 0.9f)));
        list.Add(new Feature(LocalizationManager.Localize("feat_bush"), new Color(0.5f, 0.95f, 0.35f)));
        return list;
    }
}
