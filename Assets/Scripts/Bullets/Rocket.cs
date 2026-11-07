using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rocket : BulletNormal
{
    public float jumpForce;

    public override void OnTriggerEnter(Collider other)
    {
        if (!ready) return;
        ready = false;
        if (other.CompareTag("HitBox"))
        {
            if (damageObject.isOwner)
            {
                DoCollisionDamage(other.transform.root);
            }

        }

        if(damageObject.isOwner && damageObject.ownerId == ArenaManager.Instance.mainPlayer.viewID && transform.position.y < damageObject.owner.transform.position.y + 1f && Vector3.Distance(transform.position, damageObject.owner.transform.position) < 1.2f)
        {
            MakeRocketJump();
        }

        OnFinish();

    }

    public void MakeRocketJump()
    {
        print("Rocket Jump");
        damageObject.owner.FPSPlayer.PlatformJump(jumpForce);
        damageObject.owner.healthManager.TakeSelfDamage(Random.Range(27, 46), Vector3.up);
    }

    public override void DoCollisionDamage(Transform target)
    {
        var damageable = target.GetComponent<HealthBase>();
        if (damageable == null || damageable.death || damageable.team == damageObject.team) return;

        float distance = Vector3.Distance(transform.position, damageable.centerBody.position);
        if (distance < 0.2) distance = 0.2f;
        float heSo = Mathf.Clamp(1 / distance, 0.75f, 2.2f);
        print(heSo);
        Vector3 direction = damageable.centerBody.position - transform.position;
        if (direction.y <= 0)
            direction.y = 1;
        direction = direction.normalized * heSo;
        damageable.TakeDamage(damageObject, direction);
    }
}
