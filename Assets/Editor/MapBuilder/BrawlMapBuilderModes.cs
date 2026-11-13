using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Mode-specific layouts + setup (CTF flags, KOTH point). Layout rules: see BrawlMapBuilder.cs.
// CTF: flags sit behind the spawns at z = -/+20.9 (rows 0-1, cols 8-12 must stay empty).
// KOTH: control point in the centre, tiles rows 12-14 x cols 9-11 must stay empty.
public static partial class BrawlMapBuilder
{
    public static IEnumerable<Def> AllDefs() => Competitive.Concat(CTF).Concat(KOTH).Concat(ExtraDefs());
    static partial void ExtraDefsImpl(List<Def> list);
    static IEnumerable<Def> ExtraDefs() { var l = new List<Def>(); ExtraDefsImpl(l); return l; }

    [MenuItem("Tools/Maps/Build Capture The Flag Maps")]
    public static void BuildCtfMenu() { foreach (var d in CTF) Build(d); }

    [MenuItem("Tools/Maps/Build King Of The Hill Maps")]
    public static void BuildKothMenu() { foreach (var d in KOTH) Build(d); }

    public static readonly Def[] CTF =
    {
        new Def {
            mapEnum = "Map_Brawl_GemHeist", displayName = "Gem Heist", icon = "map11", mode = GameMode.CaptureTheFlag, theme = Theme.Cave, seed = 1101,
            names = new[] { "Gem Heist", "Кража самоцветов", "ปล้นอัญมณี", "Robo de Gemas", "Phi Vụ Đá Quý" },
            half = new[] {
                ".....................",
                ".....................",
                "..##.............##..",
                "..#....x.....x....#..",
                ".....bbb.....bbb.....",
                ".....bbb.....bbb.....",
                ".##.......t.......##.",
                ".....................",
                "...###.........###...",
                "........bbbbb........",
                ".bb.....bbbbb.....bb.",
                ".bb..x.........x..bb.",
                ".bb...##.....##...bb.",
            },
            center = "........#...#........",
        },
        new Def {
            mapEnum = "Map_Brawl_LunarVault", displayName = "Lunar Vault", icon = "map12", mode = GameMode.CaptureTheFlag, theme = Theme.Moon, seed = 1202,
            names = new[] { "Lunar Vault", "Лунное хранилище", "ห้องนิรภัยจันทรา", "Bóveda Lunar", "Kho Báu Mặt Trăng" },
            half = new[] {
                ".....................",
                ".....................",
                "....#...........#....",
                "....#...bbbbb...#....",
                "....#...........#....",
                "..bb...###.###...bb..",
                "..bb.............bb..",
                "......x.......x......",
                ".##....bb...bb....##.",
                ".......bb...bb.......",
                "...#.............#...",
                "...#....#####....#...",
                ".bb...............bb.",
            },
            center = ".bb.....x...x.....bb.",
        },
        new Def {
            mapEnum = "Map_Brawl_BambooRaid", displayName = "Bamboo Raid", icon = "map13", mode = GameMode.CaptureTheFlag, theme = Theme.Bamboo, seed = 1303,
            names = new[] { "Bamboo Raid", "Бамбуковый налёт", "บุกป่าไผ่", "Asalto de Bambú", "Đột Kích Rừng Tre" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "...ff....bbb....ff...",
                "bbb......bbb......bbb",
                "bbb..#.........#..bbb",
                "bbb..#...t.t...#..bbb",
                ".....................",
                "..bbbb.........bbbb..",
                "..bbbb...###...bbbb..",
                "......x.......x......",
                ".##...............##.",
                ".....bb.......bb.....",
            },
            center = "..t.....bbbbb.....t..",
        },
        new Def {
            mapEnum = "Map_Brawl_GoldRush", displayName = "Gold Rush", icon = "map14", mode = GameMode.CaptureTheFlag, theme = Theme.Western, seed = 1404,
            names = new[] { "Gold Rush", "Золотая лихорадка", "ตื่นทอง", "Fiebre del Oro", "Cơn Sốt Vàng" },
            half = new[] {
                ".....................",
                ".....................",
                "...x.............x...",
                "..fff...........fff..",
                "........bbbbb........",
                ".##.....bbbbb.....##.",
                ".....................",
                "....t..##...##..t....",
                ".bb...............bb.",
                ".bb...ff.....ff...bb.",
                ".bb...............bb.",
                "......##..x..##......",
                "...bb...........bb...",
            },
            center = "...bb....###....bb...",
        },
        new Def {
            mapEnum = "Map_Brawl_BorderSiege", displayName = "Border Siege", icon = "map15", mode = GameMode.CaptureTheFlag, theme = Theme.Mix, seed = 1505,
            names = new[] { "Border Siege", "Осада границы", "ล้อมชายแดน", "Asedio Fronterizo", "Vây Hãm Biên Giới" },
            half = new[] {
                ".....................",
                ".....................",
                "..t.............t....",
                "....##.........##....",
                "....#...bbbbb...#....",
                ".bb..................",
                ".bb....x.....x....bb.",
                "..........#..........",
                "...bbb.........bbb...",
                "...bbb..##.##..bbb...",
                ".....................",
                ".##.......t.......##.",
                ".....bb.......bb.....",
            },
            center = "x.....#.......#.....x",
        },
    };

