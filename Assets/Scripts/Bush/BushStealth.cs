using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brawl-Stars-like bush hiding, added at runtime to every TF2Player.
/// Local viewer rules (computed on every client, no network traffic):
///  - me / teammates in a bush  -> drawn normally; the grass within RevealRadius around them fades (BushGrassFade)
///  - enemy in a bush           -> not drawn (model, weapon, header UI), unless it is inside the reveal zone
///                                 (RevealRadius) of one of my team's characters that is itself in a bush
/// 'In a bush' = about 80% of the body inside (EnterCoverage / ExitCoverage).
/// Bots / sentries use IsHiddenFrom() in FieldOfView, so they do not target hidden players either.
/// </summary>
public class BushStealth : MonoBehaviour
{
    /// <summary>All live BushStealth components (players and bots).</summary>
    public static readonly List<BushStealth> AllStealth = new List<BushStealth>();
    /// <summary>Radius of the zone around a character in a bush: grass fades for its team and enemies inside are revealed to its team.</summary>
    public static float RevealRadius = 2.4f;
    /// <summary>Enemies closer than this are visible even in a bush, whether or not anyone is in a bush. 0 = off.</summary>
    public static float RevealDistance = 0f;
    const float CheckInterval = 0.1f;
    /// <summary>Footprint radius of a character used to measure how much of it is inside a bush.</summary>
    public static float BodyRadius = 0.5f;  // = bot NavMeshAgent radius
    /// <summary>Share of the footprint that must be inside a bush to become hidden (0.8 = 80%).</summary>
    public static float EnterCoverage = 0.8f;
    /// <summary>Revealed again as soon as less than this share is inside (0.8 = about 20% of the body out).
    /// Samples come in steps of 1/17, so with 0.8 a character is hidden from 14/17 (82%) and revealed at 13/17 (76%).</summary>
    public static float ExitCoverage = 0.8f;

    // footprint sample points (unit disc): centre + inner ring of 8 + outer ring of 8
    private static readonly Vector2[] Samples = BuildSamples();
    private static Vector2[] BuildSamples()
    {
        var s = new Vector2[17];
        s[0] = Vector2.zero;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            s[1 + i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.55f;
            s[9 + i] = new Vector2(Mathf.Cos(a + Mathf.PI / 8f), Mathf.Sin(a + Mathf.PI / 8f));
        }
        return s;
    }
    private static readonly List<BushArea> tallyBush = new List<BushArea>(4);
    private static readonly List<int> tallyCount = new List<int>(4);

    private TF2Player player;
    private bool hidden;
    private float nextCheck;
    private readonly List<Renderer> renderers = new List<Renderer>();
    private CanvasGroup headerGroup;

    public BushArea CurrentBush { get; private set; }
    public Team Team => player != null ? player.team : Team.None;
    public bool IsDead => player == null || player.healthManager == null || player.healthManager.death;
    /// <summary>True when this character is in a bush and alive: its reveal zone is active.</summary>
    public bool InBush => CurrentBush != null && !IsDead;

    public Vector3 Position => player != null && player.remotePlayer != null ? player.remotePlayer.transform.position : transform.position;

    private void Awake()
    {
        player = GetComponent<TF2Player>();
    }

    private void OnEnable()
    {
        if (!AllStealth.Contains(this)) AllStealth.Add(this);
        BushGrassFade.Ensure();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + CheckInterval;

        if (BushArea.All.Count == 0 || IsDead) CurrentBush = null;
        else
        {
            Vector3 p = Position + Vector3.up * 0.3f;
            // hidden only when most of the body is inside the bush, not when it merely touches it
            if (CurrentBush != null && (!CurrentBush.isActiveAndEnabled || Coverage(CurrentBush, p) < ExitCoverage))
                CurrentBush = null;
            if (CurrentBush == null)
            {
                var best = BestBush(p, out float cov);
                if (best != null && cov >= EnterCoverage) CurrentBush = best;
            }
        }
        SetHidden(ComputeHidden());
    }

