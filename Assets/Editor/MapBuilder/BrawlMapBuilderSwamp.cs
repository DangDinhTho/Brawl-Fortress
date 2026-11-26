using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Swamp theme (a toxic bog and a glowing mushroom wood at dusk, 1960s-70s backwoods). Props from Tools/IceArt/props16.py
/// (same folder and palette as the other themes, set up by IceThemeSetup + SwampThemeSetup): mossy boulders, giant
/// mushroom clusters, cypress root knots and log stacks (one family per wall), stumps, a rowboat, rusty drums, a moonshine
/// still, fallen logs, giant glowing mushrooms, bald cypresses, lantern posts, reeds (BogGrass) as the bushes.
/// Mechanic: bog puddles on the open floor slow whoever wades through (BogPuddle.cs), mirrored for both teams.
/// Border: low mossy banks (SwampBankA/B/C) with an acid lake beyond them, the SwampGate behind each spawn; big
/// cypresses, a stilt shack and giant mushrooms out in the acid.
/// </summary>
public static partial class BrawlMapBuilder
{
    public static readonly Def[] SwampMaps =
    {
        new Def {   // a new layout: open ground in the middle of each half for the bog puddles
            mapEnum = "Map_Brawl_GlowshroomBog", displayName = "Glowshroom Bog", icon = "map_glowshroombog", mode = GameMode.Competitive, theme = Theme.Swamp, seed = 2001,
            names = new[] { "Glowshroom Bog", "Болото светогрибов", "บึงเห็ดเรืองแสง", "Ciénaga de Hongos", "Đầm Nấm Phát Sáng" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##...bbbbbbb...##..",
                "..#....bbbbbbb....#..",
                ".........x.x.........",
                ".bbb.t.........t.bbb.",
                ".bbb.............bbb.",
                "......##.....##......",
                "..f...............f..",
                "..x..bb.......bb..x..",
                ".##..bb...t...bb..##.",
                ".....................",
            },
            center = "....##.........##....",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_ToadstoolRaid", displayName = "Toadstool Raid", icon = "map_toadstoolraid", mode = GameMode.CaptureTheFlag, theme = Theme.Swamp, seed = 2002,
            names = new[] { "Toadstool Raid", "Набег на поганки", "บุกเห็ดพิษ", "Asalto a los Hongos", "Đột Kích Nấm Độc" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "...##...........##...",
                "...#....bbbbb....#...",
                ".bb.....bbbbb.....bb.",
                ".bb..x.........x..bb.",
                "......##.....##......",
                "..t.......f.......t..",
                ".....bbb.....bbb.....",
                "..##.bbb.....bbb.##..",
                "........x...x........",
                ".bb.....#...#.....bb.",
            },
            center = ".bb...............bb.",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_MireCrown", displayName = "Mire Crown", icon = "map_mirecrown", mode = GameMode.KingOfTheHill, theme = Theme.Swamp, seed = 2003,
            names = new[] { "Mire Crown", "Корона трясины", "มงกุฎหนองน้ำ", "Corona del Lodazal", "Vương Miện Đầm Lầy" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bbb...........bbb..",
                "..bbb...##.##...bbb..",
                "......t.......t......",
                ".##...............##.",
                ".#....bb.....bb....#.",
                "......bb..x..bb......",
                "..x...............x..",
                "....##...bbb...##....",
                "........bbbbb........",
                "...f.............f...",
            },
            center = ".....#.........#.....",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_BayouExpress", displayName = "Bayou Express", icon = "map_bayouexpress", mode = GameMode.Payload, theme = Theme.Swamp, seed = 2004,
            pathHalf = new[] { new Vector2(0, 16.5f), new Vector2(-8, 10), new Vector2(-9, 2) },
            names = new[] { "Bayou Express", "Экспресс по байу", "รถด่วนบึง", "Expreso del Bayou", "Tàu Tốc Hành Đầm Lầy" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "...##.....x.....##...",
                "...#..bb.......bb....",
                ".bbb..bb..t..........",
                ".bbb.........x.......",
                "......##..#..........",
                "..t.......#.......bb.",
                ".......bbb...........",
                ".##....bbb..f........",
                "....x.............bb.",
                "..bb.....#.#.........",
            },
            center = "...bb...........bb...",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_SwampMill", displayName = "Swamp Mill", icon = "map_swampmill", mode = GameMode.MannVsMachine, theme = Theme.Swamp, seed = 2005,
            pathHalf = new[] { new Vector2(0, 16.5f), new Vector2(0, 5) },
            names = new[] { "Swamp Mill", "Болотная мельница", "โรงสีหนองน้ำ", "Molino del Pantano", "Xưởng Cối Đầm Lầy" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb...x.....x...bbb.",
                ".bbb.............bbb.",
                "....##.........##....",
                "..t....bb...bb....t..",
                "..#....bb...bb....#..",
                "..#...............#..",
                ".....f.........f.....",
                ".bbb...##...##...bbb.",
                ".bbb.............bbb.",
                "......x.......x......",
            },
            center = "...#...bb...bb...#...",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_RotwoodArena", displayName = "Rotwood Arena", icon = "map_rotwoodarena", mode = GameMode.JungleBoss, theme = Theme.Swamp, seed = 2006,
            names = new[] { "Rotwood Arena", "Арена гнилого леса", "สังเวียนป่าผุ", "Arena del Bosque Podrido", "Đấu Trường Rừng Mục" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bb.......x.......bb.",
                ".bb..##.......##..bb.",
                ".....#.........#.....",
                "..t.....bbbbb.....t..",
                "........bbbbb........",
                ".##...............##.",
                ".#...x.........x...#.",
                ".....................",
                "....bb....t....bb....",
                "....bb.........bb....",
            },
            center = "........#...#........",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_GhoulBayou", displayName = "Ghoul Bayou", icon = "map_ghoulbayou", mode = GameMode.Zombie, theme = Theme.Swamp, seed = 2007,
            names = new[] { "Ghoul Bayou", "Байу упырей", "บึงผีดิบ", "Bayou de los Espectros", "Đầm Lầy Xác Sống" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..x....bbbbbbb....x..",
                "....##.........##....",
                ".bb..#.........#..bb.",
                ".bb.....t...t.....bb.",
                "......f.......f......",
                "..##.....###.....##..",
                ".....................",
                ".bbb...bb...bb...bbb.",
                ".bbb.............bbb.",
                ".....x.........x.....",
            },
            center = "..#.....bbbbb.....#..",
        },
    };

