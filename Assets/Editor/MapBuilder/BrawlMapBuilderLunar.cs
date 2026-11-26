using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Lunar theme (1960s-70s Space Age, retro-futurist; not the old asset-pack Theme.Moon): a moon base on grey regolith.
/// Props from Tools/IceArt/props15.py (same folder and palette as the other themes, set up by IceThemeSetup): moon
/// boulders, supply canister racks, fuel tanks (one family per wall), a buggy, supply pods, a geology drill, regolith
/// ridges, dish antennas, sounding rockets, floodlight masts, beacons, alien fronds (StarGrass) as the bushes, small
/// craters and boot prints. Border: the rugged rim of a big crater (CraterWallA/B/C), the airlock gate behind each
/// spawn; habitat domes, a lander, a launch rocket and the Earth in a black starry sky beyond.
/// </summary>
public static partial class BrawlMapBuilder
{
    public static readonly Def[] LunarMaps =
    {
        new Def {   // a new layout
            mapEnum = "Map_Brawl_CraterBase", displayName = "Crater Base", icon = "map_craterbase", mode = GameMode.Competitive, theme = Theme.Lunar, seed = 1901,
            names = new[] { "Crater Base", "Кратерная база", "ฐานปล่องดวงจันทร์", "Base del Cráter", "Căn Cứ Miệng Hố" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bbb...........bbb..",
                "..bbb....###....bbb..",
                ".......x.....x.......",
                ".##..t.........t..##.",
                ".....bbb.....bbb.....",
                "..f..bbb.....bbb..f..",
                ".........#.#.........",
                ".##...x.......x...##.",
                "......bb.....bb......",
                "......bb..t..bb......",
            },
            center = "...##...#...#...##...",
        },
        new Def {   // layout of Map_Brawl_FrostVault (mirrored)
            mapEnum = "Map_Brawl_MoonRockRaid", displayName = "Moon Rock Raid", icon = "map_moonrockraid", mode = GameMode.CaptureTheFlag, theme = Theme.Lunar, seed = 1902,
            names = new[] { "Moon Rock Raid", "Набег за лунным камнем", "บุกชิงหินดวงจันทร์", "Asalto a la Roca Lunar", "Đột Kích Đá Mặt Trăng" },
            half = Mir(
                ".....................",
                ".....................",
                "...##...........##...",
                "...#...x.....x...#...",
                ".bb.....bbbbb.....bb.",
                ".bb..f..bbbbb..f..bb.",
                "......##.....##......",
                "..t...............t..",
                ".....x..##.##..x.....",
                ".bbb.............bbb.",
                ".bbb...bb...bb...bbb.",
                "....##..bb.bb..##....",
                "..x......#.#......x.."),
            center = ".....##.......##.....",
        },
        new Def {   // layout of Map_Brawl_GlacierThrone (mirrored)
            mapEnum = "Map_Brawl_TranquilityHill", displayName = "Tranquility Hill", icon = "map_tranquilityhill", mode = GameMode.KingOfTheHill, theme = Theme.Lunar, seed = 1903,
            names = new[] { "Tranquility Hill", "Холм Спокойствия", "เนินแห่งความสงบ", "Colina de la Tranquilidad", "Đồi Tĩnh Lặng" },
            half = Mir(
                ".....................",
                ".....................",
                ".....................",
                "..bbb...#...#...bbb..",
                "..bbb...#...#...bbb..",
                ".....x.........x.....",
                ".##.....bbbbb.....##.",
                ".#......bbbbb......#.",
                "....t....f.f....t....",
                "..bb....#...#....bb..",
                "..bb.##.......##.bb..",
                "........x...x........",
                "...##...........##..."),
            center = ".....#.........#.....",
        },
        new Def {   // layout of Map_Brawl_MineExpress (mirrored), path A mirrored
            mapEnum = "Map_Brawl_LunarExpress", displayName = "Lunar Express", icon = "map_lunarexpress", mode = GameMode.Payload, theme = Theme.Lunar, seed = 1904,
            pathHalf = MirPath(new Vector2(0, 16.5f), new Vector2(0, 9), new Vector2(-6, 4)),
            names = new[] { "Lunar Express", "Лунный экспресс", "รถด่วนดวงจันทร์", "Expreso Lunar", "Tàu Tốc Hành Mặt Trăng" },
            half = Mir(
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
                ".....#.........#....."),
            center = "..bb.............bb..",
        },
        new Def {   // layout of Map_Brawl_LastStand (mirrored), path C
            mapEnum = "Map_Brawl_OrbitalWorks", displayName = "Orbital Works", icon = "map_orbitalworks", mode = GameMode.MannVsMachine, theme = Theme.Lunar, seed = 1905,
            pathHalf = new[] { new Vector2(0, 16.5f), new Vector2(0, 5) },
            names = new[] { "Orbital Works", "Орбитальный завод", "โรงงานวงโคจร", "Taller Orbital", "Xưởng Quỹ Đạo" },
            half = Mir(
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
                "...........t........."),
            center = ".bbb....#####....bbb.",
        },
        new Def {   // layout of Map_Brawl_MoonlitDen (retired)
            mapEnum = "Map_Brawl_StardustArena", displayName = "Stardust Arena", icon = "map_stardustarena", mode = GameMode.JungleBoss, theme = Theme.Lunar, seed = 1906,
            names = new[] { "Stardust Arena", "Арена звёздной пыли", "สังเวียนละอองดาว", "Arena de Polvo Estelar", "Đấu Trường Bụi Sao" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb.............bbb.",
                ".##.bb.........bb.##.",
                "....##.........##....",
                ".bbb.....x.x.....bbb.",
                ".##...............##.",
                "........#...#........",
                "..t......b.b......t..",
                "..###....bbb....###..",
                ".....................",
                ".....................",
            },
            center = "....x...........x....",
        },
        new Def {   // layout of Map_Brawl_MoonGraveyard (mirrored)
            mapEnum = "Map_Brawl_DarkSideCrater", displayName = "Dark Side Crater", icon = "map_darksidecrater", mode = GameMode.Zombie, theme = Theme.Lunar, seed = 1907,
            names = new[] { "Dark Side Crater", "Кратер тёмной стороны", "ปล่องด้านมืด", "Cráter del Lado Oscuro", "Miệng Hố Phía Tối" },
            half = Mir(
                ".....................",
                ".....................",
                ".....................",
                ".bb...x.......x...bb.",
                ".bb...............bb.",
                ".bb..##.......##..bb.",
                ".bb.....#...#.....bb.",
                ".bb......###......bb.",
                ".bbb.............bbb.",
                "....##...x.x...##....",
                "..t......b.b......t..",
                "........bbbbb........",
                "....................."),
            center = "...bb...........bb...",
        },
    };

