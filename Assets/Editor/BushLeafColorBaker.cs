using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Bakes Resources/BushLeafColors.asset: for every grass / bush model (name containing Grass, Tuft, Reeds or
/// Bush), the average colour of its upper leaves read from its material's texture at the vertices' uvs. Run after adding
/// a grass model (Tools/Bush/Bake Leaf Colours).</summary>
public static class BushLeafColorBaker
{
    const string OutPath = "Assets/Resources/BushLeafColors.asset";

    [MenuItem("Tools/Bush/Bake Leaf Colours")]
    public static void Bake()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        var asset = AssetDatabase.LoadAssetAtPath<BushLeafColors>(OutPath);
        if (asset == null) { asset = ScriptableObject.CreateInstance<BushLeafColors>(); AssetDatabase.CreateAsset(asset, OutPath); }
        asset.meshes.Clear(); asset.colors.Clear(); asset.meshRefs.Clear(); asset.stubbleUV.Clear();
        var texCache = new System.Collections.Generic.Dictionary<string, Texture2D>();
        foreach (var g in AssetDatabase.FindAssets("t:Model"))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            string n = Path.GetFileNameWithoutExtension(p);
            string ln = n.ToLowerInvariant();
            if (!(ln.Contains("grass") || ln.Contains("tuft") || ln.Contains("reeds") || ln.Contains("bush"))) continue;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go == null) continue;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; var mr = mf.GetComponent<MeshRenderer>();
                if (mesh == null || asset.meshes.Contains(mesh.name)) continue;
                Texture2D tex = null;
                // the new-theme props all draw from the shared palette; others from their own material's texture
                var mat = mr != null ? mr.sharedMaterial : null;
                string texPath = mat != null && mat.mainTexture != null ? AssetDatabase.GetAssetPath(mat.mainTexture) : null;
                if (p.StartsWith("Assets/Art/IceTheme")) texPath = "Assets/Art/IceTheme/IcePalette.png";
                else if (p.StartsWith("Assets/Battle Arena - Cartoon Assets"))
                {   // the old-theme maps draw these with the shared World atlas, not the model's own material
                    var world = AssetDatabase.LoadAssetAtPath<Material>("Assets/Battle Arena - Cartoon Assets/Materials/World.mat");
                    if (world != null && world.mainTexture != null) texPath = AssetDatabase.GetAssetPath(world.mainTexture);
                }
                if (!string.IsNullOrEmpty(texPath) && File.Exists(texPath))
                {
                    if (!texCache.TryGetValue(texPath, out tex)) { tex = new Texture2D(2, 2); tex.LoadImage(File.ReadAllBytes(texPath)); texCache[texPath] = tex; }
                }
                Color c = new Color(0.45f, 0.78f, 0.3f);
                // stubble: the uv of the stalks (low on the leaves) and of their cut tops (a little higher = lighter)
                Vector4 suv = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
                {
                    var v = mesh.vertices; var uv = mesh.uv;
                    if (uv.Length == v.Length && v.Length > 0)
                    {
                        float top = v.Max(x => x.y);
                        Vector2 s = Vector2.zero, t = Vector2.zero; int ns = 0, nt = 0;
                        for (int i = 0; i < v.Length; i++)
                        {
                            float h = v[i].y / Mathf.Max(top, 1e-4f);
                            if (h > 0.12f && h < 0.25f) { s += uv[i]; ns++; }
                            if (h > 0.32f && h < 0.45f) { t += uv[i]; nt++; }
                        }
                        if (ns > 0) { s /= ns; suv.x = s.x; suv.y = s.y; }
                        if (nt > 0) { t /= nt; suv.z = t.x; suv.w = t.y; } else { suv.z = suv.x; suv.w = suv.y; }
                    }
                }
                if (tex != null && tex.width > 2)
                {
                    var v = mesh.vertices; var uv = mesh.uv;
                    if (uv.Length == v.Length && v.Length > 0)
                    {
                        float top = v.Max(x => x.y);
                        Color sum = Color.black; int k = 0;
                        for (int i = 0; i < v.Length; i++)
                            if (v[i].y > top * 0.45f) { sum += tex.GetPixelBilinear(uv[i].x, uv[i].y); k++; }
                        if (k > 0) c = sum / k;
                    }
                }
                else if (mat != null && mat.HasProperty("_Color")) c = mat.color;
                c.a = 1f;
                if (c.r > 0.97f && c.g > 0.97f && c.b > 0.97f) c = new Color(0.45f, 0.78f, 0.3f);   // a blank texture: the default leaf colour
                asset.meshes.Add(mesh.name); asset.colors.Add(c); asset.meshRefs.Add(mesh); asset.stubbleUV.Add(suv);
                // placed clumps are named after the model (prefab); add that name too when the mesh is named otherwise
                if (n != mesh.name && !asset.meshes.Contains(n)) { asset.meshes.Add(n); asset.colors.Add(c); asset.meshRefs.Add(mesh); asset.stubbleUV.Add(suv); }
            }
        }
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log("[BushLeafColors] " + asset.meshes.Count + " grass meshes: " + string.Join(", ", asset.meshes.Select((m, i) => m + "=#" + ColorUtility.ToHtmlStringRGB(asset.colors[i]))));
    }
}
