using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class TankBombNetwork : MonoBehaviour
{
    public PhotonView photonView;
    public TankBomb tankBomb;

    private void Start()
    {
        ArenaNetworkManager.instan.OnSwitchMasterPlayer += OnSwitchedMaster;
    }

    public void StartRun()
    {
        OnSwitchedMaster();
        tankBomb.StartRun();
    }

    public void StartWave()
    {
        SyncStartTank();
    }

    public void OnSwitchedMaster()
    {
        if (!ManvsMachineManager.instan.tankBombStated) return;
        if (PhotonNetwork.IsMasterClient)
        {
            tankBomb.InitOnMaster();
        }
        else
        {
            tankBomb.InitOnNonMaster();
        }
    }

    public void DeployBomb()
    {
        SyncDeployBomb();
    }
    
    public void SyncStartTank()
    {
        RPC_StartTank();
        photonView.RPC(nameof(RPC_StartTank), RpcTarget.Others);
    }

    [PunRPC]
    public void RPC_StartTank()
    {
        
        ManvsMachineManager.instan.SetCurrentBomb(this.transform, 2);
        
    }

    public void SyncDeployBomb()
    {
        RPC_DeployBomb();
        photonView.RPC(nameof(RPC_DeployBomb), RpcTarget.Others);
    }

    [PunRPC]
    public void RPC_DeployBomb()
    {
        //StartCoroutine(WaitEndGame());
        tankBomb.Deploy();
        ManvsMachineManager.instan.OnDeploy(12);
        ArenaNetworkManager.instan.OnSwitchMasterPlayer -= OnSwitchedMaster;
    }

    // IEnumerator WaitEndGame()
    // {
    //     yield return new WaitForSeconds(2);
    //
    //     if (PhotonNetwork.IsMasterClient)
    //     {
    //         ArenaNetworkManager.instan.EndGame(0, 1);
    //     }
    // }
}
