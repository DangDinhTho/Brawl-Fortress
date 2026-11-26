using UnityEditor;
using UnityEngine;

/// <summary>
/// Greek theme assets (run as part of Tools/Maps/Ice Theme/Setup Assets, same folder and palette as the other themes):
/// the GreekFloor (sun-baked earth and worn marble flagstones, in the bright neutral desert light), the sea beyond the
/// cliffs (BrawlFortress/Lava with the SeaFlow texture on a flat quad, calm and slow) and the warm Mediterranean sky.
/// Textures: Tools/IceArt/greek_tex.py.
/// </summary>
public static class GreekThemeSetup
{
    const string Root = IceThemeSetup.Root;
    const string PrefabDir = IceThemeSetup.PrefabDir;
    public const string GroundMat = Root + "/GreekGround.mat";
    public const string SeaMat = Root + "/SeaWater.mat";
    public const string SkyMat = Root + "/GreekSky.mat";
    public const string PropsMat = Root + "/IcePropsGreek.mat";

    /// <summary>After the prefabs exist. Returns how many prefabs it made.</summary>
    public static int Setup(Material reference, Material props)
    {
        int made = 0;
        if (props != null)
        {   // softer than the desert light: the marble and the whitewash are near-white palette cells and glared
            var m = IceThemeSetup.Save(PropsMat, new Material(props));
            if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(0.98f, 0.97f, 0.94f));
            if (m.HasProperty("_ColorDim")) m.SetColor("_ColorDim", new Color(0.68f, 0.66f, 0.63f));
            if (m.HasProperty("_FlatRimColor")) m.SetColor("_FlatRimColor", new Color(0.58f, 0.56f, 0.53f));
            EditorUtility.SetDirty(m);
        }
        foreach (var p in new[] { Root + "/GreekGround.png", Root + "/SeaFlow.png" })
            if (AssetImporter.GetAtPath(p) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat; ti.mipmapEnabled = true;
                ti.filterMode = FilterMode.Bilinear; ti.anisoLevel = 4; ti.textureCompression = TextureImporterCompression.Compressed; ti.sRGBTexture = true;
                ti.SaveAndReimport();
            }
        var groundTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/GreekGround.png");
        if (groundTex != null)
        {
            var g = IceThemeSetup.MakeMat(GroundMat, reference, groundTex, Color.white);
            if (g.HasProperty("_Color")) g.SetColor("_Color", new Color(0.98f, 0.97f, 0.94f));
            if (g.HasProperty("_ColorDim")) g.SetColor("_ColorDim", new Color(0.72f, 0.70f, 0.66f));
            if (g.HasProperty("_FlatRimColor")) g.SetColor("_FlatRimColor", new Color(0.62f, 0.60f, 0.56f));
            EditorUtility.SetDirty(g);
            if (IceThemeSetup.MakeFloor(g, "GreekFloor")) made++;
        }
        var lavaSh = Shader.Find("BrawlFortress/Lava");
        var seaTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/SeaFlow.png");
        if (lavaSh != null && seaTex != null)
        {
            var sea = new Material(lavaSh); sea.mainTexture = seaTex;
            sea.SetFloat("_WorldUV", 1); sea.SetFloat("_Tiling", 0.07f); sea.SetFloat("_Pulse", 0.05f); sea.SetFloat("_Boost", 0.9f);
            sea.SetVector("_Scroll1", new Vector4(0.010f, 0.004f, 0, 0)); sea.SetVector("_Scroll2", new Vector4(-0.006f, 0.009f, 0, 0));
            sea.SetFloat("_HotMix", 0f);
            sea = IceThemeSetup.Save(SeaMat, sea);
            var go = new GameObject("SeaWater");
            var q = new GameObject("Surface");
            q.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var r = q.AddComponent<MeshRenderer>(); r.sharedMaterial = sea;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            q.transform.SetParent(go.transform, false); q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/SeaWater.prefab");
            Object.DestroyImmediate(go); made++;
        }
        else Debug.LogWarning("[Greek] SeaFlow.png or the BrawlFortress/Lava shader is missing");
        var skySh = Shader.Find("BrawlFortress/SkyGradient");
        if (skySh != null)
        {   // a clear Mediterranean sky: deep blue above, a warm glow at the horizon
            var sky = new Material(skySh);
            sky.SetColor("_Top", new Color(0.26f, 0.48f, 0.84f)); sky.SetColor("_Horizon", new Color(1.0f, 0.84f, 0.66f));
            sky.SetColor("_Bottom", new Color(0.40f, 0.60f, 0.78f)); sky.SetFloat("_Exponent", 0.6f);
            IceThemeSetup.Save(SkyMat, sky);
        }
        return made;
    }
}