    static readonly List<(Vector3 p, float r)> puddles = new List<(Vector3, float)>();

    [MenuItem("Tools/Maps/Build Swamp Maps")]
    static void BuildSwampMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Swamp] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Swamp] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in SwampMaps) Debug.Log(Build(d));
    }

    static void SwampTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':
                {
                    // one family per wall (PrepareVolcanoWalls): boulders (0 and 4), mushroom clusters (1), root knots (2), log stacks (3)
                    int fam = wallFamily.TryGetValue(TileOf(p), out var f) ? f : 0;
                    var jit = p + new Vector3(Jit(rnd, 0.03f), 0, Jit(rnd, 0.03f));
                    float sc = 0.96f + (float)rnd.NextDouble() * 0.08f;
                    if (fam == 1) Prefab("ShroomCluster", jit, rnd.Next(360), sc, true);
                    else if (fam == 2) Prefab("RootTangle", jit, rnd.Next(360), sc, true);
                    else if (fam == 3) Prefab("LogStack", jit, fenceRot + (rnd.Next(2) * 180f) + Jit(rnd, 4), 1f, true);
                    else Prefab(v < 0.5 ? "MossBoulderA" : "MossBoulderB", jit, rnd.Next(360), sc, true);
                }
                break;
            case 'x':
                if (v < 0.30) Prefab("Stump", p, rnd.Next(360), 1f, true);
                else if (v < 0.50) Prefab("RowBoat", p, rnd.Next(360), 0.95f, true);
                else if (v < 0.75) Prefab("RustyDrums", p, rnd.Next(360), 1f, true);
                else Prefab("MoonshineStill", p, rnd.Next(360), 0.95f, true);
                break;
            case 'f': Prefab("FallenLog", p, fenceRot + Jit(rnd, 6), 0.95f, true); break;
            case 't':
                if (v < 0.30) Prefab("GiantShroomA", p, rnd.Next(360), 1f, true);
                else if (v < 0.55) Prefab("GiantShroomB", p, rnd.Next(360), 1f, true);
                else if (v < 0.80) Prefab("SwampCypress", p, rnd.Next(360), 1f, true);
                else Prefab("SwampLantern", p, baseRot + Jit(rnd, 30), 1f, true);
                break;
            case 'b': Grass("BogGrass", p, 1.3f, rnd); break;   // ~1.95 m, like the other bushes
        }
    }

    static void SwampDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "ShroomPatch", "MossClump", "Cattails", "SwampStones", "MossClump", "ShroomPatch");
        if (!prefabs.ContainsKey(k)) k = "SwampStones";
        Prefab(k, o, rot + rnd.Next(360), k == "ShroomPatch" ? 0.8f + (float)rnd.NextDouble() * 0.4f : 1f, false);
    }

    static readonly Color SwampFog = new Color(0.20f, 0.30f, 0.28f);

    /// <summary>Dusk over the bog: a cool green-teal ambient (bright enough to read the props), a green haze, a moonlit
    /// sun; the gradient sky (the map scenes have no camera of their own).</summary>
    static void SwampLook(Light light)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.66f, 0.74f, 0.76f); RenderSettings.ambientEquatorColor = new Color(0.55f, 0.64f, 0.60f); RenderSettings.ambientGroundColor = new Color(0.42f, 0.46f, 0.42f);
        if (light) light.color = new Color(0.86f, 0.93f, 1.0f);
        SwampSky();
    }

    static void SwampSky()
    {
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(SwampThemeSetup.SkyMat);
        RenderSettings.fogColor = SwampFog;
        foreach (var cam in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)))
        { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = SwampFog; }
    }

    /// <summary>After the mode objects are in: the twilight props material everywhere, light pools under the glowing
    /// mushrooms (green) and the lanterns (warm).</summary>
    static void SwampTwilight()
    {
        var from = AssetDatabase.LoadAssetAtPath<Material>(IceThemeSetup.PropsMat);
        var to = AssetDatabase.LoadAssetAtPath<Material>(SwampThemeSetup.PropsMat);
        int n = 0;
        if (from && to)
            foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
            {
                var mats = r.sharedMaterials; bool ch = false;
                for (int i = 0; i < mats.Length; i++) if (mats[i] == from) { mats[i] = to; ch = true; }
                if (ch) { r.sharedMaterials = mats; n++; }
            }
        int pools = 0;
        foreach (Transform t in env.Cast<Transform>().ToList())
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            if (!src) continue;
            string sn = src.name;
            float size = sn == "GiantShroomA" ? 4.2f : sn == "GiantShroomB" ? 3.8f : sn == "ShroomCluster" ? 2.8f : sn == "SwampGate" ? 6.5f : sn == "SwampLantern" ? 3.2f : 0f;
            if (size <= 0f) continue;
            string pool = sn == "SwampLantern" ? "WarmPool" : "GlowPool";
            if (!prefabs.ContainsKey(pool)) continue;
            var go = Prefab(pool, t.position + Vector3.up * 0.05f, 0f, 1f, false);
            if (!go) continue;
            go.transform.localScale = new Vector3(size * t.lossyScale.x, 1f, size * t.lossyScale.z);
            foreach (var c in go.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = 0;
            pools++;
        }
        log += " twilight=" + n + " pools=" + pools;
    }

    /// <summary>Beyond the mossy banks: an acid lake all round (it starts under the banks, so none shows inside the arena),
    /// big cypresses, a stilt shack and giant mushrooms standing in it. Also the gateway behind each team's spawn.</summary>
    static void PlaceSwampBackdrop(int seed)
    {
        PlaceGates("SwampGate");
        SwampSky();
        var banks = env.Cast<Transform>().Where(t => t.name.StartsWith("SwampBank")).ToList();
        float left = banks.Where(t => t.position.x < 0).Select(t => -t.position.x).DefaultIfEmpty(17f).Max();
        float right = banks.Where(t => t.position.x > 0).Select(t => t.position.x).DefaultIfEmpty(17f).Max();
        int lakes = 0;
        void Lake(float x0, float x1, float z0, float z1)
        {
            var go = Prefab("AcidLake", new Vector3((x0 + x1) / 2f, 0.03f, (z0 + z1) / 2f), 0f, 1f, false);
            if (go) { go.transform.localScale = new Vector3(Mathf.Abs(x1 - x0), 1f, Mathf.Abs(z1 - z0)); foreach (var c in go.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = 0; lakes++; }
        }
        Lake(-left, -90f, -90f, 90f);
        Lake(right, 90f, -90f, 90f);
        EndLakes(Lake, left, right, 90f);   // the ends, with a floor inlet under the gate
        log += " acidLakes=" + lakes;

        var rnd = new Random(seed);
        for (int s = -1; s <= 1; s += 2)   // a shack on stilts beyond each long side (mirrored)
            Prefab("StiltShack", new Vector3(s * 27f, 0f, s * 6f), s > 0 ? -90f : 90f, 1.15f, false);
        for (int i = 0; i < 30; i++)
        {
            float a = i / 30f * Mathf.PI * 2f + Jit(rnd, 0.07f);
            float rad = 30f + (float)rnd.NextDouble() * 12f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.9f, 0f, Mathf.Sin(a) * rad);
            if (Mathf.Abs(p.x) < Mathf.Max(left, right) + 3f && Mathf.Abs(p.z) < 28f) continue;   // out in the acid
            if (Mathf.Abs(Mathf.Abs(p.x) - 27f) < 4f && Mathf.Abs(Mathf.Abs(p.z) - 6f) < 5f) continue;   // the shacks
            var k = Pick(rnd, "BigCypress", "BigCypress", "BigCypress", "GiantShroomA", "GiantShroomB", "MossBoulderA", "MossBoulderB", "SwampCypress");
            if (!prefabs.ContainsKey(k)) k = "MossBoulderA";
            float sc = k == "BigCypress" ? 0.9f + (float)rnd.NextDouble() * 0.5f : k.StartsWith("GiantShroom") ? 2.0f + (float)rnd.NextDouble() * 0.8f : k.StartsWith("MossBoulder") ? 1.8f + (float)rnd.NextDouble() * 1.0f : 1.6f;
            Prefab(k, p, rnd.Next(360), sc, false);
        }
    }

    /// <summary>Bog puddles that slow whoever wades through: two per half on open floor (the tile free, at most three solid
    /// neighbours, nothing standing within the puddle), at least 5 m from the spawns, 2.5 m from a payload / robot path
    /// and 6 m from each other, mirrored into the other half; on Competitive maps one more in the middle when it is open.
    /// Floor decor under them goes; a few cattails stand at their edges.</summary>
    static void PlaceBogPuddles(Def d)
    {
        puddles.Clear();
        if (!prefabs.ContainsKey("BogPuddle")) { log += " [missing BogPuddle]"; return; }
        var g = FullGrid(d);
        var rnd = new Random(d.seed * 31 + 7);
        bool Clear(Vector3 p, float r) => !Physics.CheckBox(p + Vector3.up * 1.2f, new Vector3(r, 0.8f, r), Quaternion.identity, 1 << WallLayer, QueryTriggerInteraction.Ignore);
        var cands = new List<(Vector3 p, float r, double score)>();
        for (int row = 3; row <= 12; row++)
            for (int c = 1; c < COLS - 1; c++)
            {
                if (g[row, c] != '.') continue;
                int solid = 0;
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++) { char ch = g[row + dr, c + dc]; if (ch != '.' && ch != 'b') solid++; }
                if (solid > 3) continue;
                var p = TilePos(row, c);
                if (Mathf.Abs(p.x) > 12.5f || new Vector2(p.x, p.z).magnitude < 4f) continue;
                if (spawnPoints.Any(sp => { var q = sp - p; q.y = 0; return q.magnitude < 5f; })) continue;
                if (spawnPoints.Any(sp => { var q = sp + p; q.y = 0; return q.magnitude < 5f; })) continue;
                if (pathSamples != null && (DistToPath(p) < 2.5f || DistToPath(-p) < 2.5f)) continue;
                float r = 1.6f + (float)rnd.NextDouble() * 0.5f;
                if (!Clear(p, r * 0.8f) || !Clear(-p, r * 0.8f)) { r = 1.35f; if (!Clear(p, 1.05f) || !Clear(-p, 1.05f)) continue; }
                double score = -Mathf.Abs(p.z + 7.5f) * 0.55 - Mathf.Abs(Mathf.Abs(p.x) - 4.5f) * 0.3 + rnd.NextDouble() * 0.8;
                cands.Add((p, r, score));
            }
        var chosen = new List<(Vector3 p, float r)>();
        foreach (var cnd in cands.OrderByDescending(x => x.score))
        {
            if (chosen.Count >= 2) break;
            if (chosen.Any(q => (q.p - cnd.p).magnitude < 6f || (q.p + cnd.p).magnitude < 6f)) continue;
            chosen.Add((cnd.p, cnd.r));
        }
        var list = new List<(Vector3 p, float r)>();
        foreach (var c in chosen) { list.Add(c); list.Add((-c.p, c.r)); }
        if (d.mode == GameMode.Competitive && g[13, 10] == '.' && Clear(Vector3.zero, 1.5f)) list.Add((Vector3.zero, 2.0f));
        foreach (var (p, r) in list)
        {
            var go = Prefab("BogPuddle", p, (p.x * 37f + p.z * 11f) % 360f, r, false);
            if (!go) continue;
            go.transform.rotation = Quaternion.Euler(0f, Mathf.Repeat(Mathf.Abs(p.x) * 37f + Mathf.Abs(p.z) * 11f, 360f) + (p.z > 0 ? 180f : 0f), 0f);   // mirrored puddles look alike
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;
            puddles.Add((p, r));
            // no floor decor in the mud (bushes stay)
            foreach (Transform t in env.Cast<Transform>().ToList())
            {
                if (t == go.transform) continue;
                var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                string sn = src ? src.name : t.name;
                if ((sn == "ShroomPatch" || sn == "MossClump" || sn == "Cattails" || sn == "SwampStones") && (t.position - p).sqrMagnitude < (r + 0.4f) * (r + 0.4f)) Object.DestroyImmediate(t.gameObject);
            }
        }
        // cattails at the edges (the same spots mirrored)
        int cat = 0;
        foreach (var (p, r) in puddles.Where(q => q.p.z <= 0f).ToList())
        {
            var cr = new Random(Mathf.RoundToInt(Mathf.Abs(p.x) * 100 + Mathf.Abs(p.z) * 7));
            for (int i = 0; i < 3; i++)
            {
                float a = (float)cr.NextDouble() * Mathf.PI * 2f;
                var e = p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r * 1.02f);
                foreach (var q in new[] { e, -e })
                {
                    if (!Clear(q, 0.3f) || spawnPoints.Any(sp => { var w = sp - q; w.y = 0; return w.magnitude < 2f; })) continue;
                    Prefab("Cattails", q, (float)cr.NextDouble() * 360f, 0.9f, false); cat++;
                }
            }
        }
        log += " puddles=" + puddles.Count + " cattails=" + cat;
    }

    /// <summary>Moss, small mushrooms, cattails and stones on the open peat (no colliders), clear of walls, spawns and puddles.</summary>
    static void ScatterSwampFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        for (int i = 0; i < 180 && placed < 16; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            if (puddles.Any(q => (q.p - p).magnitude < q.r + 0.6f)) continue;
            var k = Pick(rnd, "ShroomPatch", "MossClump", "Cattails", "SwampStones", "MossClump");
            if (!prefabs.ContainsKey(k)) k = "SwampStones";
            Prefab(k, p, rnd.Next(360), k == "ShroomPatch" ? 0.9f : 1f, false);
            placed++;
        }
        log += " swampScatter=" + placed;
    }
}
