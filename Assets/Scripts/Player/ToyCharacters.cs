using UnityEngine;

/// <summary>
/// One place that picks the third-person character prefab. The toy (chibi) characters are still on trial: in the editor
/// they replace the old prefab only while the switch Tools/Characters/Use Toy Characters In Play Mode is on, and a
/// build always gets the old ones.
/// </summary>
public static class ToyCharacters
{
    public const string EditorSwitch = "BrawlFortress.ToyCharacters";

    public static GameObject LoadTps(object tf2Class)
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorPrefs.GetBool(EditorSwitch, false))
        {
            var toy = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Characters/ToyDemo/TPS_{tf2Class}_Toy.prefab");
            if (toy != null) return toy;
        }
#endif
        return Resources.Load<GameObject>($"Characters/TPS/TPS_{tf2Class}");
    }
}
