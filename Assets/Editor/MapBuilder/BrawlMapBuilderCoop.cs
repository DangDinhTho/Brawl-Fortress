using UnityEditor;

/// <summary>Test maps for the modes ported from TF2_AI (Tho prefers brown desert maps; zombies on dark maps): JungleBoss (team vs the Hale boss) and Zombie (survive the horde).
/// Menu: Tools/Maps/Build Boss & Zombie Maps. Same 21x13 half-layout rules as the other Brawl maps.</summary>
public static partial class BrawlMapBuilder
{
    [MenuItem("Tools/Maps/Build Boss & Zombie Maps")]
    public static void BuildCoopPortMenu() { foreach (var d in BossZombie) UnityEngine.Debug.Log(Build(d)); }

    public static readonly Def[] BossZombie =
    {
        new Def {
            mapEnum = "Map_Brawl_HaleJungle", displayName = "Hale's Canyon", icon = "map_halejungle", mode = GameMode.JungleBoss, theme = Theme.Western, seed = 5101,
            names = new[] { "Hale's Canyon", "Каньон Хейла", "หุบเขาของเฮล", "Cañón de Hale", "Hẻm Núi Của Hale" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bb.............bb..",
                "..bb...##...##...bb..",
                ".....................",
                ".x.......bbb.......x.",
                "....##.........##....",
                "..t...............t..",
                ".......#.....#.......",
                "..bb.............bb..",
                ".........bbb.........",
                "....##.........##....",
            },
            center = "..........t..........",
        },
        new Def {
            mapEnum = "Map_Brawl_ZombieYard", displayName = "Zombie Crypt", icon = "map_zombieyard", mode = GameMode.Zombie, theme = Theme.Cave, seed = 5202,
            names = new[] { "Zombie Crypt", "Склеп зомби", "สุสานซอมบี้", "Cripta Zombi", "Hầm Mộ Zombie" },
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
    };
}
