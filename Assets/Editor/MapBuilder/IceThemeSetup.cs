using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ice (arctic) theme assets made in Blender (IceTheme_Props.zip): unzip into Assets/Art/IceTheme, then
/// Tools/Maps/Ice Theme/Setup Assets. Sets the import settings, makes the materials (FlatKit, the shared 128 px
/// palette IcePalette.png, like the other map packs) and one prefab per model with a fitting collider. The map builder
/// (Theme.Ice) picks the prefabs up from Assets/Art/IceTheme/Prefabs and keeps their own material.
/// </summary>
public static class IceThemeSetup
{
    public const string Root = "Assets/Art/IceTheme";
    public const string PrefabDir = Root + "/Prefabs";
    public const string PropsMat = Root + "/IceProps.mat";
    public const string GroundMat = Root + "/IceGround.mat";
    public const string CliffMat = Root + "/IceCliff.mat";

    public static Material Props => AssetDatabase.LoadAssetAtPath<Material>(PropsMat);
    public static Material Ground => AssetDatabase.LoadAssetAtPath<Material>(GroundMat);
    public static Material Cliff => AssetDatabase.LoadAssetAtPath<Material>(CliffMat);
    public static bool Ready => AssetDatabase.IsValidFolder(PrefabDir) && Props != null;

    [MenuItem("Tools/Maps/Ice Theme/Setup Assets")]
    static void Menu() => Debug.Log("[IceTheme] " + Setup());

