using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Grass / bush clumps were shorter than the characters (~2 m): a character showed head and shoulders above the grass
/// and then vanished all at once when fully inside. Raise() stretches every bush renderer of a scene vertically only
/// (footprint unchanged: bush areas, NavMesh and patrol points stay valid) so the median grass height reaches
/// TargetHeight. Scenes already at that height are left alone. Used by BrawlMapBuilder for new maps and by the menu
/// for the existing ones.
/// </summary>
public static class GrassHeight
{
    public const float TargetHeight = 1.95f;
    const string Menu = "Tools/Bush/Raise Grass (All Brawl Maps)";

    /// <summary>Returns the scale factor applied (1 = nothing to do).</summary>
    public static float Raise(Scene scene, float target = TargetHeight)
    {
        var grass = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Renderer>(true))
            .Where(r => BushAreaGenerator.IsBushRenderer(r)).ToList();
        if (grass.Count == 0) return 1f;
        var heights = grass.Select(r => r.bounds.size.y).OrderBy(h => h).ToList();
        float median = heights[heights.Count / 2];
        if (median >= target - 0.05f) return 1f;
        float k = Mathf.Clamp(target / median, 1f, 1.7f);
        var done = new HashSet<Transform>();
        foreach (var r in grass)
        {
            var t = r.transform;
            if (!done.Add(t)) continue;
            var s = t.localScale;
            t.localScale = new Vector3(s.x, s.y * k, s.z);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        return k;
    }

    [MenuItem(Menu)]
    public static void RaiseAllMaps()
    {
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            if (EditorSceneManager.GetSceneAt(i).isDirty) { Debug.LogError("[Grass] Save the open scenes first: " + EditorSceneManager.GetSceneAt(i).path); return; }
        var log = new System.Text.StringBuilder();
        var maps = EditorBuildSettings.scenes.Select(s => s.path)
            .Where(p => { var n = Path.GetFileNameWithoutExtension(p); return n.StartsWith("Map_Brawl_") || n.StartsWith("BrawlScene"); })
            .Distinct().ToList();
        string back = EditorSceneManager.GetActiveScene().path;
        foreach (var path in maps)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            float k = Raise(scene);
            if (k > 1f)
            {
                EditorApplication.ExecuteMenuItem("Tools/Bush/Build Bush Areas (Active Scene)");
                EditorSceneManager.SaveScene(scene);
            }
            log.AppendLine(Path.GetFileNameWithoutExtension(path) + (k > 1f ? " x" + k.ToString("F2") : " already tall"));
        }
        if (!string.IsNullOrEmpty(back) && !EditorSceneManager.GetActiveScene().isDirty) EditorSceneManager.OpenScene(back, OpenSceneMode.Single);
        Debug.Log("[Grass] " + maps.Count + " maps\n" + log);
        SessionState.SetString("GrassRaiseLog", maps.Count + " maps\n" + log);
    }
}
