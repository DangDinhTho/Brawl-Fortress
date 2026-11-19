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
    public static float BurnTime = 2f;
    /// <summary>Afterburn: damage every AfterburnInterval s while the fire lingers after the last flame hit.</summary>
    public static float AfterburnDamage = 3f;
    public static float AfterburnInterval = 0.5f;
    /// <summary>Still in the flame (hit within this time): only the flame's own damage applies, no afterburn on top.</summary>
    const float InFlameWindow = 0.3f;

    HealthBase health;
    GameObject fx;
    ParticleSystem[] systems;
    float burnUntil;
    bool emitting;
    // afterburn is dealt by the shooter's client only (like the flame damage): it knows who set the fire
    DamageObject afterburn;
    float lastHit, nextTick;
    static GameObject prefab;

    /// <summary>Sets the target on fire (or keeps it burning). 'source' = the flame's damage object: on the shooter's
    /// client (isOwner) the fire keeps hurting the target until it goes out.</summary>
    public static void Ignite(HealthBase target, DamageObject source = null)
    {
        if (target == null || target.death) return;
        var b = target.GetComponent<BurnFx>();
        if (b == null) b = target.gameObject.AddComponent<BurnFx>();
        if (b.health != target)
        {
            b.health = target;
            var hm = target as HealthManager;
            if (hm != null) { hm.OnRespawn -= b.OnRespawned; hm.OnRespawn += b.OnRespawned; }
        }
        b.lastHit = Time.time;
        if (source != null && source.isOwner)
        {
            if (b.afterburn == null) b.afterburn = new DamageObject();
            b.afterburn.team = source.team; b.afterburn.ownerId = source.ownerId; b.afterburn.owner = source.owner; b.afterburn.isOwner = true;
            if (!b.emitting) b.nextTick = Time.time + AfterburnInterval;
        }
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

    /// <summary>Puts the fire out at once and removes the effect (death / respawn): it was only told to stop emitting and
    /// stayed on the model; its looping systems started again when the respawn re-enabled the model (a harmless but
    /// visible fire after respawning).</summary>
    void Extinguish()
    {
        if (fx != null) Destroy(fx);
        fx = null; systems = null; emitting = false; afterburn = null; burnUntil = 0f;
    }

    void OnRespawned() { Extinguish(); }

    void Update()
    {
        if (fx != null && health != null && health.death) { Extinguish(); return; }
        if (!emitting) return;
        // afterburn: out of the flame, still on fire -> keeps losing health until the fire is out
        if (afterburn != null && health != null && !health.death && Time.time - lastHit > InFlameWindow && Time.time >= nextTick && Time.time < burnUntil)
        {
            nextTick = Time.time + AfterburnInterval;
            afterburn.damage = AfterburnDamage;
            health.TakeDamage(afterburn, Vector3.up * 0.1f);
        }
        if (Time.time >= burnUntil || (health != null && health.death))
        {
            foreach (var ps in systems) if (ps != null) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);  // flames die out
            emitting = false;
            afterburn = null;   // fire out: no more damage
        }
    }

    void OnDisable() { Extinguish(); }

    void OnDestroy()
    {
        var hm = health as HealthManager;
        if (hm != null) hm.OnRespawn -= OnRespawned;
    }
}
