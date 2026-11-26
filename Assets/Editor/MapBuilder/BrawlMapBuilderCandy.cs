using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Candy theme (a candy kingdom in the spirit of a 1960s-70s sweet shop). Props from Tools/IceArt/props18.py (same
/// folder and palette as the other themes; pink faces in material slot 1 = CandyPink, set up by IceThemeSetup +
/// CandyThemeSetup): wafer stacks, layer cakes, chocolate bars, gumdrop heaps and marshmallow columns (one family per
/// wall), cupcakes, a gumball machine, wrapped sweets, doughnuts, peppermint-stick fences, swirl lollipops, ice cream
/// cones, gumdrop trees, gummy bears, pink sugar grass as the bushes. Border: low layer-cake banks (CandyWallA/B/C) with
/// a chocolate lake beyond, the CandyGate behind each spawn; a candy castle, gingerbread houses and tiered cakes around.
/// Lighting: the softer Greek light (GreekLook / GreekSun) under a pastel sky.
/// </summary>
public static partial class BrawlMapBuilder
{
    public static readonly Def[] CandyMaps =
    {
        new Def {   // a new layout
            mapEnum = "Map_Brawl_CandyPlaza", displayName = "Candy Plaza", icon = "map_candyplaza", mode = GameMode.Competitive, theme = Theme.Candy, seed = 2201,
            names = new[] { "Candy Plaza", "Конфетная площадь", "ลานลูกกวาด", "Plaza de los Dulces", "Quảng Trường Kẹo Ngọt" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##....bbbbb....##..",
                "..#.....bbbbb.....#..",
                "......x.......x......",
                ".bbb...t.....t...bbb.",
                ".bbb.............bbb.",
                ".....##..f.f..##.....",
                "..t...............t..",
                "......bb.....bb......",
                ".##...bb..x..bb...##.",
                ".....................",
            },
            center = "...x....#####....x...",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_CookieHeist", displayName = "Cookie Heist", icon = "map_cookieheist", mode = GameMode.CaptureTheFlag, theme = Theme.Candy, seed = 2202,
            names = new[] { "Cookie Heist", "Похищение печенья", "ปล้นคุกกี้", "Atraco de Galletas", "Phi Vụ Bánh Quy" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb....##.##....bbb.",
                ".bbb.............bbb.",
                "....x...........x....",
                "..##....bbbbb....##..",
                "..#.....bbbbb.....#..",
                "......t.......t......",
                ".bb...............bb.",
                ".bb..##..f.f..##..bb.",
                "........x...x........",
                "...#.............#...",
            },
            center = "......bb.....bb......",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_FrostingHill", displayName = "Frosting Hill", icon = "map_frostinghill", mode = GameMode.KingOfTheHill, theme = Theme.Candy, seed = 2203,
            names = new[] { "Frosting Hill", "Холм глазури", "เนินครีมหวาน", "Colina de Glaseado", "Đồi Kem Phủ" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..t.....#...#.....t..",
                ".bbb.............bbb.",
                ".bbb..##.....##..bbb.",
                "......#...x...#......",
                ".....................",
                "..bb..bb.....bb..bb..",
                "..bb.............bb..",
                ".....x.........x.....",
                "..##....f...f....##..",
                ".....................",
            },
            center = ".t.................t.",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_CaramelExpress", displayName = "Caramel Express", icon = "map_caramelexpress", mode = GameMode.Payload, theme = Theme.Candy, seed = 2204,
            pathHalf = new[] { new Vector2(0, 16.5f), new Vector2(0, 9), new Vector2(-6, 4) },
            names = new[] { "Caramel Express", "Карамельный экспресс", "รถด่วนคาราเมล", "Expreso de Caramelo", "Tàu Tốc Hành Caramel" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##.............##..",
                "..#...bb.....bb...#..",
                "......bb.....bb......",
                ".t.................t.",
                "....##.........##....",
                ".bbb.....x.......bbb.",
                ".bbb..............t..",
                "......x..........#...",
                "..##.....bb.......##.",
                "......t.........x....",
            },
            center = "..bb.............bb..",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_ToffeeFactory", displayName = "Toffee Factory", icon = "map_toffeefactory", mode = GameMode.MannVsMachine, theme = Theme.Candy, seed = 2205,
            pathHalf = new[] { new Vector2(0, 16.5f), new Vector2(-8, 10), new Vector2(-9, 2) },
            names = new[] { "Toffee Factory", "Ирисковая фабрика", "โรงงานทอฟฟี่", "Fábrica de Caramelos", "Xưởng Kẹo Bơ" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bb..##...........bb.",
                ".bb..#............bb.",
                "........x............",
                "...t.....bbb.......#.",
                ".........bbb.......#.",
                "..##.........x.......",
                "..#...t............t.",
                ".......bb..........bb",
                "....f.....##.........",
                ".x...................",
            },
            center = "...bb...........bb...",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_GumdropArena", displayName = "Gumdrop Arena", icon = "map_gumdroparena", mode = GameMode.JungleBoss, theme = Theme.Candy, seed = 2206,
            names = new[] { "Gumdrop Arena", "Арена мармеладок", "สังเวียนเยลลี่", "Arena de Gominolas", "Đấu Trường Kẹo Dẻo" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "....x...........x....",
                "..##....bbbbb....##..",
                "..#...............#..",
                "......t.......t......",
                ".bb...............bb.",
                ".bb....##...##....bb.",
                ".....................",
                "...x.....bbb.....x...",
                "..........t..........",
                ".....................",
            },
            center = ".....##.......##.....",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_RottenSweets", displayName = "Rotten Sweets", icon = "map_rottensweets", mode = GameMode.Zombie, theme = Theme.Candy, seed = 2207,
            names = new[] { "Rotten Sweets", "Гнилые сладости", "ขนมเน่า", "Dulces Podridos", "Kẹo Thiu" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".##..x.........x..##.",
                ".#.................#.",
                "....bbb.......bbb....",
                "....bbb...t...bbb....",
                "..t...............t..",
                ".....##..f.f..##.....",
                ".bb...............bb.",
                ".bb....x.....x....bb.",
                ".....##.......##.....",
                ".....................",
            },
            center = "..x......###......x..",
        },
    };

    [MenuItem("Tools/Maps/Build Candy Maps")]
    static void BuildCandyMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Candy] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Candy] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in CandyMaps) Debug.Log(Build(d));
    }

    static void CandyTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':
                {
                    // one family per wall (PrepareVolcanoWalls): wafers (0), layer cakes (1), chocolate bars (2), gumdrops (3), marshmallows (4)
                    int fam = wallFamily.TryGetValue(TileOf(p), out var f) ? f : 0;
                    var jit = p + new Vector3(Jit(rnd, 0.02f), 0, Jit(rnd, 0.02f));
                    string k = fam switch { 1 => "CakeBlock", 2 => "ChocoBlock", 3 => "GumdropStack", 4 => "MarshmallowStack", _ => "WaferBlock" };
                    float rot = fam == 2 ? fenceRot + rnd.Next(2) * 180f + Jit(rnd, 3) : fam == 3 || fam == 4 ? rnd.Next(360) : Rot90(rnd) + Jit(rnd, 3);
                    Prefab(k, jit, rot, 1f, true);
                }
                break;
            case 'x':
                if (v < 0.30) Prefab("Cupcake", p, rnd.Next(360), 1f, true);
                else if (v < 0.50) Prefab("GumballMachine", p, baseRot + 180f + Jit(rnd, 30), 1f, true);
                else if (v < 0.75) Prefab("WrappedSweets", p, rnd.Next(360), 0.9f, true);
                else Prefab("DonutStack", p, rnd.Next(360), 1f, true);
                break;
            case 'f': Prefab("StickFence", p, fenceRot + Jit(rnd, 3), 1f, true); break;
            case 't':
                if (v < 0.20) Prefab("LollipopA", p, baseRot + Jit(rnd, 25), 1f, true);
                else if (v < 0.40) Prefab("LollipopB", p, baseRot + Jit(rnd, 25), 1f, true);
                else if (v < 0.60) Prefab("IceCreamCone", p, rnd.Next(360), 1f, true);
                else if (v < 0.85) Prefab("GumdropTree", p, rnd.Next(360), 1f, true);
                else Prefab("GummyBear", p, baseRot + 180f + Jit(rnd, 25), 1f, true);
                break;
            case 'b': Grass("SugarGrass", p, 1.3f, rnd); break;   // ~1.95 m, like the other bushes
        }
    }

    static void CandyDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "Sprinkles", "WrappedCandy", "ChocoChips", "Sprinkles", "WrappedCandy");
        if (!prefabs.ContainsKey(k)) k = "Sprinkles";
        Prefab(k, o, rot + rnd.Next(360), 1f, false);
    }

    static readonly Color CandyHaze = new Color(0.94f, 0.86f, 0.92f);
    static void CandySky()
    {
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(CandyThemeSetup.SkyMat);
        RenderSettings.fogColor = CandyHaze;
        foreach (var cam in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)))
        { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = CandyHaze; }
    }

    /// <summary>Beyond the cake banks: a chocolate lake all round (it starts under the banks, none of it inside the
    /// arena), a candy castle off each end, gingerbread houses, tiered cakes, giant lollipops and gumdrop trees.
    /// Also the candy gate behind each team's spawn.</summary>
    static void PlaceCandyBackdrop(int seed)
    {
        PlaceGates("CandyGate");
        CandySky();
        var walls = env.Cast<Transform>().Where(t => t.name.StartsWith("CandyWall")).ToList();
        float left = walls.Where(t => t.position.x < 0).Select(t => -t.position.x).DefaultIfEmpty(17f).Max();
        float right = walls.Where(t => t.position.x > 0).Select(t => t.position.x).DefaultIfEmpty(17f).Max();
        int lakes = 0;
        void Lake(float x0, float x1, float z0, float z1)
        {
            var go = Prefab("ChocoLake", new Vector3((x0 + x1) / 2f, 0.03f, (z0 + z1) / 2f), 0f, 1f, false);
            if (go) { go.transform.localScale = new Vector3(Mathf.Abs(x1 - x0), 1f, Mathf.Abs(z1 - z0)); foreach (var c in go.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = 0; lakes++; }
        }
        Lake(-left, -120f, -120f, 120f);
        Lake(right, 120f, -120f, 120f);
        Lake(-left, right, 24.2f, 120f);
        Lake(-left, right, -24.2f, -120f);
        log += " chocoLake=" + lakes;
        var rnd = new Random(seed);
        for (int s = -1; s <= 1; s += 2)   // mirrored, so both teams look at the same things
        {
            Prefab("CandyCastle", new Vector3(s * 6f, -0.3f, s * 50f), s > 0 ? 180f : 0f, 1f, false);
            Prefab("GingerbreadHouse", new Vector3(s * 30f, -0.1f, s * 8f), s > 0 ? -90f : 90f, 1.2f, false);
            Prefab("LayerCake", new Vector3(-s * 31f, 0f, s * 16f), rnd.Next(360), 1.2f, false);
        }
        for (int i = 0; i < 26; i++)
        {
            float a = i / 26f * Mathf.PI * 2f + Jit(rnd, 0.08f);
            float rad = 31f + (float)rnd.NextDouble() * 12f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.9f, 0f, Mathf.Sin(a) * rad);
            if (Mathf.Abs(p.x) < Mathf.Max(left, right) + 3f && Mathf.Abs(p.z) < 28f) continue;
            if (Mathf.Abs(Mathf.Abs(p.x) - 30.5f) < 5f && Mathf.Abs(Mathf.Abs(p.z) - 12f) < 8f) continue;   // the houses and cakes
            var k = Pick(rnd, "LollipopA", "LollipopB", "GumdropTree", "GumdropTree", "IceCreamCone", "GumdropStack", "MarshmallowStack", "Cupcake");
            float sc = k.StartsWith("Lollipop") || k == "IceCreamCone" ? 1.6f + (float)rnd.NextDouble() * 0.6f : k == "GumdropTree" ? 1.5f + (float)rnd.NextDouble() * 0.6f : 1.8f + (float)rnd.NextDouble() * 0.8f;
            Prefab(k, p, rnd.Next(360), sc, false);
        }
    }

    /// <summary>Sprinkles, wrapped sweets and chocolate chips on the open icing (no colliders), clear of walls and spawns.</summary>
    static void ScatterCandyFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        for (int i = 0; i < 160 && placed < 16; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            var k = Pick(rnd, "Sprinkles", "WrappedCandy", "ChocoChips", "Sprinkles");
            if (!prefabs.ContainsKey(k)) k = "Sprinkles";
            Prefab(k, p, rnd.Next(360), 1f, false);
            placed++;
        }
        log += " candyScatter=" + placed;
    }
}