    /// <summary>Share (0..1) of a character footprint centred at p that lies inside the bush.</summary>
    public static float Coverage(BushArea bush, Vector3 p)
    {
        int inside = 0;
        for (int i = 0; i < Samples.Length; i++)
        {
            var s = Samples[i] * BodyRadius;
            if (bush.Contains(new Vector3(p.x + s.x, p.y, p.z + s.y))) inside++;
        }
        return inside / (float)Samples.Length;
    }

    /// <summary>The bush covering the largest share of the footprint at p.</summary>
    public static BushArea BestBush(Vector3 p, out float coverage)
    {
        tallyBush.Clear(); tallyCount.Clear();
        for (int i = 0; i < Samples.Length; i++)
        {
            var s = Samples[i] * BodyRadius;
            var b = BushArea.Find(new Vector3(p.x + s.x, p.y, p.z + s.y));
            if (b == null) continue;
            int k = tallyBush.IndexOf(b);
            if (k < 0) { tallyBush.Add(b); tallyCount.Add(1); } else tallyCount[k]++;
        }
        BushArea best = null; int bestN = 0;
        for (int i = 0; i < tallyBush.Count; i++) if (tallyCount[i] > bestN) { bestN = tallyCount[i]; best = tallyBush[i]; }
        coverage = bestN / (float)Samples.Length;
        return best;
    }

    private bool ComputeHidden()
    {
        if (CurrentBush == null) return false;
        var arena = ArenaManager.Instance;
        TF2Player viewer = arena != null ? arena.mainPlayer : null;
        if (viewer == null || player.team == viewer.team) return false;   // me / teammates: always drawn normally
        var viewerStealth = viewer.GetComponent<BushStealth>();
        Vector3 viewerPos = viewerStealth != null ? viewerStealth.Position : viewer.transform.position;
        return IsHiddenFrom(this, viewer.team, viewerPos, viewerStealth != null ? viewerStealth.CurrentBush : null);
    }

    /// <summary>True when 'target' is in a bush and must not be seen/targeted by the observer's team:
    /// it is revealed only inside the reveal zone (RevealRadius) of an observer-team character that is in a bush.</summary>
    public static bool IsHiddenFrom(BushStealth target, Team observerTeam, Vector3 observerPos, BushArea observerBush)
    {
        if (target == null || target.CurrentBush == null) return false;
        if (target.Team == observerTeam) return false;
        Vector3 tp = target.Position;
        if (RevealDistance > 0f && FlatSqr(observerPos - tp) <= RevealDistance * RevealDistance) return false;
        float r2 = RevealRadius * RevealRadius;
        for (int i = 0; i < AllStealth.Count; i++)
        {
            var s = AllStealth[i];
            if (s == null || s == target || s.Team != observerTeam || !s.InBush) continue;
            if (FlatSqr(s.Position - tp) <= r2) return false;
        }
        return true;
    }

    private static float FlatSqr(Vector3 v) { v.y = 0f; return v.sqrMagnitude; }

    #region Visuals
    private void SetHidden(bool hide)
    {
        if (hide == hidden) { if (hide) SetRenderingOff(true); return; } // re-apply: weapons may have been swapped
        hidden = hide;
        SetRenderingOff(hide);
        SetHeaderVisible(!hide);
    }

    private void SetRenderingOff(bool off)
    {
        renderers.Clear();
        GetComponentsInChildren(true, renderers);
        for (int i = 0; i < renderers.Count; i++)
            if (renderers[i] != null) renderers[i].forceRenderingOff = off;
    }

    private void SetHeaderVisible(bool visible)
    {
        if (player == null || player.headerUIForFun == null) return;
        if (headerGroup == null)
        {
            headerGroup = player.headerUIForFun.GetComponent<CanvasGroup>();
            if (headerGroup == null) headerGroup = player.headerUIForFun.gameObject.AddComponent<CanvasGroup>();
        }
        headerGroup.alpha = visible ? 1f : 0f;
    }
    #endregion

    private void OnDisable()
    {
        AllStealth.Remove(this);
        if (hidden) SetRenderingOff(false);
        SetHeaderVisible(true);
        hidden = false;
        CurrentBush = null;
    }
}
