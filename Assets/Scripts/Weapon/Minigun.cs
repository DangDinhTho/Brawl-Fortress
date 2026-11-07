using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minigun : Gun
{
    public Transform barrel;
    public float speedRotate;
    private bool deployed;
    private int currShotPoint;
    public float spinInteval;
    public float shootInteval;
    public AudioClip spinSound;
    public AudioClip shootSound;


    public override void BeginFireSuccess()
    {
        base.BeginFireSuccess();
        playerOwner.playerNetwork.SyncFireLoopStart();
    }

    public override void BeginFire()
    {
        if (blockFire) return;
        deployed = true;
        OnTouchFire?.Invoke();
        StartCoroutine(RotateBarrel());
        playerOwner.playerNetwork.SyncFireDeployedStart();
        if (!playerOwner.isBot)
            playerOwner.FPSPlayer.runSpeed = playerOwner.characterConfig.characterStats.speed / 2;

        if (bulletInClip <= 0) return;
        BeginFireSuccess();
        
    }

    public override void FinishFire()
    {
        base.FinishFire();
        deployed = false;
        playerOwner.playerNetwork.SyncFireDeployedFinish();
        if (!playerOwner.isBot)
            playerOwner.FPSPlayer.runSpeed = playerOwner.characterConfig.characterStats.speed;
    }

    public override void FireLoop()
    {
        if (blockFire || !fire) return;
        if (bulletInClip <= 0)
        {
            fire = false;
            playerOwner.playerNetwork.SyncFireLoopFinish();
            OnClipEmpty?.Invoke();
            return;
        }
        FireLoopSuccess();
    }


    public override void StartReload()
    {
        return;
    }

    public override void Shoot()
    {
        //readyToShoot = false;

        currShotPoint = currShotPoint >= shotPoints.Length - 1 ? 0 : currShotPoint + 1;
        //Find the exact hit position using a raycast
        //readyToShoot = false;
        Vector3 targetPoint;
        //if (playerOwner.isBot)
        //{
            //targetPoint = playerOwner.targetLook;
            targetPoint = shotPoints[currShotPoint].position + shotPoints[currShotPoint].forward * 2;
        //}
        //else
        //{
        //    //Find the exact hit position using a raycast
        //    Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //Just a ray through the middle of your current view
        //    RaycastHit hit;

        //    //check if ray hits something

        //    if (Physics.Raycast(ray, out hit))
        //        targetPoint = hit.point;
        //    else
        //        targetPoint = ray.GetPoint(75); //Just a point far away from the player
        //}

        //Calculate direction from attackPoint to targetPoint
        Vector3 directionWithoutSpread = targetPoint - shotPoints[currShotPoint].position;

        //Calculate spread
        float x = UnityEngine.Random.Range(-spread, spread);
        float y = UnityEngine.Random.Range(-spread, spread);

        //Calculate new direction with spread
        Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0); //Just add spread to last direction


        //Instantiate bullet/projectile
        damageObject.damage = UnityEngine.Random.Range(weaponConfig.weaponStats.minDamage, weaponConfig.weaponStats.maxDamage);
        damageObject.isOwner = playerOwner.playerNetwork.isMine;
        PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), shotPoints[currShotPoint].position, shotPoints[currShotPoint].rotation, directionWithSpread.normalized, damageObject); //store instantiated bullet in currentBullet
        //playerOwner.playerNetwork.SyncFire();
        if (muzzeEffect != null)
            muzzeEffect.Play();
    }

    public override void RemoteFire()
    {
        currShotPoint = currShotPoint >= shotPoints.Length - 1 ? 0 : currShotPoint + 1;
        Vector3 directionWithoutSpread = playerOwner.targetLook - shotPoints[currShotPoint].position;
        PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), shotPoints[currShotPoint].position, shotPoints[currShotPoint].rotation, directionWithoutSpread.normalized, damageObject);
        muzzeEffect.Play();
    }

    public override void RemoteFireDeployedStart()
    {
        deployed = true;
        StartCoroutine(RotateBarrel());
    }

    public override void RemoteFireDeployedFinish()
    {
        deployed = false;
    }


    public override void RemoteFireLoopStart()
    {
        base.RemoteFireLoopStart();
        
    }

    public override void RemoteFireLoopFinish()
    {
        base.RemoteFireLoopFinish();
    }



    IEnumerator RotateBarrel()
    {
        float speed = speedRotate / 4;
        float deltaSpeed = speedRotate / (weaponConfig.weaponStats.delayFire / Time.deltaTime); 
        float deltaSpeedFinish = speedRotate /( 1.5f * (weaponConfig.weaponStats.delayFinishFire / Time.deltaTime)); 
        while (speed < speedRotate)
        {
            speed += deltaSpeed;
            barrel.Rotate(0, 0, speed);
            yield return null;
        }

        float timePlaySpin = spinInteval - 0.5f;
        float timePlayShoot = shootInteval - 0.5f;
        while (deployed)
        {
            barrel.Rotate(0, 0, speedRotate);

            if (bulletInClip > 0)
            {
                timePlayShoot += Time.deltaTime;
                if (timePlayShoot >= shootInteval)
                {
                    audioSource.PlayOneShot(shootSound);
                    print(0);
                    timePlayShoot = 0;
                }
            }
            else
            {
                timePlaySpin += Time.deltaTime;
                if (timePlaySpin >= spinInteval)
                {
                    audioSource.PlayOneShot(spinSound);
                    timePlaySpin = 0;
                }
            }

            yield return null;
        }

        audioSource.Stop();
        audioSource.PlayOneShot(weaponConfig.finishFireSound);
        while (speed > 0)
        {
            speed -= deltaSpeedFinish;
            barrel.Rotate(0, 0, speed);
            yield return null;
        }
    }

}
