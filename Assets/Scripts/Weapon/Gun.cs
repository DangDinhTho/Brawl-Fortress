//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class Gun : Weapon
//{
//    public Transform[] shotPoints;

//    protected float nextTimeReload;
//    public float spread = 0f;
//    public Action OnTouchFire;
//    public Action OnStartReload;
//    public Action OnReloadLoop;
//    public Action OnClipEmpty;
//    public Action OnFinishReload;
//    public Action OnReloadDone;
//    public Action OnInventoryUpdate;
//    public Action OnBulletUpdate;
//    public Action OnShoot;
//    [HideInInspector]
//    public int maxReloadLoop;
//    protected int _bulletInClip;
//    [HideInInspector]
//    public int bulletInClip
//    {
//        get { return _bulletInClip; }
//        set
//        {
//            _bulletInClip = value;
//            OnBulletUpdate?.Invoke();
//        }
//    }
//    protected bool reload;

//    protected int _intventory;
//    [HideInInspector]
//    public int inventory
//    {
//        get { return _intventory; }
//        set
//        {
//            _intventory = value;
//            OnInventoryUpdate?.Invoke();
//        }
//    }

//    public ParticleSystem muzzeEffect;

//    public override void InitData(TF2Player player, Transform bone, bool isRemote = false)
//    {
//        base.InitData(player, bone, isRemote);

//        if (isRemote && muzzeEffect != null)
//        {
//            LayerMask normalLayer = LayerMask.NameToLayer("Default");
//            muzzeEffect.gameObject.layer = normalLayer;
//            foreach (Transform child in muzzeEffect.transform)
//            {
//                child.gameObject.layer = normalLayer;
//            }
//        }

//        bulletInClip = weaponConfig.weaponStats.clipSize;
//        inventory = weaponConfig.weaponStats.inventorySize;
//        player.healthManager.OnRespawn += ResetGun;
//    }

//    public void ResetGun()
//    {
//        bulletInClip = weaponConfig.weaponStats.clipSize;
//        inventory = weaponConfig.weaponStats.inventorySize;
//    }

//    public override void BeginFire()
//    {
//        if(blockFire) return;
//        if (bulletInClip <= 0)
//        {
//            if (weaponState != WeaponState.Reloading)
//            {
//                OnClipEmpty?.Invoke();
//                StartReload();
//            }

//            return;
//        }

//        base.BeginFireSuccess();
//    }

//    public override void FireLoop()
//    {
//        if(blockFire || !fire) return;
//        if(bulletInClip <= 0)
//        {
//            //FinishFire();
//            if (weaponState != WeaponState.Reloading)
//            {
//                StartReload();
//                OnClipEmpty?.Invoke();
//            }

//            return;
//        }
//        FireLoopSuccess();
//    }

//    public override void FireLoopSuccess()
//    {
//        bulletInClip--;
//        Shoot();
//        base.FireLoopSuccess();
//    }

//    public virtual void StartReload()
//    {
//        if(weaponState == WeaponState.Reloading || inventory <= 0) return;
//        blockFire = true;
//        weaponState = WeaponState.Reloading;
//        OnStartReload?.Invoke();

//        int bulletNeed = weaponConfig.weaponStats.clipSize - bulletInClip;
//        this.maxReloadLoop = inventory >= bulletNeed ? bulletNeed : inventory;
//        reload = true;
//        //weaponState = WeaponState.DelayStartReload;
//        StartCoroutine(LoopReload());
//    }

//    public virtual void ReloadLoop()
//    {
//        //OnReloadLoop?.Invoke();
//        bulletInClip++;
//        inventory--;
//        if (weaponConfig.reloadSound != null)
//        {
//            audioSource.PlayOneShot(weaponConfig.reloadSound);
//        }

//    }

//    public virtual void FinishReload()
//    {
//        reload = false;
//        //weaponState = WeaponState.DelayFinishReload;
//        //OnFinishReload?.Invoke();
//        //isReloading = false;
//    }

