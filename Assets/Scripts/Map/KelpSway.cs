using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ocean maps: makes the kelp bushes (prefab KelpGrass, the Brawl leaf clump) sway gently in the water: each clump
/// tilts a few degrees back and forth around its foot, with its own phase. One component in the map scene (placed by
/// the map builder) drives them all; the kelp clumps are not static (static batching would freeze them).
/// Visual only: the bush areas (hiding) do not move.
/// </summary>
public class KelpSway : MonoBehaviour
{
    public float angle = 5f;        // degrees
    public float speed = 0.9f;      // radians per second
    public string match = "KelpGrass";

    readonly List<Transform> clumps = new List<Transform>();
    readonly List<Quaternion> rest = new List<Quaternion>();
    readonly List<float> phase = new List<float>();

    void Start()
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!t.name.StartsWith(match) || t.GetComponentInParent<KelpSway>() == this) continue;
            if (t.parent != null && t.parent.name.StartsWith(match)) continue;   // only the clump roots
            clumps.Add(t); rest.Add(t.localRotation);
            phase.Add(t.position.x * 0.7f + t.position.z * 0.45f);           // a wave rolling across the floor
        }
    }

    void Update()
    {
        float time = Time.time * speed;
        for (int i = 0; i < clumps.Count; i++)
        {
            var t = clumps[i];
            if (t == null) continue;
            float a = Mathf.Sin(time + phase[i]) * angle, b = Mathf.Cos(time * 0.73f + phase[i] * 1.3f) * angle * 0.6f;
            t.localRotation = Quaternion.Euler(a, 0f, b) * rest[i];
        }
    }
}
