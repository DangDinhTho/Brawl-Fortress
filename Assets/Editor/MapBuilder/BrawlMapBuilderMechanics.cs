using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = System.Random;

// Themed map mechanics of the newer themes (Nov 2026; runtime scripts in Assets/Scripts/Map): Xmas gifts (+ the night
// lamps lighting up bushes), Circus human cannons, Lunar meteor showers, Ice slick patches, Candy jelly walls, Egypt
// dart traps. Build() places them (PlaceMechanics, after the theme's lighting / material swaps); for maps already built,
// ApplyMechanicsBatch opens each scene, replaces its mechanics and saves it (no rebuild, no NavMesh change: every
// mechanic object is walk-through and ignored by the NavMesh bake, name prefix "Mech_").
public static partial class BrawlMapBuilder
{
    const string MechPrefix = "Mech_";
    const string MatDir = "Assets/Resources/Materials";

    static readonly Theme[] MechThemes = { Theme.Xmas, Theme.Circus, Theme.Lunar, Theme.Ice, Theme.Candy, Theme.Egypt };

    // ------------------------------------------------------------------ materials
    [MenuItem("Tools/Maps/Mechanics/Setup Materials")]
    public static string MechanicsSetup()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return "wrong project";
        string log = "";
        Material Mat(string name, string shader, Action<Material> init)
        {
            string p = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            var sh = Shader.Find(shader);
            if (sh == null) { log += " [no shader " + shader + "]"; return null; }
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); log += " +" + name; }
            else m.shader = sh;
            init(m);
            EditorUtility.SetDirty(m);
            return m;
        }
        Mat("MechDecal", "BrawlFortress/MechDecal", m => { });
        Mat("SlickIce", "BrawlFortress/SlickIce", m => { });
        Mat("MeteorWarn", "BrawlFortress/VentWarn", m => { m.SetColor("_Color", new Color(1f, 0.32f, 0.1f, 1f)); m.SetColor("_Edge", new Color(1f, 0.9f, 0.45f, 1f)); });
        Mat("MeteorTrail", "Legacy Shaders/Particles/Additive", m =>
        {
            var tex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (tex) m.mainTexture = tex;
            m.SetColor("_TintColor", new Color(1f, 0.6f, 0.25f, 0.6f));
        });
        AssetDatabase.SaveAssets();
        return "mechanics materials:" + (log.Length > 0 ? log : " up to date");
    }

    // ------------------------------------------------------------------ apply to built maps
    static IEnumerable<Def> MechDefs() => AllDefs().Where(d => MechThemes.Contains(d.theme) && !Retired.Contains(d.mapEnum));

    [MenuItem("Tools/Maps/Mechanics/Apply To All Maps")]
    public static void ApplyMechanicsMenu() => Debug.Log("[Mechanics] " + ApplyMechanicsBatch(0, 999));

    /// <summary>Places the mechanics in the built map scenes of the six themes, defs start .. start + count - 1 (order of
    /// MechDefs). About 1-2 s per map.</summary>
    public static string ApplyMechanicsBatch(int start, int count, string theme = null)
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return "wrong project";
        GuardUnsavedScenes();
        MechanicsSetup();
        var lines = new List<string>();
        foreach (var d in MechDefs().Where(x => theme == null || x.theme.ToString() == theme).Skip(start).Take(count))
            lines.Add(ApplyMechanics(d));
        return lines.Count + " maps\n" + string.Join("\n", lines);
    }

    public static string ApplyMechanics(Def d)
    {
        string path = "Assets/Scenes/" + d.mapEnum + ".unity";
        if (!File.Exists(path)) return d.mapEnum + ": no scene";
        log = "";
        LoadAssets();
        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SceneManager.SetActiveScene(scene);
        env = Root("Environment").transform;
        spawnPoints = Root("TeamPos").GetComponentsInChildren<Transform>().Where(t => t.childCount == 0).Select(t => t.position).ToList();
        PrePlace(d);   // the payload / robot path (the spawns were moved off it when the map was built)
        Physics.SyncTransforms();
        PlaceMechanics(d);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return d.mapEnum + ":" + log;
    }

    // ------------------------------------------------------------------ placement
    static void PlaceMechanics(Def d)
    {
        if (!MechThemes.Contains(d.theme)) return;
        RemoveMechanics();
        Physics.SyncTransforms();
        switch (d.theme)
        {
            case Theme.Xmas: PlaceGifts(d); if (d.night) PlaceLightPools(); break;
            case Theme.Circus: PlaceCannons(d); break;
            case Theme.Lunar: PlaceMeteors(d); break;
            case Theme.Ice: PlaceSlickIce(d); break;
            case Theme.Candy: PlaceJellyWalls(); break;
            case Theme.Egypt: PlaceDartTraps(d); break;
        }
    }

    static void RemoveMechanics()
    {
        foreach (Transform t in env.Cast<Transform>().ToList())
            if (t.name.StartsWith(MechPrefix)) Object.DestroyImmediate(t.gameObject);
        foreach (var c in env.GetComponentsInChildren<LightPool>(true)) Object.DestroyImmediate(c);
        foreach (Transform t in env)   // a first try saved LightPool from a file of another name: missing scripts
            if (t.name.StartsWith("WarmPool")) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        foreach (var c in env.GetComponentsInChildren<JellyWall>(true)) Object.DestroyImmediate(c);
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
    static bool FarFromSpawns(Vector3 p, float r) => spawnPoints.All(s => Flat(s - p) >= r && Flat(s + p) >= r);
    static bool ClearAt(Vector3 p, float r) => !Physics.CheckBox(p + Vector3.up * 1.2f, new Vector3(r, 0.8f, r), Quaternion.identity, 1 << WallLayer, QueryTriggerInteraction.Ignore);
    static bool OffPath(Vector3 p, float r) => pathSamples == null || (DistToPath(p) >= r && DistToPath(-p) >= r);

    static GameObject MechObject(string name, Vector3 pos, Quaternion rot)
    {
        var go = new GameObject(MechPrefix + name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.SetParent(env, true);
        go.transform.SetPositionAndRotation(pos, rot);
        return go;
    }

    /// <summary>A model under a mechanic: no colliders, not static (it moves, or is cloned at runtime), default layer.</summary>
    static GameObject MechModel(string prefab, Transform parent, Vector3 localPos, Quaternion localRot, float scale)
    {
        var go = Prefab(prefab, Vector3.zero, 0f, scale, false);
        if (!go) return null;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos; go.transform.localRotation = localRot; go.transform.localScale = Vector3.one * scale;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = 0; GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0); }
        return go;
    }

    /// <summary>Removes walk-through floor decor (no collider) named in 'names' within r of p (and of -p when mirror).</summary>
    static int ClearDecor(Vector3 p, float r, string[] names)
    {
        int n = 0;
        foreach (Transform t in env.Cast<Transform>().ToList())
        {
            if (t.name.StartsWith(MechPrefix)) continue;
            var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            string sn = src ? src.name : t.name;
            if (Array.IndexOf(names, sn) < 0) continue;
            if (t.GetComponentsInChildren<Collider>(true).Any(c => c.enabled)) continue;
            if (Flat(t.position - p) > r) continue;
            Object.DestroyImmediate(t.gameObject); n++;
        }
        return n;
    }

    // ------------------------------------------------------------------ Xmas: gifts, night lamps
    static void PlaceGifts(Def d)
    {
        var g = FullGrid(d);
        var rnd = new Random(d.seed * 13 + 5);
        var cands = new List<(Vector3 p, double s)>();
        for (int row = 4; row <= 12; row++)
            for (int c = 1; c < COLS - 1; c++)
            {
                if (g[row, c] != '.') continue;
                int solid = 0;
                for (int dr = -1; dr <= 1; dr++) for (int dc = -1; dc <= 1; dc++) { char ch = g[row + dr, c + dc]; if (ch != '.' && ch != 'b') solid++; }
                if (solid > 3) continue;
                var p = TilePos(row, c);
                if (Mathf.Abs(p.x) > 13f || Flat(p) < 5f || !FarFromSpawns(p, 6f) || !OffPath(p, 2.5f) || !ClearAt(p, 0.6f) || !ClearAt(-p, 0.6f)) continue;
                cands.Add((p, -Mathf.Abs(p.z + 6f) * 0.5 - Mathf.Abs(Mathf.Abs(p.x) - 7f) * 0.35 + rnd.NextDouble() * 0.5));
            }
        var spots = new List<Vector3>();
        bool middle = g[13, 10] == '.' && ClearAt(Vector3.zero, 0.6f) && OffPath(Vector3.zero, 2.5f);
        if (middle) spots.Add(Vector3.zero);
        int pairs = middle ? 1 : 2;
        foreach (var cnd in cands.OrderByDescending(x => x.s))
        {
            if (spots.Count >= (middle ? 1 : 0) + pairs * 2) break;
            if (spots.Any(q => Flat(q - cnd.p) < 6f || Flat(q + cnd.p) < 6f)) continue;
            spots.Add(cnd.p); spots.Add(-cnd.p);
        }
        var pf = prefabs.TryGetValue("GiftBox", out var gp) ? AssetDatabase.LoadAssetAtPath<GameObject>(gp) : null;
        Vector3 centre = Vector3.zero;
        if (pf) { var b = PrefabBounds(pf); centre = new Vector3(b.center.x, 0f, b.center.z); }
        for (int i = 0; i < spots.Count; i++)
        {
            var go = MechObject("Gift" + i, spots[i], Quaternion.identity);
            var gift = go.AddComponent<GiftDrop>();
            gift.id = i;
            var spin = new GameObject("GiftSpin").transform;
            spin.SetParent(go.transform, false);
            spin.localPosition = new Vector3(0f, 0.25f, 0f);
            const float s = 0.8f;   // bigger than the decorative parcels (and it floats, spins and sparkles)
            MechModel("GiftBox", spin, -centre * s, Quaternion.identity, s);
            gift.box = spin;
        }
        log += " gifts=" + spots.Count;
    }

    static void PlaceLightPools()
    {
        int n = 0;
        foreach (Transform t in env.Cast<Transform>().ToList())
        {
            if (!t.name.StartsWith("WarmPool") || Mathf.Abs(t.position.z) > 23f) continue;   // not the gates' pools
            var lp = t.gameObject.AddComponent<LightPool>();
            lp.radius = 0.36f * Mathf.Max(t.lossyScale.x, t.lossyScale.z);
            n++;
        }
        log += " lightPools=" + n;
    }

    static Bounds PrefabBounds(GameObject pf)
    {
        var b = new Bounds(); bool first = true;
        foreach (var mf in pf.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mf.sharedMesh) continue;
            var m = mf.transform.localToWorldMatrix; var mb = mf.sharedMesh.bounds;
            foreach (var sx in new[] { -1, 1 }) foreach (var sy in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
            {
                var q = m.MultiplyPoint(mb.center + Vector3.Scale(mb.extents, new Vector3(sx, sy, sz)));
                if (first) { b = new Bounds(q, Vector3.zero); first = false; } else b.Encapsulate(q);
            }
        }
        return b;
    }

    // ------------------------------------------------------------------ Circus: human cannons
    static void PlaceCannons(Def d)
    {
        var g = FullGrid(d);
        var rnd = new Random(d.seed * 17 + 3);
        bool Solid(int r, int c) => r >= 0 && r < ROWS && c >= 0 && c < COLS && g[r, c] != '.' && g[r, c] != 'b';
        var guns = new List<(Vector3 p, int r, int c)>();
        for (int row = 2; row <= 7; row++)
            for (int c = 1; c < COLS - 1; c++)
            {
                if (g[row, c] != '.') continue;
                var p = TilePos(row, c);
                if (Mathf.Abs(p.x) < 3f || Mathf.Abs(p.x) > 13.5f || !ClearAt(p, 0.75f) || !FarFromSpawns(p, 2.6f) || !OffPath(p, 2.2f)) continue;
                guns.Add((p, row, c));
            }
        var pads = new List<Vector3>();
        for (int row = 8; row <= 13; row++)
            for (int c = 1; c < COLS - 1; c++)
            {
                if (g[row, c] != '.' || (row == 13 && c == 10)) continue;
                var p = TilePos(row, c);
                if (!ClearAt(p, 1.0f) || !OffPath(p, 2.5f) || !FarFromSpawns(p, 4f)) continue;   // open floor: on the NavMesh
                pads.Add(p);
            }
        var pairs = new List<(Vector3 gun, Vector3 pad, double s)>();
        foreach (var gn in guns)
            foreach (var pd in pads)
            {
                float dz = pd.z - gn.p.z, dist = Flat(pd - gn.p);
                if (dz < 4.5f || dist < 9f || dist > 16f || Mathf.Sign(pd.x) != Mathf.Sign(gn.p.x) && Mathf.Abs(pd.x) > 1f) continue;
                int walls = 0;
                for (float k = 0.1f; k < 0.95f; k += 0.75f / dist)
                {
                    var q = Vector3.Lerp(gn.p, pd, k);
                    int r = Mathf.RoundToInt(q.z / T + 13), cc = Mathf.RoundToInt(q.x / T + 10);
                    if (Solid(r, cc)) walls++;
                }
                pairs.Add((gn.p, pd, Mathf.Min(walls, 4) * 1.0 - Mathf.Abs(dist - 12f) * 0.15 + rnd.NextDouble() * 0.3));
            }
        var chosen = new List<(Vector3 gun, Vector3 pad)>();
        foreach (float side in new[] { -1f, 1f })
            foreach (var pr in pairs.Where(x => Mathf.Sign(x.gun.x) == side).OrderByDescending(x => x.s))
            {
                bool ok = chosen.All(q => Flat(q.pad - pr.pad) >= 4f && Flat(q.pad + pr.pad) >= 4f && Flat(q.gun - pr.gun) >= 3f)
                          && Flat(pr.pad + pr.pad) >= 4f;   // a pad and its own mirror apart
                if (!ok) continue;
                chosen.Add((pr.gun, pr.pad));
                break;
            }
        int i = 0, decor = 0;
        foreach (var (gun, pad) in chosen.Concat(chosen.Select(x => (-x.gun, -x.pad))).ToList())
        {
            Vector3 dir = pad - gun; dir.y = 0f; dir.Normalize();
            var go = MechObject("Cannon" + i++, gun, Quaternion.identity);
            var hc = go.AddComponent<HumanCannon>();
            hc.landing = pad;
            // the CircusCannon model fires along its local -X
            var model = MechModel("CircusCannon", go.transform, Vector3.zero, Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 90f, 0f), 0.85f);
            hc.barrel = model ? model.transform : null;
            decor += ClearDecor(gun, 1.1f, new[] { "HayPile", "JugglingPins", "PopcornSpill" }) + ClearDecor(pad, 1.4f, new[] { "HayPile", "JugglingPins", "PopcornSpill" });
        }
        log += " cannons=" + i + " decorCleared=" + decor;
    }

    // ------------------------------------------------------------------ Lunar: meteor shower
    static void PlaceMeteors(Def d)
    {
        var g = FullGrid(d);
        var spots = new List<Vector3>();
        for (int row = 3; row <= 12; row++)
            for (int c = 1; c < COLS - 1; c++)
            {
                if (g[row, c] != '.' && g[row, c] != 'b') continue;
                var p = TilePos(row, c);
                if (Flat(p) < 3f || !FarFromSpawns(p, 5.5f) || !ClearAt(p, 0.9f)) continue;
                spots.Add(p);
            }
        var go = MechObject("MeteorShower", Vector3.zero, Quaternion.identity);
        var ms = go.AddComponent<MeteorShower>();
        ms.spots = spots.ToArray();
        ms.seed = d.seed;
        ms.warnMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/MeteorWarn.mat");
        ms.trailMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/MeteorTrail.mat");
        var rock = MechModel("MoonBoulderA", go.transform, new Vector3(0f, -30f, 0f), Quaternion.identity, 0.3f);
        if (rock) { rock.name = "MeteorRockTemplate"; ms.rock = rock.transform; rock.SetActive(false); }
        log += " meteorSpots=" + spots.Count;
    }

    // ------------------------------------------------------------------ Ice: slick patches
    static void PlaceSlickIce(Def d)
    {
        var g = FullGrid(d);
        var rnd = new Random(d.seed * 19 + 11);
        var chosen = new List<(Vector3 p, float r)>();
        // first big patches on fully open 3x3 ground; then (tight layouts) smaller ones where the 4 neighbours are open
        for (int pass = 0; pass < 2 && chosen.Count < 2; pass++)
        {
            var cands = new List<(Vector3 p, float r, double s)>();
            for (int row = pass == 0 ? 4 : 3; row <= 12; row++)
                for (int c = 2; c < COLS - 2; c++)
                {
                    bool open = g[row, c] == '.';
                    if (pass == 0) { for (int dr = -1; dr <= 1 && open; dr++) for (int dc = -1; dc <= 1; dc++) if (g[row + dr, c + dc] != '.') { open = false; break; } }
                    else open = open && g[row - 1, c] == '.' && g[row + 1, c] == '.' && g[row, c - 1] == '.' && g[row, c + 1] == '.';
                    if (!open) continue;
                    var p = TilePos(row, c);
                    if (Mathf.Abs(p.x) > 12f || Flat(p) < 4.5f || !FarFromSpawns(p, 5f) || !OffPath(p, 3.2f)) continue;
                    float r;
                    if (pass == 0)
                    {
                        r = 1.9f + (float)rnd.NextDouble() * 0.4f;
                        if (!ClearAt(p, r * 0.75f) || !ClearAt(-p, r * 0.75f)) { r = 1.6f; if (!ClearAt(p, 1.2f) || !ClearAt(-p, 1.2f)) continue; }
                    }
                    else { r = 1.4f; if (!ClearAt(p, 1.0f) || !ClearAt(-p, 1.0f)) continue; }
                    cands.Add((p, r, -Mathf.Abs(p.z + 7f) * 0.4 - Mathf.Abs(Mathf.Abs(p.x) - 5f) * 0.3 + rnd.NextDouble() * 0.8));
                }
            foreach (var cnd in cands.OrderByDescending(x => x.s))
            {
                if (chosen.Count >= 2) break;
                if (chosen.Any(q => Flat(q.p - cnd.p) < 6.5f || Flat(q.p + cnd.p) < 6.5f)) continue;
                chosen.Add((cnd.p, cnd.r));
            }
        }
        var list = new List<(Vector3 p, float r, float yaw)>();
        foreach (var c in chosen) { list.Add((c.p, c.r, 0f)); list.Add((-c.p, c.r, 180f)); }
        if (d.mode == GameMode.Competitive && g[13, 9] == '.' && g[13, 10] == '.' && g[13, 11] == '.' && g[12, 10] == '.' && ClearAt(Vector3.zero, 1.5f) && OffPath(Vector3.zero, 3.2f))
            list.Add((Vector3.zero, 2.0f, 0f));
        int i = 0, decor = 0;
        foreach (var (p, r, yaw) in list)
        {
            var go = MechObject("SlickIce" + i++, p, Quaternion.Euler(0f, yaw, 0f));
            var si = go.AddComponent<SlickIce>();
            si.radius = r;
            si.seed = Mathf.RoundToInt(Mathf.Abs(p.x) * 100f + Mathf.Abs(p.z) * 7f) + d.seed;
            decor += ClearDecor(p, r + 0.3f, new[] { "IceSlab", "IceHole", "IceShards", "BonePile", "RibCage", "BeastSkull" });
        }
        log += " slickIce=" + list.Count + " decorCleared=" + decor;
    }

    // ------------------------------------------------------------------ Candy: jelly walls
    static readonly string[] CandyWalls = { "WaferBlock", "CakeBlock", "ChocoBlock", "MarshmallowStack" };

    static void PlaceJellyWalls()
    {
        int converted = 0;
        // a layout whose walls drew few gumdrop families: turn the wall group nearest the middle (and its mirror) into gumdrops
        for (int guard = 0; guard < 2 && CountGumdropWalls() < 4; guard++)
        {
            var tiles = env.Cast<Transform>().Where(t =>
            {
                var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                return src && Array.IndexOf(CandyWalls, src.name) >= 0 && Mathf.Abs(t.position.x) < 16f && Mathf.Abs(t.position.z) < 21f
                       && t.GetComponentsInChildren<Collider>(true).Any(c => c.enabled);
            }).ToList();
            var groups = new List<List<Transform>>();
            var left = new HashSet<Transform>(tiles.Where(t => t.position.z < -0.5f));
            while (left.Count > 0)
            {
                var seed = left.First(); left.Remove(seed);
                var grp = new List<Transform> { seed };
                for (int k = 0; k < grp.Count; k++)
                    foreach (var o in left.Where(o => Flat(o.position - grp[k].position) < 1.7f).ToList()) { left.Remove(o); grp.Add(o); }
                // its mirror must exist tile for tile
                var mirror = grp.Select(t => tiles.FirstOrDefault(o => Flat(o.position + t.position) < 0.35f)).ToList();
                if (grp.Count >= 2 && mirror.All(m => m != null)) groups.Add(grp.Concat(mirror).ToList());
            }
            var best = groups.OrderBy(gr =>
            {
                var c = gr.Take(gr.Count / 2).Aggregate(Vector3.zero, (a, t) => a + t.position) / (gr.Count / 2);
                return Mathf.Abs(c.z + 7f) * 0.6f + Mathf.Abs(c.x) * 0.3f;
            }).FirstOrDefault();
            if (best == null) break;
            var rnd = new Random(Mathf.RoundToInt(best[0].position.x * 100f));
            foreach (var t in best)
            {
                Vector3 p = t.position;
                Object.DestroyImmediate(t.gameObject);
                Prefab("GumdropStack", p, rnd.Next(360), 1f, true);
                converted++;
            }
        }
        int n = 0;
        foreach (Transform t in env.Cast<Transform>().ToList())
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            if (!src || src.name != "GumdropStack") continue;
            if (!t.GetComponentsInChildren<Collider>(true).Any(c => c.enabled)) continue;   // decor, not a wall
            t.gameObject.AddComponent<JellyWall>();
            foreach (var k in t.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(k.gameObject, 0);   // wobbles: not batched
            n++;
        }
        log += " jellyWalls=" + n + (converted > 0 ? " (converted " + converted + " wall tiles)" : "");
    }

    static int CountGumdropWalls() => env.Cast<Transform>().Count(t =>
    {
        var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
        return src && src.name == "GumdropStack" && t.GetComponentsInChildren<Collider>(true).Any(c => c.enabled);
    });

    // ------------------------------------------------------------------ Egypt: dart traps
    static void PlaceDartTraps(Def d)
    {
        var g = FullGrid(d);
        var rnd = new Random(d.seed * 23 + 9);
        var cands = new List<(Vector3 face, Vector3 dir, float len, float plate, Vector3 platePos, Vector3 wall, double s)>();
        var steps = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };
        for (int row = 2; row <= 12; row++)
            for (int c = 0; c < COLS; c++)
            {
                if (g[row, c] != '#') continue;
                var wp = TilePos(row, c);
                if (ClearAt(wp, 0.45f)) continue;   // no wall built there (cut by the path)
                foreach (var (dr, dc) in steps)
                {
                    int L = 0;
                    while (L < 6)
                    {
                        int r = row + dr * (L + 1), cc = c + dc * (L + 1);
                        if (r < 0 || r > 12 || cc < 0 || cc >= COLS || g[r, cc] != '.') break;
                        if (!ClearAt(TilePos(r, cc), 0.5f)) break;
                        L++;
                    }
                    if (L < 4) continue;
                    var dir = new Vector3(dc, 0f, dr).normalized;
                    var face = wp + dir * 0.72f;
                    int k = L >= 5 ? 3 : 2;
                    var plate = TilePos(row + dr * k, c + dc * k);
                    var end = TilePos(row + dr * L, c + dc * L);
                    if (!FarFromSpawns(plate, 5f) || !FarFromSpawns(end, 4f) || !FarFromSpawns(face, 4f) || !OffPath(plate, 2.5f)) continue;
                    if (Flat(plate) < 3.5f) continue;
                    double s = -Mathf.Abs(row - 8.5f) * 0.3 - Mathf.Abs(L - 5) * 0.2 + rnd.NextDouble() * 0.6;
                    cands.Add((face, dir, L * 1.5f - 0.05f, k * 1.5f - 0.72f, plate, wp, s));
                }
            }
        var chosen = cands.OrderByDescending(x => x.s).Aggregate(new List<(Vector3 face, Vector3 dir, float len, float plate, Vector3 platePos, Vector3 wall, double s)>(), (acc, x) =>
        {
            if (acc.Count < 2 && acc.All(q => Flat(q.platePos - x.platePos) >= 6f && Flat(q.platePos + x.platePos) >= 6f && Flat(q.face - x.face) >= 3f) && Flat(x.platePos * 2f) >= 6f) acc.Add(x);
            return acc;
        });
        // the props material of the map (desert / Egypt swaps): the material of the wall the stele sits in
        Material WallMat(Vector3 wp)
        {
            var r = env.GetComponentsInChildren<Renderer>(true).Where(x => !x.name.StartsWith(MechPrefix) && x.sharedMaterial && x.sharedMaterial.name.StartsWith("IceProps"))
                       .OrderBy(x => Flat(x.transform.position - wp)).FirstOrDefault();
            return r ? r.sharedMaterial : AssetDatabase.LoadAssetAtPath<Material>(IceThemeSetup.Root + "/IceProps.mat");
        }
        int i = 0, decor = 0;
        foreach (var t in chosen.Concat(chosen.Select(x => (face: -x.face, dir: -x.dir, x.len, x.plate, platePos: -x.platePos, wall: -x.wall, x.s))).ToList())
        {
            var go = MechObject("DartTrap" + i, t.face, Quaternion.LookRotation(t.dir));
            var dt = go.AddComponent<DartTrap>();
            dt.id = i++;
            dt.length = t.len;
            dt.plateDistance = t.plate;
            dt.props = WallMat(t.wall);
            decor += ClearDecor(t.platePos, 1.0f, new[] { "BrokenColumn", "Amphorae", "DigCrates" });
        }
        log += " dartTraps=" + i + " decorCleared=" + decor;
    }
}
