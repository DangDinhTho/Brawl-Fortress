using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public float radius;
    [Range(0, 360)]
    public float angle;

    [HideInInspector]
    public GameObject playerRef;

    public LayerMask targetMask;
    public LayerMask obstructionMask;

    public bool canSeePlayer;
    public Transform targetInField;
    public Team team;
    

    private void Start()
    {
        playerRef = gameObject;
    }

    private void OnEnable()
    {
        StartCoroutine(FOVRoutine());
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    private void FieldOfViewCheck()
    {
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position + Vector3.up, radius, targetMask);

        if (rangeChecks.Length != 0)
        {
            BushArea myBush = BushArea.All.Count > 0 ? BushArea.Find(transform.position + Vector3.up * 0.3f) : null;
            for (int i = 0; i < rangeChecks.Length; i++)
            {
                Transform target = rangeChecks[i].transform;
                Vector3 directionToTarget = (target.position + Vector3.up - transform.position).normalized;

                if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, target.position);

                    if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                    {
                        HealthBase health = target.transform.root.GetComponent<HealthBase>();
                        if (health != null && !health .death && health.team != team)
                        {
                            // hidden in a bush (and we are not in the same bush) -> cannot be seen
                            if (BushStealth.IsHiddenFrom(health.GetComponentInParent<BushStealth>(), team, transform.position, myBush))
                                continue;
                            canSeePlayer = true;
                            targetInField = health.transform;
                            return;
                        }
                        
                    }
                    // else
                    //     canSeePlayer = false;
                }
                // else
                //     canSeePlayer = false;
            }

            canSeePlayer = false;
        }
        else if (canSeePlayer)
            canSeePlayer = false;
    }
}