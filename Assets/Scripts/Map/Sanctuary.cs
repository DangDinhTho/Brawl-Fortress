using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Greek maps: a sanctuary in the middle of the map (two mirrored ones beside the middle when it is taken: the central
/// monument of a layout, the King of the Hill point, a payload / robot track), built like a small ruined tholos: a
/// stepped round marble platform (krepidoma) laid with a laurel mosaic, marble slabs and a red-figure key border
/// (BrawlFortress/Sanctuary), fluted Doric column stumps around its rim (cover), an altar (bomos) with the sacred
/// flame in the middle. Whoever stands on the platform is slowly healed (like the Savanna water holes: healPerSecond of
/// max health, ticks of 'interval'), gold "+" signs rise around them (CharacterFx); the flame flickers and throws
/// embers, gold motes drift up. Players, bots and zombies; not bosses, not MvM robots. A hot spot to fight over.
/// Healing is done by the client that owns each character (HealthBase.Buff syncs it), only while the match is running;
/// no network message. The stone parts are scene objects (BrawlMapBuilder.PlaceSanctuary, meshes SanctuaryArt, in the
/// NavMesh); this object carries the logic and the flame.
/// </summary>
public class Sanctuary : MonoBehaviour
{
    public float radius = 2f;
    [Range(0f, 0.25f)] public float healPerSecond = 0.06f;
    public float interval = 0.5f;
    [Tooltip("Height of the platform's top (the floor the characters stand on)")]
    public float floorY = 0.14f;
    public Transform flame;

    static readonly List<Sanctuary> all = new List<Sanctuary>();
    static readonly Collider[] buf = new Collider[64];
    static readonly HashSet<int> healed = new HashSet<int>();
    static float nextTick;
    float nextMote, nextEmber, seed;
    Vector3 flameScale;

    public static readonly Color Gold = new Color(1f, 0.84f, 0.38f, 1f);
    static readonly Color Ember = new Color(1f, 0.55f, 0.18f, 1f), EmberHot = new Color(1f, 0.85f, 0.45f, 1f);

    void Awake()
    {
        seed = Random.value * 10f;
        if (flame) flameScale = flame.localScale;
    }

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    /// <summary>True when feet at p stand on a sanctuary (the heal test; CharacterFx shows gold "+" there).</summary>
    public static bool IsInside(Vector3 p)
    {
        for (int i = 0; i < all.Count; i++)
        {
            var s = all[i];
            Vector3 d = p - s.transform.position;
            float up = d.y; d.y = 0f;
            if (d.sqrMagnitude <= s.radius * s.radius && up > -1f && up < 1.5f) return true;
        }
        return false;
    }

    void Update()
    {
        // the sacred flame: flickers and sways, throws embers
        if (flame)
        {
            float t = Time.time * 7f + seed;
            float k = 0.88f + 0.08f * Mathf.Sin(t) + 0.06f * Mathf.Sin(t * 2.3f + 1.1f) + 0.04f * Mathf.PerlinNoise(t * 0.7f, seed);
            flame.localScale = new Vector3(flameScale.x * (1.05f - 0.1f * (k - 0.88f) * 5f), flameScale.y * k, flameScale.z * (1.05f - 0.1f * (k - 0.88f) * 5f));
            flame.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.6f) * 4f, Time.time * 25f, Mathf.Cos(t * 0.5f) * 4f);
            if (Time.time >= nextEmber)
            {
                nextEmber = Time.time + 0.12f;
                BrawlFx.Mote(flame.position + Vector3.up * 0.35f + Random.insideUnitSphere * 0.12f, Random.value < 0.5f ? Ember : EmberHot);
            }
        }
        // gold motes drifting up over the marble
        if (Time.time >= nextMote)
        {
            nextMote = Time.time + 0.2f;
            Vector2 q = Random.insideUnitCircle * radius * 0.85f;
            BrawlFx.Mote(transform.position + new Vector3(q.x, floorY + 0.1f, q.y), Gold);
        }

        if (all.Count == 0 || all[0] != this) return;   // one driver per scene
        if (Time.time < nextTick) return;
        nextTick = Time.time + interval;
        if (!MapMechanics.Running) return;
        healed.Clear();
        for (int i = 0; i < all.Count; i++) all[i].Heal();
    }

    void Heal()
    {
        Vector3 c = transform.position;
        int n = Physics.OverlapSphereNonAlloc(c + Vector3.up * 0.8f, radius + 0.8f, buf, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var hm = buf[i].GetComponentInParent<HealthBase>();
            if (hm == null || hm.death || hm.photonView == null || !hm.photonView.IsMine) continue;
            if (hm.currentHealth >= hm.maxHealth) continue;
            if (hm is RobotHealth || hm.GetComponentInParent<TF2Boss>() != null) continue;
            int id = hm.photonView.ViewID;
            if (healed.Contains(id)) continue;   // once per tick, even on two sanctuaries
            Vector3 d = hm.transform.position - c;
            float up = d.y; d.y = 0f;
            if (d.magnitude > radius || up < -1f || up > 1.5f) continue;   // feet on the platform
            healed.Add(id);
            hm.Buff(healPerSecond * interval);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.4f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
