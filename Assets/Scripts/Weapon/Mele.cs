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
        //Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //Just a ray through the middle of your current view
        //RaycastHit hit;

        Ray ray;
        RaycastHit hit;
        //Vector3 targetPoint;
        //if (playerOwner.isBot)
        //{
            //Vector3 targetPoint = playerOwner.targetLook;
            Vector3 org = playerOwner.transform.position;
            org.y = playerOwner.targetLook.y;
            Vector3 direc = playerOwner.targetLook - org;
            ray = new Ray(playerOwner.transform.position + Vector3.up, direc);
        //}
        //else
        //{
        //    //Find the exact hit position using a raycast
        //    ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //Just a ray through the middle of your current view
            
        //}


        //check if ray hits something
        if (Physics.Raycast(ray, out hit, 1.3f, hitBoxMark))
        {
            var damageable = hit.transform.root.GetComponent<HealthBase>();
            print(hit.transform.name);
            if (damageable == null || damageable.death || damageable.team == damageObject.team) return;

            Vector3 direction = damageable.centerBody.position - hit.point;
            if (direction.y <= 0)
                direction.y = 0.2f;
            direction = direction.normalized * 0.5f;
            damageObject.isOwner = true;
            damageObject.damage = Random.Range(weaponConfig.weaponStats.minDamage, weaponConfig.weaponStats.maxDamage);
            damageable.TakeDamage(damageObject, direction);
        }
    }
}
