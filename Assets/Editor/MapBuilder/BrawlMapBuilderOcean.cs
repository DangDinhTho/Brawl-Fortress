using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Ocean theme (the sea floor and a sunken city). Props from Tools/IceArt/props19.py (same folder and palette as the
/// other themes, set up by IceThemeSetup + OceanThemeSetup): coral reef heads, ruined walls of pale jade stone with
/// glowing runes, wreck hull sections and barnacled rocks (one family per wall), giant clams, a treasure chest, an anchor,
/// a brass diving helmet, a cannon, coral ridges, columns with glowing orbs, sea fans, a sea-king statue, glowing
/// crystals, a broken mast, swaying kelp (KelpGrass + KelpSway) as the bushes. A moving web of light lies on the sand
/// (Caustics). Mechanic: ocean current lanes (OceanCurrent.cs): faster downstream, slower upstream, mirrored for both
/// teams and flowing towards the middle. Border: low reef banks (ReefWallA/B/C), the AtlantisGate behind each spawn;
/// a domed temple, a sunken ship and a giant scallop out on the sea floor.
/// </summary>
public static partial class BrawlMapBuilder
{
    public static readonly Def[] OceanMaps =
    {
        new Def {   // a new layout: two clear lanes per half for the currents
            mapEnum = "Map_Brawl_CoralKingdom", displayName = "Coral Kingdom", icon = "map_coralkingdom", mode = GameMode.Competitive, theme = Theme.Ocean, seed = 2301,
            names = new[] { "Coral Kingdom", "Коралловое королевство", "อาณาจักรปะการัง", "Reino de Coral", "Vương Quốc San Hô" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "..##....bbbbb....##..",
                "..#...............#..",
                "......x.......x......",
                ".bbb.....###.....bbb.",
                ".bbb.............bbb.",
                "......t.......t......",
                "..##.............##..",
                "....bb...x.x...bb....",
                "....bb.........bb....",
                "..t...............t..",
            },
            center = ".....##.......##.....",
        },
        new Def {   // a new layout: the currents run past the flag rooms
            mapEnum = "Map_Brawl_PearlHeist", displayName = "Pearl Heist", icon = "map_pearlheist", mode = GameMode.CaptureTheFlag, theme = Theme.Ocean, seed = 2302,
            names = new[] { "Pearl Heist", "Похищение жемчуга", "ปล้นไข่มุก", "Robo de Perlas", "Phi Vụ Ngọc Trai" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".##.....x...x.....##.",
                ".#.................#.",
                "...bb...........bb...",
                "...bb...#####...bb...",
                "........#...#........",
                ".t.................t.",
                ".....................",
                "...##...bb.bb...##...",
                "....x...bb.bb...x....",
                ".........t.t.........",
            },
            center = "..bb.............bb..",
        },
        new Def {   // a new layout: one current on each side carries a team up to the hill
            mapEnum = "Map_Brawl_TridentCrown", displayName = "Trident Crown", icon = "map_tridentcrown", mode = GameMode.KingOfTheHill, theme = Theme.Ocean, seed = 2303,
            names = new[] { "Trident Crown", "Корона трезубца", "มงกุฎตรีศูล", "Corona del Tridente", "Vương Miện Đinh Ba" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".##.......x.......##.",
                ".#.................#.",
                "......bb.......bb....",
                "..t...bb..#....bb.t..",
                "..........#..........",
                ".bb...##........##...",
                ".bb...#..........#...",
                "........x.....x......",
                "..bb..........bb.....",
                "..bb..t.......t.bb...",
            },
            center = "..x...............x..",
        },
        new Def {   // a new layout: the track swings through the middle, a current on each flank
            mapEnum = "Map_Brawl_TidalExpress", displayName = "Tidal Express", icon = "map_tidalexpress", mode = GameMode.Payload, theme = Theme.Ocean, seed = 2304,
            pathHalf = new[] { new Vector2(0, 16.5f), new Vector2(0, 10), new Vector2(3, 4) },
            names = new[] { "Tidal Express", "Приливный экспресс", "รถด่วนกระแสน้ำ", "Expreso de la Marea", "Tàu Tốc Hành Thủy Triều" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".##..##.........##...",
                ".#...#.......x...#...",
                ".......x.............",
                ".bb...t..........bb..",
                ".bb..............bb..",
                "......##.....##..t...",
                "..............#......",
                "..x..bb....x.....bb..",
                "..x..bb..........bb..",
                ".....t.....##........",
            },
            center = ".bb...............bb.",
        },
        new Def {   // a new layout (path E): currents along both outer flanks
            mapEnum = "Map_Brawl_AbyssWorks", displayName = "Abyss Works", icon = "map_abyssworks", mode = GameMode.MannVsMachine, theme = Theme.Ocean, seed = 2305,
            pathHalf = new[] { new Vector2(-4, 16.5f), new Vector2(-4, 6), new Vector2(4, 2) },
            names = new[] { "Abyss Works", "Мастерская бездны", "โรงงานใต้สมุทร", "Taller del Abismo", "Xưởng Vực Thẳm" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".##..##.x.........##.",
                ".#....#............#.",
                "..........bb.........",
                "......t...bb......bb.",
                ".bb...............bb.",
                ".bb.....##.....x.....",
                "........#.........t..",
                "......x.......##.....",
                ".bb...........#......",
                "............bb.t.....",
            },
            center = "......x.......x......",
        },
        new Def {   // a new layout
            mapEnum = "Map_Brawl_AtlantisArena", displayName = "Atlantis Arena", icon = "map_atlantisarena", mode = GameMode.JungleBoss, theme = Theme.Ocean, seed = 2306,
            names = new[] { "Atlantis Arena", "Арена Атлантиды", "สังเวียนแอตแลนติส", "Arena de la Atlántida", "Đấu Trường Atlantis" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                "....x.........x......",
                "..##......bbb....##..",
                "..#...............#..",
                "......t.......t......",
                ".bb.......x.......bb.",
                ".bb..##......##...bb.",
                ".....................",
                "...x.......t.......x.",
                "....bbb....bb........",
                "....bbb....bb....t...",
            },
            center = "..##..#.......#..##..",
        },
        new Def {   // a new layout: a current down the middle
            mapEnum = "Map_Brawl_GhostReef", displayName = "Ghost Reef", icon = "map_ghostreef", mode = GameMode.Zombie, theme = Theme.Ocean, seed = 2307,
            names = new[] { "Ghost Reef", "Риф призраков", "แนวปะการังผีสิง", "Arrecife Fantasma", "Rạn San Hô Ma" },
            half = new[] {
                ".....................",
                ".....................",
                ".....................",
                ".##..x.........x..##.",
                ".#.................#.",
                "......bbb.....bbb....",
                "..t...bbb.....bbb.t..",
                ".....................",
                ".....##..#...##......",
                ".bb..........#....bb.",
                ".bb....x.........x...",
                ".....##.......##.....",
                ".....................",
            },
            center = "..x...#.......#...x..",
        },
    };

    static readonly List<(Vector3 c, Vector3 size)> currentLanes = new List<(Vector3, Vector3)>();

    [MenuItem("Tools/Maps/Build Ocean Maps")]
    static void BuildOceanMapsMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[Ocean] " + IceThemeSetup.Setup());
        if (!IceThemeSetup.Ready) { Debug.LogError("[Ocean] assets missing in " + IceThemeSetup.Root); return; }
        foreach (var d in OceanMaps) Debug.Log(Build(d));
    }

    static void OceanTile(char ch, Vector3 p, float baseRot, float fenceRot, Random rnd)
    {
        double v = MixRnd(rnd).NextDouble();
        switch (ch)
        {
            case '#':
                {
                    // one family per wall (PrepareVolcanoWalls): reef heads (0 and 4), ruined city walls (1), wreck hulls (2), barnacled rocks (3)
                    int fam = wallFamily.TryGetValue(TileOf(p), out var f) ? f : 0;
                    var jit = p + new Vector3(Jit(rnd, 0.02f), 0, Jit(rnd, 0.02f));
                    if (fam == 1) Prefab("AtlantisWall", jit, fenceRot + rnd.Next(2) * 180f + Jit(rnd, 3), 1f, true);
                    else if (fam == 2) Prefab("ShipHull", jit, fenceRot + rnd.Next(2) * 180f + Jit(rnd, 6), 1f, true);
                    else if (fam == 3) Prefab("BarnacleRock", jit, rnd.Next(360), 0.96f + (float)rnd.NextDouble() * 0.08f, true);
                    else Prefab(v < 0.5 ? "CoralReefA" : "CoralReefB", jit, rnd.Next(360), 0.96f + (float)rnd.NextDouble() * 0.08f, true);
                }
                break;
            case 'x':
                if (v < 0.25) Prefab("GiantClam", p, rnd.Next(360), 1f, true);
                else if (v < 0.45) Prefab("TreasureChest", p, rnd.Next(360), 0.95f, true);
                else if (v < 0.62) Prefab("AnchorRock", p, rnd.Next(360), 0.9f, true);
                else if (v < 0.80) Prefab("DivingHelmet", p, rnd.Next(360), 1f, true);
                else Prefab("CannonWreck", p, rnd.Next(360), 0.95f, true);
                break;
            case 'f': Prefab("CoralRidge", p, fenceRot + Jit(rnd, 5), 1f, true); break;
            case 't':
                if (v < 0.22) Prefab("AtlantisColumn", p, Rot90(rnd) + Jit(rnd, 4), 1f, true);
                else if (v < 0.44) Prefab("SeaFan", p, rnd.Next(360), 1f, true);
                else if (v < 0.60) Prefab("TridentStatue", p, baseRot + 180f + Jit(rnd, 25), 1f, true);
                else if (v < 0.82) Prefab("GlowCrystal", p, rnd.Next(360), 1f, true);
                else Prefab("BrokenMast", p, rnd.Next(360), 1f, true);
                break;
            case 'b': Grass("KelpGrass", p, 1.3f, rnd); break;   // ~1.95 m, like the other bushes
        }
    }

    static void OceanDecor(Vector3 o, float rot, Random rnd)
    {
        rnd = MixRnd(rnd);
        var k = Pick(rnd, "Starfish", "SeaShells", "SeaUrchins", "CoralBits", "SeaShells", "Starfish");
        if (!prefabs.ContainsKey(k)) k = "SeaShells";
        Prefab(k, o, rot + rnd.Next(360), 1f, false);
    }

    static readonly Color OceanFog = new Color(0.14f, 0.38f, 0.52f);

    /// <summary>Under the sea: a cool blue-green ambient (bright enough to read the corals), light from above, a deep
    /// blue-green haze, the gradient sky.</summary>
    static void OceanLook(Light light)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.66f, 0.84f, 0.92f); RenderSettings.ambientEquatorColor = new Color(0.52f, 0.68f, 0.78f); RenderSettings.ambientGroundColor = new Color(0.42f, 0.52f, 0.58f);
        if (light) light.color = new Color(0.84f, 0.94f, 1.0f);
        OceanSky();
    }

    static void OceanSky()
    {
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(OceanThemeSetup.SkyMat);
        RenderSettings.fogColor = OceanFog;
        foreach (var cam in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)))
        { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = OceanFog; }
    }

    /// <summary>After the mode objects are in: the underwater props material everywhere.</summary>
    static void OceanWater()
    {
        var from = AssetDatabase.LoadAssetAtPath<Material>(IceThemeSetup.PropsMat);
        var to = AssetDatabase.LoadAssetAtPath<Material>(OceanThemeSetup.PropsMat);
        int n = 0;
        if (from && to)
            foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
            {
                var mats = r.sharedMaterials; bool ch = false;
                for (int i = 0; i < mats.Length; i++) if (mats[i] == from) { mats[i] = to; ch = true; }
                if (ch) { r.sharedMaterials = mats; n++; }
            }
        log += " underwater=" + n;
    }

    static void NotStatic(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) { GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0); t.gameObject.layer = 0; }
    }

    /// <summary>Beyond the reef banks: the sea floor goes on (the floor is 100 m wide, the haze hides its edge), a domed
    /// temple of the sunken city off each end, a sunken ship and a giant scallop on the sides, reefs, sea fans and
    /// crystals around. Also the gate behind each spawn, the caustics over the arena and the kelp sway.</summary>
    static void PlaceOceanBackdrop(int seed)
    {
        PlaceGates("AtlantisGate");
        OceanSky();
        var ca = Prefab("Caustics", new Vector3(0f, 0.03f, 0f), 0f, 1f, false);
        if (ca) { ca.transform.localScale = new Vector3(46f, 1f, 54f); NotStatic(ca); }
        // kelp sways: not static, one driver for the scene
        int kelp = 0;
        foreach (Transform t in env.Cast<Transform>().ToList())
            if (t.name.StartsWith("KelpGrass")) { NotStatic(t.gameObject); kelp++; }
        var sway = new GameObject("KelpSway"); sway.transform.SetParent(env, false); sway.AddComponent<KelpSway>();
        log += " kelp=" + kelp;
        var rnd = new Random(seed);
        for (int s = -1; s <= 1; s += 2)   // mirrored, so both teams look at the same things
        {
            Prefab("AtlantisTemple", new Vector3(s * 6f, -0.3f, s * 46f), rnd.Next(360), 1f, false);
            Prefab("SunkenShip", new Vector3(s * 30f, 0f, -s * 8f), s > 0 ? 100f : -80f, 1f, false);
            Prefab("GiantScallop", new Vector3(-s * 29f, 0f, s * 14f), s > 0 ? 250f : 70f, 1.1f, false);
        }
        for (int i = 0; i < 30; i++)
        {
            float a = i / 30f * Mathf.PI * 2f + Jit(rnd, 0.08f);
            float rad = 30f + (float)rnd.NextDouble() * 12f;
            var p = new Vector3(Mathf.Cos(a) * rad * 0.9f, 0f, Mathf.Sin(a) * rad);
            if (Mathf.Abs(p.x) < 22f && Mathf.Abs(p.z) < 28f) continue;
            if (Mathf.Abs(Mathf.Abs(p.x) - 29.5f) < 6f && Mathf.Abs(Mathf.Abs(p.z) - 11f) < 8f) continue;   // the ship and the shell
            var k = Pick(rnd, "CoralReefA", "CoralReefB", "SeaFan", "GlowCrystal", "AtlantisColumn", "BarnacleRock", "CoralReefA", "SeaFan");
            float sc = k.StartsWith("CoralReef") || k == "BarnacleRock" ? 1.8f + (float)rnd.NextDouble() * 0.9f : 1.4f + (float)rnd.NextDouble() * 0.6f;
            Prefab(k, p, rnd.Next(360), sc, false);
        }
    }

    /// <summary>Ocean current lanes: on each half the two best straight runs of open tiles along the length of the map
    /// (two tiles wide, at least 5 tiles long, rows 3-12: not out of the spawn), 5 m apart, clear of a payload / robot
    /// path; mirrored into the other half. Every lane flows towards the middle: a fast way forward for its own team,
    /// slow to retreat along. Floor decor in a lane goes (the flow must read clearly).</summary>
    static void PlaceCurrents(Def d)
    {
        currentLanes.Clear();
        if (!prefabs.ContainsKey("OceanCurrent")) { log += " [missing OceanCurrent]"; return; }
        var g = FullGrid(d);
        bool open(char ch) => ch == '.' || ch == 'b';
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
        var chosen = new List<(int c, int r0, int r1)>();
        foreach (var cd in cands.OrderByDescending(q => q.score))
        {
            if (chosen.Count >= 2) break;
            float x = (TilePos(cd.r0, cd.c).x + TilePos(cd.r0, cd.c + 1).x) * 0.5f;
            if (chosen.Any(q => Mathf.Abs((TilePos(q.r0, q.c).x + TilePos(q.r0, q.c + 1).x) * 0.5f - x) < 5f)) continue;
            var a = TilePos(cd.r0, cd.c); var b = TilePos(cd.r1, cd.c);
            Vector3 center = new Vector3(x, 0f, (a.z + b.z) * 0.5f);
            Vector3 size = new Vector3(2.6f, 1f, Mathf.Abs(b.z - a.z) + 1.3f);
            if (pathSamples != null && pathSamples.Any(sp => InLane(sp, center, size, 1.5f) || InLane(-sp, center, size, 1.5f))) continue;
            chosen.Add((cd.c, cd.r0, cd.r1));
        }
        foreach (var cd in chosen)
        {
            float x = (TilePos(cd.r0, cd.c).x + TilePos(cd.r0, cd.c + 1).x) * 0.5f;
            var a = TilePos(cd.r0, cd.c); var b = TilePos(cd.r1, cd.c);
            Vector3 center = new Vector3(x, 0f, (a.z + b.z) * 0.5f);
            Vector3 size = new Vector3(2.6f, 1f, Mathf.Abs(b.z - a.z) + 1.3f);
            foreach (var (p, yaw) in new[] { (center, center.z < 0 ? 0f : 180f), (-center, center.z < 0 ? 180f : 0f) })
            {
                var go = Prefab("OceanCurrent", p, yaw, 1f, false);
                if (!go) continue;
                go.transform.localScale = size;
                NotStatic(go);
                currentLanes.Add((p, size));
                foreach (Transform t in env.Cast<Transform>().ToList())
                {
                    if (t == go.transform) continue;
                    var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                    string sn = src ? src.name : t.name;
                    if ((sn == "Starfish" || sn == "SeaShells" || sn == "SeaUrchins" || sn == "CoralBits") && InLane(t.position, p, size, 0.3f)) Object.DestroyImmediate(t.gameObject);
                }
            }
        }
        log += " currents=" + currentLanes.Count;
    }

    static bool InLane(Vector3 q, Vector3 c, Vector3 size, float margin) =>
        Mathf.Abs(q.x - c.x) < size.x * 0.5f + margin && Mathf.Abs(q.z - c.z) < size.z * 0.5f + margin;

    /// <summary>Shells, starfish, urchins and bits of coral on the open sand (no colliders), clear of walls, spawns and lanes.</summary>
    static void ScatterOceanFloor(int seed)
    {
        var rnd = new Random(seed + 17);
        int placed = 0;
        for (int i = 0; i < 160 && placed < 16; i++)
        {
            var p = new Vector3(Jit(rnd, 14f), 0f, Jit(rnd, 19f));
            if (Physics.CheckSphere(p + Vector3.up * 1.3f, 1.0f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (spawnPoints != null && spawnPoints.Any(sp => (sp - p).sqrMagnitude < 9f)) continue;
            if (currentLanes.Any(l => InLane(p, l.c, l.size, 0.5f))) continue;
            var k = Pick(rnd, "Starfish", "SeaShells", "SeaUrchins", "CoralBits");
            if (!prefabs.ContainsKey(k)) k = "SeaShells";
            Prefab(k, p, rnd.Next(360), 1f, false);
            placed++;
        }
        log += " oceanScatter=" + placed;
    }
}
