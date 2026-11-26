using UnityEngine;

/// <summary>
/// Static batching done when the map loads instead of at build time (Nov 2026). Build-time static batching stored a
/// combined copy of every static prop of every map in the build (~1.9 GB uncompressed for 139 maps, 5-36 MB per map,
/// while the props' own meshes are shared by the maps of a theme). The build processor RuntimeStaticBatchProcessor
/// (Assets/Editor) takes the static props of each scene out of Unity's batching and lists them here; Start combines
/// them with StaticBatchingUtility, the same batches as before at the cost of a short combine while the map loads.
/// Static batching is also off in the Player Settings (Android, iOS), so nothing is combined at build time anyway.
/// The meshes must be Read/Write enabled (IceThemeSetup sets it on the prop FBX; the processor warns in the build log
/// about any that are not: those props are drawn unbatched).
/// </summary>
public class RuntimeStaticBatch : MonoBehaviour
{
    [Tooltip("The scene's static props (filled by the build processor)")]
    public GameObject[] targets;

    /// <summary>Time the last combine took (ms), for profiling.</summary>
    public static float LastCombineMs;

    void Start()
    {
        if (targets == null || targets.Length == 0) return;
        float t0 = Time.realtimeSinceStartup;
        int n = 0;
        for (int i = 0; i < targets.Length; i++) if (targets[i] != null) targets[n++] = targets[i];
        if (n < targets.Length) System.Array.Resize(ref targets, n);
        StaticBatchingUtility.Combine(targets, gameObject);
        // the combined meshes are drawn from the GPU copy: drop their CPU copy (a build-time batch has none either)
        var done = new System.Collections.Generic.HashSet<Mesh>();
        for (int i = 0; i < targets.Length; i++)
        {
            var mf = targets[i].GetComponent<MeshFilter>();
            var m = mf != null ? mf.sharedMesh : null;
            if (m != null && m.isReadable && m.name.StartsWith("Combined Mesh") && done.Add(m)) m.UploadMeshData(true);
        }
        LastCombineMs = (Time.realtimeSinceStartup - t0) * 1000f;
        targets = null;   // not needed any more
    }
}
