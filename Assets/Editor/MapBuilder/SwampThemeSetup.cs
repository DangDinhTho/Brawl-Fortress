using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Swamp theme assets (run as part of Tools/Maps/Ice Theme/Setup Assets, same folder and palette as the other themes):
/// the twilight materials (props and floor a little darker and greener: FlatKit ignores the scene light, see the Xmas
/// night), the glow of the swamp props (BrawlFortress/LavaGlow: unlit, breathing slowly), the SwampFloor, the acid lake
/// (BrawlFortress/Lava with the AcidFlow texture on a flat quad), the BogPuddle prefab (a flat wobbly disc of unit
/// radius, BrawlFortress/Bog, script Assets/Scripts/Map/BogPuddle.cs), a green additive light pool and the sky.
/// Textures: Tools/IceArt/swamp_tex.py.
/// </summary>
public static class SwampThemeSetup
{
    const string Root = IceThemeSetup.Root;
    const string PrefabDir = IceThemeSetup.PrefabDir;
    public const string PropsMat = Root + "/IcePropsSwamp.mat";
    public const string GroundMat = Root + "/SwampGround.mat";
    public const string GlowMat = Root + "/SwampGlow.mat";
    public const string AcidMat = Root + "/AcidLake.mat";
    public const string BogMat = Root + "/BogMud.mat";
    public const string PoolMat = Root + "/GlowPool.mat";
    public const string SkyMat = Root + "/SwampSky.mat";
    const string BogMesh = Root + "/BogPuddleMesh.asset";

    /// <summary>The props of this theme: their second material slot is the swamp glow, not the Xmas bulb glow.</summary>
    public static readonly string[] Props =
    {
        "MossBoulderA", "MossBoulderB", "ShroomCluster", "RootTangle", "LogStack", "Stump", "RowBoat", "RustyDrums",
        "MoonshineStill", "FallenLog", "GiantShroomA", "GiantShroomB", "SwampCypress", "SwampLantern", "BogGrass",
        "ShroomPatch", "MossClump", "Cattails", "SwampStones", "SwampBankA", "SwampBankB", "SwampBankC", "SwampGate",
        "BigCypress", "StiltShack",
    };
    public static bool IsSwamp(string name) => Props.Contains(name);

    public static Material Glow(Texture2D palette)
    {
        var sh = Shader.Find("BrawlFortress/LavaGlow");
        if (sh == null) return null;
        var m = new Material(sh); m.mainTexture = palette;
        m.SetFloat("_Flicker", 0.10f); m.SetFloat("_Boost", 1f); m.SetColor("_Hot", new Color(0.85f, 1f, 0.9f));
        return IceThemeSetup.Save(GlowMat, m);
    }

    static void Twilight(Material m, Color lit, Color shade, Color rim)
    {
        if (m.HasProperty("_Color")) m.SetColor("_Color", lit);
        if (m.HasProperty("_ColorDim")) m.SetColor("_ColorDim", shade);
        if (m.HasProperty("_FlatRimColor")) m.SetColor("_FlatRimColor", rim);
        EditorUtility.SetDirty(m);
    }

