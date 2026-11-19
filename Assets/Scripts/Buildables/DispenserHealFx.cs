using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dispenser heal effect: team mates being healed by the dispenser get the medigun heal effect (green plus signs rising
/// and a ground ring, MedigunHealFX). The dispenser only heals on the master (EntryFieldOfView runs there), so every
/// client finds the healed players itself: same team, alive, hurt, human (the dispenser does not buff bots), within the
/// heal radius, dispenser built and idle. Checked 4 times a second.
/// </summary>
[DisallowMultipleComponent]
public class DispenserHealFx : MonoBehaviour
{
    public GameObject healFxPrefab;
    public float checkInterval = 0.25f;

    DispenserController dispenser;
    float nextCheck;
    readonly Dictionary<TF2Player, GameObject> active = new Dictionary<TF2Player, GameObject>();
    readonly HashSet<TF2Player> found = new HashSet<TF2Player>();
    readonly List<TF2Player> drop = new List<TF2Player>();
    static readonly Collider[] hits = new Collider[24];

    void Awake() { dispenser = GetComponent<DispenserController>(); }

    void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + checkInterval;
        found.Clear();
        bool working = dispenser != null && dispenser.state == BuildableState.Idle && dispenser.health != null && !dispenser.health.death;
        if (working && dispenser.fov != null)
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, dispenser.fov.radius, hits, dispenser.fov.targetMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                var p = hits[i] != null ? hits[i].transform.root.GetComponent<TF2Player>() : null;
                if (p == null || p.isBot || p.team != dispenser.team || p.healthManager == null || p.healthManager.death) continue;
                if (p.healthManager.currentHealth >= p.healthManager.maxHealth) continue;
                found.Add(p);
            }
        }
        foreach (var p in found)
            if (!active.ContainsKey(p) || active[p] == null) active[p] = Spawn(p);
        drop.Clear();
        foreach (var kv in active) if (kv.Key == null || !found.Contains(kv.Key)) drop.Add(kv.Key);
        foreach (var p in drop) { Stop(active[p]); active.Remove(p); }
    }

    GameObject Spawn(TF2Player p)
    {
        if (healFxPrefab == null) return null;
        var fx = Instantiate(healFxPrefab, p.transform);
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localRotation = Quaternion.identity;
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>()) ps.Play();
        return fx;
    }

    static void Stop(GameObject fx)
    {
        if (fx == null) return;
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        fx.transform.SetParent(null, true);   // not tied to the player any more (death / respawn re-enables his model)
        Destroy(fx, 2f); // let the rising plus signs finish
    }

    void OnDisable()
    {
        foreach (var kv in active) Stop(kv.Value);
        active.Clear();
    }
}
