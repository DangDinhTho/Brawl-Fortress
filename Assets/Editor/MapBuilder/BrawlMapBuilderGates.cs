using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

/// <summary>
/// The themed gate behind each team's spawn (IceGate, TempleGate, ToriiGate...) and the match camera.
/// They stood at |z| 22.5, astride the end VitualWall (inner face 22.1), right behind the back spawn row (19.6). 8-9.7 m
/// tall, they hid the players at their spawn from the match camera, which hangs 10.4 m above and 10.6 m behind the player
/// (towards the player's own end), and on Payload / MvM the bomb hole (16.5) too. Now they stand further back, in the
/// alcove between the end border pieces, each scaled down just enough to stay under the camera's line of sight
/// (GateScale). The end lakes (Volcano, Swamp, Greek, Candy) leave a floor inlet under the gate.
/// </summary>
public static partial class BrawlMapBuilder
{
    /// <summary>|z| of the gates' centre.</summary>
    public const float GateZ = 27f;
    /// <summary>The end lakes start at |z| 24.2; under the gate (|x| below the half width) they start at GateInletBack.</summary>
    const float GateInletHalf = 6.5f, GateInletBack = 30.5f;
    /// <summary>Match camera relative to the player it follows: up / back (towards the player's own end); look-at height.</summary>
    const float CamUp = 10.4f, CamBack = 10.6f, CamTargetY = 1.2f;
    /// <summary>The gate must not hide a player standing this far from the middle (1 m behind the back spawn row).</summary>
    const float GateCheckZ = 20.6f;
    /// <summary>Floor decals with faces flush with the floor (y = 0): lifted a little so they do not z-fight with it.</summary>
    static readonly string[] FlushDecals = { "LavaCrack", "CooledFlow" };
    const float DecalLift = 0.012f;

    static readonly string[] EndGates = { "IceGate", "XmasGate", "TempleGate", "VolcanoGate", "FactoryGate", "CircusGate", "AirlockGate",
                                          "SwampGate", "GreekGate", "CandyGate", "AtlantisGate", "SafariGate", "ToriiGate" };
    static readonly string[] EndLakeNames = { "LavaLake", "AcidLake", "SeaWater", "ChocoLake" };