    /// <summary>After the prefabs exist. Returns how many prefabs it made.</summary>
    public static int Setup(Material reference, Material props)
    {
        int made = 0;
        foreach (var p in new[] { Root + "/SwampGround.png", Root + "/AcidFlow.png", Root + "/BogMud.png" })
            if (AssetImporter.GetAtPath(p) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat; ti.mipmapEnabled = true;
                ti.filterMode = FilterMode.Bilinear; ti.anisoLevel = 4; ti.textureCompression = TextureImporterCompression.Compressed; ti.sRGBTexture = true;
                ti.SaveAndReimport();
            }
        // twilight: props a little darker and greener, deep teal shade (the glowing mushrooms stand out)
        if (props != null) Twilight(IceThemeSetup.Save(PropsMat, new Material(props)), new Color(0.84f, 0.92f, 0.84f), new Color(0.34f, 0.44f, 0.46f), new Color(0.32f, 0.42f, 0.36f));
        var groundTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/SwampGround.png");
        if (groundTex != null)
        {
            var g = IceThemeSetup.MakeMat(GroundMat, reference, groundTex, Color.white);
            Twilight(g, new Color(1.0f, 1.04f, 0.98f), new Color(0.50f, 0.56f, 0.54f), new Color(0.32f, 0.42f, 0.36f));
            if (IceThemeSetup.MakeFloor(g, "SwampFloor")) made++;
        }

        var lavaSh = Shader.Find("BrawlFortress/Lava");
        var acidTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/AcidFlow.png");
        if (lavaSh != null && acidTex != null)
        {   // acid: the lava shader with the green texture, slower and calmer
            var acid = new Material(lavaSh); acid.mainTexture = acidTex;
            acid.SetFloat("_WorldUV", 1); acid.SetFloat("_Tiling", 0.10f); acid.SetFloat("_Pulse", 0.12f); acid.SetFloat("_Boost", 0.8f);
            acid.SetVector("_Scroll1", new Vector4(0.006f, 0.004f, 0, 0)); acid.SetVector("_Scroll2", new Vector4(-0.005f, 0.007f, 0, 0));
            acid.SetColor("_HotColor", new Color(0.6f, 1f, 0.3f)); acid.SetFloat("_HotMix", 0f);
            acid = IceThemeSetup.Save(AcidMat, acid);
            var lakeGo = new GameObject("AcidLake");
            var q = new GameObject("Surface");
            q.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var qr = q.AddComponent<MeshRenderer>(); qr.sharedMaterial = acid;
            qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; qr.receiveShadows = false;
            q.transform.SetParent(lakeGo.transform, false); q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            PrefabUtility.SaveAsPrefabAsset(lakeGo, PrefabDir + "/AcidLake.prefab");
            Object.DestroyImmediate(lakeGo); made++;
        }
        else Debug.LogWarning("[Swamp] AcidFlow.png or the BrawlFortress/Lava shader is missing");

        var bogSh = Shader.Find("BrawlFortress/Bog");
        var bogTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/BogMud.png");
        if (bogSh != null && bogTex != null)
        {
            var bog = new Material(bogSh); bog.mainTexture = bogTex;
            bog.SetColor("_Tint", new Color(1.1f, 1.15f, 1.0f)); bog.SetColor("_RimColor", new Color(0.62f, 0.52f, 0.30f)); bog.SetFloat("_RimWidth", 0.2f);
            bog.SetColor("_BubbleColor", new Color(0.82f, 0.88f, 0.48f)); bog.SetFloat("_Bubbles", 1.5f); bog.SetFloat("_Sheen", 0.5f);
            bog = IceThemeSetup.Save(BogMat, bog);
            var go = new GameObject("BogPuddle");
            go.AddComponent<BogPuddle>();
            var s = new GameObject("Mud");
            s.AddComponent<MeshFilter>().sharedMesh = PuddleMesh();
            var r = s.AddComponent<MeshRenderer>(); r.sharedMaterial = bog;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s.transform.SetParent(go.transform, false); s.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/BogPuddle.prefab");
            Object.DestroyImmediate(go); made++;
        }
        else Debug.LogWarning("[Swamp] BogMud.png or the BrawlFortress/Bog shader is missing");

        // a soft green light pool under the big glowing mushrooms (the WarmPool texture, tinted)
        var poolTex = AssetDatabase.LoadAssetAtPath<Texture2D>(IceThemeSetup.PoolTex);
        if (poolTex != null)
        {
            var pm = new Material(Shader.Find("Legacy Shaders/Particles/Additive")); pm.mainTexture = poolTex;
            if (pm.HasProperty("_TintColor")) pm.SetColor("_TintColor", new Color(0.30f, 0.75f, 0.85f, 0.32f));
            pm = IceThemeSetup.Save(PoolMat, pm);
            var root = new GameObject("GlowPool");
            var spot = new GameObject("Spot");
            spot.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var mr = spot.AddComponent<MeshRenderer>(); mr.sharedMaterial = pm;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            spot.transform.SetParent(root.transform, false); spot.transform.localRotation = Quaternion.Euler(90, 0, 0);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/GlowPool.prefab");
            Object.DestroyImmediate(root); made++;
        }

        var skySh = Shader.Find("BrawlFortress/SkyGradient");
        if (skySh != null)
        {   // dusk over the bog: deep blue-violet above, a sickly green glow at the horizon
            var sky = new Material(skySh);
            sky.SetColor("_Top", new Color(0.10f, 0.10f, 0.22f)); sky.SetColor("_Horizon", new Color(0.36f, 0.52f, 0.40f));
            sky.SetColor("_Bottom", new Color(0.16f, 0.24f, 0.20f)); sky.SetFloat("_Exponent", 0.8f);
            IceThemeSetup.Save(SkyMat, sky);
        }
        return made;
    }

    /// <summary>A flat disc of radius ~1 with a wobbly edge: vertex alpha 1 in the middle, 0 on the rim (the Bog shader
    /// draws the wet edge band from it). Written into the existing asset with the Mesh API.</summary>
    static Mesh PuddleMesh()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(BogMesh);
        if (mesh == null) { mesh = new Mesh { name = "BogPuddle" }; AssetDatabase.CreateAsset(mesh, BogMesh); }
        mesh.Clear();
        const int S = 40;
        float[] rings = { 0.35f, 0.62f, 0.82f, 0.93f, 1.0f };
        float[] alpha = { 0.85f, 0.55f, 0.28f, 0.12f, 0.0f };
        var verts = new List<Vector3> { Vector3.zero }; var cols = new List<Color> { new Color(1, 1, 1, 1) };
        var tris = new List<int>();
        for (int k = 0; k < rings.Length; k++)
            for (int j = 0; j < S; j++)
            {
                float a = j * Mathf.PI * 2f / S;
                float wob = 1f + 0.06f * Mathf.Sin(a * 3f + 0.7f) + 0.04f * Mathf.Sin(a * 5f + 2.1f) + 0.025f * Mathf.Sin(a * 9f);
                float r = rings[k] * (k == 0 ? 1f : wob);
                verts.Add(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                cols.Add(new Color(1, 1, 1, alpha[k]));
            }
        for (int j = 0; j < S; j++) { int a = 1 + j, b = 1 + (j + 1) % S; tris.AddRange(new[] { 0, b, a }); }
        for (int k = 0; k < rings.Length - 1; k++)
            for (int j = 0; j < S; j++)
            {
                int a = 1 + k * S + j, b = 1 + k * S + (j + 1) % S, c = a + S, d = b + S;
                tris.AddRange(new[] { a, b, d, a, d, c });
            }
        mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }
}
