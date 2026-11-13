using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fake blob shadow: a small soft round dark disc centred under the character's feet
/// (added to every TF2Player / TF2Bot / TF2Robot in CreateCharacter).
/// Its size depends on the class (SizeFor: Heavy biggest, Scout smallest) times the model scale. The disc is a child of the player, so BushStealth hides it with the character.
/// Hidden while dead. Material: Resources/Materials/FakeShadow.mat (shader BrawlFortress/FakeShadow).
/// </summary>
public class FakeShadow : MonoBehaviour
{
    /// <summary>Darkness at the centre (0..1).</summary>
    public static float Opacity = 0.25f;
    /// <summary>Global multiplier on the per-class size, and limits (m).</summary>
    public static float SizeScale = 1f, MinSize = 0.5f, MaxSize = 2.5f;
    /// <summary>Used until / if the body cannot be measured (m).</summary>
    public static float DefaultSize = 0.95f;
    /// <summary>Stop the character's own real-time shadow so it is not doubled with the blob.</summary>
    public static bool DisableRealShadow = true;

    static Mesh quad;
    static Material material;
    static readonly int ColorId = Shader.PropertyToID("_Color");

    TF2Player player;
    Transform disc;
    MeshRenderer rend;
    MaterialPropertyBlock mpb;
    float visible = 1f;
    float size;
    int measureTries;
    float nextMeasure, nextShadowOff;
    readonly List<Renderer> own = new List<Renderer>();

    void Awake() { player = GetComponent<TF2Player>(); size = DefaultSize; }

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
        nextMeasure = Time.time + 0.3f; // let the animator pose the body first
    }

    /// <summary>Shadow diameter per class at model scale 1 (m). The skinned-mesh bounds were too loose to measure reliably.</summary>
    public static float SizeFor(TF2Class c)
    {
        switch (c)
        {
            case TF2Class.heavy: return 1.25f;
            case TF2Class.soldier: return 1.0f;
            case TF2Class.demoman: return 1.0f;
            case TF2Class.pyro: return 1.0f;
            case TF2Class.medic: return 0.95f;
            case TF2Class.engineer: return 0.95f;
            case TF2Class.sniper: return 0.9f;
            case TF2Class.spy: return 0.85f;
            case TF2Class.scout: return 0.8f;
            default: return DefaultSize;
        }
    }

    /// <summary>Size from the class table, scaled by the model's scale (e.g. bigger MvM robots).</summary>
    bool Measure()
    {
        if (player == null) return false;
        float s = SizeFor(player.tF2Class);
        if (player.remotePlayer != null) s *= player.remotePlayer.transform.lossyScale.x;
        size = Mathf.Clamp(s * SizeScale, MinSize, MaxSize);
        return player.tF2Class != TF2Class.None;
    }

    Vector3 Feet => player != null && player.remotePlayer != null ? player.remotePlayer.transform.position : transform.position;
    bool Dead => player != null && player.healthManager != null && player.healthManager.death;

    void LateUpdate()
    {
        if (disc == null) return;
        if (measureTries < 3 && Time.time >= nextMeasure)
        {
            measureTries = Measure() ? 3 : measureTries + 1;
            nextMeasure = Time.time + 0.5f;
        }
        if (DisableRealShadow && Time.unscaledTime >= nextShadowOff)
        {
            nextShadowOff = Time.unscaledTime + 2f; // weapons / cosmetics can be swapped at runtime
            own.Clear(); GetComponentsInChildren(true, own);
            for (int i = 0; i < own.Count; i++)
                if (own[i] != null && own[i] != rend) own[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        visible = Mathf.MoveTowards(visible, Dead ? 0f : 1f, Time.deltaTime * 6f);
        if (visible <= 0f) { if (rend.enabled) rend.enabled = false; return; }
        if (!rend.enabled) rend.enabled = true;

        Vector3 feet = Feet;
        disc.position = new Vector3(feet.x, feet.y + 0.03f, feet.z);
        disc.rotation = Quaternion.identity;
        var ps = transform.lossyScale;
        disc.localScale = new Vector3(size / Mathf.Max(ps.x, 1e-4f), 1f / Mathf.Max(ps.y, 1e-4f), size / Mathf.Max(ps.z, 1e-4f));

        mpb.SetColor(ColorId, new Color(0f, 0f, 0f, Opacity * visible));
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

    /// <summary>Current diameter (m), for debugging.</summary>
    public float CurrentSize => size;

    void OnDestroy() { if (disc != null) Destroy(disc.gameObject); }
}