    /// <summary>Largest uniform scale (at most 0.98, the old size) at which the gate, turned to the arena with its centre at
    /// |z| = zc, stays at least 30 cm under the match camera's line of sight to a player standing at |z| = GateCheckZ.</summary>
    public static float GateScale(GameObject prefab, float zc)
    {
        var verts = new List<Vector3>();
        foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            var m = prefab.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            foreach (var v in mf.sharedMesh.vertices) verts.Add(m.MultiplyPoint3x4(v));
        }
        float slope = CamUp / CamBack;
        for (float s = 0.98f; s > 0.3f; s -= 0.01f)
        {
            bool ok = true;
            foreach (var v in verts)
            {
                // the gate's front (local +Z) faces the arena: local z = +d is d metres nearer the middle
                float d = (zc - v.z * s) - GateCheckZ;                       // horizontal distance from the player, outwards
                if (v.y * s > CamTargetY + slope * d - 0.3f) { ok = false; break; }
            }
            if (ok) return (float)Math.Round(s, 2);
        }
        return 0.3f;
    }

    /// <summary>The gate piece of a theme behind each team's spawn, closing the gap in the border there (walled only by the
    /// invisible VitualWall), at |z| GateZ and the scale GateScale finds for it.</summary>
    static void PlaceGates(string prefab)
    {
        if (!prefabs.ContainsKey(prefab)) return;
        float s = GateScale(AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[prefab]), GateZ);
        Prefab(prefab, new Vector3(-0.3f, 0f, GateZ), 180f, s, true);   // front (+Z) turned to the arena
        Prefab(prefab, new Vector3(-0.3f, 0f, -GateZ), 0f, s, true);
        log += " gate=" + prefab + "x" + s.ToString("F2");
    }

    /// <summary>The lake beyond each end, from |z| 24.2 to far, with a floor inlet under the gate (|x| below
    /// GateInletHalf, up to GateInletBack). lake(x0, x1, z0, z1) is the theme's quad maker.</summary>
    static void EndLakes(Action<float, float, float, float> lake, float left, float right, float far)
    {
        for (int sg = -1; sg <= 1; sg += 2)
        {
            lake(-left, -GateInletHalf, sg * 24.2f, sg * far);
            lake(GateInletHalf, right, sg * 24.2f, sg * far);
            lake(-GateInletHalf, GateInletHalf, sg * GateInletBack, sg * far);
        }
    }

    static bool IsFlushDecal(string name) => Array.IndexOf(FlushDecals, name) >= 0;

    // ------------------------------------------------------------------ the camera's view behind the spawns
    /// <summary>Border piece families (variants A / B / C) that PrepareBorderAndGround lays round the arena.</summary>
    static readonly string[] BorderFamilies = { "GlacierWall", "IceCliff", "SandCliff", "BasaltRim", "FactoryWall", "CircusWall", "CraterWall",
                                                "SwampBank", "GreekCliff", "CandyWall", "ReefWall", "SavannaRidge", "ShrineWall" };
    /// <summary>The match camera's lines of sight to what must stay visible at the ends: every spawn point (and 1 m behind
    /// it) and the bomb hole of Payload / MvM, from the chest (CamTargetY) and 1.2 m to either side.</summary>
    static List<Ray> EndCamRays(out float len)
    {
        var pts = new List<Vector3>();
        var tp = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "TeamPos");
        if (tp)
            foreach (var t in tp.GetComponentsInChildren<Transform>())
                if (t.childCount == 0) { pts.Add(t.position); pts.Add(t.position + new Vector3(0f, 0f, Mathf.Sign(t.position.z))); }
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var c in root.GetComponentsInChildren<BombCartController>(true)) if (c.target) pts.Add(c.target.position);
            foreach (var c in root.GetComponentsInChildren<ManvsMachineManager>(true)) if (c.target) pts.Add(c.target.position);
        }
        var off = new Vector3(0f, CamUp, CamBack);
        len = off.magnitude;
        var rays = new List<Ray>();
        foreach (var p in pts)
        {
            if (Mathf.Abs(p.z) < 8f) continue;
            float sg = Mathf.Sign(p.z);
            foreach (float dx in new[] { -1.2f, 0f, 1.2f })
                rays.Add(new Ray(new Vector3(p.x + dx, CamTargetY, p.z), new Vector3(0f, CamUp, sg * CamBack)));
        }
        return rays;
    }

    static bool RayTri(Vector3 o, Vector3 d, Vector3 a, Vector3 b, Vector3 c, out float t)
    {
        t = 0f;
        Vector3 e1 = b - a, e2 = c - a, p = Vector3.Cross(d, e2);
        float det = Vector3.Dot(e1, p);
        if (Mathf.Abs(det) < 1e-8f) return false;
        float inv = 1f / det; Vector3 s = o - a;
        float u = Vector3.Dot(s, p) * inv; if (u < 0f || u > 1f) return false;
        Vector3 q = Vector3.Cross(s, e1);
        float v = Vector3.Dot(d, q) * inv; if (v < 0f || u + v > 1f) return false;
        t = Vector3.Dot(e2, q) * inv;
        return t > 0f;
    }

    /// <summary>True when a mesh of t cuts one of the camera's lines of sight (EndCamRays).</summary>
    static bool BlocksEndCamera(Transform t, List<Ray> rays, float len)
    {
        foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
        {
            var r = mf.GetComponent<Renderer>();
            if (!r || !r.enabled || mf.sharedMesh == null) continue;
            var b = r.bounds;
            Vector3[] verts = null; int[] tris = null;
            foreach (var ray in rays)
            {
                if (!b.IntersectRay(ray, out float bd) || bd > len) continue;
                if (verts == null) { verts = mf.sharedMesh.vertices; tris = mf.sharedMesh.triangles; }
                var w2l = mf.transform.worldToLocalMatrix;
                Vector3 lo = w2l.MultiplyPoint3x4(ray.origin), ld = w2l.MultiplyPoint3x4(ray.origin + ray.direction * len) - lo;
                for (int i = 0; i < tris.Length; i += 3)
                    if (RayTri(lo, ld, verts[tris[i]], verts[tris[i + 1]], verts[tris[i + 2]], out float k) && k <= 1f) return true;
            }
        }
        return false;
    }

    /// <summary>Keeps the match camera's view of the back of the arena clear. Beyond each end wall (|z| > 22.5): a border
    /// piece that rises into the camera's line of sight to a player standing behind the spawns (the Savanna ridge with the
    /// baobab) becomes a lower variant of its family; a backdrop piece (big cypress, baobab, cherry tree...) is pushed
    /// outwards until it clears it (the glow / warm pool under it follows). Returns a log fragment.</summary>
    static string ClearEndCamera()
    {
        if (prefabs == null) LoadAssets();
        Physics.SyncTransforms();
        var rays = EndCamRays(out float len);
        int moved = 0, swapped = 0; var stuck = new List<string>();
        var kids = env.Cast<Transform>().ToList();
        foreach (var t in kids)
        {
            if (t == null || Mathf.Abs(t.position.z) < 22.5f) continue;   // the arena itself is never touched
            var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            string n = src ? src.name : t.name;
            if (Array.IndexOf(EndGates, n) >= 0 || EndLakeNames.Any(l => n.StartsWith(l)) || n.StartsWith("GlowPool") || n.StartsWith("WarmPool")) continue;
            if (!BlocksEndCamera(t, rays, len)) continue;
            string fam = BorderFamilies.FirstOrDefault(f => n.StartsWith(f) && n.Length == f.Length + 1);
            if (fam != null)
            {
                GameObject repl = null;
                foreach (char c in "ABC")
                {
                    string cand = fam + c;
                    if (cand == n || !prefabs.TryGetValue(cand, out var path)) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
                    go.transform.SetParent(env, false);
                    go.transform.SetPositionAndRotation(t.position, t.rotation);
                    go.transform.localScale = t.localScale;
                    bool colOn = t.GetComponentsInChildren<Collider>(true).Any(k => k.enabled);
                    foreach (var k in go.GetComponentsInChildren<Collider>(true)) k.enabled = colOn;
                    Finish(go);
                    if (!BlocksEndCamera(go.transform, rays, len)) { repl = go; break; }
                    Object.DestroyImmediate(go);
                }
                if (repl) { repl.transform.SetSiblingIndex(t.GetSiblingIndex()); Object.DestroyImmediate(t.gameObject); swapped++; }
                else stuck.Add(n);
                continue;
            }
            var old = t.position;
            for (int i = 0; i < 16 && BlocksEndCamera(t, rays, len); i++) t.position += new Vector3(0f, 0f, Mathf.Sign(t.position.z));
            if (BlocksEndCamera(t, rays, len)) { t.position = old; stuck.Add(n); continue; }
            var delta = t.position - old;
            foreach (var pool in kids.Where(k => k != null && (k.name.StartsWith("GlowPool") || k.name.StartsWith("WarmPool"))
                                                 && new Vector2(k.position.x - old.x, k.position.z - old.z).sqrMagnitude < 0.09f))
                pool.position += delta;
            moved++;
        }
        Physics.SyncTransforms();
        return " endCam: moved=" + moved + " swapped=" + swapped + (stuck.Count > 0 ? " stuck=" + string.Join("/", stuck) : "");
    }

    // ------------------------------------------------------------------ existing scenes
    [MenuItem("Tools/Maps/Fix End Gates (all maps)")]
    public static void FixEndGatesMenu() => Debug.Log("[EndGates] " + FixEndGatesBatch(0, 999));

    /// <summary>FixEndGates over the build's map scenes start .. start + count - 1 (in build order). About 8 s per map with a
    /// themed gate (NavMesh rebake): drive ~12 per MCP call.</summary>
    public static string FixEndGatesBatch(int start, int count)
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return "wrong project";
        GuardUnsavedScenes();
        var log = new List<string>();
        foreach (var path in EditorBuildSettings.scenes.Where(x => x.enabled && x.path.Contains("/Map_Brawl_")).Select(x => x.path).Skip(start).Take(count))
        {
            string r = FixEndGates(path);
            if (r.Length > 0) log.Add(r);
        }
        return log.Count + " maps fixed\n" + string.Join("\n", log);
    }

    /// <summary>Moves / rescales the end gates of an existing map scene (opened alone, then saved): to |z| GateZ at the
    /// GateScale size, the glow / warm pool under a gate with it; cuts the floor inlet into the end lakes; lifts the flush
    /// floor decals; rebakes the NavMesh (the gates' old footprint was cut out of it) and the bush patrol points.
    /// Returns "" for a scene without a themed end gate (the old themes keep the base scene's open arch).</summary>
    public static string FixEndGates(string scenePath)
    {
        scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var envGo = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Environment");
        if (!envGo) return "";
        env = envGo.transform;
        var kids = env.Cast<Transform>().ToList();
        int gates = 0, pools = 0; string gname = ""; float gs = 0f;
        foreach (var t in kids)
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            string n = src ? src.name : t.name;
            if (Array.IndexOf(EndGates, n) < 0) continue;
            Vector3 p = t.position;
            if (Mathf.Abs(p.x) > 3f || Mathf.Abs(p.z) < 20f || Mathf.Abs(p.z) > GateZ + 1f) continue;   // Japan's shrine torii stand far out
            float oldS = t.lossyScale.x;
            gs = GateScale(src ? src : t.gameObject, GateZ); gname = n;
            var np = new Vector3(p.x, p.y, Mathf.Sign(p.z) * GateZ);
            // the glow / warm pool laid under the gate (Swamp, Xmas night) follows it
            foreach (var pool in kids.Where(k => (k.name.StartsWith("GlowPool") || k.name.StartsWith("WarmPool"))
                                                 && new Vector2(k.position.x - p.x, k.position.z - p.z).sqrMagnitude < 0.09f))
            {
                pool.position = new Vector3(np.x, pool.position.y, np.z);
                var ls = pool.localScale; float k = gs / Mathf.Max(oldS, 0.01f);
                pool.localScale = new Vector3(ls.x * k, ls.y, ls.z * k);
                pools++;
            }
            t.position = np;
            t.localScale = Vector3.one * gs;
            gates++;
        }
        if (gates == 0) return "";
        int lakes = SplitEndLakes(kids);
        int decals = 0;
        foreach (var t in kids)
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            if (!IsFlushDecal(src ? src.name : t.name) || Mathf.Abs(t.position.y) > 0.004f) continue;
            t.position += Vector3.up * DecalLift; decals++;
        }
        string cam = ClearEndCamera();
        string nav = RebakeOpenScene(scene.name);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return scene.name + ": " + gname + " x" + gs.ToString("F2") + " gates=" + gates + " pools=" + pools + " lakesSplit=" + lakes + " decals=" + decals + cam + " | " + nav;
    }

    /// <summary>Splits each end lake quad (|z| 24.2 .. far, across the middle) into the two sides plus a middle piece that
    /// starts at GateInletBack, so the gate stands on the floor.</summary>
    static int SplitEndLakes(List<Transform> kids)
    {
        int n = 0;
        foreach (var t in kids)
        {
            if (!EndLakeNames.Any(l => t.name.StartsWith(l))) continue;
            Vector3 p = t.position, s = t.localScale;
            float sg = Mathf.Sign(p.z);
            float x0 = p.x - s.x / 2f, x1 = p.x + s.x / 2f, zn = p.z - sg * s.z / 2f, zf = p.z + sg * s.z / 2f;
            if (Mathf.Abs(Mathf.Abs(zn) - 24.2f) > 0.05f || x0 > -GateInletHalf || x1 < GateInletHalf) continue;
            SetQuad(t, x0, -GateInletHalf, zn, zf);
            SetQuad(CopyOf(t), GateInletHalf, x1, zn, zf);
            SetQuad(CopyOf(t), -GateInletHalf, GateInletHalf, sg * GateInletBack, zf);
            n++;
        }
        return n;
    }

    static void SetQuad(Transform t, float x0, float x1, float z0, float z1)
    {
        t.position = new Vector3((x0 + x1) / 2f, t.position.y, (z0 + z1) / 2f);
        t.localScale = new Vector3(Mathf.Abs(x1 - x0), t.localScale.y, Mathf.Abs(z1 - z0));
    }

    static Transform CopyOf(Transform t)
    {
        var go = Object.Instantiate(t.gameObject, t.parent);
        go.name = t.name;
        go.transform.SetPositionAndRotation(t.position, t.rotation);
        go.transform.localScale = t.localScale;
        return go.transform;
    }

    /// <summary>Rebakes the NavMesh of the open map scene (bushes walkable) and its bush patrol points; does not save.</summary>
    static string RebakeOpenScene(string id)
    {
        var data = BakeNavMesh(id);
        NavMesh.RemoveAllNavMeshData();
        var inst = NavMesh.AddNavMeshData(data);
        var tp = Root("TeamPos").GetComponentsInChildren<Transform>().Where(t => t.childCount == 0).ToList();
        int off = tp.Count(t => !NavMesh.SamplePosition(t.position, out var h, 1f, NavMesh.AllAreas));
        string path = "?";
        var red = tp.FirstOrDefault(t => t.position.z < 0); var blue = tp.FirstOrDefault(t => t.position.z > 0);
        if (red && blue && NavMesh.SamplePosition(red.position, out var ha, 2f, NavMesh.AllAreas) && NavMesh.SamplePosition(blue.position, out var hb, 2f, NavMesh.AllAreas))
        { var np = new NavMeshPath(); NavMesh.CalculatePath(ha.position, hb.position, NavMesh.AllAreas, np); path = np.status.ToString(); }
        inst.Remove();
        int pts = BushPatrolPoints.Build(scene);
        return "redToBlue=" + path + " spawnsOffNav=" + off + " bushPoints=" + pts + " offNav=" + BushPatrolPoints.LastOffNav;
    }
}
