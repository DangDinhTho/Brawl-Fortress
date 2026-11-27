using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ice maps: a patch of polished clear ice on the floor. Characters on it slide: they speed up and slow down slowly
/// (traction: a share of the normal acceleration, so stopping takes about 0.9 s instead of 0.25 s and turns drift),
/// with a little more top speed (speedBoost). The player through PlayerFPSController.Move (traction) and
/// TerrainSpeed.FactorAt (speed); NavMesh agents through TerrainSpeedAgent (acceleration and speed). No network
/// message: the patches are part of the map scene and positions are synced as usual.
/// Placed by the map builder (BrawlMapBuilder.PlaceSlickIce); the mesh is built at runtime (MechMesh.Patch,
/// BrawlFortress/SlickIce) from radius / wobble / seed, so mirrored patches look alike.
/// </summary>
public class SlickIce : MonoBehaviour
{
    public float radius = 2f;
    [Range(0f, 0.4f)] public float wobble = 0.16f;
    public int seed;
    [Range(0.05f, 1f)] public float traction = 0.28f;
    public float speedBoost = 1.12f;

    static readonly List<SlickIce> all = new List<SlickIce>();

    void Awake()
    {
        MechMesh.Child(transform, "IceSheet", MechMesh.Patch(radius, wobble, seed), MechMesh.Ice, 0.015f);
    }

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    static SlickIce Under(Vector3 p)
    {
        for (int i = 0; i < all.Count; i++)
        {
            var s = all[i];
            Vector3 d = p - s.transform.position;
            if (d.y < -1f || d.y > 1.2f) continue;
            d.y = 0f;
            float r = s.radius * 0.9f;
            if (d.sqrMagnitude <= r * r) return s;
        }
        return null;
    }

    /// <summary>Share of the normal acceleration for a character with its feet at p: 1 off the ice.</summary>
    public static float TractionAt(Vector3 p) { var s = all.Count > 0 ? Under(p) : null; return s != null ? s.traction : 1f; }

    /// <summary>Top speed multiplier at p: 1 off the ice.</summary>
    public static float SpeedFactorAt(Vector3 p) { var s = all.Count > 0 ? Under(p) : null; return s != null ? s.speedBoost : 1f; }

    public static bool IsOn(Vector3 p) => all.Count > 0 && Under(p) != null;

    void Update()
    {
        if (all.Count > 0 && all[0] == this) TerrainSpeed.EnsureAgents();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
