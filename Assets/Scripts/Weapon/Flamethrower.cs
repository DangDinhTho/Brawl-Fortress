using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flamethrower : Gun
{
    public GameObject flame;
    public FlameDamage flameDamage;
    public AudioClip fireSound;

    public override void InitData(TF2Player player, Transform bone, bool isRemote = false)
    {
        base.InitData(player, bone, isRemote);
        if (isRemote)
        {
            LayerMask normalLayer = LayerMask.NameToLayer("Default");
            flame.gameObject.layer = normalLayer;
            foreach (Transform child in flame.transform)
            {
                child.gameObject.layer = normalLayer;
            }
            
        }
        else
        {
            ParticleSystem[] particleSystems = flame.GetComponentsInChildren<ParticleSystem>();
            if(particleSystems != null)
            {
                for(int i = 0; i < particleSystems.Length; i++)
                {
                    particleSystems[i].simulationSpace = ParticleSystemSimulationSpace.World;
                }
            }
        }
        flameDamage.gameObject.layer = LayerMask.NameToLayer("Bullet");

        flameDamage.damageObject = damageObject;
        flameDamage.damageObject.isOwner = player.photonView.IsMine;
    }


    public override void BeginFireSuccess()
    {
        base.BeginFireSuccess();
        playerOwner.playerNetwork.SyncFireLoopStart();
        audioSource.PlayDelayed(weaponConfig.weaponStats.delayFire);
    }

    public override void BeginFire()
    {
        if (blockFire) return;
        OnTouchFire?.Invoke();
        playerOwner.playerNetwork.SyncFireDeployedStart();
        //playerOwner.FPSPlayer.runSpeed /= 2;

        damageActive = false;
        flameDamage.gameObject.SetActive(false);

        if (bulletInClip <= 0) return;
        BeginFireSuccess();

    }

    public override void FinishFire()
    {
        base.FinishFire();
        playerOwner.playerNetwork.SyncFireDeployedFinish();
        //playerOwner.FPSPlayer.runSpeed *= 2;
        shotPoints[0].gameObject.SetActive(false);
        damageActive = false;
        flameDamage.gameObject.SetActive(false);
        audioSource.Stop();
    }

    public override void FireLoop()
    {
        if (blockFire || !fire) return;
        if (bulletInClip <= 0)
        {
            fire = false;
            FinishFire();
            playerOwner.playerNetwork.SyncFireLoopFinish();
            OnClipEmpty?.Invoke();
            return;
        }
        FireLoopSuccess();
    }

    public override void FireLoopSuccess()
    {
        shotPoints[0].gameObject.SetActive(true);
        Shoot();
    }

    private bool damageActive;
    public override void Shoot()
    {
        damageActive = !damageActive;
        flameDamage.gameObject.SetActive(damageActive);
        bulletInClip--;
    }


    public override void RemoteFire()
    {
        
    }

    public override void RemoteFireLoopStart()
    {
        //base.RemoteFireLoopStart();
        shotPoints[0].gameObject.SetActive(true);
        audioSource.Play();

    }

    public override void RemoteFireLoopFinish()
    {
        shotPoints[0].gameObject.SetActive(false);
        audioSource.Stop();
        //base.RemoteFireLoopFinish();
    }



    TF2Player flameOwner;

    /// <summary>Top-down: the flame comes out of the barrel tip (its bone) but points flat along the character's aim, not
    /// along the weapon bone, which the run / fire animations swing (the flame looked off the barrel, and tilted 17 deg up).</summary>
    void LateUpdate()
    {
        if (flame == null || !flame.gameObject.activeInHierarchy) return;
        if (flameOwner == null) flameOwner = GetComponentInParent<TF2Player>();
        if (flameOwner == null) return;
        Vector3 f = flameOwner.transform.forward; f.y = 0f;
        if (f.sqrMagnitude < 1e-4f) return;
        flame.transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
    }
}
