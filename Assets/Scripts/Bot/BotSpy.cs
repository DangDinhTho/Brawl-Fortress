using UnityEngine;

/// <summary>
/// Spy bots cloak now and then (added by BotController.Init for spy bots, owner side): every few seconds, when not
/// cloaked, with no enemy close and enough charge on the watch, they cloak with some chance. Attacking uncloaks them
/// like a player (SpyCloakFx), and the watch ends the cloak when its charge runs out.
/// </summary>
[DisallowMultipleComponent]
public class BotSpy : MonoBehaviour
{
    public float checkInterval = 3f;
    [Range(0f, 1f)] public float cloakChance = 0.4f;
    [Tooltip("Do not cloak with an enemy this close (m)")] public float enemyTooClose = 6f;
    [Range(0f, 1f), Tooltip("Share of the watch charge needed")] public float minCharge = 0.6f;

    TF2Player bot;
    SpyController spy;
    FieldOfView fov;
    float nextCheck;

    public void Init(BotController owner)
    {
        bot = GetComponent<TF2Player>();
        spy = GetComponentInChildren<SpyController>(true);
        fov = GetComponent<FieldOfView>();
        nextCheck = Time.time + Random.Range(2f, 5f);
    }

    void Update()
    {
        if (bot == null || spy == null || spy.invisWatch == null || !bot.photonView.IsMine || Time.time < nextCheck) return;
        nextCheck = Time.time + checkInterval;
        if (bot.healthManager == null || bot.healthManager.death || bot.MeshInvisible) return;
        if (fov != null && fov.canSeePlayer && fov.targetInField != null)
        {
            Vector3 d = fov.targetInField.position - transform.position; d.y = 0f;
            if (d.magnitude < enemyTooClose) return;
        }
        if (spy.invisWatch.Charge < spy.invisWatch.maxEXP * minCharge) return;
        if (Random.value > cloakChance) return;
        spy.Invisibility();
    }
}
