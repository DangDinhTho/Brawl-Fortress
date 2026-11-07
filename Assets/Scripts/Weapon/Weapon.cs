using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [HideInInspector]
    public TF2Player playerOwner;
    public WeaponId weaponId;
    public WeaponConfig weaponConfig;
    public Action OnBeginFire;
    public Action OnFire;
    public Action OnFinishFire;
    public Action OnWeaponHide;
    public AudioSource audioSource;

    protected float timeNextShot;
    public AlignBones alignBones;
    protected WeaponState weaponState;
    protected bool blockFire;
    [HideInInspector]
    public bool fire;

    protected Camera fpsCam;

    protected DamageObject damageObject;
    //private Transform weaponContainer;
    //private Rigidbody _rigidbody;

    private void Awake()
    {
        if (alignBones == null)
            alignBones = GetComponent<AlignBones>();
        //_rigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    

    public virtual void InitData(TF2Player player, Transform bone, bool isRemote = false)
    {
        weaponConfig.LoadStats();
        this.playerOwner = player;
        if (alignBones)
            alignBones.Setup(bone);
        if (isRemote)
        {
            SetLayer(LayerMask.NameToLayer("Default"));
        }
        else
        {
            //fpsCam = player.fPSReferences.fpsCam;

        }
        damageObject = new DamageObject()
        {
            team = playerOwner.team,
            damage = weaponConfig.weaponStats.damage,
            isOwner = false,
            ownerId = playerOwner.viewID,
            owner = playerOwner
        };
        //weaponContainer = transform.parent;
    }

    public virtual void SetLayer(LayerMask layer)
    {
        this.gameObject.layer = layer;
        foreach(Transform child in transform)
        {
            child.gameObject.layer = layer;
        }
    }

    // Update is called once per frame
    public virtual void Update()
    {
        
    }

    public virtual void HideWeapon()
    {
        OnHide();
    }
    
    
    public virtual void BeginFire()
    {
        if(blockFire) return;
            
        //if(weaponState == WeaponState.Reloading) return;
        //weaponState = WeaponState.DelayStartFire;
        BeginFireSuccess();
    }

    public virtual void BeginFireSuccess()
    {
        if (playerOwner.healthManager.death) return;
        fire = true;
        OnBeginFire?.Invoke();
        if (weaponConfig.beginFireSound != null)
        {
            audioSource.PlayOneShot(weaponConfig.beginFireSound);
        }
        StartCoroutine(LoopFire());
        
    }

    public virtual void FireLoop()
    {
        if(blockFire) return;
        FireLoopSuccess();
    }

    public virtual void FireLoopSuccess()
    {
        OnFire?.Invoke();
        if (weaponConfig.shotSound != null)
        {
            audioSource.PlayOneShot(weaponConfig.shotSound);
        }
    }

    public virtual void FinishFire()
    {
        fire = false;
        if (audioSource != null && weaponConfig.finishFireSound != null)
        {
            audioSource.PlayOneShot(weaponConfig.finishFireSound);
        }
        OnFinishFire?.Invoke();

    }


    public virtual void RemoteFire()
    {
        if (weaponConfig.shotSound != null)
        {
            audioSource.PlayOneShot(weaponConfig.shotSound);
        }
    }

    public virtual void RemoteFireDeployedStart()
    {
        if (weaponConfig.beginFireSound != null)
        {
            audioSource.PlayOneShot(weaponConfig.beginFireSound);
        }
    }

    public virtual void RemoteFireDeployedFinish()
    {
        if (weaponConfig.finishFireSound != null)
        {
            audioSource.PlayOneShot(weaponConfig.finishFireSound);
        }
    }

    public virtual void RemoteFireLoopStart()
    {
        fire = true;
        StartCoroutine(StartRemoteFireLoop());
    }

    public virtual void RemoteFireLoopFinish()
    {
        fire = false;
    }

    public virtual IEnumerator LoopFire()
    {
        weaponState = WeaponState.Firing;
        if (weaponConfig.weaponStats.delayFire > 0)
            yield return new WaitForSeconds(weaponConfig.weaponStats.delayFire);
        
        while (fire)
        {
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

    public virtual IEnumerator StartRemoteFireLoop()
    {
        weaponState = WeaponState.Firing;
        if (weaponConfig.weaponStats.delayFire > 0)
            yield return new WaitForSeconds(weaponConfig.weaponStats.delayFire);

        while (fire)
        {
            if (Time.time >= timeNextShot)
            {
                RemoteFire();
                timeNextShot = Time.time + weaponConfig.weaponStats.interval;
            }
            yield return null;
        }

        if (weaponConfig.weaponStats.delayFinishFire > 0)
            yield return new WaitForSeconds(weaponConfig.weaponStats.delayFinishFire);

        if (weaponState == WeaponState.Firing)
            weaponState = WeaponState.Idle;
    }

    public virtual void OnDeath()
    {
        // alignBones.enabled = false;
        // transform.parent = null;
        // _rigidbody.isKinematic = false;
        OnHide();
    }

    public virtual void OnRespawn()
    {
        ShowWeapon();
        // _rigidbody.isKinematic = true;
        // transform.parent = weaponContainer;
        // transform.localPosition = Vector3.zero;
        // transform.rotation = Quaternion.Euler(Vector3.zero);
        // alignBones.enabled = true;
    }

    public virtual void ShowWeapon()
    {
        gameObject.SetActive(true);
    }

    public virtual void OnHide()
    {
        FinishFire();
        OnWeaponHide?.Invoke();
        gameObject.SetActive(false);
    }
}

