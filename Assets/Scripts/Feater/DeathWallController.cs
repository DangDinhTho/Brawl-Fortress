using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathWallController : MonoBehaviour
{
    private DamageObject damageObject;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HealthManager healthManager = other.GetComponent<HealthManager>();
            if (healthManager != null && !healthManager.death)
            {
                damageObject = new DamageObject()
                {
                    damage = 500,
                    ownerId = -1,
                    isOwner = true,
                    team = ConfigManager.GetEnemyTeam(healthManager.team)
                };
                Vector3 direction = other.transform.position - transform.position;
                direction *= (2f / direction.magnitude);
                healthManager.TakeDamage(damageObject, direction);
            }
        }
    }
}
