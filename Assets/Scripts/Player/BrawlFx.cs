using UnityEngine;

/// <summary>
/// Brawl-Stars-style little effects in a match, shared by every character (CharacterFx calls them): one world-space
/// ParticleSystem per kind, made on first use in the active scene, fed with Emit (one draw call per kind, no pooling).
/// Shapes come from the procedural shader BrawlFortress/FxParticle through Resources/Materials/FxPuff, FxPlus, FxSpark.
///  - Dust: a puff of dust behind the feet at each step.
///  - Mud: dark drops splashing out of a bog puddle (Swamp, slowed), Bubble: bubbles (bog, or swimming against an
///    Ocean current), Splash: water drops in a Savanna water hole.
///  - Streak: speed lines behind a character sped up (Japan torii path, with an Ocean current).
///  - Heal: green "+" rising (water hole).
///  - Hit: a burst of sparks where a character is hit.
/// </summary>
public static class BrawlFx
{
    static Transform root;
    static ParticleSystem dust, mud, bubble, streak, heal, spark, leaf;
    static ParticleSystem.EmitParams ep;

    public static Color DustColor = new Color(1f, 0.98f, 0.94f, 0.85f);

    static bool Ready()
    {
        if (root != null) return true;
        var puff = Resources.Load<Material>("Materials/FxPuff");
        var plus = Resources.Load<Material>("Materials/FxPlus");
        var star = Resources.Load<Material>("Materials/FxSpark");
        var leafMat = Resources.Load<Material>("Materials/FxLeaf");
        if (puff == null) return false;
        root = new GameObject("BrawlFx").transform;
        dust = Make("Dust", puff, 200, false, 0f, 3.5f, Fade(0.0f, 0.4f), Grow(0.6f, 1.5f));
        mud = Make("Mud", puff, 120, false, 1.6f, 0.5f, Fade(0.0f, 0.75f), Grow(1f, 0.6f));
        bubble = Make("Bubble", puff, 80, false, -0.05f, 1.2f, Fade(0.15f, 0.7f), Grow(0.6f, 1.15f));
        streak = Make("Streak", puff, 120, true, 0f, 0f, Fade(0.0f, 0.4f), Grow(1f, 0.7f));
        heal = Make("Heal", plus ? plus : puff, 60, false, -0.05f, 1.5f, Fade(0.15f, 0.6f), Grow(0.6f, 1f));
        spark = Make("Spark", star ? star : puff, 120, false, 0f, 6f, Fade(0.0f, 0.45f), Grow(1.2f, 0.3f));
        leaf = Make("Leaf", leafMat ? leafMat : puff, 300, false, 0.55f, 2.2f, Fade(0.0f, 0.7f), Grow(1f, 0.85f));
        var rot = leaf.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);   // tumbling (rad/s)
        return true;
    }

    static Gradient Fade(float fadeIn, float holdUntil)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(fadeIn > 0f ? 0f : 1f, 0f), new GradientAlphaKey(1f, Mathf.Max(fadeIn, 0.01f)),
                          new GradientAlphaKey(1f, holdUntil), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    static AnimationCurve Grow(float from, float to) => AnimationCurve.EaseInOut(0f, from, 1f, to);

    static ParticleSystem Make(string name, Material mat, int max, bool stretched, float gravity, float drag, Gradient fade, AnimationCurve size)
    {
        var go = new GameObject("Fx" + name);
        go.transform.SetParent(root, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false; main.loop = true; main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = max; main.startSpeed = 0f; main.startLifetime = 0.5f; main.startSize = 0.3f;
        main.gravityModifier = gravity;
        var em = ps.emission; em.enabled = false;
        var sh = ps.shape; sh.enabled = false;
        var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(fade);
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, size);
        if (drag > 0f) { var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.drag = drag; lv.multiplyDragByParticleSize = false; lv.multiplyDragByParticleVelocity = false; }
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = stretched ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (stretched) { r.velocityScale = 0.09f; r.lengthScale = 1.5f; }
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.sortMode = ParticleSystemSortMode.None;
        ps.Play();
        return ps;
    }

    static void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color c, float rot = 0f)
    {
        ep.position = pos; ep.velocity = vel; ep.startSize = size; ep.startLifetime = life; ep.startColor = c;
        ep.rotation = rot; ep.applyShapeToPosition = false;
        ps.Emit(ep, 1);
    }

    static Vector3 Rnd(float r) => new Vector3(Random.Range(-r, r), Random.Range(-r, r), Random.Range(-r, r));

    /// <summary>A step's puff of dust at the feet; dir = the way the character moves (flat, normalised), side = -1 / +1 (left / right foot).</summary>
    public static void Dust(Vector3 feet, Vector3 dir, float side)
    {
        if (!Ready()) return;
        Vector3 right = new Vector3(dir.z, 0f, -dir.x);
        Vector3 p = feet - dir * 0.22f + right * (0.12f * side) + Vector3.up * 0.1f;
        Emit(dust, p, -dir * 0.7f + Vector3.up * 0.45f + right * (0.25f * side) + Rnd(0.15f), Random.Range(0.5f, 0.66f), Random.Range(0.55f, 0.7f), DustColor, Random.Range(0f, 360f));
        Emit(dust, p + right * (0.14f * side) - dir * 0.1f, -dir * 0.45f + Vector3.up * 0.3f + right * (0.35f * side) + Rnd(0.2f), Random.Range(0.36f, 0.48f), Random.Range(0.5f, 0.6f), DustColor);
        if (Random.value < 0.4f)
            Emit(dust, p - right * (0.1f * side), -dir * 0.3f + Vector3.up * 0.35f + Rnd(0.25f), Random.Range(0.3f, 0.4f), 0.5f, DustColor);
    }

    /// <summary>Mud drops splashing out of a bog puddle at a step.</summary>
    public static void Mud(Vector3 feet, Vector3 dir)
    {
        if (!Ready()) return;
        for (int i = 0; i < 5; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector3 o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Color c = Random.value < 0.5f ? new Color(0.62f, 0.9f, 0.25f, 1f) : new Color(0.5f, 0.56f, 0.24f, 1f);
            Emit(mud, feet + o * 0.15f + Vector3.up * 0.08f, o * Random.Range(0.6f, 1.2f) - dir * 0.4f + Vector3.up * Random.Range(1.8f, 2.7f), Random.Range(0.14f, 0.22f), Random.Range(0.4f, 0.55f), c);
        }
        Emit(dust, feet + Vector3.up * 0.08f, Vector3.up * 0.1f, 0.7f, 0.35f, new Color(0.7f, 0.95f, 0.4f, 0.5f));   // a light ripple where the foot went in
    }

    /// <summary>Water drops in a water hole at a step.</summary>
    public static void Splash(Vector3 feet, Vector3 dir)
    {
        if (!Ready()) return;
        for (int i = 0; i < 5; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector3 o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Emit(mud, feet + o * 0.15f + Vector3.up * 0.08f, o * Random.Range(0.6f, 1.2f) - dir * 0.3f + Vector3.up * Random.Range(1.8f, 2.6f), Random.Range(0.08f, 0.13f), 0.45f, new Color(0.75f, 0.92f, 1f, 0.9f));
        }
    }

    /// <summary>A few bubbles rising around the feet (in a bog, or against an Ocean current).</summary>
    public static void Bubble(Vector3 feet, Color c)
    {
        if (!Ready()) return;
        for (int i = 0; i < 3; i++)
        {
            Vector3 o = new Vector3(Random.Range(-0.35f, 0.35f), 0f, Random.Range(-0.35f, 0.35f));
            Emit(bubble, feet + o + Vector3.up * Random.Range(0.05f, 0.5f), Vector3.up * Random.Range(0.5f, 0.9f), Random.Range(0.16f, 0.26f), Random.Range(0.6f, 0.9f), c);
        }
    }

    /// <summary>Speed lines behind a character moving faster than normal.</summary>
    public static void Streak(Vector3 feet, Vector3 dir, Color c)
    {
        if (!Ready()) return;
        Vector3 right = new Vector3(dir.z, 0f, -dir.x);
        for (int i = 0; i < 2; i++)
        {
            Vector3 p = feet - dir * Random.Range(0.1f, 0.5f) + right * Random.Range(-0.45f, 0.45f) + Vector3.up * Random.Range(0.3f, 1.5f);
            Emit(streak, p, -dir * Random.Range(6f, 8f), Random.Range(0.1f, 0.14f), Random.Range(0.18f, 0.25f), c);
        }
    }

    /// <summary>Green "+" signs rising around a character healed by a water hole.</summary>
    public static void Heal(Vector3 feet)
    {
        if (!Ready()) return;
        for (int i = 0; i < 3; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector3 o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(0.25f, 0.5f);
            Emit(heal, feet + o + Vector3.up * Random.Range(0.4f, 1.3f), Vector3.up * Random.Range(1.0f, 1.5f), Random.Range(0.3f, 0.38f), Random.Range(0.7f, 0.9f), new Color(0.42f, 1f, 0.45f, 1f));
        }
    }

    /// <summary>Leaves torn out of a bush: count leaves of colour c from p, thrown up and out (strength 1 = a clump torn
    /// apart, less = a shake), tumbling down.</summary>
    public static void Leaves(Vector3 p, Color c, int count, float strength)
    {
        if (!Ready()) return;
        for (int i = 0; i < count; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector3 o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Vector3 v = (o * Random.Range(1.2f, 3.2f) + Vector3.up * Random.Range(2.2f, 4.5f)) * strength;
            Color k = Color.Lerp(c, c * Random.Range(0.75f, 1.15f), 0.8f); k.a = 1f;
            Emit(leaf, p + o * Random.Range(0f, 0.35f) + Vector3.up * Random.Range(-0.3f, 0.4f), v, Random.Range(0.2f, 0.32f), Random.Range(0.8f, 1.3f), k, Random.Range(0f, 360f));
        }
    }

    /// <summary>Sparks where a character is hit: at the chest, thrown along the shot's direction.</summary>
    public static void Hit(Vector3 chest, Vector3 shotDir)
    {
        if (!Ready()) return;
        shotDir.y = 0f;
        if (shotDir.sqrMagnitude < 1e-4f) shotDir = Vector3.forward; else shotDir.Normalize();
        Vector3 p = chest - shotDir * 0.25f;
        for (int i = 0; i < 6; i++)
        {
            Vector3 v = (shotDir * Random.Range(0.5f, 1.5f) + Rnd(1f) + Vector3.up * 0.4f) * Random.Range(2.5f, 4.5f);
            Color c = Random.value < 0.5f ? new Color(1f, 0.95f, 0.65f, 1f) : new Color(1f, 0.72f, 0.25f, 1f);
            Emit(spark, p + Rnd(0.12f), v, Random.Range(0.3f, 0.45f), Random.Range(0.2f, 0.28f), c, Random.Range(0f, 90f));
        }
        Emit(spark, p, Vector3.zero, 1.1f, 0.13f, new Color(1f, 1f, 0.9f, 0.95f));   // the flash of the impact
    }
}
