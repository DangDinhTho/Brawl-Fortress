using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Xmas theme: a 1960s-70s Christmas in deep snow. Props from Tools/IceArt/props8.py (same folder and palette as the
/// Ice theme, set up by IceThemeSetup): presents, decorated pines with C9 bulb strings, a fence with lights, period
/// street lamps, candy canes, snowmen, a sleigh, giant baubles, PineGrass bushes, the SnowFloor. Border and lighting
/// are the Ice ones (glacier walls turned to the arena, cold ambient).
/// </summary>
public static partial class BrawlMapBuilder
{
    public static readonly Def[] XmasMaps =
    {
        new Def {
            mapEnum = "Map_Brawl_JingleSquare", displayName = "Jingle Square", icon = "map_jinglesquare", mode = GameMode.Competitive, theme = Theme.Xmas, seed = 1225,
            names = new[] { "Jingle Square", "Праздничная площадь", "ลานระฆังคริสต์มาส", "Plaza Navideña", "Quảng Trường Giáng Sinh" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bb...#.....#...bb..",
                "..bb...#..t..#...bb..",
                "....x...........x....",
                ".##....fff.fff....##.",
                ".......b.....b.......",
                "..t...bbb...bbb...t..",
                ".....##.......##.....",
                ".bb.......x.......bb.",
                ".bb..f.........f..bb.",
                "....#....###....#....",
            },
            center = "..x......t.t......x..",
        },
        // one more per mode by day, then one per mode at night (layouts of retired maps, or mirrored ones)
        new Def {   // layout of Map_Brawl_LunarVault
            mapEnum = "Map_Brawl_PresentHeist", displayName = "Present Heist", icon = "map_presentheist", mode = GameMode.CaptureTheFlag, theme = Theme.Xmas, seed = 1301,
            names = new[] { "Present Heist", "Похищение подарков", "ปล้นของขวัญ", "Robo de Regalos", "Phi Vụ Hộp Quà" },
            half = new[] {
                ".....................",
                ".....................",
                "....#...........#....",
                "....#...bbbbb...#....",
                "....#...........#....",
                "..bb...###.###...bb..",
                "..bb.............bb..",
                "......x.......x......",
                ".##....bb...bb....##.",
                ".......bb...bb.......",
                "...#.............#...",
                "...#....#####....#...",
                ".bb...............bb.",
            },
            center = ".bb.....x...x.....bb.",
        },
        new Def {   // layout of Map_Brawl_BambooShrine
            mapEnum = "Map_Brawl_TreeTopHill", displayName = "Tree Top Hill", icon = "map_treetophill", mode = GameMode.KingOfTheHill, theme = Theme.Xmas, seed = 1302,
            names = new[] { "Tree Top Hill", "Холм у ёлки", "เนินต้นคริสต์มาส", "Colina del Árbol", "Đồi Cây Thông" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb......t......bbb.",
                ".bbb.............bbb.",
                "......ff.....ff......",
                "bbbb.............bbbb",
                "bbbb....bbbbb....bbbb",
                "........bbbbb........",
                "...t.............t...",
                "......#.......#......",
                "..bbb...........bbb..",
                "..bbb..f.......f.bbb.",
            },
            center = "bb....t.......t....bb",
        },
        new Def {   // layout of Map_Brawl_MineExpress
            mapEnum = "Map_Brawl_SleighRide", displayName = "Sleigh Ride", icon = "map_sleighride", mode = GameMode.Payload, theme = Theme.Xmas, seed = 1303, pathHalf = new[] { new Vector2(0f, 16.5f), new Vector2(0f, 9), new Vector2(-6f, 4) },
            names = new[] { "Sleigh Ride", "Катание на санях", "นั่งเลื่อนหิมะ", "Paseo en Trineo", "Chuyến Xe Trượt Tuyết" },
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
        new Def {   // layout of Map_Brawl_RobotRidge
            mapEnum = "Map_Brawl_ToyWorkshop", displayName = "Toy Workshop", icon = "map_toyworkshop", mode = GameMode.MannVsMachine, theme = Theme.Xmas, seed = 1304, pathHalf = new[] { new Vector2(0f, 16.5f), new Vector2(0f, 9), new Vector2(-6f, 4) },
            names = new[] { "Toy Workshop", "Мастерская игрушек", "โรงงานของเล่น", "Taller de Juguetes", "Xưởng Đồ Chơi" },
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
        new Def {   // layout of Map_Brawl_BigfootGulch
            mapEnum = "Map_Brawl_SnowGlobe", displayName = "Snow Globe Arena", icon = "map_snowglobe", mode = GameMode.JungleBoss, theme = Theme.Xmas, seed = 1305,
            names = new[] { "Snow Globe Arena", "Арена снежного шара", "สนามลูกแก้วหิมะ", "Arena Bola de Nieve", "Đấu Trường Quả Cầu Tuyết" },
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
        new Def {   // layout of Map_Brawl_GhoulMine
            mapEnum = "Map_Brawl_CandyCaneAlley", displayName = "Candy Cane Alley", icon = "map_candycanealley", mode = GameMode.Zombie, theme = Theme.Xmas, seed = 1306,
            names = new[] { "Candy Cane Alley", "Аллея леденцов", "ซอยลูกกวาด", "Callejón de Bastones", "Hẻm Kẹo Gậy" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb.....x.x.....bbb.",
                ".bb...............bb.",
                "....##...bbb...##....",
                ".x.......bbb.......x.",
                "..##.............##..",
                "........#...#........",
                ".bb..t.........t..bb.",
                ".bb......###......bb.",
                "....##.........##....",
                ".....................",
            },
            center = "....x...........x....",
        },
        new Def {   // layout of Map_Brawl_DustyCanyon
            mapEnum = "Map_Brawl_LanternEve", displayName = "Lantern Eve", icon = "map_lanterneve", mode = GameMode.Competitive, theme = Theme.Xmas, seed = 1311, night = true,
            names = new[] { "Lantern Eve", "Вечер фонарей", "ค่ำคืนโคมไฟ", "Víspera de Faroles", "Đêm Đèn Lồng" },
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
        new Def {   // layout of Map_Brawl_GoldRush (mirrored)
            mapEnum = "Map_Brawl_MidnightGifts", displayName = "Midnight Gifts", icon = "map_midnightgifts", mode = GameMode.CaptureTheFlag, theme = Theme.Xmas, seed = 1312, night = true,
            names = new[] { "Midnight Gifts", "Полночные подарки", "ของขวัญเที่ยงคืน", "Regalos de Medianoche", "Quà Lúc Nửa Đêm" },
            half = new[] {
                ".....................",
                ".....................",
                "...x.............x...",
                "..fff...........fff..",
                "........bbbbb........",
                ".##.....bbbbb.....##.",
                ".....................",
                "....t..##...##..t....",
                ".bb...............bb.",
                ".bb...ff.....ff...bb.",
                ".bb...............bb.",
                "......##..x..##......",
                "...bb...........bb...",
            },
            center = "...bb....###....bb...",
        },
        new Def {   // layout of Map_Brawl_SheriffSquare (mirrored)
            mapEnum = "Map_Brawl_StarlightHill", displayName = "Starlight Hill", icon = "map_starlighthill", mode = GameMode.KingOfTheHill, theme = Theme.Xmas, seed = 1313, night = true,
            names = new[] { "Starlight Hill", "Звёздный холм", "เนินแสงดาว", "Colina Estelar", "Đồi Ánh Sao" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "....x...........x....",
                "..bb.............bb..",
                "..bb..fff...fff..bb..",
                ".....................",
                ".##......bbb......##.",
                ".......t.....t.......",
                ".bb.............bb...",
                ".bb.....##.##...bb...",
                ".....................",
                ".x....f.......f....x.",
            },
            center = "..bb...#.....#...bb..",
        },
        new Def {   // layout of Map_Brawl_DynamiteTrail (mirrored)
            mapEnum = "Map_Brawl_NightExpress", displayName = "Night Express", icon = "map_nightexpress", mode = GameMode.Payload, theme = Theme.Xmas, seed = 1314, night = true, pathHalf = new[] { new Vector2(-0f, 16.5f), new Vector2(8f, 10), new Vector2(9f, 2) },
            names = new[] { "Night Express", "Ночной экспресс", "รถด่วนยามค่ำ", "Expreso Nocturno", "Chuyến Tàu Đêm" },
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
                ".bb.............bb...",
                ".bb.......##....bb...",
                ".....................",
                ".x....f.......f....x.",
            },
            center = "..bb...#.....#...bb..",
        },
        new Def {   // layout of Map_Brawl_IronGrove (mirrored)
            mapEnum = "Map_Brawl_CandleWorks", displayName = "Candle Works", icon = "map_candleworks", mode = GameMode.MannVsMachine, theme = Theme.Xmas, seed = 1315, night = true, pathHalf = new[] { new Vector2(-0f, 16.5f), new Vector2(8f, 10), new Vector2(9f, 2) },
            names = new[] { "Candle Works", "Свечная фабрика", "โรงงานเทียน", "Fábrica de Velas", "Xưởng Nến" },
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
        new Def {   // layout of Map_Brawl_StompingGrounds
            mapEnum = "Map_Brawl_YetiEve", displayName = "Yeti's Eve", icon = "map_yetieve", mode = GameMode.JungleBoss, theme = Theme.Xmas, seed = 1316, night = true,
            names = new[] { "Yeti's Eve", "Вечер Йети", "ค่ำคืนเยติ", "Noche del Yeti", "Đêm Của Yeti" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb.....x.x.....bbb.",
                ".bbb.............bbb.",
                "....##...bbb...##....",
                ".x.......bbb.......x.",
                "..##.............##..",
                "........#...#........",
                ".bb..t.........t..bb.",
                ".bb......###......bb.",
                "....##.........##....",
                "..bb.............bb..",
            },
            center = "....x...........x....",
        },
        new Def {   // layout of Map_Brawl_CryptTunnels
            mapEnum = "Map_Brawl_SilentNight", displayName = "Silent Night", icon = "map_silentnight", mode = GameMode.Zombie, theme = Theme.Xmas, seed = 1317, night = true,
            names = new[] { "Silent Night", "Тихая ночь", "ราตรีเงียบสงบ", "Noche de Paz", "Đêm Yên Lặng" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bbb...........bbb..",
                "..###....bbb....###..",
                "..##.............##..",
                ".##.bb.........bb.##.",
                "........#...#........",
                ".bb.....#...#.....bb.",
                "....##.........##....",
                ".bb..t.........t..bb.",
                "..bb.............bb..",
                ".....................",
            },
            center = ".......#.....#.......",
        },
    };

    [MenuItem("Tools/Maps/Build Xmas Maps")]
    static void BuildXmasMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Xmas] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Xmas] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in XmasMaps) Debug.Log(Build(d));
    }

    static bool xmasNight;   // set by Build: night maps lean on lamps and lit trees
    static readonly System.Collections.Generic.HashSet<int> nightLampKeys = new System.Collections.Generic.HashSet<int>();
    static readonly System.Collections.Generic.List<Vector3> nightLampPos = new System.Collections.Generic.List<Vector3>();

    /// <summary>Night maps: about a third of the free tiles next to a wall get a street lamp (with its small collider, so
    /// the NavMesh baked later goes round it), so every night map has warm light all over, not only on its 't' tiles.
    /// Decided per canonical tile: both halves stay mirrored.</summary>
    static bool NightLamp(char[,] g, int r, int c, int cr, int cc, int seed, Vector3 p)
    {
        bool wall = false;
        for (int dr = -1; dr <= 1; dr++) for (int dc = -1; dc <= 1; dc++)
        {
            int rr = r + dr, c2 = c + dc;
            if ((dr != 0 || dc != 0) && rr >= 0 && rr < g.GetLength(0) && c2 >= 0 && c2 < g.GetLength(1) && g[rr, c2] == '#') wall = true;
        }
        int key = cr * 100 + cc;
        bool canonical = r < 13 || (r == 13 && c <= 10);   // the grid is walked row by row: canonical tiles come first
        if (canonical)
        {
            if (!wall || nightLampKeys.Count >= 6 || Mix(seed * 31 + cr * 977 + cc * 59) % 100 >= 30) return false;   // at most 6 per half
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) return false;
            if (nightLampPos.Any(q => (q - p).sqrMagnitude < 4.5f * 4.5f)) return false;   // spread out, no clusters
            nightLampKeys.Add(key); nightLampPos.Add(p);
        }
        else if (!nightLampKeys.Contains(key)) return false;   // the mirrored tile follows its twin
        Prefab("XmasLamp", p, (Mix(cr * 7 + cc) % 4) * 90f, 1f, true);
        return true;
    }

    static void XmasTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':   // presents stacked as walls, now and then a snow-capped ice block
                {
                    string k = v < 0.40 ? "GiftStackA" : v < 0.75 ? "GiftStackB" : "GlacierBlockB";
                    float s = 0.97f + (float)rnd.NextDouble() * 0.03f;
                    Prefab(k, p + new Vector3(Jit(rnd, 0.03f), 0, Jit(rnd, 0.03f)), Rot90(rnd) + baseRot + Jit(rnd, 3), s, true);
                }
                break;
            case 'x':
                if (v < 0.45) Prefab("GiftBox", p, Rot90(rnd) + Jit(rnd, 10), 1f, true);
                else if (v < 0.70) Prefab("Sleigh", p, Rot90(rnd) + Jit(rnd, 4), 1f, true);
                else Prefab("IceChunkA", p, Rot90(rnd) + Jit(rnd, 4), 1f, true);
                break;
            case 'f': Prefab("LightFence", p, fenceRot + Jit(rnd, 2), 1f, true); break;
            case 't' when xmasNight:
                if (v < 0.45) Prefab("XmasLamp", p, Rot90(rnd), 1f, true);
                else if (v < 0.85) Prefab("XmasTree", p, rnd.Next(360), 0.85f, true);
                else Prefab("Snowman2", p, baseRot + 180f + Jit(rnd, 30), 0.9f, true);
                break;
            case 't':
                if (v < 0.40) Prefab("XmasTree", p, rnd.Next(360), 0.85f, true);
                else if (v < 0.65) Prefab("XmasLamp", p, Rot90(rnd), 1f, true);
                else if (v < 0.80) Prefab("CandyCane", p, rnd.Next(360), 1f, true);
                else Prefab("Snowman2", p, baseRot + 180f + Jit(rnd, 30), 0.9f, true);   // facing the middle
                break;
            case 'b': Grass("PineGrass", p, 1.3f, rnd); break;   // ~1.95 m, like the other bushes
        }
    }

    static void XmasDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "BigBauble", "GiftBox", "GiftBox", "BigBauble", "GiftBox");   // (the old SnowDrift reads as a white log here)
        if (!prefabs.ContainsKey(k)) k = "GiftBox";
        float sc = k == "SnowDrift" ? 0.6f : k == "BigBauble" ? 0.7f + (float)rnd.NextDouble() * 0.2f : 0.45f;
        Prefab(k, o, rot, sc, false, null, k == "SnowDrift" ? new Vector3(sc, sc * 0.35f, sc) : (Vector3?)null);   // drifts lie flat
    }

    /// <summary>Night look: swap the prop / snow / glow materials for their night versions (dark blue, warm glow), dim
    /// the scene, and lay warm light pools on the snow under every lamp, lit tree and light fence.</summary>
    static void XmasNight()
    {
        var swap = new System.Collections.Generic.Dictionary<Material, Material>();
        void Map(string a, string b)
        {
            var ma = AssetDatabase.LoadAssetAtPath<Material>(a); var mb = AssetDatabase.LoadAssetAtPath<Material>(b);
            if (ma && mb) swap[ma] = mb;
        }
        Map(IceThemeSetup.PropsMat, IceThemeSetup.PropsNightMat);
        Map(IceThemeSetup.SnowGroundMat, IceThemeSetup.SnowNightMat);
        Map(IceThemeSetup.GlowMat, IceThemeSetup.GlowNightMat);
        int swapped = 0;
        foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
        {
            var mats = r.sharedMaterials; bool ch = false;
            for (int i = 0; i < mats.Length; i++) if (mats[i] && swap.TryGetValue(mats[i], out var n)) { mats[i] = n; ch = true; }
            if (ch) { r.sharedMaterials = mats; swapped++; }
        }
        // scene: moonlit navy fog and background
        var navy = new Color(0.08f, 0.10f, 0.22f);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.30f, 0.34f, 0.58f); RenderSettings.ambientEquatorColor = new Color(0.22f, 0.25f, 0.45f); RenderSettings.ambientGroundColor = new Color(0.16f, 0.18f, 0.32f);
        RenderSettings.fogColor = navy;
        var light = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).FirstOrDefault(l => l.type == LightType.Directional);
        if (light) { light.color = new Color(0.70f, 0.76f, 1.0f); light.intensity = 0.6f; }
        foreach (var cam in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)))
        { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = navy; }
        // warm pools under the lights
        int pools = 0;
        if (prefabs.ContainsKey("WarmPool"))
            foreach (Transform t in env.Cast<Transform>().ToList())
            {
                var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                if (!src) continue;
                float size = src.name == "XmasLamp" ? 5.0f : src.name == "XmasTree" ? 4.0f : src.name == "LightFence" ? 2.6f : src.name == "XmasGate" ? 7.0f : 0f;
                if (size <= 0f) continue;
                var pool = Prefab("WarmPool", t.position + Vector3.up * 0.05f, 0f, 1f, false);
                if (pool) { pool.transform.localScale = new Vector3(size * t.lossyScale.x, 1f, size * t.lossyScale.z); pools++; }
            }
        log += " night: swapped=" + swapped + " pools=" + pools;
    }

    /// <summary>Beyond the border: glaciers turned to the arena, big decorated pines, snowmen and lamps.</summary>
    static void PlaceXmasBackdrop(int seed)
    {
        PlaceGates("XmasGate");   // the village gate behind each spawn
        var rnd = new Random(seed);
        for (int i = 0; i < 28; i++)
        {
            float a = i / 28f * Mathf.PI * 2f + Jit(rnd, 0.06f);
            float rad = 33f + (float)rnd.NextDouble() * 7f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.85f, 0f, Mathf.Sin(a) * rad);
            var k = Pick(rnd, "XmasTree", "XmasTree", "XmasTree", "GlacierWallA", "GlacierWallB", "Snowman2", "XmasLamp", "GiftStackA");
            if (!prefabs.ContainsKey(k)) k = "IceSpire";
            float sc = k == "XmasTree" ? 1.3f + (float)rnd.NextDouble() * 0.5f : k.StartsWith("Glacier") ? 0.9f + (float)rnd.NextDouble() * 0.4f : k == "Snowman2" ? 1.3f : 1.2f;
            float face = Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg;   // +Z looks at the arena centre
            float yaw = k.StartsWith("Glacier") ? face + Jit(rnd, 15) : k == "Snowman2" ? face + 180f + Jit(rnd, 20) : rnd.Next(360);
            Prefab(k, p, yaw, sc, false);
        }
    }

    /// <summary>Snow drifts, baubles and a few presents on the open floor (no colliders), clear of walls and spawns.</summary>
    static void ScatterXmasFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        for (int i = 0; i < 140 && placed < 14; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;   // clear of the floor collider
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            var k = Pick(rnd, "BigBauble", "GiftBox", "GiftBox");
            if (!prefabs.ContainsKey(k)) k = "GiftBox";
            float sc = k == "SnowDrift" ? 0.5f + (float)rnd.NextDouble() * 0.3f : k == "BigBauble" ? 0.8f : 0.5f;
            Prefab(k, p, rnd.Next(360), sc, false, null, k == "SnowDrift" ? new Vector3(sc, sc * 0.35f, sc) : (Vector3?)null);
            placed++;
        }
        log += " xmasScatter=" + placed;
    }
}
