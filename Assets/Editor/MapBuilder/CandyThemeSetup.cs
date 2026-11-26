using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Candy theme assets (run as part of Tools/Maps/Ice Theme/Setup Assets, same folder and palette as the other themes).
/// The 8 x 8 palette is full and has no pink: candy props put their pink faces in material slot 1, which here is
/// CandyPink.mat, the lit FlatKit props material tinted pink (white cells read pink, frost mauve, cream peach), not a
/// glow. Also the CandyFloor (mint icing, sprinkles, biscuit tiles), the chocolate lake (BrawlFortress/Lava with the
/// ChocoFlow texture) and the pastel sky. Textures: Tools/IceArt/candy_tex.py; props: Tools/IceArt/props18.py.
/// </summary>
public static class CandyThemeSetup
{
    const string Root = IceThemeSetup.Root;
    const string PrefabDir = IceThemeSetup.PrefabDir;
    public const string PinkMat = Root + "/CandyPink.mat";
    public const string GroundMat = Root + "/CandyGround.mat";
    public const string ChocoMat = Root + "/ChocoLake.mat";
    public const string SkyMat = Root + "/CandySky.mat";

    /// <summary>The props of this theme: their second material slot is the pink tint, not a glow.</summary>
    public static readonly string[] Props =
    {
        "WaferBlock", "CakeBlock", "ChocoBlock", "GumdropStack", "MarshmallowStack", "Cupcake", "GumballMachine",
        "WrappedSweets", "DonutStack", "StickFence", "LollipopA", "LollipopB", "IceCreamCone", "GumdropTree", "GummyBear",
        "SugarGrass", "Sprinkles", "WrappedCandy", "ChocoChips", "CandyWallA", "CandyWallB", "CandyWallC", "CandyGate",
        "CandyCastle", "GingerbreadHouse", "LayerCake",
    };
    public static bool IsCandy(string name) => Props.Contains(name);

    /// <summary>The pink slot: a copy of the lit props material, tinted (as bright as the Greek props material the
    /// candy maps use for slot 0). props18.py PINK_TINT is the look, for the Blender preview.</summary>
    public static Material Pink(Material props)
    {
        if (props == null) return null;
        var m = IceThemeSetup.Save(PinkMat, new Material(props));
        if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(0.92f, 0.48f, 0.68f));
        if (m.HasProperty("_ColorDim")) m.SetColor("_ColorDim", new Color(0.70f, 0.38f, 0.54f));
        if (m.HasProperty("_FlatRimColor")) m.SetColor("_FlatRimColor", new Color(0.60f, 0.46f, 0.54f));
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>After the prefabs exist. Returns how many prefabs it made.</summary>
    public static int Setup(Material reference)
    {
        int made = 0;
        foreach (var p in new[] { Root + "/CandyGround.png", Root + "/ChocoFlow.png" })
            if (AssetImporter.GetAtPath(p) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat; ti.mipmapEnabled = true;
                ti.filterMode = FilterMode.Bilinear; ti.anisoLevel = 4; ti.textureCompression = TextureImporterCompression.Compressed; ti.sRGBTexture = true;
                ti.SaveAndReimport();
            }
        var groundTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/CandyGround.png");
        if (groundTex != null)
        {
            var g = IceThemeSetup.MakeMat(GroundMat, reference, groundTex, Color.white);
            if (g.HasProperty("_Color")) g.SetColor("_Color", new Color(0.98f, 0.98f, 0.97f));
            if (g.HasProperty("_ColorDim")) g.SetColor("_ColorDim", new Color(0.74f, 0.74f, 0.74f));
            if (g.HasProperty("_FlatRimColor")) g.SetColor("_FlatRimColor", new Color(0.60f, 0.60f, 0.60f));
            EditorUtility.SetDirty(g);
            if (IceThemeSetup.MakeFloor(g, "CandyFloor")) made++;
        }
        var lavaSh = Shader.Find("BrawlFortress/Lava");
        var chocoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/ChocoFlow.png");
        if (lavaSh != null && chocoTex != null)
        {   // melted chocolate: the lava shader, slow and calm
            var choco = new Material(lavaSh); choco.mainTexture = chocoTex;
            choco.SetFloat("_WorldUV", 1); choco.SetFloat("_Tiling", 0.09f); choco.SetFloat("_Pulse", 0.05f); choco.SetFloat("_Boost", 1.0f);
            choco.SetVector("_Scroll1", new Vector4(0.007f, 0.003f, 0, 0)); choco.SetVector("_Scroll2", new Vector4(-0.004f, 0.006f, 0, 0));
            choco.SetFloat("_HotMix", 0f);
            choco = IceThemeSetup.Save(ChocoMat, choco);
            var go = new GameObject("ChocoLake");
            var q = new GameObject("Surface");
            q.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var r = q.AddComponent<MeshRenderer>(); r.sharedMaterial = choco;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            q.transform.SetParent(go.transform, false); q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/ChocoLake.prefab");
            Object.DestroyImmediate(go); made++;
        }
        else Debug.LogWarning("[Candy] ChocoFlow.png or the BrawlFortress/Lava shader is missing");
        var skySh = Shader.Find("BrawlFortress/SkyGradient");
        if (skySh != null)
        {   // a pastel sky: soft blue above, a pink glow at the horizon
            var sky = new Material(skySh);
            sky.SetColor("_Top", new Color(0.50f, 0.70f, 0.98f)); sky.SetColor("_Horizon", new Color(1.0f, 0.80f, 0.90f));
            sky.SetColor("_Bottom", new Color(0.85f, 0.70f, 0.82f)); sky.SetFloat("_Exponent", 0.7f);
            IceThemeSetup.Save(SkyMat, sky);
        }
        return made;
    }
}
