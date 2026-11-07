using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SentryController : BuildableEntry
{
    private Vector3 defaultTargetPos;
    public Transform target;
    public FieldOfView fov;

    private bool firing;
    public float followSpeed;
    public SentryLevelController[] sentryLevelControllers;
    public Transform[] randomTargets;
    public AudioClip sentryShootSound;
    
    public override void Start()
    {
        base.Start();
        defaultTargetPos = target.localPosition;
        fov.team = team;
    }

    public override void OnSwitchMaster()
    {
        base.OnSwitchMaster();
        if (PhotonNetwork.IsMasterClient)
        {
            fov.enabled = true;
        }
        else
        {
            fov.enabled = false;
        }
    }

    public override void OnBuildCompleted(int _level)
    {
        state = BuildableState.Idle;
        if (_level < level)
        {
            Upgrade(_level);
            return;
        }
        level = _level;
        if (level == 3)
        {
            mainLevel = ReferenceLevels[3];
        }
        else
            mainLevel = ReferenceLevels[_level - 1];
        
        OnBuildComplete?.Invoke(_level);
        if (!photonView.IsMine && firing)
        {
            sentryLevelControllers[level - 1].StartFire();
        }
    }

    public override void Upgrade(int _level)
    {
        if (state != BuildableState.Idle || _level >= 3 || _level <= 0) return;
        levelBuilding = _level;
        target.localPosition = defaultTargetPos;
        ReferenceLevels[_level - 1].gameObject.SetActive(false);
        float buildTime = ReferenceLevels[_level].StartBuild();
        state = BuildableState.Building;
        OnStartBuild?.Invoke(buildTime);
    }

    private int targetIndex;
    public void Update()
    {
        if(!photonView.IsMine) return;
        
        if (state != BuildableState.Idle)
        {
            if (firing)
            {
                SyncFire(false);
            }
            return;
        }
        if (fov.canSeePlayer)
        {
            target.position =
                Vector3.MoveTowards(target.position, fov.targetInField.position + Vector3.up, followSpeed * Time.deltaTime);
            if (!firing && sentryLevelControllers[level - 1].CheckCanFire())
            {
                SyncFire(true);
            }
        }
        else
        {
            if (firing)
                SyncFire(false);

            if(Vector3.Distance(target.position, randomTargets[targetIndex].position) <= 0.5f)
            {
                targetIndex = UnityEngine.Random.Range(0, randomTargets.Length);
            }
            else
            {
                target.position = Vector3.MoveTowards(target.position, randomTargets[targetIndex].position, followSpeed / 10 * Time.deltaTime);
            }
            
        }
    }
    
    
    public void SyncFire(bool fire)
    {
        RPC_Fire(fire);
        photonView.RPC(nameof(RPC_Fire), RpcTarget.Others, fire);
    }

    [PunRPC]
    public void RPC_Fire(bool fire)
    {
        firing = fire;
        if (fire)
            sentryLevelControllers[level - 1].StartFire();
        else
        {
            sentryLevelControllers[level - 1].FinishFire();
        }
    }
}
