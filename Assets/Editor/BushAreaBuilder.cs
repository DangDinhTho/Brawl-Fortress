using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Bakes BushArea objects into the active scene (see BushAreaGenerator for the rules).</summary>
public static class BushAreaBuilder
{
    [MenuItem("Tools/Bush/Build Bush Areas")]
    public static void BuildMenu()
    {
        var scene = SceneManager.GetActiveScene();
        int n = BushAreaGenerator.Generate(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Bush] " + scene.name + ": built " + n + " bush areas");
    }
}
