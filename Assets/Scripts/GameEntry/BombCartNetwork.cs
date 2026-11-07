using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombCartNetwork : MonoBehaviour
{
    public PhotonView photonView;
    public BombCartController bombCartController;
    public AudioSource audioSource;

    public void StartGame()
    {
        OnSwitchMaster();
        ArenaNetworkManager.instan.OnSwitchMasterPlayer += OnSwitchMaster;
        audioSource.Play();
    }

    public void HideCart()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void OnSwitchMaster()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            bombCartController.InitOnMaster();
        }
        else
        {
            bombCartController.InitOnNonMaster();
        }
    }

    public void SyncDeployBomb()
    {
        RPC_DeployBomb();
        photonView.RPC(nameof(RPC_DeployBomb), RpcTarget.Others);
    }

    [PunRPC]
    public void RPC_DeployBomb()
    {
        PayloadManager.instance.OnDeployBomb();
        ArenaNetworkManager.instan.OnSwitchMasterPlayer -= OnSwitchMaster;
        //StartCoroutine(WaitEndGame());
    }

    // IEnumerator WaitEndGame()
    // {
    //     yield return new WaitForSeconds(3);
    //
    //     if (PhotonNetwork.IsMasterClient)
    //     {
    //         
    //     }
    // }
}
