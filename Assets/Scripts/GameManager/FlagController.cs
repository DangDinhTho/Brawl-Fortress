using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagController : MonoBehaviour
{
    public PhotonView photonView;
    public FlagState state;
    [HideInInspector]
    public TF2Player owner;
    public LayerMask groundMask;
    public Team team;

    public Action<FlagState> OnstateUpdate;


    IEnumerator WaitBackHome()
    {
        float timeCountDown = ConfigManager.timeFlagBackHome;
        while (timeCountDown > 0)
        {
            if(state != FlagState.dropped) yield break;
            timeCountDown -= Time.deltaTime;
            yield return null;
        }
        if (photonView.IsMine && state == FlagState.dropped)
        {
            Sync_BackHome(false);
        }
    }

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
        if(Physics.Raycast(owner.transform.position, Vector3.down, out hit, 100f, groundMask))
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
    
    public void Sync_BackHome(bool success)
    {
        RPC_BackHome(success);
        photonView.RPC(nameof(RPC_BackHome), RpcTarget.Others, success);
    }

    [PunRPC]
    public void RPC_Pickup(int viewID)
    {
        TF2Player player = PhotonView.Find(viewID).GetComponent<TF2Player>();
        transform.parent = player.remotePlayer.remoteBone.Find("bip_pelvis/bip_spine_0/bip_spine_1/bip_spine_2/bip_spine_3");
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        state = FlagState.moving;
        OnstateUpdate?.Invoke(state);
        owner = player;
        player.healthManager.OnDeath += OnPlayerDeath;
        if (player.isMainPlayer)
        {
            CaptureTheFlagHUD.instan.OnMainPlayerHold(player);
        }
    }


    [PunRPC]
    public void RPC_Drop(Vector3 position)
    {
        transform.parent = null;
        transform.position = position;
        transform.rotation = Quaternion.Euler(-62f, 0, 0);
        state = FlagState.dropped;
        OnstateUpdate?.Invoke(state);
        owner.healthManager.OnDeath -= OnPlayerDeath;
        if (owner.isMainPlayer)
        {
            CaptureTheFlagHUD.instan.OnMainPlayerDrop(owner);
        }
        owner = null;

        StartCoroutine(WaitBackHome());
    }
    
    
    [PunRPC]
    public void RPC_BackHome(bool sucsess)
    {
        transform.parent = null;
        transform.position = CaptureTheFlagManager.instan.homePositions[(int)team];
        transform.rotation = Quaternion.Euler(-62f, 0, 0);
        state = FlagState.home;
        OnstateUpdate?.Invoke(state);
        if (owner != null)
        {
            owner.healthManager.OnDeath -= OnPlayerDeath;
            if (owner.isMainPlayer)
            {
                CaptureTheFlagHUD.instan.OnMainPlayerDrop(owner);
            }

            owner = null;
        }

        if (sucsess)
        {
            CaptureTheFlagManager.instan.AddScore(ConfigManager.GetEnemyTeam(team));
        }
    }
}

public enum FlagState: byte
{
    home,
    moving,
    dropped
}