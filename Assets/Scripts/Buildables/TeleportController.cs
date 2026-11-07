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
            otherTeleport = PhotonNetwork.GetPhotonView(otherViewId)?.transform.GetComponent<TeleportController>();
            otherTeleport.SetOtherTeleport(this);
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
        OnStartBuild?.Invoke(upgradeTime);
    }

    IEnumerator WaitUpgrade(int _level)
    {
        yield return new WaitForSeconds(upgradeTime);
        level++;
        animator.speed = speedUpgrades[_level - 1];
        state = BuildableState.Idle;
        
    }

    private void Update()
    {
        //check if ray hits something
        if (buildableType != BuildableType.TeleporterEnter || state != BuildableState.Idle || (otherTeleport != null && otherTeleport.state != BuildableState.Idle))
        {
            if (arrowActive)
            {
                for (int i = 0; i < arrowObjs.Length; i++)
                {
                    arrowObjs[i].SetActive(false);
                }
                effect.SetActive(false);
                targetMove = null;
                timeDelay = 0;
                arrowActive = false;
            }
            return;
        }

        if (otherTeleport != null)
        {
            if(!arrowActive)
            {
                for (int i = 0; i < arrowObjs.Length; i++)
                {
                    arrowObjs[i].SetActive(true);
                }

                arrowActive = true;
            }
            Vector3 otherPos = otherTeleport.transform.position;
            otherPos.y = arrow.position.y;
            arrow.LookAt(otherPos);
            
            if (Physics.Raycast(transform.position, Vector3.up, out hit, 1f, checkMark))
            {
                if (targetMove == null || targetMove != hit.transform.root)
                {
                    
                    TF2Player tf2Player = hit.transform.root.GetComponent<TF2Player>();
                    if (tf2Player != null && tf2Player.team == team)
                    {
                        targetMove = hit.transform.root;
                        timeDelay = 0;
                        effect.SetActive(true);
                    }
                }


                if (targetMove != null && timeDelay >= timeDelayTeleports[level - 1])
                {
                    TF2Player tf2Player = targetMove.GetComponent<TF2Player>();
                    if (tf2Player != null && tf2Player.photonView.IsMine && tf2Player.team == team)
                    {
                        tf2Player.FPSPlayer.State = PlayerState.Teleport;
                        tf2Player.transform.position = otherTeleport.transform.position + Vector3.up * 0.2f;
                        otherTeleport.OnPlayerExit(tf2Player);
                    }

                    timeDelay = 0;
                    targetMove = null;
                    effect.SetActive(false);
                }
               

                timeDelay += Time.deltaTime;

            }
            else if(timeDelay > 0)
            {
                targetMove = null;
                timeDelay = 0;
                effect.SetActive(false);
            }
        }
        else if(arrowActive)
        {
            for (int i = 0; i < arrowObjs.Length; i++)
            {
                arrowObjs[i].SetActive(false);
            }
            effect.SetActive(false);
            targetMove = null;
            timeDelay = 0;
            arrowActive = false;
        }
        
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
        if (tF2Player != null && tF2Player.photonView.IsMine)
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
