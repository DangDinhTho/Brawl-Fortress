using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brawl-Stars-style feedback on every character (added next to FootRing in TF2Player.CreateCharacter / TF2Bot):
///  - footsteps: a puff of dust behind the feet every StepDistance metres while it moves on the ground (bots and remote
///    players too: the movement is read from the position, no network message);
///  - special ground (BrawlFx): mud drops and bubbles in a Swamp bog puddle (slowed), water drops in a Savanna water hole,
///    speed lines on a Japan torii path or swimming with an Ocean current, bubbles swimming against one, green "+"
///    when a water hole heals it;
///  - hit: the model flashes white (FlashTime) and sparks fly where the shot came in (HealthBase.OnTakeDamage, on every
///    client).
/// Nothing shows for a character the viewer cannot see (hidden in a bush, a cloaked Spy) or for a dead one; no dust in a
/// bush (the bush hides the feet). The flash uses a MaterialPropertyBlock (_Color / _ColorDim of the FlatKit material)
/// and leaves renderers that already have one alone (the Spy's cloak).
/// </summary>
public class CharacterFx : MonoBehaviour
{
    public static float StepDistance = 1.05f, MinSpeed = 1.2f, FlashTime = 0.12f, FlashBoost = 2.6f;
    public static float StreakInterval = 0.07f, BubbleInterval = 0.24f;

    TF2Player player;
    BushStealth stealth;
    HealthBase health;
    Vector3 lastPos;
    bool hasLast;
    float walked, side = 1f, nextStreak, nextBubble, flash;
    int lastHp = -1;
    readonly List<Renderer> rends = new List<Renderer>();
    readonly HashSet<Renderer> flashed = new HashSet<Renderer>();
    MaterialPropertyBlock mpb;
    static readonly int ColorId = Shader.PropertyToID("_Color"), ColorDimId = Shader.PropertyToID("_ColorDim");

    void Awake() { player = GetComponent<TF2Player>(); mpb = new MaterialPropertyBlock(); }

    void Start() { stealth = GetComponent<BushStealth>(); Hook(); }

    void Hook()
    {
        if (health != null || player == null || player.healthManager == null) return;
        health = player.healthManager;
        health.OnTakeDamage += OnHit;
        health.OnHealthChanged += OnHealth;
        lastHp = health.currentHealth;
    }

    void OnDestroy()
    {
        if (health != null) { health.OnTakeDamage -= OnHit; health.OnHealthChanged -= OnHealth; }
        EndFlash();
    }

    Vector3 Feet => player != null && player.remotePlayer != null ? player.remotePlayer.transform.position : transform.position;
    bool Dead => health == null || health.death;

    /// <summary>The local viewer cannot see this character: hidden in a bush or a cloaked Spy (enemy).</summary>
    bool Unseen
    {
        get
        {
            if (stealth != null && stealth.Hidden) return true;
            var viewer = ArenaManager.Instance != null ? ArenaManager.Instance.mainPlayer : null;
            return player.MeshInvisible && viewer != player;
        }
    }

    void OnHit(Vector3 dir)
    {
        if (Dead || Unseen) return;
        BrawlFx.Hit(Feet + Vector3.up * 1.0f, dir);
        flash = FlashTime;
    }

    void OnHealth(int cur, int max)
    {
        int prev = lastHp; lastHp = cur;
        if (prev < 0 || cur <= prev || Dead || Unseen) return;
        if (WaterHole.IsInside(Feet)) BrawlFx.Heal(Feet);   // healed by a water hole
    }

    void LateUpdate()
    {
        if (player == null) return;
        if (health == null) Hook();
        UpdateFlash();

        Vector3 feet = Feet;
        if (!hasLast || Dead) { lastPos = feet; hasLast = true; walked = 0f; return; }
        Vector3 d = feet - lastPos; d.y = 0f;
        lastPos = feet;
        float dist = d.magnitude;
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        if (dist > 3f) { walked = 0f; return; }   // respawn / teleport
        bool moving = dist / dt > MinSpeed;
        if (Unseen) { walked = 0f; return; }
        Vector3 dir = dist > 1e-4f ? d / dist : transform.forward;

        bool bog = BogPuddle.SpeedFactorAt(feet) < 0.999f;
        float current = OceanCurrent.FactorAt(feet, dir);
        bool boost = moving && (ToriiPath.FactorAt(feet) > 1.001f || current > 1.05f);
        bool against = moving && current < 0.95f;

        // continuous: speed lines, bubbles
        if (boost && Time.time >= nextStreak)
        {
            nextStreak = Time.time + StreakInterval;
            BrawlFx.Streak(feet, dir, current > 1.05f ? new Color(0.75f, 0.95f, 1f, 0.85f) : new Color(1f, 1f, 1f, 0.85f));
        }
        if ((bog || against) && Time.time >= nextBubble)
        {
            nextBubble = Time.time + BubbleInterval;
            BrawlFx.Bubble(feet, bog ? new Color(0.75f, 1f, 0.45f, 0.95f) : new Color(0.85f, 0.97f, 1f, 0.8f));
        }

        // steps
        if (!moving) { walked = Mathf.Min(walked, StepDistance * 0.6f); return; }
        walked += dist;
        if (walked < StepDistance) return;
        walked = 0f;
        side = -side;
        if (!Grounded(feet)) return;
        if (bog) BrawlFx.Mud(feet, dir);
        else if (WaterHole.IsInside(feet)) BrawlFx.Splash(feet, dir);
        else if (stealth == null || !stealth.InBush) BrawlFx.Dust(feet, dir, side);
    }

    static readonly RaycastHit[] hits = new RaycastHit[4];

    /// <summary>Ground right under the feet (no dust in the air: rocket jumps, falls).</summary>
    bool Grounded(Vector3 feet)
    {
        int n = Physics.RaycastNonAlloc(feet + Vector3.up * 0.4f, Vector3.down, hits, 0.75f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (!hits[i].collider.transform.IsChildOf(transform)) return true;
        return false;
    }

    // ---------------------------------------------------------------- hit flash
    void UpdateFlash()
    {
        if (flash <= 0f) return;
        flash -= Time.deltaTime;
        if (flash <= 0f || Dead || Unseen) { flash = 0f; EndFlash(); return; }
        if (flashed.Count == 0)
        {
            rends.Clear();
            var root = player.remotePlayer != null ? player.remotePlayer.transform : transform;
            root.GetComponentsInChildren(false, rends);
        }
        float k = flash / FlashTime;   // 1 -> 0
        Color c = Color.Lerp(Color.white, Color.white * FlashBoost, k); c.a = 1f;
        for (int i = 0; i < rends.Count; i++)
        {
            var r = rends[i];
            if (r == null || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || !r.enabled) continue;
            if (!flashed.Contains(r))
            {
                r.GetPropertyBlock(mpb);
                if (!mpb.isEmpty) continue;   // someone else's block (Spy cloak): leave it
                flashed.Add(r);
            }
            mpb.Clear();
            mpb.SetColor(ColorId, c);
            mpb.SetColor(ColorDimId, c * 0.85f);
            r.SetPropertyBlock(mpb);
        }
    }

    void EndFlash()
    {
        foreach (var r in flashed) if (r != null) r.SetPropertyBlock(null);
        flashed.Clear();
    }
}
