using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Moves static batching from build time to map load (Nov 2026; see RuntimeStaticBatch). For every scene processed
/// (player build, and each scene loaded in Play mode): the MeshRenderers marked Batching Static lose that flag in the
/// processed copy (the scene file is not touched) and are listed on a new "RuntimeStaticBatch" object, which combines
/// them in Start. In a build, props whose mesh is not Read/Write enabled are reported (they would be drawn unbatched):
/// turn Read/Write on in their importer (IceThemeSetup does it for the theme props).
/// </summary>
[BuildCallbackVersion(1)]
class RuntimeStaticBatchProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 1000;   // after the other scene processors

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        var targets = new List<GameObject>();
        var notReadable = new HashSet<string>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var go = mr.gameObject;
                var flags = GameObjectUtility.GetStaticEditorFlags(go);
                if ((flags & StaticEditorFlags.BatchingStatic) == 0) continue;
                var mf = go.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                GameObjectUtility.SetStaticEditorFlags(go, flags & ~StaticEditorFlags.BatchingStatic);
                targets.Add(go);
                if (!mf.sharedMesh.isReadable) notReadable.Add(AssetDatabase.GetAssetPath(mf.sharedMesh) + " (" + mf.sharedMesh.name + ")");
            }
        if (targets.Count == 0) return;
        var holder = new GameObject("RuntimeStaticBatch");
        SceneManager.MoveGameObjectToScene(holder, scene);
        holder.AddComponent<RuntimeStaticBatch>().targets = targets.ToArray();
        if (notReadable.Count > 0 && report != null)
            Debug.LogWarning("[RuntimeStaticBatch] " + scene.name + ": " + notReadable.Count + " static meshes are not Read/Write enabled and will not be batched: " + string.Join(", ", notReadable));
    }
}
