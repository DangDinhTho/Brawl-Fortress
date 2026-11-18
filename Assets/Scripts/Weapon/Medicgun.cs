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

    /// <summary>Half-angle of the cone around the aim in which a teammate is picked up (mobile aiming is rough).</summary>
    public float lockAngle = 25f;
    static readonly Collider[] overlap = new Collider[64];

    Vector3 BeamOrigin => playerOwner.transform.position + Vector3.up * 1.4f;

    /// <summary>Players aim with the TPS camera; bots aim with their body.</summary>
    Vector3 AimDirection
    {
        get
        {
            if (!playerOwner.isBot && Camera.main != null) return Camera.main.transform.forward;
            return playerOwner.transform.forward;
        }
    }

    void ReleaseTarget()
    {
        lockTarget = false;
        target.parent = playerOwner.transform;
        target.localPosition = Vector3.up;
        targetViewId = playerOwner.viewID;
        healthTarget = null;
    }

    bool Blocked(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        return Physics.Raycast(from, d.normalized, d.magnitude, obstructionMask);
    }

    /// <summary>The living teammate (never the medic himself) closest to the aim inside the lock cone, in range and in sight.</summary>
    float LockRange => MaxDistance; // = the aim indicator's range: what the player sees is what he gets
    /// <summary>Half-angle of the lock cone = half the aim indicator's fan (weaponConfig.aimAngle), else lockAngle.</summary>
    float LockHalfAngle => weaponConfig != null && weaponConfig.aimAngle > 0f ? weaponConfig.aimAngle * 0.5f : lockAngle;
    /// <summary>True while the beam is locked on a teammate (the aim indicator hides then).</summary>
    public bool HasTarget => lockTarget && healthTarget != null;

    /// <summary>The living teammate (never the medic himself) closest to the aim inside the lock cone, in range and in
    /// sight. The aim is the camera for players (the body for bots); the body direction is accepted too, because on mobile
    /// the character turns to where the player shoots while the camera may lag.</summary>
    HealthManager FindTeammate()
    {
        Vector3 origin = BeamOrigin;
        Vector3 aim = AimDirection.normalized, body = playerOwner.transform.forward;
        int count = Physics.OverlapSphereNonAlloc(origin, LockRange, overlap, checkMark, QueryTriggerInteraction.Collide);
        HealthManager best = null; float bestAngle = LockHalfAngle;
        for (int i = 0; i < count; i++)
        {
            var health = overlap[i].transform.root.GetComponent<HealthManager>();
            if (health == null || health == playerOwner.healthManager || health.death || health.team != playerOwner.team) continue;
            if (health.transform == playerOwner.transform) continue;
            Vector3 center = health.transform.position + Vector3.up;
            Vector3 to = center - origin;
            if (to.magnitude > LockRange) continue;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            float angle = Mathf.Min(Vector3.Angle(aim, to), Vector3.Angle(new Vector3(aim.x, 0f, aim.z), flat), Vector3.Angle(body, flat));
            if (angle > bestAngle || Blocked(origin, center)) continue;
            bestAngle = angle; best = health;
        }
        return best;
    }

    public override void Shoot()
    {
        if (lockTarget)
        {
            // keep healing while the teammate is alive, on our team, in range and in sight
            bool keep = healthTarget != null && !healthTarget.death && healthTarget.team == playerOwner.team && healthTarget != playerOwner.healthManager;
            if (keep)
            {
                Vector3 center = healthTarget.transform.position + Vector3.up;
                keep = Vector3.Distance(BeamOrigin, center) <= LockRange + 0.5f && !Blocked(BeamOrigin, center);
            }
            if (keep) healthTarget.Buff(0.01f);
            else ReleaseTarget();
        }
        if (!lockTarget)
        {
            if (target.parent != playerOwner.transform) { target.parent = playerOwner.transform; target.localPosition = Vector3.up; }
            targetViewId = playerOwner.viewID;
            var health = FindTeammate();
            if (health != null)
            {
                target.parent = health.transform;
                target.localPosition = Vector3.up;
                healthTarget = health;
                targetViewId = health.photonView.ViewID;
                lockTarget = true;
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
