using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntryFieldOfView : MonoBehaviour
{
    public float radius;

    public LayerMask targetMask;
    public LayerMask obstructionMask;

    public Dictionary<Team, List<TF2Player>> detectTeams = new Dictionary<Team, List<TF2Player>>();
    private bool runing;

    private void Start()
    {
        
    }

    private void OnEnable()
    {
        Init();
    }

    private void OnDisable()
    {
        runing = false;
    }

    public void Init()
    {
        if(runing) return;
        if (detectTeams.Count == 0)
        {
            detectTeams.Add(Team.red, new List<TF2Player>());
            detectTeams.Add(Team.blue, new List<TF2Player>());
        }

        StartCoroutine(FOVRoutine());
    }

    private IEnumerator FOVRoutine()
    {
        runing = true;
        WaitForSeconds wait = new WaitForSeconds(0.5f);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    private void FieldOfViewCheck()
    {
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);
        detectTeams[Team.red].Clear();
        detectTeams[Team.blue].Clear();
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
                        if (!detectTeams[player.team].Contains(player))
                        {
                            detectTeams[player.team].Add(player);
                        }
                    }

                }
            }

        }
    }
}
