using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lets engineer bots build (added by BotController.Init for engineer bots, zombies excluded). Runs on the bot's owner.
/// Every 1.5 s, when the bot is alive, has no enemy within 6 m, is 8 m from where it spawned (5 m after 25 s) and has the metal, it builds what it is missing, Sentry first,
/// then Dispenser (a destroyed one is rebuilt). It uses the same pieces as the player's build popup: the blueprint in
/// front of it (EngineerBlueprint.SelectBuild), the blueprint's own placement check (Blueprint.reject), the network
/// build (EngineerBlueprint.Build) and the metal cost (Wrench.UseExp). A blocked spot is retried facing 90 / 180 / 270
/// degrees, then given up until the next try. Teleporters are not built by bots (they need a sensible pair of spots).
/// </summary>
[DisallowMultipleComponent]
public class BotEngineer : MonoBehaviour
{
    public float checkInterval = 1.5f;
    [Tooltip("Wait this long after spawning / respawning before building")] public float startDelay = 2f;
    [Tooltip("Build only this far from where the bot (re)spawned: out at the front, not in the spawn room (m)")] public float minDistanceFromSpawn = 8f;
    [Tooltip("Building is put off only while an enemy is this close (m); farther enemies do not stop it (sentries belong where the fighting is)")] public float enemyTooClose = 6f;
    [Tooltip("Still near spawn after this long (holding a position): a shorter distance is enough")] public float fallbackAfter = 25f;
    public float fallbackDistance = 5f;
    static readonly BuildableType[] Priority = { BuildableType.Sentry, BuildableType.Dispenser };

    BotController controller;
    TF2Player bot;
    EngineerBlueprint blueprint;
    FieldOfView fov;
    readonly Dictionary<BuildableType, BuildableEntry> built = new Dictionary<BuildableType, BuildableEntry>();
    float nextCheck;
    Vector3 spawnPos;
    float spawnTime, captureAt = -1f;
    Vector3 lastPos;
    bool hasLast;
    bool busy;

    public bool Building => busy;

    public void Init(BotController owner)
    {
        controller = owner;
        bot = GetComponent<TF2Player>();
        fov = GetComponent<FieldOfView>();
        blueprint = bot != null && bot.remotePlayer != null ? bot.remotePlayer.GetComponentInChildren<EngineerBlueprint>(true) : null;
        OnSpawned();
        if (bot != null && bot.healthManager != null) bot.healthManager.OnRespawn += OnSpawned;
    }

    void OnSpawned()
    {
        nextCheck = Time.time + startDelay;
        spawnTime = Time.time;
        captureAt = Time.time + 0.2f; // the respawn move lands a moment later
    }

    Wrench GetWrench()
    {
        if (bot == null || bot.remotePlayer == null) return null;
        return bot.remotePlayer.weaponSlots.TryGetValue(WeaponType.Mele, out var w) ? w as Wrench : null;
    }

    bool EnemyClose()
    {
        if (fov == null || !fov.canSeePlayer || fov.targetInField == null) return false;
        Vector3 d = fov.targetInField.position - transform.position; d.y = 0f;
        return d.magnitude < enemyTooClose;
    }

    bool Has(BuildableType type) => built.TryGetValue(type, out var e) && e != null && e.health != null && !e.health.death;

    void Update()
    {
        // the spawn point is where the bot lands after a jump (placed at its spawn point / respawned): it is created
        // somewhere else first, so the position right after Init is not the spawn point
        Vector3 pos = transform.position;
        if (hasLast && (pos - lastPos).sqrMagnitude > 3f * 3f) { spawnPos = pos; spawnTime = Time.time; captureAt = -1f; nextCheck = Mathf.Max(nextCheck, Time.time + startDelay); }
        lastPos = pos; hasLast = true;
        if (captureAt >= 0f && Time.time >= captureAt) { spawnPos = pos; captureAt = -1f; }
        if (busy || blueprint == null || bot == null || !bot.photonView.IsMine || Time.time < nextCheck || captureAt >= 0f) return;
        nextCheck = Time.time + checkInterval;
        if (bot.healthManager == null || bot.healthManager.death) return;
        if (EnemyClose()) return;                                  // fight first when someone is close
        // out at the front first, not right at the spawn point
        Vector3 away = transform.position - spawnPos; away.y = 0f;
        float need = Time.time - spawnTime >= fallbackAfter ? fallbackDistance : minDistanceFromSpawn;
        if (away.magnitude < need) return;
        var wrench = GetWrench();
        if (wrench == null) return;
        foreach (var type in Priority)
        {
            if (Has(type) || !blueprint.blueprints.ContainsKey(type)) continue;
            if (wrench.exp < blueprint.blueprints[type].expRequired) return;   // save metal for the higher priority one
            StartCoroutine(TryBuild(type, wrench));
            return;
        }
    }

    IEnumerator TryBuild(BuildableType type, Wrench wrench)
    {
        busy = true;
        var agent = controller != null ? controller.agent : null;
        bool stopped = agent != null && agent.enabled && agent.isOnNavMesh;
        if (stopped) agent.isStopped = true;
        Quaternion start = transform.rotation;
        BuildableEntry entry = null;
        blueprint.SelectBuild(type);
        for (int turn = 0; turn < 4 && entry == null; turn++)
        {
            transform.rotation = start * Quaternion.Euler(0f, 90f * turn, 0f);
            // the blueprint checks its spot in its own Update: give it two frames at this heading
            yield return null;
            yield return null;
            if (bot.healthManager.death || EnemyClose()) break;
            if (!blueprint.blueprints[type].reject) entry = blueprint.Build();
        }
        if (entry != null)
        {
            wrench.UseExp(blueprint.blueprints[type].expRequired);
            built[type] = entry;
        }
        else blueprint.Cancel();
        if (stopped && agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
        busy = false;
    }

    void OnDisable()
    {
        if (busy && blueprint != null) blueprint.Cancel();
        busy = false;
    }
}
