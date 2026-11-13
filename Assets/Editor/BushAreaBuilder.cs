using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool to bake bush-hiding areas (BushArea) into map scenes. Nothing is generated at runtime.
/// Menu: Tools/Bush/Bush Area Tool. Rules: see Assets/Scripts/Bush/BushAreas.md
/// </summary>
public class BushAreaBuilder : EditorWindow
{
    Vector2 scroll;

    [MenuItem("Tools/Bush/Bush Area Tool")]
    static void Open() => GetWindow<BushAreaBuilder>("Bush Areas").Show();

    [MenuItem("Tools/Bush/Build Bush Areas (Active Scene)")]
    static void BuildActive() => Build(SceneManager.GetActiveScene());

    static int Build(Scene scene)
    {
        if (!scene.IsValid()) return 0;
        int n = BushAreaGenerator.Generate(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Bush] " + scene.name + ": " + n + " bush areas (save the scene to keep them)");
        return n;
    }

    static void Clear(Scene scene)
    {
        var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == BushAreaGenerator.RootName);
        if (root == null) return;
        Undo.DestroyObjectImmediate(root);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    static int CountAreas(Scene scene) => scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<BushArea>(true).Length);

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Bakes BushArea objects (root '" + BushAreaGenerator.RootName + "') from meshes whose name contains: " +
            string.Join(", ", BushAreaGenerator.Keywords) + ", taller than " + BushAreaGenerator.MinHeight + " m. " +
            "Rebuilding replaces the old '" + BushAreaGenerator.RootName + "' object. Save the scene afterwards.", MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var sc = SceneManager.GetSceneAt(i);
            if (!sc.isLoaded) continue;
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                EditorGUILayout.LabelField(sc.name + (sc.isDirty ? " *" : ""), GUILayout.Width(200));
                EditorGUILayout.LabelField(CountAreas(sc) + " bushes", GUILayout.Width(80));
                if (GUILayout.Button("Build", GUILayout.Width(60))) Build(sc);
                if (GUILayout.Button("Clear", GUILayout.Width(60))) Clear(sc);
                if (GUILayout.Button("Save", GUILayout.Width(60))) EditorSceneManager.SaveScene(sc);
            }
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Select bushes of active scene"))
        {
            var root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == BushAreaGenerator.RootName);
            if (root != null) Selection.objects = root.GetComponentsInChildren<BushArea>().Select(b => (Object)b.gameObject).ToArray();
        }
    }
}
