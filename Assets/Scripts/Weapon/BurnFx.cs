using UnityEngine;

/// <summary>
/// "On fire" look on a character hit by a flamethrower, like the medigun heal effect on the healed one: flames licking up
/// around the body and embers (Resources/Effects/BurnFX). Each flame hit keeps it burning for burnTime; then it stops
/// emitting and fades out. Visual only, on every client: FlameDamage's trigger runs on every client (the flame plays on
/// all of them), only the damage is applied by the shooter.
/// </summary>
[DisallowMultipleComponent]
public class BurnFx : MonoBehaviour
{
    public static float BurnTime = 1.5f;

    HealthBase health;
    GameObject fx;
    ParticleSystem[] systems;
    float burnUntil;
    bool emitting;
    static GameObject prefab;

    /// <summary>Sets the target on fire (or keeps it burning).</summary>
    public static void Ignite(HealthBase target)
    {
        if (target == null || target.death) return;
        var b = target.GetComponent<BurnFx>();
        if (b == null) b = target.gameObject.AddComponent<BurnFx>();
        b.health = target;
        b.Keep();
    }

    void Keep()
    {
        burnUntil = Time.time + BurnTime;
        if (fx == null)
        {
            if (prefab == null) prefab = Resources.Load<GameObject>("Effects/BurnFX");
            if (prefab == null) return;
            var player = GetComponent<TF2Player>();
            Transform anchor = player != null && player.remotePlayer != null ? player.remotePlayer.transform : transform;
            fx = Instantiate(prefab, anchor);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = Quaternion.identity;
            systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        }
        if (!emitting)
        {
            foreach (var ps in systems) ps.Play(false);
            emitting = true;
        }
    }

    void Update()
    {
        if (!emitting) return;
        if (Time.time >= burnUntil || (health != null && health.death))
        {
            foreach (var ps in systems) if (ps != null) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);  // flames die out
            emitting = false;
        }
    }

    void OnDisable()
    {
        if (fx != null) Destroy(fx);
        fx = null; emitting = false;
    }
}
