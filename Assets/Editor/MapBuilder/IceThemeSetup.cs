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
    public const string GlowMat = Root + "/XmasGlow.mat";
    public const string SnowGroundMat = Root + "/SnowGround.mat";
    public const string SandGroundMat = Root + "/SandGround.mat";   // Egypt floor
    // Xmas night: FlatKit ignores the light (_LightContribution 0), so night is done with darker material colours, a warm
    // palette for the glowing slot (bulbs, lamp glass, star) and additive warm light pools on the snow
    public const string PropsNightMat = Root + "/IcePropsNight.mat";
    public const string SnowNightMat = Root + "/SnowGroundNight.mat";
    public const string GlowNightMat = Root + "/XmasGlowNight.mat";
    public const string WarmPalette = Root + "/IcePaletteWarm.png";
    public const string PoolTex = Root + "/WarmPool.png";
    public const string PoolMat = Root + "/WarmPool.mat";

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
        foreach (var groundTex in new[] { Root + "/IceGround.png", Root + "/SnowGround.png", Root + "/SandGround.png" })   // (Volcano textures: VolcanoThemeSetup)   // Xmas: snow floor
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
        // Xmas: faces exported in a second material slot (C9 bulbs, lamp glass, the star) are drawn unlit = they glow
        var glowShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        var glowMat = new Material(glowShader);
        foreach (var p in new[] { "_BaseMap", "_MainTex" }) if (glowMat.HasProperty(p)) glowMat.SetTexture(p, tex);
        if (glowMat.HasProperty("_BaseColor")) glowMat.SetColor("_BaseColor", Color.white);
        var glow = Save(GlowMat, glowMat);
        var volcanoGlow = VolcanoThemeSetup.Glow(tex);   // Volcano props: lava glow (unlit, two-sided, shimmering)
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
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mf = r.GetComponent<MeshFilter>();
                int subs = mf && mf.sharedMesh ? mf.sharedMesh.subMeshCount : r.sharedMaterials.Length;
                var g1 = volcanoGlow != null && VolcanoThemeSetup.IsVolcano(name) ? volcanoGlow : glow;
                r.sharedMaterials = Enumerable.Range(0, Mathf.Max(1, subs)).Select(i => i == 1 ? g1 : props).ToArray();
            }
            AddCollider(inst, name);
            PrefabUtility.SaveAsPrefabAsset(inst, PrefabDir + "/" + prefabName + ".prefab");
            Object.DestroyImmediate(inst);
            made++;
        }
        if (MakeFloor(groundMat, "IceFloor")) made++;
        var stex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/SnowGround.png");
        Material snowMat = null;
        if (stex != null) { snowMat = MakeMat(SnowGroundMat, reference, stex, Color.white); if (MakeFloor(snowMat, "SnowFloor")) made++; }
        MakeNightAssets(props, snowMat, tex, glow);
        var sandTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/SandGround.png");
        if (sandTex != null && MakeFloor(MakeMat(SandGroundMat, reference, sandTex, Color.white), "SandFloor")) made++;
        made += VolcanoThemeSetup.Setup(reference);   // basalt floor, lava lake, eruption vent
        AssetDatabase.SaveAssets();
        return made + " prefabs in " + PrefabDir + " (material " + (reference ? reference.shader.name : "Standard") + ")";
    }

    /// <summary>Writes only when the bytes differ (Unity keeps imported textures mapped: rewriting an unchanged file fails
    /// with IO error 1224), retrying a few times; never throws.</summary>
    static bool WriteIfChanged(string path, byte[] bytes)
    {
        if (File.Exists(path))
        {
            try { if (File.ReadAllBytes(path).SequenceEqual(bytes)) return false; } catch (IOException) { }
        }
        for (int i = 0; i < 5; i++)
        {
            try { File.WriteAllBytes(path, bytes); return true; }
            catch (IOException) { System.Threading.Thread.Sleep(300); }
        }
        Debug.LogWarning("[IceTheme] could not write " + path + " (locked): kept the old file");
        return false;
    }

    static void NightTint(Material m)
    {
        if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(0.46f, 0.50f, 0.74f));
        if (m.HasProperty("_ColorDim")) m.SetColor("_ColorDim", new Color(0.22f, 0.24f, 0.44f));
        if (m.HasProperty("_FlatRimColor")) m.SetColor("_FlatRimColor", new Color(0.30f, 0.30f, 0.46f));
        EditorUtility.SetDirty(m);
    }

    static void MakeNightAssets(Material props, Material snow, Texture2D pal, Material glow)
    {
        NightTint(Save(PropsNightMat, new Material(props)));
        if (snow != null) NightTint(Save(SnowNightMat, new Material(snow)));
        // warm palette: every colour pulled towards amber, so the glowing faces read as warm incandescent light
        if (pal != null)
        {
            var src = new Texture2D(2, 2); src.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(pal)));
            var px = src.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i]; float l = 0.3f * c.r + 0.59f * c.g + 0.11f * c.b;
                var warm = new Color(1.0f, 0.80f, 0.42f) * (0.62f + 0.55f * l);
                px[i] = Color.Lerp(c, new Color(Mathf.Min(1, warm.r), Mathf.Min(1, warm.g), Mathf.Min(1, warm.b)), 0.75f);
            }
            src.SetPixels(px); if (WriteIfChanged(WarmPalette, src.EncodeToPNG())) AssetDatabase.ImportAsset(WarmPalette);
            if (AssetImporter.GetAtPath(WarmPalette) is TextureImporter wi)
            {
                wi.textureType = TextureImporterType.Default; wi.filterMode = FilterMode.Bilinear; wi.mipmapEnabled = false;
                wi.wrapMode = TextureWrapMode.Clamp; wi.textureCompression = TextureImporterCompression.Uncompressed; wi.sRGBTexture = true; wi.SaveAndReimport();
            }
            var gn = new Material(glow.shader); gn.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WarmPalette);
            Save(GlowNightMat, gn);
        }
        // light pool: a soft round additive spot, warm
        const int N = 128; var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float dx = (x + 0.5f) / N * 2 - 1, dy = (y + 0.5f) / N * 2 - 1, r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(1 - r); a = a * a * (3 - 2 * a);
            t.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        if (WriteIfChanged(PoolTex, t.EncodeToPNG())) AssetDatabase.ImportAsset(PoolTex);
        if (AssetImporter.GetAtPath(PoolTex) is TextureImporter pi) { pi.alphaIsTransparency = true; pi.wrapMode = TextureWrapMode.Clamp; pi.mipmapEnabled = true; pi.SaveAndReimport(); }
        var pm = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        pm.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PoolTex);
        if (pm.HasProperty("_TintColor")) pm.SetColor("_TintColor", new Color(1.0f, 0.56f, 0.14f, 0.55f));
        var poolMat = Save(PoolMat, pm);
        var root = new GameObject("WarmPool");
        var go = new GameObject("Spot");
        go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = poolMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        go.transform.SetParent(root.transform, false);
        go.transform.localRotation = Quaternion.Euler(90, 0, 0);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/WarmPool.prefab");
        Object.DestroyImmediate(root);
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

    internal static Material MakeMat(string path, Material reference, Texture tex, Color tint)
    {
        var m = reference != null ? new Material(reference) : new Material(Shader.Find("Standard"));
        foreach (var p in new[] { "_BaseMap", "_MainTex" }) if (m.HasProperty(p)) { m.SetTexture(p, tex); m.SetTextureScale(p, Vector2.one); m.SetTextureOffset(p, Vector2.zero); }
        foreach (var p in new[] { "_BaseColor", "_Color" }) if (m.HasProperty(p)) m.SetColor(p, tint);
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
        m.DisableKeyword("_EMISSION");
        return Save(path, m);
    }

    internal static Material Save(string path, Material m)
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
        if (name.StartsWith("GlacierBlock") || name.StartsWith("IceChunk") || name == "FrozenLog" || name.StartsWith("GiftStack") || name == "GiftBox" || name == "Sleigh"
            || name.StartsWith("SandBlock") || name == "DigCrates" || name == "Amphorae" || name == "Sarcophagus" || name == "RuinLow"
            || name.StartsWith("BasaltBlock") || name == "BasaltLow" || name == "LavaRock" || name == "ObsidianChunk" || name == "SurveyCrates"
            || name.StartsWith("BasaltBoulders") || name.StartsWith("BasaltSlabs") || name == "ObsidianWall" || name.StartsWith("TuffRock")
            || name == "LavaBombs" || name == "MineCart")
        {
            // snow drips hang over the edge: keep the collider inside its 1.5 m tile so it never blocks the next one
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, b.center.y, 0f);
            bc.size = new Vector3(Mathf.Min(b.size.x, 1.45f), b.size.y, Mathf.Min(b.size.z, 1.45f));
            return;
        }
        switch (name)
        {
            case "FrostBush": case "SnowTuft": case "SnowMound": case "IceSlab": case "SnowDrift": case "IceShards": case "FrostGrass": case "MammothBones": case "RibCage": case "BeastSkull": case "BonePile": case "IceHole": case "PineGrass": case "BigBauble": case "PapyrusGrass": case "BrokenColumn": case "Pyramid": case "DigTent":
            case "LavaCrack": case "PumiceRocks": case "Volcano": case "LavaVent":
            case "ScorchGrassA": case "ScorchGrassB": case "EmberTuft": case "CooledFlow": return;   // walk through / ground decor
            case "Igloo": case "SnowRock": case "IceBoulder":
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                return;
            case "IcePine": Capsule(go, 0.45f, 2.4f); return;
            case "IceSpire": Capsule(go, 0.55f, 3.4f); return;
            case "XmasTree": Capsule(go, 0.65f, 3.2f); return;
            case "SandCliffA": case "SandCliffB": case "SandCliffC":
                {   // the rock body only: the rubble and sand in front (+Z, ~1.3 m) must not push the border into the arena
                    var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(b.center.x, b.center.y, b.center.z - 0.65f);
                    bc.size = new Vector3(b.size.x, b.size.y, b.size.z - 1.3f); return;
                }
            case "Obelisk": Capsule(go, 0.50f, 3.5f); return;
            case "BasaltRimA": case "BasaltRimB": case "BasaltRimC":
                {   // the column body only (not the toppled columns and cracks in front, ~1 m), and tall enough that the
                    // border stays a wall above the low basalt: the lava lake behind it is out of bounds
                    var bc = go.AddComponent<BoxCollider>(); float h = Mathf.Max(b.size.y, 6f);
                    bc.center = new Vector3(b.center.x, h / 2f, b.center.z - 0.5f); bc.size = new Vector3(b.size.x, h, b.size.z - 1.0f); return;
                }
            case "ObsidianSpire": Capsule(go, 0.50f, 3.4f); return;
            case "FireCrystal": Capsule(go, 0.55f, 2.5f); return;
            case "CharredTree": Capsule(go, 0.30f, 2.6f); return;
            case "MagmaPillar": Capsule(go, 0.55f, 2.7f); return;
            case "BurntPine": Capsule(go, 0.30f, 3.0f); return;
            case "IceGate": case "XmasGate":
            case "TempleGate": case "VolcanoGate": { var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, 4.5f, 0f); bc.size = new Vector3(11f, 9f, 2.4f); return; }   // the pylons only, not the sand in front
            case "PalmTree": Capsule(go, 0.25f, 3.2f); return;
            case "CatStatue": Capsule(go, 0.55f, 2.3f); return;
            case "LotusColumn": Capsule(go, 0.50f, 3.0f); return;
            case "XmasLamp": Capsule(go, 0.26f, 3.3f); return;
            case "CandyCane": Capsule(go, 0.30f, 2.2f); return;
            case "Snowman2": Capsule(go, 0.55f, 2.3f); return;
            case "Snowman": Capsule(go, 0.50f, 2.3f); return;
            case "LampPost": Capsule(go, 0.24f, 3.3f); return;
            case "Flag": Capsule(go, 0.16f, 3.3f); return;
            case "IceShardCluster": { var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, b.center.y, 0f); bc.size = new Vector3(Mathf.Min(b.size.x * 0.75f, 1.4f), b.size.y, Mathf.Min(b.size.z * 0.75f, 1.4f)); return; }
            case "IceCrystals": { var bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = Vector3.Scale(b.size, new Vector3(0.8f, 1f, 0.8f)); return; }
            default: { var bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size; return; }
        }
    }

    /// <summary>The ice floor: a 100 x 100 m plane whose texture repeats every 10 m, with a collider (replaces Floor1).</summary>
    internal static bool MakeFloor(Material mat, string name)
    {
        const float size = 100f, tile = 10f;
        string meshPath = Root + "/IceFloorMesh.asset";   // one shared 100 x 100 m mesh
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
        var go = new GameObject(name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
        return true;
    }

    static void Capsule(GameObject go, float r, float h)
    {
        var c = go.AddComponent<CapsuleCollider>(); c.radius = r; c.height = h; c.direction = 1; c.center = new Vector3(0, h / 2f, 0);
    }
}
