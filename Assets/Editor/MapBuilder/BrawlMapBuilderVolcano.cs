using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Volcano theme: a lava island of black columnar basalt in the 1960s, with a volcanology survey camp. Props from
/// Tools/IceArt/props11.py (same folder and palette as the other themes, set up by IceThemeSetup + VolcanoThemeSetup):
/// basalt column blocks, boulders, platy slabs, obsidian veins, red tuff (one family per wall), obsidian spires, fire
/// crystals, magma pillars, burnt trees, lava bombs, survey crates, a mine cart, ScorchGrass (burnt grass bushes),
/// cracks, cooling lava flows and charred tufts on a basalt floor, warning signs by the vents. Border: low basalt rims (BasaltRimA/B/C) with lava
/// lakes all round behind them and a glowing volcano far off; a basalt gateway behind each spawn.
/// Gameplay: eruption vents (EruptionVent) on the open floor, two per half, mirrored, erupting in turn; the ground
/// warns ~2 s before each eruption.
/// </summary>
public static partial class BrawlMapBuilder
{
    static readonly List<Vector3> ventPos = new List<Vector3>();
    static readonly Dictionary<Vector2Int, int> wallFamily = new Dictionary<Vector2Int, int>();
    static Vector2Int TileOf(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.z / T + 13), Mathf.RoundToInt(p.x / T + 10));

    /// <summary>Each wall (a group of touching '#' tiles) gets one rock family: 0 basalt columns, 1 boulders, 2 platy
    /// slabs, 3 obsidian veins, 4 red tuff. Keyed by the group's first tile in canonical (half) coordinates, so a wall
    /// and its mirror get the same family.</summary>
    static void PrepareVolcanoWalls(Def d)
    {
        wallFamily.Clear();
        var g = FullGrid(d); var seen = new bool[ROWS, COLS];
        int[] weights = { 26, 20, 20, 14, 20 };
        for (int r = 0; r < ROWS; r++)
            for (int c = 0; c < COLS; c++)
            {
                if (g[r, c] != '#' || seen[r, c]) continue;
                var group = new List<Vector2Int>(); var q = new Queue<Vector2Int>();
                q.Enqueue(new Vector2Int(r, c)); seen[r, c] = true;
                while (q.Count > 0)
                {
                    var t = q.Dequeue(); group.Add(t);
                    foreach (var n in new[] { new Vector2Int(t.x + 1, t.y), new Vector2Int(t.x - 1, t.y), new Vector2Int(t.x, t.y + 1), new Vector2Int(t.x, t.y - 1) })
                        if (n.x >= 0 && n.x < ROWS && n.y >= 0 && n.y < COLS && !seen[n.x, n.y] && g[n.x, n.y] == '#') { seen[n.x, n.y] = true; q.Enqueue(n); }
                }
                int key = group.Min(t => { Canon(t.x, t.y, out int cr, out int cc, out _); return cr * COLS + cc; });
                int roll = Mix(d.seed * 977 + key) % weights.Sum(), fam = 0;
                while (roll >= weights[fam]) { roll -= weights[fam]; fam++; }
                foreach (var t in group) wallFamily[t] = fam;
            }
        log += " wallFamilies=" + string.Join("", wallFamily.Values.Distinct().OrderBy(x => x));
    }
    static string[] Mir(params string[] rows) => rows.Select(r => new string(r.Reverse().ToArray())).ToArray();
    static Vector2[] MirPath(params Vector2[] p) => p.Select(v => new Vector2(-v.x, v.y)).ToArray();

    public static readonly Def[] VolcanoMaps =
    {
        new Def {   // layout of Map_Brawl_BambooTemple
            mapEnum = "Map_Brawl_MagmaRift", displayName = "Magma Rift", icon = "map_magmarift", mode = GameMode.Competitive, theme = Theme.Volcano, seed = 1501,
            names = new[] { "Magma Rift", "Магмовый разлом", "รอยแยกแมกมา", "Grieta de Magma", "Khe Nứt Magma" },
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
        new Def {   // layout of Map_Brawl_GemHeist (mirrored)
            mapEnum = "Map_Brawl_ObsidianKeep", displayName = "Obsidian Keep", icon = "map_obsidiankeep", mode = GameMode.CaptureTheFlag, theme = Theme.Volcano, seed = 1502,
            names = new[] { "Obsidian Keep", "Обсидиановая крепость", "ป้อมออบซิเดียน", "Fortaleza de Obsidiana", "Pháo Đài Hắc Diện" },
            half = Mir(
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
                ".bb...##.....##...bb."),
            center = "........#...#........",
        },
        new Def {   // layout of Map_Brawl_MoonAltar (mirrored)
            mapEnum = "Map_Brawl_CalderaCrown", displayName = "Caldera Crown", icon = "map_calderacrown", mode = GameMode.KingOfTheHill, theme = Theme.Volcano, seed = 1503,
            names = new[] { "Caldera Crown", "Корона кальдеры", "มงกุฎปล่องภูเขาไฟ", "Corona de la Caldera", "Vương Miện Miệng Núi" },
            half = Mir(
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
                ".....#.........#....."),
            center = ".bb..#.........#..bb.",
        },
        new Def {   // layout of Map_Brawl_BambooLine (mirrored), path A mirrored
            mapEnum = "Map_Brawl_LavaLine", displayName = "Lava Line", icon = "map_lavaline", mode = GameMode.Payload, theme = Theme.Volcano, seed = 1504,
            pathHalf = MirPath(new Vector2(0, 16.5f), new Vector2(0, 9), new Vector2(-6, 4)),
            names = new[] { "Lava Line", "Лавовая линия", "รางลาวา", "Línea de Lava", "Đường Ray Dung Nham" },
            half = Mir(
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
                "......##.....##......"),
            center = "bb....t.......t....bb",
        },
        new Def {   // layout of Map_Brawl_MoonbaseSiege (mirrored), path E mirrored
            mapEnum = "Map_Brawl_CinderWorks", displayName = "Cinder Works", icon = "map_cinderworks", mode = GameMode.MannVsMachine, theme = Theme.Volcano, seed = 1505,
            pathHalf = MirPath(new Vector2(-4, 16.5f), new Vector2(-4, 6), new Vector2(4, 2)),
            names = new[] { "Cinder Works", "Шлаковый завод", "โรงงานเถ้าถ่าน", "Fundición de Ceniza", "Xưởng Tro Than" },
            half = Mir(
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
                ".bb...............bb."),
            center = "....t....###....t....",
        },
        new Def {   // layout of Map_Brawl_DustDevil
            mapEnum = "Map_Brawl_EmberThrone", displayName = "Ember Throne", icon = "map_emberthrone", mode = GameMode.JungleBoss, theme = Theme.Volcano, seed = 1506,
            names = new[] { "Ember Throne", "Огненный трон", "บัลลังก์ถ่านไฟ", "Trono de Brasas", "Ngai Vàng Than Hồng" },
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
                ".....................",
            },
            center = "..........t..........",
        },
        new Def {   // layout of Map_Brawl_DeadCrater
            mapEnum = "Map_Brawl_AshenHollow", displayName = "Ashen Hollow", icon = "map_ashenhollow", mode = GameMode.Zombie, theme = Theme.Volcano, seed = 1507,
            names = new[] { "Ashen Hollow", "Пепельная лощина", "หุบเถ้าถ่าน", "Hondonada de Ceniza", "Thung Lũng Tro Tàn" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bb.............bb..",
                "..bb...##...##...bb..",
                ".#.................#.",
                ".x.......bbb.......x.",
                "....##...bbb...##....",
                "..t...............t..",
                ".........###.........",
                ".bb......###......bb.",
                ".........bbb.........",
                ".....................",
            },
            center = ".........bbb.........",
        },
    };

    [MenuItem("Tools/Maps/Build Volcano Maps")]
    static void BuildVolcanoMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Volcano] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Volcano] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in VolcanoMaps) Debug.Log(Build(d));
    }

    static void VolcanoTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':
                {
                    // one rock family per wall (mirrored walls share it), a little variety inside the family
                    int fam = wallFamily.TryGetValue(TileOf(p), out var f) ? f : 0;
                    string k = fam switch
                    {
                        1 => v < 0.5 ? "BasaltBouldersA" : "BasaltBouldersB",
                        2 => v < 0.5 ? "BasaltSlabsA" : "BasaltSlabsB",
                        3 => v < 0.6 ? "ObsidianWall" : "BasaltBlockA",
                        4 => v < 0.5 ? "TuffRockA" : "TuffRockB",
                        _ => v < 0.40 ? "BasaltBlockA" : v < 0.70 ? "BasaltBlockB" : "BasaltBlockC",
                    };
                    if (!prefabs.ContainsKey(k)) k = "BasaltBlockA";
                    float rot = fam == 1 || fam == 4 ? rnd.Next(360) : Rot90(rnd) + baseRot + Jit(rnd, 4);
                    Prefab(k, p + new Vector3(Jit(rnd, 0.03f), 0, Jit(rnd, 0.03f)), rot, 0.96f + (float)rnd.NextDouble() * 0.06f, true);
                }
                break;
            case 'x':
                if (v < 0.22) Prefab("LavaRock", p, rnd.Next(360), 0.95f, true);
                else if (v < 0.42) Prefab("ObsidianChunk", p, rnd.Next(360), 1f, true);
                else if (v < 0.60) Prefab("SurveyCrates", p, Rot90(rnd) + Jit(rnd, 8), 1f, true);
                else if (v < 0.82) Prefab("LavaBombs", p, rnd.Next(360), 1f, true);
                else Prefab("MineCart", p, Rot90(rnd) + Jit(rnd, 6), 1f, true);
                break;
            case 'f': Prefab("BasaltLow", p, fenceRot + Jit(rnd, 3), 1f, true); break;
            case 't':
                if (v < 0.22) Prefab("ObsidianSpire", p, rnd.Next(360), 0.95f, true);
                else if (v < 0.44) Prefab("FireCrystal", p, rnd.Next(360), 0.95f, true);
                else if (v < 0.60) Prefab("CharredTree", p, rnd.Next(360), 1f, true);
                else if (v < 0.80) Prefab("MagmaPillar", p, rnd.Next(360), 0.95f, true);
                else Prefab("BurntPine", p, rnd.Next(360), 1f, true);
                break;
            case 'b':   // burnt grass bushes, two leaf shapes mixed (~1.9 m like the other bushes)
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 2; j++)
                    {
                        var o = new Vector3((i - 0.5f) * 0.74f + Jit(rnd, 0.08f), 0, (j - 0.5f) * 0.74f + Jit(rnd, 0.08f));
                        Prefab(rnd.NextDouble() < 0.5 ? "ScorchGrassA" : "ScorchGrassB", p + o, rnd.Next(360), 1.3f * (0.92f + (float)rnd.NextDouble() * 0.16f), false);
                    }
                break;
        }
    }

    static void VolcanoDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "LavaCrack", "PumiceRocks", "EmberTuft", "CooledFlow", "EmberTuft", "ObsidianChunk", "LavaCrack");
        if (!prefabs.ContainsKey(k)) k = "PumiceRocks";
        float sc = k == "ObsidianChunk" ? 0.42f : k == "LavaCrack" ? 0.85f : k == "EmberTuft" ? 0.32f + (float)rnd.NextDouble() * 0.1f : k == "CooledFlow" ? 0.75f : 1f;
        Prefab(k, o, rot + rnd.Next(360), sc, false);
    }

    /// <summary>Warm volcanic light: Western lighting with a hot ambient, ashy haze and a smoky dusk sky.</summary>
    static void VolcanoLook(Light light)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(1.0f, 0.82f, 0.72f); RenderSettings.ambientEquatorColor = new Color(0.86f, 0.64f, 0.56f); RenderSettings.ambientGroundColor = new Color(0.56f, 0.40f, 0.38f);
        var haze = VolcanoHaze;
        RenderSettings.fogColor = haze;
        if (light) light.color = new Color(1.0f, 0.86f, 0.72f);
        VolcanoSky();
    }

    static readonly Color VolcanoHaze = new Color(0.62f, 0.40f, 0.34f);
    /// <summary>Smoky dusk behind everything (also after the mode objects are in: they may bring a camera).</summary>
    static void VolcanoSky()
    {
        // the map scene has no camera of its own (the game brings it): the sky must come from the skybox
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(VolcanoThemeSetup.SkyMat); RenderSettings.fogColor = VolcanoHaze;
        foreach (var cam in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)))
        { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = VolcanoHaze; }
    }

    /// <summary>Beyond the basalt rims: lava lakes all round the island (they start under the rims, so no lava shows inside
    /// the arena), rocks and spires standing in the lava, a glowing volcano far off at each end. Also the basalt gateway
    /// behind each team's spawn (the gap in the border there is closed by the invisible VitualWall).</summary>
    static void PlaceVolcanoBackdrop(int seed)
    {
        PlaceGates("VolcanoGate");
        VolcanoSky();
        // lava: each side starts at the outermost rim's centre line, the ends behind the gateways
        var rims = env.Cast<Transform>().Where(t => t.name.StartsWith("BasaltRim")).ToList();
        float left = rims.Where(t => t.position.x < 0).Select(t => -t.position.x).DefaultIfEmpty(17f).Max();
        float right = rims.Where(t => t.position.x > 0).Select(t => t.position.x).DefaultIfEmpty(17f).Max();
        int lakes = 0;
        void Lake(float x0, float x1, float z0, float z1)
        {
            var go = Prefab("LavaLake", new Vector3((x0 + x1) / 2f, 0.03f, (z0 + z1) / 2f), 0f, 1f, false);
            if (go) { go.transform.localScale = new Vector3(Mathf.Abs(x1 - x0), 1f, Mathf.Abs(z1 - z0)); lakes++; }
        }
        Lake(-left, -90f, -90f, 90f);
        Lake(right, 90f, -90f, 90f);
        EndLakes(Lake, left, right, 90f);   // the ends, with a floor inlet under the gate
        log += " lavaLakes=" + lakes + "(" + left.ToString("F1") + "/" + right.ToString("F1") + ")";

        var rnd = new Random(seed);
        // far volcanoes, one off each end (mirrored, so both teams look at one)
        for (int s = -1; s <= 1; s += 2)
        {
            var p = new Vector3(s * (16f + (float)rnd.NextDouble() * 6f), -2f, s * 80f);
            Prefab("Volcano", p, rnd.Next(360), 1.0f + (float)rnd.NextDouble() * 0.15f, false);
        }
        // islands of basalt, spires and crystals standing in the lava
        for (int i = 0; i < 30; i++)
        {
            float a = i / 30f * Mathf.PI * 2f + Jit(rnd, 0.07f);
            float rad = 30f + (float)rnd.NextDouble() * 12f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.9f, 0f, Mathf.Sin(a) * rad);
            if (Mathf.Abs(p.x) < Mathf.Max(left, right) + 3f && Mathf.Abs(p.z) < 28f) continue;   // stay out in the lava
            var k = Pick(rnd, "ObsidianSpire", "BasaltBouldersA", "FireCrystal", "TuffRockA", "BasaltSlabsA", "MagmaPillar", "BurntPine", "BasaltBlockC", "LavaRock", "ObsidianWall");
            if (!prefabs.ContainsKey(k)) k = "BasaltBlockA";
            float sc = k.StartsWith("Basalt") || k.StartsWith("Tuff") || k == "ObsidianWall" ? 1.6f + (float)rnd.NextDouble() * 0.9f : k == "LavaRock" ? 1.8f : 1.3f + (float)rnd.NextDouble() * 0.6f;
            Prefab(k, p, rnd.Next(360), sc, false);
        }
    }

    /// <summary>Glowing cracks and loose stones on the open basalt (no colliders), clear of walls and spawns.</summary>
    static void ScatterVolcanoFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        for (int i = 0; i < 180 && placed < 18; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            if (ventPos.Any(v => (v - p).sqrMagnitude < 2.4f * 2.4f)) continue;   // not over an eruption vent
            var k = Pick(rnd, "LavaCrack", "PumiceRocks", "EmberTuft", "CooledFlow", "EmberTuft");
            if (!prefabs.ContainsKey(k)) k = "PumiceRocks";
            Prefab(k, p, rnd.Next(360), k == "LavaCrack" ? 0.9f : k == "EmberTuft" ? 0.34f : k == "CooledFlow" ? 0.8f : 1f, false);
            placed++;
        }
        log += " volcanoScatter=" + placed;
    }

    /// <summary>Eruption vents: two per half on open floor (all 8 neighbours open, nothing standing within ~1.2 m), at
    /// least 5 m from the spawns, 4.5 m from the middle (the hill / the flags' path), 3 m from a payload / robot path and
    /// 7 m from each other; mirrored into the other half with the same phase. The two pairs erupt half a cycle apart.</summary>
    static void PlaceVents(Def d)
    {
        if (!prefabs.ContainsKey("EruptionVent")) { log += " [missing EruptionVent]"; return; }
        ventPos.Clear();
        var g = FullGrid(d);
        var rnd = new Random(d.seed * 31 + 5);
        var cands = new List<(Vector3 p, double score)>();
        for (int r = 2; r <= 11; r++)
            for (int c = 1; c < COLS - 1; c++)
            {
                // open floor: the tile itself free, no bush next to it (a warning hidden in a bush is unfair), at most
                // two solid neighbours (room to step out of the disc)
                if (g[r, c] != '.') continue;
                int solid = 0; bool bush = false;
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        char ch = g[r + dr, c + dc];
                        if (ch == 'b') bush = true; else if (ch != '.') solid++;
                    }
                if (bush || solid > 2) continue;
                var p = TilePos(r, c);
                if (Mathf.Abs(p.x) > 12.5f) continue;                                         // not against the border
                if (new Vector2(p.x, p.z).magnitude < 4.5f) continue;
                if (spawnPoints.Any(sp => { var q = sp - p; q.y = 0; return q.magnitude < 5f; })) continue;
                if (spawnPoints.Any(sp => { var q = sp + p; q.y = 0; return q.magnitude < 5f; })) continue;   // mirror too
                if (pathSamples != null && (DistToPath(p) < 3f || DistToPath(-p) < 3f)) continue;
                if (Physics.CheckBox(p + Vector3.up * 1.2f, new Vector3(1.2f, 0.8f, 1.2f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.CheckBox(-p + Vector3.up * 1.2f, new Vector3(1.2f, 0.8f, 1.2f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                // prefer the middle of the half (where fights pass), a little randomness between equal spots
                double score = -Mathf.Abs(p.z + 8.5f) * 0.6 - Mathf.Abs(Mathf.Abs(p.x) - 5f) * 0.25 + rnd.NextDouble() * 0.8;
                cands.Add((p, score));
            }
        var chosen = new List<Vector3>();
        foreach (var cnd in cands.OrderByDescending(x => x.score))
        {
            if (chosen.Count >= 2) break;
            if (chosen.Any(q => (q - cnd.p).magnitude < 6f)) continue;
            chosen.Add(cnd.p);
        }
        int n = 0;
        for (int i = 0; i < chosen.Count; i++)
        {
            float phase = i * 5f;   // the two pairs take turns (period 10 s)
            foreach (var (p, rot) in new[] { (chosen[i], 0f), (-chosen[i], 180f) })
            {
                var go = Prefab("EruptionVent", p, rot, 1f, false);
                if (!go) continue;
                // it moves (the fountain scales, the disc and particles switch on): not static, not a wall
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) { GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0); t.gameObject.layer = 0; }
                // no floor decor under it (cracks, stones from the tile pass)
                foreach (Transform t in env.Cast<Transform>().ToList())
                {
                    if (t == go.transform || t.GetComponentInChildren<Collider>(true) is Collider cl && cl.enabled) continue;
                    var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                    string sn = src ? src.name : t.name;
                    if ((sn == "LavaCrack" || sn == "PumiceRocks" || sn == "ObsidianChunk") && (t.position - p).sqrMagnitude < 2.2f * 2.2f) Object.DestroyImmediate(t.gameObject);
                }
                ventPos.Add(p);
                var v = go.GetComponent<EruptionVent>();
                if (v) { v.phase = phase; PrefabUtility.RecordPrefabInstancePropertyModifications(v); }
                n++;
            }
        }
        log += " vents=" + n;
    }
}
