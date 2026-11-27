using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = System.Random;

/// <summary>
/// Builds Brawl-Stars-like 3v3 maps from an ASCII tile layout + a prop theme.
/// Base scene = Map_Brawl_Oasis (spawns, doors, virtual walls, cliff border, MapManager...).
/// Grid: 21 columns x 27 rows, tile 1.5 m (x -15..15, z -19.5..19.5). Layout = 13 rows for the red half
/// (row 0 = red back line) + 1 centre row; the blue half is the point mirror ((r,c) -> (26-r, 20-c)).
/// Legend: '.' empty, '#' wall, 'b' bush, 'x' crate/small cover, 'f' low fence, 't' tall cover.
/// Menu: Tools/Maps/Build Competitive Maps.
/// </summary>
public static partial class BrawlMapBuilder
{
    public enum Theme { Western, Bamboo, Cave, Moon, Mix, Ice, Xmas, Egypt, Volcano, Factory, Circus, Lunar, Swamp, Greek, Candy, Ocean, Savanna, Japan }

    public class Def
    {
        public string mapEnum, displayName, icon;
        public GameMode mode;
        public Theme theme;
        public int seed;
        public string[] half;   // 13 rows, red half, row 0 = back line
        public string center;   // row 13, must be a palindrome
        public string[] names;  // en, ru, th, es, vi
        public Vector2[] pathHalf; // Payload / MvM: cart path from the blue side to the centre (x,z); mirrored to the red side
        public bool night;         // Xmas: evening look (dark materials, warm lights)
    }

    const string BaseScene = "Assets/Scenes/Map_Brawl_Oasis.unity";
    const string PrefabRoot = "Assets/Battle Arena - Cartoon Assets/Prefabs";
    const string WorldMat = "Assets/Battle Arena - Cartoon Assets/Materials/World.mat";
    const string WorldTMat = "Assets/Battle Arena - Cartoon Assets/Materials/World_Transparent.mat";
    const float T = 1.5f;
    const int COLS = 21, ROWS = 27;
    const int WallLayer = 11;

    /// <summary>Ice (arctic) maps: props from Assets/Art/IceTheme (IceThemeSetup). Kept apart from the bulk rebuilds.</summary>
    public static readonly Def[] IceMaps =
    {
        new Def {
            mapEnum = "Map_Brawl_FrostPeak", displayName = "Frost Peak", icon = "map_frostpeak", mode = GameMode.Competitive, theme = Theme.Ice, seed = 901,
            names = new[] { "Frost Peak", "Морозный пик", "ยอดเขาน้ำแข็ง", "Pico Helado", "Đỉnh Băng Giá" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bb...x.......x...bb.",
                ".bb...ff.....ff...bb.",
                ".bb..##.......##..bb.",
                ".bb.x...........x.bb.",
                ".bb......###......bb.",
                ".bbb.............bbb.",
                "....##.........##....",
                "..t...f..b.b..f...t..",
                "........bbbbb........",
                ".##...............##.",
            },
            center = ".......#.....#.......",
        },
        // one Ice map per other game mode (paths of the Payload / MvM maps = PathE / PathB, inlined)
        new Def {
            mapEnum = "Map_Brawl_FrostVault", displayName = "Frost Vault", icon = "map_frostvault", mode = GameMode.CaptureTheFlag, theme = Theme.Ice, seed = 911,
            names = new[] { "Frost Vault", "Ледяное хранилище", "ห้องนิรภัยน้ำแข็ง", "Bóveda Helada", "Kho Băng Giá" },
            half = new[] {
                ".....................",
                ".....................",
                "...##...........##...",
                "...#...x.....x...#...",
                ".bb.....bbbbb.....bb.",
                ".bb..f..bbbbb..f..bb.",
                "......##.....##......",
                "..t...............t..",
                ".....x..##.##..x.....",
                ".bbb.............bbb.",
                ".bbb...bb...bb...bbb.",
                "....##..bb.bb..##....",
                "..x......#.#......x..",
            },
            center = ".....##.......##.....",
        },
        new Def {
            mapEnum = "Map_Brawl_GlacierThrone", displayName = "Glacier Throne", icon = "map_glacierthrone", mode = GameMode.KingOfTheHill, theme = Theme.Ice, seed = 921,
            names = new[] { "Glacier Throne", "Ледниковый трон", "บัลลังก์ธารน้ำแข็ง", "Trono Glaciar", "Ngai Vàng Sông Băng" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bbb...#...#...bbb..",
                "..bbb...#...#...bbb..",
                ".....x.........x.....",
                ".##.....bbbbb.....##.",
                ".#......bbbbb......#.",
                "....t....f.f....t....",
                "..bb....#...#....bb..",
                "..bb.##.......##.bb..",
                "........x...x........",
                "...##...........##...",
            },
            center = ".....#.........#.....",
        },
        new Def {
            mapEnum = "Map_Brawl_FrozenRail", displayName = "Frozen Rail", icon = "map_frozenrail", mode = GameMode.Payload, theme = Theme.Ice, seed = 931, pathHalf = new[] { new Vector2(-4, 16.5f), new Vector2(-4, 6), new Vector2(4, 2) },
            names = new[] { "Frozen Rail", "Ледяная колея", "รางน้ำแข็ง", "Riel Congelado", "Đường Ray Đóng Băng" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##.............##..",
                "..#..bb.......bb..#..",
                ".....bb...x...bb.....",
                ".bbb.............bbb.",
                ".bbb..##.....##..bbb.",
                "........t...t........",
                "..x......bbb......x..",
                ".##......bbb......##.",
                ".....f.........f.....",
                "...bb...#...#...bb...",
            },
            center = "...bb...........bb...",
        },
        new Def {
            mapEnum = "Map_Brawl_IcebreakerPass", displayName = "Icebreaker Pass", icon = "map_icebreakerpass", mode = GameMode.MannVsMachine, theme = Theme.Ice, seed = 941, pathHalf = new[] { new Vector2(6, 16.5f), new Vector2(6, 8), new Vector2(-6, 5), new Vector2(-6, 1) },
            names = new[] { "Icebreaker Pass", "Перевал Ледокол", "ช่องเขาทลายน้ำแข็ง", "Paso Rompehielos", "Đèo Phá Băng" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bb..x.........x..bb.",
                ".bb.....##.##.....bb.",
                ".bb...............bb.",
                "....##..bbbbb..##....",
                "........bbbbb........",
                ".t.......f.f.......t.",
                "...###.........###...",
                ".bb......x.x......bb.",
                ".bb...............bb.",
                "....##...#.#...##....",
            },
            center = "..bb.....#.#.....bb..",
        },
        new Def {
            mapEnum = "Map_Brawl_YetiGlacier", displayName = "Yeti Glacier", icon = "map_yetiglacier", mode = GameMode.JungleBoss, theme = Theme.Ice, seed = 951,
            names = new[] { "Yeti Glacier", "Ледник Йети", "ธารน้ำแข็งเยติ", "Glaciar del Yeti", "Sông Băng Yeti" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bb.............bb..",
                "..bb...##...##...bb..",
                ".....................",
                ".t.......bbb.......t.",
                "....##.........##....",
                "..x.....f...f.....x..",
                ".......#.....#.......",
                "..bbb...........bbb..",
                "..bbb....x.x....bbb..",
                ".....##.......##.....",
            },
            center = "..........t..........",
        },
        new Def {
            mapEnum = "Map_Brawl_FrostbiteHollow", displayName = "Frostbite Hollow", icon = "map_frostbitehollow", mode = GameMode.Zombie, theme = Theme.Ice, seed = 961,
            names = new[] { "Frostbite Hollow", "Обмороженная лощина", "หุบเหวหิมะกัด", "Hondonada Gélida", "Thung Lũng Tê Cóng" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb.....x.x.....bbb.",
                ".bbb..##.....##..bbb.",
                ".....##.......##.....",
                ".x.......bbb.......x.",
                "..t......bbb......t..",
                "...##..f.....f..##...",
                ".bb.....#...#.....bb.",
                ".bb..x.........x..bb.",
                "....###.......###....",
                "..bb......t......bb..",
            },
            center = "....x...........x....",
        },
    };

