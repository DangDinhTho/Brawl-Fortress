using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brawl-Stars-like bush hiding, added at runtime to every TF2Player.
/// Local viewer rules (computed on every client, no network traffic):
///  - me / teammates in a bush  -> drawn see-through (ghost)
///  - enemy in a bush           -> not drawn (model, weapon, header UI), unless the local player
///                                 is in the SAME bush, or closer than RevealDistance
/// Bots / sentries use IsHiddenFrom() in FieldOfView, so they do not target hidden players either.
/// </summary>
public class BushStealth : MonoBehaviour
{
    /// <summary>Enemies closer than this are visible even in a bush. 0 = only the same-bush rule.</summary>
    public static float RevealDistance = 0f;
    /// <summary>Two characters in the same (connected) bush see each other only within this range,
    /// so a long bush strip (e.g. the map border) does not reveal people at the other end.</summary>
    public static float SameBushRange = 6f;
    public const float GhostAlpha = 0.45f;
    const float CheckInterval = 0.1f;
    const float RefreshInterval = 0.5f;

    enum State { Normal, Ghost, Hidden }

    private TF2Player player;
    private State state = State.Normal;
    private float nextCheck, nextRefresh;
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly Dictionary<Renderer, (Material[] original, Material[] ghost)> ghosted = new Dictionary<Renderer, (Material[], Material[])>();
    private readonly Dictionary<Material, Material> ghostCache = new Dictionary<Material, Material>();
    private CanvasGroup headerGroup;

    private static Shader ghostShader;
    private static Material clearMaterial;

    public BushArea CurrentBush { get; private set; }
    public Team Team => player != null ? player.team : Team.None;

    public Vector3 Position => player != null && player.remotePlayer != null ? player.remotePlayer.transform.position : transform.position;

    private void Awake()
    {
        player = GetComponent<TF2Player>();
    }

    private bool IsDead => player == null || player.healthManager == null || player.healthManager.death;

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + CheckInterval;

        CurrentBush = (BushArea.All.Count == 0 || IsDead) ? null : BushArea.Find(Position + Vector3.up * 0.3f);
        Apply(ComputeState());
    }

    private State ComputeState()
    {
        if (CurrentBush == null) return State.Normal;
        var arena = ArenaManager.Instance;
        TF2Player viewer = arena != null ? arena.mainPlayer : null;
        if (viewer == null) return State.Normal;
        if (player.team == viewer.team) return State.Ghost;

        var viewerStealth = viewer.GetComponent<BushStealth>();
        BushArea viewerBush = viewerStealth != null ? viewerStealth.CurrentBush : null;
        Vector3 viewerPos = viewerStealth != null ? viewerStealth.Position : viewer.transform.position;
        return IsHiddenFrom(this, viewer.team, viewerPos, viewerBush) ? State.Hidden : State.Normal;
    }

    /// <summary>True when 'target' is in a bush and must not be seen/targeted by an observer of the given team.</summary>
    public static bool IsHiddenFrom(BushStealth target, Team observerTeam, Vector3 observerPos, BushArea observerBush)
    {
        if (target == null || target.CurrentBush == null) return false;
        if (target.Team == observerTeam) return false;
        if (observerBush != null && observerBush == target.CurrentBush
            && (observerPos - target.Position).sqrMagnitude <= SameBushRange * SameBushRange) return false;
        if (RevealDistance > 0f && (observerPos - target.Position).sqrMagnitude <= RevealDistance * RevealDistance) return false;
        return true;
    }

    #region Visuals
    private void Apply(State next)
    {
        bool refresh = next != State.Normal && Time.unscaledTime >= nextRefresh;
        if (next == state && !refresh) return;
        nextRefresh = Time.unscaledTime + RefreshInterval;

        if (next != state)
        {
            // leave current state
            if (state == State.Hidden) SetRenderingOff(false);
            if (state == State.Ghost) RestoreGhost();
            state = next;
        }

        CollectRenderers();
        switch (state)
        {
            case State.Hidden: SetRenderingOff(true); SetHeaderVisible(false); break;
            case State.Ghost: ApplyGhost(); SetHeaderVisible(true); break;
            default: SetHeaderVisible(true); break;
        }
    }

    private void CollectRenderers()
    {
        renderers.Clear();
        GetComponentsInChildren(true, renderers);
    }

    private void SetRenderingOff(bool off)
    {
        if (!off) CollectRenderers();
        for (int i = 0; i < renderers.Count; i++)
            if (renderers[i] != null) renderers[i].forceRenderingOff = off;
    }

    private void SetHeaderVisible(bool visible)
    {
        if (player.headerUIForFun == null) return;
        if (headerGroup == null)
        {
            headerGroup = player.headerUIForFun.GetComponent<CanvasGroup>();
            if (headerGroup == null) headerGroup = player.headerUIForFun.gameObject.AddComponent<CanvasGroup>();
        }
        headerGroup.alpha = visible ? 1f : 0f;
    }

    private void ApplyGhost()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var rd = renderers[i];
            if (rd == null || rd is ParticleSystemRenderer || rd is LineRenderer || rd is TrailRenderer) continue;
            var current = rd.sharedMaterials;
            if (ghosted.TryGetValue(rd, out var entry) && SameMaterials(current, entry.ghost)) continue; // already ghost
            var ghost = new Material[current.Length];
            bool changed = false;
            for (int m = 0; m < current.Length; m++)
            {
                ghost[m] = GetGhost(current[m]);
                if (ghost[m] != current[m]) changed = true;
            }
            if (!changed) continue;
            ghosted[rd] = (current, ghost);
            rd.sharedMaterials = ghost;
        }
    }

    private void RestoreGhost()
    {
        foreach (var kv in ghosted)
        {
            // only restore if nobody else (team colour, spy cloak) replaced the materials meanwhile
            if (kv.Key != null && SameMaterials(kv.Key.sharedMaterials, kv.Value.ghost))
                kv.Key.sharedMaterials = kv.Value.original;
        }
        ghosted.Clear();
    }

    private Material GetGhost(Material src)
    {
        if (src == null) return null;
        if (src.name.StartsWith("InvisibilityMat")) return src; // spy cloak already handles it
        if (ghostCache.TryGetValue(src, out var g)) return g;

        if (ghostShader == null)
        {
            var template = Resources.Load<Material>("Materials/BushGhost");
            ghostShader = template != null ? template.shader : Shader.Find("FlatKit/Stylized Surface Ghost");
        }
        if (src.shader != null && src.shader.name.StartsWith("FlatKit/Stylized Surface") && ghostShader != null)
        {
            g = new Material(src) { name = src.name + "_ghost" };
            g.shader = ghostShader;
            g.SetFloat("_GhostAlpha", GhostAlpha);
            g.renderQueue = 3000;
        }
        else
        {
            // eyes and other opaque parts would show through the see-through body: hide them
            if (clearMaterial == null) clearMaterial = Resources.Load<Material>("Materials/InvisibilityMat");
            g = clearMaterial != null ? clearMaterial : src;
        }
        ghostCache[src] = g;
        return g;
    }

    private static bool SameMaterials(Material[] a, Material[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }
    #endregion

    private void OnDisable()
    {
        if (state == State.Hidden) SetRenderingOff(false);
        if (state == State.Ghost) RestoreGhost();
        SetHeaderVisible(true);
        state = State.Normal;
        CurrentBush = null;
    }

    private void OnDestroy()
    {
        foreach (var kv in ghostCache)
            if (kv.Value != null && kv.Value != kv.Key && kv.Value != clearMaterial) Destroy(kv.Value);
        ghostCache.Clear();
    }
}
