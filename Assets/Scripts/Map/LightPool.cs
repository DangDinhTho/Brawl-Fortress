using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Xmas night maps: the warm light pools under the lamps, trees and light fences. A character hiding in a bush inside
/// a pool of light is seen (BushStealth.IsHiddenFrom): the lamps light up the grass around them. Added to the WarmPool
/// quads by the map builder (radius = a share of the quad's size).
/// </summary>
public class LightPool : MonoBehaviour
{
    public float radius = 1.6f;
    static readonly List<LightPool> all = new List<LightPool>();

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    public static bool Lit(Vector3 p)
    {
        for (int i = 0; i < all.Count; i++)
        {
            var l = all[i];
            Vector3 d = p - l.transform.position; d.y = 0f;
            if (d.sqrMagnitude <= l.radius * l.radius) return true;
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.4f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
