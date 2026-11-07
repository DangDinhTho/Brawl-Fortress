using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class KothPoint : MonoBehaviour
{
    public float radius;

    public LayerMask targetMask;
    public LayerMask obstructionMask;

    public KingOfTheHillManager kingOfTheHillManager;
    //public Dictionary<Team, List<TF2Player>> detectTeams = new Dictionary<Team, List<TF2Player>>();
    //private bool runing;
    [HideInInspector]
    public Team teamOwner;
    public float deltaTimeCheck = 0.5f;
    
    
    private void Start()
    {
        RunCheck();
        ArenaNetworkManager.instan.OnSwitchMasterPlayer += RunCheck;
    }

    public void RunCheck()
    {
        StopAllCoroutines();
        if (PhotonNetwork.IsMasterClient && !ArenaManager.Instance.endGameSign)
        {
            StartCoroutine(FOVRoutine());
        }
            
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(deltaTimeCheck);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    private void FieldOfViewCheck()
    {
        //teamOwner = Team.None;
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);
        if (rangeChecks.Length != 0)
        {
            for (int i = 0; i < rangeChecks.Length; i++)
            {
                Transform target = rangeChecks[i].transform;
                Vector3 directionToTarget = (target.position - transform.position).normalized;

                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position + Vector3.up, directionToTarget, distanceToTarget, obstructionMask))
                {
                    TF2Player player = target.transform.root.GetComponent<TF2Player>();
                    if (player != null && !player.healthManager.death)
                    {
                        if (teamOwner == Team.None)
                        {
                            teamOwner = player.team;
                        }
                        else if(teamOwner != player.team)
                        {
                            teamOwner = Team.None;
                            kingOfTheHillManager.SetNoneOwnerOnMaster();
                            return;
                        }
                    }

                }
            }
            

            if (teamOwner != Team.None)
            {
                kingOfTheHillManager.SetTeamOwnerOnMaster(teamOwner, deltaTimeCheck);
            }
            else
            {
                //beforeTime = Time.time;
                kingOfTheHillManager.SetNoneOwnerOnMaster();
            }

        }
        else
        {
            teamOwner = Team.None;
            kingOfTheHillManager.SetNoneOwnerOnMaster();
        }
    }
}
