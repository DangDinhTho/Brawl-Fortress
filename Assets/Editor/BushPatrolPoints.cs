using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes bot patrol points inside every BushArea of a scene into root object 'BushPoints' and assigns them to
/// MapManager.BushPoints (bots then often walk into bushes, see MapManager.GetPatrolPoint). Editor only.
/// 1 point per bush, 2 for bushes of 9 m2 or more (centre-most tile + the tile farthest from it).
/// Runs automatically after Tools/Bush/Build Bush Areas.
/// </summary>
public static class BushPatrolPoints
{
    public const string RootName = "BushPoints";
    const float BigBushArea = 9f;
    /// <summary>Points of the last Build that could not be snapped onto the NavMesh inside their bush.</summary>
    public static int LastOffNav;

    [MenuItem("Tools/Bush/Build Bush Patrol Points (Active Scene)")]
    static void BuildActive()
    {
        var s = SceneManager.GetActiveScene();
        int n = Build(s);
        Debug.Log("[Bush] " + s.name + ": " + n + " bush patrol points (save the scene to keep them)");
    }

    public static int Build(Scene scene)
    {
        if (!scene.IsValid()) return 0;
        var mm = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MapManager>(true)).FirstOrDefault();
        var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        if (old) Object.DestroyImmediate(old);
        var bushes = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BushArea>(true)).Where(b => b.boxes.Count > 0).ToList();
        var points = new List<Transform>();
        // in edit mode the scene's baked NavMesh is often not registered (or another map's still is):
        // load exactly this scene's NavMeshSurface data while placing the points
        var navInst = new NavMeshDataInstance();
        NavMesh.RemoveAllNavMeshData();
        {
            var surf = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).FirstOrDefault(c => c && c.GetType().Name == "NavMeshSurface");
            var data = surf ? new SerializedObject(surf).FindProperty("m_NavMeshData")?.objectReferenceValue as NavMeshData : null;
            if (data) navInst = NavMesh.AddNavMeshData(data, surf.transform.position, surf.transform.rotation);
        }
        LastOffNav = 0;
        if (bushes.Count > 0)
        {
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            foreach (var b in bushes)
            {
                b.RecalculateTotal();
                var c = Vector3.zero; foreach (var bx in b.boxes) c += bx.center; c /= b.boxes.Count;
                var first = b.boxes.OrderBy(bx => Flat(bx.center - c)).First();
                var picks = new List<Bounds> { first };
                float area = b.boxes.Sum(bx => bx.size.x * bx.size.z);
                if (area >= BigBushArea && b.boxes.Count > 1) picks.Add(b.boxes.OrderByDescending(bx => Flat(bx.center - first.center)).First());
                int k = 0;
                foreach (var bx in picks)
                {
                    var p = new Vector3(bx.center.x, bx.min.y, bx.center.z);
                    if (NavMesh.SamplePosition(p, out var hit, 1.2f, NavMesh.AllAreas) && b.Contains(new Vector3(hit.position.x, bx.center.y, hit.position.z)))
                        p = hit.position;
                    else LastOffNav++;
                    var go = new GameObject(b.name + "_" + (k++));
                    go.transform.SetParent(root.transform, false);
                    go.transform.position = p;
                    points.Add(go.transform);
                }
            }
        }
        if (navInst.valid) navInst.Remove();
        if (mm)
        {
            Undo.RecordObject(mm, "Bush patrol points");
            mm.BushPoints = points.ToArray();
            EditorUtility.SetDirty(mm);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        return points.Count;
    }

    static float Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude; }
}
