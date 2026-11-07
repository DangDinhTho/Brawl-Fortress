using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class BuildableEntry : MonoBehaviour
{
    public BuildableType buildableType;
    public TF2Player owner;
    public PhotonView photonView;

    public BuildableLevel[] ReferenceLevels;
    public int level;
    [HideInInspector]
    public int levelBuilding;
    public BuildableLevel mainLevel;
    public BuildableState state;
    public int[] upgradeExp;
    [HideInInspector]
    public int currentExp;
    [HideInInspector]
    public Team team;

    public HealthBase health;
    
    public Action<float> OnStartBuild;
    public Action<int> OnBuildComplete;
    public Action<int, int> OnExpChanged;
    [HideInInspector]
    public float timeBuildingRemain;
    public AudioSource audioSource;
    public AudioClip explosionSound;

    public virtual void Start()
    {
        if (photonView.InstantiationData != null)
        {
            int ownerId = (int)photonView.InstantiationData[0];
            owner = PhotonNetwork.GetPhotonView(ownerId)?.transform.GetComponent<TF2Player>();
            team = owner.team;
            health.team = team;
            SetTeam(team);
        }

        for (int i = 0; i < ReferenceLevels.Length; i++)
        {
            ReferenceLevels[i].gameObject.SetActive(false);
        }
        float buildTime = ReferenceLevels[0].StartBuild();
        OnStartBuild?.Invoke(buildTime);
        //StartCoroutine(OnStartBuildLate(buildTime));
        state = BuildableState.Building;
        health.OnDeath += OnDeath;
        ArenaNetworkManager.instan.OnSwitchMasterPlayer += OnSwitchMaster;
        OnSwitchMaster();
        level = 1;
    }

    private void OnDestroy()
    {
        ArenaNetworkManager.instan.OnSwitchMasterPlayer -= OnSwitchMaster;
    }

    public virtual void OnSwitchMaster()
    {
        
    }

    public void SetTeam(Team team)
    {
        //for(int i = 0; i< ReferenceLevels.Length; i++)
        //{
        //    MeshDynamicColor[] meshDynamicColors = ReferenceLevels[i].GetComponentsInChildren<MeshDynamicColor>(true);
        //}

        MeshDynamicColor[] meshDynamicColors = GetComponentsInChildren<MeshDynamicColor>(true);
        if (meshDynamicColors != null)
        {
            for (int i = 0; i < meshDynamicColors.Length; i++)
            {
                meshDynamicColors[i].SetTeamColor(team);
            }
        }
    }

    //IEnumerator OnStartBuildLate(float buildTime)
    //{
    //    yield return new WaitForEndOfFrame();
    //    OnStartBuild?.Invoke(buildTime);
    //}

    public void OnDeath()
    {
        state = BuildableState.Died;
        ObjectPooling.Instance.Instantiate("BigExplosion", transform.position, transform.rotation);
        audioSource.PlayOneShot(explosionSound);
        health.OnDeath -= OnDeath;
        Destroy(gameObject, 0.5f);
        // if (photonView.IsMine)
        // {
        //     PhotonNetwork.Destroy(gameObject);
        // }
    }

    public virtual void AddExp(int exp)
    {
        if (state != BuildableState.Idle || level >= 3) return;
        //currentExp += exp;
        SyncEXP(currentExp + exp);
    }

    public virtual void OnBuildCompleted(int level)
    {
        state = BuildableState.Idle;
        OnBuildComplete?.Invoke(level);
    }

    public virtual void Upgrade(int _level)
    {
        state = BuildableState.Building;
        levelBuilding = _level;
    }

    public virtual bool OnMove()
    {
        if (state != BuildableState.Idle) return false;
        SyncMove();
        return true;
    }

    public virtual void OnDrop(Vector3 position, Quaternion rotation)
    {
        //gameObject.SetActive(true);
        SyncDrop(position, rotation);
    }

    public void SyncUpgrade(int level)
    {
        RPC_Upgrade(level);
        photonView.RPC(nameof(RPC_Upgrade), RpcTarget.Others, level);
    }

    [PunRPC]
    public void RPC_Upgrade(int level)
    {
        Upgrade(level);
        currentExp = 0;
    }

    public void SyncEXP(int exp)
    {
        RPC_EXP(exp);
        photonView.RPC(nameof(RPC_EXP), RpcTarget.Others, exp);
    }

    [PunRPC]
    public void RPC_EXP(int exp)
    {
        currentExp = exp;
        OnExpChanged?.Invoke(currentExp, upgradeExp[level - 1]);
        if (photonView.IsMine && currentExp >= upgradeExp[level - 1])
        {
            SyncUpgrade(level);
        }
    }

    public void SyncMove()
    {
        RPC_Move();
        photonView.RPC(nameof(RPC_Move), RpcTarget.Others);
    }

    [PunRPC]
    public void RPC_Move()
    {
        for (int i = 0; i < ReferenceLevels.Length; i++)
        {
            ReferenceLevels[i].gameObject.SetActive(false);
        }
        state = BuildableState.Moving;
    }

    public void SyncDrop(Vector3 position, Quaternion rotation)
    {
        RPC_Drop(position, rotation);
        photonView.RPC(nameof(RPC_Drop), RpcTarget.Others, position, rotation);
    }

    [PunRPC]
    public void RPC_Drop(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        float buildTime = ReferenceLevels[0].StartBuild();
        OnStartBuild?.Invoke(buildTime);
        state = BuildableState.Building;
    }
}

public enum BuildableState: byte
{
    Idle = 0,
    Building,
    Moving,
    Died
}

public enum BuildableType: byte
{
    None = 0,
    Sentry,
    Dispenser,
    TeleporterEnter,
    TeleporterExit,
}
