using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ocean maps: a current lane on the sea floor. Moving along it (its +Z) makes characters faster, against it slower,
/// across it changes nothing: a fast lane forward for each team (the lanes are mirrored, both flowing towards the
/// middle). The local player through <see cref="TerrainSpeed.FactorAt"/> (PlayerFPSController), NavMesh agents
/// through a <see cref="TerrainSpeedAgent"/>. No network message: the lanes are part of the map scene.
/// The lane is the object's local box: x and z in [-0.5, 0.5] (the builder scales the object to the lane's width and
/// length, y stays 1). Placed by BrawlMapBuilder.PlaceCurrents; prefab made by OceanThemeSetup (BrawlFortress/Current).
/// </summary>
public class OceanCurrent : MonoBehaviour
{
    [Tooltip("Downstream x(1 + strength), upstream x(1 - strength)")]
    [Range(0f, 0.8f)] public float strength = 0.35f;

    static readonly List<OceanCurrent> all = new List<OceanCurrent>();

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    /// <summary>Speed multiplier for a character at p moving along moveDir: 1 outside every lane or when not moving.</summary>
    public static float FactorAt(Vector3 p, Vector3 moveDir)
    {
        moveDir.y = 0f;
        if (all.Count == 0 || moveDir.sqrMagnitude < 0.0001f) return 1f;
        moveDir.Normalize();
        float f = 1f;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            var t = c.transform;
            Vector3 d = p - t.position;
            if (d.y < -1f || d.y > 1.5f) continue;
            Vector3 l = t.InverseTransformPoint(p);
            if (Mathf.Abs(l.x) > 0.5f || Mathf.Abs(l.z) > 0.5f) continue;
            Vector3 fwd = t.forward; fwd.y = 0f; fwd.Normalize();
            f *= 1f + c.strength * Vector3.Dot(moveDir, fwd);
        }
        return f;
    }

    void Update()
    {
        if (all.Count > 0 && all[0] == this) TerrainSpeed.EnsureAgents();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.7f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0.2f, 1f));
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.5f);
    }
#endif
}
