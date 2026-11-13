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

    public virtual void MakeDamage()
    {
        // Hits every enemy inside the swing cone (weaponConfig.range / aimAngle) = what the aim indicator shows.
        float reach = weaponConfig.range > 0f ? weaponConfig.range : 1.3f;
        float half = Mathf.Max(10f, weaponConfig.aimAngle * 0.5f);
        Vector3 origin = playerOwner.transform.position + Vector3.up;
        Vector3 fwd;
        if (playerOwner.isBot) { fwd = playerOwner.targetLook - playerOwner.transform.position; }
        else fwd = playerOwner.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = playerOwner.transform.forward;
        fwd.Normalize();

        var hits = Physics.OverlapSphere(origin, reach + 0.6f, hitBoxMark, QueryTriggerInteraction.Collide);
        var done = new HashSet<HealthBase>();
        foreach (var h in hits)
        {
            var damageable = h.transform.root.GetComponent<HealthBase>();
            if (damageable == null || damageable.death || damageable.team == damageObject.team || !done.Add(damageable)) continue;
            Vector3 to = damageable.centerBody.position - origin; to.y = 0f;
            if (to.magnitude > reach + 0.4f) continue;                                   // 0.4 = body radius
            if (to.sqrMagnitude > 0.04f && Vector3.Angle(fwd, to) > half) continue;
            Vector3 direction = damageable.centerBody.position - h.ClosestPoint(origin);
            if (direction.y <= 0)
                direction.y = 0.2f;
            direction = direction.normalized * 0.5f;
            damageObject.isOwner = true;
            damageObject.damage = Random.Range(weaponConfig.weaponStats.minDamage, weaponConfig.weaponStats.maxDamage);
            damageable.TakeDamage(damageObject, direction);
        }
    }
}