    [MenuItem("Tools/Maps/Build Frost Peak (Ice)")]
    static void BuildFrostPeakMenu()
    {
        Debug.Log("[IceTheme] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[IceTheme] assets missing: unzip IceTheme_Props.zip into " + IceThemeSetup.Root); return; }
        Debug.Log(Build(IceMaps[0]));
    }

    [MenuItem("Tools/Maps/Build All Ice Maps")]
    static void BuildAllIceMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[IceTheme] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[IceTheme] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in IceMaps) Debug.Log(Build(d));
    }

    public static readonly Def[] Competitive =
    {
        new Def {
            mapEnum = "Map_Brawl_Cavern", displayName = "Crystal Cavern", icon = "map6", mode = GameMode.Competitive, theme = Theme.Cave, seed = 101,
            names = new[] { "Crystal Cavern", "Хрустальная пещера", "ถ้ำคริสตัล", "Caverna de Cristal", "Hang Pha Lê" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bb...x.......x...bb.",
                ".bb...............bb.",
                ".bb..##.......##..bb.",
                ".bb...............bb.",
                ".bb......###......bb.",
                ".bbb.............bbb.",
                "....##.........##....",
                "..t......b.b......t..",
                "........bbbbb........",
                ".##...............##.",
            },
            center = ".......#.....#.......",
        },
        new Def {
            mapEnum = "Map_Brawl_MoonMaze", displayName = "Moonlit Maze", icon = "map7", mode = GameMode.Competitive, theme = Theme.Moon, seed = 202,
            names = new[] { "Moonlit Maze", "Лунный лабиринт", "เขาวงกตแสงจันทร์", "Laberinto Lunar", "Mê Cung Ánh Trăng" },
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
        new Def {
            mapEnum = "Map_Brawl_BambooTemple", displayName = "Bamboo Temple", icon = "map8", mode = GameMode.Competitive, theme = Theme.Bamboo, seed = 303,
            names = new[] { "Bamboo Temple", "Бамбуковый храм", "วัดไผ่", "Templo de Bambú", "Đền Tre" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb......t......bbb.",
                ".bbb.............bbb.",
                ".....ff.......ff.....",
                ".bbb...bbbbbbb...bbb.",
                ".bbb...bbbbbbb...bbb.",
                "......#.......#......",
                "..t.......x.......t..",
                "..bbbbb.....#..bbbb..",
                "..bbbbb.....#..bbbb..",
                "...........t.........",
            },
            center = ".bbb....#####....bbb.",
        },
        new Def {
            mapEnum = "Map_Brawl_DustyCanyon", displayName = "Dusty Canyon", icon = "map9", mode = GameMode.Competitive, theme = Theme.Western, seed = 404,
            names = new[] { "Dusty Canyon", "Пыльный каньон", "หุบเขาฝุ่น", "Cañón Polvoriento", "Hẻm Núi Bụi Mù" },
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
        new Def {
            mapEnum = "Map_Brawl_FrontierRuins", displayName = "Frontier Ruins", icon = "map10", mode = GameMode.Competitive, theme = Theme.Mix, seed = 505,
            names = new[] { "Frontier Ruins", "Руины фронтира", "ซากปรักหักพังชายแดน", "Ruinas de la Frontera", "Tàn Tích Biên Cương" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "....bb.........bb....",
                "....bb...ttt...bb....",
                ".....................",
                "..##.............##..",
                "..#...............#..",
                "......bb.....bb......",
                "..x...bb.....bb...x..",
                "........#...#........",
                ".bb.....#...#.....bb.",
                ".bb...............bb.",
            },
            center = "....t....###....t....",
        },
    };

    // ------------------------------------------------------------------ menu
    [MenuItem("Tools/Maps/Build Competitive Maps")]
    public static void BuildCompetitiveMenu()
    {
        foreach (var d in Competitive) Build(d);
    }

    public static Def Find(string mapEnum) => AllDefs().First(d => d.mapEnum == mapEnum);

    // ------------------------------------------------------------------ state
    static Dictionary<string, string> prefabs;
    static Material world, worldT;
    static Scene scene;
    static Transform env;
    static Dictionary<string, GameObject> moon; // templates cloned from Map_Brawl_ShowDown
    static List<Vector3> spawnPoints;
    static string log;

    public static string Build(Def d)
    {
        log = "";
        Validate(d);
        string path = "Assets/Scenes/" + d.mapEnum + ".unity";

        // Tho's rule: a map scene is always opened alone (OpenSceneMode.Single), never additively with another
        // scene. Source scenes (lighting, Moon templates, mode objects) are opened Single one by one BEFORE the
        // map and copied into hidden preview scenes (see BrawlMapBuilderSources.cs).
        GuardUnsavedScenes();
        BeginSources();
        try
        {
        PrefetchSources(d);

        // Unity keeps a scene file memory-mapped for a while after it was open (IOException "user-mapped section
        // open" on the copy): unloading unused assets releases it.
        EditorUtility.UnloadUnusedAssetsImmediate(); System.GC.Collect(); System.GC.WaitForPendingFinalizers();
        if (File.Exists(path))
        {
            try { File.Copy(BaseScene, path, true); }
            catch (IOException)
            {   // still mapped: let Unity write it (save the base scene over it; the .meta / GUID stay)
                var b = EditorSceneManager.OpenScene(BaseScene, OpenSceneMode.Single);
                if (!EditorSceneManager.SaveScene(b, path, false)) throw;
            }
        }
        else AssetDatabase.CopyAsset(BaseScene, path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        LoadAssets();
        xmasNight = d.theme == Theme.Xmas && d.night; nightLampKeys.Clear(); nightLampPos.Clear();

        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SceneManager.SetActiveScene(scene);
        env = Root("Environment").transform;
        spawnPoints = Root("TeamPos").GetComponentsInChildren<Transform>().Where(t => t.childCount == 0).Select(t => t.position).ToList();
        if (d.theme == Theme.Moon) LoadMoonTemplates();
        PrePlace(d);

        ApplyLighting(d.theme);
        PrepareBorderAndGround(d.theme);
        Physics.SyncTransforms();
        if (d.theme == Theme.Volcano || d.theme == Theme.Factory || d.theme == Theme.Circus || d.theme == Theme.Lunar || d.theme == Theme.Swamp || d.theme == Theme.Greek || d.theme == Theme.Candy || d.theme == Theme.Ocean || d.theme == Theme.Savanna || d.theme == Theme.Japan) PrepareVolcanoWalls(d);   // one family per wall
        PlaceGrid(d);
        GrassHeight.Raise(scene); // grass about as tall as the characters
        if (d.theme == Theme.Moon) PlaceMoonBackdrop(d.seed);
        if (d.theme == Theme.Ice) { PlaceIceBackdrop(d.seed); ScatterIceFloor(d.seed); }
        if (d.theme == Theme.Xmas) { PlaceXmasBackdrop(d.seed); ScatterXmasFloor(d.seed); }
        if (d.theme == Theme.Xmas && d.night) XmasNight();
        if (d.theme == Theme.Egypt) { PlaceEgyptBackdrop(d.seed); ScatterEgyptFloor(d.seed); }
        if (d.theme == Theme.Volcano) { PlaceVolcanoBackdrop(d.seed); PlaceVents(d); ScatterVolcanoFloor(d.seed); }
        if (d.theme == Theme.Factory) { PlaceFactoryBackdrop(d.seed); ScatterFactoryFloor(d.seed); }
        if (d.theme == Theme.Circus) { PlaceCircusBackdrop(d.seed); ScatterCircusFloor(d.seed); }
        if (d.theme == Theme.Lunar) { PlaceLunarBackdrop(d.seed); ScatterLunarFloor(d.seed); }
        if (d.theme == Theme.Swamp) { PlaceSwampBackdrop(d.seed); PlaceBogPuddles(d); ScatterSwampFloor(d.seed); }
        if (d.theme == Theme.Greek) { PlaceGreekBackdrop(d.seed); ScatterGreekFloor(d.seed); }
        if (d.theme == Theme.Candy) { PlaceCandyBackdrop(d.seed); ScatterCandyFloor(d.seed); }
        if (d.theme == Theme.Ocean) { PlaceOceanBackdrop(d.seed); PlaceCurrents(d); ScatterOceanFloor(d.seed); }
        if (d.theme == Theme.Savanna) { PlaceSavannaBackdrop(d.seed); PlaceWaterHoles(d); ScatterSavannaFloor(d.seed); }
        if (d.theme == Theme.Japan) { PlaceJapanBackdrop(d.seed); PlaceToriiPaths(d); ScatterJapanFloor(d.seed); }
        var holder = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "_templates");
        if (holder) Object.DestroyImmediate(holder);
        Physics.SyncTransforms();

        SetupMode(d);
        if (d.theme == Theme.Volcano) VolcanoSky();
        if (d.theme == Theme.Egypt || d.theme == Theme.Factory || d.theme == Theme.Circus || d.theme == Theme.Lunar) EgyptSun(d.theme == Theme.Egypt);
        if (d.theme == Theme.Lunar) LunarSky();
        if (d.theme == Theme.Swamp) { SwampTwilight(); SwampSky(); }
        if (d.theme == Theme.Greek) { GreekSun(); GreekSky(); }
        if (d.theme == Theme.Candy) { GreekSun(); CandySky(); }   // the softer Greek light, pastel sky
        if (d.theme == Theme.Savanna) { EgyptSun(); SavannaSky(); }   // the bright neutral desert daylight, a clear sky
        if (d.theme == Theme.Japan) { GreekSun(); JapanSky(); }       // the soft Greek daylight, a spring sky
        if (d.theme == Theme.Ocean) { OceanWater(); OceanSky(); }   // bright neutral daylight materials
        if (d.theme >= Theme.Ice) log += ClearEndCamera();   // nothing tall behind the spawns / bomb hole in the camera's way
        Physics.SyncTransforms();
        // bushes, navmesh, path points
        EditorApplication.ExecuteMenuItem("Tools/Bush/Build Bush Areas (Active Scene)");
        var bushes = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Bushes");
        log += " bushes=" + (bushes ? bushes.transform.childCount : 0);
        var navData = BakeNavMesh(d.mapEnum);
        FixPathPoints(d, navData);
        // bot patrol points inside bushes need the final NavMesh: rebuild them now
        log += " bushPoints=" + BushPatrolPoints.Build(scene) + "/offNav=" + BushPatrolPoints.LastOffNav;

        var mm = Root("MapManager").GetComponent<MapManager>();
        var mso = new SerializedObject(mm);
        mso.FindProperty("map").enumValueIndex = Array.IndexOf(Enum.GetNames(typeof(Map)), d.mapEnum);
        mso.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene);
        RenderIcon(d);
        EditorSceneManager.SaveScene(scene);

        AddLocalization(d);
        AddBuildSettings(path);
        AddCard(d);
        AssetDatabase.SaveAssets();
        CleanupTemplates();
        return d.mapEnum + ":" + log;
        }
        finally { EndSources(); }
    }

    static void Validate(Def d)
    {
        if (d.half.Length != 13) throw new Exception(d.mapEnum + " half must have 13 rows");
        foreach (var r in d.half.Append(d.center))
            if (r.Length != COLS) throw new Exception(d.mapEnum + " bad row length " + r.Length + ": " + r);
        if (new string(d.center.Reverse().ToArray()) != d.center) throw new Exception(d.mapEnum + " centre row must be a palindrome");
        ValidateMode(d);
    }

    static GameObject Root(string n) => scene.GetRootGameObjects().First(g => g.name == n);

    static void LoadAssets()
    {
        prefabs = new Dictionary<string, string>();
        foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            prefabs[Path.GetFileNameWithoutExtension(p)] = p;
        }
        if (AssetDatabase.IsValidFolder(IceThemeSetup.PrefabDir))
            foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { IceThemeSetup.PrefabDir }))
            { var p = AssetDatabase.GUIDToAssetPath(g); prefabs[Path.GetFileNameWithoutExtension(p)] = p; }
        world = AssetDatabase.LoadAssetAtPath<Material>(WorldMat);
        worldT = AssetDatabase.LoadAssetAtPath<Material>(WorldTMat);
    }

    // ------------------------------------------------------------------ moon templates
    static Scene moonScene;
    static void LoadMoonTemplates()
    {
        moon = new Dictionary<string, GameObject>();
        templateHolder = null;
        moonScene = Source("Assets/Scenes/Map_Brawl_ShowDown.unity");
        var menv = moonScene.GetRootGameObjects().First(g => g.name == "Environment");
        var mfs = menv.GetComponentsInChildren<MeshFilter>(true);
        foreach (var key in new[] { "Wall_Tile_Block1", "Grass_Tile_A_opt", "PolyPyramid", "Night_Plane", "RockFlat3", "RockFlat5", "RockFlat6", "Skull_opt", "Bone_opt", "Bone_ChestA_opt", "Bones_ChestB_opt" })
        {
            var mf = mfs.FirstOrDefault(m => m.sharedMesh && m.sharedMesh.name.StartsWith(key)) ?? mfs.FirstOrDefault(m => m.sharedMesh && m.sharedMesh.name.Contains(key));
            if (mf) moon[key] = KeepTemplate(mf.gameObject);
        }
        var chest = mfs.FirstOrDefault(m => m.sharedMesh && m.sharedMesh.name == "Chest");
        if (chest) moon["Chest"] = KeepTemplate(chest.gameObject);
        moonScene = default;
        SceneManager.SetActiveScene(scene);
        log += " moonTemplates=" + moon.Count;
    }

    static GameObject templateHolder;
    static GameObject KeepTemplate(GameObject src)
    {
        if (templateHolder == null)
        {
            templateHolder = new GameObject("_templates");
            SceneManager.MoveGameObjectToScene(templateHolder, scene);
            templateHolder.SetActive(false);
        }
        var c = Object.Instantiate(src);
        c.name = src.name;
        SceneManager.MoveGameObjectToScene(c, scene);
        c.transform.SetParent(templateHolder.transform, false);
        return c;
    }

    static void CleanupTemplates()
    {
        moonScene = default;
    }

    static GameObject CloneMoon(string key, Vector3 pos, float rotY, Vector3 scale, bool col)
    {
        var go = Object.Instantiate(moon[key]);
        go.transform.SetParent(null, false);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.SetActive(true);
        go.name = moon[key].name;
        go.transform.SetParent(env, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, rotY, 0);
        go.transform.localScale = scale;
        foreach (var c in go.GetComponents<Collider>()) Object.DestroyImmediate(c);
        if (col) go.AddComponent<BoxCollider>();
        Finish(go);
        return go;
    }

    // ------------------------------------------------------------------ placement helpers
    static void Finish(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = WallLayer;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, (StaticEditorFlags)int.MaxValue);
        }
    }

    static GameObject Prefab(string name, Vector3 pos, float rotY, float s, bool col, Material mat = null, Vector3? scale3 = null)
    {
        if (!prefabs.TryGetValue(name, out var p)) { log += " [missing " + name + "]"; return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(p), scene);
        go.transform.SetParent(env, false);
        if (IsFlushDecal(name) && Mathf.Abs(pos.y) < 0.004f) pos.y = DecalLift;   // faces flush with the floor: no z-fighting
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, rotY, 0);
        go.transform.localScale = scale3 ?? Vector3.one * s;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = col;
        // ice props keep their own palette material; the others get the shared World atlas
        if (mat != null || !p.StartsWith(IceThemeSetup.Root))
        {
            var m = mat ? mat : world;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterials = r.sharedMaterials.Select(_ => m).ToArray();
        }
        Finish(go);
        return go;
    }

    static T Pick<T>(Random rnd, params T[] items) => items[rnd.Next(items.Length)];
    static float Rot90(Random rnd) => rnd.Next(4) * 90f;

    // PlaceGates (the themed gate behind each spawn): BrawlMapBuilderGates.cs
    // per-tile Randoms are seeded with consecutive numbers, so their draws are correlated (a pick from a list skips
    // whole ranges of it). Mix() scrambles a draw (murmur3 finalizer) before it is used for a choice.
    static int Mix(int h) { unchecked { uint x = (uint)h; x ^= x >> 16; x *= 0x85ebca6b; x ^= x >> 13; x *= 0xc2b2ae35; x ^= x >> 16; return (int)(x & 0x7fffffff); } }
    static Random MixRnd(Random rnd) => new Random(Mix(rnd.Next()));
    static float Jit(Random rnd, float a) => (float)(rnd.NextDouble() * 2 - 1) * a;

    // ------------------------------------------------------------------ lighting / border / ground
    static void ApplyLighting(Theme theme)
    {
        string refScene = theme switch
        {
            Theme.Western => "Assets/Scenes/BrawlScene.unity",
            Theme.Ice => "Assets/Scenes/BrawlScene.unity",
            Theme.Xmas => "Assets/Scenes/BrawlScene.unity",
            Theme.Egypt => "Assets/Scenes/BrawlScene.unity",
            Theme.Volcano => "Assets/Scenes/BrawlScene.unity",
            Theme.Factory => "Assets/Scenes/BrawlScene.unity",
            Theme.Circus => "Assets/Scenes/BrawlScene.unity",
            Theme.Lunar => "Assets/Scenes/BrawlScene.unity",
            Theme.Swamp => "Assets/Scenes/BrawlScene.unity",
            Theme.Greek => "Assets/Scenes/BrawlScene.unity",
            Theme.Candy => "Assets/Scenes/BrawlScene.unity",
            Theme.Ocean => "Assets/Scenes/BrawlScene.unity",
            Theme.Savanna => "Assets/Scenes/BrawlScene.unity",
            Theme.Japan => "Assets/Scenes/BrawlScene.unity",
            Theme.Bamboo => "Assets/Scenes/BrawlScene_2.unity",
            Theme.Cave => "Assets/Scenes/Map_Brawl_Payload.unity",
            Theme.Moon => "Assets/Scenes/Map_Brawl_ShowDown.unity",
            _ => null,
        };
        if (refScene == null || !sourceLights.TryGetValue(refScene, out var li)) return;
        var mode = li.mode; var sky = li.sky; var eq = li.eq; var gr = li.gr; var amb = li.amb; var ai = li.ai;
        var box = li.box; var fog = li.fog; var fogC = li.fogC; var fogM = li.fogM; var fs = li.fs; var fe = li.fe; var fd = li.fd;
        RenderSettings.ambientMode = mode; RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = eq;
        RenderSettings.ambientGroundColor = gr; RenderSettings.ambientLight = amb; RenderSettings.ambientIntensity = ai;
        RenderSettings.skybox = box; RenderSettings.fog = fog; RenderSettings.fogColor = fogC; RenderSettings.fogMode = fogM;
        RenderSettings.fogStartDistance = fs; RenderSettings.fogEndDistance = fe; RenderSettings.fogDensity = fd;
        var light = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).FirstOrDefault(l => l.type == LightType.Directional);
        if (light && li.hasLight)
        {
            light.color = li.lColor; light.intensity = li.lIntensity; light.shadows = li.lShadows;
            light.shadowStrength = li.lShadowStrength; light.transform.rotation = li.lRotation;
        }
        if (theme == Theme.Ice || theme == Theme.Xmas) IceLook(light);
        if (theme == Theme.Volcano) VolcanoLook(light);
        if (theme == Theme.Egypt || theme == Theme.Factory || theme == Theme.Circus) EgyptLook(light);
        if (theme == Theme.Greek) GreekLook(light);
        if (theme == Theme.Candy) { GreekLook(light); CandySky(); }
        if (theme == Theme.Ocean) OceanLook(light);
        if (theme == Theme.Savanna) { EgyptLook(light); SavannaSky(); }
        if (theme == Theme.Japan) { GreekLook(light); JapanSky(); }
        if (theme == Theme.Lunar) LunarLook(light);
        if (theme == Theme.Swamp) SwampLook(light);
    }

    /// <summary>Western lighting, cooled down: blue-white ambient and fog, a slightly warm sun.</summary>
    static void IceLook(Light light)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.78f, 0.86f, 1.0f); RenderSettings.ambientEquatorColor = new Color(0.70f, 0.78f, 0.92f); RenderSettings.ambientGroundColor = new Color(0.58f, 0.64f, 0.78f);
        RenderSettings.fogColor = new Color(0.82f, 0.90f, 1.0f);
        if (light) { light.color = new Color(1.0f, 0.97f, 0.92f); }
    }

    static readonly string[] Desert = { "cliff01", "cliff02", "cliff03", "cliff04" };
    static readonly string[] Green = { "Cliff09", "Cliff10", "Cliff11", "Cliff12" };
    static readonly string[] CaveC = { "Cliff5", "Cliff8", "Cliff7", "Cliff6" };

    static void PrepareBorderAndGround(Theme theme)
    {
        bool keepGates = theme == Theme.Western || theme == Theme.Bamboo || theme == Theme.Mix;
        var sdCliffMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Maps/Brawl_ShowDown/Materials/SD_Wall_Tile_Block_18.mat");
        int idx = 0;
        foreach (Transform t in env.Cast<Transform>().ToList())
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            string n = src ? src.name : t.name;
            int ci = Array.IndexOf(Desert, n);
            if (ci >= 0 && (theme == Theme.Ice || theme == Theme.Xmas || theme == Theme.Egypt || theme == Theme.Volcano || theme == Theme.Factory || theme == Theme.Circus || theme == Theme.Lunar || theme == Theme.Swamp || theme == Theme.Greek || theme == Theme.Candy || theme == Theme.Ocean || theme == Theme.Savanna || theme == Theme.Japan))   // Xmas keeps the glacier border
            {
                // themed border: one of three cliffs of the theme, on the same spot, turn and scale as the old cliff
                // (Ice / Xmas: glacier walls; Egypt: layered sandstone cliffs, one with a carved temple front)
                string gname = theme == Theme.Egypt && prefabs.ContainsKey("SandCliffA") ? "SandCliff" + "ABC"[idx % 3]
                             : theme == Theme.Volcano && prefabs.ContainsKey("BasaltRimA") ? "BasaltRim" + "ABC"[idx % 3]
                             : theme == Theme.Factory && prefabs.ContainsKey("FactoryWallA") ? "FactoryWall" + "ABC"[idx % 3]
                             : theme == Theme.Circus && prefabs.ContainsKey("CircusWallA") ? "CircusWall" + "ABC"[idx % 3]
                             : theme == Theme.Lunar && prefabs.ContainsKey("CraterWallA") ? "CraterWall" + "ABC"[idx % 3]
                             : theme == Theme.Swamp && prefabs.ContainsKey("SwampBankA") ? "SwampBank" + "ABC"[idx % 3]
                             : theme == Theme.Greek && prefabs.ContainsKey("GreekCliffA") ? "GreekCliff" + "ABC"[idx % 3]
                             : theme == Theme.Candy && prefabs.ContainsKey("CandyWallA") ? "CandyWall" + "ABC"[idx % 3]
                             : theme == Theme.Ocean && prefabs.ContainsKey("ReefWallA") ? "ReefWall" + "ABC"[idx % 3]
                             : theme == Theme.Savanna && prefabs.ContainsKey("SavannaRidgeA") ? "SavannaRidge" + "ABC"[idx % 3]
                             : theme == Theme.Japan && prefabs.ContainsKey("ShrineWallA") ? "ShrineWall" + "ABC"[idx % 3]
                             : prefabs.ContainsKey("GlacierWallA") ? "GlacierWall" + "ABC"[idx % 3] : "IceCliff" + "ABC"[idx % 3];
                var ng = Prefab(gname, t.position, 0, 1, true);
                if (ng)
                {
                    ng.transform.rotation = t.rotation;
                    // the glacier is ~10 m long against the old cliffs' 7.4 m; the sandstone cliff is built at 7.6 m
                    ng.transform.localScale = t.localScale * (gname.StartsWith("Glacier") ? 0.74f : 1f);
                    if (gname.StartsWith("Glacier") || gname.StartsWith("SandCliff") || gname.StartsWith("BasaltRim") || gname.StartsWith("FactoryWall") || gname.StartsWith("CircusWall") || gname.StartsWith("CraterWall") || gname.StartsWith("SwampBank") || gname.StartsWith("GreekCliff") || gname.StartsWith("CandyWall") || gname.StartsWith("ReefWall") || gname.StartsWith("SavannaRidge") || gname.StartsWith("ShrineWall"))
                    {
                        // its detailed face (columns and fallen ice; Blender -Y) imports as local +Z: turn it to the arena
                        Vector3 front = ng.transform.forward; front.y = 0f;
                        Vector3 toArena = -ng.transform.position; toArena.y = 0f;
                        if (Vector3.Dot(front, toArena) < 0f) ng.transform.Rotate(0f, 180f, 0f, Space.World);
                    }
                    if (gname.StartsWith("BasaltRim") || gname.StartsWith("CircusWall") || gname.StartsWith("CraterWall") || gname.StartsWith("SwampBank") || gname.StartsWith("GreekCliff") || gname.StartsWith("CandyWall") || gname.StartsWith("ReefWall") || gname.StartsWith("SavannaRidge") || gname.StartsWith("ShrineWall"))
                    {   // a low rim on the ground (the lava lake behind it shows over it), not stretched to the old cliff
                        var p = ng.transform.position; ng.transform.position = new Vector3(p.x, 0f, p.z);
                        var sc = ng.transform.localScale; ng.transform.localScale = new Vector3(sc.x, 1f, sc.z);
                    }
                    if (gname.StartsWith("SandCliff") || gname.StartsWith("FactoryWall"))
                    {
                        // the old cliffs are sunk into the ground by different depths; this one stands on the ground
                        // (its carved temple front must not end up buried) and is stretched to the old cliff's top
                        var orb = t.GetComponentsInChildren<Renderer>(); float oldTop = orb.Length > 0 ? orb.Max(r => r.bounds.max.y) : 6f;
                        var p = ng.transform.position; ng.transform.position = new Vector3(p.x, 0f, p.z);
                        var sc = ng.transform.localScale; ng.transform.localScale = new Vector3(sc.x, 1f, sc.z);
                        Physics.SyncTransforms();
                        var nrb = ng.GetComponentsInChildren<Renderer>(); float h = nrb.Length > 0 ? nrb.Max(r => r.bounds.max.y) : 6f;
                        ng.transform.localScale = new Vector3(sc.x, Mathf.Clamp(oldTop / Mathf.Max(h, 0.1f), 0.6f, 2.0f), sc.z);
                    }
                }
                idx++;
                Object.DestroyImmediate(t.gameObject);
                continue;
            }
            if (ci >= 0)
            {
                string[] set = theme switch
                {
                    Theme.Bamboo => Green,
                    Theme.Cave => CaveC,
                    Theme.Mix => (idx % 3 == 0 ? Green : idx % 3 == 1 ? CaveC : Desert),
                    _ => Desert,
                };
                idx++;
                Material m = theme == Theme.Moon ? sdCliffMat : (theme == Theme.Ice && IceThemeSetup.Cliff ? IceThemeSetup.Cliff : world);
                if (set[ci] != n || theme == Theme.Moon || theme == Theme.Ice)
                {
                    var ng = Prefab(set[ci], t.position, 0, 1, true, m);
                    ng.transform.rotation = t.rotation; ng.transform.localScale = t.localScale;
                    Object.DestroyImmediate(t.gameObject);
                }
                continue;
            }
            if (n == "Gate" && keepGates) continue;
            Object.DestroyImmediate(t.gameObject);
        }

        switch (theme)
        {
            case Theme.Western:
            case Theme.Mix:
                Prefab("Floor1", Vector3.zero, 0, 1, true, worldT, new Vector3(50, 1, 50));
                break;
            case Theme.Bamboo:
                Prefab("SolGrass01", Vector3.zero, 0, 1, true, worldT, new Vector3(10, 1, 10));
                break;
            case Theme.Cave:
                Prefab("Floor2", Vector3.zero, 0, 1, true, world, new Vector3(75, 1, 75));
                break;
            case Theme.Japan:
                if (prefabs.ContainsKey("JapanFloor")) Prefab("JapanFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, world, new Vector3(50, 1, 50));
                break;
            case Theme.Savanna:
                if (prefabs.ContainsKey("SavannaFloor")) Prefab("SavannaFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, world, new Vector3(50, 1, 50));
                break;
            case Theme.Ocean:
                if (prefabs.ContainsKey("SeabedFloor")) Prefab("SeabedFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, world, new Vector3(50, 1, 50));
                break;
            case Theme.Candy:
                if (prefabs.ContainsKey("CandyFloor")) Prefab("CandyFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, world, new Vector3(50, 1, 50));
                break;
            case Theme.Greek:
                if (prefabs.ContainsKey("GreekFloor")) Prefab("GreekFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, world, new Vector3(50, 1, 50));
                break;
            case Theme.Swamp:
                if (prefabs.ContainsKey("SwampFloor")) Prefab("SwampFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, world, new Vector3(50, 1, 50));
                break;
            case Theme.Lunar:
                if (prefabs.ContainsKey("MoonFloor")) Prefab("MoonFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, world, new Vector3(50, 1, 50));
                break;
            case Theme.Circus:
                if (prefabs.ContainsKey("FairFloor")) Prefab("FairFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, world, new Vector3(50, 1, 50));
                break;
            case Theme.Factory:
                if (prefabs.ContainsKey("YardFloor")) Prefab("YardFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, world, new Vector3(50, 1, 50));
                break;
            case Theme.Volcano:
                if (prefabs.ContainsKey("BasaltFloor")) Prefab("BasaltFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, world, new Vector3(50, 1, 50));
                break;
            case Theme.Egypt:
                if (prefabs.ContainsKey("SandFloor")) Prefab("SandFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, world, new Vector3(50, 1, 50));
                break;
            case Theme.Xmas:
                if (prefabs.ContainsKey("SnowFloor")) Prefab("SnowFloor", Vector3.zero, 0, 1, true);
                else if (prefabs.ContainsKey("IceFloor")) Prefab("IceFloor", Vector3.zero, 0, 1, true);
                break;
            case Theme.Ice:
                if (prefabs.ContainsKey("IceFloor")) Prefab("IceFloor", Vector3.zero, 0, 1, true);
                else Prefab("Floor1", Vector3.zero, 0, 1, true, IceThemeSetup.Ground ? IceThemeSetup.Ground : worldT, new Vector3(50, 1, 50));
                break;
            case Theme.Moon:
                var plane = CloneMoon("Night_Plane", Vector3.zero, 0, Vector3.one * 0.6f, false);
                plane.AddComponent<MeshCollider>();
                Physics.SyncTransforms();
                if (Physics.Raycast(new Vector3(0, 20, 0), Vector3.down, out var hit, 40f))
                    plane.transform.position += Vector3.up * (-hit.point.y);
                break;
        }
    }

    /// <summary>Arctic scenery outside the play border: pines, igloos, tents, bushes, rocks (no colliders).</summary>
    static void PlaceIceBackdrop(int seed)
    {
        PlaceGates("IceGate");   // a natural glacier arch behind each spawn
        var rnd = new Random(seed);
        for (int i = 0; i < 26; i++)
        {
            float a = i / 26f * Mathf.PI * 2f + Jit(rnd, 0.06f);
            float rad = 33f + (float)rnd.NextDouble() * 7f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.85f, 0f, Mathf.Sin(a) * rad);
            // wild arctic beyond the border: more glacier, seracs, boulders (no man-made props)
            var k = Pick(rnd, "GlacierWallA", "GlacierWallB", "GlacierWallC", "IceSpire", "IceSpire", "IceBoulder", "SnowRock", "IceCrystals", "IceShardCluster", "RibCage");
            if (!prefabs.ContainsKey(k)) k = "IceSpire";
            float sc = k.StartsWith("Glacier") ? 0.9f + (float)rnd.NextDouble() * 0.5f : k == "IceSpire" ? 1.3f + (float)rnd.NextDouble() * 0.7f : k == "IceBoulder" ? 1.5f + (float)rnd.NextDouble() * 0.8f : k == "SnowRock" ? 1.2f + (float)rnd.NextDouble() * 0.6f
                     : k == "IceShardCluster" ? 1.4f + (float)rnd.NextDouble() * 0.5f : k == "RibCage" ? 1.3f : 1.3f;
            // glacier front (local +Z) looks back at the arena centre
            float yaw = k.StartsWith("Glacier") ? Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg + Jit(rnd, 15) : rnd.Next(360);
            Prefab(k, p, yaw, sc, false);
        }
    }

    /// <summary>Ice plates and shards scattered thinly over the open floor (no colliders), so the tiling floor texture
    /// does not read as a pattern. Skips spots near walls, props and spawns.</summary>
    static void ScatterIceFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        for (int i = 0; i < 140 && placed < 26; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)   /* clear of the floor collider */) continue;
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            var k = Pick(rnd, "IceSlab", "IceHole", "IceShards", "IceSlab", "BonePile", "IceShards", "IceSlab");
            if (!prefabs.ContainsKey(k)) k = "IceSlab";
            Prefab(k, p, rnd.Next(360), k == "IceSlab" ? 0.8f + (float)rnd.NextDouble() * 0.6f : k == "IceHole" ? 0.75f : k == "BonePile" ? 0.95f : 0.8f, false);
            placed++;
        }
        log += " iceScatter=" + placed;
        // a few big remains (rib cage, beast skull) on open ground: clear of walls, spawns and bushes (they have no collider)
        var grass = env.Cast<Transform>().Where(t => t.name.StartsWith("FrostGrass")).Select(t => t.position).ToList();
        int big = 0;
        for (int i = 0; i < 200 && big < 4; i++)
        {
            var p = new Vector3(Jit(rnd, 13f), 0f, Jit(rnd, 18f));
            if (Physics.CheckSphere(p + Vector3.up * 1.6f, 1.4f, ~0, QueryTriggerInteraction.Ignore)) continue;   // bottom at 0.2 m: clear of the floor collider
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 16f)) continue;
            if (grass.Any(g => (g - p).sqrMagnitude < 4f)) continue;
            string k = big % 2 == 0 ? "RibCage" : "BeastSkull";
            if (!prefabs.ContainsKey(k)) break;
            Prefab(k, p, rnd.Next(360), k == "RibCage" ? 0.85f : 0.9f, false);
            big++;
        }
        log += " bigBones=" + big;
    }

    static void PlaceMoonBackdrop(int seed)
    {
        var rnd = new Random(seed);
        for (int i = 0; i < 18; i++)
        {
            float a = i / 18f * Mathf.PI * 2f;
            float rad = 33f + (float)rnd.NextDouble() * 6f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.85f, -0.5f, Mathf.Sin(a) * rad);
            CloneMoon("PolyPyramid", p, rnd.Next(360), Vector3.one * (0.9f + (float)rnd.NextDouble() * 0.6f), false);
        }
    }

    // ------------------------------------------------------------------ grid
    static char[,] FullGrid(Def d)
    {
        var g = new char[ROWS, COLS];
        for (int r = 0; r < 13; r++)
            for (int c = 0; c < COLS; c++)
            {
                g[r, c] = d.half[r][c];
                g[ROWS - 1 - r, COLS - 1 - c] = d.half[r][c];
            }
        for (int c = 0; c < COLS; c++) g[13, c] = d.center[c];
        return g;
    }

    static Vector3 TilePos(int r, int c) => new Vector3((c - 10) * T, 0, (r - 13) * T);

    static void Canon(int r, int c, out int cr, out int cc, out bool mirrored)
    {
        if (r < 13) { cr = r; cc = c; mirrored = false; }
        else if (r > 13) { cr = ROWS - 1 - r; cc = COLS - 1 - c; mirrored = true; }
        else { cr = 13; cc = c <= 10 ? c : COLS - 1 - c; mirrored = c > 10; }
    }

    static bool Blocked(Vector3 p)
    {
        foreach (var s in spawnPoints) { var d = s - p; d.y = 0; if (d.magnitude < 1.6f) return true; }
        var hits = Physics.OverlapBox(p + Vector3.up * 1.2f, new Vector3(0.7f, 0.8f, 0.7f), Quaternion.identity, 1 << WallLayer, QueryTriggerInteraction.Ignore);
        return hits.Length > 0;
    }

    static void PlaceGrid(Def d)
    {
        var g = FullGrid(d);
        // which tiles are inside the cliffs: test before placing anything (only the border + gates exist now)
        var blocked = new bool[ROWS, COLS];
        for (int r = 0; r < ROWS; r++) for (int c = 0; c < COLS; c++) blocked[r, c] = Blocked(TilePos(r, c)) || NearPath(TilePos(r, c));
        int placed = 0, skipped = 0, decor = 0;
        for (int r = 0; r < ROWS; r++)
            for (int c = 0; c < COLS; c++)
            {
                Canon(r, c, out int cr, out int cc, out bool mir);
                // both mirrored tiles must be free, otherwise skip both (keeps symmetry)
                int mr = ROWS - 1 - r, mc = COLS - 1 - c;
                bool free = !blocked[r, c] && !blocked[mr, mc];
                var rnd = new Random(d.seed * 7919 + cr * 131 + cc);
                float baseRot = mir ? 180f : 0f;
                char ch = g[r, c];
                var p = TilePos(r, c);
                if (ch == '.')
                {
                    if (!free) continue;
                    if (xmasNight && NightLamp(g, r, c, cr, cc, d.seed, p)) { placed++; continue; }   // Xmas night: extra lamps along the walls
                    if (rnd.NextDouble() < (d.theme == Theme.Ice || d.theme == Theme.Xmas || d.theme == Theme.Egypt || d.theme == Theme.Volcano || d.theme == Theme.Factory || d.theme == Theme.Circus || d.theme == Theme.Lunar || d.theme == Theme.Swamp || d.theme == Theme.Greek || d.theme == Theme.Candy || d.theme == Theme.Ocean || d.theme == Theme.Savanna || d.theme == Theme.Japan ? 0.09 : 0.06)) { Decor(d.theme, p, baseRot, rnd); decor++; }
                    continue;
                }
                if (!free) { skipped++; continue; }
                bool alongX = !(c > 0 && g[r, c - 1] == ch || c < COLS - 1 && g[r, c + 1] == ch) && (r > 0 && g[r - 1, c] == ch || r < ROWS - 1 && g[r + 1, c] == ch) ? false : true;
                Tile(d.theme, ch, p, baseRot, alongX, rnd);
                placed++;
            }
        log += " tiles=" + placed + " skipped=" + skipped + " decor=" + decor;
    }

    static void Tile(Theme th, char ch, Vector3 p, float baseRot, bool alongX, Random rnd)
    {
        float fenceRot = (alongX ? 0f : 90f) + baseRot;
        switch (th)
        {
            case Theme.Western:
                switch (ch)
                {
                    case '#': if (rnd.NextDouble() < 0.7) Prefab("Mudwall03", p, Rot90(rnd) + baseRot, 0.62f, true); else Prefab("Box", p, Rot90(rnd) + baseRot, 0.74f, true); break;
                    case 'x': if (rnd.NextDouble() < 0.5) Prefab("Barrel01", p, rnd.Next(360), 0.42f, true); else Prefab("Box", p, rnd.Next(4) * 90 + 15, 0.6f, true); break;
                    case 'f': Prefab("Fence02", p, fenceRot, 0.6f, true); break;
                    case 't': if (rnd.NextDouble() < 0.5) Prefab("Cactus2", p, rnd.Next(360), 0.7f, true); else Prefab("Pilar3", p, baseRot, 1f, true); break;
                    case 'b': Grass("Grass01", p, 1.25f, rnd); break;
                }
                break;
            case Theme.Bamboo:
                switch (ch)
                {
                    case '#': Prefab(Pick(rnd, "Stone04", "Stone02"), p, rnd.Next(360), 0.55f, true); break;
                    case 'x': if (rnd.NextDouble() < 0.6) Prefab("Box02", p, Rot90(rnd) + 10, 0.62f, true); else Prefab("Barrel02", p, rnd.Next(360), 0.42f, true); break;
                    case 'f': Prefab("Fence03", p, fenceRot, 0.68f, true); break;
                    case 't': BambooCluster(p, rnd, true); break;
                    case 'b': Grass("Grass03", p, 1.0f, rnd); break;
                }
                break;
            case Theme.Cave:
                switch (ch)
                {
                    case '#': Prefab(Pick(rnd, "Wall4", "Wall5", "Wall3"), p, Rot90(rnd) + Jit(rnd, 8), 1.2f, true, null, new Vector3(1.25f, 1.9f, 1.25f)); break;
                    case 'x': if (rnd.NextDouble() < 0.6) Prefab("Crystal4", p, rnd.Next(360), 1.0f, true); else Prefab("Chariot", p, Rot90(rnd), 0.5f, true); break;
                    case 'f': Prefab("Plank4", p, fenceRot, 0.42f, true); break;
                    case 't':
                        if (rnd.NextDouble() < 0.5) Prefab("Mushroom", p, rnd.Next(360), 0.9f, true);
                        else { Prefab("Crystal1", p + new Vector3(-0.3f, 0, 0.1f), rnd.Next(360), 1.2f, true); Prefab("Crystal2", p + new Vector3(0.35f, 0, -0.2f), rnd.Next(360), 1.0f, true); Prefab("Crystal3", p + new Vector3(0.1f, 0, 0.35f), rnd.Next(360), 0.9f, true); }
                        break;
                    case 'b': Grass("Grass02", p, 1.4f, rnd); break;
                }
                break;
            case Theme.Moon:
                {
                    float s = 1.5f / 2.13f;
                    switch (ch)
                    {
                        case '#': CloneMoon("Wall_Tile_Block1", p, Rot90(rnd), Vector3.one * s, true); break;
                        case 'x': if (moon.ContainsKey("Chest")) CloneMoon("Chest", p, Rot90(rnd) + baseRot, Vector3.one * 0.68f, true); else CloneMoon("Wall_Tile_Block1", p, 0, new Vector3(s, s * 0.6f, s), true); break;
                        case 'f': CloneMoon("Wall_Tile_Block1", p, Rot90(rnd), new Vector3(s, s * 0.5f, s), true); break;
                        case 't': CloneMoon("Wall_Tile_Block1", p, Rot90(rnd), new Vector3(s, s * 1.6f, s), true); break;
                        case 'b': var gobj = CloneMoon("Grass_Tile_A_opt", p, Rot90(rnd), Vector3.one * (1.5f / 2.27f), false); gobj.name = "Grass_Tile_Night"; break;
                    }
                }
                break;
            case Theme.Xmas: XmasTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Egypt: EgyptTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Volcano: VolcanoTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Factory: FactoryTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Circus: CircusTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Lunar: LunarTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Swamp: SwampTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Greek: GreekTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Candy: CandyTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Ocean: OceanTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Savanna: SavannaTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Japan: JapanTile(ch, p, baseRot, fenceRot, rnd); break;
            case Theme.Ice:
                switch (ch)
                {
                    // ice age: natural pieces only (the man-made snow props are kept for the Xmas theme)
                    case '#':
                        {
                            // four block shapes, turned and barely nudged: a wall reads as one tidy row, not a jumble
                            string k = prefabs.ContainsKey("GlacierBlockA") ? Pick(rnd, "GlacierBlockA", "GlacierBlockB", "GlacierBlockC", "GlacierBlockD") : "GlacierBlock";
                            float s = 0.97f + (float)rnd.NextDouble() * 0.03f, hy = 0.96f + (float)rnd.NextDouble() * 0.08f;
                            Prefab(k, p + new Vector3(Jit(rnd, 0.03f), 0, Jit(rnd, 0.03f)), Rot90(rnd) + baseRot + Jit(rnd, 3), s, true, null, new Vector3(s, s * hy, s));
                        }
                        break;
                    case 'x':
                        {
                            // small ice blocks (same style as the walls), a log frozen in ice, boulders
                            double v = MixRnd(rnd).NextDouble();
                            if (v < 0.25 && prefabs.ContainsKey("IceChunkA")) Prefab("IceChunkA", p, Rot90(rnd) + Jit(rnd, 4), 1f, true);
                            else if (v < 0.50 && prefabs.ContainsKey("IceChunkB")) Prefab("IceChunkB", p, Rot90(rnd) + Jit(rnd, 4), 1f, true);
                            else if (v < 0.75 && prefabs.ContainsKey("FrozenLog")) Prefab("FrozenLog", p, Rot90(rnd) + Jit(rnd, 4), 1f, true);
                            else if (v < 0.92) Prefab("IceBoulder", p, rnd.Next(360), 0.92f, true);
                            else Prefab("SnowRock", p, rnd.Next(360), 0.52f, true);
                        }
                        break;
                    case 'f': Prefab("PressureRidge", p, fenceRot + Jit(rnd, 6), 0.72f, true); break;
                    case 't':
                        {
                            double v = MixRnd(rnd).NextDouble();
                            if (v < 0.4) Prefab("IceSpire", p, rnd.Next(360), 0.8f, true);
                            else if (v < 0.75 && prefabs.ContainsKey("IceShardCluster")) Prefab("IceShardCluster", p, rnd.Next(360), 0.9f, true);
                            else Prefab("IceCrystals", p, rnd.Next(360), 0.85f, true);
                        }
                        break;
                    case 'b':   // Brawl-style frost grass to hide in (falls back to a snow drift without the model)
                        if (prefabs.ContainsKey("FrostGrass")) Grass("FrostGrass", p, 1.3f, rnd);   // ~1.9 m after GrassHeight (its stretch is capped at x1.7), like the other maps' bushes
                        else if (rnd.NextDouble() < 0.35) Prefab("SnowDrift", p, rnd.Next(360), 0.55f, false);
                        break;
                }
                break;
            case Theme.Mix:
                switch (ch)
                {
                    case '#':
                        double v = rnd.NextDouble();
                        if (v < 0.4) Prefab(Pick(rnd, "Pilar2", "Pilar3", "Pilar4"), p, Rot90(rnd) + baseRot, 1f, true);
                        else if (v < 0.7) Prefab("Stone04", p, rnd.Next(360), 0.55f, true);
                        else Prefab("Mudwall03", p, Rot90(rnd), 0.62f, true);
                        break;
                    case 'x': { var k = Pick(rnd, "Crystal4", "Box", "Barrel02"); Prefab(k, p, rnd.Next(360), k == "Crystal4" ? 1f : k == "Box" ? 0.65f : 0.42f, true); } break;
                    case 'f': Prefab("Fence02", p, fenceRot, 0.6f, true); break;
                    case 't': BambooCluster(p, rnd, true); break;
                    case 'b': Grass("Grass03", p, 1.0f, rnd); break;
                }
                break;
        }
    }

    static void Grass(string prefab, Vector3 p, float s, Random rnd)
    {
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
            {
                var o = new Vector3((i - 0.5f) * 0.74f + Jit(rnd, 0.08f), 0, (j - 0.5f) * 0.74f + Jit(rnd, 0.08f));
                Prefab(prefab, p + o, rnd.Next(360), s * (0.92f + (float)rnd.NextDouble() * 0.16f), false);
            }
    }

    static void BambooCluster(Vector3 p, Random rnd, bool col)
    {
        Prefab("Bamboo04", p + new Vector3(-0.25f, 0, -0.2f), rnd.Next(360), 0.38f, col);
        Prefab("Bamboo06", p + new Vector3(0.3f, 0, 0.1f), rnd.Next(360), 0.36f, col);
        Prefab("Bamboo05", p + new Vector3(-0.1f, 0, 0.35f), rnd.Next(360), 0.35f, col);
        Prefab("Grass03", p + new Vector3(0.25f, 0, -0.3f), rnd.Next(360), 0.6f, false);
    }

    static void Decor(Theme th, Vector3 p, float baseRot, Random rnd)
    {
        var o = p + new Vector3(Jit(rnd, 0.4f), 0, Jit(rnd, 0.4f));
        float rot = rnd.Next(360) + baseRot;
        switch (th)
        {
            case Theme.Western: Prefab(Pick(rnd, "rock1", "rock2", "rock4", "rock6", "rock8", "Bone1", "Flower01"), o, rot, 0.8f, false); break;
            case Theme.Bamboo: Prefab(Pick(rnd, "Flower02", "Flower03", "Flower04", "Stone05", "Stone07", "Stone08"), o, rot, 0.6f, false); break;
            case Theme.Cave: { var dn = Pick(rnd, "Rock9", "Rock10", "Rock13", "Rock14", "Rock15", "Rail1"); if (dn == "Rail1" && pathSamples != null) dn = "Rock13"; /* no loose rail pieces next to a real track */ Prefab(dn, o, rot, 0.8f, false); break; }
            case Theme.Moon:
                {
                    var k = Pick(rnd, "RockFlat3", "RockFlat5", "RockFlat6", "Skull_opt", "Bone_opt");
                    if (moon.ContainsKey(k)) CloneMoon(k, o, rot, Vector3.one * 0.6f, false);
                }
                break;
            case Theme.Mix: Prefab(Pick(rnd, "Flower02", "rock8", "Rock13", "Bone1", "Flower04", "Stone08"), o, rot, 0.7f, false); break;
            case Theme.Xmas: XmasDecor(o, rot, rnd); break;
            case Theme.Egypt: EgyptDecor(o, rot, rnd); break;
            case Theme.Volcano: VolcanoDecor(o, rot, rnd); break;
            case Theme.Factory: FactoryDecor(o, rot, rnd); break;
            case Theme.Circus: CircusDecor(o, rot, rnd); break;
            case Theme.Lunar: LunarDecor(o, rot, rnd); break;
            case Theme.Swamp: SwampDecor(o, rot, rnd); break;
            case Theme.Greek: GreekDecor(o, rot, rnd); break;
            case Theme.Candy: CandyDecor(o, rot, rnd); break;
            case Theme.Ocean: OceanDecor(o, rot, rnd); break;
            case Theme.Savanna: SavannaDecor(o, rot, rnd); break;
            case Theme.Japan: JapanDecor(o, rot, rnd); break;
            case Theme.Ice:
                {
                    rnd = MixRnd(rnd);   // decorrelated from the neighbouring tiles' picks
                    var k = Pick(rnd, "IceSlab", "BonePile", "SnowDrift", "IceHole", "IceShards", "MammothBones", "IceShards", "SnowRock", "BonePile", "IceCrystals");
                    if (!prefabs.ContainsKey(k)) k = "IceSlab";
                    float sc = k == "IceSlab" ? 0.9f + (float)rnd.NextDouble() * 0.4f : k == "SnowDrift" ? 0.6f : k == "IceShards" ? 1.0f : k == "SnowRock" ? 0.28f
                             : k == "IceCrystals" ? 0.32f : k == "BonePile" ? 0.9f + (float)rnd.NextDouble() * 0.2f : k == "IceHole" ? 0.75f : 0.75f;
                    Prefab(k, o, rot, sc, false);
                }
                break;
        }
    }

    // ------------------------------------------------------------------ navmesh + path points
    static NavMeshData BakeNavMesh(string id)
    {
        // the generated maps call it NavmeshSurface; the hand-made source maps (Map_Brawl_Payload, Map_Brawl_MvM) NavMeshSurface
        var navGo = scene.GetRootGameObjects().First(g => g.name == "NavmeshSurface" || g.name == "NavMeshSurface");
        navGo.transform.position = Vector3.zero; navGo.transform.rotation = Quaternion.identity;
        var surf = navGo.GetComponents<Component>().First(c => c != null && c.GetType().Name == "NavMeshSurface");
        var so = new SerializedObject(surf);
        int mask = so.FindProperty("m_LayerMask").intValue;
        var sources = new List<NavMeshBuildSource>();
        var markups = new List<NavMeshBuildMarkup>();
        // bushes (grass / bamboo clumps) are walk-through like in Brawl Stars: never let them cut holes in the NavMesh
        foreach (var rr in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
            if (BushAreaGenerator.IsBushRenderer(rr)) markups.Add(new NavMeshBuildMarkup { root = rr.transform, ignoreFromBuild = true });
        // Volcano: the lava lakes and the eruption vents (fountain, warning disc) are not ground to walk on or obstacles
        foreach (var t in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
            if (t.name.StartsWith("LavaLake") || t.GetComponent<EruptionVent>() || t.name.StartsWith("AcidLake") || t.name.StartsWith("SeaWater") || t.name.StartsWith("ChocoLake") || t.name.StartsWith("Caustics") || t.GetComponent<OceanCurrent>() || t.GetComponent<WaterHole>() || t.GetComponent<ToriiPath>() || t.name.StartsWith("GlowPool") || t.name.StartsWith("WarmPool") || t.GetComponent<BogPuddle>()) markups.Add(new NavMeshBuildMarkup { root = t, ignoreFromBuild = true });
        foreach (var root in scene.GetRootGameObjects())
        {
            var tmp = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(root.transform, mask, NavMeshCollectGeometry.RenderMeshes, 0, markups, tmp);
            sources.AddRange(tmp);
        }
        var settings = NavMesh.GetSettingsByID(so.FindProperty("m_AgentTypeID").intValue);
        var built = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(Vector3.zero, new Vector3(110, 40, 110)), Vector3.zero, Quaternion.identity);
        string dir = "Assets/Scenes/" + id;
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Scenes", id);
        string ap = dir + "/NavMesh-NavmeshSurface.asset";
        var asset = AssetDatabase.LoadAssetAtPath<NavMeshData>(ap);
        if (asset) { EditorUtility.CopySerialized(built, asset); asset.name = "NavMesh-NavmeshSurface"; EditorUtility.SetDirty(asset); }
        else { built.name = "NavMesh-NavmeshSurface"; AssetDatabase.CreateAsset(built, ap); asset = built; }
        so.FindProperty("m_NavMeshData").objectReferenceValue = asset;
        so.ApplyModifiedPropertiesWithoutUndo();
        return asset;
    }

    /// <summary>Rebakes the NavMesh (bushes walkable) and the bush patrol points of an existing map scene,
    /// opened alone (Single). Does not touch the layout. Use for maps not rebuilt by Build().</summary>
    public static string RebakeNavMesh(string id)
    {
        GuardUnsavedScenes();
        scene = EditorSceneManager.OpenScene("Assets/Scenes/" + id + ".unity", OpenSceneMode.Single);
        string r = RebakeOpenScene(id);
        EditorSceneManager.SaveScene(scene);
        return id + ": " + r;
    }

    static void FixPathPoints(Def d, NavMeshData data)
    {
        NavMesh.RemoveAllNavMeshData();
        var inst = NavMesh.AddNavMeshData(data);
        try
        {
            var g = FullGrid(d);
            var pts = new List<Vector3>();
            int[] rows = { 4, 7, 10, 13, 16, 19, 22 };
            int[] cols = { 3, 6, 10, 14, 17 };
            foreach (var r in rows)
                foreach (var c in cols)
                {
                    if (g[r, c] != '.') continue;
                    var p = TilePos(r, c);
                    if (Physics.CheckSphere(p + Vector3.up * 1.2f, 0.8f, 1 << WallLayer, QueryTriggerInteraction.Ignore)) continue;
                    if (NavMesh.SamplePosition(p, out var hit, 0.5f, NavMesh.AllAreas)) pts.Add(new Vector3(hit.position.x, 0, hit.position.z));
                }
            var root = Root("PathPoints").transform;
            var template = root.GetChild(0).gameObject;
            var list = new List<Transform>();
            for (int i = 0; i < pts.Count; i++)
            {
                Transform t = i < root.childCount ? root.GetChild(i) : Object.Instantiate(template, root).transform;
                t.name = i == 0 ? "Point" : "Point (" + i + ")";
                t.position = pts[i];
                list.Add(t);
            }
            for (int i = root.childCount - 1; i >= pts.Count; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
            var mm = Root("MapManager").GetComponent<MapManager>();
            mm.PathPoints = list.ToArray();
            EditorUtility.SetDirty(mm);

            var path = new NavMeshPath();
            NavMesh.CalculatePath(new Vector3(0, 0, -18.5f), new Vector3(0, 0, 18.5f), NavMesh.AllAreas, path);
            int offSpawn = spawnPoints.Count(s => !NavMesh.SamplePosition(s, out _, 0.6f, NavMesh.AllAreas));
            log += " pathPoints=" + pts.Count + " redToBlue=" + path.status + " spawnsOffNav=" + offSpawn + " tris=" + NavMesh.CalculateTriangulation().indices.Length / 3;
        }
        finally { NavMesh.RemoveNavMeshData(inst); }
    }

    // ------------------------------------------------------------------ icon
    /// <summary>Re-renders the map-select icon of the maps changed in Nov 2026 (every Payload map: new rail track; Egypt:
    /// sunlight; Factory and Circus: rebuilt after their first icon) without rebuilding them.
    /// Run it after a Unity restart when a build warned "locked, old image kept" (Unity keeps imported icons mapped).</summary>
    [MenuItem("Tools/Maps/Refresh Recent Map Icons")]
    public static void RefreshPayloadIcons()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        GuardUnsavedScenes();
        foreach (var d in AllDefs().Where(x => x.mode == GameMode.Payload || x.theme == Theme.Egypt || x.theme == Theme.Factory || x.theme == Theme.Circus))
        {
            string path = "Assets/Scenes/" + d.mapEnum + ".unity";
            if (!File.Exists(path)) continue;
            log = "";
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            RenderIcon(d);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MapBuilder] icon " + d.icon + log);
        }
    }

    static void RenderIcon(Def d)
    {
        // new themes: hero shot + per-map grading (MapIconStudio, Assets/Editor/MapBuilder/MapIconGrades.json)
        if (d.theme >= Theme.Ice) { MapIconStudio.RenderAndGrade(d); return; }
        var go = new GameObject("_iconcam");
        SceneManager.MoveGameObjectToScene(go, scene);
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 40; cam.farClipPlane = 300;
        cam.transform.position = new Vector3(0, 21, -25);
        cam.transform.LookAt(new Vector3(0, 0, -1.5f));
        var rt = new RenderTexture(800, 800, 24) { antiAliasing = 8 };
        cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(800, 800, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 800, 800), 0, 0); tex.Apply();
        RenderTexture.active = null; cam.targetTexture = null;
        Object.DestroyImmediate(rt); Object.DestroyImmediate(go);

        var px = tex.GetPixels();
        int W = 800;
        for (int i = 0; i < px.Length; i++)
        {
            int x = i % W, y = i / W;
            var c = px[i];
            float r = Mathf.Pow(c.r, 1.25f) * 0.84f, g = Mathf.Pow(c.g, 1.25f) * 0.84f, b = Mathf.Pow(c.b, 1.25f) * 0.84f;
            float l = 0.3f * r + 0.59f * g + 0.11f * b;
            r = l + (r - l) * 1.15f; g = l + (g - l) * 1.15f; b = l + (b - l) * 1.15f;
            if (g > r * 1.1f && g > b * 1.3f) { r *= 0.9f; g *= 0.88f; b *= 0.9f; }
            r = (r - 0.4f) * 1.12f + 0.4f; g = (g - 0.4f) * 1.12f + 0.4f; b = (b - 0.4f) * 1.12f + 0.4f;
            float dx = (x - W / 2f) / (W / 2f), dy = (y - W / 2f) / (W / 2f);
            float dd = Mathf.Sqrt(dx * dx + dy * dy);
            float v = 1f - 0.32f * Mathf.Pow(Mathf.Clamp01((dd - 0.55f) / 0.6f), 1.5f);
            px[i] = new Color(Mathf.Clamp01(r * v), Mathf.Clamp01(g * v), Mathf.Clamp01(b * v));
        }
        tex.SetPixels(px); tex.Apply();
        var small = new RenderTexture(400, 400, 0);
        Graphics.Blit(tex, small);
        RenderTexture.active = small;
        var outTex = new Texture2D(400, 400, TextureFormat.RGB24, false);
        outTex.ReadPixels(new Rect(0, 0, 400, 400), 0, 0); outTex.Apply();
        RenderTexture.active = null; small.Release();
        string ip = "Assets/UI/Image/MapIcon/" + d.icon + ".png";
        WriteMapped(ip, outTex.EncodeToPNG());
        AssetDatabase.ImportAsset(ip, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(ip);
        if (imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
    }

    // Unity keeps imported files memory-mapped (Win32 error 1224 on write): unload the asset and retry.
    static void WriteMapped(string path, byte[] bytes)
    {
        try { File.WriteAllBytes(path, bytes); return; } catch (IOException) { }
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o && !(o is GameObject) && !(o is Component)) Resources.UnloadAsset(o);
        EditorUtility.UnloadUnusedAssetsImmediate(); GC.Collect(); GC.WaitForPendingFinalizers();
        AssetDatabase.ReleaseCachedFileHandles();   // the asset database keeps imported files mapped
        try { File.WriteAllBytes(path, bytes); }
        catch (IOException e) { log += "\nWARNING: " + path + " is locked, old image kept (" + e.Message + ")"; Debug.LogWarning("[MapBuilder] " + path + " locked: " + e.Message); }
    }

    // ------------------------------------------------------------------ registration
    static void AddLocalization(Def d)
    {
        const string csv = "Assets/SimpleLocalization/Resources/Localization/Main.csv";
        var txt = File.ReadAllText(csv);
        if (txt.Contains("\n" + d.mapEnum + ",")) return;
        if (!txt.EndsWith("\n")) txt += "\r\n";
        txt += d.mapEnum + "," + string.Join(",", d.names) + "\r\n";
        File.WriteAllText(csv, txt, new System.Text.UTF8Encoding(false));
        AssetDatabase.ImportAsset(csv);
    }

    static void AddBuildSettings(string path)
    {
        if (Retired.Contains(Path.GetFileNameWithoutExtension(path))) return;   // retired maps stay out of the build
        var list = EditorBuildSettings.scenes.ToList();
        if (list.Any(s => s.path == path)) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    /// <summary>Takes the retired maps' scenes out of the build list (menu Tools/Maps/Remove Retired Maps From Build).</summary>
    [MenuItem("Tools/Maps/Remove Retired Maps From Build")]
    public static string RemoveRetiredFromBuild()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return "wrong project";
        var all = EditorBuildSettings.scenes;
        var kept = all.Where(s => !Retired.Contains(Path.GetFileNameWithoutExtension(s.path))).ToArray();
        if (kept.Length != all.Length) EditorBuildSettings.scenes = kept;
        return "removed " + (all.Length - kept.Length) + " retired map scenes from the build, " + kept.Length + " left";
    }

    /// <summary>Maps taken out of the map selection (Nov 2026): in each mode one map per art theme is kept, the free
    /// one when two share a theme. Their Map enum values stay (the enum goes over the network as a byte), but since
    /// Nov 2026 their scenes are out of the build too (Tho: the build had grown a lot). A room an older version opens on
    /// one of them cannot be joined (UIRoomItem / PopupInsideRoom check the scene is in the build); a solo or Photon
    /// room that still asks for one plays the mode's first map (ArenaManager.WaitLoadMap). The scene files stay in the
    /// project; AddCard and AddBuildSettings never put them back.</summary>
    public static readonly HashSet<string> Retired = new HashSet<string>
    {
        "Map_Brawl_Oasis", "Map_Brawl_DustyCanyon", "Map_Brawl_BambooTemple", "Map_Brawl_MoonMaze",          // Competitive
        "Map_Brawl_LunarVault", "Map_Brawl_BambooShrine", "Map_Brawl_MineExpress", "Map_Brawl_RobotRidge",   // CTF, KOTH, Payload, MvM
        "Map_Brawl_HaleMesa", "Map_Brawl_DustDevil", "Map_Brawl_FrontierOutpost", "Map_Brawl_BigfootGulch",  // Boss Battle
        "Map_Brawl_StompingGrounds", "Map_Brawl_YetiHollow", "Map_Brawl_MoonlitDen",
        "Map_Brawl_GhoulMine", "Map_Brawl_CryptTunnels", "Map_Brawl_LastLight", "Map_Brawl_DeadCrater",      // Zombie
        // second pass (Nov 2026): the Mix-theme maps (orange desert floor, mixed cliffs) read as the same desert as the
        // Western map of their mode (Wild West, Gold Rush, Sheriff Square, Dynamite Trail, Mann vs Machine)
        "Map_Brawl_FrontierRuins", "Map_Brawl_BorderSiege", "Map_Brawl_CrossroadRuins", "Map_Brawl_FrontierConvoy",
        "Map_Brawl_LastStand",
        // Nov 2026: the hand-made Cave Payload map crashed on a phone; Map_Brawl_CrystalRail (generated, Cave) replaces
        // it. Its scene stays in the project: the generated Payload maps clone its cart systems, Cave maps its lighting.
        "Map_Brawl_Payload",
    };

    [MenuItem("Tools/Maps/Remove Retired Map Cards")]
    public static string RemoveRetiredCards()
    {
        const string pp = "Assets/Resources/Popups/PopupSelectMap.prefab";
        var root = PrefabUtility.LoadPrefabContents(pp);
        int n = 0;
        try
        {
            foreach (var c in root.GetComponentsInChildren<UIMapIcon>(true).ToList())
                if (Retired.Contains(c.map.ToString())) { Object.DestroyImmediate(c.gameObject); n++; }
            if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, pp);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return "removed " + n + " retired map cards";
    }

    static void AddCard(Def d)
    {
        const string pp = "Assets/Resources/Popups/PopupSelectMap.prefab";
        if (Retired.Contains(d.mapEnum)) { RemoveRetiredCards(); return; }   // rebuilt, but not selectable
        var root = PrefabUtility.LoadPrefabContents(pp);
        try
        {
            var icons = root.GetComponentsInChildren<UIMapIcon>(true).ToList();
            var map = (Map)Enum.Parse(typeof(Map), d.mapEnum);
            var card = icons.FirstOrDefault(i => i.map == map);
            if (card == null)
            {
                var tmpl = icons.Where(i => i.gameMode == d.mode).LastOrDefault() ?? icons.Last();
                var go = Object.Instantiate(tmpl.gameObject, tmpl.transform.parent);
                go.transform.SetSiblingIndex(tmpl.transform.GetSiblingIndex() + 1);
                card = go.GetComponent<UIMapIcon>();
            }
            card.gameObject.name = "Map (" + d.mapEnum.Replace("Map_Brawl_", "") + ")";
            card.map = map; card.gameMode = d.mode;
            card.mapIcon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Image/MapIcon/" + d.icon + ".png");
            foreach (var t in card.GetComponentsInChildren<TMPro.TMP_Text>(true))
                if (t.name == "Name") t.text = d.displayName;
            var cg = card.GetComponent<CanvasGroup>(); if (cg) cg.alpha = 1;
            card.selectedObj.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, pp);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
