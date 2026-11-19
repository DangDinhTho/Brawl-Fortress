using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameDamage : MonoBehaviour
{
    public DamageObject damageObject;

    TF2Player flameOwner;
    float nextBurnCheck;

    /// <summary>Sets an enemy in the flame on fire (BurnFx, visual only, every client).</summary>
    void Burn(Collider other)
    {
        if (flameOwner == null) flameOwner = GetComponentInParent<TF2Player>();
        var target = other.transform.root.GetComponent<HealthBase>();
        if (target == null || target.death) return;
        if (flameOwner != null ? target.team == flameOwner.team : target.team == damageObject.team) return;
        BurnFx.Ignite(target);
    }

    // standing in the flame keeps the target burning (checked a few times a second)
    private void OnTriggerStay(Collider other)
    {
        if (Time.time < nextBurnCheck || !other.CompareTag("HitBox")) return;
        Burn(other);
    }

    private void LateUpdate() { if (Time.time >= nextBurnCheck) nextBurnCheck = Time.time + 0.2f; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HitBox"))
        {
            Burn(other);
            if (damageObject.isOwner)
            {
                //damageObject.damage = direcDamage;
                //direction = true;
                DoCollisionDamage(other.transform.root);
            }

        }

    }

    void DoCollisionDamage(Transform target)
    {
        var damageable = target.GetComponent<HealthBase>();
        if (damageable == null || damageable.death || damageable.team == damageObject.team) return;

        float distance = Vector3.Distance(transform.position, damageable.centerBody.position);
        if (distance < 0.2) distance = 0.2f;
        float heSo = Mathf.Clamp(1 / distance, 0.75f, 2.2f);
        Vector3 direction = damageable.centerBody.position - transform.position;
        if (direction.y <= 0)
            direction.y = 1;
        direction = direction.normalized * heSo;
        damageable.TakeDamage(damageObject, direction);
    }
}
