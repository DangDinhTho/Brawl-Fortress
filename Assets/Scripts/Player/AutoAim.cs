using UnityEngine;

/// <summary>
/// Brawl Stars style auto-aim for the local player: a tap on the attack stick (or holding it with a hold weapon) shoots
/// at the nearest enemy within the weapon's range. Like the bots' FieldOfView it never picks what the player cannot see:
/// enemies hidden in a bush (BushStealth.IsHiddenFrom), cloaked Spies, Spies disguised as the player's team.
/// Characters come before buildings (sentries...), targets in the open before targets behind a wall (lobbed grenades /
/// stickies fly over walls, so walls do not matter for them). Targets are aimed at where they stand (no lead), as in
/// Brawl Stars: dragging the stick is still the way to aim ahead of a runner.
/// The Medigun picks a teammate instead: the most hurt one in range.
/// </summary>
public static class AutoAim
{
    /// <summary>Extra reach beyond weaponConfig.range (m): a body's edge is inside the range before its centre.</summary>
    public const float BodyMargin = 0.5f;
    /// <summary>Score penalty (m) for buildings: a character slightly farther away is picked first.</summary>
    public const float BuildingPenalty = 2.5f;
    /// <summary>Keep the current target unless another one is this much (m) closer: no flicking between two enemies.</summary>
    public const float Stickiness = 1.5f;

    static readonly Collider[] hits = new Collider[96];
    static int targetMask = -1, wallMask = -1;

    static void InitMasks()
    {
        if (targetMask != -1) return;
        targetMask = LayerMask.GetMask("Player", "Healthable", "Bot");
        wallMask = LayerMask.GetMask("Wall", "VitualWall");
    }

    /// <summary>The weapon can use auto-aim: it attacks something (range > 0).</summary>
    public static bool Supports(Weapon w) => w != null && w.weaponConfig != null && w.weaponConfig.range > 0f;

    /// <summary>How far auto-aim looks for a target with this weapon (m, flat, from the player's centre).</summary>
    public static float Range(TF2Player me, Weapon w)
    {
        if (!Supports(w)) return 0f;
        float r = w.weaponConfig.range;
        // the flame starts at the nozzle, in front of the body (see AimIndicator)
        if (w is Flamethrower ft && ft.shotPoints != null && ft.shotPoints.Length > 0 && ft.shotPoints[0] != null && me != null)
        {
            Vector3 rel = ft.shotPoints[0].position - me.transform.position; rel.y = 0f;
            r += rel.magnitude;
        }
        return r + BodyMargin;
    }

    /// <summary>The target a tap would shoot at, or null (then the attack goes where the player faces).
    /// 'current' is kept while it stays valid and no other target is clearly closer.</summary>
    public static HealthBase FindTarget(TF2Player me, Weapon w, HealthBase current = null)
    {
        if (me == null || !Supports(w)) return null;
        InitMasks();
        bool friendly = w is Medicgun;
        bool lobbed = w is GrenadeLauncher || w is StickybombLauncher;
        float range = Range(me, w);
        Vector3 from = me.transform.position;
        BushArea myBush = BushArea.All.Count > 0 ? BushArea.Find(from + Vector3.up * 0.3f) : null;

        int n = Physics.OverlapSphereNonAlloc(from + Vector3.up, range + 1f, hits, targetMask, QueryTriggerInteraction.Collide);
        HealthBase best = null; float bestScore = float.MaxValue, currentScore = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var h = hits[i] != null ? hits[i].GetComponentInParent<HealthBase>() : null;
            if (h == null || h.death || h == me.healthManager || h.GetComponentInParent<TF2Player>() == me) continue;
            if (friendly ? h.team != me.team : h.team == me.team) continue;
            Vector3 pos = h.transform.position;
            float d = Flat(pos - from).magnitude;
            if (d > range) continue;
            var tp = h.GetComponent<TF2Player>();
            if (!friendly)
            {
                if (tp != null && tp.MeshInvisible && d > 1.5f) continue;                       // cloaked Spy
                if (tp != null && tp.MeshTeam != Team.None && tp.MeshTeam == me.team) continue; // Spy disguised as one of us
                if (BushStealth.IsHiddenFrom(h.GetComponentInParent<BushStealth>(), me.team, from, myBush)) continue;
            }
            float score;
            if (friendly)
            {
                // the most hurt teammate first, distance only breaks ties
                float hp = h.maxHealth > 0 ? Mathf.Clamp01(h.currentHealth / (float)h.maxHealth) : 1f;
                score = hp * 10f + d * 0.2f;
            }
            else
            {
                score = d;
                if (tp == null) score += BuildingPenalty;
            }
            if (!lobbed && Blocked(from, pos)) score += 100f;   // behind a wall: only when nothing is in the open
            if (h == current) currentScore = score;
            if (score < bestScore) { bestScore = score; best = h; }
        }
        if (current != null && currentScore < float.MaxValue && best != current && bestScore > currentScore - Stickiness) return current;
        return best;
    }

    /// <summary>Flat direction from the player to the target (not normalized), zero without a target.</summary>
    public static Vector3 DirectionTo(TF2Player me, HealthBase target)
    {
        if (me == null || target == null) return Vector3.zero;
        return Flat(target.transform.position - me.transform.position);
    }

    static bool Blocked(Vector3 from, Vector3 to)
    {
        Vector3 a = from + Vector3.up * 0.9f, b = to + Vector3.up * 0.9f;
        Vector3 d = b - a;
        return d.sqrMagnitude > 1e-4f && Physics.Raycast(a, d.normalized, d.magnitude, wallMask, QueryTriggerInteraction.Ignore);
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
}
