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
            if (d.mode == GameMode.Payload) PlaceRails(pts);
            EditorUtility.SetDirty(mgr);
            log += " path=" + pts.Count + "pts frags=" + frags.Count + " vehicle=" + vehicle.name + " pv=" + string.Join("/", clones.SelectMany(c => c.GetComponentsInChildren<Photon.Pun.PhotonView>(true)).Select(pv => new SerializedObject(pv).FindProperty("sceneViewId").intValue));
        }
        finally { }
    }

    // ------------------------------------------------------------------ rail track
    // One continuous procedural mesh that follows the cart path exactly (same cubic Bezier as the fragments):
    // two steel rails (extruded strips) + wooden sleepers every SleeperStep metres.
    const float RailGauge = 0.95f, RailW = 0.11f, RailTop = 0.17f, RailBase = 0.07f;
    const float SleeperLen = 1.45f, SleeperW = 0.28f, SleeperTop = 0.07f, SleeperStep = 0.62f;

    static void PlaceRails(List<Vector3> pts)
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
        var tan = new Vector3[n]; var right = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            var tv = even[Mathf.Min(i + 1, n - 1)] - even[Mathf.Max(i - 1, 0)]; tv.y = 0; tv.Normalize();
            tan[i] = tv; right[i] = Vector3.Cross(Vector3.up, tv).normalized;
        }

        var v = new List<Vector3>(); var nm = new List<Vector3>();
        var triSteel = new List<int>(); var triWood = new List<int>();

        // rails: top + inner + outer side, continuous strip
        foreach (float side in new[] { -1f, 1f })
        {
            float off = side * RailGauge * 0.5f;
            void Strip(System.Func<int, Vector3> pa, System.Func<int, Vector3> pb, System.Func<int, Vector3> normal)
            {
                int b0 = v.Count;
                for (int i = 0; i < n; i++) { v.Add(pa(i)); v.Add(pb(i)); var q = normal(i); nm.Add(q); nm.Add(q); }
                for (int i = 0; i < n - 1; i++)
                {
                    int a = b0 + i * 2;
                    // orient each quad so it faces its normal
                    var fn = Vector3.Cross(v[a + 2] - v[a], v[a + 1] - v[a]);
                    if (Vector3.Dot(fn, nm[a]) >= 0) triSteel.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                    else triSteel.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }
            Vector3 P(int i, float lat, float y) => even[i] + right[i] * (off + lat) + Vector3.up * y;
            Strip(i => P(i, -RailW / 2, RailTop), i => P(i, RailW / 2, RailTop), i => Vector3.up);
            Strip(i => P(i, -RailW / 2, RailBase), i => P(i, -RailW / 2, RailTop), i => -right[i]);
            Strip(i => P(i, RailW / 2, RailBase), i => P(i, RailW / 2, RailTop), i => right[i]);
        }

        // sleepers: boxes every SleeperStep along the arc length
        float acc = SleeperStep * 0.5f;
        for (int i = 0; i < n - 1; i++)
        {
            float seg = (even[i + 1] - even[i]).magnitude;
            acc += seg;
            if (acc < SleeperStep) continue;
            acc -= SleeperStep;
            var c = even[i]; var f = tan[i]; var rt = right[i];
            Box(v, nm, triWood, c + Vector3.up * (SleeperTop * 0.5f + 0.01f), rt * (SleeperLen * 0.5f), Vector3.up * (SleeperTop * 0.5f), f * (SleeperW * 0.5f));
        }

        var mesh = new Mesh { name = "RailTrack" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(v); mesh.SetNormals(nm);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(triWood, 0); mesh.SetTriangles(triSteel, 1);
        mesh.RecalculateBounds();
        string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(scene.path), System.IO.Path.GetFileNameWithoutExtension(scene.path)).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(dir).Replace('\\', '/'), System.IO.Path.GetFileName(dir));
        string meshPath = dir + "/RailTrack.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing) { EditorUtility.CopySerialized(mesh, existing); existing.name = "RailTrack"; EditorUtility.SetDirty(existing); mesh = existing; }
        else AssetDatabase.CreateAsset(mesh, meshPath);

        var go = new GameObject("RailTrack");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.SetParent(env, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new[] { RailMat("RailSleeper", new Color(0.66f, 0.40f, 0.22f)), RailMat("RailSteel", new Color(0.58f, 0.63f, 0.74f)) };
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Finish(go);
        log += " rail=continuous(" + n + "pts)";
    }

    static void Box(List<Vector3> v, List<Vector3> nm, List<int> tri, Vector3 c, Vector3 ex, Vector3 ey, Vector3 ez)
    {
        // 5 faces (no bottom)
        void Face(Vector3 fc, Vector3 u, Vector3 w)
        {
            var nrm = Vector3.Cross(u, w).normalized;
            if (Vector3.Dot(nrm, fc - c) < 0) { var tmp = u; u = w; w = tmp; nrm = -nrm; }
            int b = v.Count;
            v.Add(fc - u - w); v.Add(fc + u - w); v.Add(fc + u + w); v.Add(fc - u + w);
            for (int k = 0; k < 4; k++) nm.Add(nrm);
            tri.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
        }
        Face(c + ey, ex, ez);
        Face(c + ex, ey, ez); Face(c - ex, ey, ez);
        Face(c + ez, ex, ey); Face(c - ez, ex, ey);
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
