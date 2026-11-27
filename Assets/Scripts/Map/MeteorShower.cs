using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lunar maps: a meteor shower. Every 'period' seconds a few meteors come down on the open floor: a warning disc fills
/// where each will hit (warnTime), the glowing rock streaks down out of the sky in the last fallTime, and the impact
/// hurts everyone within 'radius' (damageFraction of max health; not bosses), tears the grass and leaves a glowing
/// crater for a moment. The spots change every wave, in mirrored pairs (fair for both teams).
/// No network message: the wave number comes from the shared clock (MapMechanics.Clock) and seeds the choice of spots,
/// so every client sees the same meteors at the same time; each client damages the characters it owns (like the
/// Volcano vents) and the master client tears the grass (BushDamage sends it to the others).
/// Placed by the map builder (BrawlMapBuilder.PlaceMeteors): 'spots' = candidate points of one half (mirrored here),
/// 'rock' = a hidden moon rock model to clone, warnMaterial = BrawlFortress/VentWarn (Resources/Materials/MeteorWarn).
/// </summary>
public class MeteorShower : MonoBehaviour
{
    public Vector3[] spots;
    public int seed;
    public Transform rock;
    public Material warnMaterial;
    public Material trailMaterial;
    public float period = 18f;
    public float phase = 6f;
    public int pairs = 2;
    public float warnTime = 1.8f;
    public float fallTime = 0.5f;
    public float radius = 1.6f;
    [Range(0f, 1f)] public float damageFraction = 0.3f;
    public float craterTime = 2.2f;

    class Slot
    {
        public Vector3 at;
        public MeshRenderer warn, crater;
        public Transform rock;
        public TrailRenderer trail;
        public bool struck;
        public readonly HashSet<int> hit = new HashSet<int>();
    }

