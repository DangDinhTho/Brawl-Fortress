using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Map features that change how fast characters move: Swamp bog puddles (<see cref="BogPuddle"/>, slower) and Ocean
/// currents (<see cref="OceanCurrent"/>, faster downstream, slower upstream). One place for the player
/// (PlayerFPSController) and for NavMesh agents (<see cref="TerrainSpeedAgent"/>).
/// </summary>
public static class TerrainSpeed
{
    /// <summary>Speed multiplier for a character at p moving along moveDir (any length; zero = standing still).</summary>
    public static float FactorAt(Vector3 p, Vector3 moveDir) => BogPuddle.SpeedFactorAt(p) * OceanCurrent.FactorAt(p, moveDir);

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
    float baseSpeed = -1f, applied = -1f;

    void Awake() { agent = GetComponent<NavMeshAgent>(); }

    void LateUpdate()
    {
        if (agent == null || !agent.enabled) return;
        if (!Mathf.Approximately(agent.speed, applied)) baseSpeed = agent.speed;
        float want = baseSpeed * TerrainSpeed.FactorAt(transform.position, agent.desiredVelocity);
        if (!Mathf.Approximately(agent.speed, want)) agent.speed = want;
        applied = want;
    }
}
