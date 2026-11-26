using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Egypt theme: a 1960s archaeological dig among painted sandstone ruins. Props from Tools/IceArt/props9.py (same
/// folder and palette as the Ice theme, set up by IceThemeSetup): carved sandstone blocks, obelisks, date palms, cat
/// statues, lotus columns, dig crates, amphorae, a gold sarcophagus, low ruined walls, PapyrusGrass bushes, the SandFloor.
/// Border and lighting are the Western ones (desert cliffs, warm sun); pyramids, palms and tents beyond the border.
/// </summary>
public static partial class BrawlMapBuilder
{
    public static readonly Def[] EgyptMaps =
    {
        new Def {   // layout of Map_Brawl_MoonMaze
            mapEnum = "Map_Brawl_SunTemple", displayName = "Sun Temple", icon = "map_suntemple", mode = GameMode.Competitive, theme = Theme.Egypt, seed = 1401,
            names = new[] { "Sun Temple", "Храм Солнца", "วิหารสุริยะ", "Templo del Sol", "Đền Mặt Trời" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bbb...........bbb..",
                "..###....bbb....###..",
                ".........###.........",
                ".#.................#.",
                ".##.bb.........bb.##.",
                "....##...x.x...##....",
                "........#...#........",
                ".bb.....#...#.....bb.",
                ".bb..##.......##..bb.",
                "......b.......b......",
            },
            center = "...###....x....###...",
        },
        new Def {   // layout of Map_Brawl_BambooRaid (mirrored)
            mapEnum = "Map_Brawl_ScarabVault", displayName = "Scarab Vault", icon = "map_scarabvault", mode = GameMode.CaptureTheFlag, theme = Theme.Egypt, seed = 1402,
            names = new[] { "Scarab Vault", "Хранилище скарабея", "ห้องนิรภัยแมงทับ", "Bóveda del Escarabajo", "Kho Bọ Hung" },
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
        new Def {   // layout of Map_Brawl_CrystalCrown (mirrored)
            mapEnum = "Map_Brawl_ObeliskHill", displayName = "Obelisk Hill", icon = "map_obeliskhill", mode = GameMode.KingOfTheHill, theme = Theme.Egypt, seed = 1403,
            names = new[] { "Obelisk Hill", "Холм обелиска", "เนินเสาโอเบลิสก์", "Colina del Obelisco", "Đồi Cột Tháp" },
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
        new Def {   // layout of Map_Brawl_MoonRail (mirrored)
            mapEnum = "Map_Brawl_NileExpress", displayName = "Nile Express", icon = "map_nileexpress", mode = GameMode.Payload, theme = Theme.Egypt, seed = 1404, pathHalf = new[] { new Vector2(-6f, 16.5f), new Vector2(-6f, 8), new Vector2(6f, 5), new Vector2(6f, 1) },
            names = new[] { "Nile Express", "Нильский экспресс", "รถด่วนแม่น้ำไนล์", "Expreso del Nilo", "Tàu Tốc Hành Sông Nile" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bbb...........bbb..",
                "..###...........###..",
                ".......x.....x.......",
                "#bb...............bb#",
                ".bb..##.......##..bb.",
                ".....................",
                "...#....bbbbb....#...",
                "...#....bbbbb....#...",
                ".....................",
                ".bb...##.....##...bb.",
            },
            center = ".bb......###......bb.",
        },
        new Def {   // layout of Map_Brawl_CrystalFactory (mirrored)
            mapEnum = "Map_Brawl_DuneFoundry", displayName = "Dune Foundry", icon = "map_dunefoundry", mode = GameMode.MannVsMachine, theme = Theme.Egypt, seed = 1405, pathHalf = new[] { new Vector2(-6f, 16.5f), new Vector2(-6f, 8), new Vector2(6f, 5), new Vector2(6f, 1) },
            names = new[] { "Dune Foundry", "Литейня в дюнах", "โรงหล่อเนินทราย", "Fundición de Dunas", "Xưởng Đúc Cồn Cát" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".....x.........x.....",
                "..bb.............bb..",
                "..bb...fff.fff...bb..",
                ".....................",
                "....##.........##....",
                "....##...bbb...##....",
                ".x.......bbb.......x.",
                "......f.......f......",
                ".bbb.............bbb.",
                ".bbb....##.##....bbb.",
            },
            center = "......t...x...t......",
        },
        new Def {   // layout of Map_Brawl_HaleMesa
            mapEnum = "Map_Brawl_PharaohsCourt", displayName = "Pharaoh's Court", icon = "map_pharaohscourt", mode = GameMode.JungleBoss, theme = Theme.Egypt, seed = 1406,
            names = new[] { "Pharaoh's Court", "Двор фараона", "ลานฟาโรห์", "Corte del Faraón", "Sân Pharaoh" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bbb...........bbb..",
                "..###....bbb....###..",
                ".........###.........",
                ".#.................#.",
                ".##.bb.........bb.##.",
                "....##...x.x...##....",
                "........#...#........",
                ".bb.....#...#.....bb.",
                ".....................",
                ".....................",
            },
            center = ".......#.....#.......",
        },
        new Def {   // layout of Map_Brawl_LastLight
            mapEnum = "Map_Brawl_MummyValley", displayName = "Mummy Valley", icon = "map_mummyvalley", mode = GameMode.Zombie, theme = Theme.Egypt, seed = 1407,
            names = new[] { "Mummy Valley", "Долина мумий", "หุบเขามัมมี่", "Valle de las Momias", "Thung Lũng Xác Ướp" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb.............bbb.",
                ".##.bb.........bb.##.",
                "....##.........##....",
                ".bbb.....x.x.....bbb.",
                ".##...............##.",
                "........#...#........",
                "..t......b.b......t..",
                "..###....bbb....###..",
                ".....................",
                ".....................",
            },
            center = "....x...........x....",
        },
    };

    [MenuItem("Tools/Maps/Build Egypt Maps")]
    static void BuildEgyptMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Egypt] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Egypt] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in EgyptMaps) Debug.Log(Build(d));
    }

    static void EgyptTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':
                {
                    string k = v < 0.40 ? "SandBlockA" : v < 0.70 ? "SandBlockB" : "SandBlockC";
                    Prefab(k, p + new Vector3(Jit(rnd, 0.03f), 0, Jit(rnd, 0.03f)), Rot90(rnd) + baseRot + Jit(rnd, 3), 0.97f + (float)rnd.NextDouble() * 0.03f, true);
                }
                break;
            case 'x':
                if (v < 0.35) Prefab("DigCrates", p, Rot90(rnd) + Jit(rnd, 6), 1f, true);
                else if (v < 0.70) Prefab("Amphorae", p, rnd.Next(360), 1f, true);
                else Prefab("Sarcophagus", p, Rot90(rnd) + Jit(rnd, 3), 0.95f, true);
                break;
            case 'f': Prefab("RuinLow", p, fenceRot + Jit(rnd, 2), 1f, true); break;
            case 't':
                if (v < 0.30) Prefab("Obelisk", p, Rot90(rnd), 0.9f, true);
                else if (v < 0.60) Prefab("PalmTree", p, rnd.Next(360), 1f, true);
                else if (v < 0.80) Prefab("CatStatue", p, baseRot + 180f + Jit(rnd, 20), 0.9f, true);   // facing the middle
                else Prefab("LotusColumn", p, Rot90(rnd), 1f, true);
                break;
            case 'b': Grass("PapyrusGrass", p, 1.3f, rnd); break;   // ~1.95 m, like the other bushes
        }
    }

    static void EgyptDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "BrokenColumn", "Amphorae", "BrokenColumn", "Amphorae", "DigCrates");   // (BonePile sits on a snow disc: not here)
        if (!prefabs.ContainsKey(k)) k = "BrokenColumn";
        float sc = k == "BrokenColumn" ? 0.75f : k == "Amphorae" ? 0.5f : 0.5f;
        Prefab(k, o, rot, sc, false);
    }

    /// <summary>Beyond the cliffs: pyramids far off, palms, obelisks, columns and the expedition's tents. Also the temple
    /// gateway behind each team's spawn, where the base scene's gate arch stood (the gap in the border there is closed by
    /// the invisible VitualWall, the gateway closes it for the eye).</summary>
    static void PlaceEgyptBackdrop(int seed)
    {
        PlaceGates("TempleGate");
        var rnd = new Random(seed);
        for (int i = 0; i < 24; i++)
        {
            float a = i / 24f * Mathf.PI * 2f + Jit(rnd, 0.06f);
            var k = Pick(rnd, "PalmTree", "PalmTree", "Obelisk", "LotusColumn", "DigTent", "Pyramid", "PalmTree");
            if (!prefabs.ContainsKey(k)) k = "PalmTree";
            float rad = k == "Pyramid" ? 44f + (float)rnd.NextDouble() * 6f : 33f + (float)rnd.NextDouble() * 7f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.85f, 0f, Mathf.Sin(a) * rad);
            float sc = k == "Pyramid" ? 1.1f + (float)rnd.NextDouble() * 0.5f : k == "PalmTree" ? 1.1f + (float)rnd.NextDouble() * 0.4f : 1.2f;
            Prefab(k, p, k == "Pyramid" ? Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg : rnd.Next(360), sc, false);
        }
    }

    /// <summary>The desert sun: a bright, nearly neutral ambient and a light haze (Tho: sunny, not yellow) (the Western scene's ambient is dark grey and
    /// FlatKit adds the ambient), a warm sun. The blue sky stays.</summary>
    static void EgyptLook(Light light)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.94f, 0.94f, 0.92f); RenderSettings.ambientEquatorColor = new Color(0.87f, 0.86f, 0.83f); RenderSettings.ambientGroundColor = new Color(0.70f, 0.68f, 0.64f);
        RenderSettings.fogColor = new Color(0.86f, 0.88f, 0.90f);
        if (light) light.color = new Color(1.0f, 0.98f, 0.94f);
    }

    /// <summary>After everything is placed (mode objects and the rail track included): props and sand take the desert
    /// materials (bright, neutral daylight; made by IceThemeSetup). Egypt maps (egypt = true) take the white-sun props
    /// material instead (IcePropsEgypt: the sand family toned down; Tho found the golden desert look too yellow).</summary>
    static void EgyptSun(bool egypt = false)
    {
        var swap = new System.Collections.Generic.Dictionary<Material, Material>();
        void Map(string from, string to)
        {
            var a = AssetDatabase.LoadAssetAtPath<Material>(from); var b = AssetDatabase.LoadAssetAtPath<Material>(to);
            if (a && b) swap[a] = b;
        }
        if (egypt && AssetDatabase.LoadAssetAtPath<Material>(IceThemeSetup.PropsEgyptMat) == null) IceThemeSetup.EgyptWhiteSun();
        Map(IceThemeSetup.PropsMat, egypt ? IceThemeSetup.PropsEgyptMat : IceThemeSetup.PropsDesertMat);
        if (egypt) Map(IceThemeSetup.PropsDesertMat, IceThemeSetup.PropsEgyptMat);
        Map(IceThemeSetup.SandGroundMat, IceThemeSetup.SandDesertMat);
        int n = 0;
        foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
        {
            var mats = r.sharedMaterials; bool ch = false;
            for (int i = 0; i < mats.Length; i++) if (mats[i] && swap.TryGetValue(mats[i], out var m)) { mats[i] = m; ch = true; }
            if (ch) { r.sharedMaterials = mats; n++; }
        }
        log += " desertSun=" + n;
    }

    /// <summary>The white-sun look on the existing Egypt map scenes (no rebuild): makes the materials, swaps the props to
    /// IcePropsEgypt and saves each scene.</summary>
    [MenuItem("Tools/Maps/Egypt/Apply White Sun To Egypt Maps")]
    public static string ApplyEgyptWhiteSun()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return "wrong project";
        GuardUnsavedScenes();
        IceThemeSetup.EgyptWhiteSun();
        var done = new System.Collections.Generic.List<string>();
        foreach (var d in AllDefs().Where(x => x.theme == Theme.Egypt))
        {
            string path = "Assets/Scenes/" + d.mapEnum + ".unity";
            if (!System.IO.File.Exists(path)) continue;
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
            log = "";
            EgyptSun(true);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            done.Add(d.mapEnum + log);
        }
        return string.Join("\n", done);
    }

    /// <summary>Fallen column drums, small jars and bones on the open sand (no colliders), clear of walls and spawns.</summary>
    static void ScatterEgyptFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        for (int i = 0; i < 140 && placed < 14; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;   // clear of the floor collider
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            var k = Pick(rnd, "BrokenColumn", "Amphorae", "BrokenColumn");
            if (!prefabs.ContainsKey(k)) k = "BrokenColumn";
            Prefab(k, p, rnd.Next(360), k == "BrokenColumn" ? 0.8f : 0.5f, false);
            placed++;
        }
        log += " egyptScatter=" + placed;
    }
}
