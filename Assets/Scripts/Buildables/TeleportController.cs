using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class TeleportController : BuildableEntry
{
    private Animator animator;
    public float upgradeTime = 1.5f;
    public float[] speedUpgrades;
    public TeleportController otherTeleport;
    public LayerMask checkMark;
    public GameObject effect;
    public float[] timeDelayTeleports;
    public Transform arrow;
    public GameObject[] arrowObjs;
    
    
    private Transform targetMove;
    private RaycastHit hit;
    private float timeDelay;
    private bool arrowActive;
    
    public override void Start()
    {
        base.Start();
        state = BuildableState.Building;
        for(int i = 0; i < arrowObjs.Length; i++)
        {
            arrowObjs[i].SetActive(false);
        }
        if (photonView.InstantiationData != null && photonView.InstantiationData.Length > 1)
        {
            int otherViewId = (int)photonView.InstantiationData[1];
            var pv = PhotonNetwork.GetPhotonView(otherViewId);
            otherTeleport = pv != null ? pv.GetComponent<TeleportController>() : null;
            if (otherTeleport != null) otherTeleport.SetOtherTeleport(this); // the pair may already be destroyed
        }
    }

    public void SetOtherTeleport(TeleportController teleportController)
    {
        this.otherTeleport = teleportController;
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
        mainLevel = ReferenceLevels[0];
        animator = ReferenceLevels[0].animator;
        OnBuildComplete?.Invoke(_level);
    }

    public override void Upgrade(int _level)
    {
        if (state != BuildableState.Idle || level >= 3) return;
        base.Upgrade(_level);
        state = BuildableState.Building;
        for (int i = 0; i < arrowObjs.Length; i++)
        {
            arrowObjs[i].SetActive(false);
        }
        StartCoroutine(WaitUpgrade(_level));
        OnStartBuild?.Invoke(upgradeTime / BuildableLevel.BuildSpeed);
    }

    IEnumerator WaitUpgrade(int _level)
    {
        yield return new WaitForSeconds(upgradeTime / BuildableLevel.BuildSpeed);
        level++;
        animator.speed = speedUpgrades[_level - 1];
        state = BuildableState.Idle;
        
    }

    // ---- teleport (only the entrance moves people; the exit only plays its effect)
    static readonly Collider[] padHits = new Collider[12];
    float nextScan;

    void HideArrows()
    {
        if (!arrowActive) return;
        for (int i = 0; i < arrowObjs.Length; i++) arrowObjs[i].SetActive(false);
        effect.SetActive(false);
        targetMove = null;
        timeDelay = 0;
        arrowActive = false;
    }

    /// <summary>A living team mate standing on the pad (box over the whole pad, not one ray at its centre).</summary>
    TF2Player FindRider()
    {
        Vector3 c = transform.position + Vector3.up * 0.7f;
        // the pad (1 m) plus ~25 cm all round: touching its edge is enough
        int n = Physics.OverlapBoxNonAlloc(c, new Vector3(0.75f, 0.7f, 0.75f), padHits, transform.rotation, checkMark, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var p = padHits[i] != null ? padHits[i].transform.root.GetComponent<TF2Player>() : null;
            if (p != null && p.team == team && p.healthManager != null && !p.healthManager.death) return p;
        }
        return null;
    }

    private void Update()
    {
        if (buildableType != BuildableType.TeleporterEnter || state != BuildableState.Idle || otherTeleport == null || otherTeleport.state != BuildableState.Idle)
        {
            HideArrows();
            return;
        }

        if (!arrowActive)
        {
            for (int i = 0; i < arrowObjs.Length; i++) arrowObjs[i].SetActive(true);
            arrowActive = true;
        }
        Vector3 otherPos = otherTeleport.transform.position;
        otherPos.y = arrow.position.y;
        arrow.LookAt(otherPos);

        // who stands on the pad: checked 10 times a second
        if (Time.time >= nextScan)
        {
            nextScan = Time.time + 0.1f;
            var rider = FindRider();
            Transform riderT = rider != null ? rider.transform : null;
            if (riderT != targetMove)
            {
                targetMove = riderT;
                timeDelay = 0;
                effect.SetActive(riderT != null);
            }
        }
        if (targetMove == null) return;

        timeDelay += Time.deltaTime;
        if (timeDelay < timeDelayTeleports[Mathf.Clamp(level - 1, 0, timeDelayTeleports.Length - 1)]) return;

        var player = targetMove.GetComponent<TF2Player>();
        if (player != null && player.photonView.IsMine) TeleportPlayer(player);
        timeDelay = 0;
        targetMove = null;
        effect.SetActive(false);
    }

    /// <summary>Moves the rider onto the exit. Bots walk with a NavMeshAgent (Warp, or it pulls them back); players move
    /// with a CharacterController (disabled while the position is set, or it can overwrite it).</summary>
    void TeleportPlayer(TF2Player player)
    {
        Vector3 dest = otherTeleport.transform.position + Vector3.up * 0.05f;
        if (player.FPSPlayer != null) player.FPSPlayer.State = PlayerState.Teleport;
        var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        var cc = player.GetComponent<CharacterController>();
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Warp(dest);
        else
        {
            bool ccOn = cc != null && cc.enabled;
            if (ccOn) cc.enabled = false;
            player.transform.position = dest;
            if (ccOn) cc.enabled = true;
        }
        Physics.SyncTransforms();
        otherTeleport.OnPlayerExit(player);
    }

    public void OnPlayerExit(TF2Player player)
    {
        StartCoroutine(PlayEffectOnPlayerExit(player));
        photonView.RPC(nameof(RPC_OnPlayerExit), RpcTarget.Others);
    }

    IEnumerator PlayEffectOnPlayerExit(TF2Player tF2Player = null)
    {
        effect.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        if (tF2Player != null && tF2Player.photonView.IsMine && tF2Player.FPSPlayer != null)
        {
            tF2Player.FPSPlayer.State = PlayerState.Idle;
        }
        yield return new WaitForSeconds(2.5f);
        effect.SetActive(false);
    }


    [PunRPC]
    public void RPC_OnPlayerExit()
    {
        StartCoroutine(PlayEffectOnPlayerExit());
    }

    
    // IEnumerator Teleport()
    // {
    //     while (true)
    //     {
    //         if (state != BuildableState.Died) yield return null;
    //         if (Physics.Raycast(transform.position, Vector3.up, out hit, 1f, checkMark))
    //         {
    //             if (targetMove == null || targetMove != hit.transform.root)
    //             {
    //                 targetMove = hit.transform.root;
    //                 timeDelay = 0;
    //             }
    //
    //             if (timeDelay >= timeDelayTeleports[level - 1])
    //             {
    //                 TF2Player tf2Player = targetMove.GetComponent<TF2Player>();
    //                 if (tf2Player != null && tf2Player.photonView.IsMine)
    //                 {
    //                     tf2Player.transform.position = otherTeleport.transform.position + Vector3.up * 0.2f;
    //                 }
    //
    //                 timeDelay = 0;
    //                 targetMove = null;
    //             }
    //
    //             timeDelay += Time.deltaTime;
    //
    //         }
    //         else if(timeDelay > 0)
    //         {
    //             targetMove = null;
    //             timeDelay = 0;
    //         }
    //
    //         yield return null;
    //     }
    // }
}
