using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Medicgun : Gun
{
    public Transform target;
    public LayerMask obstructionMask;
    public LayerMask checkMark;
    private bool lockTarget;
    public float maxDistance = 20;
    /// <summary>Beam length: weaponConfig.range when set (what the aim indicator shows), else maxDistance.</summary>
    float MaxDistance => weaponConfig != null && weaponConfig.range > 0f ? weaponConfig.range : maxDistance;
    public HealthManager healthTarget;
    private int targetViewId;
    public LineRenderer lineRenderer;
    public Gradient blueGradient;


    public override void InitData(TF2Player player, Transform bone, bool isRemote = false)
    {
        base.InitData(player, bone, isRemote);
        //if (!isRemote)
        //{
        //shotPoints[0].position = shotPoints[1].position;
        //}

        if(player.team == Team.blue)
        {
            lineRenderer.colorGradient = blueGradient;
        }
    }

    public override void BeginFireSuccess()
    {
        base.BeginFireSuccess();
        playerOwner.playerNetwork.SyncFireLoopStart();
    }

    public override void BeginFire()
    {
        if (blockFire) return;
        OnTouchFire?.Invoke();
        playerOwner.playerNetwork.SyncFireDeployedStart();
        if (!playerOwner.isBot)
            playerOwner.FPSPlayer.runSpeed = playerOwner.characterConfig.characterStats.speed / 2;

        if (bulletInClip <= 0) return;
        BeginFireSuccess();

    }

    public override void FinishFire()
    {
        base.FinishFire();
        playerOwner.playerNetwork.SyncFireDeployedFinish();
        if (!playerOwner.isBot)
            playerOwner.FPSPlayer.runSpeed = playerOwner.characterConfig.characterStats.speed;
        shotPoints[0].gameObject.SetActive(false);
        lockTarget = false;
    }

    public override void FireLoop()
    {
        if (blockFire || !fire) return;
        if (bulletInClip <= 0)
        {
            fire = false;
            playerOwner.playerNetwork.SyncFireLoopFinish();
            OnClipEmpty?.Invoke();
            return;
        }
        FireLoopSuccess();
    }

    public override void FireLoopSuccess()
    {
        shotPoints[0].gameObject.SetActive(true);
        Shoot();
    }

    public override void StartReload()
    {
        return;
    }

    public override void Shoot()
    {
        if (!lockTarget)
        {
            target.parent = playerOwner.transform;
            target.localPosition = Vector3.up;
            targetViewId = playerOwner.viewID;
            //target.position = playerOwner.transform.position + playerOwner.transform.forward * 3 + Vector3.up * 1.5f;
            //float distanceToTarget = Vector3.Distance(transform.position, target.position);
            //Vector3 directionToTarget = (target.position - transform.position).normalized;
            //Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //Just a ray through the middle of your current view

            RaycastHit hit;

            if (Physics.Raycast(playerOwner.transform.position + Vector3.up, playerOwner.transform.forward, out hit, MaxDistance, checkMark))
            {
                HealthManager health = hit.transform.root.GetComponent<HealthManager>();
                if (health != null && !health.death && health.team == playerOwner.team)
                {
                    target.parent = health.transform;
                    target.localPosition = Vector3.up;
                    healthTarget = health;
                    targetViewId = health.photonView.ViewID;
                    lockTarget = true;
                }
            }

        }
        else
        {
            float distanceToTarget = Vector3.Distance(playerOwner.transform.position, target.position);
            Vector3 directionToTarget = (target.position - fpsCam.transform.position).normalized;
            if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask) && distanceToTarget < MaxDistance)
            {
                if (healthTarget != null)
                    healthTarget.Buff(0.01f);
                print("Buff");
            }
            else
            {
                lockTarget = false;
                target.parent = playerOwner.transform;
                target.localPosition = Vector3.up;
                targetViewId = playerOwner.viewID;
                healthTarget = null;
            }
        }
        playerOwner.playerNetwork.SyncMedicGunTarget(targetViewId);
    }


    public override void RemoteFire()
    {
         //ObjectPooling.Instance.Instantiate(weaponConfig.bulletId.ToString(), shotPoints[currShotPoint].position, shotPoints[currShotPoint].rotation, directionWithoutSpread.normalized, damageObject);
    }

    public override void RemoteFireDeployedStart()
    {
    }

    public override void RemoteFireDeployedFinish()
    {
    }


    public override void RemoteFireLoopStart()
    {
        //base.RemoteFireLoopStart();
        shotPoints[0].gameObject.SetActive(true);

    }

    public override void RemoteFireLoopFinish()
    {
        shotPoints[0].gameObject.SetActive(false);
        //base.RemoteFireLoopFinish();
    }

    public void RemoteTargetViewId(int viewID)
    {
        if(viewID != targetViewId)
        {
            PhotonView targetPhotonView = PhotonNetwork.GetPhotonView(viewID);
            if(targetPhotonView != null)
            {
                target.parent = targetPhotonView.transform;
                
            }
            else
            {
                target.parent = playerOwner.transform;
            }
            target.localPosition = Vector3.up;
            targetViewId = viewID;
        }
        
    }
}
