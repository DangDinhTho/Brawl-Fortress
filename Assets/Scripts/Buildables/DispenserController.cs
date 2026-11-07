using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class DispenserController : BuildableEntry
{
    public EntryFieldOfView fov;
    public float[] speedBuffs;
    public float interval = 1;

    private float nextTimeBuff;
    
    public override void Start()
    {
        base.Start();
        state = BuildableState.Building;
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
        if (_level < this.level)
        {
            Upgrade(_level);
            return;
        }

        this.level = _level;
        mainLevel = ReferenceLevels[_level - 1];
        OnBuildComplete?.Invoke(_level);
    }

    public override void Upgrade(int _level)
    {
        if (state != BuildableState.Idle || _level >= 3 || _level <= 0) return;
        ReferenceLevels[_level - 1].gameObject.SetActive(false);
        float buildTime = ReferenceLevels[_level].StartBuild();
        state = BuildableState.Building;
        OnStartBuild?.Invoke(buildTime);
    }

    private void Update()
    {
        if (state != BuildableState.Idle) return;
        if (Time.time > nextTimeBuff)
        {
            for (int i = 0; i < fov.detectTeams[team].Count; i++)
            {
                TF2Player targetBuff = fov.detectTeams[team][i];
                if(!targetBuff.photonView.IsMine || targetBuff.isBot) continue;
                targetBuff.FPSPlayer.BuffWeapon(speedBuffs[level - 1], true);
                targetBuff.healthManager.Buff(speedBuffs[level - 1]);
            }

            nextTimeBuff = Time.time + interval;
        }
    }
}
