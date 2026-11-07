using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using System;

public class HealthBase : MonoBehaviour
{
    public int maxHealth;

    protected int _currentHealth;
    public int currentHealth
    {
        set
        {
            _currentHealth = value;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        get
        {
            return _currentHealth;
        }
    }
    [HideInInspector] public bool death;
    
    public Action OnDeath;
    public Action<int> OnDeathReason;
    public Action OnRespawn;
    public Action<Vector3> OnTakeDamage;
    public Action<int, int> OnHealthChanged;
    public PhotonView photonView;
    public Team team;
    public Transform textDamageTransform;
    [HideInInspector] public Transform centerBody;

    public virtual void Awake()
    {
        photonView = GetComponent<PhotonView>();
        Init();
    }

    public virtual void SetCenterBody()
    {
        centerBody = transform;
    }

    // Start is called before the first frame update
    public virtual void Init()
    {
        currentHealth = maxHealth;
        this.enabled = true;
        SetCenterBody();
    }

    public virtual void Buff(float rance)
    {
        int healthAdd = Mathf.RoundToInt(rance * maxHealth);
        currentHealth = Mathf.Min(currentHealth + healthAdd, maxHealth);
        SyncHealth(currentHealth);
    }

    public virtual void TakeDamage(DamageObject damageObject, Vector3 direction)
    {
        if(damageObject == null || !damageObject.isOwner || death) return;
        photonView.RPC(nameof(SyncTakeDamage), RpcTarget.Others, (int)damageObject.damage, damageObject.ownerId, direction);
        SyncTakeDamage((int)damageObject.damage, damageObject.ownerId, direction);
        if(damageObject.ownerId == ArenaManager.Instance.mainPlayer.viewID || photonView.ViewID == ArenaManager.Instance.mainPlayer.viewID)
        {
            ArenaManager.Instance.ShowTextDamage(damageObject.damage, textDamageTransform != null ? textDamageTransform : transform);
        }
    }

    [PunRPC]
    public virtual void SyncTakeDamage(int damage, int attacker, Vector3 direction)
    {
        if (death) return;
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            if (photonView.IsMine)
                Die(attacker, direction);
        }
        else
        {
            OnTakeDamage?.Invoke(direction);
        }
        ArenaManager.Instance.AddDamageForPlayer(attacker, damage);
    }


    public virtual void Die(int attacker, Vector3 direction)
    {
        photonView.RPC(nameof(SyncDie), RpcTarget.Others, attacker, direction);
        SyncDie(attacker, direction);
    }

    [PunRPC]
    public virtual void SyncDie(int attacker, Vector3 direction)
    {
        if (death) return;
        death = true;
        currentHealth = 0;
        OnDeath?.Invoke();
        OnDeathReason?.Invoke(attacker);
        
        ArenaManager.Instance.AddKillForPlayer(attacker, 1);
    }


    public virtual void SyncHealth(int _health)
    {
        photonView.RPC(nameof(RPC_SyncHealth), RpcTarget.Others, _health);
    }

    [PunRPC]
    public void RPC_SyncHealth(int _health)
    {
        currentHealth = _health;
    }

}