//    public virtual void Shoot()
//    {
//        //readyToShoot = false;
//        Vector3 targetPoint;
//        //if (playerOwner.isBot)
//        //{
//        //targetPoint = playerOwner.targetLook;
//        targetPoint = shotPoints[0].position + shotPoints[0].forward * 2;
//        //}
//        //else
//        //{
//        //    //Find the exact hit position using a raycast
//        //    Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //Just a ray through the middle of your current view
//        //    RaycastHit hit;

//        //    //check if ray hits something

//        //    if (Physics.Raycast(ray, out hit))
//        //        targetPoint = hit.point;
//        //    else
//        //        targetPoint = ray.GetPoint(75); //Just a point far away from the player
//        //}


//        //Calculate direction from attackPoint to targetPoint
//        Vector3 directionWithoutSpread = targetPoint - shotPoints[0].position;

//        //Calculate spread
//        float x = UnityEngine.Random.Range(-spread, spread);
//        float y = UnityEngine.Random.Range(-spread, spread);

//        //Calculate new direction with spread
//        Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0); //Just add spread to last direction

//        //Instantiate bullet/projectile
//        damageObject.damage = UnityEngine.Random.Range(weaponConfig.weaponStats.minDamage, weaponConfig.weaponStats.maxDamage);
//        damageObject.isOwner = playerOwner.photonView.IsMine;
//        PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), shotPoints[0].position, shotPoints[0].rotation, directionWithSpread.normalized, damageObject); //store instantiated bullet in currentBullet

//        if (muzzeEffect != null)
//            muzzeEffect.Play();

//        playerOwner.playerNetwork.SyncFire();
//        OnShoot?.Invoke();
//    }


//    //public virtual void BotShoot()
//    //{
//    //    //Calculate direction from attackPoint to targetPoint
//    //    Vector3 directionWithoutSpread = playerOwner.targetLook - shotPoints[0].position;

//    //    //Calculate spread
//    //    float x = UnityEngine.Random.Range(-spread, spread);
//    //    float y = UnityEngine.Random.Range(-spread, spread);

//    //    //Calculate new direction with spread
//    //    Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0); //Just add spread to last direction

//    //    //Instantiate bullet/projectile
//    //    damageObject.damage = UnityEngine.Random.Range(weaponConfig.minDamage, weaponConfig.maxDamage);
//    //    damageObject.isOwner = playerOwner.playerNetwork.isMine;
//    //    PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), shotPoints[0].position, shotPoints[0].rotation, directionWithSpread.normalized, damageObject); //store instantiated bullet in currentBullet
//    //    playerOwner.playerNetwork.SyncFire();
//    //}

//    public override void RemoteFire()
//    {
//        base.RemoteFire();
//        Vector3 directionWithoutSpread = playerOwner.targetLook - shotPoints[0].position;
//        PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), shotPoints[0].position, shotPoints[0].rotation, directionWithoutSpread.normalized, damageObject);
//        if (muzzeEffect != null)
//            muzzeEffect.Play();
//    }

//    public  virtual IEnumerator LoopReload()
//    {

//        if (weaponConfig.weaponStats.delayReload > 0)
//        {
//            yield return new WaitForSeconds(weaponConfig.weaponStats.delayReload);
//            //blockFire = false;
//        }

//        if (weaponConfig.weaponStats.timeReload > 0)
//        {
//            int count = 0;
//            while (count < maxReloadLoop && reload)
//            {

//                //if (Time.time >= nextTimeReload)
//                //{
//                    OnReloadLoop?.Invoke();
//                    //ReloadLoop();
//                    //nextTimeReload = Time.time + weaponConfig.timeReload;
//                    //count++;
//                //}
//                //yield return null;
//                yield return new WaitForSeconds(weaponConfig.weaponStats.timeReload * 0.65f);
//                ReloadLoop();
//                count++;
//            }
//        }
//        else
//        {
//            if (weaponConfig.reloadSound != null)
//            {
//                audioSource.PlayOneShot(weaponConfig.reloadSound);
//            }
//            bulletInClip += maxReloadLoop;
//            inventory -= maxReloadLoop;
//        }

