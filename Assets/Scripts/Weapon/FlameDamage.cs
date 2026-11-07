using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameDamage : MonoBehaviour
{
    public DamageObject damageObject;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HitBox"))
        {
            if (damageObject.isOwner)
            {
                //damageObject.damage = direcDamage;
                //direction = true;
                print("hit");
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
