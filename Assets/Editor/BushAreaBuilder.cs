using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Builds BushArea objects from grass tiles (renderers whose name contains 'Grass') in the active scene.</summary>
public static class BushAreaBuilder
{
    [MenuItem("Tools/Bush/Build Bush Areas From Grass")]
    public static void BuildMenu()
    {
        int n = Build(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Grass");
        Debug.Log("[BushAreaBuilder] built " + n + " bush areas");
    }

    public static int Build(UnityEngine.SceneManagement.Scene scene, string nameFilter)
    {
        var tiles = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true))
            .Where(r => r.name.Contains(nameFilter) && r.GetComponentInParent<BushArea>() == null)
            .Select(r => r.bounds).ToList();

        var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Bushes");
        if (old != null) Object.DestroyImmediate(old);
        if (tiles.Count == 0) return 0;

        // footprint boxes: XZ of the tile (slightly shrunk so blades poking out do not count), Y from ground to above head
        var boxes = tiles.Select(b =>
        {
            var size = new Vector3(b.size.x * 0.9f, b.size.y + 3f, b.size.z * 0.9f);
            var center = new Vector3(b.center.x, b.min.y + size.y * 0.5f - 0.5f, b.center.z);
            return new Bounds(center, size);
        }).ToList();

        // union-find on touching boxes
        int[] parent = Enumerable.Range(0, boxes.Count).ToArray();
        int Root(int i) { while (parent[i] != i) i = parent[i] = parent[parent[i]]; return i; }
        for (int i = 0; i < boxes.Count; i++)
        {
            var a = boxes[i]; a.Expand(new Vector3(0.3f, 0, 0.3f));
            for (int j = i + 1; j < boxes.Count; j++)
                if (a.Intersects(boxes[j])) parent[Root(i)] = Root(j);
        }

        var rootGo = new GameObject("Bushes");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rootGo, scene);
        int count = 0;
        foreach (var grp in Enumerable.Range(0, boxes.Count).GroupBy(Root))
        {
            var go = new GameObject("Bush_" + count++);
            go.transform.SetParent(rootGo.transform, false);
            var area = go.AddComponent<BushArea>();
            area.boxes = grp.Select(i => boxes[i]).ToList();
            var total = area.boxes[0]; foreach (var b in area.boxes) total.Encapsulate(b);
            go.transform.position = total.center;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        return count;
    }
}
