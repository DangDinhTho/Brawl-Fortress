using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyringeGun : Gun
{

    public override void BeginFireSuccess()
    {
        base.BeginFireSuccess();
        playerOwner.playerNetwork.SyncFireLoopStart();
    }

    public override void BeginFire()
    {
        if (blockFire) return;
        //playerOwner.playerNetwork.SyncFireDeployedStart();
        //playerOwner.FPSPlayer.runSpeed /= 2;
        if (bulletInClip <= 0 && weaponState != WeaponState.Reloading)
        {
            StartReload();
            return;
        }
        BeginFireSuccess();

    }

    public override void FinishFire()
    {
        base.FinishFire();
        playerOwner.playerNetwork.SyncFireLoopFinish();
        //playerOwner.playerNetwork.SyncFireDeployedFinish();
        //playerOwner.FPSPlayer.runSpeed *= 2;
    }

    public override void FireLoop()
    {
        if (blockFire || !fire) return;
        if (bulletInClip <= 0)
        {
            FinishFire();
            StartReload();
            OnClipEmpty?.Invoke();
            return;
        }
        FireLoopSuccess();
    }


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
    //    float x = UnityEngine.Random.Range(-spread, spread);
    //    float y = UnityEngine.Random.Range(-spread, spread);

    //    //Calculate new direction with spread
    //    Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0); //Just add spread to last direction


    //    //Instantiate bullet/projectile
    //    damageObject.damage = UnityEngine.Random.Range(weaponConfig.minDamage, weaponConfig.maxDamage);
    //    damageObject.isOwner = playerOwner.playerNetwork.isMine;
    //    PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), shotPoints[0].position, shotPoints[0].rotation, directionWithSpread.normalized, damageObject); //store instantiated bullet in currentBullet
    //    //playerOwner.playerNetwork.SyncFire();
    //}

    //public override void RemoteFire()
    //{
    //    Vector3 directionWithoutSpread = playerOwner.targetLook - shotPoints[0].position;
    //    PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), shotPoints[0].position, shotPoints[0].rotation, directionWithoutSpread.normalized, damageObject);
    //}

    ////public override void RemoteFireDeployedStart()
    ////{
    ////    deployed = true;
    ////    StartCoroutine(RotateBarrel());
    ////}

    ////public override void RemoteFireDeployedFinish()
    ////{
    ////    deployed = false;
    ////}


    ////public override void RemoteFireLoopStart()
    ////{
    ////    base.RemoteFireLoopStart();

    ////}

    ////public override void RemoteFireLoopFinish()
    ////{
    ////    base.RemoteFireLoopFinish();
    ////}



    
}