    [MenuItem("Tools/Maps/Build Lunar Maps")]
    static void BuildLunarMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Lunar] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Lunar] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in LunarMaps) Debug.Log(Build(d));
    }

    static void LunarTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':
                {
                    // one family per wall (PrepareVolcanoWalls): boulders (0 and 3), canister racks (1), fuel tanks (2 and 4)
                    int fam = wallFamily.TryGetValue(TileOf(p), out var f) ? f : 0;
                    var jit = p + new Vector3(Jit(rnd, 0.02f), 0, Jit(rnd, 0.02f));
                    if (fam == 1) Prefab("CargoStack", jit, fenceRot + Jit(rnd, 3), 1f, true);
                    else if (fam == 2 || fam == 4) Prefab("FuelTanks", jit, Rot90(rnd) + Jit(rnd, 4), 1f, true);
                    else Prefab(v < 0.5 ? "MoonBoulderA" : "MoonBoulderB", jit, rnd.Next(360), 0.95f + (float)rnd.NextDouble() * 0.1f, true);
                }
                break;
            case 'x':
                if (v < 0.30) Prefab("MoonBuggy", p, rnd.Next(360), 1f, true);
                else if (v < 0.65) Prefab("SupplyPods", p, rnd.Next(360), 1f, true);
                else Prefab("RockSampler", p, rnd.Next(360), 1f, true);
                break;
            case 'f': Prefab("RockRidge", p, fenceRot + Jit(rnd, 6), 1f, true); break;
            case 't':
                if (v < 0.25) Prefab("RadioDish", p, baseRot + Jit(rnd, 60), 1f, true);
                else if (v < 0.50) Prefab("MiniRocket", p, rnd.Next(360), 1f, true);
                else if (v < 0.75) Prefab("FloodMast", p, baseRot + 180f + Jit(rnd, 20), 1f, true);
                else Prefab("Beacon", p, rnd.Next(360), 1f, true);
                break;
            case 'b': Grass("StarGrass", p, 1.3f, rnd); break;   // ~1.95 m, like the other bushes
        }
    }

    static void LunarDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "SmallCrater", "MoonPebbles", "Footprints", "SmallCrater", "MoonPebbles");
        if (!prefabs.ContainsKey(k)) k = "MoonPebbles";
        Prefab(k, o, rot + rnd.Next(360), k == "SmallCrater" ? 0.7f + (float)rnd.NextDouble() * 0.5f : 1f, false);
    }

    /// <summary>Harsh sunlight in a black starry sky: a cool neutral ambient (bright enough to read the props), a dark
    /// blue haze at the horizon, the StarSky skybox (the map scenes have no camera of their own).</summary>
    static void LunarLook(Light light)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.86f, 0.88f, 0.94f); RenderSettings.ambientEquatorColor = new Color(0.78f, 0.80f, 0.86f); RenderSettings.ambientGroundColor = new Color(0.62f, 0.62f, 0.66f);
        LunarSky();
        if (light) light.color = new Color(1.0f, 0.99f, 0.96f);
    }

    static void LunarSky()
    {
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(IceThemeSetup.StarSkyMat);
        RenderSettings.fogColor = new Color(0.06f, 0.08f, 0.16f);
    }

    /// <summary>Beyond the crater rim: habitat domes, a lander, a launch rocket on its pad (mirrored for both teams),
    /// boulders and dishes around, the Earth high in the sky. Also the airlock gate behind each team's spawn.</summary>
    static void PlaceLunarBackdrop(int seed)
    {
        PlaceGates("AirlockGate");
        LunarSky();
        var rnd = new Random(seed);
        for (int s = -1; s <= 1; s += 2)
        {
            Prefab("MoonDome", new Vector3(s * 20f, 0f, s * 38f), s > 0 ? 180f : 0f, 1f, false);
            Prefab("LaunchRocket", new Vector3(-s * 24f, 0f, s * 44f), 0f, 1.2f, false);
            Prefab("Lander", new Vector3(s * 32f, 0f, -s * 10f), rnd.Next(360), 1.2f, false);
        }
        var earth = Prefab("Earth", new Vector3(-62f, 34f, 8f), 25f, 11f, false);
        for (int i = 0; i < 22; i++)
        {
            float a = i / 22f * Mathf.PI * 2f + Jit(rnd, 0.08f);
            float rad = 30f + (float)rnd.NextDouble() * 9f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.85f, 0f, Mathf.Sin(a) * rad);
            var k = Pick(rnd, "MoonBoulderA", "MoonBoulderB", "MoonBoulderA", "RadioDish", "FuelTanks", "SmallCrater");
            float sc = k.StartsWith("MoonBoulder") ? 1.8f + (float)rnd.NextDouble() * 1.2f : k == "SmallCrater" ? 3f : 1.4f;
            Prefab(k, p, rnd.Next(360), sc, false);
        }
    }

    /// <summary>Small craters, pebbles and boot prints on the open regolith (no colliders), clear of walls and spawns.</summary>
    static void ScatterLunarFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        for (int i = 0; i < 160 && placed < 14; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            var k = Pick(rnd, "SmallCrater", "MoonPebbles", "Footprints", "SmallCrater");
            if (!prefabs.ContainsKey(k)) k = "MoonPebbles";
            Prefab(k, p, rnd.Next(360), k == "SmallCrater" ? 0.8f + (float)rnd.NextDouble() * 0.6f : 1f, false);
            placed++;
        }
        log += " lunarScatter=" + placed;
    }
}