//        reload = false;

//        OnReloadDone?.Invoke();

//        if (weaponConfig.weaponStats.delayFinishReload > 0)
//        {
//            //blockFire = true;
//            //weaponState = WeaponState.DelayFinishReload;
//            float reloadTimeRemaining = weaponConfig.weaponStats.timeReload + weaponConfig.weaponStats.delayFinishReload;
//            yield return new WaitForSeconds(reloadTimeRemaining);

//        }

//        OnFinishReload?.Invoke();
//        blockFire = false;
//        if (weaponState == WeaponState.Reloading)
//        {
//            if (fire)
//            {
//                weaponState = WeaponState.Firing;
//            }
//            else
//            {
//                weaponState = WeaponState.Idle;
//            }
//        }


//    }

//    public void Buff(float rance, bool constranClip = false)
//    {
//        if(!playerOwner.isBot && fire) return;
//        if(weaponConfig.weaponStats.inventorySize <= 0)
//        {
//            int ammoAdd = Mathf.RoundToInt(rance * weaponConfig.weaponStats.clipSize);
//            bulletInClip = Mathf.Min(bulletInClip + ammoAdd, weaponConfig.weaponStats.clipSize);
//        }
//        else
//        {
//            int ammoAdd = Mathf.RoundToInt(rance * weaponConfig.weaponStats.inventorySize);
//            inventory = Mathf.Min(inventory + ammoAdd, weaponConfig.weaponStats.inventorySize);

//            if (constranClip)
//            {
//                int ammoAddClip = Mathf.RoundToInt(rance * weaponConfig.weaponStats.clipSize);
//                bulletInClip = Mathf.Min(bulletInClip + ammoAddClip, weaponConfig.weaponStats.clipSize);
//            }
//        }
//    }

//    public override void OnHide()
//    {
//        FinishReload();
//        blockFire = false;
//        weaponState = WeaponState.Idle;
//        base.OnHide();
//    }
//}




