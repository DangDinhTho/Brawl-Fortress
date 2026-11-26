using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Payload + Mann vs Machine maps: each map has its own cart/tank path.
// pathHalf = waypoints (x,z) from the blue start towards the centre; the red half is the point mirror,
// so the full path runs blue base -> red base. Tiles within PathClear metres of the path are left empty.
// Systems (BezierCurves, BombCart/BombTank, AtomBomb, Target, explosion, deploy camera, mode manager) are
// cloned from Map_Brawl_Payload / Map_Brawl_MvM and every internal reference is remapped to the clones.
public static partial class BrawlMapBuilder
{
    const float PathClear = 1.7f;
    static List<Vector3> pathSamples;   // dense samples of the current map's path (y = 0)
    static List<Vector3> pathPoints;    // waypoints of the full path (y = 0)

    static partial void ExtraDefsImpl(List<Def> list) { list.AddRange(Payload); list.AddRange(MvM); }

    [MenuItem("Tools/Maps/Build Payload Maps")]
    public static void BuildPayloadMenu() { foreach (var d in Payload) Build(d); }

    [MenuItem("Tools/Maps/Build Mann Vs Machine Maps")]
    public static void BuildMvmMenu() { foreach (var d in MvM) Build(d); }

    static readonly Vector2[] PathA = { new Vector2(0, 16.5f), new Vector2(0, 9), new Vector2(-6, 4) };
    static readonly Vector2[] PathB = { new Vector2(6, 16.5f), new Vector2(6, 8), new Vector2(-6, 5), new Vector2(-6, 1) };
    static readonly Vector2[] PathC = { new Vector2(0, 16.5f), new Vector2(0, 5) };
    static readonly Vector2[] PathD = { new Vector2(0, 16.5f), new Vector2(-8, 10), new Vector2(-9, 2) };
    static readonly Vector2[] PathE = { new Vector2(-4, 16.5f), new Vector2(-4, 6), new Vector2(4, 2) };

