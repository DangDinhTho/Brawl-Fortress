using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Map features that change how fast characters move: Swamp bog puddles (<see cref="BogPuddle"/>, slower), Ocean
/// currents (<see cref="OceanCurrent"/>, faster downstream, slower upstream) and Japan torii paths
/// (<see cref="ToriiPath"/>, faster both ways), Ice slick patches (<see cref="SlickIce"/>, a little faster, sliding). One place for the player
/// (PlayerFPSController) and for NavMesh agents (<see cref="TerrainSpeedAgent"/>).
/// </summary>
public static class TerrainSpeed
{
    /// <summary>Speed multiplier for a character at p moving along moveDir (any length; zero = standing still).</summary>
    public static float FactorAt(Vector3 p, Vector3 moveDir) => BogPuddle.SpeedFactorAt(p) * OceanCurrent.FactorAt(p, moveDir) * ToriiPath.FactorAt(p) * SlickIce.SpeedFactorAt(p);

    static float nextAgentScan;
    /// <summary>Called by the map features: gives every NavMesh agent (bots spawn and respawn) a TerrainSpeedAgent,
    /// at most once a second.</summary>
    public static void EnsureAgents()
    {
        if (Time.unscaledTime < nextAgentScan) return;
        nextAgentScan = Time.unscaledTime + 1f;
        foreach (var ag in Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None))
            if (ag.enabled && ag.GetComponent<TerrainSpeedAgent>() == null) ag.gameObject.AddComponent<TerrainSpeedAgent>();
    }
}

/// <summary>
/// Applies the terrain speed to a NavMesh agent (bots, robots, zombies, bosses; the owner client moves them). Other
/// scripts set agent.speed when they change state; this keeps their value as the base and applies the factor on top
/// (a change it did not make becomes the new base).
/// </summary>
public class TerrainSpeedAgent : MonoBehaviour
{
    NavMeshAgent agent;
    TF2Player player;
    float baseSpeed = -1f, applied = -1f, baseAccel = -1f, appliedAccel = -1f;

    void Awake() { agent = GetComponent<NavMeshAgent>(); player = GetComponentInParent<TF2Player>(); }

    void LateUpdate()
    {
        if (agent == null || !agent.enabled) return;
        if (!Mathf.Approximately(agent.speed, applied)) baseSpeed = agent.speed;
        float want = baseSpeed * TerrainSpeed.FactorAt(transform.position, agent.desiredVelocity) * SpeedBuff.FactorOf(player);
        if (!Mathf.Approximately(agent.speed, want)) agent.speed = want;
        applied = want;
        // Ice slick patches: a share of the acceleration (the agent slides into turns and stops)
        if (!Mathf.Approximately(agent.acceleration, appliedAccel)) baseAccel = agent.acceleration;
        float acc = baseAccel * SlickIce.TractionAt(transform.position);
        if (!Mathf.Approximately(agent.acceleration, acc)) agent.acceleration = acc;
        appliedAccel = acc;
    }
}
