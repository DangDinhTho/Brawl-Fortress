using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Quick scene switching: Shift+F1 Splash (FlashScene), Shift+F2 Home, Shift+F3 Arena.</summary>
public static class SceneShortcuts
{
    const string Splash = "Assets/Scenes/FlashScene.unity";
    const string Home = "Assets/Scenes/Home.unity";
    const string Arena = "Assets/Scenes/Arena.unity";

    [MenuItem("Tools/Scenes/Splash (Loading) #F1", false, 1)]
    static void OpenSplash() => Open(Splash);

    [MenuItem("Tools/Scenes/Home #F2", false, 2)]
    static void OpenHome() => Open(Home);

    [MenuItem("Tools/Scenes/Arena #F3", false, 3)]
    static void OpenArena() => Open(Arena);

    static void Open(string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SceneShortcuts] Exit Play mode before switching scenes.");
            return;
        }
        // asks to save unsaved changes; Cancel aborts the switch
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }
}
