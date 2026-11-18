using UnityEngine;

/// <summary>
/// Draws the medigun beam (replaces DrawLineRender on the medigun's LineHealthBuff object).
///  - healing a teammate (the Target transform is parented to someone else): a soft curved beam from the gun to him;
///  - no teammate: a short soft "rope" in front of the gun, simulated with inertia (verlet), so it sways when the medic
///    runs or turns and settles back forward. The beam never bends back to the medic himself.
/// A thin bright core line is added on top of the team-coloured beam. Colours come from the LineRenderer's gradient
/// (Medicgun.InitData puts the blue gradient on blue team).
/// </summary>
[DisallowMultipleComponent]
public class MedigunBeam : MonoBehaviour
{
    public Transform origin;      // gun muzzle
    public Transform forwardHint; // a point in front of the muzzle (gives the rest direction of the rope)
    public Transform target;      // Medicgun.target: parented to the healed teammate, or to the medic when idle
    public LineRenderer line;

    [Header("Beam to a teammate")]
    public int beamPoints = 16;
    public float beamWidthStart = 0.38f, beamWidthEnd = 0.3f;
    [Range(0f, 1f)] public float beamAlpha = 0.85f;

    [Header("Idle rope")]
    public int ropePoints = 10;
    public float ropeLength = 1.8f;
    public float ropeWidthStart = 0.28f, ropeWidthEnd = 0.05f;
    [Tooltip("Pull of each rope point back towards the straight forward shape (0..1 per step)")]
    public float ropeStiffness = 0.12f;
    [Tooltip("Velocity kept each step (inertia)")]
    public float ropeDamping = 0.86f;
    public float ropeGravity = 1.5f;

    [Header("Heal effect on the teammate")]
    public GameObject healFxPrefab;

    [Header("Core")]
    [Range(0f, 1f)] public float coreWidth = 0.35f;
    [Range(0f, 1f)] public float coreWhiten = 0.6f;

    HealthManager owner;
    Vector3[] rope, ropePrev, beam;
    LineRenderer core;
    Color colStart = Color.red, colEnd = Color.red;
    bool ropeValid;
    int lastMode = -1;

    GameObject fx; HealthManager fxOn;

