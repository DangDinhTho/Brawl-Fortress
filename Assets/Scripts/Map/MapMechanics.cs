using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Shared pieces of the themed map mechanics (Nov 2026: Xmas gifts, Circus cannons, Lunar meteors, Ice slick patches,
/// Candy jelly walls, Egypt dart traps; the older ones: Volcano vents, Swamp bogs, Ocean currents, Savanna water holes,
/// Japan torii paths). The same clock on every client, the characters a mechanic may act on, and one Photon event
/// channel for the two mechanics that need the master client to decide (who took a gift, when a trap fires).
/// </summary>
public static class MapMechanics
{
    /// <summary>The shared match clock: PhotonNetwork.Time (also in Photon OfflineMode), else the level time.</summary>
    public static double Clock => PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode ? PhotonNetwork.Time : Time.timeSinceLevelLoad;

    /// <summary>True while the match runs (mechanics never hurt, heal or buff before or after it).</summary>
    public static bool Running
    {
        get { var a = ArenaManager.Instance; return a != null && a.gameState == GameState.Started; }
    }

    /// <summary>True on the client that decides for the room: the master client, or alone (offline / solo).</summary>
    public static bool Authority => !PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient;

    /// <summary>Players and bots of both teams (not MvM robots, not bosses; zombies are not TF2Players).</summary>
    public static IEnumerable<TF2Player> Characters()
    {
        var a = ArenaManager.Instance;
        if (a == null) yield break;
        foreach (var list in a.teamMembers.Values)
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p == null || p is TF2Robot || p.tF2Class >= TF2Class.END_CHARACTER) continue;
                yield return p;
            }
    }

    public static bool Alive(TF2Player p) => p != null && p.healthManager != null && !p.healthManager.death;

    /// <summary>Damage a share of a character's max health, no attacker (ownerId -1, like the eruption vents). Call it on
    /// the character's owner only.</summary>
    public static void Hurt(HealthBase hm, float fraction, Vector3 dir)
    {
        if (hm == null || hm.death) return;
        var dmg = new DamageObject
        {
            damage = Mathf.Max(1, Mathf.RoundToInt(hm.maxHealth * fraction)),
            ownerId = -1,
            isOwner = true,
            team = ConfigManager.GetEnemyTeam(hm.team),
        };
        hm.TakeDamage(dmg, dir);
    }

    static readonly Collider[] buf = new Collider[64];

    /// <summary>Damages, once per 'hit' set, every character this client owns whose body is within radius (flat) of c:
    /// players, bots, robots, zombies; not bosses. Like the eruption vents.</summary>
    public static void HurtArea(Vector3 c, float radius, float fraction, HashSet<int> hit)
    {
        if (!Running) return;
        int n = Physics.OverlapSphereNonAlloc(c + Vector3.up, radius + 0.6f, buf, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var hm = buf[i].GetComponentInParent<HealthManager>();
            if (hm == null || hm.death || hm.photonView == null || !hm.photonView.IsMine) continue;
            if (hit != null && hit.Contains(hm.photonView.ViewID)) continue;
            if (hm.GetComponentInParent<TF2Boss>() != null) continue;
            Vector3 p = hm.centerBody != null ? hm.centerBody.position : hm.transform.position;
            Vector3 d = p - c; float up = d.y; d.y = 0f;
            if (d.magnitude > radius || up < -1f || up > 3.5f) continue;
            if (hit != null) hit.Add(hm.photonView.ViewID);
            Hurt(hm, fraction, (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward) * 2f + Vector3.up);
        }
    }

    // ------------------------------------------------------------------ events
    public const byte GiftEvent = 42, TrapEvent = 43;
    static readonly Dictionary<byte, Action<object[]>> handlers = new Dictionary<byte, Action<object[]>>();

    public static void On(byte code, Action<object[]> handler) { handlers[code] = handler; Register(); }

    /// <summary>Runs the event here and sends it to the other clients (online only).</summary>
    public static void Send(byte code, object[] data)
    {
        if (handlers.TryGetValue(code, out var h)) h(data);
        if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode && PhotonNetwork.InRoom)
            PhotonNetwork.RaiseEvent(code, data, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
    }

    class Listener : IOnEventCallback
    {
        public void OnEvent(EventData e)
        {
            if (e.Code != GiftEvent && e.Code != TrapEvent) return;
            if (e.CustomData is object[] data && handlers.TryGetValue(e.Code, out var h)) h(data);
        }
    }

    static Listener listener;
    static void Register()
    {
        if (listener != null) return;
        listener = new Listener();
        PhotonNetwork.AddCallbackTarget(listener);
    }
}

/// <summary>
/// A short speed boost on a character (a Christmas gift). Every client keeps it (speed lines in CharacterFx); the
/// owner's movement uses it (PlayerFPSController, TerrainSpeedAgent).
/// </summary>
public static class SpeedBuff
{
    public const float Factor = 1.3f;
    static readonly Dictionary<TF2Player, float> until = new Dictionary<TF2Player, float>();

    public static void Give(TF2Player p, float seconds) { if (p != null) until[p] = Time.time + seconds; }

    public static bool Active(TF2Player p) => p != null && until.TryGetValue(p, out var t) && t > Time.time;

    public static float FactorOf(TF2Player p) => Active(p) ? Factor : 1f;
}
