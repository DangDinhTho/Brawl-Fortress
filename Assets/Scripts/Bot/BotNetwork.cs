using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BotNetwork : PlayerNetwork
{
    private BotController botController;
    private TF2Bot bot;

    private void Awake()
    {
        m_Transform = transform;
        m_PositionControl = new PhotonTransformViewPositionControl(m_PositionModel);
        m_RotationControl = new PhotonTransformViewRotationControl(m_RotationModel);
        bot = (TF2Bot)player;
        botController = bot.botController;
    }

    void OnEnable()
    {
        m_firstTake = true;
    }

    // Start is called before the first frame update
    public override void Init()
    {
        isMine = PhotonNetwork.IsMasterClient; //photonView.IsMine;

        remotePlayer = bot.remotePlayer;

        if (!isMine)
        {
            bot.behaviorTree.enabled = false;
            botController.enabled = false;
            bot.agent.enabled = false;
            bot.fieldOfView.enabled = false;
        }
        else
        {
            //bot.behaviorTree.enabled = true;
            botController.enabled = true;
            bot.agent.enabled = true;
            bot.fieldOfView.enabled = true;
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

            bot.targetLook = Vector3.MoveTowards(bot.targetLook, this.m_NetworkPosition, this.m_Distance * Time.deltaTime * PhotonNetwork.SerializationRate);

        }
        else
        {
            OnLocalPlayer();
        }
    }

    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
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
            stream.SendNext(bot.targetLook);
            stream.SendNext(botController.State);
            stream.SendNext(botController.fpsState);
            stream.SendNext(botController.agent.velocity);
            //stream.SendNext(botController.weaponType);
        }
        else
        {
            //Network player, receive data
            m_NetworkPosition = (Vector3)stream.ReceiveNext();

            if (m_firstTake)
            {
                bot.targetLook = this.m_NetworkPosition;

                this.m_Distance = 0f;
                m_firstTake = false;
            }
            else
            {
                this.m_Distance = Vector3.Distance(bot.targetLook, this.m_NetworkPosition);
            }

            bodyState = (PlayerState)stream.ReceiveNext();
            FPState = (PlayerFPState)stream.ReceiveNext();
            velocity = (Vector3)stream.ReceiveNext();
            //weaponType = (WeaponType)stream.ReceiveNext();
            m_ReceivedNetworkUpdate = true;
        }
    }

    public override void OnLocalPlayer()
    {
        //send the state of player local for remote animation
        remotePlayer.playerAnimator.BodyState = botController.State;
        remotePlayer.playerAnimator.Velocity = botController.agent.velocity;
        remotePlayer.playerAnimator.FPState = botController.fpsState;
        remotePlayer.weaponType = botController.weaponType;
    }

    /// <summary>
    /// Function called each frame when the player is remote
    /// </summary>
     public override void OnRemotePlayer()
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

}
