using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mele : Weapon
{
    public LayerMask hitBoxMark;
    public float makeDamageDelay = 0.3f;

    public override void FireLoopSuccess()
    {
        base.FireLoopSuccess();
        playerOwner.playerNetwork.SyncFire();
        StartCoroutine(StartMakeDamage());
    }

    IEnumerator StartMakeDamage()
    {
        yield return new WaitForSeconds(makeDamageDelay);
        MakeDamage();
    }

    /// <summary>Max height difference between attacker and target for a melee hit (no hitting through floors).</summary>
    public const float MaxHeightDiff = 2.5f;

    public virtual void MakeDamage()
    {
        // Hits every enemy whose hitbox is inside the swing: within weaponConfig.range of the attacker, measured to the
        // nearest point of the hitbox (exact for big and small bodies), and inside the aimAngle cone.
        float reach = weaponConfig.range > 0f ? weaponConfig.range : 1.3f;
        float half = Mathf.Max(10f, weaponConfig.aimAngle * 0.5f);
        Vector3 pos = playerOwner.transform.position;
        Vector3 origin = pos + Vector3.up;
        Vector3 facing = Flat(playerOwner.transform.forward);
        // bots: their aim point can lag behind (the boss AI faces its target but does not always update it), so a target
        // in front of the body OR in the aim direction counts
        Vector3 aim = playerOwner.isBot ? Flat(playerOwner.targetLook - pos) : facing;
        if (aim.sqrMagnitude < 1e-6f) aim = facing;

        var hits = Physics.OverlapSphere(origin, reach + 1.5f, hitBoxMark, QueryTriggerInteraction.Collide);
        var done = new HashSet<HealthBase>();
        foreach (var h in hits)
        {
            var damageable = h.transform.root.GetComponent<HealthBase>();
            if (damageable == null || damageable.death || damageable.team == damageObject.team || done.Contains(damageable)) continue;
            Vector3 closest = h.ClosestPoint(origin);
            if (Mathf.Abs(closest.y - origin.y) > MaxHeightDiff) continue;
            Vector3 to = closest - origin; to.y = 0f;
            if (to.magnitude > reach) continue;
            Vector3 toBody = Flat(damageable.centerBody.position - pos);
            if (to.sqrMagnitude > 0.04f && Vector3.Angle(facing, toBody) > half && Vector3.Angle(aim, toBody) > half) continue;
            done.Add(damageable);
            Vector3 direction = damageable.centerBody.position - closest;
            if (direction.y <= 0)
                direction.y = 0.2f;
            direction = direction.normalized * 0.5f;
            damageObject.isOwner = true;
            damageObject.damage = Random.Range(weaponConfig.weaponStats.minDamage, weaponConfig.weaponStats.maxDamage);
            damageable.TakeDamage(damageObject, direction);
        }
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.zero; }

}
