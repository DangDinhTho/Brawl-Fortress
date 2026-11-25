using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Factory theme: a 1960s-70s engineering works and building site. Props from Tools/IceArt/props13.py (same folder and
/// palette as the other themes, set up by IceThemeSetup): red brick walls, stacked crates, red-oxide steel girders,
/// concrete pipes (one family per wall, like the volcano walls), oil drums, cable drums, a drum cement mixer, brick
/// pallets, corrugated-iron hoarding, steel columns, timber scaffolds, cast-iron yard lamps, a riveted boiler, weeds on
/// the lot (YardGrass), a concrete yard floor. Border: brick workshops and an iron shed (FactoryWallA/B/C), the works
/// gate behind each spawn; tower cranes, chimneys (no smoke), water towers and sheds beyond. Daylight like the desert.
/// </summary>
public static partial class BrawlMapBuilder
{
    public static readonly Def[] FactoryMaps =
    {
        new Def {   // a new layout
            mapEnum = "Map_Brawl_SteelYard", displayName = "Steel Yard", icon = "map_steelyard", mode = GameMode.Competitive, theme = Theme.Factory, seed = 1601,
            names = new[] { "Steel Yard", "Стальной двор", "ลานเหล็กกล้า", "Patio de Acero", "Bãi Thép" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..bbb...........bbb..",
                "..bbb...#####...bbb..",
                ".........x.x.........",
                ".##.ff.........ff.##.",
                "....t...........t....",
                "......bbb...bbb......",
                "..x...bbb...bbb...x..",
                ".###.....###.....###.",
                ".........bbb.........",
                "...f.............f...",
            },
            center = "..bb...##...##...bb..",
        },
    };

    [MenuItem("Tools/Maps/Build Factory Maps")]
    static void BuildFactoryMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Factory] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Factory] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in FactoryMaps) Debug.Log(Build(d));
    }

    static void FactoryTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':
                {
                    // one family per wall (PrepareVolcanoWalls): brick (0 and 3), crates (1), girders (2), pipes (4)
                    int fam = wallFamily.TryGetValue(TileOf(p), out var f) ? f : 0;
                    var jit = p + new Vector3(Jit(rnd, 0.02f), 0, Jit(rnd, 0.02f));
                    switch (fam)
                    {
                        case 1: Prefab("CrateStack", jit, Rot90(rnd) + Jit(rnd, 6), 1f, true); break;
                        case 2: Prefab("GirderStack", jit, fenceRot + 90f + Jit(rnd, 3), 1f, true); break;
                        case 4: Prefab("PipeStack", jit, fenceRot + 90f + Jit(rnd, 3), 1f, true); break;
                        default:
                            // the brick wall section is 0.6 m thick: stretched to fill the tile, its long face along the wall
                            Prefab(v < 0.75 ? "BrickWallA" : "BrickWallB", p, fenceRot + (rnd.NextDouble() < 0.5 ? 0f : 180f), 1f, true, null, new Vector3(1f, 1f, 2.1f));
                            break;
                    }
                }
                break;
            case 'x':
                if (v < 0.30) Prefab("OilDrums", p, rnd.Next(360), 1f, true);
                else if (v < 0.55) Prefab("CableReel", p, Rot90(rnd) + Jit(rnd, 10), 1f, true);
                else if (v < 0.75) Prefab("CementMixer", p, rnd.Next(360), 0.9f, true);
                else Prefab("BrickPallet", p, Rot90(rnd) + Jit(rnd, 5), 1f, true);
                break;
            case 'f': Prefab("IronFence", p, fenceRot + Jit(rnd, 2), 1f, true); break;
            case 't':
                if (v < 0.25) Prefab("GirderColumn", p, Rot90(rnd), 1f, true);
                else if (v < 0.50) Prefab("Scaffold", p, Rot90(rnd) + Jit(rnd, 4), 0.95f, true);
                else if (v < 0.75) Prefab("FactoryLamp", p, baseRot + 180f + Jit(rnd, 30), 1f, true);
                else Prefab("Boiler", p, rnd.Next(360), 1f, true);
                break;
            case 'b': Grass("YardGrass", p, 1.3f, rnd); break;   // ~1.95 m, like the other bushes
        }
    }

    static void FactoryDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "BrickRubble", "ScrapPile", "Cogwheel", "PlankBundle", "Wheelbarrow", "BrickRubble");
        if (!prefabs.ContainsKey(k)) k = "BrickRubble";
        Prefab(k, o, rot + rnd.Next(360), k == "Cogwheel" ? 0.8f : 0.9f, false);
    }

    /// <summary>Beyond the border: tower cranes, chimneys (no smoke), water towers and workshop sheds. Also the works gate
    /// behind each team's spawn (the gap in the border there is closed by the invisible VitualWall).</summary>
    static void PlaceFactoryBackdrop(int seed)
    {
        PlaceGates("FactoryGate");
        var rnd = new Random(seed);
        for (int s = -1; s <= 1; s += 2)   // mirrored landmarks, so both teams see the same skyline
        {
            Prefab("TowerCrane", new Vector3(s * 22f, 0f, s * 34f), s > 0 ? 200f : 20f, 1f, false);
            Prefab("Chimney", new Vector3(-s * 30f, 0f, s * 40f), 0f, 1f, false);
            Prefab("WaterTower", new Vector3(s * 34f, 0f, -s * 12f), rnd.Next(360), 1f, false);
        }
        for (int i = 0; i < 18; i++)
        {
            float a = i / 18f * Mathf.PI * 2f + Jit(rnd, 0.08f);
            float rad = 34f + (float)rnd.NextDouble() * 8f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.85f, 0f, Mathf.Sin(a) * rad);
            var k = Pick(rnd, "FactoryShed", "FactoryShed", "PipeStack", "GirderStack", "CrateStack", "Scaffold");
            float sc = k == "FactoryShed" ? 1f : 1.5f;
            Prefab(k, p, k == "FactoryShed" ? Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg : rnd.Next(360), sc, false);
        }
    }

    /// <summary>Broken bricks, scrap, a gear wheel, planks on the open yard (no colliders), clear of walls and spawns.</summary>
    static void ScatterFactoryFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        for (int i = 0; i < 160 && placed < 14; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            var k = Pick(rnd, "BrickRubble", "ScrapPile", "Cogwheel", "PlankBundle", "BrickRubble");
            if (!prefabs.ContainsKey(k)) k = "BrickRubble";
            Prefab(k, p, rnd.Next(360), k == "Cogwheel" ? 0.8f : 0.9f, false);
            placed++;
        }
        log += " factoryScatter=" + placed;
    }
}
