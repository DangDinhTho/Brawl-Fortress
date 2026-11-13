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
public static class BrawlMapBuilder
{
    public enum Theme { Western, Bamboo, Cave, Moon, Mix }

    public class Def
    {
        public string mapEnum, displayName, icon;
        public GameMode mode;
        public Theme theme;
        public int seed;
        public string[] half;   // 13 rows, red half, row 0 = back line
        public string center;   // row 13, must be a palindrome
        public string[] names;  // en, ru, th, es, vi
    }

    const string BaseScene = "Assets/Scenes/Map_Brawl_Oasis.unity";
    const string PrefabRoot = "Assets/Battle Arena - Cartoon Assets/Prefabs";
    const string WorldMat = "Assets/Battle Arena - Cartoon Assets/Materials/World.mat";
    const string WorldTMat = "Assets/Battle Arena - Cartoon Assets/Materials/World_Transparent.mat";
    const float T = 1.5f;
    const int COLS = 21, ROWS = 27;
    const int WallLayer = 11;

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

    public static Def Find(string mapEnum) => Competitive.First(d => d.mapEnum == mapEnum);

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

        // close other map scenes (keep only non-map scenes like FlashScene)
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var s = SceneManager.GetSceneAt(i);
            if ((s.name.StartsWith("Map_") || s.name.StartsWith("Brawl")) && SceneManager.sceneCount > 1)
                EditorSceneManager.CloseScene(s, true);
        }

        if (File.Exists(path)) File.Copy(BaseScene, path, true);
        else AssetDatabase.CopyAsset(BaseScene, path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        LoadAssets();

        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        env = Root("Environment").transform;
        spawnPoints = Root("TeamPos").GetComponentsInChildren<Transform>().Where(t => t.childCount == 0).Select(t => t.position).ToList();
        if (d.theme == Theme.Moon) LoadMoonTemplates();

        ApplyLighting(d.theme);
        PrepareBorderAndGround(d.theme);
        Physics.SyncTransforms();
        PlaceGrid(d);
        if (d.theme == Theme.Moon) PlaceMoonBackdrop(d.seed);
        var holder = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "_templates");
        if (holder) Object.DestroyImmediate(holder);
        Physics.SyncTransforms();

        // bushes, navmesh, path points
        EditorApplication.ExecuteMenuItem("Tools/Bush/Build Bush Areas (Active Scene)");
        var bushes = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Bushes");
        log += " bushes=" + (bushes ? bushes.transform.childCount : 0);
        var navData = BakeNavMesh(d.mapEnum);
        FixPathPoints(d, navData);

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

    static void Validate(Def d)
    {
        if (d.half.Length != 13) throw new Exception(d.mapEnum + " half must have 13 rows");
        foreach (var r in d.half.Append(d.center))
            if (r.Length != COLS) throw new Exception(d.mapEnum + " bad row length " + r.Length + ": " + r);
        if (new string(d.center.Reverse().ToArray()) != d.center) throw new Exception(d.mapEnum + " centre row must be a palindrome");
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
        world = AssetDatabase.LoadAssetAtPath<Material>(WorldMat);
        worldT = AssetDatabase.LoadAssetAtPath<Material>(WorldTMat);
    }

    // ------------------------------------------------------------------ moon templates
    static Scene moonScene;
    static void LoadMoonTemplates()
    {
        moon = new Dictionary<string, GameObject>();
        templateHolder = null;
        moonScene = EditorSceneManager.OpenScene("Assets/Scenes/Map_Brawl_ShowDown.unity", OpenSceneMode.Additive);
        var menv = moonScene.GetRootGameObjects().First(g => g.name == "Environment");
        var mfs = menv.GetComponentsInChildren<MeshFilter>(true);
        foreach (var key in new[] { "Wall_Tile_Block1", "Grass_Tile_A_opt", "PolyPyramid", "Night_Plane", "RockFlat3", "RockFlat5", "RockFlat6", "Skull_opt", "Bone_opt", "Bone_ChestA_opt", "Bones_ChestB_opt" })
        {
            var mf = mfs.FirstOrDefault(m => m.sharedMesh && m.sharedMesh.name.StartsWith(key)) ?? mfs.FirstOrDefault(m => m.sharedMesh && m.sharedMesh.name.Contains(key));
            if (mf) moon[key] = KeepTemplate(mf.gameObject);
        }
        var chest = mfs.FirstOrDefault(m => m.sharedMesh && m.sharedMesh.name == "Chest");
        if (chest) moon["Chest"] = KeepTemplate(chest.gameObject);
        EditorSceneManager.CloseScene(moonScene, true);
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
        if (moonScene.IsValid() && moonScene.isLoaded) EditorSceneManager.CloseScene(moonScene, true);
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
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, rotY, 0);
        go.transform.localScale = scale3 ?? Vector3.one * s;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = col;
        var m = mat ? mat : world;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterials = r.sharedMaterials.Select(_ => m).ToArray();
        Finish(go);
        return go;
    }

    static T Pick<T>(Random rnd, params T[] items) => items[rnd.Next(items.Length)];
    static float Rot90(Random rnd) => rnd.Next(4) * 90f;
    static float Jit(Random rnd, float a) => (float)(rnd.NextDouble() * 2 - 1) * a;

    // ------------------------------------------------------------------ lighting / border / ground
    static void ApplyLighting(Theme theme)
    {
        string refScene = theme switch
        {
            Theme.Western => "Assets/Scenes/BrawlScene.unity",
            Theme.Bamboo => "Assets/Scenes/BrawlScene_2.unity",
            Theme.Cave => "Assets/Scenes/Map_Brawl_Payload.unity",
            Theme.Moon => "Assets/Scenes/Map_Brawl_ShowDown.unity",
            _ => null,
        };
        if (refScene == null) return;
        var rs = SceneManager.GetSceneByPath(refScene);
        bool opened = false;
        if (!rs.isLoaded) { rs = EditorSceneManager.OpenScene(refScene, OpenSceneMode.Additive); opened = true; }
        SceneManager.SetActiveScene(rs);
        var mode = RenderSettings.ambientMode; var sky = RenderSettings.ambientSkyColor; var eq = RenderSettings.ambientEquatorColor;
        var gr = RenderSettings.ambientGroundColor; var amb = RenderSettings.ambientLight; var ai = RenderSettings.ambientIntensity;
        var box = RenderSettings.skybox; var fog = RenderSettings.fog; var fogC = RenderSettings.fogColor; var fogM = RenderSettings.fogMode;
        var fs = RenderSettings.fogStartDistance; var fe = RenderSettings.fogEndDistance; var fd = RenderSettings.fogDensity;
        var refLight = rs.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).FirstOrDefault(l => l.type == LightType.Directional);
        SceneManager.SetActiveScene(scene);
        RenderSettings.ambientMode = mode; RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = eq;
        RenderSettings.ambientGroundColor = gr; RenderSettings.ambientLight = amb; RenderSettings.ambientIntensity = ai;
        RenderSettings.skybox = box; RenderSettings.fog = fog; RenderSettings.fogColor = fogC; RenderSettings.fogMode = fogM;
        RenderSettings.fogStartDistance = fs; RenderSettings.fogEndDistance = fe; RenderSettings.fogDensity = fd;
        var light = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).FirstOrDefault(l => l.type == LightType.Directional);
        if (light && refLight)
        {
            light.color = refLight.color; light.intensity = refLight.intensity; light.shadows = refLight.shadows;
            light.shadowStrength = refLight.shadowStrength; light.transform.rotation = refLight.transform.rotation;
        }
        if (opened) EditorSceneManager.CloseScene(rs, true);
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
                Material m = theme == Theme.Moon ? sdCliffMat : world;
                if (set[ci] != n || theme == Theme.Moon)
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
            case Theme.Moon:
                var plane = CloneMoon("Night_Plane", Vector3.zero, 0, Vector3.one * 0.6f, false);
                plane.AddComponent<MeshCollider>();
                Physics.SyncTransforms();
                if (Physics.Raycast(new Vector3(0, 20, 0), Vector3.down, out var hit, 40f))
                    plane.transform.position += Vector3.up * (-hit.point.y);
                break;
        }
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
        for (int r = 0; r < ROWS; r++) for (int c = 0; c < COLS; c++) blocked[r, c] = Blocked(TilePos(r, c));
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
                    if (rnd.NextDouble() < 0.06) { Decor(d.theme, p, baseRot, rnd); decor++; }
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
            case Theme.Cave: Prefab(Pick(rnd, "Rock9", "Rock10", "Rock13", "Rock14", "Rock15", "Rail1"), o, rot, 0.8f, false); break;
            case Theme.Moon:
                {
                    var k = Pick(rnd, "RockFlat3", "RockFlat5", "RockFlat6", "Skull_opt", "Bone_opt");
                    if (moon.ContainsKey(k)) CloneMoon(k, o, rot, Vector3.one * 0.6f, false);
                }
                break;
            case Theme.Mix: Prefab(Pick(rnd, "Flower02", "rock8", "Rock13", "Bone1", "Flower04", "Stone08"), o, rot, 0.7f, false); break;
        }
    }

    // ------------------------------------------------------------------ navmesh + path points
    static NavMeshData BakeNavMesh(string id)
    {
        var navGo = Root("NavmeshSurface");
        navGo.transform.position = Vector3.zero; navGo.transform.rotation = Quaternion.identity;
        var surf = navGo.GetComponents<Component>().First(c => c != null && c.GetType().Name == "NavMeshSurface");
        var so = new SerializedObject(surf);
        int mask = so.FindProperty("m_LayerMask").intValue;
        var sources = new List<NavMeshBuildSource>();
        var markups = new List<NavMeshBuildMarkup>();
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
    static void RenderIcon(Def d)
    {
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
        File.WriteAllBytes(ip, outTex.EncodeToPNG());
        AssetDatabase.ImportAsset(ip, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(ip);
        if (imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
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
        var list = EditorBuildSettings.scenes.ToList();
        if (list.Any(s => s.path == path)) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    static void AddCard(Def d)
    {
        const string pp = "Assets/Resources/Popups/PopupSelectMap.prefab";
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
