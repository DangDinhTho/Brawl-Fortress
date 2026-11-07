using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wrench : Mele
{
    public Action<int> OnExpChange;
    [HideInInspector]
    public int exp;
    private int expBase = 200;
    public AudioClip buildHitSound;

    public override void InitData(TF2Player player, Transform bone, bool isRemote = false)
    {
        base.InitData(player, bone, isRemote);
        if (expBase > 200) expBase = 0;
        exp = expBase;
        
    }

    public void OnBuff(float chance)
    {
        if(chance > 1) return;
        exp += (int)(expBase * chance);
        OnExpChange?.Invoke(exp);
    }

    public void UseExp(int _expUse)
    {
        exp -= _expUse;
        OnExpChange?.Invoke(exp);
    }

    public override void MakeDamage()
    {
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
        if (Physics.Raycast(ray, out hit, 1.75f, hitBoxMark))
        {
            var buildable = hit.transform.root.GetComponent<BuildableEntry>();
            if(buildable != null && buildable.team == playerOwner.team)
            {
                if (exp > 0)
                {
                    int expRan = UnityEngine.Random.Range(15, 30);
                    if (expRan > exp) expRan = exp;
                    buildable.AddExp(expRan);
                    exp -= expRan;
                    OnExpChange?.Invoke(exp);
                    print("AddEXP");
                    audioSource.PlayOneShot(buildHitSound);
                }
                return;
            }

            var damageable = hit.transform.root.GetComponent<HealthBase>();
            if (damageable == null || damageable.death || damageable.team == damageObject.team) return;

            Vector3 direction = damageable.centerBody.position - hit.point;
            if (direction.y <= 0)
                direction.y = 0.2f;
            direction = direction.normalized * 0.5f;
            damageObject.isOwner = true;
            damageObject.damage = UnityEngine.Random.Range(weaponConfig.weaponStats.minDamage, weaponConfig.weaponStats.maxDamage);
            damageable.TakeDamage(damageObject, direction);
        }
    }
}
