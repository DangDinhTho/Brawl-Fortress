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
    [Tooltip("Enemies this close are noticed in every direction (behind too), still not through walls or into bushes")]
    public float closeSense = 4f;
    

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
        // promo recording: bots stay passive so a shot can be staged (PromoHooks, editor only)
        if (PromoHooks.BotsPassive) { canSeePlayer = false; return; }
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position + Vector3.up, radius, targetMask);
        // the NEAREST visible enemy (was: the first collider found, in random physics order -> bots, zombies above all,
        // walked past a player standing right in front of them to chase someone far away)
        HealthBase best = null; float bestDist = float.MaxValue;
        if (rangeChecks.Length != 0)
        {
            BushArea myBush = BushArea.All.Count > 0 ? BushArea.Find(transform.position + Vector3.up * 0.3f) : null;
            for (int i = 0; i < rangeChecks.Length; i++)
            {
                Transform target = rangeChecks[i].transform;
                HealthBase health = target.root.GetComponent<HealthBase>();
                if (health == null || health.death || health.team == team) continue;
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                if (distanceToTarget >= bestDist) continue;
                Vector3 directionToTarget = (target.position + Vector3.up - transform.position).normalized;
                if (distanceToTarget > closeSense && Vector3.Angle(transform.forward, directionToTarget) >= angle / 2) continue;
                if (Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask)) continue;
                // hidden in a bush (and we are not in the same bush) -> cannot be seen
                if (BushStealth.IsHiddenFrom(health.GetComponentInParent<BushStealth>(), team, transform.position, myBush)) continue;
                best = health; bestDist = distanceToTarget;
            }
        }
        canSeePlayer = best != null;
        if (best != null) targetInField = best.transform;
    }
}