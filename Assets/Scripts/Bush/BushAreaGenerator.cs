#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Turns grass / bush meshes of a map into BushArea objects.
/// - Editor: Tools/Bush/Build Bush Areas (bakes them into the scene so they can be tweaked).
/// Editor-only: nothing is generated at runtime, a map only has bushes that were baked into its scene.
/// A mesh counts as bush when its object/mesh name contains one of Keywords and it is at least MinHeight tall.
/// </summary>
public static class BushAreaGenerator
{
    public static readonly string[] Keywords = { "grass", "bush" };
    public const float MinHeight = 0.5f;      // flat grass decals / ground textures are ignored
    public const float MinFootprint = 0.9f;   // small tufts get a footprint of at least this size (m)
    public const float LinkGap = 0.35f;       // tufts closer than this belong to the same bush
    public const float MinClusterArea = 1.5f; // a lone decorative tuft is not a hiding spot (m^2)
    public const string RootName = "Bushes";

    public static bool IsBushRenderer(Renderer r)
    {
        if (r == null || r is SkinnedMeshRenderer || r is ParticleSystemRenderer) return false;
        if (r.GetComponentInParent<BushArea>() != null || r.GetComponentInParent<TF2Player>() != null) return false;
        string n = r.name.ToLowerInvariant();
        var mf = r.GetComponent<MeshFilter>();
        string mesh = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name.ToLowerInvariant() : "";
        bool match = false;
        foreach (var k in Keywords) if (n.Contains(k) || mesh.Contains(k)) { match = true; break; }
        return match && r.bounds.size.y >= MinHeight;
    }

    public static int Generate(Scene scene)
    {
        var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        if (old != null)
        {
            if (Application.isPlaying) Object.Destroy(old); else Object.DestroyImmediate(old);
        }

        var tiles = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true))
            .Where(IsBushRenderer)
            .Select(r => r.bounds).ToList();
        if (tiles.Count == 0) return 0;

        // footprint boxes: XZ of the mesh (at least MinFootprint), Y from just below the ground to above the head
        var boxes = tiles.Select(b =>
        {
            var size = new Vector3(Mathf.Max(b.size.x, MinFootprint), b.size.y + 3f, Mathf.Max(b.size.z, MinFootprint));
            var center = new Vector3(b.center.x, b.min.y + size.y * 0.5f - 0.5f, b.center.z);
            return new Bounds(center, size);
        }).ToList();

        int[] parent = Enumerable.Range(0, boxes.Count).ToArray();
        int Find(int i) { while (parent[i] != i) i = parent[i] = parent[parent[i]]; return i; }
        for (int i = 0; i < boxes.Count; i++)
        {
            var a = boxes[i]; a.Expand(new Vector3(LinkGap, 0f, LinkGap));
            for (int j = i + 1; j < boxes.Count; j++)
                if (a.Intersects(boxes[j])) parent[Find(i)] = Find(j);
        }

        var rootGo = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(rootGo, scene);
        int count = 0;
        foreach (var grp in Enumerable.Range(0, boxes.Count).GroupBy(Find))
        {
            var list = grp.Select(i => boxes[i]).ToList();
            var total = list[0]; foreach (var b in list) total.Encapsulate(b);
            if (total.size.x * total.size.z < MinClusterArea) continue;
            var go = new GameObject("Bush_" + count++);
            go.transform.SetParent(rootGo.transform, false);
            go.transform.position = total.center;
            var area = go.AddComponent<BushArea>();
            area.boxes = list;
            area.RecalculateTotal();
        }
        return count;
    }
}
#endif
