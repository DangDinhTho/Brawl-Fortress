using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per grass model (by name): the leaf colour for the torn-bush leaves (BushDamage.LeafColor), the palette colour of its
/// upper leaves, and the mesh itself for the stubble a torn clump leaves (BushArea.Shatter). Baked in the editor by
/// Tools/Bush/Bake Leaf Colours: in a build the meshes and palettes are not readable, and the map's grass is statically
/// batched (its MeshFilter then holds the scene's combined mesh, not the grass mesh). The leaf colour is tinted at
/// runtime by the material's _Color (night, desert, Egypt materials).
/// </summary>
public class BushLeafColors : ScriptableObject
{
    public List<string> meshes = new List<string>();
    public List<Color> colors = new List<Color>();
    public List<Mesh> meshRefs = new List<Mesh>();
    [Tooltip("Per entry: xy = palette uv at the root of the leaves (stubble stalks), zw = uv a little higher, lighter (their cut tops)")]
    public List<Vector4> stubbleUV = new List<Vector4>();

    public static bool TryGetStubbleUV(string name, out Vector2 stem, out Vector2 cut)
    {
        stem = cut = default; Load();
        if (name == null || !map.TryGetValue(name, out int i) || i >= instance.stubbleUV.Count) return false;
        var v = instance.stubbleUV[i];
        stem = new Vector2(v.x, v.y); cut = new Vector2(v.z, v.w);
        return true;
    }

    static BushLeafColors instance;
    static bool loaded;
    static Dictionary<string, int> map;

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        instance = Resources.Load<BushLeafColors>("BushLeafColors");
        map = new Dictionary<string, int>();
        if (instance != null)
            for (int i = 0; i < instance.meshes.Count; i++) map[instance.meshes[i]] = i;
    }

    public static bool TryGet(string name, out Color c)
    {
        c = default; Load();
        if (name == null || !map.TryGetValue(name, out int i) || i >= instance.colors.Count) return false;
        c = instance.colors[i]; return true;
    }

    public static Mesh FindMesh(string name)
    {
        Load();
        if (name == null || !map.TryGetValue(name, out int i) || i >= instance.meshRefs.Count) return null;
        return instance.meshRefs[i];
    }

    /// <summary>Grass model name of a placed clump: its object name without " (12)" / "(Clone)".</summary>
    public static string GrassName(Renderer r)
    {
        if (r == null) return null;
        string n = r.gameObject.name;
        int k = n.IndexOf(" (");
        if (k > 0) n = n.Substring(0, k);
        if (n.EndsWith("(Clone)")) n = n.Substring(0, n.Length - 7);
        return n;
    }
}
