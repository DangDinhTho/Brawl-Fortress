using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Greek theme (a Mediterranean acropolis by the sea, 1960s-70s). Props from Tools/IceArt/props17.py (same folder and
/// palette as the other themes, set up by IceThemeSetup + GreekThemeSetup): marble ashlar walls, broken columns on their
/// stylobate, whitewashed house corners, dry-stone walls (one family per wall), fallen drums and capitals, storage jars,
/// an olive mill, standing Doric columns, a headless statue, olive trees, cypresses, bronze braziers, scrub bushes.
/// Border: low limestone cliffs topped with ruins, a chapel or olive trees (GreekCliffA/B/C), the sea beyond, the
/// GreekGate (a propylon) behind each spawn; a great ruined temple on its rock, whitewashed villages, windmills and
/// fishing caiques around. Lighting: the bright neutral desert light (EgyptLook / EgyptSun) under a warm sky.
/// </summary>
public static partial class BrawlMapBuilder
{
    public static readonly Def[] GreekMaps =
    {
        new Def {   // a new layout
            mapEnum = "Map_Brawl_MarbleAgora", displayName = "Marble Agora", icon = "map_marbleagora", mode = GameMode.Competitive, theme = Theme.Greek, seed = 2101,
            names = new[] { "Marble Agora", "Мраморная агора", "ลานหินอ่อน", "Ágora de Mármol", "Quảng Trường Cẩm Thạch" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "...##...#...#...##...",
                "..bbb...........bbb..",
                "..bbb..t.....t..bbb..",
                "........x...x........",
                ".##...bbb...bbb...##.",
                ".#....bbb...bbb....#.",
                "....t...........t....",
                "..f.....##.##.....f..",
                ".......x.....x.......",
                "...bb...........bb...",
            },
            center = "...bb....###....bb...",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_AmphoraHeist", displayName = "Amphora Heist", icon = "map_amphoraheist", mode = GameMode.CaptureTheFlag, theme = Theme.Greek, seed = 2102,
            names = new[] { "Amphora Heist", "Похищение амфоры", "ปล้นโถแอมโฟรา", "Atraco del Ánfora", "Phi Vụ Bình Gốm" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..b...##.....##...b..",
                ".bbb.............bbb.",
                ".bbb...x.....x...bbb.",
                "......##..t..##......",
                "..t...............t..",
                "....bb...###...bb....",
                "....bb.........bb....",
                ".##.....x...x.....##.",
                ".#.......f.f.......#.",
                "......bb.....bb......",
            },
            center = ".....bb.......bb.....",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_OracleHill", displayName = "Oracle Hill", icon = "map_oraclehill", mode = GameMode.KingOfTheHill, theme = Theme.Greek, seed = 2103,
            names = new[] { "Oracle Hill", "Холм оракула", "เนินแห่งเทพพยากรณ์", "Colina del Oráculo", "Đồi Nhà Tiên Tri" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bb.....#...#.....bb.",
                ".bb.....#...#.....bb.",
                "....x...........x....",
                "..##....bbbbb....##..",
                "..#.....bbbbb.....#..",
                ".....t.........t.....",
                "bb.................bb",
                "bb....##.....##....bb",
                "......#...x...#......",
                "...f.............f...",
            },
            center = "...t.............t...",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_HarborLine", displayName = "Harbor Line", icon = "map_harborline", mode = GameMode.Payload, theme = Theme.Greek, seed = 2104,
            pathHalf = new[] { new Vector2(-4, 16.5f), new Vector2(-4, 6), new Vector2(4, 2) },
            names = new[] { "Harbor Line", "Портовая линия", "ทางรถไฟท่าเรือ", "Línea del Puerto", "Tuyến Bến Cảng" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##...x...........##",
                "..#..bb...........#..",
                ".....bb..t...........",
                ".bbb.................",
                ".bbb...##.......bb...",
                "........#.......bb...",
                "..t..............x...",
                ".##...........t......",
                ".....x.......bb......",
                "...bb...........x....",
            },
            center = "..#...............#..",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_BronzeForge", displayName = "Bronze Forge", icon = "map_bronzeforge", mode = GameMode.MannVsMachine, theme = Theme.Greek, seed = 2105,
            pathHalf = new[] { new Vector2(6, 16.5f), new Vector2(6, 8), new Vector2(-6, 5), new Vector2(-6, 1) },
            names = new[] { "Bronze Forge", "Бронзовая кузница", "โรงหล่อสำริด", "Forja de Bronce", "Lò Rèn Đồng Thau" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bb...........x...bb.",
                ".bb.......##......bb.",
                "..#.......#.......t..",
                "..##.........t...##..",
                "bb.......bbbb.....#..",
                "...x..........f......",
                "..t................t.",
                "..##....bb.........#.",
                "....f...bb.......x...",
                "..#................#.",
            },
            center = "...bb...........bb...",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_TitanTerrace", displayName = "Titan Terrace", icon = "map_titanterrace", mode = GameMode.JungleBoss, theme = Theme.Greek, seed = 2106,
            names = new[] { "Titan Terrace", "Терраса титанов", "ระเบียงไททัน", "Terraza de los Titanes", "Sân Thượng Titan" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..t.......#.......t..",
                "......bb.....bb......",
                ".##...bb.....bb...##.",
                ".....................",
                "...x....##.##....x...",
                ".....................",
                ".bbb.....t.t.....bbb.",
                ".bbb.............bbb.",
                "......#.......#......",
                ".....................",
            },
            center = "....bb.........bb....",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_NecropolisSteps", displayName = "Necropolis Steps", icon = "map_necropolissteps", mode = GameMode.Zombie, theme = Theme.Greek, seed = 2107,
            names = new[] { "Necropolis Steps", "Ступени некрополя", "บันไดนครแห่งความตาย", "Escalinata de la Necrópolis", "Bậc Thang Nghĩa Địa Cổ" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".#####.........#####.",
                ".....................",
                "..bb....x...x....bb..",
                "..bb..t.......t..bb..",
                ".....................",
                ".##..f....#....f..##.",
                "......bbb...bbb......",
                "......bbb...bbb......",
                "..x.......t.......x..",
                "....##.........##....",
            },
            center = "........b...b........",
        },
    };

    [MenuItem("Tools/Maps/Build Greek Maps")]
    static void BuildGreekMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Greek] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Greek] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in GreekMaps) Debug.Log(Build(d));
    }

    static void GreekTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':
                {
                    // one family per wall (PrepareVolcanoWalls): marble ashlars (0 and 4), broken columns (1), white houses (2), dry stone (3)
                    int fam = wallFamily.TryGetValue(TileOf(p), out var f) ? f : 0;
                    var jit = p + new Vector3(Jit(rnd, 0.02f), 0, Jit(rnd, 0.02f));
                    if (fam == 1) Prefab("BrokenColumns", jit, Rot90(rnd) + Jit(rnd, 3), 1f, true);
                    else if (fam == 2) Prefab("WhiteWall", jit, Rot90(rnd), 1f, true);
                    else if (fam == 3) Prefab("StoneWall", jit, fenceRot + rnd.Next(2) * 180f + Jit(rnd, 3), 1f, true);
                    else Prefab(fam == 4 || v < 0.45 ? "MarbleBlocksB" : "MarbleBlocksA", jit, fenceRot + rnd.Next(2) * 180f + Jit(rnd, 3), 1f, true);
                }
                break;
            case 'x':
                if (v < 0.30) Prefab("FallenDrums", p, rnd.Next(360), 1f, true);
                else if (v < 0.50) Prefab("ColumnCapital", p, rnd.Next(360), 1f, true);
                else if (v < 0.75) Prefab("Pithoi", p, rnd.Next(360), 0.95f, true);
                else Prefab("OlivePress", p, rnd.Next(360), 0.92f, true);
                break;
            case 'f': Prefab("MarbleLow", p, fenceRot + Jit(rnd, 4), 1f, true); break;
            case 't':
                if (v < 0.25) Prefab("DoricColumn", p, Rot90(rnd) + Jit(rnd, 4), 1f, true);
                else if (v < 0.40) Prefab("MarbleStatue", p, baseRot + 180f + Jit(rnd, 25), 1f, true);
                else if (v < 0.65) Prefab("OliveTree", p, rnd.Next(360), 1f, true);
                else if (v < 0.85) Prefab("GreekCypress", p, rnd.Next(360), 1f, true);
                else Prefab("BronzeBrazier", p, rnd.Next(360), 1f, true);
                break;
            case 'b': Grass("MaquisGrass", p, 1.3f, rnd); break;   // ~1.95 m, like the other bushes
        }
    }

    static void GreekDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "MarbleRubble", "PotteryShards", "GeraniumPot", "MarbleRubble", "PotteryShards");
        if (!prefabs.ContainsKey(k)) k = "MarbleRubble";
        Prefab(k, o, rot + rnd.Next(360), 1f, false);
    }

    static readonly Color GreekHaze = new Color(0.72f, 0.80f, 0.88f);

    /// <summary>Warm Mediterranean sun, a little softer than the desert look (white marble and whitewash glared under it).</summary>
    static void GreekLook(Light light)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.74f, 0.75f, 0.78f); RenderSettings.ambientEquatorColor = new Color(0.66f, 0.65f, 0.64f); RenderSettings.ambientGroundColor = new Color(0.52f, 0.50f, 0.48f);
        if (light) light.color = new Color(1.0f, 0.96f, 0.90f);
        GreekSky();
    }

    /// <summary>After the mode objects are in: the softer Greek props material everywhere.</summary>
    static void GreekSun()
    {
        var from = AssetDatabase.LoadAssetAtPath<Material>(IceThemeSetup.PropsMat);
        var to = AssetDatabase.LoadAssetAtPath<Material>(GreekThemeSetup.PropsMat);
        int n = 0;
        if (from && to)
            foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
            {
                var mats = r.sharedMaterials; bool ch = false;
                for (int i = 0; i < mats.Length; i++) if (mats[i] == from) { mats[i] = to; ch = true; }
                if (ch) { r.sharedMaterials = mats; n++; }
            }
        log += " greekSun=" + n;
    }
    static void GreekSky()
    {
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(GreekThemeSetup.SkyMat);
        RenderSettings.fogColor = GreekHaze;
        foreach (var cam in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)))
        { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = GreekHaze; }
    }

    /// <summary>Beyond the cliffs: the sea all round (it starts under the cliffs, none of it inside the arena), a great
    /// ruined temple on its rock off each end, whitewashed villages and windmills on islets, fishing caiques. Also the
    /// propylon behind each team's spawn.</summary>
    static void PlaceGreekBackdrop(int seed)
    {
        PlaceGates("GreekGate");
        GreekSky();
        var cliffs = env.Cast<Transform>().Where(t => t.name.StartsWith("GreekCliff")).ToList();
        float left = cliffs.Where(t => t.position.x < 0).Select(t => -t.position.x).DefaultIfEmpty(17f).Max();
        float right = cliffs.Where(t => t.position.x > 0).Select(t => t.position.x).DefaultIfEmpty(17f).Max();
        int seas = 0;
        void Sea(float x0, float x1, float z0, float z1)
        {
            var go = Prefab("SeaWater", new Vector3((x0 + x1) / 2f, 0.03f, (z0 + z1) / 2f), 0f, 1f, false);
            if (go) { go.transform.localScale = new Vector3(Mathf.Abs(x1 - x0), 1f, Mathf.Abs(z1 - z0)); foreach (var c in go.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = 0; seas++; }
        }
        Sea(-left, -120f, -120f, 120f);
        Sea(right, 120f, -120f, 120f);
        Sea(-left, right, 24.2f, 120f);
        Sea(-left, right, -24.2f, -120f);
        log += " sea=" + seas;
        var rnd = new Random(seed);
        for (int s = -1; s <= 1; s += 2)   // mirrored, so both teams look at the same things
        {
            Prefab("GreekTemple", new Vector3(s * 8f, -0.5f, s * 52f), s > 0 ? 8f : 188f, 1f, false);
            Prefab("WhiteVillage", new Vector3(s * 33f, -0.3f, s * 6f), s > 0 ? -90f : 90f, 1f, false);
            Prefab("Windmill", new Vector3(-s * 30f, -0.3f, s * 16f), s > 0 ? 100f : -80f, 0.9f, false);
            Prefab("Windmill", new Vector3(s * 34f, -0.3f, -s * 18f), s > 0 ? -70f : 110f, 0.85f, false);
        }
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f + Jit(rnd, 0.2f);
            float rad = 36f + (float)rnd.NextDouble() * 14f;
            var p = new Vector3(Mathf.Cos(a) * rad, -0.2f, Mathf.Sin(a) * rad * 1.15f);
            if (Mathf.Abs(p.x) < 24f && Mathf.Abs(p.z) < 30f) continue;
            Prefab("Caique", p, rnd.Next(360), 1f, false);
        }
    }

    /// <summary>Marble chips, potsherds and pots of geraniums on the open ground (no colliders), clear of walls and spawns.</summary>
    static void ScatterGreekFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        for (int i = 0; i < 160 && placed < 14; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            var k = Pick(rnd, "MarbleRubble", "PotteryShards", "MarbleRubble", "GeraniumPot");
            if (!prefabs.ContainsKey(k)) k = "MarbleRubble";
            Prefab(k, p, rnd.Next(360), 1f, false);
            placed++;
        }
        log += " greekScatter=" + placed;
    }
}
