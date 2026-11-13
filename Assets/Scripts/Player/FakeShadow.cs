using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fake blob shadow under a character (added to every TF2Player in CreateCharacter).
/// The shape follows the map's main directional light, computed ONCE per map (the light never moves):
///  - stretched along the light direction (longer when the light is low),
///  - shifted away from the light so it starts under the feet and points the way real shadows point,
///  - darkness = how dark real shadows are on that map: shadowStrength * direct / (direct + ambient).
/// The disc is a child of the player, so BushStealth hides it with the character. Hidden while dead.
/// Material: Resources/Materials/FakeShadow.mat (shader BrawlFortress/FakeShadow, procedural disc).
/// </summary>
public class FakeShadow : MonoBehaviour
{
    /// <summary>Width of the shadow (m), roughly the body width.</summary>
    public static float Size = 1.3f;
    /// <summary>Approximate character height used to project the shadow (m).</summary>
    public static float CharacterHeight = 2.0f;
    /// <summary>Share of the real projected length kept by the soft blob (1 = full projection).</summary>
    public static float StretchFactor = 0.6f;
    /// <summary>Longest stretch allowed, as a multiple of CharacterHeight (very low sun).</summary>
    public static float MaxStretch = 2f;
    /// <summary>Multiplier on the computed darkness, and its limits.</summary>
    public static float OpacityScale = 1f, MinOpacity = 0.2f, MaxOpacity = 0.8f;
    /// <summary>Darkness used when the map has no directional light / no real-time shadows.</summary>
    public static float FallbackOpacity = 0.45f;
    /// <summary>Stop the character's own real-time shadow (only drawn on High+ quality / in the editor) so it is not doubled with the blob.</summary>
    public static bool DisableRealShadow = true;

    // ---- computed once per map
    static int lightSceneHandle = int.MinValue;
    static Vector3 shadowDir = Vector3.forward;   // ground direction the shadow points to
    static float shadowLen;                        // extra length along shadowDir (m)
    static float opacity = 0.45f;

    static Mesh quad;
    static Material material;
    static readonly int ColorId = Shader.PropertyToID("_Color");

    TF2Player player;
    Transform disc;
    MeshRenderer rend;
    MaterialPropertyBlock mpb;
    float visible = 1f;
    float nextShadowOff;
    readonly System.Collections.Generic.List<Renderer> own = new System.Collections.Generic.List<Renderer>();

    void Awake() { player = GetComponent<TF2Player>(); }

    void Start()
    {
        if (material == null) material = Resources.Load<Material>("Materials/FakeShadow");
        if (material == null) { enabled = false; return; }
        if (quad == null) quad = BuildQuad();
        var go = new GameObject("FakeShadow");
        go.layer = gameObject.layer;
        disc = go.transform;
        disc.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = quad;
        rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = material;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        mpb = new MaterialPropertyBlock();
        EnsureLight();
    }

    /// <summary>Reads the map light once per map (when the active scene changes).</summary>
    static void EnsureLight()
    {
        int h = SceneManager.GetActiveScene().handle;
        if (h == lightSceneHandle) return;
        lightSceneHandle = h;

        Light sun = RenderSettings.sun;
        if (sun == null || sun.type != LightType.Directional || !sun.isActiveAndEnabled)
        {
            sun = null; float best = -1f;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional || !l.isActiveAndEnabled) continue;
                float w = l.intensity * l.color.grayscale;
                if (w > best) { best = w; sun = l; }
            }
        }
        if (sun == null) { shadowLen = 0f; opacity = FallbackOpacity; return; }

        Vector3 d = sun.transform.forward;
        float sinE = Mathf.Clamp(-d.y, 0.05f, 1f);                 // light elevation
        Vector3 flat = new Vector3(d.x, 0f, d.z);
        shadowDir = flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.forward;
        float tan = Mathf.Min(flat.magnitude / sinE, MaxStretch);  // shadow length per metre of height
        shadowLen = CharacterHeight * tan * StretchFactor;

        if (sun.shadows == LightShadows.None) { opacity = FallbackOpacity; return; }
        float direct = sun.intensity * sun.color.linear.grayscale * sinE;
        float ambient;
        switch (RenderSettings.ambientMode)
        {
            case UnityEngine.Rendering.AmbientMode.Skybox:
                var c = new Color[1]; RenderSettings.ambientProbe.Evaluate(new[] { Vector3.up }, c);
                ambient = c[0].grayscale; break;
            case UnityEngine.Rendering.AmbientMode.Trilight:
                ambient = RenderSettings.ambientSkyColor.linear.grayscale * RenderSettings.ambientIntensity; break;
            default:
                ambient = RenderSettings.ambientLight.linear.grayscale * RenderSettings.ambientIntensity; break;
        }
        float dark = sun.shadowStrength * direct / Mathf.Max(direct + ambient, 1e-4f);
        opacity = Mathf.Clamp(dark * OpacityScale, MinOpacity, MaxOpacity);
    }

    Vector3 Feet => player != null && player.remotePlayer != null ? player.remotePlayer.transform.position : transform.position;
    bool Dead => player != null && player.healthManager != null && player.healthManager.death;

    void LateUpdate()
    {
        if (disc == null) return;
        visible = Mathf.MoveTowards(visible, Dead ? 0f : 1f, Time.deltaTime * 6f);
        if (visible <= 0f) { if (rend.enabled) rend.enabled = false; return; }
        if (!rend.enabled) rend.enabled = true;

        if (DisableRealShadow && Time.unscaledTime >= nextShadowOff)
        {
            nextShadowOff = Time.unscaledTime + 2f; // weapons / cosmetics can be swapped at runtime
            own.Clear(); GetComponentsInChildren(true, own);
            for (int i = 0; i < own.Count; i++)
                if (own[i] != null && own[i] != rend) own[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        Vector3 feet = Feet;
        float length = Size + shadowLen;
        disc.position = new Vector3(feet.x, feet.y + 0.03f, feet.z) + shadowDir * (shadowLen * 0.5f);
        disc.rotation = Quaternion.LookRotation(shadowDir, Vector3.up);
        var ps = transform.lossyScale;
        disc.localScale = new Vector3(Size / Mathf.Max(ps.x, 1e-4f), 1f / Mathf.Max(ps.y, 1e-4f), length / Mathf.Max(ps.z, 1e-4f));

        mpb.SetColor(ColorId, new Color(0f, 0f, 0f, opacity * visible));
        rend.SetPropertyBlock(mpb);
    }

    static Mesh BuildQuad()
    {
        var m = new Mesh { name = "FakeShadowQuad" };
        m.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) };
        m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Editor/debug: values computed for the current map.</summary>
    public static string Describe() { EnsureLight(); return "dir=" + shadowDir.ToString("F2") + " extraLen=" + shadowLen.ToString("F2") + " opacity=" + opacity.ToString("F2"); }

    void OnDestroy() { if (disc != null) Destroy(disc.gameObject); }
}
