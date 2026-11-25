using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Volcano theme assets (run as part of Tools/Maps/Ice Theme/Setup Assets, same folder and palette as the other themes):
/// the glow material of the volcano props (BrawlFortress/LavaGlow: unlit, two-sided, shimmering), the BasaltFloor,
/// the lava lake (BrawlFortress/Lava on a flat quad, laid in world space), and the EruptionVent prefab (vent base,
/// warning disc, lava fountain, ember particles; script Assets/Scripts/Map/EruptionVent.cs).
/// </summary>
public static class VolcanoThemeSetup
{
    const string Root = IceThemeSetup.Root;
    const string PrefabDir = IceThemeSetup.PrefabDir;
    public const string GlowMat = Root + "/VolcanoGlow.mat";
    public const string GroundMat = Root + "/BasaltGround.mat";
    public const string LakeMat = Root + "/LavaLake.mat";
    public const string FountainMat = Root + "/LavaFountain.mat";
    public const string WarnMat = Root + "/VentWarn.mat";
    public const string EmberMat = Root + "/LavaEmbers.mat";
    public const string SkyMat = Root + "/VolcanoSky.mat";
    const string FountainMesh = Root + "/LavaFountainMesh.asset";

    /// <summary>The props of this theme: their second material slot is the lava glow, not the Xmas bulb glow.</summary>
    public static readonly string[] Props =
    {
        "BasaltBlockA", "BasaltBlockB", "BasaltBlockC", "BasaltLow", "ObsidianSpire", "FireCrystal", "CharredTree",
        "LavaRock", "ObsidianChunk", "SurveyCrates", "LavaCrack", "PumiceRocks",
        "BasaltBouldersA", "BasaltBouldersB", "BasaltSlabsA", "BasaltSlabsB", "ObsidianWall", "TuffRockA", "TuffRockB",
        "MagmaPillar", "BurntPine", "LavaBombs", "MineCart", "CooledFlow", "ScorchGrassA", "ScorchGrassB", "EmberTuft",
        "BasaltRimA", "BasaltRimB", "BasaltRimC", "VolcanoGate", "Volcano", "LavaVent",
    };
    public static bool IsVolcano(string name) => Props.Contains(name);

    public static Material Glow(Texture2D palette)
    {
        var sh = Shader.Find("BrawlFortress/LavaGlow");
        if (sh == null) return null;
        var m = new Material(sh); m.mainTexture = palette;
        m.SetFloat("_Flicker", 0.12f); m.SetFloat("_Boost", 1f);
        return IceThemeSetup.Save(GlowMat, m);
    }

    /// <summary>After the prefabs exist: floor, lava lake, vent. Returns how many prefabs it made.</summary>
    public static int Setup(Material reference)
    {
        int made = 0;
        foreach (var p in new[] { Root + "/BasaltGround.png", Root + "/LavaFlow.png" })
            if (AssetImporter.GetAtPath(p) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat; ti.mipmapEnabled = true;
                ti.filterMode = FilterMode.Bilinear; ti.anisoLevel = 4; ti.textureCompression = TextureImporterCompression.Compressed; ti.sRGBTexture = true;
                ti.SaveAndReimport();
            }
        var basalt = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/BasaltGround.png");
        if (basalt != null && IceThemeSetup.MakeFloor(IceThemeSetup.MakeMat(GroundMat, reference, basalt, Color.white), "BasaltFloor")) made++;

        var lavaTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/LavaFlow.png");
        var lavaSh = Shader.Find("BrawlFortress/Lava");
        if (lavaTex == null || lavaSh == null) { Debug.LogWarning("[Volcano] LavaFlow.png or the BrawlFortress/Lava shader is missing"); return made; }
        var lake = new Material(lavaSh); lake.mainTexture = lavaTex;
        lake.SetFloat("_WorldUV", 1); lake.SetFloat("_Tiling", 0.11f); lake.SetFloat("_Pulse", 0.18f);
        lake = IceThemeSetup.Save(LakeMat, lake);
        var fount = new Material(lavaSh); fount.mainTexture = lavaTex;
        fount.SetFloat("_WorldUV", 0); fount.SetVector("_Scroll1", new Vector4(0.05f, -0.9f, 0, 0)); fount.SetVector("_Scroll2", new Vector4(-0.04f, -1.35f, 0, 0));
        fount.SetFloat("_Pulse", 0.3f); fount.SetFloat("_Boost", 1.2f);
        fount.SetColor("_HotColor", new Color(1f, 0.58f, 0.14f)); fount.SetFloat("_HotMix", 0.6f);
        fount = IceThemeSetup.Save(FountainMat, fount);

        // lava lake: a flat quad (the builder scales it to cover the lake); no collider, players never reach it
        var lakeGo = new GameObject("LavaLake");
        var q = new GameObject("Surface");
        q.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var qr = q.AddComponent<MeshRenderer>(); qr.sharedMaterial = lake;
        qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; qr.receiveShadows = false;
        q.transform.SetParent(lakeGo.transform, false); q.transform.localRotation = Quaternion.Euler(90, 0, 0);
        PrefabUtility.SaveAsPrefabAsset(lakeGo, PrefabDir + "/LavaLake.prefab");
        Object.DestroyImmediate(lakeGo); made++;

        if (MakeVent(fount)) made++;
        var skySh = Shader.Find("BrawlFortress/SkyGradient");
        if (skySh != null) IceThemeSetup.Save(SkyMat, new Material(skySh));   // smoky dusk sky (default colours of the shader)
        return made;
    }

