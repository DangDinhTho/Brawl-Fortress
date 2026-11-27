using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Circus maps: a circus cannon that fires whoever steps into it in a high arc over the walls onto its landing target
/// (a red and white ring on the ground). A short fuse (sparks), a bang, the flight (about a second), a dusty landing.
/// Players and bots alike (a bot that walks over it flies too); both teams may use any cannon. The cannons are mirrored
/// for the two halves. The arc is drawn while the local player stands next to the cannon.
/// No network message: the character's owner (its player, the master client for bots) moves it along the arc, its
/// position is synced as usual. A player's CharacterController is switched off during the flight (state Teleport, as
/// for the Engineer's teleporter); a bot's NavMeshAgent stops driving its position and is warped onto the target.
/// Placed by the map builder (BrawlMapBuilder.PlaceCannons) with a CircusCannon model as its 'barrel' child.
/// </summary>
public class HumanCannon : MonoBehaviour
{
    public Vector3 landing;
    public Transform barrel;
    public float triggerRadius = 0.85f;
    public float fuse = 0.45f;
    public float speed = 13f;
    public float peak = 3.8f;

    static readonly HashSet<TF2Player> flying = new HashSet<TF2Player>();
    static readonly Dictionary<TF2Player, float> rest = new Dictionary<TF2Player, float>();
    readonly Dictionary<TF2Player, float> lit = new Dictionary<TF2Player, float>();
    readonly List<TF2Player> fire = new List<TF2Player>();
    LineRenderer arc;
    Vector3 barrelPos;
    float recoil, nextSpark, arcAlpha;

    public static bool IsFlying(TF2Player p) => p != null && flying.Contains(p);

    void Awake()
    {
        if (barrel) barrelPos = barrel.localPosition;
        // landing target: red / white rings with a gold bull's-eye
        var pad = MechMesh.Child(transform, "CannonTarget", MechMesh.Bands(new[] { 0.28f, 0.55f, 0.85f, 1.1f, 1.22f },
            new[] { new Color(1f, 0.82f, 0.25f, 0.95f), new Color(0.92f, 0.16f, 0.14f, 0.9f), new Color(1f, 0.97f, 0.9f, 0.85f),
                    new Color(0.92f, 0.16f, 0.14f, 0.9f), new Color(1f, 0.97f, 0.9f, 0.6f) }), MechMesh.Decal, 0f);
        pad.transform.position = landing + Vector3.up * 0.03f;
        // the arc, shown while the local player is close
        var go = new GameObject("CannonArc");
        go.transform.SetParent(transform, false);
        arc = go.AddComponent<LineRenderer>();
        arc.sharedMaterial = MechMesh.Decal;
        arc.useWorldSpace = true;
        arc.widthMultiplier = 0.12f;
        arc.numCapVertices = 2;
        arc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        arc.receiveShadows = false;
        int n = 28; arc.positionCount = n;
        Vector3 a = Muzzle; float h = Height;
        for (int i = 0; i < n; i++) { float u = i / (float)(n - 1); arc.SetPosition(i, Vector3.Lerp(a, landing, u) + Vector3.up * (4f * h * u * (1f - u))); }
        SetArcAlpha(0f);
    }

    Vector3 Muzzle => transform.position + Vector3.up * 0.5f;
    float Flat { get { Vector3 d = landing - transform.position; d.y = 0f; return d.magnitude; } }
    float Height => peak + Flat * 0.08f;

    void SetArcAlpha(float a)
    {
        arcAlpha = a;
        var c = new Color(1f, 0.95f, 0.8f, 0.55f * a);
        arc.startColor = c; arc.endColor = new Color(1f, 0.4f, 0.3f, 0.7f * a);
        arc.enabled = a > 0.01f;
    }

    void Update()
    {
        // arc preview for the local player
        var me = ArenaManager.Instance != null ? ArenaManager.Instance.mainPlayer : null;
        float want = 0f;
        if (me != null && MapMechanics.Alive(me))
        {
            Vector3 d = me.transform.position - transform.position; d.y = 0f;
            want = 1f - Mathf.InverseLerp(2.2f, 3.6f, d.magnitude);
        }
        if (!Mathf.Approximately(want, arcAlpha)) SetArcAlpha(Mathf.MoveTowards(arcAlpha, want, Time.deltaTime * 4f));

        if (barrel)
        {
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 2.5f);
            Vector3 back = landing - transform.position; back.y = 0f;
            barrel.localPosition = barrelPos - transform.InverseTransformDirection(back.normalized) * (0.35f * recoil);
        }

