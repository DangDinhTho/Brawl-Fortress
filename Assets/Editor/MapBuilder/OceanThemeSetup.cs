using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ocean theme assets (run as part of Tools/Maps/Ice Theme/Setup Assets, same folder and palette as the other themes):
/// the glow of the ocean props (BrawlFortress/LavaGlow: unlit, breathing, cyan-white when hot), the underwater props
/// material (a little bluer, a deep teal shade), the SeabedFloor, the Caustics quad (moving light web on the sand), the
/// OceanCurrent lane prefab (BrawlFortress/Current + Assets/Scripts/Map/OceanCurrent.cs) and the sky.
/// Textures: Tools/IceArt/ocean_tex.py; props: Tools/IceArt/props19.py.
/// </summary>
public static class OceanThemeSetup
{
    const string Root = IceThemeSetup.Root;
    const string PrefabDir = IceThemeSetup.PrefabDir;
    public const string GlowMat = Root + "/OceanGlow.mat";
    public const string PropsMat = Root + "/IcePropsOcean.mat";
    public const string GroundMat = Root + "/SeabedGround.mat";
    public const string CausticsMat = Root + "/Caustics.mat";
    public const string CurrentMat = Root + "/OceanCurrent.mat";
    public const string SkyMat = Root + "/OceanSky.mat";

    /// <summary>The props of this theme: their second material slot is the ocean glow.</summary>
    public static readonly string[] Props =
    {
        "CoralReefA", "CoralReefB", "AtlantisWall", "ShipHull", "BarnacleRock", "GiantClam", "TreasureChest", "AnchorRock",
        "DivingHelmet", "CannonWreck", "CoralRidge", "AtlantisColumn", "SeaFan", "TridentStatue", "GlowCrystal",
        "BrokenMast", "KelpGrass", "Starfish", "SeaShells", "SeaUrchins", "CoralBits", "ReefWallA", "ReefWallB",
        "ReefWallC", "AtlantisGate", "AtlantisTemple", "SunkenShip", "GiantScallop",
    };
    public static bool IsOcean(string name) => Props.Contains(name);

    public static Material Glow(Texture2D palette)
    {
        var sh = Shader.Find("BrawlFortress/LavaGlow");
        if (sh == null) return null;
        var m = new Material(sh); m.mainTexture = palette;
        m.SetFloat("_Flicker", 0.10f); m.SetFloat("_Boost", 1.05f); m.SetColor("_Hot", new Color(0.85f, 1f, 1f));
        return IceThemeSetup.Save(GlowMat, m);
    }

    static void Tint(Material m, Color lit, Color shade, Color rim)
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
        if (AssetImporter.GetAtPath(Root + "/SeabedGround.png") is TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat; ti.mipmapEnabled = true;
            ti.filterMode = FilterMode.Bilinear; ti.anisoLevel = 4; ti.textureCompression = TextureImporterCompression.Compressed; ti.sRGBTexture = true;
            ti.SaveAndReimport();
        }
        // under water: colours a little cooler, a deep teal shade (corals keep their colour)
        if (props != null) Tint(IceThemeSetup.Save(PropsMat, new Material(props)), new Color(0.92f, 0.98f, 1.02f), new Color(0.46f, 0.60f, 0.70f), new Color(0.40f, 0.62f, 0.70f));
        var gtex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/SeabedGround.png");
        if (gtex != null)
        {
            var g = IceThemeSetup.MakeMat(GroundMat, reference, gtex, Color.white);
            Tint(g, new Color(0.92f, 0.98f, 1.0f), new Color(0.58f, 0.68f, 0.74f), new Color(0.40f, 0.62f, 0.70f));
            if (IceThemeSetup.MakeFloor(g, "SeabedFloor")) made++;
        }
        var cSh = Shader.Find("BrawlFortress/Caustics");
        if (cSh != null)
        {
            var cm = IceThemeSetup.Save(CausticsMat, new Material(cSh));
            var go = new GameObject("Caustics");
            var q = new GameObject("Light");
            q.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var r = q.AddComponent<MeshRenderer>(); r.sharedMaterial = cm;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            q.transform.SetParent(go.transform, false); q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/Caustics.prefab");
            Object.DestroyImmediate(go); made++;
        }
        var lSh = Shader.Find("BrawlFortress/Current");
        if (lSh != null)
        {
            var lm = IceThemeSetup.Save(CurrentMat, new Material(lSh));
            var go = new GameObject("OceanCurrent");
            go.AddComponent<OceanCurrent>();
            var q = new GameObject("Flow");
            q.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var r = q.AddComponent<MeshRenderer>(); r.sharedMaterial = lm;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            q.transform.SetParent(go.transform, false); q.transform.localPosition = new Vector3(0, 0.04f, 0); q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/OceanCurrent.prefab");
            Object.DestroyImmediate(go); made++;
        }
        var skySh = Shader.Find("BrawlFortress/SkyGradient");
        if (skySh != null)
        {   // under the sea: light from above, deep blue-green all round
            var sky = new Material(skySh);
            sky.SetColor("_Top", new Color(0.30f, 0.62f, 0.78f)); sky.SetColor("_Horizon", new Color(0.12f, 0.36f, 0.50f));
            sky.SetColor("_Bottom", new Color(0.06f, 0.20f, 0.30f)); sky.SetFloat("_Exponent", 0.8f);
            IceThemeSetup.Save(SkyMat, sky);
        }
        return made;
    }
}
