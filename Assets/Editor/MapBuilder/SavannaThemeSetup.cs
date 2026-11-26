using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Savanna theme assets (run as part of Tools/Maps/Ice Theme/Setup Assets, same folder and palette as the other themes):
/// the SavannaFloor (red ochre earth in the bright neutral daylight of the desert materials), the WaterHole prefab
/// (BrawlFortress/Pond on a flat disc + Assets/Scripts/Map/WaterHole.cs) and the sky. The props use the shared palette
/// material (swapped for the desert one by EgyptSun) and the warm glow (lantern glass).
/// Textures: Tools/IceArt/savanna_tex.py; props: Tools/IceArt/props20.py.
/// </summary>
public static class SavannaThemeSetup
{
    const string Root = IceThemeSetup.Root;
    const string PrefabDir = IceThemeSetup.PrefabDir;
    public const string GroundMat = Root + "/SavannaGround.mat";
    public const string PondMat = Root + "/Pond.mat";
    public const string SkyMat = Root + "/SavannaSky.mat";
    public const string BarkMat = Root + "/BaobabBark.mat";
    const string PondMesh = Root + "/WaterHoleMesh.asset";

    /// <summary>The props whose material slot 1 is the baobab bark (Tools/IceArt/props20.py: the palette has no
    /// pinkish brown, so the bark faces sit on the sand ramp in slot 1 and this material tints them), not a glow.</summary>
    public static readonly string[] BarkProps = { "BaobabTree", "BaobabGiant", "SavannaRidgeB" };
    public static bool IsBaobab(string name) => System.Array.IndexOf(BarkProps, name) >= 0;

    /// <summary>The baobab bark: the lit props material tinted pinkish brown (the sand ramp under it gives a darker foot
    /// and a lighter top), in the same bright desert daylight as IcePropsDesert.</summary>
    public static Material Bark(Material props)
    {
        if (props == null) return null;
        var m = IceThemeSetup.Save(BarkMat, new Material(props));
        // brown with a hint of pink (1.02 / 0.92 / 1.24 read as candy pink in the game)
        if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(0.80f, 0.69f, 0.88f));
        if (m.HasProperty("_ColorDim")) m.SetColor("_ColorDim", new Color(0.56f, 0.46f, 0.58f));
        if (m.HasProperty("_FlatRimColor")) m.SetColor("_FlatRimColor", new Color(0.52f, 0.42f, 0.44f));
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Tint(Material m, Color lit, Color shade)
    {
        if (m.HasProperty("_Color")) m.SetColor("_Color", lit);
        if (m.HasProperty("_ColorDim")) m.SetColor("_ColorDim", shade);
        EditorUtility.SetDirty(m);
    }

    /// <summary>After the prefabs exist. Returns how many prefabs it made.</summary>
    public static int Setup(Material reference)
    {
        int made = 0;
        if (AssetImporter.GetAtPath(Root + "/SavannaGround.png") is TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat; ti.mipmapEnabled = true;
            ti.filterMode = FilterMode.Bilinear; ti.anisoLevel = 4; ti.textureCompression = TextureImporterCompression.Compressed; ti.sRGBTexture = true;
            ti.SaveAndReimport();
        }
        var gtex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/SavannaGround.png");
        if (gtex != null)
        {   // the same bright neutral daylight as the desert materials (Egypt, Factory, Circus)
            var g = IceThemeSetup.MakeMat(GroundMat, reference, gtex, Color.white);
            Tint(g, new Color(1.04f, 1.03f, 1.0f), new Color(0.76f, 0.73f, 0.70f));
            if (IceThemeSetup.MakeFloor(g, "SavannaFloor")) made++;
        }
        var pondSh = Shader.Find("BrawlFortress/Pond");
        if (pondSh != null)
        {
            var pm = IceThemeSetup.Save(PondMat, new Material(pondSh));
            var go = new GameObject("WaterHole");
            go.AddComponent<WaterHole>();
            var w = new GameObject("Water");
            w.AddComponent<MeshFilter>().sharedMesh = DiscMesh();
            var r = w.AddComponent<MeshRenderer>(); r.sharedMaterial = pm;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            w.transform.SetParent(go.transform, false); w.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/WaterHole.prefab");
            Object.DestroyImmediate(go); made++;
        }
        else Debug.LogWarning("[Savanna] the BrawlFortress/Pond shader is missing");
        var skySh = Shader.Find("BrawlFortress/SkyGradient");
        if (skySh != null)
        {   // a clear savanna sky: deep blue above, a pale warm haze at the horizon
            var sky = new Material(skySh);
            sky.SetColor("_Top", new Color(0.30f, 0.56f, 0.88f)); sky.SetColor("_Horizon", new Color(0.92f, 0.88f, 0.76f));
            sky.SetColor("_Bottom", new Color(0.82f, 0.70f, 0.52f)); sky.SetFloat("_Exponent", 0.7f);
            IceThemeSetup.Save(SkyMat, sky);
        }
        return made;
    }

    /// <summary>A flat disc of radius ~1 with a wobbly edge: vertex alpha 1 in the middle, 0 on the rim (the Pond shader
    /// draws the depth and the wet edge from it). Written into the existing asset with the Mesh API.</summary>
    static Mesh DiscMesh()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(PondMesh);
        if (mesh == null) { mesh = new Mesh { name = "WaterHole" }; AssetDatabase.CreateAsset(mesh, PondMesh); }
        mesh.Clear();
        const int S = 44;
        float[] rings = { 0.3f, 0.55f, 0.75f, 0.88f, 0.95f, 1.0f };
        float[] alpha = { 0.95f, 0.75f, 0.5f, 0.3f, 0.12f, 0.0f };
        var verts = new List<Vector3> { Vector3.zero }; var cols = new List<Color> { new Color(1, 1, 1, 1) };
        var tris = new List<int>();
        for (int k = 0; k < rings.Length; k++)
            for (int j = 0; j < S; j++)
            {
                float a = j * Mathf.PI * 2f / S;
                float wob = 1f + 0.07f * Mathf.Sin(a * 2f + 1.3f) + 0.05f * Mathf.Sin(a * 5f + 0.4f) + 0.025f * Mathf.Sin(a * 9f + 2.0f);
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
