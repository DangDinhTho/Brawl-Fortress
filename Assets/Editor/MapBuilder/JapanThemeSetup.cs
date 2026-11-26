using UnityEditor;
using UnityEngine;

/// <summary>
/// Japan theme assets (run as part of Tools/Maps/Ice Theme/Setup Assets, same folder and palette as the other themes):
/// the SakuraPink material (slot 1 of the cherry blossom props: the palette has no pink, so the blossom faces sit on the
/// sand ramp in slot 1 and this lit material tints them), the JapanFloor (the gravel of a shrine yard), the ToriiPath
/// prefab (Assets/Scripts/Map/ToriiPath.cs: the speed box; the torii and slabs are placed by the builder) and the sky.
/// Textures: Tools/IceArt/japan_tex.py; props: Tools/IceArt/props21.py.
/// </summary>
public static class JapanThemeSetup
{
    const string Root = IceThemeSetup.Root;
    const string PrefabDir = IceThemeSetup.PrefabDir;
    public const string PinkMat = Root + "/SakuraPink.mat";
    public const string GroundMat = Root + "/ShrineGround.mat";
    public const string SkyMat = Root + "/JapanSky.mat";

    /// <summary>The props whose material slot 1 is the cherry blossom pink, not a glow.</summary>
    public static readonly string[] PinkProps = { "SakuraTree", "PetalScatter", "ShrineWallB" };
    public static bool IsPink(string name) => System.Array.IndexOf(PinkProps, name) >= 0;

    /// <summary>The cherry blossom: the lit props material tinted pink (the sand ramp under it: deeper pink underneath,
    /// near white on top), for the soft Greek daylight the Japan maps use.</summary>
    public static Material Pink(Material props)
    {
        if (props == null) return null;
        var m = IceThemeSetup.Save(PinkMat, new Material(props));
        if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(1.02f, 0.74f, 1.10f));
        if (m.HasProperty("_ColorDim")) m.SetColor("_ColorDim", new Color(0.80f, 0.56f, 0.78f));
        if (m.HasProperty("_FlatRimColor")) m.SetColor("_FlatRimColor", new Color(0.70f, 0.52f, 0.60f));
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>After the prefabs exist. Returns how many prefabs it made.</summary>
    public static int Setup(Material reference)
    {
        int made = 0;
        if (AssetImporter.GetAtPath(Root + "/ShrineGround.png") is TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat; ti.mipmapEnabled = true;
            ti.filterMode = FilterMode.Bilinear; ti.anisoLevel = 4; ti.textureCompression = TextureImporterCompression.Compressed; ti.sRGBTexture = true;
            ti.SaveAndReimport();
        }
        var gtex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/ShrineGround.png");
        if (gtex != null)
        {
            var g = IceThemeSetup.MakeMat(GroundMat, reference, gtex, Color.white);
            if (g.HasProperty("_Color")) g.SetColor("_Color", new Color(0.98f, 0.97f, 0.95f));
            if (g.HasProperty("_ColorDim")) g.SetColor("_ColorDim", new Color(0.74f, 0.72f, 0.70f));
            EditorUtility.SetDirty(g);
            if (IceThemeSetup.MakeFloor(g, "JapanFloor")) made++;
        }
        {   // the speed box of a torii path (no renderer: the torii and the stone slabs are separate props)
            var go = new GameObject("ToriiPath");
            go.AddComponent<ToriiPath>();
            PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/ToriiPath.prefab");
            Object.DestroyImmediate(go); made++;
        }
        var skySh = Shader.Find("BrawlFortress/SkyGradient");
        if (skySh != null)
        {   // a spring sky: soft blue above, a hazy pale pink-gold at the horizon
            var sky = new Material(skySh);
            sky.SetColor("_Top", new Color(0.42f, 0.64f, 0.90f)); sky.SetColor("_Horizon", new Color(0.97f, 0.88f, 0.86f));
            sky.SetColor("_Bottom", new Color(0.86f, 0.80f, 0.76f)); sky.SetFloat("_Exponent", 0.75f);
            IceThemeSetup.Save(SkyMat, sky);
        }
        return made;
    }
}
