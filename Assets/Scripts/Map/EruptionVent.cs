using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Volcano maps: a lava vent in the floor that erupts on a fixed cycle. Every client reads the same clock
/// (PhotonNetwork.Time, also in Photon OfflineMode), so the vents erupt at the same moment everywhere without a network
/// message. Before each eruption the ground warns: the vent's cracks brighten and a red disc fills, from the middle out,
/// the area that will be hit. Mirrored vents of the two halves share a phase (fair for both teams).
/// Damage: dealt by the client that owns each character (its player, or the master client for bots and robots), once
/// per eruption, a share of the character's max health, like the DeathWall (no attacker, ownerId -1). Bosses are immune.
/// Placed by the map builder (BrawlMapBuilder.PlaceVents); the prefab is made by VolcanoThemeSetup.
/// </summary>
public class EruptionVent : MonoBehaviour
{
    [Header("Cycle (seconds)")]
    public float period = 10f;
    public float warnTime = 2.2f;
    public float eruptTime = 1.3f;
    [Tooltip("Offset on the shared clock: mirrored vents use the same phase")]
    public float phase;

    [Header("Hit")]
    public float radius = 1.7f;
    [Range(0f, 1f)] public float damageFraction = 0.35f;

    [Header("Parts")]
    public Renderer glow;            // the vent's cracks (BrawlFortress/LavaGlow, _Boost)
    public Renderer warnDisc;        // BrawlFortress/VentWarn (_Fill, _Alpha)
    public Transform fountain;       // lava column, scaled up while erupting (unit height)
    public ParticleSystem embers;    // burst when it erupts
    public ParticleSystem sputter;   // a few sparks while it warns
    public float fountainHeight = 4.2f;
    public float fountainWidth = 1.7f;

    static readonly int BoostId = Shader.PropertyToID("_Boost");
    static readonly int FillId = Shader.PropertyToID("_Fill");
    static readonly int AlphaId = Shader.PropertyToID("_Alpha");

    MaterialPropertyBlock mpbGlow, mpbWarn;
    long lastCycle = long.MinValue;
    bool wasErupting, wasWarning;
    float nextCheck;
    readonly HashSet<int> hit = new HashSet<int>();
    readonly Collider[] buf = new Collider[48];

    void Awake()
    {
        mpbGlow = new MaterialPropertyBlock();
        mpbWarn = new MaterialPropertyBlock();
        if (fountain) fountain.gameObject.SetActive(false);
        if (warnDisc) warnDisc.enabled = false;
    }

    double Clock => PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode ? PhotonNetwork.Time : Time.timeSinceLevelLoad;

    void Update()
    {
        double t = Clock + phase;
        long cycle = (long)System.Math.Floor(t / period);
        float u = (float)(t - cycle * (double)period);                     // 0 .. period
        if (cycle != lastCycle) { lastCycle = cycle; hit.Clear(); }

        bool erupting = u < eruptTime;                                       // [0, erupt): eruption
        float warnStart = period - warnTime;                                 // [period - warn, period): warning
        bool warning = u >= warnStart;
        float w = warning ? (u - warnStart) / warnTime : 0f;
        float e = erupting ? u / eruptTime : 0f;

        // cracks: a slow breath when idle, brighter and flickering as the eruption comes, white-hot while erupting
        float boost = 1f + 0.08f * Mathf.Sin(Time.time * 1.3f + phase);
        if (warning) boost = 1f + 1.1f * w + 0.25f * w * Mathf.Sin(Time.time * 22f);
        if (erupting) boost = Mathf.Lerp(2.2f, 1.1f, e * e);
        if (glow) { glow.GetPropertyBlock(mpbGlow); mpbGlow.SetFloat(BoostId, boost); glow.SetPropertyBlock(mpbGlow); }

        if (warnDisc)
        {
            bool show = warning || erupting;
            if (warnDisc.enabled != show) warnDisc.enabled = show;
            if (show)
            {
                warnDisc.GetPropertyBlock(mpbWarn);
                mpbWarn.SetFloat(FillId, warning ? Mathf.SmoothStep(0f, 1f, w) : 1f);
                mpbWarn.SetFloat(AlphaId, warning ? 0.75f + 0.25f * Mathf.Sin(Time.time * 14f) : 1f - e);
                warnDisc.SetPropertyBlock(mpbWarn);
            }
        }

        if (fountain)
        {
            if (fountain.gameObject.activeSelf != erupting) fountain.gameObject.SetActive(erupting);
            if (erupting)
            {
                // shoots up fast, holds with a little wobble, falls back
                float h = e < 0.12f ? e / 0.12f : e > 0.72f ? 1f - (e - 0.72f) / 0.28f : 1f;
                h = Mathf.Max(0.02f, h * (0.94f + 0.06f * Mathf.Sin(Time.time * 18f)));
                float wd = (0.85f + 0.25f * h) * fountainWidth;
                fountain.localScale = new Vector3(wd, fountainHeight * h, wd);
            }
        }

        if (warning && !wasWarning && sputter) sputter.Play();
        if (erupting && !wasErupting && embers) embers.Play();
        wasWarning = warning; wasErupting = erupting;

        if (erupting && Time.time >= nextCheck) { nextCheck = Time.time + 0.1f; Burn(); }
    }

    void Burn()
    {
        var arena = ArenaManager.Instance;
        if (arena == null || arena.gameState != GameState.Started) return;   // never before the match or after it
        Vector3 c = transform.position;
        int n = Physics.OverlapSphereNonAlloc(c + Vector3.up * 1f, radius + 0.6f, buf, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var hm = buf[i].GetComponentInParent<HealthManager>();
            if (hm == null || hm.death || hm.photonView == null || !hm.photonView.IsMine) continue;
            if (hit.Contains(hm.photonView.ViewID)) continue;
            if (hm.GetComponentInParent<TF2Boss>() != null) continue;
            Vector3 p = hm.centerBody != null ? hm.centerBody.position : hm.transform.position;
            Vector3 d = p - c; float up = d.y; d.y = 0f;
            if (d.magnitude > radius || up < -1f || up > 3.5f) continue;
            hit.Add(hm.photonView.ViewID);
            var dmg = new DamageObject
            {
                damage = Mathf.Max(1, Mathf.RoundToInt(hm.maxHealth * damageFraction)),
                ownerId = -1,
                isOwner = true,
                team = ConfigManager.GetEnemyTeam(hm.team),
            };
            Vector3 dir = (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward) * 2f + Vector3.up;
            hm.TakeDamage(dmg, dir);
            BurnFx.Ignite(hm);   // visual only (no source: no afterburn damage)
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