using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : Weapon
{
    public Transform[] shotPoints;

    protected float nextTimeReload;
    public float spread = 0f;
    public Action OnTouchFire;
    public Action OnStartReload;
    public Action OnReloadLoop;
    public Action OnClipEmpty;
    public Action OnFinishReload;
    public Action OnReloadDone;
    public Action OnInventoryUpdate;
    public Action OnBulletUpdate;
    public Action OnShoot;
    [HideInInspector]
    public int maxReloadLoop;
    protected int _bulletInClip;
    [HideInInspector]
    public int bulletInClip
    {
        get { return _bulletInClip; }
        set
        {
            _bulletInClip = value;
            OnBulletUpdate?.Invoke();
        }
    }
    protected bool reload = true;

    protected int _intventory;
    [HideInInspector]
    public int inventory
    {
        get { return _intventory; }
        set
        {
            _intventory = value;
            OnInventoryUpdate?.Invoke();
        }
    }

    public ParticleSystem muzzeEffect;
    protected bool autoReload;
    private Transform lookAtOrg;

    public override void InitData(TF2Player player, Transform bone, bool isRemote = false)
    {
        base.InitData(player, bone, isRemote);

        if (isRemote && muzzeEffect != null)
        {
            LayerMask normalLayer = LayerMask.NameToLayer("Default");
            muzzeEffect.gameObject.layer = normalLayer;
            foreach (Transform child in muzzeEffect.transform)
            {
                child.gameObject.layer = normalLayer;
            }
        }

        bulletInClip = weaponConfig.weaponStats.clipSize;
        inventory = weaponConfig.weaponStats.inventorySize;
        player.healthManager.OnRespawn += ResetGun;
        //if (player.isBot)
        //{
        //    autoReload = true;
        //}
        //else
        //{
        //    autoReload = SettingHelper.autoReload;
        //    SettingHelper.OnAutoReloadUpdate += OnAutoReloadUpdate;
        //}

        //lookAtOrg = player.remotePlayer.tPSLookAt.aim;
        //if (lookAtOrg == null)
        //{
        //    lookAtOrg = shotPoints[0];
        //}
    }


    //public void OnDestroy()
    //{
    //    if (!playerOwner.isBot)
    //        SettingHelper.OnAutoReloadUpdate -= OnAutoReloadUpdate;
    //}

    private void OnAutoReloadUpdate(bool value)
    {
        autoReload = value;
    }

    private void OnEnable()
    {
        fire = false;
        reload = false;
        weaponState = WeaponState.Idle;
        blockFire = false;
    }

    public void ResetGun()
    {
        bulletInClip = weaponConfig.weaponStats.clipSize;
        inventory = weaponConfig.weaponStats.inventorySize;
    }

    public override void BeginFire()
    {
        if (blockFire) return;
        if (bulletInClip <= 0 && weaponState != WeaponState.Reloading)
        {
            OnClipEmpty?.Invoke();
            //if (autoReload)
            StartReload();
            return;
        }

        base.BeginFireSuccess();
    }

    public override void FireLoop()
    {
        if (blockFire || !fire || weaponState == WeaponState.Reloading) return;
        if (bulletInClip <= 0 && weaponState != WeaponState.Reloading)
        {
            //FinishFire();
            //if (autoReload)
            StartReload();
            OnClipEmpty?.Invoke();
            return;
        }


        FireLoopSuccess();
    }

    public override void FireLoopSuccess()
    {
        bulletInClip--;
        Shoot();
        base.FireLoopSuccess();
        ReloadIfEmpty();
    }

    /// <summary>Brawl-Stars style: reload starts right after the last bullet is fired (Tap and Hold), not on the next fire attempt.</summary>
    protected void ReloadIfEmpty()
    {
        if (bulletInClip > 0 || weaponState == WeaponState.Reloading || !gameObject.activeInHierarchy) return;
        StartReload();
    }

    protected float reloadStartTime, reloadDuration;
    public bool IsReloading => weaponState == WeaponState.Reloading;
    /// <summary>Reload progress 0..1 (time-based, used by FootRing), -1 when not reloading.</summary>
    public float ReloadProgress => !IsReloading ? -1f : reloadDuration <= 0f ? 1f : Mathf.Clamp01((Time.time - reloadStartTime) / reloadDuration);


    public override void FinishFire()
    {
        base.FinishFire();
        if (autoReload && bulletInClip < weaponConfig.weaponStats.clipSize)
            StartReload();
    }

    public virtual void StartReload()
    {
        if (weaponState == WeaponState.Reloading || inventory <= 0) return;
        //blockFire = true;
        weaponState = WeaponState.Reloading;
        OnStartReload?.Invoke();

        int bulletNeed = weaponConfig.weaponStats.clipSize - bulletInClip;
        this.maxReloadLoop = inventory >= bulletNeed ? bulletNeed : inventory;
        reload = true;
        //weaponState = WeaponState.DelayStartReload;
        reloadStartTime = Time.time;
        var st = weaponConfig.weaponStats;
        reloadDuration = st.delayReload + (st.timeReload > 0 ? maxReloadLoop * st.timeReload : 0f) + st.delayFinishReload;
        StartCoroutine(LoopReload());
    }

    public virtual void ReloadLoop()
    {
        //OnReloadLoop?.Invoke();
        bulletInClip++;
        inventory--;
        if (weaponConfig.reloadSound != null)
        {
            audioSource.PlayOneShot(weaponConfig.reloadSound);
        }

    }

    public virtual void FinishReload()
    {
        reload = false;
        //weaponState = WeaponState.DelayFinishReload;
        //OnFinishReload?.Invoke();
        //isReloading = false;
    }

    public virtual void Shoot()
    {
        damageObject.isOwner = playerOwner.photonView.IsMine;
        SpawnShot(shotPoints[0], AimForward(shotPoints[0]), true);

        if (muzzeEffect != null)
            muzzeEffect.Play();

        playerOwner.playerNetwork.SyncFire();
        OnShoot?.Invoke();
    }

    /// <summary>False for lobbed projectiles (grenades / stickies) that must keep the barrel's upward angle.</summary>
    protected virtual bool FlatAim => true;

    /// <summary>Aim direction: the player's facing (= aim joystick, flat) for humans, the barrel for bots / lobbed projectiles.</summary>
    protected Vector3 AimForward(Transform shotPoint)
    {
        if (!FlatAim || playerOwner == null || playerOwner.isBot) return shotPoint.forward;
        Vector3 f = playerOwner.transform.forward; f.y = 0f;
        return f.sqrMagnitude > 1e-6f ? f.normalized : shotPoint.forward;
    }

    /// <summary>One shot = weaponConfig.pellets bullets inside the aim cone (aimAngle), each flying weaponConfig.range metres.</summary>
    protected void SpawnShot(Transform shotPoint, Vector3 forward, bool rollDamage)
    {
        int n = Mathf.Max(1, weaponConfig.pellets);
        float half = Mathf.Max(0f, weaponConfig.aimAngle) * 0.5f;
        if (rollDamage)
            damageObject.damage = UnityEngine.Random.Range(weaponConfig.weaponStats.minDamage, weaponConfig.weaponStats.maxDamage) / n;
        // The indicator fan starts at the player's feet. Flat shots therefore start on the fan's axis
        // (player centre, at barrel height, pushed forward to the barrel) and fly only what is left of 'range',
        // so a bullet never goes past the indicator, whatever the barrel offset is.
        Vector3 spawn = shotPoint.position;
        float dist = weaponConfig.range;
        if (FlatAim && playerOwner != null)
        {
            Vector3 flat = forward; flat.y = 0f;
            if (flat.sqrMagnitude > 1e-6f)
            {
                flat.Normalize();
                Vector3 center = playerOwner.transform.position; center.y = shotPoint.position.y;
                float ahead = Mathf.Clamp(Vector3.Dot(shotPoint.position - center, flat), 0f, dist * 0.5f);
                spawn = center + flat * ahead;
                dist -= ahead;
            }
        }
        for (int i = 0; i < n; i++)
        {
            float yaw = n == 1 ? UnityEngine.Random.Range(-half, half) : Mathf.Lerp(-half, half, i / (float)(n - 1));
            Vector3 dir = (Quaternion.AngleAxis(yaw, Vector3.up) * forward).normalized;
            ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), spawn, Quaternion.LookRotation(dir), dir, damageObject, dist);
        }
    }


    //public virtual void BotShoot()
    //{
    //    //Calculate direction from attackPoint to targetPoint
    //    Vector3 directionWithoutSpread = playerOwner.targetLook - shotPoints[0].position;

    //    //Calculate spread
    //    float x = UnityEngine.Random.Range(-spread, spread);
    //    float y = UnityEngine.Random.Range(-spread, spread);

    //    //Calculate new direction with spread
    //    Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0); //Just add spread to last direction

    //    //Instantiate bullet/projectile
    //    damageObject.damage = UnityEngine.Random.Range(weaponConfig.minDamage, weaponConfig.maxDamage);
    //    damageObject.isOwner = playerOwner.playerNetwork.isMine;
    //    PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), shotPoints[0].position, shotPoints[0].rotation, directionWithSpread.normalized, damageObject); //store instantiated bullet in currentBullet
    //    playerOwner.playerNetwork.SyncFire();
    //}

    public override void RemoteFire()
    {
        base.RemoteFire();
        Vector3 aim = playerOwner.targetLook - shotPoints[0].position;
        if (FlatAim) aim.y = 0f;
        SpawnShot(shotPoints[0], aim.sqrMagnitude > 1e-6f ? aim.normalized : shotPoints[0].forward, false);
        if (muzzeEffect != null)
            muzzeEffect.Play();
    }

    public virtual IEnumerator LoopReload()
    {

        if (weaponConfig.weaponStats.delayReload > 0)
        {
            yield return new WaitForSeconds(weaponConfig.weaponStats.delayReload);
            //blockFire = false;
        }

        if (weaponConfig.weaponStats.timeReload > 0)
        {
            int count = 0;
            while (count < maxReloadLoop && reload)
            {

                //if (Time.time >= nextTimeReload)
                //{
                OnReloadLoop?.Invoke();
                //ReloadLoop();
                //nextTimeReload = Time.time + weaponConfig.timeReload;
                //count++;
                //}
                //yield return null;
                yield return new WaitForSeconds(weaponConfig.weaponStats.timeReload);
                ReloadLoop();
                count++;
                if (fire) break;
            }
        }
        else
        {
            if (weaponConfig.reloadSound != null)
            {
                audioSource.PlayOneShot(weaponConfig.reloadSound);
            }
            bulletInClip += maxReloadLoop;
            inventory -= maxReloadLoop;
        }

        reload = false;

        OnReloadDone?.Invoke();

        if (weaponConfig.weaponStats.delayFinishReload > 0)
        {
            //blockFire = true;
            //weaponState = WeaponState.DelayFinishReload;
            float reloadTimeRemaining = weaponConfig.weaponStats.delayFinishReload;
            yield return new WaitForSeconds(reloadTimeRemaining);

        }

        OnFinishReload?.Invoke();
        blockFire = false;
        if (weaponState == WeaponState.Reloading)
            weaponState = WeaponState.Idle;

    }

    public void Buff(float rance, bool constranClip = false)
    {
        if (!playerOwner.isBot && fire) return;
        if (weaponConfig.weaponStats.inventorySize <= 0)
        {
            int ammoAdd = Mathf.RoundToInt(rance * weaponConfig.weaponStats.clipSize);
            bulletInClip = Mathf.Min(bulletInClip + ammoAdd, weaponConfig.weaponStats.clipSize);
        }
        else
        {
            int ammoAdd = Mathf.RoundToInt(rance * weaponConfig.weaponStats.inventorySize);
            inventory = Mathf.Min(inventory + ammoAdd, weaponConfig.weaponStats.inventorySize);

            if (constranClip)
            {
                int ammoAddClip = Mathf.RoundToInt(rance * weaponConfig.weaponStats.clipSize);
                bulletInClip = Mathf.Min(bulletInClip + ammoAddClip, weaponConfig.weaponStats.clipSize);
            }
        }
    }

    public override void OnHide()
    {
        FinishReload();
        blockFire = false;
        weaponState = WeaponState.Idle;
        base.OnHide();
    }


    public override IEnumerator LoopFire()
    {
        while (weaponState == WeaponState.Reloading)
        {
            yield return null;
        }
        weaponState = WeaponState.Firing;
        if (weaponConfig.weaponStats.delayFire > 0)
            yield return new WaitForSeconds(weaponConfig.weaponStats.delayFire);

        while (fire)
        {
            if (weaponState == WeaponState.Reloading) yield return null;
            if (Time.time >= timeNextShot)
            {
                FireLoop();
                timeNextShot = Time.time + weaponConfig.weaponStats.interval;
            }
            yield return null;
        }

        if (weaponConfig.weaponStats.delayFinishFire > 0)
            yield return new WaitForSeconds(weaponConfig.weaponStats.delayFinishFire);

        if (weaponState == WeaponState.Firing)
            weaponState = WeaponState.Idle;
    }
}