    public static readonly Def[] KOTH =
    {
        new Def {
            mapEnum = "Map_Brawl_CrystalCrown", displayName = "Crystal Crown", icon = "map16", mode = GameMode.KingOfTheHill, theme = Theme.Cave, seed = 2101,
            names = new[] { "Crystal Crown", "Хрустальная корона", "มงกุฎคริสตัล", "Corona de Cristal", "Vương Miện Pha Lê" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "...##...........##...",
                "...#..bb.....bb..#...",
                "......bb.....bb......",
                ".bbb.............bbb.",
                ".bbb....x...x....bbb.",
                "....##.........##....",
                ".....................",
                "..t....##...##....t..",
                "..bb.............bb..",
                "..bb..#.......#..bb..",
            },
            center = "......#.......#......",
        },
        new Def {
            mapEnum = "Map_Brawl_MoonAltar", displayName = "Moon Altar", icon = "map17", mode = GameMode.KingOfTheHill, theme = Theme.Moon, seed = 2202,
            names = new[] { "Moon Altar", "Лунный алтарь", "แท่นบูชาจันทรา", "Altar Lunar", "Tế Đàn Mặt Trăng" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bbb...........bbb..",
                "..###....###....###..",
                ".....................",
                "bb.....x.....x.....bb",
                "bb..##.........##..bb",
                "....#...........#....",
                "......bb.....bb......",
                "......bb..x..bb......",
                ".##...............##.",
                ".....#.........#.....",
            },
            center = ".bb..#.........#..bb.",
        },
        new Def {
            mapEnum = "Map_Brawl_BambooShrine", displayName = "Bamboo Shrine", icon = "map18", mode = GameMode.KingOfTheHill, theme = Theme.Bamboo, seed = 2303,
            names = new[] { "Bamboo Shrine", "Бамбуковое святилище", "ศาลเจ้าไผ่", "Santuario de Bambú", "Miếu Tre" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb......t......bbb.",
                ".bbb.............bbb.",
                "......ff.....ff......",
                "bbbb.............bbbb",
                "bbbb....bbbbb....bbbb",
                "........bbbbb........",
                "...t.............t...",
                "......#.......#......",
                "..bbb...........bbb..",
                "..bbb..f.......f.bbb.",
            },
            center = "bb....t.......t....bb",
        },
        new Def {
            mapEnum = "Map_Brawl_SheriffSquare", displayName = "Sheriff Square", icon = "map19", mode = GameMode.KingOfTheHill, theme = Theme.Western, seed = 2404,
            names = new[] { "Sheriff Square", "Площадь шерифа", "ลานนายอำเภอ", "Plaza del Sheriff", "Quảng Trường Cảnh Sát Trưởng" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "....x...........x....",
                "..bb.............bb..",
                "..bb..fff...fff..bb..",
                ".....................",
                ".##......bbb......##.",
                ".......t.....t.......",
                "...bb.............bb.",
                "...bb...##.##.....bb.",
                ".....................",
                ".x....f.......f....x.",
            },
            center = "..bb...#.....#...bb..",
        },
        new Def {
            mapEnum = "Map_Brawl_CrossroadRuins", displayName = "Crossroad Ruins", icon = "map20", mode = GameMode.KingOfTheHill, theme = Theme.Mix, seed = 2505,
            names = new[] { "Crossroad Ruins", "Руины перекрёстка", "ซากทางแยก", "Ruinas del Cruce", "Tàn Tích Ngã Tư" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "...##...........##...",
                "......bb..t..bb......",
                "......bb.....bb......",
                ".t.................t.",
                "...bbb...###...bbb...",
                "...bbb.........bbb...",
                ".....................",
                ".##....x.....x....##.",
                ".......bb...bb.......",
                "...#...bb...bb...#...",
            },
            center = "...#.............#...",
        },
    };

