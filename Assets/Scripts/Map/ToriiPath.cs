using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Japan maps: a stone path under a row of small torii (like the senbon torii tunnels). Running along it, either way,
/// makes characters faster (players, bots, robots, zombies, bosses): a quick route for both teams, a lane to fight over.
/// The local player through <see cref="TerrainSpeed.FactorAt"/> (PlayerFPSController), NavMesh agents through a
/// <see cref="TerrainSpeedAgent"/>. No network message: the paths are part of the map scene.
/// The path is the object's local box: x and z in [-0.5, 0.5] (the builder scales the object to the path's width and
/// length, y stays 1); the torii and the stone slabs are separate props. Placed by BrawlMapBuilder.PlaceToriiPaths;
/// prefab made by JapanThemeSetup.
/// </summary>
public class ToriiPath : MonoBehaviour
{
    [Tooltip("Speed multiplier on the path (both directions)")]
    [Range(1f, 2f)] public float speedFactor = 1.3f;

    static readonly List<ToriiPath> all = new List<ToriiPath>();

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    /// <summary>Speed multiplier for a character at p: 1 off every path.</summary>
    public static float FactorAt(Vector3 p)
    {
        float f = 1f;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            var t = c.transform;
            float dy = p.y - t.position.y;
            if (dy < -1f || dy > 1.5f) continue;
            Vector3 l = t.InverseTransformPoint(p);
            if (Mathf.Abs(l.x) > 0.5f || Mathf.Abs(l.z) > 0.5f) continue;
            f = Mathf.Max(f, c.speedFactor);
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
        Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.7f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0.2f, 1f));
    }
#endif
}