        // fuses burning
        if (lit.Count > 0 && Time.time >= nextSpark)
        {
            nextSpark = Time.time + 0.06f;
            BrawlFx.Burst(Muzzle + Vector3.up * 0.3f, new Color(1f, 0.9f, 0.4f, 1f), new Color(1f, 0.5f, 0.15f, 1f), 2, 2.5f);
        }
        fire.Clear();
        foreach (var kv in lit) if (Time.time >= kv.Value) fire.Add(kv.Key);
        foreach (var p in fire) { lit.Remove(p); if (MapMechanics.Alive(p) && !flying.Contains(p)) StartCoroutine(Fly(p)); }

        if (!MapMechanics.Running) return;
        Vector3 c = transform.position;
        foreach (var p in MapMechanics.Characters())
        {
            if (p.photonView == null || !p.photonView.IsMine || !MapMechanics.Alive(p)) continue;
            if (flying.Contains(p) || lit.ContainsKey(p) || (rest.TryGetValue(p, out var r) && Time.time < r)) continue;
            Vector3 d = p.transform.position - c; float up = d.y; d.y = 0f;
            if (up < -0.8f || up > 1.5f || d.sqrMagnitude > triggerRadius * triggerRadius) continue;
            lit[p] = Time.time + fuse;
        }
    }

    IEnumerator Fly(TF2Player p)
    {
        flying.Add(p);
        var fps = p.FPSPlayer;
        var cc = p.GetComponent<CharacterController>();
        var agent = p.GetComponent<NavMeshAgent>();
        bool ccOn = cc != null && cc.enabled, agentOn = agent != null && agent.enabled;
        if (fps != null && p.photonView.IsMine) fps.State = PlayerState.Teleport;
        if (ccOn) cc.enabled = false;
        if (agentOn) agent.updatePosition = false;

        Vector3 a = Muzzle, b = landing;
        float dur = Mathf.Max(0.8f, Flat / speed), h = Height;
        Vector3 dir = b - a; dir.y = 0f;
        if (dir.sqrMagnitude > 1e-4f) p.transform.rotation = Quaternion.LookRotation(dir.normalized);
        recoil = 1f;
        BrawlFx.Burst(a + dir.normalized * 0.6f + Vector3.up * 0.4f, new Color(1f, 0.92f, 0.5f, 1f), new Color(0.95f, 0.3f, 0.2f, 1f), 14, 6f);
        BrawlFx.DustRing(transform.position, 0.8f, new Color(1f, 0.97f, 0.9f, 0.8f), 10);

        float t = 0f;
        while (t < dur && MapMechanics.Alive(p))
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            p.transform.position = Vector3.Lerp(a, b, u) + Vector3.up * (4f * h * u * (1f - u));
            yield return null;
        }
        bool alive = MapMechanics.Alive(p);
        if (alive) p.transform.position = b;
        if (agentOn && agent != null)
        {
            if (alive && agent.isOnNavMesh) agent.Warp(b);
            agent.updatePosition = true;
        }
        if (ccOn && cc != null && alive) cc.enabled = true;
        if (fps != null && p.photonView.IsMine) fps.State = PlayerState.Idle;
        Physics.SyncTransforms();
        if (alive) BrawlFx.DustRing(b, 1f, new Color(1f, 0.97f, 0.9f, 0.85f), 12);
        flying.Remove(p);
        rest[p] = Time.time + 0.8f;
    }

    void OnDisable()
    {
        lit.Clear();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        Gizmos.DrawWireSphere(landing, 1.1f);
        Vector3 prev = Muzzle; float h = Height;
        for (int i = 1; i <= 20; i++) { float u = i / 20f; var q = Vector3.Lerp(Muzzle, landing, u) + Vector3.up * (4f * h * u * (1f - u)); Gizmos.DrawLine(prev, q); prev = q; }
    }
#endif
}