    // ------------------------------------------------------------------ mode setup
    static void ValidateMode(Def d)
    {
        if (d.mode == GameMode.KingOfTheHill)
        {
            for (int c = 9; c <= 11; c++)
                if (d.half[12][c] != '.' || d.center[c] != '.') throw new Exception(d.mapEnum + ": KOTH centre tiles (rows 12-14, cols 9-11) must be empty");
        }
        if (d.mode == GameMode.CaptureTheFlag)
        {
            for (int r = 0; r <= 1; r++) for (int c = 8; c <= 12; c++)
                if (d.half[r][c] != '.') throw new Exception(d.mapEnum + ": CTF flag area (rows 0-1, cols 8-12) must be empty");
        }
    }

    static void SetupMode(Def d)
    {
        if (d.mode == GameMode.Competitive) return;
        var mmGo = Root("MapManager");
        var comp = mmGo.GetComponent<CompetitiveManager>();
        if (comp) Object.DestroyImmediate(comp);
        switch (d.mode)
        {
            case GameMode.CaptureTheFlag: SetupCtf(mmGo); break;
            case GameMode.KingOfTheHill: SetupKoth(mmGo); break;
            default: SetupExtraModeImpl(d, mmGo); break;
        }
        SceneManager.SetActiveScene(scene);
    }

    static partial void SetupExtraModeImpl(Def d, GameObject mmGo);

    static GameObject CloneRoot(Scene src, string name)
    {
        var o = src.GetRootGameObjects().First(g => g.name == name);
        var c = Object.Instantiate(o);
        c.name = name;
        SceneManager.MoveGameObjectToScene(c, scene);
        return c;
    }

    static void SetupCtf(GameObject mmGo)
    {
        var src = Source("Assets/Scenes/Map_Brawl_CTF.unity");
        try
        {
            var red = CloneRoot(src, "BriefCase");
            var blue = CloneRoot(src, "BriefCase (1)");
            var redH = CloneRoot(src, "BriefCase_Highlight");
            var blueH = CloneRoot(src, "BriefCase (1)_Highlight");
            PlaceFlag(red, redH, new Vector3(0, 0.3f, -20.9f));
            PlaceFlag(blue, blueH, new Vector3(0, 0.3f, 20.9f));
            var mgr = mmGo.AddComponent<CaptureTheFlagManager>();
            mgr.flagControllers = new[] { red.GetComponent<FlagController>(), blue.GetComponent<FlagController>() };
            mgr.targetScore = 3;
            EditorUtility.SetDirty(mgr);
            var pvs = new[] { red, blue }.Select(f => f.GetComponent<Photon.Pun.PhotonView>()).ToArray();
            log += " flagViewIds=" + string.Join("/", pvs.Select(p => p ? p.ViewID.ToString() : "none"));
        }
        finally { }
    }

    static void PlaceFlag(GameObject flag, GameObject hl, Vector3 pos)
    {
        flag.transform.position = pos;
        var fh = hl.GetComponent<FlagHighlight>();
        fh.flag = flag.GetComponent<FlagController>();
        fh.visual = flag.transform.Find("briefcase");
        hl.transform.position = Vector3.zero;
        if (fh.ring) fh.ring.position = new Vector3(pos.x, 0.04f, pos.z);
        if (fh.beam) fh.beam.position = new Vector3(pos.x, fh.beamHeight * 0.5f, pos.z);
        EditorUtility.SetDirty(fh);
    }

    static void SetupKoth(GameObject mmGo)
    {
        var src = Source("Assets/Scenes/Map_Brawl_Koth.unity");
        try
        {
            var srcMgr = src.GetRootGameObjects().First(g => g.name == "MapManager").GetComponent<KingOfTheHillManager>();
            var point = CloneRoot(src, "Point");
            var pbase = CloneRoot(src, "PointBase");
            point.transform.position = new Vector3(0, 0.13f, 0);
            pbase.transform.position = new Vector3(0, 0.06f, 0);
            var mgr = mmGo.AddComponent<KingOfTheHillManager>();
            EditorUtility.CopySerialized(srcMgr, mgr);
            var kp = point.GetComponent<KothPoint>();
            mgr.kothPoint = kp;
            mgr.pointLight = point.GetComponentInChildren<Light>(true);
            kp.kingOfTheHillManager = mgr;
            var vis = pbase.GetComponent<KothPointVisual>();
            var ring = point.transform.Find("PointRing");
            var beam = point.transform.Find("PointBeam");
            vis.renderers = new[] { pbase.GetComponent<Renderer>() };
            vis.ring = ring; vis.beam = beam;
            vis.glowRenderers = new[] { ring.GetComponent<Renderer>(), beam.GetComponent<Renderer>() };
            EditorUtility.SetDirty(mgr); EditorUtility.SetDirty(kp); EditorUtility.SetDirty(vis);
            log += " koth=" + (mgr.kothPoint ? "ok" : "missing") + "/light=" + (mgr.pointLight ? "ok" : "missing");
        }
        finally { }
    }
}