    static Mesh Fountain()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(FountainMesh);
        if (mesh == null) { mesh = new Mesh { name = "LavaFountain" }; AssetDatabase.CreateAsset(mesh, FountainMesh); }
        mesh.Clear();
        // a jet of molten rock, unit height: a wide boiling foot, a narrow neck, a splash crown with a jagged lip
        // folding back into a dip at the top (open lava shader is two-sided)
        float[] vs = { 0f, 0.10f, 0.28f, 0.52f, 0.72f, 0.84f, 0.93f, 0.99f, 0.95f, 0.88f };
        float[] rs = { 0.62f, 0.46f, 0.31f, 0.27f, 0.31f, 0.44f, 0.60f, 0.66f, 0.40f, 0.0f };
        const int S = 14;
        var verts = new System.Collections.Generic.List<Vector3>(); var uvs = new System.Collections.Generic.List<Vector2>(); var tris = new System.Collections.Generic.List<int>();
        for (int i = 0; i < vs.Length; i++)
            for (int j = 0; j <= S; j++)
            {
                float a = j * Mathf.PI * 2f / S;
                float wob = 1f + 0.06f * Mathf.Sin(a * 3f + i * 1.7f);
                float lip = i == 7 ? (j % 2 == 0 ? 1.15f : 0.8f) : 1f;          // jagged splash lip
                float lift = i == 7 ? (j % 2 == 0 ? 0.04f : -0.03f) : 0f;
                verts.Add(new Vector3(Mathf.Cos(a) * rs[i] * wob * lip, vs[i] + lift, Mathf.Sin(a) * rs[i] * wob * lip));
                uvs.Add(new Vector2((float)j / S * 2f, vs[i] * 3f));
            }
        for (int i = 0; i < vs.Length - 1; i++)
            for (int j = 0; j < S; j++)
            {
                int a = i * (S + 1) + j, b = a + 1, c = a + S + 1, d = c + 1;
                tris.AddRange(new[] { a, c, b, b, c, d });
            }
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    static bool MakeVent(Material fountainMat)
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/LavaVent.prefab");
        var warnSh = Shader.Find("BrawlFortress/VentWarn");
        if (basePrefab == null || warnSh == null) { Debug.LogWarning("[Volcano] LavaVent prefab or VentWarn shader missing"); return false; }
        var warn = IceThemeSetup.Save(WarnMat, new Material(warnSh));
        var pool = AssetDatabase.LoadAssetAtPath<Texture2D>(IceThemeSetup.PoolTex);   // soft round dot
        var em = new Material(Shader.Find("Legacy Shaders/Particles/Additive")); em.mainTexture = pool;
        if (em.HasProperty("_TintColor")) em.SetColor("_TintColor", new Color(1f, 0.55f, 0.15f, 0.8f));
        em = IceThemeSetup.Save(EmberMat, em);

        var root = new GameObject("EruptionVent");
        var vent = root.AddComponent<EruptionVent>();
        var b = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        b.transform.SetParent(root.transform, false);
        vent.glow = b.GetComponentInChildren<Renderer>();

        var w = new GameObject("WarnDisc");
        w.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var wr = w.AddComponent<MeshRenderer>(); wr.sharedMaterial = warn;
        wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; wr.receiveShadows = false;
        w.transform.SetParent(root.transform, false);
        w.transform.localPosition = new Vector3(0, 0.08f, 0); w.transform.localRotation = Quaternion.Euler(90, 0, 0);
        w.transform.localScale = Vector3.one * (vent.radius * 2f / 0.96f);   // the disc's edge (uv 0.96) on the hit radius
        vent.warnDisc = wr;

        var f = new GameObject("Fountain");
        f.AddComponent<MeshFilter>().sharedMesh = Fountain();
        var fr = f.AddComponent<MeshRenderer>(); fr.sharedMaterial = fountainMat;
        fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        f.transform.SetParent(root.transform, false);
        vent.fountain = f.transform;
        f.SetActive(false); wr.enabled = false;   // off until the vent script shows them (and never baked into the NavMesh)

        vent.embers = Particles(root.transform, "Embers", em, burst: true);
        vent.sputter = Particles(root.transform, "Sputter", em, burst: false);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/EruptionVent.prefab");
        Object.DestroyImmediate(root);
        return true;
    }

    static ParticleSystem Particles(Transform parent, string name, Material mat, bool burst)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, 0.2f, 0);
        go.transform.localRotation = Quaternion.Euler(-90, 0, 0);   // emit upward
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false; main.loop = false;
        main.duration = burst ? 1.2f : 2.0f;
        main.startLifetime = burst ? new ParticleSystem.MinMaxCurve(0.8f, 1.4f) : new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = burst ? new ParticleSystem.MinMaxCurve(5f, 9f) : new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSize = burst ? new ParticleSystem.MinMaxCurve(0.15f, 0.38f) : new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.1f), new Color(1f, 0.85f, 0.35f));
        main.gravityModifier = burst ? 1.3f : 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = burst ? 90 : 30;
        var emission = ps.emission;
        emission.rateOverTime = burst ? 0f : 12f;
        if (burst) emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36), new ParticleSystem.Burst(0.3f, 22), new ParticleSystem.Burst(0.65f, 14) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = burst ? 22f : 35f; shape.radius = burst ? 0.35f : 0.5f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.3f));
        var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return ps;
    }
}
