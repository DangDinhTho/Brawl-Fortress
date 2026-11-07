using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HealthManager : HealthBase
{
    protected TF2Player player;

    protected Ragdoll ragdoll;

    
    
    public event Action OnRespawn;
    public event Action<int> OnRespawnCountDownUpdate;
    protected int timeDeath;

    public override void Awake()
    {
        
    }
    public override void SetCenterBody()
    {

    }

    // Start is called before the first frame update
    public void Init(TF2Player _player)
    {
        this.player = _player;
        this.team = _player.team;

        var rigidBodies = GetComponentsInChildren<Rigidbody>(true);
        foreach (var rigidBody in rigidBodies)
        {
            if (rigidBody.name == "Pickup") continue;
            rigidBody.tag = "HitBox";
            rigidBody.gameObject.layer = LayerMask.NameToLayer("HitBox");
            HitBox hitBox = rigidBody.gameObject.AddComponent<HitBox>();
            //hitBox.healthManager = this;
        }

        ragdoll = player.remotePlayer.ragdoll;
        ragdoll.IgnorePlayerCollider();
        centerBody = ragdoll.hipRigidbody.transform;
        photonView = GetComponent<PhotonView>();

        maxHealth = player.characterConfig.characterStats.maxHealth;
        currentHealth = maxHealth;

        this.enabled = true;
    }

    public void TakeSelfDamage(int damage, Vector3 direction)
    {
        if (death) return;
        photonView.RPC(nameof(SyncTakeDamage), RpcTarget.Others, damage, player.viewID, direction);
        SyncTakeDamage(damage, player.viewID, direction);
    }

    public override void Buff(float rance)
    {
        int healthAdd = Mathf.RoundToInt(rance * maxHealth);
        currentHealth = Mathf.Min(currentHealth + healthAdd, maxHealth);
        SyncHealth(currentHealth);
    }


    public override void Die(int attacker, Vector3 direction)
    {
        photonView.RPC(nameof(SyncDie), RpcTarget.Others, attacker, direction);
        SyncDie(attacker, direction);
        int random = Random.Range(0, 2);
        if (random == 0)
        {
            ArenaNetworkManager.instan.DropKit(transform.position);
        }
        
    }

    [PunRPC]
    public override void SyncDie(int attacker, Vector3 direction)
    {
        if (death) return;
        death = true;
        currentHealth = 0;
        OnDeath?.Invoke();
        OnDeathReason?.Invoke(attacker);
        //StartCoroutine(WaitActivateRagdoll(direction));
        //player.SetShadow(false);
        ragdoll.ActivateRagdoll(direction);
        //ragdoll.ApplyForce(direction);

        ArenaManager.Instance.AddKillForPlayer(attacker, 1);
        ArenaManager.Instance.AddDeathForPlayer(player.viewID, 1);


        StartCoroutine(WaitRespawn());
        
        if(!player.isMainPlayer && ArenaManager.Instance.mainPlayer.healthManager.death && CameraController.instan.freeLook.enabled && CameraController.instan.freeLook.Follow == transform)
        {
            TF2Player teammate = ArenaManager.Instance.GetNextTeamMateAlive();
            CameraController.instan.SetFreeLook(true, teammate.transform, teammate.healthManager.centerBody);
        }
        
    }

    //public virtual IEnumerator WaitActivateRagdoll(Vector3 direction)
    //{
    //    yield return new WaitForSeconds(0.2f);
    //    ragdoll.ActivateRagdoll();
    //    ragdoll.ApplyForce(direction);
    //}

    public void Respawn()
    {
        photonView.RPC(nameof(SyncRespawn), RpcTarget.Others);
        SyncRespawn();
    }

    [PunRPC]
    public void SyncRespawn()
    {
        death = false;
        player.BackToDefaultPos();
        currentHealth = maxHealth;
        player.remotePlayer.gameObject.SetActive(true);
        ragdoll.DeactivateRagdoll();
        OnRespawn?.Invoke();
        if (player.isMainPlayer)
        {
            CameraController.instan.freeLook.Follow = player.transform;
        }
        //player.SetShadow(true);
    }
    
    public virtual IEnumerator WaitRespawn()
    {
        int maxTimeDeath = TimerManager.instance.GetDeathTime();
        timeDeath = maxTimeDeath;
        bool switchedCam = false;
        
        while(timeDeath > 0)
        {
            OnRespawnCountDownUpdate?.Invoke(timeDeath);
            timeDeath--;

            if(!switchedCam && timeDeath == maxTimeDeath - ConfigManager.timeToSwitchCamOnDeath)
            {
                if (player.isMainPlayer)
                {
                    TF2Player teammate = ArenaManager.Instance.GetNextTeamMateAlive();
                    CameraController.instan.SetFreeLook(true, teammate.transform, teammate.healthManager.centerBody);
                }
                switchedCam = true;
                player.remotePlayer.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(1);
        }

        if (photonView.IsMine)
            Respawn();
    }
}

/// Bad Pipes
/// Drunken Sailor
/// Oblooterated
/// Pooped Deck
/// Scotsmann's Stagger
/// Spent Well Spirits