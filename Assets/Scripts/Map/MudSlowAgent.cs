using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Slows a NavMesh agent while it stands in a bog puddle. Other scripts set agent.speed when they change state; this
/// keeps their value as the base and applies the puddle factor on top (a change it did not make becomes the new base).
/// </summary>
public class MudSlowAgent : MonoBehaviour
{
    NavMeshAgent agent;
    float baseSpeed = -1f, applied = -1f;

    void Awake() { agent = GetComponent<NavMeshAgent>(); }

    void LateUpdate()
    {
        if (agent == null || !agent.enabled) return;
        if (!Mathf.Approximately(agent.speed, applied)) baseSpeed = agent.speed;
        float want = baseSpeed * BogPuddle.SpeedFactorAt(transform.position);
        if (!Mathf.Approximately(agent.speed, want)) agent.speed = want;
        applied = want;
    }
}
