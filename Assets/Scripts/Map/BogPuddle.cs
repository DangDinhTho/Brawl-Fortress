using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Swamp maps: a puddle of bog mud on the floor that slows whoever wades through it (players, bots, robots, zombies,
/// bosses), a hot spot for fights. Movement is owned by each character's client (the player's own, the master client
/// for bots), so every client only slows what it moves: the local player through
/// <see cref="SpeedFactorAt"/> (PlayerFPSController), NavMesh agents through a <see cref="MudSlowAgent"/> added at run
/// time. No network message: the puddles are part of the map scene.
/// Placed by the map builder (BrawlMapBuilder.PlaceBogPuddles); the prefab is made by SwampThemeSetup (a flat mesh of
/// unit radius, BrawlFortress/Bog), scaled to the puddle's size.
/// </summary>
public class BogPuddle : MonoBehaviour
{
    [Range(0.1f, 1f)] public float speedFactor = 0.6f;
    [Tooltip("Slow radius in local units (the mesh has radius 1 with a wobbly edge: a little inside it)")]
    public float radius = 0.94f;

    static readonly List<BogPuddle> all = new List<BogPuddle>();
    static float nextAgentScan;

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    float WorldRadius => radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

    /// <summary>Speed multiplier at a character's feet: 1 outside every puddle.</summary>
    public static float SpeedFactorAt(Vector3 p)
    {
        float f = 1f;
        for (int i = 0; i < all.Count; i++)
        {
            var b = all[i];
            Vector3 d = p - b.transform.position;
            if (d.y < -1f || d.y > 1.2f) continue;          // jumping over it, or on something above
            d.y = 0f;
            float r = b.WorldRadius;
            if (d.sqrMagnitude < r * r) f = Mathf.Min(f, b.speedFactor);
        }
        return f;
    }

    void Update()
    {
        // one puddle looks for new NavMesh agents now and then (bots spawn and respawn during the match)
        if (all.Count == 0 || all[0] != this || Time.unscaledTime < nextAgentScan) return;
        nextAgentScan = Time.unscaledTime + 1f;
        foreach (var ag in FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None))
            if (ag.enabled && ag.GetComponent<MudSlowAgent>() == null) ag.gameObject.AddComponent<MudSlowAgent>();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.4f, 0.1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, WorldRadius);
    }
#endif
}
