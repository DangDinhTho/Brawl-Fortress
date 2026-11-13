using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brawl-Stars-like grass fade for the LOCAL viewer: the grass tiles (BushArea.renderers) within
/// BushStealth.RevealRadius of a character of the viewer's team that is in a bush become see-through.
/// Enemies inside that zone are revealed by BushStealth.IsHiddenFrom. Created automatically by BushStealth.
/// </summary>
public class BushGrassFade : MonoBehaviour
{
    /// <summary>Alpha of faded grass.</summary>
    public static float FadedAlpha = 0.3f;
    /// <summary>Fade in / out time in seconds.</summary>
    public static float FadeTime = 0.15f;

    private static BushGrassFade instance;
    private static readonly int GhostAlphaId = Shader.PropertyToID("_GhostAlpha");
    private static Shader ghostShader;

    class Faded { public Material[] original; public Material[] ghost; public float alpha = 1f; }
    private readonly Dictionary<Renderer, Faded> faded = new Dictionary<Renderer, Faded>();
    private readonly Dictionary<Material, Material> ghostCache = new Dictionary<Material, Material>();
    private readonly List<Vector3> centres = new List<Vector3>(4);
    private readonly List<Renderer> done = new List<Renderer>();
    private MaterialPropertyBlock mpb;

    public static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("BushGrassFade");
        instance = go.AddComponent<BushGrassFade>();
    }

    private void LateUpdate()
    {
        centres.Clear();
        var arena = ArenaManager.Instance;
        var viewer = arena != null ? arena.mainPlayer : null;
        if (viewer != null)
        {
            var list = BushStealth.AllStealth;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].Team == viewer.team && list[i].InBush) centres.Add(list[i].Position);
        }

        float r = BushStealth.RevealRadius, r2 = r * r;
        float step = FadeTime > 0f ? Time.deltaTime / FadeTime : 1f;

        // start fading tiles that entered a zone
        if (centres.Count > 0)
        {
            var bushes = BushArea.All;
            for (int b = 0; b < bushes.Count; b++)
            {
                var bush = bushes[b];
                if (bush == null || bush.renderers == null) continue;
                for (int k = 0; k < bush.renderers.Count; k++)
                {
                    var rd = bush.renderers[k];
                    if (rd == null || faded.ContainsKey(rd) || !Near(rd, r2)) continue;
                    var f = MakeFaded(rd);
                    if (f != null) faded[rd] = f;
                }
            }
        }

        // animate every faded tile toward its target
        done.Clear();
        foreach (var kv in faded)
        {
            var rd = kv.Key; var f = kv.Value;
            if (rd == null) { done.Add(rd); continue; }
            float target = centres.Count > 0 && Near(rd, r2) ? FadedAlpha : 1f;
            f.alpha = Mathf.MoveTowards(f.alpha, target, step * (1f - FadedAlpha));
            if (target >= 1f && f.alpha >= 1f) { rd.sharedMaterials = f.original; done.Add(rd); continue; }
            SetAlpha(rd, f.alpha);
        }
        for (int i = 0; i < done.Count; i++)
        {
            if (done[i] != null) done[i].SetPropertyBlock(null);
            faded.Remove(done[i]);
        }
    }

    private bool Near(Renderer rd, float r2)
    {
        Vector3 c = rd.bounds.center;
        for (int i = 0; i < centres.Count; i++)
        {
            float dx = c.x - centres[i].x, dz = c.z - centres[i].z;
            if (dx * dx + dz * dz <= r2) return true;
        }
        return false;
    }

    private Faded MakeFaded(Renderer rd)
    {
        var orig = rd.sharedMaterials;
        var ghost = new Material[orig.Length];
        bool any = false;
        for (int i = 0; i < orig.Length; i++) { ghost[i] = GetGhost(orig[i]); if (ghost[i] != orig[i]) any = true; }
        if (!any) return null;
        rd.sharedMaterials = ghost;
        var f = new Faded { original = orig, ghost = ghost, alpha = 1f };
        SetAlpha(rd, 1f);
        return f;
    }

    private void SetAlpha(Renderer rd, float a)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        rd.GetPropertyBlock(mpb);
        mpb.SetFloat(GhostAlphaId, a);
        rd.SetPropertyBlock(mpb);
    }

    private Material GetGhost(Material src)
    {
        if (src == null) return null;
        if (ghostCache.TryGetValue(src, out var g)) return g;
        if (ghostShader == null)
        {
            var template = Resources.Load<Material>("Materials/BushGhost");
            ghostShader = template != null ? template.shader : Shader.Find("FlatKit/Stylized Surface Ghost");
        }
        if (ghostShader == null || src.shader == null) g = src;
        else if (src.shader.name.StartsWith("FlatKit/Stylized Surface"))
        {
            g = new Material(src) { name = src.name + "_grassfade" };
            g.shader = ghostShader;
            g.renderQueue = 3000;
        }
        else
        {
            // other shaders: rebuild a ghost from their colour + texture
            g = new Material(ghostShader) { name = src.name + "_grassfade" };
            Texture tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;
            Color col = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
            col.a = 1f;
            g.SetTexture("_MainTex", tex);
            g.SetColor("_Color", col);
            g.SetColor("_ColorDim", col * 0.8f);
            g.SetFloat("_CelPrimaryMode", 1); g.EnableKeyword("_CELPRIMARYMODE_SINGLE");
            g.EnableKeyword("_TEXTUREBLENDINGMODE_MULTIPLY");
            g.renderQueue = 3000;
        }
        ghostCache[src] = g;
        return g;
    }

    private void OnDisable()
    {
        foreach (var kv in faded)
            if (kv.Key != null) { kv.Key.sharedMaterials = kv.Value.original; kv.Key.SetPropertyBlock(null); }
        faded.Clear();
    }

    private void OnDestroy()
    {
        foreach (var kv in ghostCache) if (kv.Value != null && kv.Value != kv.Key) Destroy(kv.Value);
        ghostCache.Clear();
        if (instance == this) instance = null;
    }
}
