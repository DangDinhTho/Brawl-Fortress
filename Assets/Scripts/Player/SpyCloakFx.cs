using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spy cloak look, on every renderer of the character (body, weapons, cosmetics), seen differently by each viewer:
///  - enemies: fades out (fadeTime) then is not drawn at all (header hidden too; FootRing hides via TF2Player.MeshInvisible);
///  - team mates and the Spy himself: fades to a see-through ghost (allyAlpha) and stays visible.
/// Uncloaking fades back in; firing a weapon while cloaked uncloaks at once (fast fade), and on the Spy's owner it also
/// ends the cloak on the watch (InvisWatch) so everyone sees it. Driven by TF2Player.SetMeshInvsibility / SetMeshTeam.
/// The see-through look uses a transparent copy of each material (Resources/Materials/CloakGhost, texture kept), faded
/// with a MaterialPropertyBlock; the original materials come back when the fade-in ends.
/// </summary>
[DisallowMultipleComponent]
public class SpyCloakFx : MonoBehaviour
{
    public static float FadeTime = 0.6f;
    public static float FastFadeTime = 0.15f;
    public static float AllyAlpha = 0.35f;

    TF2Player player;
    InvisWatch watch;
    bool cloaked;
    float alpha = 1f;          // 1 = normal, 0 = gone
    bool ghostOn;
    bool fast;
    float nextRefresh;
    CanvasGroup header;

    readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
    readonly List<Renderer> scratch = new List<Renderer>();
    static Material ghostBase;
    static readonly Dictionary<Material, Material> ghostCache = new Dictionary<Material, Material>();
    static MaterialPropertyBlock mpb;
    static readonly int ColorId = Shader.PropertyToID("_Color");

    public bool Cloaked => cloaked;

    public static SpyCloakFx Of(TF2Player p)
    {
        var fx = p.GetComponent<SpyCloakFx>();
        if (fx == null) fx = p.gameObject.AddComponent<SpyCloakFx>();
        return fx;
    }

    void Awake()
    {
        player = GetComponent<TF2Player>();
        var spy = GetComponentInChildren<SpyController>(true);
        watch = spy != null ? spy.invisWatch : null;
        if (mpb == null) mpb = new MaterialPropertyBlock();
    }

    bool ViewerIsEnemy()
    {
        var viewer = ArenaManager.Instance != null ? ArenaManager.Instance.mainPlayer : null;
        return viewer != null && viewer != player && viewer.team != player.team;
    }

    public void SetCloaked(bool on)
    {
        if (on && cloaked) return;
        if (!on && !cloaked && !ghostOn) return;   // (already uncloaking after a seen attack: re-capture the team look below)
        cloaked = on;
        fast = false;
        if (!on) CaptureCurrent(true);   // the team / disguise materials just set by SetMeshTeam are the ones to come back to
        else CaptureCurrent(false);
        ApplyGhost();
    }

    static Material Ghost(Material src)
    {
        if (src == null) return null;
        if (ghostCache.TryGetValue(src, out var g) && g != null) return g;
        if (ghostBase == null) ghostBase = Resources.Load<Material>("Materials/CloakGhost");
        if (ghostBase == null) return null;
        g = new Material(ghostBase) { name = src.name + " (cloak)" };
        if (src.HasProperty("_MainTex")) g.mainTexture = src.mainTexture;
        ghostCache[src] = g;
        return g;
    }

    /// <summary>Opaque parts of the character only (body, weapons, cosmetics). Transparent / effect renderers are left
    /// alone: the foot ring is a quad drawn round by its own shader and turned into a square with the ghost material
    /// (it hides itself from enemies while cloaked, see FootRing).</summary>
    static bool Drawn(Renderer r)
    {
        if (!(r is SkinnedMeshRenderer || r is MeshRenderer)) return false;
        var mats = r.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i];
            if (m == null) continue;
            if (m.name.EndsWith(" (cloak)")) return true;          // already ghosted by us
            if (m.renderQueue > 2500) return false;                 // transparent / effect (foot ring, glows...)
        }
        return true;
    }

    /// <summary>Remembers the materials the character is drawn with now (new renderers only, unless 'replace').</summary>
    void CaptureCurrent(bool replace)
    {
        scratch.Clear();
        GetComponentsInChildren(true, scratch);
        foreach (var r in scratch)
        {
            if (r == null || !Drawn(r)) continue;
            if (replace || !originals.ContainsKey(r))
            {
                var mats = r.sharedMaterials;
                // never store our own ghost copies as originals
                bool isGhost = mats.Length > 0 && mats[0] != null && mats[0].name.EndsWith(" (cloak)");
                if (!isGhost) originals[r] = mats;
            }
        }
    }

    void ApplyGhost()
    {
        foreach (var kv in originals)
        {
            var r = kv.Key;
            if (r == null) continue;
            var src = kv.Value;
            var ghost = new Material[src.Length];
            for (int i = 0; i < src.Length; i++) ghost[i] = Ghost(src[i]) ?? src[i];
            r.sharedMaterials = ghost;
        }
        ghostOn = true;
        SetAlpha(alpha);
    }

    void RestoreOriginals()
    {
        foreach (var kv in originals)
        {
            if (kv.Key == null) continue;
            kv.Key.sharedMaterials = kv.Value;
            kv.Key.SetPropertyBlock(null);
            kv.Key.forceRenderingOff = false;
        }
        originals.Clear();
        ghostOn = false;
    }

    void SetAlpha(float a)
    {
        foreach (var kv in originals)
        {
            var r = kv.Key;
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, new Color(1f, 1f, 1f, a));
            r.SetPropertyBlock(mpb);
            r.forceRenderingOff = a <= 0.01f;   // fully cloaked to enemies: not drawn at all
        }
        if (header == null && player != null && player.headerUIForFun != null)
        {
            header = player.headerUIForFun.GetComponent<CanvasGroup>();
            if (header == null) header = player.headerUIForFun.gameObject.AddComponent<CanvasGroup>();
        }
        if (header != null) header.alpha = Mathf.Clamp01(a * 2f);
    }

    void LateUpdate()
    {
        if (player == null) return;
        bool dead = player.healthManager != null && player.healthManager.death;

        // attacking while cloaked gives the Spy away at once
        if (cloaked && !dead)
        {
            var w = player.remotePlayer != null ? player.remotePlayer.weapon : null;
            if (w != null && w.fire)
            {
                if (player.photonView.IsMine && watch != null) watch.ShowAvailble(true);   // ends the cloak for everyone
                else { cloaked = false; CaptureCurrent(false); }
                fast = true;
            }
        }
        if (dead && cloaked) { cloaked = false; fast = true; }

        float target = !cloaked ? 1f : (ViewerIsEnemy() ? 0f : AllyAlpha);
        if (!ghostOn && Mathf.Approximately(alpha, target) && target >= 1f) return;   // idle, nothing to do

        // weapons swapped while cloaked: new renderers get the ghost look too
        if (ghostOn && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.25f;
            int before = originals.Count;
            CaptureCurrent(false);
            if (originals.Count != before) ApplyGhost();
        }
        if (!ghostOn) ApplyGhost();

        float speed = 1f / Mathf.Max(0.01f, fast ? FastFadeTime : FadeTime);
        alpha = Mathf.MoveTowards(alpha, target, speed * Time.deltaTime);
        SetAlpha(alpha);
        if (!cloaked && alpha >= 1f) { RestoreOriginals(); SetAlpha(1f); fast = false; }
    }

    void OnDisable()
    {
        if (ghostOn) RestoreOriginals();
        alpha = 1f; cloaked = false;
        if (header != null) header.alpha = 1f;
    }
}
