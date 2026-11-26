using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Savanna theme (wild Africa, a 1960s-70s game reserve). Props from Tools/IceArt/props20.py (same folder and palette as
/// the other themes, set up by IceThemeSetup + SavannaThemeSetup): granite kopjes, thorn-branch bomas, red termite
/// mounds and expedition stores (one family per wall), boulders, dead logs, an elephant skull, water barrels, camp
/// crates, thorn hedges, baobabs, umbrella acacias, termite spires, candelabra euphorbias, golden elephant grass as the
/// bushes. Mechanic: water holes on the open plain that slowly heal whoever stands in them (WaterHole.cs), mirrored for
/// both teams. Border: low red-earth banks crowned with boulders (SavannaRidgeA/B/C), the SafariGate behind each spawn;
/// giant baobabs, giraffes, elephants and an expedition camp out on the plain. Lighting: the bright neutral desert
/// daylight (EgyptLook / EgyptSun) under a clear sky.
/// </summary>
public static partial class BrawlMapBuilder
{
    public static readonly Def[] SavannaMaps =
    {
        new Def {   // a new layout: open ground in each half for the water holes, one in the middle
            mapEnum = "Map_Brawl_BaobabGrove", displayName = "Baobab Grove", icon = "map_baobabgrove", mode = GameMode.Competitive, theme = Theme.Savanna, seed = 2401,
            names = new[] { "Baobab Grove", "Баобабовая роща", "ป่าเบาบับ", "Bosque de Baobabs", "Rừng Bao Báp" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".##.....bbbbb.....##.",
                ".#.......bbb.......#.",
                "......x.......x......",
                "..t...............t..",
                ".........###.........",
                ".bb...............bb.",
                ".bb...............bb.",
                ".....##..x.x..##.....",
                "..t......bbb......t..",
                "........x...x........",
            },
            center = "...bb...#...#...bb...",
        },
        new Def {   // a new layout: a water hole on each flank of the middle
            mapEnum = "Map_Brawl_SafariRaid", displayName = "Safari Raid", icon = "map_safariraid", mode = GameMode.CaptureTheFlag, theme = Theme.Savanna, seed = 2402,
            names = new[] { "Safari Raid", "Сафари-налёт", "บุกซาฟารี", "Asalto Safari", "Đột Kích Safari" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##...x.....x...##..",
                "..#.....bbbbb.....#..",
                ".........bbb.........",
                ".t.................t.",
                "........##.##........",
                ".bb...............bb.",
                ".bb.......t.......bb.",
                "....x...........x....",
                "..##....bbbbb....##..",
                ".....................",
            },
            center = "...t....#...#....t...",
        },
        new Def {   // a new layout: water holes on the way up to the hill
            mapEnum = "Map_Brawl_TermiteHill", displayName = "Termite Hill", icon = "map_termitehill", mode = GameMode.KingOfTheHill, theme = Theme.Savanna, seed = 2403,
            names = new[] { "Termite Hill", "Холм термитов", "เนินจอมปลวก", "Colina de Termitas", "Đồi Mối" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".##.......#.......##.",
                ".#....bb.....bb....#.",
                "......bb..x..bb......",
                "..t...............t..",
                ".....................",
                ".bb.....#...#.....bb.",
                ".bb.....#...#.....bb.",
                ".....................",
                "...x....bb.bb....x...",
                "..t.....f...f.....t..",
            },
            center = "....##.........##....",
        },
        new Def {   // a new layout (path B): the water holes kept 4 m off the track
            mapEnum = "Map_Brawl_SavannaExpress", displayName = "Savanna Express", icon = "map_savannaexpress", mode = GameMode.Payload, theme = Theme.Savanna, seed = 2404,
            pathHalf = new[] { new Vector2(6, 16.5f), new Vector2(6, 8), new Vector2(-6, 5), new Vector2(-6, 1) },
            names = new[] { "Savanna Express", "Саванный экспресс", "รถด่วนสะวันนา", "Expreso de la Sabana", "Tàu Tốc Hành Thảo Nguyên" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##...........##....",
                "..#......x.......#...",
                ".........bb..........",
                ".t........bb.........",
                ".....................",
                "..............##.x...",
                ".....................",
                ".bb...x..............",
                ".bb.......bb.........",
                ".t......x............",
            },
            center = "..bb.............bb..",
        },
        new Def {   // a new layout (path A): the robots never walk through the water
            mapEnum = "Map_Brawl_RangerStation", displayName = "Ranger Station", icon = "map_rangerstation", mode = GameMode.MannVsMachine, theme = Theme.Savanna, seed = 2405,
            pathHalf = new[] { new Vector2(0, 16.5f), new Vector2(0, 9), new Vector2(-6, 4) },
            names = new[] { "Ranger Station", "Станция рейнджеров", "สถานีเรนเจอร์", "Estación de Guardabosques", "Trạm Kiểm Lâm" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##..x.......x..##..",
                "..#...............#..",
                ".......bb.....bb.....",
                ".t.................t.",
                "......#.......#......",
                ".bb..................",
                ".bb.............##...",
                "....x.............x..",
                "...bb....x...........",
                "..t...bb.............",
            },
            center = "...b...#.....#...b...",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_LionsDen", displayName = "Lion's Den", icon = "map_lionsden", mode = GameMode.JungleBoss, theme = Theme.Savanna, seed = 2406,
            names = new[] { "Lion's Den", "Логово льва", "ถ้ำสิงโต", "Guarida del León", "Hang Sư Tử" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "....x...........x....",
                "..##....bbbbb....##..",
                "..#...............#..",
                "......t.......t......",
                ".....................",
                ".bb...............bb.",
                ".bb......###......bb.",
                ".....................",
                "....x...bb.bb...x....",
                "..t...............t..",
            },
            center = ".....##.......##.....",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_BonePlains", displayName = "Bone Plains", icon = "map_boneplains", mode = GameMode.Zombie, theme = Theme.Savanna, seed = 2407,
            names = new[] { "Bone Plains", "Равнина костей", "ทุ่งกระดูก", "Llanura de Huesos", "Đồng Bằng Xương" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".##..x.........x..##.",
                ".#.................#.",
                "....bbb.......bbb....",
                "....bbb...t...bbb....",
                ".....................",
                "..t...............t..",
                "......##.....##......",
                ".bb...............bb.",
                ".bb.......x.......bb.",
                "....##.........##....",
            },
            center = "..x...#.......#...x..",
        },
    };

    static readonly List<(Vector3 p, float r)> waterHoles = new List<(Vector3, float)>();

    [MenuItem("Tools/Maps/Build Savanna Maps")]
    static void BuildSavannaMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Savanna] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Savanna] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in SavannaMaps) Debug.Log(Build(d));
    }

    static void SavannaTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':
                {
                    // one family per wall (PrepareVolcanoWalls): kopje boulders (0 and 4), thorn bomas (1), termite mounds (2), expedition stores (3)
                    int fam = wallFamily.TryGetValue(TileOf(p), out var f) ? f : 0;
                    var jit = p + new Vector3(Jit(rnd, 0.02f), 0, Jit(rnd, 0.02f));
                    if (fam == 1) Prefab("ThornBoma", jit, fenceRot + rnd.Next(2) * 180f + Jit(rnd, 4), 1f, true);
                    else if (fam == 2) Prefab("TermiteMound", jit, rnd.Next(360), 0.95f + (float)rnd.NextDouble() * 0.1f, true);
                    else if (fam == 3) Prefab("SafariStack", jit, Rot90(rnd) + Jit(rnd, 4), 1f, true);
                    else Prefab(fam == 4 || v < 0.35 ? "KopjeRockB" : "KopjeRockA", jit, rnd.Next(360), 0.96f + (float)rnd.NextDouble() * 0.08f, true);
                }
                break;
            case 'x':
                if (v < 0.26) Prefab("SavannaBoulder", p, rnd.Next(360), 1f, true);
                else if (v < 0.46) Prefab("DeadLog", p, rnd.Next(360), 0.95f, true);
                else if (v < 0.62) Prefab("ElephantSkull", p, baseRot + 180f + Jit(rnd, 35), 0.95f, true);
                else if (v < 0.81) Prefab("WaterBarrels", p, rnd.Next(360), 1f, true);
                else Prefab("SafariCrate", p, Rot90(rnd) + Jit(rnd, 10), 1f, true);
                break;
            case 'f': Prefab("ThornHedge", p, fenceRot + Jit(rnd, 5), 1f, true); break;
            case 't':
                if (v < 0.32) Prefab("BaobabTree", p, rnd.Next(360), 0.95f + (float)rnd.NextDouble() * 0.1f, true);
                else if (v < 0.60) Prefab("AcaciaTree", p, rnd.Next(360), 1f, true);
                else if (v < 0.80) Prefab("TermiteSpire", p, rnd.Next(360), 1f, true);
                else Prefab("Euphorbia", p, rnd.Next(360), 1f, true);
                break;
            case 'b': Grass("SavannaGrass", p, 1.3f, rnd); break;   // ~1.95 m, like the other bushes
        }
    }

    static void SavannaDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "StrawTuft", "AloePlant", "HornSkull", "SavannaStones", "StrawTuft", "SavannaStones");
        if (!prefabs.ContainsKey(k)) k = "SavannaStones";
        float sc = k == "StrawTuft" ? 0.32f + (float)rnd.NextDouble() * 0.1f : 1f;
        Prefab(k, o, rot + rnd.Next(360), sc, false);
    }

    static readonly Color SavannaFog = new Color(0.90f, 0.87f, 0.78f);

    /// <summary>A clear savanna sky with a pale warm haze at the horizon (the bright desert daylight comes from EgyptLook).</summary>
    static void SavannaSky()
    {
        var sky = AssetDatabase.LoadAssetAtPath<Material>(SavannaThemeSetup.SkyMat);
        if (sky) RenderSettings.skybox = sky;
        RenderSettings.fogColor = SavannaFog;
        foreach (var cam in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)))
        { cam.clearFlags = CameraClearFlags.Skybox; cam.backgroundColor = SavannaFog; }
    }

    /// <summary>Beyond the red-earth banks the plain goes on (the floor is 100 m wide, the haze hides its edge): a giant
    /// baobab off each end, an expedition camp, giraffes and elephants on the sides, acacias, baobabs, termite spires and
    /// kopjes around. Also the gate behind each spawn.</summary>
    static void PlaceSavannaBackdrop(int seed)
    {
        PlaceGates("SafariGate");
        SavannaSky();
        var rnd = new Random(seed);
        for (int s = -1; s <= 1; s += 2)   // mirrored, so both teams look at the same things
        {
            Prefab("BaobabGiant", new Vector3(s * 9f, 0f, s * 42f), rnd.Next(360), 1f, false);
            Prefab("SafariCamp", new Vector3(s * 31f, 0f, -s * 9f), s > 0 ? 250f : 70f, 1f, false);
            Prefab("Giraffe", new Vector3(-s * 27f, 0f, s * 10f), s > 0 ? 200f : 20f, 1f, false);
            Prefab("Giraffe", new Vector3(-s * 30.5f, 0f, s * 15f), s > 0 ? 160f : 340f, 0.9f, false);
            Prefab("Elephant", new Vector3(-s * 25.5f, 0f, -s * 4f), s > 0 ? 100f : 280f, 1.35f, false);   // big enough to show over the banks
            Prefab("Elephant", new Vector3(s * 24.5f, 0f, s * 24f), s > 0 ? 300f : 120f, 1.15f, false);
            Prefab("AcaciaTree", new Vector3(-s * 25f, 0f, s * 13f), rnd.Next(360), 1.6f, false);   // the giraffes browse at it
        }
        for (int i = 0; i < 34; i++)
        {
            float a = i / 34f * Mathf.PI * 2f + Jit(rnd, 0.08f);
            float rad = 30f + (float)rnd.NextDouble() * 14f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.9f, 0f, Mathf.Sin(a) * rad);
            if (Mathf.Abs(p.x) < 22f && Mathf.Abs(p.z) < 28f) continue;
            if (Mathf.Abs(Mathf.Abs(p.x) - 30f) < 7f && Mathf.Abs(p.z) < 20f) continue;   // the camp and the animals
            if (Mathf.Abs(p.x) < 14f && Mathf.Abs(p.z) > 36f) continue;                  // the giant baobabs
            var k = Pick(rnd, "AcaciaTree", "BaobabTree", "TermiteSpire", "KopjeRockA", "AcaciaTree", "KopjeRockB", "Euphorbia", "AcaciaTree");
            float sc = k.StartsWith("Kopje") ? 2.2f + (float)rnd.NextDouble() * 1.0f : k == "BaobabTree" ? 1.5f + (float)rnd.NextDouble() * 0.6f : 1.3f + (float)rnd.NextDouble() * 0.5f;
            Prefab(k, p, rnd.Next(360), sc, false);
        }
    }

    /// <summary>Water holes that heal whoever stands in them: two per half on open floor (the tile free, at most three
    /// solid neighbours, away from spawns, the middle and a payload / robot path), mirrored, plus one in the middle on
    /// Competitive when it is free. Reeds at the edges; no floor decor in the water.</summary>
    static void PlaceWaterHoles(Def d)
    {
        waterHoles.Clear();
        if (!prefabs.ContainsKey("WaterHole")) { log += " [missing WaterHole]"; return; }
        Physics.SyncTransforms();   // the tiles were moved into place after they were made: without this their colliders still sit at the origin
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
                if (pathSamples != null && (DistToPath(p) < 4f || DistToPath(-p) < 4f)) continue;   // the cart / robots never wade through (a hole is up to 2 m wide)
                float r = 1.6f + (float)rnd.NextDouble() * 0.4f;
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
        if (d.mode == GameMode.Competitive && g[13, 10] == '.' && Clear(Vector3.zero, 1.5f)) list.Add((Vector3.zero, 1.9f));
        foreach (var (p, r) in list)
        {
            var go = Prefab("WaterHole", p, 0f, r, false);
            if (!go) continue;
            go.transform.rotation = Quaternion.Euler(0f, Mathf.Repeat(Mathf.Abs(p.x) * 37f + Mathf.Abs(p.z) * 11f, 360f) + (p.z > 0 ? 180f : 0f), 0f);   // mirrored holes look alike
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = 0; GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0); }
            waterHoles.Add((p, r));
            foreach (Transform t in env.Cast<Transform>().ToList())   // no floor decor in the water (bushes stay)
            {
                if (t == go.transform) continue;
                var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                string sn = src ? src.name : t.name;
                if ((sn == "StrawTuft" || sn == "AloePlant" || sn == "HornSkull" || sn == "SavannaStones") && (t.position - p).sqrMagnitude < (r + 0.4f) * (r + 0.4f)) Object.DestroyImmediate(t.gameObject);
            }
        }
        // reeds at the edges (the same spots mirrored)
        int reeds = 0;
        foreach (var (p, r) in waterHoles.Where(q => q.p.z <= 0f).ToList())
        {
            var cr = new Random(Mathf.RoundToInt(Mathf.Abs(p.x) * 100 + Mathf.Abs(p.z) * 7));
            for (int i = 0; i < 4; i++)
            {
                float a = (i + (float)cr.NextDouble() * 0.6f) * Mathf.PI * 0.5f;
                var e = p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r * 1.0f);
                foreach (var q in new[] { e, -e })
                {
                    if (!Clear(q, 0.3f) || spawnPoints.Any(sp => { var w = sp - q; w.y = 0; return w.magnitude < 2f; })) continue;
                    Prefab("PondReeds", q, (float)cr.NextDouble() * 360f, 0.42f + (float)cr.NextDouble() * 0.1f, false); reeds++;
                }
            }
        }
        log += " waterHoles=" + waterHoles.Count + " reeds=" + reeds;
    }

    /// <summary>Dry tufts, aloes, horned skulls and stones on the open earth (no colliders), clear of walls, spawns and water.</summary>
    static void ScatterSavannaFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        Physics.SyncTransforms();
        for (int i = 0; i < 180 && placed < 18; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            if (waterHoles.Any(q => (q.p - p).magnitude < q.r + 0.6f)) continue;
            var k = Pick(rnd, "StrawTuft", "AloePlant", "HornSkull", "SavannaStones", "StrawTuft", "StrawTuft");
            if (!prefabs.ContainsKey(k)) k = "SavannaStones";
            float sc = k == "StrawTuft" ? 0.32f + (float)rnd.NextDouble() * 0.12f : 1f;
            Prefab(k, p, rnd.Next(360), sc, false);
            placed++;
        }
        log += " savannaScatter=" + placed;
    }
}
