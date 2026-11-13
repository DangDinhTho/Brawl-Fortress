using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagController : MonoBehaviour
{
    // Max horizontal distance (m) between the carrier's feet and the flag for a pickup.
    // The pickup trigger spheres (r 1.25) are larger than this, so the distance is re-checked in OnTriggerStay.
    public const float PickupDistance = 1.0f;
    const string SpinePath = "bip_pelvis/bip_spine_0/bip_spine_1/bip_spine_2/bip_spine_3";

    public PhotonView photonView;
    public FlagState state;
    [HideInInspector]
    public TF2Player owner;
    public LayerMask groundMask;
    public Team team;

    [Header("Carry pose (on the carrier's back)")]
    public float backOffset = 0.5f;
    public float upOffset = -0.12f;
    public float carryScale = 1.15f;

    public Action<FlagState> OnstateUpdate;

    // Rotation (root space) that makes the case lie flat on the ground; used to build the carry pose.
    static readonly Quaternion GroundRotation = Quaternion.Euler(-62f, 0, 0);
    // Maps the lying case (thin axis = up, long axis = forward) onto an upright back pose: thin axis -> back, long axis -> up, width -> right.
    static readonly Quaternion BackRotation = Quaternion.LookRotation(Vector3.up, Vector3.back);

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

    public bool CanBePickedBy(TF2Player player)
    {
        if (player == null || state == FlagState.moving || player.team == team) return false;
        Vector3 d = transform.position - player.transform.position;
        if (Mathf.Abs(d.y) > 2f) return false;
        d.y = 0;
        return d.sqrMagnitude <= PickupDistance * PickupDistance;
    }

    public void TryPickup(TF2Player player)
    {
        if (CanBePickedBy(player)) Pickup(player);
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

    void AttachToBack(TF2Player player)
    {
        RemotePlayer rp = player.remotePlayer;
        Transform body = rp != null ? rp.transform : player.transform;
        Transform spine = null;
        if (rp != null && rp.remoteBone != null) spine = rp.remoteBone.Find(SpinePath);
        if (spine == null && rp != null) spine = rp.spine1;
        if (spine == null) spine = body;

        transform.SetParent(spine, true);
        transform.rotation = body.rotation * BackRotation * GroundRotation;
        transform.position = spine.position - body.forward * backOffset + body.up * upOffset;
        Vector3 ls = spine.lossyScale;
        transform.localScale = new Vector3(carryScale / Mathf.Max(ls.x, 0.0001f), carryScale / Mathf.Max(ls.y, 0.0001f), carryScale / Mathf.Max(ls.z, 0.0001f));
    }

    void ResetScale()
    {
        transform.localScale = Vector3.one;
    }

    [PunRPC]
    public void RPC_Pickup(int viewID)
    {
        TF2Player player = PhotonView.Find(viewID).GetComponent<TF2Player>();
        AttachToBack(player);
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
        ResetScale();
        transform.position = position;
        transform.rotation = GroundRotation;
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
        ResetScale();
        transform.position = CaptureTheFlagManager.instan.homePositions[(int)team];
        transform.rotation = GroundRotation;
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
