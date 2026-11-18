using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Zombie movement, melee and animation sync (added by TF2Zombie.CreateCharacter).
///  - slow steady shamble: normal zombies 1.4-1.8 m/s, scouts 2.5 m/s (owner only, NavMeshAgent);
///  - chase: once a target is seen the zombie walks right up to it (stop 1.2 m). The behaviour tree it shares with the
///    gun bots keeps an 8 m stand-off, which left zombies hovering out of reach without ever attacking;
///  - melee: target within 2 m -> stop, face it and hold the trigger of the melee weapon (the Mele weapon swings on its
///    own rate, plays the attack animation and deals damage); out of reach -> release and chase again;
///  - while walking the body faces the walk direction (no strafe animation for zombies);
///  - the walk / run clip plays at a rate that matches the real ground speed (every client), attacks keep their timing.
/// </summary>
[DisallowMultipleComponent]
public class ZombieMotion : MonoBehaviour
{
    [Header("Speed")]
    public Vector2 walkSpeedRange = new Vector2(1.4f, 1.8f);
    public float scoutSpeed = 2.5f;
    public float turnSpeed = 10f;

    [Header("Melee")]
    [Tooltip("The zombie stops this close to its target (m)")] public float meleeStop = 1.2f;
    [Tooltip("Swings when the target is this close (m, centre to centre)")] public float meleeReach = 2.0f;

    [Header("Animation")]
    [Tooltip("Ground speed of the walk clip at animator speed 1 (m/s)")] public float walkClipSpeed = 1.5f;
    [Tooltip("Ground speed of the run clip (scouts) at animator speed 1 (m/s)")] public float runClipSpeed = 2.6f;

    TF2Zombie zombie;
    NavMeshAgent agent;
    Animator anim;
    FieldOfView fov;
    BotController bot;
    Vector3 lastPos;
    float groundSpeed, nextChase;
    bool attacking;
    static readonly int AttackState = Animator.StringToHash("Zombie Attack");

    public void Init(TF2Zombie owner)
    {
        zombie = owner;
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        fov = GetComponent<FieldOfView>();
        bot = GetComponent<BotController>();
        lastPos = transform.position;
        ApplySpeed();
    }

    public void ApplySpeed()
    {
        if (zombie == null || agent == null || !zombie.photonView.IsMine) return;
        agent.speed = zombie.tF2Class == TF2Class.scout ? scoutSpeed : Random.Range(walkSpeedRange.x, walkSpeedRange.y);
        agent.acceleration = 8f;
        agent.angularSpeed = 540f;
    }

    void StopAttack()
    {
        if (!attacking) return;
        attacking = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        if (bot != null && bot.currWeapon != null && bot.currWeapon.fire) bot.FinishFire();
    }

    void LateUpdate()
    {
        if (zombie == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (zombie.photonView.IsMine && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (zombie.healthManager.death) StopAttack();
            else OwnerMove(dt);
        }

        // every client: walk / run clip rate from the real ground speed
        Vector3 d = transform.position - lastPos; d.y = 0f;
        lastPos = transform.position;
        groundSpeed = Mathf.Lerp(groundSpeed, d.magnitude / dt, 8f * dt);
        if (anim == null || !anim.isActiveAndEnabled) return;
        if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash == AttackState || anim.IsInTransition(0)) { anim.speed = 1f; return; }
        float clipSpeed = zombie.tF2Class == TF2Class.scout ? runClipSpeed : walkClipSpeed;
        anim.speed = groundSpeed > 0.3f ? Mathf.Clamp(groundSpeed / clipSpeed, 0.8f, 1.5f) : 1f;
    }

    void OwnerMove(float dt)
    {
        agent.stoppingDistance = meleeStop;
        Transform target = fov != null && fov.canSeePlayer ? fov.targetInField : null;
        if (target != null)
        {
            var hb = target.GetComponentInParent<HealthBase>();
            if (hb != null && hb.death) target = null;
        }
        float dist = float.MaxValue;
        if (target != null) { Vector3 to = target.position - transform.position; to.y = 0f; dist = to.magnitude; }

        if (target != null && dist <= meleeReach)
        {
            // melee: stand, face, swing
            attacking = true;
            agent.isStopped = true;
            Vector3 to = target.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 12f * dt);
            if (bot != null && bot.currWeapon != null && !bot.currWeapon.fire && !bot.blockAttack) bot.Fire();
            return;
        }

        StopAttack();
        if (target != null && Time.time >= nextChase)
        {
            // walk right up to it (the shared behaviour tree would keep an 8 m distance)
            nextChase = Time.time + 0.25f;
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }

        // face the walking direction
        Vector3 v = agent.velocity; v.y = 0f;
        if (v.sqrMagnitude > 0.3f * 0.3f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(v), turnSpeed * dt);
    }

    void OnDisable() { StopAttack(); }
}