    /// <summary>Plays the heal effect on the healed teammate, stops it (particles fade out) otherwise.</summary>
    void HealFx(HealthManager on)
    {
        if (healFxPrefab == null) return;
        if (on != null)
        {
            if (fx == null) fx = Instantiate(healFxPrefab);
            if (fxOn != on)
            {
                fx.transform.SetParent(on.transform, false);
                fx.transform.localPosition = Vector3.zero; fx.transform.localRotation = Quaternion.identity;
                fxOn = on;
            }
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>()) if (!ps.isEmitting) ps.Play(false);
        }
        else if (fx != null)
        {
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>()) if (ps.isEmitting) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            fxOn = null;
        }
    }

    void OnDisable() { HealFx(null); }

    void OnDestroy() { if (fx != null) Destroy(fx); }

    void OnEnable()
    {
        ropeValid = false;
        lastMode = -1;
        if (line != null)
        {
            var keys = line.colorGradient.colorKeys;
            if (keys.Length > 0) { colStart = keys[0].color; colEnd = keys[keys.Length - 1].color; }
        }
    }

    void EnsureCore()
    {
        if (core != null || line == null) return;
        var go = new GameObject("BeamCore");
        go.transform.SetParent(line.transform, false);
        core = go.AddComponent<LineRenderer>();
        core.sharedMaterial = line.sharedMaterial;
        core.useWorldSpace = true;
        core.textureMode = line.textureMode;
        core.alignment = line.alignment;
        core.numCapVertices = 2;
        core.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        core.receiveShadows = false;
        core.sortingOrder = line.sortingOrder + 1;
    }

    static Gradient Grad(Color a, Color b, float alphaStart, float alphaEnd)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                  new[] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaEnd, 1f) });
        return g;
    }

    void Style(bool toTeammate)
    {
        int mode = toTeammate ? 1 : 0;
        if (mode == lastMode) return;
        lastMode = mode;
        float a0 = beamAlpha, a1 = toTeammate ? beamAlpha : 0f;
        line.colorGradient = Grad(colStart, colEnd, a0, a1);
        line.widthMultiplier = 1f;
        line.widthCurve = AnimationCurve.Linear(0f, toTeammate ? beamWidthStart : ropeWidthStart, 1f, toTeammate ? beamWidthEnd : ropeWidthEnd);
        EnsureCore();
        if (core != null)
        {
            core.colorGradient = Grad(Color.Lerp(colStart, Color.white, coreWhiten), Color.Lerp(colEnd, Color.white, coreWhiten), 0.95f, toTeammate ? 0.95f : 0f);
            core.widthCurve = AnimationCurve.Linear(0f, (toTeammate ? beamWidthStart : ropeWidthStart) * coreWidth, 1f, (toTeammate ? beamWidthEnd : ropeWidthEnd) * coreWidth);
        }
    }

    void LateUpdate()
    {
        if (line == null || origin == null) return;
        line.useWorldSpace = true;
        Vector3 o = origin.position;
        Vector3 fwd = forwardHint != null ? (forwardHint.position - o) : origin.forward;
        if (fwd.sqrMagnitude < 1e-4f) fwd = origin.forward;
        fwd.Normalize();

        // healing only when the target hangs on SOMEONE ELSE (the local player's gun may live under the camera rig,
        // so comparing transform roots is not enough)
        // the weapon's bones are moved onto the character's hand at runtime (AlignBones), so the beam is no longer under
        // the Medicgun object: its owner is the character it hangs on
        if (owner == null) owner = GetComponentInParent<HealthManager>();
        var holder = target != null ? target.GetComponentInParent<HealthManager>() : null;
        bool toTeammate = holder != null && holder != owner && !holder.death;
        HealFx(toTeammate ? holder : null);
        Style(toTeammate);

        Vector3[] pts;
        if (toTeammate)
        {
            ropeValid = false;
            if (beam == null || beam.Length != beamPoints) beam = new Vector3[beamPoints];
            Vector3 end = target.position;
            float dist = Vector3.Distance(o, end);
            Vector3 ctrl = o + fwd * dist * 0.4f; // leaves the gun forward, then bends to the teammate
            for (int i = 0; i < beamPoints; i++)
            {
                float t = i / (beamPoints - 1f);
                beam[i] = Vector3.Lerp(Vector3.Lerp(o, ctrl, t), Vector3.Lerp(ctrl, end, t), t);
            }
            pts = beam;
        }
        else
        {
            SimulateRope(o, fwd);
            pts = rope;
        }

        line.positionCount = pts.Length;
        line.SetPositions(pts);
        if (core != null)
        {
            core.positionCount = pts.Length;
            core.SetPositions(pts);
        }
    }

    void SimulateRope(Vector3 o, Vector3 fwd)
    {
        int n = Mathf.Max(3, ropePoints);
        float seg = ropeLength / (n - 1);
        if (rope == null || rope.Length != n) { rope = new Vector3[n]; ropePrev = new Vector3[n]; ropeValid = false; }
        if (!ropeValid)
        {
            for (int i = 0; i < n; i++) rope[i] = ropePrev[i] = o + fwd * seg * i;
            ropeValid = true;
        }
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        rope[0] = o; ropePrev[0] = o;
        for (int i = 1; i < n; i++)
        {
            Vector3 vel = (rope[i] - ropePrev[i]) * ropeDamping;
            ropePrev[i] = rope[i];
            Vector3 rest = o + fwd * seg * i;
            rope[i] += vel + Vector3.down * ropeGravity * dt * dt + (rest - rope[i]) * ropeStiffness;
        }
        // keep the segment lengths (a few relaxation passes)
        for (int pass = 0; pass < 3; pass++)
        {
            rope[0] = o;
            for (int i = 1; i < n; i++)
            {
                Vector3 d = rope[i] - rope[i - 1];
                float len = d.magnitude;
                if (len < 1e-5f) continue;
                rope[i] = rope[i - 1] + d * (seg / len);
            }
        }
    }
}
