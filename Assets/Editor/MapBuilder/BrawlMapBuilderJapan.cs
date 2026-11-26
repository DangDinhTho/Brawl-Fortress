using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Japan theme (a 1960s-70s Shinto shrine and temple garden in spring). Props from Tools/IceArt/props21.py (same folder
/// and palette as the other themes, set up by IceThemeSetup + JapanThemeSetup; cherry blossom pink in material slot 1
/// = SakuraPink): castle stone walls, bamboo fences, sake barrel walls, white plaster walls under tile roofs and mossy
/// rocks (one family per wall), stone lanterns, water basins, offering boxes, sake barrels, bamboo lattice fences,
/// cherry trees, cloud-pruned black pines, tall stone lanterns, komainu guardians, bamboo thickets as the bushes. The
/// landmark: the vermilion torii - a big one behind each spawn (ToriiGate), rows of small ones over stone paths in the
/// arena. Mechanic: running along a torii path is faster, both ways (ToriiPath.cs). Border: earthen walls under tile
/// roofs on a stone base (ShrineWallA/B/C); a pagoda, a shrine hall and a far snowy peak beyond. Lighting: the soft Greek
/// daylight under a spring sky.
/// </summary>
public static partial class BrawlMapBuilder
{
    public static readonly Def[] JapanMaps =
    {
        new Def {   // a new layout: two straight open runs per half for the torii paths, lined up through the middle
            mapEnum = "Map_Brawl_SakuraShrine", displayName = "Sakura Shrine", icon = "map_sakurashrine", mode = GameMode.Competitive, theme = Theme.Japan, seed = 2501,
            names = new[] { "Sakura Shrine", "Храм сакуры", "ศาลเจ้าซากุระ", "Santuario Sakura", "Đền Hoa Anh Đào" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##.....bbb.....##..",
                "..#...............#..",
                "..........x..........",
                ".bbb.............bbb.",
                ".bbb.....#.#.....bbb.",
                "..t......#.#......t..",
                ".....................",
                "..##.....bbb.....##..",
                "..#...............#..",
                "....x...........x....",
            },
            center = "..t......x.x......t..",
        },
        new Def {   // a new layout: torii paths down both outer flanks
            mapEnum = "Map_Brawl_ShrineHeist", displayName = "Shrine Heist", icon = "map_shrineheist", mode = GameMode.CaptureTheFlag, theme = Theme.Japan, seed = 2502,
            names = new[] { "Shrine Heist", "Ограбление храма", "ปล้นศาลเจ้า", "Atraco al Santuario", "Phi Vụ Ngôi Đền" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..#.......x.......#..",
                "........bbbbb........",
                ".....................",
                "..t...#.......#...t..",
                "......#...x...#......",
                "..bb.............bb..",
                "..bb....f...f....bb..",
                ".....................",
                ".......##...##.......",
                "..x.......t.......x..",
            },
            center = "......bb.....bb......",
        },
        new Def {   // a new layout: torii paths past both sides of the hill
            mapEnum = "Map_Brawl_PagodaHill", displayName = "Pagoda Hill", icon = "map_pagodahill", mode = GameMode.KingOfTheHill, theme = Theme.Japan, seed = 2503,
            names = new[] { "Pagoda Hill", "Холм пагоды", "เนินเจดีย์", "Colina de la Pagoda", "Đồi Chùa Tháp" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".##.......#.......##.",
                ".#.......bbb.......#.",
                "...x.....bbb.....x...",
                ".....................",
                ".bb.....##.##.....bb.",
                ".bb...............bb.",
                "...t.............t...",
                ".....................",
                "..##....bb.bb....##..",
                "....x...........x....",
            },
            center = "..x...............x..",
        },
        new Def {   // a new layout (path C): the track down the middle, a torii path on each side
            mapEnum = "Map_Brawl_SakuraExpress", displayName = "Sakura Express", icon = "map_sakuraexpress", mode = GameMode.Payload, theme = Theme.Japan, seed = 2504,
            pathHalf = new[] { new Vector2(0, 16.5f), new Vector2(0, 5) },
            names = new[] { "Sakura Express", "Экспресс сакуры", "รถด่วนซากุระ", "Expreso Sakura", "Tàu Tốc Hành Anh Đào" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##...x.....x...##..",
                "..#...............#..",
                "........x...x........",
                ".t.................t.",
                "...bb...........bb...",
                "...bb...#...#...bb...",
                "........#...#........",
                ".....................",
                "..x.....f...f.....x..",
                ".......bb...bb.......",
            },
            center = "..bb.............bb..",
        },
        new Def {   // a new layout (path D): two torii paths on the side the robots do not take
            mapEnum = "Map_Brawl_LanternWorks", displayName = "Lantern Works", icon = "map_lanternworks", mode = GameMode.MannVsMachine, theme = Theme.Japan, seed = 2505,
            pathHalf = new[] { new Vector2(0, 16.5f), new Vector2(-8, 10), new Vector2(-9, 2) },
            names = new[] { "Lantern Works", "Фонарная мастерская", "โรงงานโคมไฟ", "Taller de Linternas", "Xưởng Đèn Lồng" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##..x.......x..##..",
                "..#...............#..",
                "..........bb......x..",
                ".....##..............",
                ".....#...............",
                "...........x.........",
                "..........#.......x..",
                ".....##....#.........",
                "...........bb........",
                "........x............",
            },
            center = ".b.................b.",
        },
        new Def {   // a new layout: two torii paths framing the middle
            mapEnum = "Map_Brawl_KomainuCourt", displayName = "Komainu Court", icon = "map_komainucourt", mode = GameMode.JungleBoss, theme = Theme.Japan, seed = 2506,
            names = new[] { "Komainu Court", "Двор комаину", "ลานโคมาอินุ", "Patio de los Komainu", "Sân Komainu" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "....x...........x....",
                "..##.....bbb.....##..",
                "..#...............#..",
                "....t...........t....",
                ".bb...............bb.",
                ".bb......###......bb.",
                ".....................",
                "...x......x......x...",
                ".....bb.......bb.....",
                "..t...............t..",
            },
            center = ".....##...x...##.....",
        },
        new Def {   // a new layout: torii paths that stop before the middle
            mapEnum = "Map_Brawl_HauntedTemple", displayName = "Haunted Temple", icon = "map_hauntedtemple", mode = GameMode.Zombie, theme = Theme.Japan, seed = 2507,
            names = new[] { "Haunted Temple", "Храм с привидениями", "วัดผีสิง", "Templo Embrujado", "Ngôi Chùa Ma Ám" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".##..x.........x..##.",
                ".#.................#.",
                "..bb......t......bb..",
                "..bb.............bb..",
                ".....................",
                "....x...##.##...x....",
                "........#...#........",
                ".bb...............bb.",
                ".bb.......x.......bb.",
                "....##.........##....",
            },
            center = "..x...#.......#...x..",
        },
    };

    static readonly List<(Vector3 c, Vector3 size)> toriiPaths = new List<(Vector3, Vector3)>();

    [MenuItem("Tools/Maps/Build Japan Maps")]
    static void BuildJapanMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Japan] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Japan] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in JapanMaps) Debug.Log(Build(d));
    }

    static void JapanTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':
                {
                    // one family per wall (PrepareVolcanoWalls): stone walls (0), bamboo fences (1), sake barrels (2), plaster walls (3), mossy rocks (4)
                    int fam = wallFamily.TryGetValue(TileOf(p), out var f) ? f : 0;
                    var jit = p + new Vector3(Jit(rnd, 0.02f), 0, Jit(rnd, 0.02f));
                    if (fam == 1) Prefab("BambooFenceWall", jit, fenceRot + rnd.Next(2) * 180f + Jit(rnd, 2), 1f, true);
                    else if (fam == 2) Prefab("SakeBarrels", jit, fenceRot + rnd.Next(2) * 180f + Jit(rnd, 3), 1f, true);
                    else if (fam == 3) Prefab("PlasterWall", jit, fenceRot + rnd.Next(2) * 180f, 1f, true);
                    else if (fam == 4) Prefab("MossRock", jit, rnd.Next(360), 0.96f + (float)rnd.NextDouble() * 0.08f, true);
                    else Prefab("IshigakiStones", jit, Rot90(rnd) + Jit(rnd, 3), 1f, true);
                }
                break;
            case 'x':
                if (v < 0.3) Prefab("StoneLanternLow", p, rnd.Next(360), 1f, true);
                else if (v < 0.55) Prefab("WaterBasin", p, rnd.Next(360), 1f, true);
                else if (v < 0.75) Prefab("OfferingBox", p, baseRot + 180f + Jit(rnd, 20), 1f, true);
                else Prefab("TaruStack", p, rnd.Next(360), 1f, true);
                break;
            case 'f': Prefab("BambooFence", p, fenceRot + Jit(rnd, 3), 1f, true); break;
            case 't':
                if (v < 0.36) Prefab("SakuraTree", p, rnd.Next(360), 0.95f + (float)rnd.NextDouble() * 0.1f, true);
                else if (v < 0.6) Prefab("BlackPine", p, rnd.Next(360), 1f, true);
                else if (v < 0.8) Prefab("KasugaLantern", p, Rot90(rnd) + Jit(rnd, 5), 1f, true);
                else Prefab("Komainu", p, baseRot + 180f + Jit(rnd, 20), 1f, true);
                break;
            case 'b': Grass("BambooGrass", p, 1.3f, rnd); break;   // ~1.95 m, like the other bushes
        }
    }

    static void JapanDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "SteppingStones", "MossTuft", "PetalScatter", "ZenStones", "PetalScatter", "MossTuft");
        if (!prefabs.ContainsKey(k)) k = "MossTuft";
        Prefab(k, o, rot + rnd.Next(360), k == "ZenStones" ? 0.8f : 1f, false);
    }

    static readonly Color JapanFog = new Color(0.93f, 0.89f, 0.87f);

    /// <summary>A spring sky with a pale pink-gold haze at the horizon (the soft daylight comes from GreekLook / GreekSun).</summary>
    static void JapanSky()
    {
        var sky = AssetDatabase.LoadAssetAtPath<Material>(JapanThemeSetup.SkyMat);
        if (sky) RenderSettings.skybox = sky;
        RenderSettings.fogColor = JapanFog;
        foreach (var cam in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)))
        { cam.clearFlags = CameraClearFlags.Skybox; cam.backgroundColor = JapanFog; }
    }

    static float FaceCentre(Vector3 p) => Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg;   // yaw that turns a prop's front (+Z) to the arena

    /// <summary>Beyond the walls: a pagoda on one side, a shrine hall with a torii before it on the other, a far snowy peak
    /// off each end, cherry trees, pines, stone lanterns and rocks around. Also the big torii behind each spawn.</summary>
    static void PlaceJapanBackdrop(int seed)
    {
        PlaceGates("ToriiGate");
        JapanSky();
        var rnd = new Random(seed);
        for (int s = -1; s <= 1; s += 2)   // mirrored, so both teams look at the same things
        {
            var pag = new Vector3(-s * 30f, 0f, s * 13f); Prefab("Pagoda", pag, FaceCentre(pag), 1f, false);
            var hall = new Vector3(s * 33f, 0f, -s * 8f); Prefab("ShrineHall", hall, FaceCentre(hall), 1f, false);
            var tg = hall + (new Vector3(-hall.x, 0f, -hall.z)).normalized * 11f; Prefab("ToriiGate", tg, FaceCentre(tg), 0.7f, false);
            Prefab("SnowPeak", new Vector3(s * 22f, -2f, s * 120f), rnd.Next(360), 1.5f, false);
        }
        for (int i = 0; i < 34; i++)
        {
            float a = i / 34f * Mathf.PI * 2f + Jit(rnd, 0.08f);
            float rad = 30f + (float)rnd.NextDouble() * 13f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.9f, 0f, Mathf.Sin(a) * rad);
            if (Mathf.Abs(p.x) < 22f && Mathf.Abs(p.z) < 28f) continue;
            if (Mathf.Abs(Mathf.Abs(p.x) - 31f) < 8f && Mathf.Abs(p.z) < 20f) continue;   // the pagodas and the halls
            var k = Pick(rnd, "SakuraTree", "BlackPine", "SakuraTree", "KasugaLantern", "MossRock", "SakuraTree", "BlackPine");
            float sc = k == "SakuraTree" ? 1.6f + (float)rnd.NextDouble() * 0.6f : k == "BlackPine" ? 1.4f + (float)rnd.NextDouble() * 0.5f : k == "MossRock" ? 1.8f + (float)rnd.NextDouble() * 0.8f : 1.3f;
            Prefab(k, p, rnd.Next(360), sc, false);
        }
    }

    /// <summary>Torii paths: on each half the two best straight runs of open tiles along the length of the map (two tiles
    /// wide, at least 5 long, rows 3-12, no bushes in them), 5 m apart, clear of a payload / robot path; mirrored. A run
    /// that reaches the middle row over open tiles goes on to the centre line, so the two halves join into one path.
    /// Each gets the speed box (ToriiPath), stone slabs every 0.66 m (laid out from the middle outwards so mirrored paths
    /// line up) and a torii at its start. Floor decor in a path goes.</summary>
    static void PlaceToriiPaths(Def d)
    {
        toriiPaths.Clear();
        if (!prefabs.ContainsKey("ToriiPath") || !prefabs.ContainsKey("ToriiSmall")) { log += " [missing ToriiPath]"; return; }
        var g = FullGrid(d);
        bool open(char ch) => ch == '.';
        var cands = new List<(int c, int r0, int r1, double score)>();
        for (int c = 1; c < COLS - 2; c++)
        {
            int r = 3;
            while (r <= 12)
            {
                if (!(open(g[r, c]) && open(g[r, c + 1]))) { r++; continue; }
                int r0 = r;
                while (r <= 12 && open(g[r, c]) && open(g[r, c + 1])) r++;
                int r1 = r - 1, len = r1 - r0 + 1;
                if (len < 5) continue;
                float x = (TilePos(r0, c).x + TilePos(r0, c + 1).x) * 0.5f;
                double score = len - Mathf.Abs(Mathf.Abs(x) - 5f) * 0.25;
                cands.Add((c, r0, r1, score));
            }
        }
        bool Blocked(Vector3 cen, Vector3 size) => pathSamples != null && pathSamples.Any(sp => InLane(sp, cen, size, 1.5f) || InLane(-sp, cen, size, 1.5f));
        float LaneX(int c) => (TilePos(3, c).x + TilePos(3, c + 1).x) * 0.5f;
        var chosen = new List<(int c, int r0, int r1)>();
        foreach (var cd in cands.OrderByDescending(q => q.score))
        {
            if (chosen.Count >= 2) break;
            float x = LaneX(cd.c);
            if (chosen.Any(q => Mathf.Abs(LaneX(q.c) - x) < 5f)) continue;
            float za = TilePos(cd.r0, cd.c).z - 0.65f, zb = TilePos(cd.r1, cd.c).z + 0.65f;
            if (Blocked(new Vector3(x, 0f, (za + zb) * 0.5f), new Vector3(2.6f, 1f, zb - za))) continue;
            chosen.Add((cd.c, cd.r0, cd.r1));
        }
        int torii = 0, slabs = 0;
        foreach (var cd in chosen)
        {
            float x = LaneX(cd.c);
            float za = TilePos(cd.r0, cd.c).z - 0.65f, zb = TilePos(cd.r1, cd.c).z + 0.65f;
            // it runs on to the centre line and joins the other half's path only when that one is its mirror image (a
            // path at -x in this half: its mirror lies at +x on the other side), the middle tiles are open and the
            // longer path stays clear of a payload / robot path; otherwise it ends before the middle with a torii there
            bool joins = cd.r1 == 12 && open(g[13, cd.c]) && open(g[13, cd.c + 1]) && chosen.Any(q => Mathf.Abs(LaneX(q.c) + x) < 0.1f)
                         && !Blocked(new Vector3(x, 0f, za * 0.5f), new Vector3(2.6f, 1f, -za));
            if (joins) zb = 0f;
            var cen = new Vector3(x, 0f, (za + zb) * 0.5f); var size = new Vector3(2.6f, 1f, zb - za);
            foreach (int s in new[] { 1, -1 })
            {
                var box = Prefab("ToriiPath", cen * s, 0f, 1f, false);
                if (box) { box.transform.localScale = size; NotStatic(box); }
                toriiPaths.Add((cen * s, size));
                for (float z = zb - 0.33f; z > za + 0.2f; z -= 0.66f)   // slabs from the middle outwards, almost touching (gaps read as a ladder from above)
                {
                    Prefab("PathSlab", new Vector3(x, 0f, z) * s, Jit(new Random(slabs), 1.5f), 1f, false); slabs++;
                }
                // one torii at the start of the path (its outer end), and at the inner end when it does not run on into
                // the other half; never in rows (a row of torii read as clutter from the top-down camera)
                Prefab("ToriiSmall", new Vector3(x, 0f, za + 0.45f) * s, 0f, 1f, true); torii++;
                if (!joins) { Prefab("ToriiSmall", new Vector3(x, 0f, zb - 0.45f) * s, 0f, 1f, true); torii++; }
            }
        }
        foreach (Transform t in env.Cast<Transform>().ToList())   // no floor decor on the paths
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            string sn = src ? src.name : t.name;
            if ((sn == "SteppingStones" || sn == "MossTuft" || sn == "PetalScatter" || sn == "ZenStones") && toriiPaths.Any(l => InLane(t.position, l.c, l.size, 0.4f))) Object.DestroyImmediate(t.gameObject);
        }
        log += " toriiPaths=" + toriiPaths.Count + " torii=" + torii + " slabs=" + slabs;
    }

    /// <summary>Stepping stones, moss, fallen petals and raked gravel on the open yard (no colliders), clear of walls,
    /// spawns and the torii paths.</summary>
    static void ScatterJapanFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        Physics.SyncTransforms();
        for (int i = 0; i < 180 && placed < 18; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            if (toriiPaths.Any(l => InLane(p, l.c, l.size, 0.6f))) continue;
            var k = Pick(rnd, "SteppingStones", "MossTuft", "PetalScatter", "ZenStones", "PetalScatter");
            if (!prefabs.ContainsKey(k)) k = "MossTuft";
            Prefab(k, p, rnd.Next(360), k == "ZenStones" ? 0.8f : 1f, false);
            placed++;
        }
        log += " japanScatter=" + placed;
    }
}
