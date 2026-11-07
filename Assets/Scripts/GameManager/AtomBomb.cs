using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtomBomb : MonoBehaviour
{
    public PhotonView photonView;
    public FlagState state;
    [HideInInspector]
    public TF2Player owner;
    public LayerMask groundMask;
    public Team team;
    private bool notFirst;

    public Action<FlagState> OnstateUpdate;

    public void Pickup(TF2Player player)
    {
        if (player.photonView.IsMine)
        {
            Sync_Pickup(player.viewID);
        }
    }



    public void OnPlayerDeath()
    {
        if (owner.photonView.IsMine)
        {
            Drop();
        }
    }

    public void Drop()
    {
        RaycastHit hit;
        if (Physics.Raycast(owner.transform.position, Vector3.down, out hit, 100f, groundMask))
        {
            Sync_Drop(hit.point);
        }
        else
        {
            Vector3 pos = owner.transform.position;
            pos.y = 0;
            Sync_Drop(pos);
        }
    }

    public void DestroyBomb()
    {
        Sync_Destroy();
    }

    public void Deploy()
    {
        Sync_Deploy();
    }


    public void Sync_Pickup(int viewID)
    {
        RPC_Pickup(viewID);
        photonView.RPC(nameof(RPC_Pickup), RpcTarget.Others, viewID);
    }

    public void Sync_Drop(Vector3 position)
    {
        RPC_Drop(position);
        photonView.RPC(nameof(RPC_Drop), RpcTarget.Others, position);
    }
    
    public void Sync_Destroy()
    {
        RPC_Destroy();
        photonView.RPC(nameof(RPC_Destroy), RpcTarget.Others);
    }
    
    public void Sync_Deploy()
    {
        RPC_Deploy();
        photonView.RPC(nameof(RPC_Deploy), RpcTarget.Others);
    }


    [PunRPC]
    public void RPC_Pickup(int viewID)
    {
        TF2Robot tf2Robot = PhotonView.Find(viewID).GetComponent<TF2Robot>();
        if (tf2Robot == null) return;
        transform.parent = tf2Robot.remotePlayer.remoteBone.Find("bip_pelvis/bip_spine_0/bip_spine_1/bip_spine_2/bip_spine_3");
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        state = FlagState.moving;
        OnstateUpdate?.Invoke(state);
        owner = tf2Robot;
        tf2Robot.healthManager.OnDeath += OnPlayerDeath;
        if (!notFirst)
        {
            ManvsMachineManager.instan.SetCurrentBomb(transform, 1);
            notFirst = true;
        }
        tf2Robot.PickUpBomb();
    }


    [PunRPC]
    public void RPC_Drop(Vector3 position)
    {
        transform.parent = null;
        transform.position = position;
        transform.rotation = Quaternion.Euler(-70f, 0, 0);
        state = FlagState.dropped;
        OnstateUpdate?.Invoke(state);
        owner.healthManager.OnDeath -= OnPlayerDeath;
        
        owner = null;

    }
    
    [PunRPC]
    public void RPC_Destroy()
    {
        transform.parent = null;
        transform.position = new Vector3(0, -200f, 0);
        //transform.rotation = Quaternion.Euler(-62f, 0, 0);
        state = FlagState.dropped;
        OnstateUpdate?.Invoke(state);
        if (owner != null)
        {
            owner.healthManager.OnDeath -= OnPlayerDeath;
        }
        owner = null;
        gameObject.SetActive(false);
    }


    [PunRPC]
    public void RPC_Deploy()
    {
        if (owner == null) return;
        owner.remotePlayer.playerAnimator.Animator.SetTrigger("DeployBomb");
        ManvsMachineManager.instan.OnDeploy(6);
    }

}
