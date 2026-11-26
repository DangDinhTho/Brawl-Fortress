using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Savanna maps: a water hole on the open plain that slowly heals whoever stands in it (every animal comes to the
/// water): players, bots and zombies; not bosses, not MvM robots. A hot spot to fight over.
/// Healing is done by the client that owns each character (its player, or the master client for bots), like the
/// eruption vents' damage: <see cref="HealthBase.Buff"/> raises the health and syncs it to the others. Only while the
/// match is running. One water hole drives the ticks for all of them.
/// Placed by the map builder (BrawlMapBuilder.PlaceWaterHoles); the prefab is made by SavannaThemeSetup (a flat mesh of
/// unit radius, BrawlFortress/Pond), scaled to the hole's size.
/// </summary>
public class WaterHole : MonoBehaviour
{
    [Tooltip("Share of max health healed per second while standing in the water")]
    [Range(0f, 0.25f)] public float healPerSecond = 0.06f;
    [Tooltip("Seconds between heal ticks")]
    public float interval = 0.5f;
    [Tooltip("Heal radius in local units (the mesh has radius 1 with a wobbly edge: a little inside it)")]
    public float radius = 0.9f;

    static readonly List<WaterHole> all = new List<WaterHole>();
    static readonly Collider[] buf = new Collider[64];
    static readonly HashSet<int> healed = new HashSet<int>();
    static float nextTick;

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    float WorldRadius => radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

    void Update()
    {
        if (all.Count == 0 || all[0] != this) return;   // one driver per scene
        if (Time.time < nextTick) return;
        nextTick = Time.time + interval;
        var arena = ArenaManager.Instance;
        if (arena == null || arena.gameState != GameState.Started) return;   // never before the match or after it
        healed.Clear();
        for (int i = 0; i < all.Count; i++) all[i].Heal();
    }

    void Heal()
    {
        Vector3 c = transform.position;
        float r = WorldRadius;
        int n = Physics.OverlapSphereNonAlloc(c + Vector3.up * 0.8f, r + 0.8f, buf, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var hm = buf[i].GetComponentInParent<HealthBase>();
            if (hm == null || hm.death || hm.photonView == null || !hm.photonView.IsMine) continue;
            if (hm.currentHealth >= hm.maxHealth) continue;
            if (hm is RobotHealth || hm.GetComponentInParent<TF2Boss>() != null) continue;   // robots and bosses do not drink
            if (!healed.Add(hm.photonView.ViewID)) continue;                                  // once per tick, even in two holes
            Vector3 d = hm.transform.position - c;
            float up = d.y; d.y = 0f;
            if (d.magnitude > r || up < -1f || up > 1.5f) { healed.Remove(hm.photonView.ViewID); continue; }   // feet in the water
            hm.Buff(healPerSecond * interval);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 0.5f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, WorldRadius);
    }
#endif
}