    public static readonly Def[] Payload =
    {
        new Def {
            mapEnum = "Map_Brawl_MineExpress", displayName = "Mine Express", icon = "map21", mode = GameMode.Payload, theme = Theme.Cave, seed = 3101, pathHalf = PathA,
            names = new[] { "Mine Express", "Шахтный экспресс", "รถด่วนเหมือง", "Expreso Minero", "Tàu Mỏ Tốc Hành" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##.............##..",
                "..#...bb.....bb...#..",
                "......bb.....bb......",
                ".bbb.....x.x.....bbb.",
                ".bbb.............bbb.",
                "....##.........##....",
                "..x...............x..",
                ".......bb...bb.......",
                ".##....bb...bb....##.",
                ".....#.........#.....",
            },
            center = "..bb.............bb..",
        },
        new Def {
            mapEnum = "Map_Brawl_MoonRail", displayName = "Moon Rail", icon = "map22", mode = GameMode.Payload, theme = Theme.Moon, seed = 3202, pathHalf = PathB,
            names = new[] { "Moon Rail", "Лунная дорога", "รางจันทรา", "Riel Lunar", "Đường Ray Ánh Trăng" },
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
        new Def {
            mapEnum = "Map_Brawl_BambooLine", displayName = "Bamboo Line", icon = "map23", mode = GameMode.Payload, theme = Theme.Bamboo, seed = 3303, pathHalf = PathC,
            names = new[] { "Bamboo Line", "Бамбуковая линия", "เส้นทางไผ่", "Línea de Bambú", "Tuyến Đường Tre" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb......t......bbb.",
                ".bbb.............bbb.",
                "......ff.....ff......",
                "bbbb...bb...bb...bbbb",
                "bbbb...bb...bb...bbbb",
                ".....................",
                "..t....#.....#....t..",
                "..bbbb...........bbbb",
                "..bbbb.....x.....bbbb",
                "......##.....##......",
            },
            center = "bb....t.......t....bb",
        },
        new Def {
            mapEnum = "Map_Brawl_DynamiteTrail", displayName = "Dynamite Trail", icon = "map24", mode = GameMode.Payload, theme = Theme.Western, seed = 3404, pathHalf = PathD,
            names = new[] { "Dynamite Trail", "Тропа динамита", "เส้นทางไดนาไมต์", "Sendero Dinamita", "Đường Mìn" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "....x...........x....",
                "..bb.............bb..",
                "..bb..fff...fff..bb..",
                ".....................",
                ".##......bbb......##.",
                "......t.......t......",
                "...bb.............bb.",
                "...bb....##.......bb.",
                ".....................",
                ".x....f.......f....x.",
            },
            center = "..bb...#.....#...bb..",
        },
        new Def {
            mapEnum = "Map_Brawl_FrontierConvoy", displayName = "Frontier Convoy", icon = "map25", mode = GameMode.Payload, theme = Theme.Mix, seed = 3505, pathHalf = PathE,
            names = new[] { "Frontier Convoy", "Конвой фронтира", "ขบวนรถชายแดน", "Convoy Fronterizo", "Đoàn Xe Biên Cương" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "...##...........##...",
                "......bb..x..bb......",
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

    public static readonly Def[] MvM =
    {
        new Def {
            mapEnum = "Map_Brawl_RobotRidge", displayName = "Robot Ridge", icon = "map26", mode = GameMode.MannVsMachine, theme = Theme.Western, seed = 4101, pathHalf = PathA,
            names = new[] { "Robot Ridge", "Хребет роботов", "สันเขาหุ่นยนต์", "Cresta Robot", "Đồi Robot" },
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
            mapEnum = "Map_Brawl_IronGrove", displayName = "Iron Grove", icon = "map27", mode = GameMode.MannVsMachine, theme = Theme.Bamboo, seed = 4202, pathHalf = PathD,
            names = new[] { "Iron Grove", "Железная роща", "ป่าเหล็ก", "Arboleda de Hierro", "Rừng Thép" },
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
            mapEnum = "Map_Brawl_CrystalFactory", displayName = "Crystal Factory", icon = "map28", mode = GameMode.MannVsMachine, theme = Theme.Cave, seed = 4303, pathHalf = PathB,
            names = new[] { "Crystal Factory", "Хрустальная фабрика", "โรงงานคริสตัล", "Fábrica de Cristal", "Nhà Máy Pha Lê" },
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
            mapEnum = "Map_Brawl_MoonbaseSiege", displayName = "Moonbase Siege", icon = "map29", mode = GameMode.MannVsMachine, theme = Theme.Moon, seed = 4404, pathHalf = PathE,
            names = new[] { "Moonbase Siege", "Осада лунной базы", "ล้อมฐานจันทรา", "Asedio a la Base Lunar", "Vây Hãm Căn Cứ Trăng" },
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
        new Def {
            mapEnum = "Map_Brawl_LastStand", displayName = "Last Stand Ruins", icon = "map30", mode = GameMode.MannVsMachine, theme = Theme.Mix, seed = 4505, pathHalf = PathC,
            names = new[] { "Last Stand Ruins", "Руины последнего рубежа", "ซากป้อมสุดท้าย", "Ruinas de la Última Defensa", "Tàn Tích Cố Thủ" },
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
    };

    // ------------------------------------------------------------------ path
    static void PrePlace(Def d)
    {
        pathSamples = null; pathPoints = null;
        if (d.pathHalf == null) return;
        pathPoints = d.pathHalf.Select(v => new Vector3(v.x, 0, v.y)).ToList();
        for (int i = d.pathHalf.Length - 1; i >= 0; i--) pathPoints.Add(new Vector3(-d.pathHalf[i].x, 0, -d.pathHalf[i].y));
        pathSamples = new List<Vector3>();
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Ctrl(i, out var a, out var b, out var c1, out var c2);
            for (int k = 0; k <= 12; k++) pathSamples.Add(Cubic(a, c1, c2, b, k / 12f));
        }
        // move spawns that stand on the path
        var tp = Root("TeamPos").GetComponentsInChildren<Transform>().Where(t => t.childCount == 0).ToList();
        foreach (var s in tp)
        {
            int guard = 0;
            while (DistToPath(s.position) < 2.4f && guard++ < 6)
                s.position += new Vector3(s.position.x <= 0 ? -1.5f : 1.5f, 0, 0);
        }
        spawnPoints = tp.Select(t => t.position).ToList();
    }

    static void Ctrl(int i, out Vector3 a, out Vector3 b, out Vector3 c1, out Vector3 c2)
    {
        a = pathPoints[i]; b = pathPoints[i + 1];
        Vector3 ta = i == 0 ? (b - a) : (pathPoints[i + 1] - pathPoints[i - 1]) * 0.5f;
        Vector3 tb = i + 2 >= pathPoints.Count ? (b - a) : (pathPoints[i + 2] - pathPoints[i]) * 0.5f;
        c1 = a + ta / 3f; c2 = b - tb / 3f;
    }

    static Vector3 Cubic(Vector3 a, Vector3 c1, Vector3 c2, Vector3 b, float t)
    {
        float u = 1 - t;
        return u * u * u * a + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * b;
    }

    static float DistToPath(Vector3 p)
    {
        if (pathSamples == null) return float.MaxValue;
        float best = float.MaxValue;
        p.y = 0;
        for (int i = 0; i < pathSamples.Count - 1; i++)
        {
            var a = pathSamples[i]; var b = pathSamples[i + 1];
            var ab = b - a; float l = ab.sqrMagnitude;
            float t = l > 0 ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / l) : 0;
            best = Mathf.Min(best, (a + ab * t - p).magnitude);
        }
        return best;
    }

    static bool NearPath(Vector3 p) => pathSamples != null && DistToPath(p) < PathClear;

    // ------------------------------------------------------------------ cloning with reference remap
    static Dictionary<Object, Object> CloneSet(Scene src, IEnumerable<string> roots, List<GameObject> clones)
    {
        var map = new Dictionary<Object, Object>();
        foreach (var n in roots)
        {
            var o = src.GetRootGameObjects().FirstOrDefault(g => g.name == n);
            if (!o) { log += " [no " + n + "]"; continue; }
            var c = Object.Instantiate(o);
            c.name = n;
            SceneManager.MoveGameObjectToScene(c, scene);
            var ot = o.GetComponentsInChildren<Transform>(true);
            var ct = c.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < ot.Length && i < ct.Length; i++)
            {
                map[ot[i].gameObject] = ct[i].gameObject;
                var oc = ot[i].GetComponents<Component>();
                var cc = ct[i].GetComponents<Component>();
                for (int k = 0; k < oc.Length && k < cc.Length; k++) if (oc[k] && cc[k]) map[oc[k]] = cc[k];
            }
            clones.Add(c);
        }
        return map;
    }

    static void Remap(Component comp, Dictionary<Object, Object> map)
    {
        if (!comp) return;
        var so = new SerializedObject(comp);
        var it = so.GetIterator();
        bool changed = false;
        while (it.Next(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
            var v = it.objectReferenceValue;
            if (v && map.TryGetValue(v, out var nv)) { it.objectReferenceValue = nv; changed = true; }
        }
        if (changed) so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Component CopyManager(GameObject srcMapManager, GameObject dstMapManager, Type t, Dictionary<Object, Object> map)
    {
        var s = srcMapManager.GetComponent(t);
        var c = dstMapManager.GetComponent(t) ?? dstMapManager.AddComponent(t);
        EditorUtility.CopySerialized(s, c);
        Remap(c, map);
        return c;
    }

    // ------------------------------------------------------------------ setup
    static partial void SetupExtraModeImpl(Def d, GameObject mmGo)
    {
        if (d.mode == GameMode.Payload) SetupObjective(d, mmGo, "Assets/Scenes/Map_Brawl_Payload.unity",
            new[] { "BezierCurves", "BombCart", "Target", "MegaExplosionYellow", "Main Camera" }, typeof(PayloadManager), "BombCart", 0.25f);
        else if (d.mode == GameMode.MannVsMachine) SetupObjective(d, mmGo, "Assets/Scenes/Map_Brawl_MvM.unity",
            new[] { "BezierCurves", "BombTank", "AtomBomb", "Target", "MegaExplosionYellow", "BasicZoneBlue", "DeployCam" }, typeof(ManvsMachineManager), "BombTank", 0.0f);
        else if (d.mode == GameMode.JungleBoss) { var j = mmGo.GetComponent<JungleModeManager>() ?? mmGo.AddComponent<JungleModeManager>(); j.bossType = TF2Class.hale; }
        else if (d.mode == GameMode.Zombie) { if (!mmGo.GetComponent<ZombieModeManager>()) mmGo.AddComponent<ZombieModeManager>(); }
    }

    static void SetupObjective(Def d, GameObject mmGo, string srcPath, string[] roots, Type managerType, string vehicleName, float pathY)
    {
        var src = Source(srcPath);
        try
        {
            var clones = new List<GameObject>();
            var map = CloneSet(src, roots, clones);
            var srcMM = src.GetRootGameObjects().First(g => g.name == "MapManager");
            var mgr = CopyManager(srcMM, mmGo, managerType, map);
            foreach (var c in clones) foreach (var comp in c.GetComponentsInChildren<Component>(true)) Remap(comp, map);

            GameObject R(string n) => clones.First(c => c.name == n);
            var curves = R("BezierCurves").transform;
            var vehicle = R(vehicleName);
            var target = R("Target").transform;

            // rebuild the bezier fragments along this map's path
            var template = curves.GetChild(0).gameObject;
            for (int i = curves.childCount - 1; i >= 1; i--) Object.DestroyImmediate(curves.GetChild(i).gameObject);
            var frags = new List<GameObject>();
            var pts = pathPoints.Select(p => p + Vector3.up * pathY).ToList();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var f = i == 0 ? template : Object.Instantiate(template, curves);
                f.name = i == 0 ? "fragment" : "fragment (" + i + ")";
                var bf = f.GetComponent<BezierFragment>();
                Ctrl(i, out var a, out var b, out var c1, out var c2);
                var up = Vector3.up * pathY;
                f.transform.position = a + up;
                bf.pointA.position = a + up; bf.pointB.position = b + up;
                bf.bezierPoint[0].position = c1 + up; bf.bezierPoint[1].position = c2 + up;
                bf.pointA.rotation = Quaternion.LookRotation((c1 - a).normalized + new Vector3(0, 0, 1e-4f));
                bf.pointB.rotation = Quaternion.LookRotation((b - c2).normalized + new Vector3(0, 0, 1e-4f));
                bf.Update();
                EditorUtility.SetDirty(bf);
                frags.Add(f);
            }
            var coaster = vehicle.GetComponent<CoasterfollowerAdv>();
            coaster.fragments = frags.ToArray();
            EditorUtility.SetDirty(coaster);
            var startDir = (pts[1] - pts[0]).normalized;
            vehicle.transform.position = pts[0];
            vehicle.transform.rotation = Quaternion.LookRotation(startDir);
            var end = pts[pts.Count - 1];
            target.position = new Vector3(end.x, 0.1f, end.z);
            var cart = vehicle.GetComponent<BombCartController>();
            if (cart) { cart.target = target; EditorUtility.SetDirty(cart); }
            if (d.mode == GameMode.Payload) SizeCart(vehicle);

            foreach (var n in new[] { "MegaExplosionYellow", "BasicZoneBlue" })
            {
                var g = clones.FirstOrDefault(c => c.name == n);
                if (g) g.transform.position = new Vector3(end.x, g.name == "BasicZoneBlue" ? 0.05f : 0.6f, end.z);
            }
            var camGo = clones.FirstOrDefault(c => c.GetComponent<Camera>());
            if (camGo)
            {
                camGo.transform.position = new Vector3(end.x + 6f, 10f, end.z + 9f);
                camGo.transform.LookAt(new Vector3(end.x, 0.5f, end.z));
                camGo.SetActive(false);
            }
            var bomb = clones.FirstOrDefault(c => c.name == "AtomBomb");
            if (bomb) bomb.transform.position = new Vector3(pts[0].x + 2.5f, 0.3f, pts[0].z - 1.5f);

            // ground the vehicle-related objects on our map layer rules (keep their own layers)
            if (d.mode == GameMode.Payload) PlaceRails(pts, d.theme);
            EditorUtility.SetDirty(mgr);
            log += " path=" + pts.Count + "pts frags=" + frags.Count + " vehicle=" + vehicle.name + " pv=" + string.Join("/", clones.SelectMany(c => c.GetComponentsInChildren<Photon.Pun.PhotonView>(true)).Select(pv => new SerializedObject(pv).FindProperty("sceneViewId").intValue));
        }
        finally { }
    }

    // ------------------------------------------------------------------ cart size
    // The cart was small for its track (about 1 m wide on the 0.95 m gauge with 1.5 m sleepers): 1.25 x keeps its wheels
    // on the rails and makes it read better; the push zone (EntryFieldOfView radius, 2 m) grows with it.
    const float CartScale = 1.25f, CartPushRadius = 2.5f;

    static void SizeCart(GameObject vehicle)
    {
        vehicle.transform.localScale = Vector3.one * CartScale;
        var fov = vehicle.GetComponent<EntryFieldOfView>();
        if (fov) { fov.radius = CartPushRadius; EditorUtility.SetDirty(fov); }
        EditorUtility.SetDirty(vehicle.transform);
    }

    /// <summary>Applies the cart size and push zone to every Payload map in the build (generated or hand-made) without
    /// rebuilding them. The generated maps clone the cart of Map_Brawl_Payload, Build() also sizes it.</summary>
    [MenuItem("Tools/Maps/Apply Payload Cart Size")]
    public static string ApplyPayloadCartSize()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return "wrong project";
        GuardUnsavedScenes();
        var done = new List<string>();
        foreach (var path in EditorBuildSettings.scenes.Where(x => x.enabled && x.path.Contains("/Map_Brawl_")).Select(x => x.path))
        {
            var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var cart = sc.GetRootGameObjects().FirstOrDefault(g => g.GetComponent<BombCartController>());
            if (!cart) continue;
            SizeCart(cart);
            EditorSceneManager.MarkSceneDirty(sc);
            EditorSceneManager.SaveScene(sc);
            done.Add(sc.name);
        }
        return done.Count + " payload maps: " + string.Join(", ", done);
    }

    // ------------------------------------------------------------------ the hand-made source map
    /// <summary>Map_Brawl_Payload is hand-made (the generated maps clone its cart systems; Build() never rebuilds it).
    /// Its cart's Bezier path did not follow its hand-laid rail pieces (up to 2.4 m off: the cart rolled beside the track)
    /// and its NavMeshSurface had been switched off since the first commit (no NavMesh: bots stood still). This rebuilds
    /// the fragments as a Catmull-Rom through the centres of the rail pieces (under the "rail" object), from the cart to a
    /// little past the target, switches the surface on, rebakes it (bushes walkable), snaps the patrol points onto it and
    /// rebuilds the bush patrol points.</summary>
    [MenuItem("Tools/Maps/Fix Payload Source Map")]
    public static string FixPayloadSourceMap()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return "wrong project";
        GuardUnsavedScenes();
        log = "";
        scene = EditorSceneManager.OpenScene("Assets/Scenes/Map_Brawl_Payload.unity", OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        var curves = roots.First(g => g.name == "BezierCurves").transform;
        var vehicle = roots.First(g => g.GetComponent<CoasterfollowerAdv>());
        var target = roots.First(g => g.name == "Target").transform;
        float y = curves.GetChild(0).GetComponent<BezierFragment>().pointA.position.y;
        var railRoot = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)).First(t => t.name == "rail");
        var centres = railRoot.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds.center).ToList();

        // the path: from the cart along the rail pieces (ordered down the map) to 2.2 m past the last one
        Vector3 start = vehicle.transform.position;
        var pts = new List<Vector3> { new Vector3(start.x, y, start.z) };
        foreach (var c in centres.Where(c => c.z < start.z - 1f).OrderByDescending(c => c.z)) pts.Add(new Vector3(c.x, y, c.z));
        var dir = pts[pts.Count - 1] - pts[pts.Count - 2]; dir.y = 0f; dir.Normalize();
        pts.Add(pts[pts.Count - 1] + dir * 2.2f);
        pathPoints = pts;   // Ctrl() makes the Catmull-Rom tangents from pathPoints

        var template = curves.GetChild(0).gameObject;
        for (int i = curves.childCount - 1; i >= 1; i--) Object.DestroyImmediate(curves.GetChild(i).gameObject);
        var frags = new List<GameObject>();
        for (int i = 0; i < pts.Count - 1; i++)
        {
            var f = i == 0 ? template : Object.Instantiate(template, curves);
            f.name = i == 0 ? "fragment" : "fragment (" + i + ")";
            var bf = f.GetComponent<BezierFragment>();
            Ctrl(i, out var a, out var b, out var c1, out var c2);
            f.transform.position = a;
            bf.pointA.position = a; bf.pointB.position = b;
            bf.bezierPoint[0].position = c1; bf.bezierPoint[1].position = c2;
            bf.pointA.rotation = Quaternion.LookRotation((c1 - a).normalized + new Vector3(0, 0, 1e-4f));
            bf.pointB.rotation = Quaternion.LookRotation((b - c2).normalized + new Vector3(0, 0, 1e-4f));
            bf.Update();
            EditorUtility.SetDirty(bf);
            frags.Add(f);
        }
        var coaster = vehicle.GetComponent<CoasterfollowerAdv>();
        coaster.fragments = frags.ToArray();
        EditorUtility.SetDirty(coaster);
        vehicle.transform.rotation = Quaternion.LookRotation(pts[1] - pts[0]);
        // the deviation that was the bug: rail piece centres to the new path
        pathSamples = new List<Vector3>();
        for (int i = 0; i < pts.Count - 1; i++) { Ctrl(i, out var a, out var b, out var c1, out var c2); for (int k = 0; k <= 12; k++) pathSamples.Add(Cubic(a, c1, c2, b, k / 12f)); }
        float dev = centres.Max(c => DistToPath(new Vector3(c.x, y, c.z)));
        float targetDev = DistToPath(new Vector3(target.position.x, y, target.position.z));

        // the NavMesh
        var nav = roots.First(g => g.name == "NavMeshSurface" || g.name == "NavmeshSurface");
        nav.SetActive(true);
        Physics.SyncTransforms();
        var data = BakeNavMesh(scene.name);
        UnityEngine.AI.NavMesh.RemoveAllNavMeshData();
        var inst = UnityEngine.AI.NavMesh.AddNavMeshData(data);
        var tp = roots.First(g => g.name == "TeamPos").GetComponentsInChildren<Transform>().Where(t => t.childCount == 0).ToList();
        int off = tp.Count(t => !UnityEngine.AI.NavMesh.SamplePosition(t.position, out var h, 1f, UnityEngine.AI.NavMesh.AllAreas));
        string path = "?";
        var red = tp.FirstOrDefault(t => t.position.z < tp.Average(q => q.position.z)); var blue = tp.FirstOrDefault(t => t.position.z > tp.Average(q => q.position.z));
        if (red && blue && UnityEngine.AI.NavMesh.SamplePosition(red.position, out var ha, 2f, UnityEngine.AI.NavMesh.AllAreas) && UnityEngine.AI.NavMesh.SamplePosition(blue.position, out var hb, 2f, UnityEngine.AI.NavMesh.AllAreas))
        { var np = new UnityEngine.AI.NavMeshPath(); UnityEngine.AI.NavMesh.CalculatePath(ha.position, hb.position, UnityEngine.AI.NavMesh.AllAreas, np); path = np.status.ToString(); }
        int cartOff = pathSamples.Count(q => !UnityEngine.AI.NavMesh.SamplePosition(q, out var h, 2.5f, UnityEngine.AI.NavMesh.AllAreas));
        int snapped = 0;
        var pp = roots.FirstOrDefault(g => g.name == "PathPoints");
        if (pp)
            foreach (Transform t in pp.transform)
                if (!UnityEngine.AI.NavMesh.SamplePosition(t.position, out var h0, 0.5f, UnityEngine.AI.NavMesh.AllAreas) && UnityEngine.AI.NavMesh.SamplePosition(t.position, out var h1, 4f, UnityEngine.AI.NavMesh.AllAreas))
                { t.position = h1.position; snapped++; }
        inst.Remove();
        int bushPts = BushPatrolPoints.Build(scene);
        EditorSceneManager.SaveScene(scene);
        return scene.name + ": path=" + pts.Count + "pts railDev=" + dev.ToString("0.00") + " targetOffPath=" + targetDev.ToString("0.00")
               + " redToBlue=" + path + " spawnsOffNav=" + off + "/" + tp.Count + " cartPathOffNav=" + cartOff + " pathPointsSnapped=" + snapped
               + " bushPoints=" + bushPts + "/offNav=" + BushPatrolPoints.LastOffNav + log;
    }

