using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Circus theme: a 1960s-70s travelling circus and funfair on a summer field. Props from Tools/IceArt/props14.py (same
/// folder and palette as the other themes, set up by IceThemeSetup): painted cage wagons, steamer trunks, hay bales (one
/// family per wall), elephant tubs, a popcorn cart, a circus cannon, rope barriers, a high striker, a ticket booth,
/// globe lamps, balloon posts, long field grass (FairGrass), a trampled grass floor with sawdust paths. Border: the
/// striped canvas sidewall of the big top (CircusWallA/B/C); the fairground arch behind each spawn; the big top, a Ferris
/// wheel and a carousel beyond. Daylight like the desert.
/// </summary>
public static partial class BrawlMapBuilder
{
    public static readonly Def[] CircusMaps =
    {
        new Def {   // a new layout
            mapEnum = "Map_Brawl_CircusRing", displayName = "Circus Ring", icon = "map_circusring", mode = GameMode.Competitive, theme = Theme.Circus, seed = 1701,
            names = new[] { "Circus Ring", "Цирковая арена", "ลานละครสัตว์", "Pista del Circo", "Sân Khấu Xiếc" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bbb....x.x....bbb..",
                "..bbb...........bbb..",
                "......##.....##......",
                ".t..f...........f..t.",
                "......bbb...bbb......",
                "..##..bbb...bbb..##..",
                ".........###.........",
                "..x...............x..",
                "..bbb...t...t...bbb..",
                "..bbb...........bbb..",
            },
            center = ".....f....x....f.....",
        },
        new Def {   // layout of Map_Brawl_BorderSiege (mirrored)
            mapEnum = "Map_Brawl_BigTopRaid", displayName = "Big Top Raid", icon = "map_bigtopraid", mode = GameMode.CaptureTheFlag, theme = Theme.Circus, seed = 1702,
            names = new[] { "Big Top Raid", "Набег на шапито", "บุกเต็นท์ใหญ่", "Asalto a la Carpa", "Đột Kích Lều Xiếc" },
            half = Mir(
                ".....................",
                ".....................",
                "..t.............t....",
                "....##.........##....",
                "....#...bbbbb...#....",
                ".bb..................",
                ".bb....x.....x....bb.",
                "..........#..........",
                "...bbb.........bbb...",
                "...bbb..##.##..bbb...",
                ".....................",
                ".##.......t.......##.",
                ".....bb.......bb....."),
            center = "x.....#.......#.....x",
        },
        new Def {   // layout of Map_Brawl_CrossroadRuins (mirrored)
            mapEnum = "Map_Brawl_RingmasterHill", displayName = "Ringmaster Hill", icon = "map_ringmasterhill", mode = GameMode.KingOfTheHill, theme = Theme.Circus, seed = 1703,
            names = new[] { "Ringmaster Hill", "Холм конферансье", "เนินพิธีกรละครสัตว์", "Colina del Maestro", "Đồi Người Dẫn Trò" },
            half = Mir(
                ".....................",
                ".....................",
                ".....................",
                "...##...........##...",
                "......bb..t..bb......",
                "......bb.....bb......",
                ".t.................t.",
                "...bbb...###...bbb...",
                "...bbb.........bbb...",
                ".....................",
                ".##....x.....x....##.",
                ".......bb...bb.......",
                "...#...bb...bb...#..."),
            center = "...#.............#...",
        },
        new Def {   // layout of Map_Brawl_FrozenRail (mirrored), its path mirrored
            mapEnum = "Map_Brawl_CircusTrain", displayName = "Circus Train", icon = "map_circustrain", mode = GameMode.Payload, theme = Theme.Circus, seed = 1704,
            pathHalf = MirPath(new Vector2(-4, 16.5f), new Vector2(-4, 6), new Vector2(4, 2)),
            names = new[] { "Circus Train", "Цирковой поезд", "รถไฟละครสัตว์", "Tren del Circo", "Đoàn Tàu Xiếc" },
            half = Mir(
                ".....................",
                ".....................",
                ".....................",
                "..##.............##..",
                "..#..bb.......bb..#..",
                ".....bb...x...bb.....",
                ".bbb.............bbb.",
                ".bbb..##.....##..bbb.",
                "........t...t........",
                "..x......bbb......x..",
                ".##......bbb......##.",
                ".....f.........f.....",
                "...bb...#...#...bb..."),
            center = "...bb...........bb...",
        },
        new Def {   // layout of Map_Brawl_IcebreakerPass (mirrored), its path mirrored
            mapEnum = "Map_Brawl_ClockworkFair", displayName = "Clockwork Fair", icon = "map_clockworkfair", mode = GameMode.MannVsMachine, theme = Theme.Circus, seed = 1705,
            pathHalf = MirPath(new Vector2(6, 16.5f), new Vector2(6, 8), new Vector2(-6, 5), new Vector2(-6, 1)),
            names = new[] { "Clockwork Fair", "Заводная ярмарка", "งานรื่นเริงไขลาน", "Feria de Cuerda", "Hội Chợ Lên Dây Cót" },
            half = Mir(
                ".....................",
                ".....................",
                ".....................",
                ".bb..x.........x..bb.",
                ".bb.....##.##.....bb.",
                ".bb...............bb.",
                "....##..bbbbb..##....",
                "........bbbbb........",
                ".t.......f.f.......t.",
                "...###.........###...",
                ".bb......x.x......bb.",
                ".bb...............bb.",
                "....##...#.#...##...."),
            center = "..bb.....#.#.....bb..",
        },
        new Def {   // layout of Map_Brawl_FrontierOutpost
            mapEnum = "Map_Brawl_StrongmanArena", displayName = "Strongman Arena", icon = "map_strongmanarena", mode = GameMode.JungleBoss, theme = Theme.Circus, seed = 1706,
            names = new[] { "Strongman Arena", "Арена силача", "สังเวียนจอมพลัง", "Arena del Forzudo", "Sàn Đấu Lực Sĩ" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".bbb.....x.x.....bbb.",
                "....##...bbb...##....",
                "..##.............##..",
                ".bb..t.........t..bb.",
                ".bb......###......bb.",
                "....##.........##....",
                ".##...............##.",
                "..t......b.b......t..",
                ".....................",
                ".....................",
            },
            center = "...bb...........bb...",
        },
        new Def {   // layout of Map_Brawl_ZombieYard (mirrored)
            mapEnum = "Map_Brawl_HauntedFair", displayName = "Haunted Fair", icon = "map_hauntedfair", mode = GameMode.Zombie, theme = Theme.Circus, seed = 1707,
            names = new[] { "Haunted Fair", "Ярмарка призраков", "งานวัดผีสิง", "Feria Embrujada", "Hội Chợ Ma Ám" },
            half = Mir(
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
                "..bb.............bb.."),
            center = "....x...........x....",
        },
    };

    [MenuItem("Tools/Maps/Build Circus Maps")]
    static void BuildCircusMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Circus] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Circus] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in CircusMaps) Debug.Log(Build(d));
    }

    static void CircusTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':
                {
                    // one family per wall (PrepareVolcanoWalls): show wagons (0 and 3), trunks (1), hay (2 and 4)
                    int fam = wallFamily.TryGetValue(TileOf(p), out var f) ? f : 0;
                    var jit = p + new Vector3(Jit(rnd, 0.02f), 0, Jit(rnd, 0.02f));
                    if (fam == 1) Prefab("TrunkStack", jit, Rot90(rnd) + Jit(rnd, 6), 1f, true);
                    else if (fam == 2 || fam == 4) Prefab("HayStack", jit, Rot90(rnd) + Jit(rnd, 5), 1f, true);
                    else Prefab(v < 0.5 ? "CircusWagonA" : "CircusWagonB", jit, fenceRot + (rnd.NextDouble() < 0.5 ? 0f : 180f) + Jit(rnd, 2), 1f, true);
                }
                break;
            case 'x':
                if (v < 0.40) Prefab("StarTubs", p, rnd.Next(360), 1f, true);
                else if (v < 0.70) Prefab("PopcornCart", p, rnd.Next(360), 1f, true);
                else Prefab("CircusCannon", p, baseRot + 180f + Jit(rnd, 40), 1f, true);   // aimed at the sky over the middle
                break;
            case 'f': Prefab("RopeBarrier", p, fenceRot + Jit(rnd, 2), 1f, true); break;
            case 't':
                if (v < 0.25) Prefab("HighStriker", p, baseRot + 180f + Jit(rnd, 20), 1f, true);
                else if (v < 0.50) Prefab("TicketBooth", p, baseRot + 180f + Jit(rnd, 15), 1f, true);
                else if (v < 0.75) Prefab("CarnivalLamp", p, rnd.Next(360), 1f, true);
                else Prefab("BalloonPost", p, rnd.Next(360), 1f, true);
                break;
            case 'b': Grass("FairGrass", p, 1.3f, rnd); break;   // ~1.95 m, like the other bushes
        }
    }

    static void CircusDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "HayPile", "JugglingPins", "PopcornSpill", "HayPile");
        if (!prefabs.ContainsKey(k)) k = "HayPile";
        Prefab(k, o, rot + rnd.Next(360), k == "HayPile" ? 0.8f : 1f, false);
    }

    /// <summary>Beyond the big top's sidewall: the big top itself, a Ferris wheel and a carousel at each end (mirrored, so
    /// both teams see the same skyline), wagons, booths, lamps and balloons around. Also the fairground arch behind each
    /// team's spawn (the gap in the border there is closed by the invisible VitualWall).</summary>
    static void PlaceCircusBackdrop(int seed)
    {
        PlaceGates("CircusGate");
        var rnd = new Random(seed);
        for (int s = -1; s <= 1; s += 2)
        {
            Prefab("BigTop", new Vector3(s * 16f, 0f, s * 46f), 0f, 1f, false);
            Prefab("FerrisWheel", new Vector3(-s * 22f, 0f, s * 38f), s > 0 ? 20f : 200f, 1f, false);
            Prefab("Carousel", new Vector3(s * 32f, 0f, -s * 8f), rnd.Next(360), 1f, false);
        }
        for (int i = 0; i < 22; i++)
        {
            float a = i / 22f * Mathf.PI * 2f + Jit(rnd, 0.08f);
            float rad = 30f + (float)rnd.NextDouble() * 8f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.85f, 0f, Mathf.Sin(a) * rad);
            var k = Pick(rnd, "CircusWagonA", "CircusWagonB", "TicketBooth", "CarnivalLamp", "BalloonPost", "HayStack", "TrunkStack");
            Prefab(k, p, rnd.Next(360), 1.4f, false);
        }
    }

    /// <summary>Loose hay, dropped juggling pins, spilt popcorn on the open field (no colliders), clear of walls and spawns.</summary>
    static void ScatterCircusFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        for (int i = 0; i < 160 && placed < 12; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            var k = Pick(rnd, "HayPile", "JugglingPins", "PopcornSpill", "HayPile");
            if (!prefabs.ContainsKey(k)) k = "HayPile";
            Prefab(k, p, rnd.Next(360), k == "HayPile" ? 0.8f : 1f, false);
            placed++;
        }
        log += " circusScatter=" + placed;
    }
}
