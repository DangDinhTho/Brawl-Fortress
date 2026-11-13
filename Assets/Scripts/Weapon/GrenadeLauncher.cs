using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeLauncher : Gun
{
    protected override bool FlatAim => false;
    //public float shootForce;
    //public float upwardForce;



    //public override void FireLoopSuccess()
    //{
    //    base.FireLoopSuccess();
    //    //Shoot();
    //}

    //public override void Shoot()
    //{
    //    //readyToShoot = false;

    //    //Find the exact hit position using a raycast
    //    Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //Just a ray through the middle of your current view
    //    RaycastHit hit;

    //    //check if ray hits something
    //    Vector3 targetPoint;
    //    if (Physics.Raycast(ray, out hit))
    //        targetPoint = hit.point;
    //    else
    //        targetPoint = ray.GetPoint(75); //Just a point far away from the player

    //    //Calculate direction from attackPoint to targetPoint
    //    Vector3 directionWithoutSpread = targetPoint - shotPoints[0].position;

    //    //Calculate spread
    //    float x = Random.Range(-spread, spread);
    //    float y = Random.Range(-spread, spread);

    //    //Calculate new direction with spread
    //    Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0); //Just add spread to last direction

    //    //Instantiate bullet/projectile
    //    damageObject.damage = Random.Range(weaponConfig.minDamage, weaponConfig.maxDamage);
    //    damageObject.isOwner = playerOwner.playerNetwork.isMine;
    //    PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), shotPoints[0].position, shotPoints[0].rotation, directionWithSpread.normalized, damageObject); //store instantiated bullet in currentBullet
    //    playerOwner.playerNetwork.SyncFire();
    //}


    //public override void RemoteFire()
    //{
    //    base.RemoteFire();
    //    Vector3 directionWithoutSpread = playerOwner.targetLook - shotPoints[0].position;
    //    PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), shotPoints[0].position, shotPoints[0].rotation, directionWithoutSpread.normalized, damageObject);
    //}
}
