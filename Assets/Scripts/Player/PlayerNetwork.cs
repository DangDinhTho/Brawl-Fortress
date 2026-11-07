using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNetwork : PhotonObject, IPunObservable
{

    public bool Test;
    public TF2Player player;
    [SerializeField]
    protected PhotonTransformViewPositionModel m_PositionModel = new PhotonTransformViewPositionModel();
    [SerializeField]
    protected PhotonTransformViewRotationModel m_RotationModel = new PhotonTransformViewRotationModel();

    
    protected bool m_ReceivedNetworkUpdate = false;
    protected Transform m_Transform;
    protected PhotonTransformViewPositionControl m_PositionControl;
    protected PhotonTransformViewRotationControl m_RotationControl;
    //private Vector3 HeadPos = Vector3.zero;// Head Look to
    public bool isMine;
    private PlayerFPSController fpControler;
    protected RemotePlayer remotePlayer;

    protected float m_Distance;
    protected Vector3 m_NetworkPosition;
    protected bool m_firstTake;


    protected PlayerFPState FPState = PlayerFPState.Idle;
    protected Vector3 velocity;
    protected PlayerState bodyState;
    //protected WeaponType weaponType;

    private void Awake()
    {
        m_Transform = transform;
        m_PositionControl = new PhotonTransformViewPositionControl(m_PositionModel);
        m_RotationControl = new PhotonTransformViewRotationControl(m_RotationModel);
    }

    void OnEnable()
    {
        m_firstTake = true;
    }

    // Start is called before the first frame update
    public virtual void Init()
    {
        //if (!Test)
        isMine = photonView.IsMine;
        

        fpControler = player.FPSPlayer;
        remotePlayer = player.remotePlayer;
        
        if (!isMine)
        {
            fpControler.enabled = false;
            //player.remotePlayer.gameObject.SetActive(true);
            //player.fPSReferences.gameObject.SetActive(false);
            player.FPSPlayer.m_CharacterController.enabled = false;
        }
        else
        {
            fpControler.enabled = true;
            //player.remotePlayer.gameObject.SetActive(false);
            //player.fPSReferences.gameObject.SetActive(true);
        }
        this.enabled = true;
    }

    
    // Update is called once per frame
    void Update()
    {
        ///if the player is not ours, then
        //if (PhotonNetwork.IsConnected == false || !PhotonNetwork.InRoom) return;

        if (photonView != null && !isMine)
        {
            OnRemotePlayer();

            player.targetLook = Vector3.MoveTowards(player.targetLook, this.m_NetworkPosition, this.m_Distance * Time.deltaTime * PhotonNetwork.SerializationRate);

        }
        else
        {
            OnLocalPlayer();
        }
    }

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        m_PositionControl.OnPhotonSerializeView(m_Transform.localPosition, stream, info);
        m_RotationControl.OnPhotonSerializeView(m_Transform.localRotation, stream, info);

#if UNITY_EDITOR
        if (isMine == false)
        {
            //DoDrawEstimatedPositionError();
        }
#endif

        if (stream.IsWriting)
        {
            //We own this player: send the others our data
            stream.SendNext(player.targetLook);
            stream.SendNext(fpControler.State);
            stream.SendNext(fpControler.fpsState);
            stream.SendNext(fpControler.Velocity);
            //stream.SendNext(fpControler.weaponType);
        }
        else
        {
            //Network player, receive data
            m_NetworkPosition = (Vector3)stream.ReceiveNext();

            if (m_firstTake)
            {
                player.targetLook = this.m_NetworkPosition;

                this.m_Distance = 0f;
                m_firstTake = false;
            }
            else
            {
                this.m_Distance = Vector3.Distance(player.targetLook, this.m_NetworkPosition);
            }

            bodyState = (PlayerState)stream.ReceiveNext();
            FPState = (PlayerFPState)stream.ReceiveNext();
            velocity = (Vector3)stream.ReceiveNext();
            //weaponType = (WeaponType)stream.ReceiveNext();
            m_ReceivedNetworkUpdate = true;
        }
    }

   public virtual void OnLocalPlayer()
    {
        //send the state of player local for remote animation
        remotePlayer.playerAnimator.BodyState = fpControler.State;
        remotePlayer.playerAnimator.Velocity = fpControler.Velocity;
        remotePlayer.playerAnimator.FPState = fpControler.fpsState;
        //remotePlayer.weaponType = fpControler.weaponType;
    }

    /// <summary>
    /// Function called each frame when the player is remote
    /// </summary>
   public virtual  void OnRemotePlayer()
    {
        if (m_Transform.parent == null)
        {
            UpdatePosition();
            UpdateRotation();
        }

        remotePlayer.playerAnimator.BodyState = bodyState;
        remotePlayer.playerAnimator.Velocity = velocity;
        remotePlayer.playerAnimator.FPState = FPState;
        //remotePlayer.weaponType = weaponType;

        //if (!isOneTeamMode)
        //{
        //    //Determine if remote player is teamMate or enemy
        //    if (isFriend)
        //        OnTeammatePlayer();
        //    else
        //        OnEnemyPlayer();
        //}
        //else
        //{
        //    OnEnemyPlayer();
        //}

        //if (!isWeaponBlocked)
        //{
        //    CurrentTPVGun();
        //    currentGunID = networkGunID;
        //}
    }

    public void UpdatePosition()
    {
        if (m_PositionModel.SynchronizeEnabled == false || m_ReceivedNetworkUpdate == false)
        {
            return;
        }

        m_Transform.localPosition = m_PositionControl.UpdatePosition(m_Transform.localPosition);
    }
    /// <summary>
    /// 
    /// </summary>
    public void UpdateRotation()
    {
        if (m_RotationModel.SynchronizeEnabled == false || m_ReceivedNetworkUpdate == false)
        {
            return;
        }

        m_Transform.localRotation = m_RotationControl.GetRotation(m_Transform.localRotation);
    }


    public void SyncSwitchWeapon(WeaponType weaponType)
    {
        photonView.RPC(nameof(RPC_SwitchWeapon), RpcTarget.Others, weaponType);
    }
    [PunRPC]
    public void RPC_SwitchWeapon(WeaponType weaponType)
    {
        if (remotePlayer == null) return;
        remotePlayer.SwitchWeapon(weaponType);

    }


    public void SyncFire()
    {
        //if(remotePlayer.gameObject.activeInHierarchy) RPC_RemoteFire();
        photonView.RPC(nameof(RPC_RemoteFire), RpcTarget.Others);
    }
    [PunRPC]
    public void RPC_RemoteFire()
    {
        remotePlayer.weapon.RemoteFire();
        remotePlayer.playerAnimator.FireAnimator();
    }


    public void SyncFireDeployedStart()
    {
        if (remotePlayer.gameObject.activeInHierarchy) RPC_RemoteFireDeployedStart();
        photonView.RPC(nameof(RPC_RemoteFireDeployedStart), RpcTarget.Others);
    }
    [PunRPC]
    public void RPC_RemoteFireDeployedStart()
    {
        remotePlayer.weapon.RemoteFireDeployedStart();
        remotePlayer.OnDeployed();
    }

    public void SyncFireDeployedFinish() {
        if (remotePlayer.gameObject.activeInHierarchy)
            RPC_RemoteFireDeployedFinish();
        photonView.RPC(nameof(RPC_RemoteFireDeployedFinish), RpcTarget.Others);
    }
    [PunRPC]
    public void RPC_RemoteFireDeployedFinish()
    {
        remotePlayer.weapon.RemoteFireLoopFinish();
        remotePlayer.playerAnimator.FireLoopFinishAnimator();
        remotePlayer.weapon.RemoteFireDeployedFinish();
        remotePlayer.OnFinishDeploy();
    }


    public void SyncFireLoopStart()
    {
        if (remotePlayer.gameObject.activeInHierarchy) RPC_RemoteFireLoopStart();
        photonView.RPC(nameof(RPC_RemoteFireLoopStart), RpcTarget.Others);
    }
    [PunRPC]
    public void RPC_RemoteFireLoopStart()
    {
        remotePlayer.weapon.RemoteFireLoopStart();
        remotePlayer.playerAnimator.FireLoopStartAnimator();
    }



    public void SyncFireLoopFinish() {
        if (remotePlayer.gameObject.activeInHierarchy)
            RPC_RemoteFireLoopFinish();
        photonView.RPC(nameof(RPC_RemoteFireLoopFinish), RpcTarget.Others);
    }
    [PunRPC]
    public void RPC_RemoteFireLoopFinish()
    {
        remotePlayer.weapon.RemoteFireLoopFinish();
        remotePlayer.playerAnimator.FireLoopFinishAnimator();
    }

    public void SyncFireEmpty() {
        if (remotePlayer.gameObject.activeInHierarchy)
            RPC_RemoteFireEmpty();
        photonView.RPC(nameof(RPC_RemoteFireEmpty), RpcTarget.Others);
    }
    [PunRPC]
    public void RPC_RemoteFireEmpty()
    {
        remotePlayer.weapon.RemoteFireLoopFinish();
        remotePlayer.playerAnimator.FireLoopFinishAnimator();
    }

    public void SyncInvisibility(bool invis)
    {
        photonView.RPC(nameof(RPC_RemoteInvisibility), RpcTarget.Others, invis);
    }

    [PunRPC]
    public void RPC_RemoteInvisibility(bool invis)
    {
        if (invis)
            remotePlayer.player.SetMeshInvsibility();
        else
            remotePlayer.player.SetMeshTeam();
    }

    public void SyncTaunt(bool state, Taunt taunt) => photonView.RPC(nameof(RPC_Taunt), RpcTarget.Others, state, taunt);
    [PunRPC]
    public void RPC_Taunt(bool state, Taunt taunt)
    {
        if (state)
        {
            remotePlayer.PlayTaunt(taunt);
        }
        else
        {
            remotePlayer.EndTaunt();
        }
    }


    public void SyncMedicGunTarget(int viewID) => photonView.RPC(nameof(RPC_SetMedicGunTarget), RpcTarget.Others, viewID);
    [PunRPC]
    public void RPC_SetMedicGunTarget(int viewID)
    {
        if(remotePlayer.weapon is Medicgun)
        {
            ((Medicgun)remotePlayer.weapon).RemoteTargetViewId(viewID);
        }
        
    }
}