    // ------------------------------------------------------------------ rail track
    // One continuous procedural mesh that follows the cart path exactly (same cubic Bezier as the fragments), drawn with
    // the shared palette material (FlatKit, like the props): a hand-laid 1960s mine track. Two rails with a real profile
    // (head, web, foot: dark worn iron on the head, rust below), rough-sawn sleepers of uneven length, width and tone, a
    // little askew and chamfered, tie plates with spike heads, fishplates at the rail joints, all bedded in a lumpy
    // ballast strip with ragged edges, coloured for the theme.
    const float RailGauge = 0.95f, RailTop = 0.17f;
    const float SleeperStep = 0.62f;
    // palette cells (order of PALETTE in Tools/IceArt/common.py, 8 x 8 cells)
    const int PWood = 10, PWoodDark = 11, PStone = 13, PStoneDark = 14, PBlack = 19, PMetal = 20, PBasaltDark = 58;
    static Vector2 Cell(int i) => new Vector2((i % 8 + 0.5f) / 8f, 1f - (i / 8 + 0.5f) / 8f);

    class TrackMesh
    {
        public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>();
        /// <summary>a flat quad a-b-c-d, turned to face 'hint'</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int cell, Vector3 hint)
        {
            var nr = Vector3.Cross(b - a, d - a).normalized;
            if (Vector3.Dot(nr, hint) < 0) { var tmp = b; b = d; d = tmp; nr = -nr; }
            int i = v.Count; var u = Cell(cell);
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int k = 0; k < 4; k++) { n.Add(nr); uv.Add(u); }
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });   // clockwise seen from the side the normal faces (Unity front)
        }
        /// <summary>box without a bottom: centre of its base, half extents along r / up / f</summary>
        public void Box(Vector3 b, Vector3 r, Vector3 f, float hl, float hw, float h, int cell)
        {
            Vector3 up = Vector3.up * h, c = b + up * 0.5f;
            Vector3 p(float x, float z, float y) => b + r * x + f * z + Vector3.up * y;
            Quad(p(-hl, -hw, h), p(hl, -hw, h), p(hl, hw, h), p(-hl, hw, h), cell, Vector3.up);
            Quad(p(hl, -hw, 0), p(hl, hw, 0), p(hl, hw, h), p(hl, -hw, h), cell, r);
            Quad(p(-hl, -hw, 0), p(-hl, hw, 0), p(-hl, hw, h), p(-hl, -hw, h), cell, -r);
            Quad(p(-hl, hw, 0), p(hl, hw, 0), p(hl, hw, h), p(-hl, hw, h), cell, f);
            Quad(p(-hl, -hw, 0), p(hl, -hw, 0), p(hl, -hw, h), p(-hl, -hw, h), cell, -f);
        }
    }

    static void PlaceRails(List<Vector3> pts, Theme theme)
    {
        // dense, evenly spaced samples of the full path (y = 0)
        var raw = new List<Vector3>();
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Ctrl(i, out var a, out var b, out var c1, out var c2);
            for (int k = (i == 0 ? 0 : 1); k <= 40; k++) raw.Add(Cubic(a, c1, c2, b, k / 40f));
        }
        var even = new List<Vector3> { raw[0] };
        float step = 0.2f, carry = 0;
        for (int i = 0; i < raw.Count - 1; i++)
        {
            var a = raw[i]; var b = raw[i + 1]; float len = (b - a).magnitude;
            float d = step - carry;
            while (d <= len) { even.Add(Vector3.Lerp(a, b, d / len)); d += step; }
            carry = len - (d - step);
        }
        if ((even[even.Count - 1] - raw[raw.Count - 1]).sqrMagnitude > 1e-4f) even.Add(raw[raw.Count - 1]);
        // extend 0.6 m past both ends so the cart and the target sit on track
        var d0 = (even[0] - even[1]).normalized; var d1 = (even[even.Count - 1] - even[even.Count - 2]).normalized;
        even.Insert(0, even[0] + d0 * 0.6f); even.Add(even[even.Count - 1] + d1 * 0.6f);

        int n = even.Count;
        var tan = new Vector3[n]; var right = new Vector3[n]; var arc = new float[n];
        for (int i = 0; i < n; i++)
        {
            var tv = even[Mathf.Min(i + 1, n - 1)] - even[Mathf.Max(i - 1, 0)]; tv.y = 0; tv.Normalize();
            tan[i] = tv; right[i] = Vector3.Cross(Vector3.up, tv).normalized;
            if (i > 0) arc[i] = arc[i - 1] + (even[i] - even[i - 1]).magnitude;
        }
        var m = new TrackMesh();
        var rnd = new System.Random(Mathf.RoundToInt(arc[n - 1] * 100f) + n);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        // ballast: a lumpy gravel bed with ragged edges (the sleepers sit in it)
        // grey stone everywhere (it must stand out from sand and snow alike), black basalt on the volcano
        int ballast = theme == Theme.Volcano ? PBasaltDark : PMetal;   // neutral mid grey (stone_dark read as a heavy blue band on sand)
        {
            float[] lat = { -1.0f, -0.80f, -0.35f, 0.35f, 0.80f, 1.0f };
            var rows = new Vector3[n][];
            for (int i = 0; i < n; i++)
            {
                rows[i] = new Vector3[lat.Length];
                for (int k = 0; k < lat.Length; k++)
                {
                    float l = lat[k], y = 0.05f;
                    if (k == 0 || k == lat.Length - 1) { l += Mathf.Sign(l) * 0.16f * Mathf.PerlinNoise(arc[i] * 0.9f, k * 3.1f); y = 0.006f; }
                    else y += 0.012f * (Mathf.PerlinNoise(arc[i] * 2.3f, k * 1.7f) - 0.5f) * 2f;
                    rows[i][k] = even[i] + right[i] * l + Vector3.up * y;
                }
            }
            for (int i = 0; i < n - 1; i++)
                for (int k = 0; k < lat.Length - 1; k++)
                    m.Quad(rows[i][k], rows[i + 1][k], rows[i + 1][k + 1], rows[i][k + 1], ballast, Vector3.up);
        }

        // sleepers: rough-sawn, uneven, a little askew, chamfered top edges; tie plates and spikes under each rail
        float acc = SleeperStep * 0.5f; int sleeperNo = 0;
        for (int i = 0; i < n - 1; i++)
        {
            acc += (even[i + 1] - even[i]).magnitude;
            if (acc < SleeperStep) continue;
            acc -= SleeperStep; sleeperNo++;
            float yaw = R(-5f, 5f);
            var rt = Quaternion.Euler(0, yaw, 0) * right[i]; var f = Quaternion.Euler(0, yaw, 0) * tan[i];
            var c = even[i] + right[i] * R(-0.04f, 0.04f);
            float hl = R(0.66f, 0.78f), hw = R(0.12f, 0.16f), h = R(0.062f, 0.078f), ch = 0.022f;
            double tone = rnd.NextDouble();
            int wood = tone < 0.85 ? PWoodDark : PWood;   // weathered dark wood, a few a shade lighter (no light planks: too much contrast)
            float y0 = 0.01f;
            // uneven sawn ends: each corner a little longer or shorter
            float l0 = hl + R(-0.05f, 0.05f), l1 = hl + R(-0.05f, 0.05f), l2 = hl + R(-0.05f, 0.05f), l3 = hl + R(-0.05f, 0.05f);
            Vector3 P(float x, float z, float y) => c + rt * x + f * z + Vector3.up * (y0 + y);
            Vector3[] bot = { P(-l0, -hw, 0), P(l1, -hw, 0), P(l2, hw, 0), P(-l3, hw, 0) };
            Vector3[] mid = { P(-l0, -hw, h - ch), P(l1, -hw, h - ch), P(l2, hw, h - ch), P(-l3, hw, h - ch) };
            Vector3[] top = { P(-l0 + ch, -hw + ch, h), P(l1 - ch, -hw + ch, h), P(l2 - ch, hw - ch, h), P(-l3 + ch, hw - ch, h) };
            var cen = c + Vector3.up * (y0 + h * 0.5f);
            m.Quad(top[0], top[1], top[2], top[3], wood, Vector3.up);
            for (int s = 0; s < 4; s++)
            {
                int s2 = (s + 1) % 4;
                var outw = ((bot[s] + bot[s2]) * 0.5f - cen); outw.y = 0;
                m.Quad(bot[s], bot[s2], mid[s2], mid[s], wood, outw);
                m.Quad(mid[s], mid[s2], top[s2], top[s], wood, outw + Vector3.up * 0.5f);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                var rc = c + rt * (side * RailGauge * 0.5f) + Vector3.up * (y0 + h);
                m.Box(rc, rt, f, 0.13f, 0.09f, 0.012f, PBlack);                                     // tie plate
                foreach (float s in new[] { -0.10f, 0.10f })
                    m.Box(rc + rt * s + f * (s * 0.4f), rt, f, 0.018f, 0.018f, 0.04f, PBlack);      // spike heads
            }
            if (sleeperNo % 9 == 0)   // a rail joint: fishplates either side of each rail's web
                foreach (float side in new[] { -1f, 1f })
                    foreach (float s in new[] { -1f, 1f })
                    {
                        var jc = even[i] + right[i] * (side * RailGauge * 0.5f + s * 0.032f) + Vector3.up * 0.10f;
                        m.Box(jc, right[i], tan[i], 0.012f, 0.24f, 0.028f, PStoneDark);
                    }
        }

        // rails: head / web / foot profile, swept along the path (lat, y); worn dark iron on top, rust below
        var prof = new (float x, float y)[] { (-0.075f, 0.080f), (-0.075f, 0.088f), (-0.022f, 0.098f), (-0.022f, 0.128f), (-0.058f, 0.134f), (-0.058f, 0.160f), (-0.046f, RailTop),
                                              (0.046f, RailTop), (0.058f, 0.160f), (0.058f, 0.134f), (0.022f, 0.128f), (0.022f, 0.098f), (0.075f, 0.088f), (0.075f, 0.080f) };
        int SegCell(int k)
        {
            float ya = prof[k].y, yb = prof[k + 1].y;
            if (ya >= 0.159f && yb >= 0.159f) return PStone;           // the worn running surface and its bevels
            if (Mathf.Min(ya, yb) >= 0.133f) return PStoneDark;        // head sides
            return PWoodDark;                                          // rusty web and foot
        }
        foreach (float side in new[] { -1f, 1f })
        {
            float off = side * RailGauge * 0.5f;
            for (int k = 0; k < prof.Length - 1; k++)
            {
                var pa = prof[k]; var pb = prof[k + 1];
                var mid2 = new Vector2((pa.x + pb.x) * 0.5f, (pa.y + pb.y) * 0.5f) - new Vector2(0f, 0.12f);
                int cell = SegCell(k);
                for (int i = 0; i < n - 1; i++)
                {
                    Vector3 Q(int j, (float x, float y) q) => even[j] + right[j] * (off + q.x) + Vector3.up * q.y;
                    var hint = right[i] * mid2.x + Vector3.up * mid2.y;
                    m.Quad(Q(i, pa), Q(i + 1, pa), Q(i + 1, pb), Q(i, pb), cell, hint);
                }
            }
        }

        string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(scene.path), System.IO.Path.GetFileNameWithoutExtension(scene.path)).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(dir).Replace('\\', '/'), System.IO.Path.GetFileName(dir));
        string meshPath = dir + "/RailTrack.asset";
        // write straight into the existing asset with the Mesh API (CopySerialized left the editor drawing the old data)
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        var mesh = existing ? existing : new Mesh();
        mesh.Clear(); mesh.name = "RailTrack";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(m.v); mesh.SetNormals(m.n); mesh.SetUVs(0, m.uv); mesh.subMeshCount = 1; mesh.SetTriangles(m.t, 0);
        mesh.RecalculateBounds();
        if (existing) EditorUtility.SetDirty(mesh); else AssetDatabase.CreateAsset(mesh, meshPath);

        var go = new GameObject("RailTrack");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.SetParent(env, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        // the shared palette material of the props (FlatKit); Xmas night: its night copy (XmasNight ran before the mode objects)
        var pal = xmasNight ? AssetDatabase.LoadAssetAtPath<Material>(IceThemeSetup.PropsNightMat) : IceThemeSetup.Props;
        mr.sharedMaterial = pal ? pal : RailMat("RailSleeper", new Color(0.55f, 0.36f, 0.22f));
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Finish(go);
        log += " rail=continuous(" + n + "pts)";
    }

    static Material RailMat(string name, Color col)
    {
        string path = "Assets/Maps/Payload/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m) return m;
        if (!AssetDatabase.IsValidFolder("Assets/Maps/Payload")) AssetDatabase.CreateFolder("Assets/Maps", "Payload");
        m = new Material(world) { name = name };
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
        m.mainTexture = null;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        m.color = col;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
