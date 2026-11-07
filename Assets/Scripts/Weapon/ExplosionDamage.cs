using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionDamage : PoolingEntry
{
    public float explosionRadius = 20f;
    public LayerMask detectLayers;
    public bool CheckRootsOnly = false;
    public float timeLife = 2f;
    private DamageObject damageObject;
    private RaycastHit hitInfo;

    public override void OnCreated(params object[] args)
    {
        base.OnCreated(args);
        if (args.Length > 0)
        {
            damageObject = (DamageObject)args[0];
            if (damageObject.isOwner)
                DoCollisionDamage();
        }
        if(args.Length > 1)
        {
            explosionRadius = (float)args[1];
        }

        
        StartCoroutine(Finish());
    }

    public override void OnFinish()
    {
        base.OnFinish();
        damageObject = null;
    }


    void DoCollisionDamage()
    {
        Collider[] colls = Physics.OverlapSphere(transform.position, explosionRadius, detectLayers, QueryTriggerInteraction.Ignore);
        List<string> Hited = new List<string>();

        foreach (Collider c in colls)
        {
            //the damage to real players is handled separately
           // if (c.CompareTag("Untagged") || c.CompareTag("Player")) continue;

            if (c.CompareTag("HitBox"))
            {
                if (Hited.Contains(c.transform.root.name)) continue;
                if (!ExplosionCanHitTarget(c.transform.root, new Vector3(0, 0.6f, 0)) && !ExplosionCanHitTarget(c.transform.root, new Vector3(0, 0.15f, 0))) continue;

                Hited.Add(c.transform.root.name);
            }
            //else
            //{
            //    if (!ExplosionCanHitTarget(c.transform, new Vector3(0, 0.1f, 0), CheckRootsOnly)) continue;
            //}

            var damageable = c.transform.root.GetComponent<HealthBase>();
            if (damageable == null || damageable.team == damageObject.team) continue;

            //damageData.Cause = (!creator.isRealPlayer) ? DamageCause.Bot : DamageCause.Explosion;

            //if (damageData.MFPSActor == null)
            //{
            //    Debug.Log($"Explosion actor '{creator.ActorViewID}' was not found in the scene, maybe left the match?");
            //    return;
            //}
            float distance = Vector3.Distance(transform.position, damageable.centerBody.position);
            if (distance < 0.2) distance = 0.2f;
            float heSo = Mathf.Clamp(1 / distance, 0.75f, 2.2f);
            //print(heSo);
            Vector3 direction = damageable.centerBody.position - transform.position;
            if (direction.y <= 0)
                direction.y = 1;
            direction = direction.normalized * heSo;
            damageable.TakeDamage(damageObject, direction);
        }
    }

    private bool ExplosionCanHitTarget(Transform target, Vector3 offset, bool rootOnly = true)
    {
        bool result = false;
        Vector3 rhs = transform.position + offset;
        Vector3 normalized = (target.position + offset - rhs).normalized;
        if (Physics.Raycast(rhs, normalized, out hitInfo, explosionRadius))
        {
            if (rootOnly)
            {
                if (hitInfo.transform.root == target) { return true; }
            }
            else
            {
                // If the collider has a rigidbody in the transform root, it will always hinder the raycast
                // so lets ignore it.
                if (target.IsChildOf(hitInfo.transform)) return true;

                if (hitInfo.transform == target) { return true; }
            }
        }
        return result;
    }


    IEnumerator Finish()
    {
        yield return new WaitForSeconds(timeLife);
        OnFinish();
    }

}
