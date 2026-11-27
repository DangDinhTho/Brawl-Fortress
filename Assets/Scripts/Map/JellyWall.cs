using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Candy maps: a wall of gumdrops (jelly). Bullets and rockets that hit it bounce off once (angle of incidence =
/// angle of reflection) instead of stopping, and the wall wobbles: bank shots around corners. Melee, flames, grenades
/// and sticky bombs are not affected.
/// No network message: projectiles fly on every client (only the shooter's client deals their damage) and the bounce
/// is plain geometry, the same on every client. BulletNormal.TryBounce asks Reflect; the wall only wobbles.
/// Added by the map builder (BrawlMapBuilder.PlaceJellyWalls) to the GumdropStack wall tiles; they are left out of
/// static batching so they can wobble.
/// </summary>
public class JellyWall : MonoBehaviour
{
    public float wobbleTime = 0.45f;
    public float wobbleAmount = 0.09f;

    static readonly List<JellyWall> all = new List<JellyWall>();
    Vector3 baseScale;
    float wobble;

    void Awake() { baseScale = transform.localScale; }
    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); transform.localScale = baseScale; }

    public static int Count => all.Count;

    /// <summary>Reflects dir off the jelly wall the projectile at pos has just entered (other = the collider it hit).
    /// Returns false if the collider is not part of a jelly wall. pos is moved just outside the wall.</summary>
    public static bool Reflect(Collider other, ref Vector3 pos, ref Vector3 dir)
    {
        var jelly = other.GetComponentInParent<JellyWall>();
        if (jelly == null) return false;
        Vector3 flat = new Vector3(dir.x, 0f, dir.z);
        if (flat.sqrMagnitude < 1e-6f) return false;
        flat.Normalize();
        Vector3 n;
        if (other.Raycast(new Ray(pos - flat * 1.5f, flat), out var hit, 3f) && Mathf.Abs(hit.normal.y) < 0.8f)
        {
            n = hit.normal; n.y = 0f; n.Normalize();
            pos = new Vector3(hit.point.x, pos.y, hit.point.z) + n * 0.12f;
        }
        else
        {   // already deep inside: push back the way it came
            n = -flat;
        }
        Vector3 r = Vector3.Reflect(flat, n);
        float y = dir.y;
        dir = new Vector3(r.x, 0f, r.z).normalized * Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        dir.y = y;
        jelly.Wobble(pos);
        return true;
    }

    public void Wobble(Vector3 at)
    {
        wobble = 1f;
        BrawlFx.Burst(at, new Color(1f, 0.55f, 0.75f, 1f), new Color(0.6f, 1f, 0.6f, 1f), 6, 3f);
    }

    void Update()
    {
        if (wobble <= 0f) return;
        wobble = Mathf.Max(0f, wobble - Time.deltaTime / wobbleTime);
        float s = Mathf.Sin((1f - wobble) * Mathf.PI * 5f) * wobble * wobbleAmount;
        transform.localScale = new Vector3(baseScale.x * (1f + s), baseScale.y * (1f - s * 1.2f), baseScale.z * (1f + s));
        if (wobble <= 0f) transform.localScale = baseScale;
    }
}