    static readonly int FillId = Shader.PropertyToID("_Fill"), AlphaId = Shader.PropertyToID("_Alpha");
    readonly List<Slot> slots = new List<Slot>();
    readonly List<int> pick = new List<int>();
    MaterialPropertyBlock mpb;
    long wave = long.MinValue;
    static readonly Vector3 FallFrom = new Vector3(4.5f, 17f, 3f);

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        if (rock) rock.gameObject.SetActive(false);
        var quad = Quad(radius);
        var craterMesh = MechMesh.SoftDisc(radius * 0.9f, new Color(1f, 0.55f, 0.18f, 0.75f));
        for (int i = 0; i < pairs * 2; i++)
        {
            var s = new Slot();
            var root = new GameObject("Meteor" + i).transform;
            root.SetParent(transform, false);
            s.warn = MechMesh.Child(root, "Warn", quad, warnMaterial, 0.04f);
            s.warn.enabled = false;
            s.crater = MechMesh.Child(root, "Crater", craterMesh, MechMesh.Decal, 0.035f);
            s.crater.enabled = false;
            if (rock)
            {
                s.rock = Instantiate(rock, transform);
                s.rock.name = "MeteorRock" + i;
                s.rock.gameObject.SetActive(false);
                foreach (var c in s.rock.GetComponentsInChildren<Collider>(true)) Destroy(c);
                s.trail = s.rock.gameObject.AddComponent<TrailRenderer>();
                s.trail.sharedMaterial = trailMaterial;
                s.trail.time = 0.32f;
                s.trail.minVertexDistance = 0.2f;
                s.trail.widthCurve = AnimationCurve.Linear(0f, 1.1f, 1f, 0f);
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.45f, 0.12f), 0.5f), new GradientColorKey(new Color(0.8f, 0.15f, 0.05f), 1f) },
                          new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
                s.trail.colorGradient = g;
                s.trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                s.trail.receiveShadows = false;
            }
            slots.Add(s);
        }
    }

    static Mesh Quad(float r)
    {
        var m = new Mesh { name = "MeteorWarnQuad" };
        m.vertices = new[] { new Vector3(-r, 0, -r), new Vector3(-r, 0, r), new Vector3(r, 0, r), new Vector3(r, 0, -r) };
        m.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        m.RecalculateBounds();
        return m;
    }

    void NewWave(long k)
    {
        wave = k;
        pick.Clear();
        if (spots == null || spots.Length == 0) return;
        var rnd = new System.Random(unchecked(seed * 7919 + (int)k * 131 + 17));
        for (int tries = 0; pick.Count < pairs && tries < 40; tries++)
        {
            int i = rnd.Next(spots.Length);
            bool ok = true;
            foreach (int j in pick) if ((spots[j] - spots[i]).sqrMagnitude < 25f || (spots[j] + spots[i]).sqrMagnitude < 25f) { ok = false; break; }
            if (ok) pick.Add(i);
        }
        for (int s = 0; s < slots.Count; s++)
        {
            var sl = slots[s];
            int pi = s / 2;
            sl.struck = false; sl.hit.Clear();
            if (pi >= pick.Count) { sl.at = new Vector3(0f, -100f, 0f); continue; }
            Vector3 p = spots[pick[pi]];
            sl.at = s % 2 == 0 ? p : new Vector3(-p.x, p.y, -p.z);
            sl.warn.transform.parent.position = sl.at;
        }
    }

    void Update()
    {
        double t = MapMechanics.Clock + phase;
        long k = (long)System.Math.Floor(t / period);
        float u = (float)(t - k * (double)period);
        if (k != wave) NewWave(k);
        foreach (var s in slots)
        {
            if (s.at.y < -50f) { s.warn.enabled = false; s.crater.enabled = false; if (s.rock) s.rock.gameObject.SetActive(false); continue; }
            bool warning = u < warnTime;
            if (s.warn.enabled != warning) s.warn.enabled = warning;
            if (warning)
            {
                float w = u / warnTime;
                s.warn.GetPropertyBlock(mpb);
                mpb.SetFloat(FillId, Mathf.SmoothStep(0f, 1f, w));
                mpb.SetFloat(AlphaId, 0.8f + 0.2f * Mathf.Sin(Time.time * 14f));
                s.warn.SetPropertyBlock(mpb);
            }
            if (s.rock)
            {
                float f = (u - (warnTime - fallTime)) / fallTime;
                bool falling = f >= 0f && f < 1f;
                if (s.rock.gameObject.activeSelf != falling)
                {
                    s.rock.gameObject.SetActive(falling);
                    if (falling && s.trail) { s.rock.position = s.at + FallFrom; s.trail.Clear(); }
                }
                if (falling) { s.rock.position = s.at + FallFrom * (1f - f) + Vector3.up * 0.3f; s.rock.Rotate(220f * Time.deltaTime, 160f * Time.deltaTime, 0f); }
            }
            if (!s.struck && u >= warnTime)
            {
                s.struck = true;
                Strike(s);
            }
            float c = u - warnTime;
            bool crater = c >= 0f && c < craterTime;
            if (s.crater.enabled != crater) s.crater.enabled = crater;
            if (crater)
            {
                s.crater.GetPropertyBlock(mpb);
                mpb.SetFloat(AlphaId, 1f - c / craterTime);
                s.crater.SetPropertyBlock(mpb);
            }
        }
    }

    void Strike(Slot s)
    {
        BrawlFx.Burst(s.at + Vector3.up * 0.3f, new Color(1f, 0.85f, 0.4f, 1f), new Color(1f, 0.4f, 0.12f, 1f), 18, 7f);
        BrawlFx.DustRing(s.at, radius, new Color(0.8f, 0.78f, 0.82f, 0.85f), 14);
        MapMechanics.HurtArea(s.at, radius, damageFraction, s.hit);
        BushDamage.Explosion(s.at, radius, 300f, MapMechanics.Authority);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (spots == null) return;
        Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.8f);
        foreach (var p in spots) { Gizmos.DrawWireSphere(p, radius); Gizmos.DrawWireSphere(new Vector3(-p.x, p.y, -p.z), radius); }
    }
#endif
}