    public static string Setup()
    {
        if (!AssetDatabase.IsValidFolder(Root)) return "missing " + Root + " (unzip IceTheme_Props.zip there)";
        var fbx = Directory.GetFiles(Root, "*.fbx").Select(p => p.Replace('\\', '/')).ToArray();
        if (fbx.Length == 0) return "no .fbx in " + Root;

        // palette: crisp colour cells
        string pal = Root + "/IcePalette.png";
        if (AssetImporter.GetAtPath(pal) is TextureImporter ti)
        {
            // bilinear: flat cells are sampled at their centre (unchanged), the gradient cells (frost_grad, ice_grad) blend
            // smoothly between their 16 rows instead of showing stripes; no mipmaps, so cells never bleed into each other
            ti.textureType = TextureImporterType.Default; ti.filterMode = FilterMode.Bilinear; ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.sRGBTexture = true;
            ti.SaveAndReimport();
        }
        // ice ground: a tiling texture (cracks, frost, snow patches)
        string groundTex = Root + "/IceGround.png";
        if (AssetImporter.GetAtPath(groundTex) is TextureImporter gi)
        {
            gi.textureType = TextureImporterType.Default; gi.wrapMode = TextureWrapMode.Repeat; gi.mipmapEnabled = true;
            gi.filterMode = FilterMode.Bilinear; gi.anisoLevel = 4; gi.textureCompression = TextureImporterCompression.Compressed; gi.sRGBTexture = true;
            gi.SaveAndReimport();
        }
        foreach (var f in fbx)
        {
            if (!(AssetImporter.GetAtPath(f) is ModelImporter mi)) continue;
            mi.globalScale = 1f; mi.useFileScale = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
            mi.importNormals = ModelImporterNormals.Import;
            mi.SaveAndReimport();
        }

        // materials
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(pal);
        var reference = FlatKitReference();
        var props = MakeMat(PropsMat, reference, tex, Color.white);
        var gtex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/IceGround.png");
        var groundMat = MakeMat(GroundMat, reference, gtex, gtex ? Color.white : new Color(0.88f, 0.93f, 1.0f));
        var world = AssetDatabase.LoadAssetAtPath<Material>("Assets/Battle Arena - Cartoon Assets/Materials/World.mat");
        if (world != null)
        {
            var cliff = Save(CliffMat, new Material(world));
            if (cliff.HasProperty("_Color")) cliff.SetColor("_Color", new Color(0.80f, 0.90f, 1.0f));
            if (cliff.HasProperty("_BaseColor")) cliff.SetColor("_BaseColor", new Color(0.80f, 0.90f, 1.0f));
            EditorUtility.SetDirty(cliff);
        }

        // prefabs
        if (!AssetDatabase.IsValidFolder(PrefabDir)) AssetDatabase.CreateFolder(Root, "Prefabs");
        int made = 0;
        foreach (var f in fbx)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(f);
            if (model == null) continue;
            string name = Path.GetFileNameWithoutExtension(f);
            bool bush = name == "FrostBush" || name == "SnowTuft";
            string prefabName = name == "SnowTuft" ? "SnowTuftBush" : name;     // "bush" in the name: a hiding bush
            var inst = Object.Instantiate(model);
            inst.name = prefabName;
            foreach (var t in inst.GetComponentsInChildren<Transform>(true)) if (bush && !t.name.ToLower().Contains("bush")) t.name += "Bush";
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true)) r.sharedMaterials = r.sharedMaterials.Select(_ => props).ToArray();
            AddCollider(inst, name);
            PrefabUtility.SaveAsPrefabAsset(inst, PrefabDir + "/" + prefabName + ".prefab");
            Object.DestroyImmediate(inst);
            made++;
        }
        if (MakeFloor(groundMat)) made++;
        AssetDatabase.SaveAssets();
        return made + " prefabs in " + PrefabDir + " (material " + (reference ? reference.shader.name : "Standard") + ")";
    }

    static Material FlatKitReference()
    {
        // the matte FlatKit character material: same cel shading and rim as the rest of the game
        foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art/BrightPort" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (m != null && m.shader != null && m.shader.name == "FlatKit/Stylized Surface") return m;
        }
        return null;
    }

    static Material MakeMat(string path, Material reference, Texture tex, Color tint)
    {
        var m = reference != null ? new Material(reference) : new Material(Shader.Find("Standard"));
        foreach (var p in new[] { "_BaseMap", "_MainTex" }) if (m.HasProperty(p)) { m.SetTexture(p, tex); m.SetTextureScale(p, Vector2.one); m.SetTextureOffset(p, Vector2.zero); }
        foreach (var p in new[] { "_BaseColor", "_Color" }) if (m.HasProperty(p)) m.SetColor(p, tint);
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
        m.DisableKeyword("_EMISSION");
        return Save(path, m);
    }

    static Material Save(string path, Material m)
    {
        var old = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (old != null) { old.shader = m.shader; old.CopyPropertiesFromMaterial(m); old.shaderKeywords = m.shaderKeywords; EditorUtility.SetDirty(old); Object.DestroyImmediate(m); return old; }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void AddCollider(GameObject go, string name)
    {
        var rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return;
        Bounds b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
        if (name.StartsWith("GlacierBlock") || name.StartsWith("IceChunk") || name == "FrozenLog")
        {
            // snow drips hang over the edge: keep the collider inside its 1.5 m tile so it never blocks the next one
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, b.center.y, 0f);
            bc.size = new Vector3(Mathf.Min(b.size.x, 1.45f), b.size.y, Mathf.Min(b.size.z, 1.45f));
            return;
        }
        switch (name)
        {
            case "FrostBush": case "SnowTuft": case "SnowMound": case "IceSlab": case "SnowDrift": case "IceShards": case "FrostGrass": case "MammothBones": case "RibCage": case "BeastSkull": case "BonePile": case "IceHole": return;   // walk through / ground decor
            case "Igloo": case "SnowRock": case "IceBoulder":
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                return;
            case "IcePine": Capsule(go, 0.45f, 2.4f); return;
            case "IceSpire": Capsule(go, 0.55f, 3.4f); return;
            case "Snowman": Capsule(go, 0.50f, 2.3f); return;
            case "LampPost": Capsule(go, 0.24f, 3.3f); return;
            case "Flag": Capsule(go, 0.16f, 3.3f); return;
            case "IceShardCluster": { var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, b.center.y, 0f); bc.size = new Vector3(Mathf.Min(b.size.x * 0.75f, 1.4f), b.size.y, Mathf.Min(b.size.z * 0.75f, 1.4f)); return; }
            case "IceCrystals": { var bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = Vector3.Scale(b.size, new Vector3(0.8f, 1f, 0.8f)); return; }
            default: { var bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size; return; }
        }
    }

    /// <summary>The ice floor: a 100 x 100 m plane whose texture repeats every 10 m, with a collider (replaces Floor1).</summary>
    static bool MakeFloor(Material mat)
    {
        const float size = 100f, tile = 10f;
        string meshPath = Root + "/IceFloorMesh.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null) { mesh = new Mesh { name = "IceFloor" }; AssetDatabase.CreateAsset(mesh, meshPath); }
        mesh.Clear();
        int n = 10; var v = new Vector3[(n + 1) * (n + 1)]; var uv = new Vector2[v.Length]; var nr = new Vector3[v.Length]; var tri = new int[n * n * 6];
        for (int j = 0; j <= n; j++) for (int i = 0; i <= n; i++)
        {
            float x = -size / 2 + size * i / n, z = -size / 2 + size * j / n; int k = j * (n + 1) + i;
            v[k] = new Vector3(x, 0, z); uv[k] = new Vector2(x / tile, z / tile); nr[k] = Vector3.up;
        }
        int t = 0;
        for (int j = 0; j < n; j++) for (int i = 0; i < n; i++)
        {
            int a = j * (n + 1) + i, b = a + 1, c = a + n + 1, d = c + 1;
            tri[t++] = a; tri[t++] = c; tri[t++] = b; tri[t++] = b; tri[t++] = c; tri[t++] = d;
        }
        mesh.vertices = v; mesh.uv = uv; mesh.normals = nr; mesh.triangles = tri; mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        var go = new GameObject("IceFloor");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/IceFloor.prefab");
        Object.DestroyImmediate(go);
        return true;
    }

    static void Capsule(GameObject go, float r, float h)
    {
        var c = go.AddComponent<CapsuleCollider>(); c.radius = r; c.height = h; c.direction = 1; c.center = new Vector3(0, h / 2f, 0);
    }
}
