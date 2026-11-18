using UnityEngine;

/// <summary>
/// Medic bots heal teammates with the medigun instead of only attacking.
/// Added by BotController.Init on medic bots (runs on the owner of the bot only).
///  - looks every 0.3 s for the teammate who needs healing most (hurt, alive, in range and in sight; never itself);
///  - heal mode: holds the medigun (Secondary), pauses the attack behaviour tree, follows the teammate at 4-6 m, faces
///    him and keeps the trigger down (Medicgun locks on the teammate in front);
///  - goes back to fighting (Primary, behaviour tree resumed) when nobody needs healing, or when an enemy is in sight
///    and the teammate is not in danger.
/// </summary>
[DisallowMultipleComponent]
public class BotMedic : MonoBehaviour
{
    public float searchRadius = 15f;
    public float followMin = 4f, followMax = 6f;
    [Tooltip("Heal teammates below this health ratio")]
    [Range(0f, 1f)] public float hurtRatio = 0.97f;
    [Tooltip("Keep healing even with an enemy in sight while the teammate is below this ratio")]
    [Range(0f, 1f)] public float dangerRatio = 0.6f;
    public LayerMask obstructionMask;
    [Tooltip("Seconds a state (healing / fighting) is kept at least")] public float minModeTime = 2.5f;
    [Tooltip("Seconds an enemy must stay in sight before the medic stops healing to fight")] public float enemyConfirmTime = 0.8f;
    float enemySeenFor, nextModeChange;

    public bool Healing { get; private set; }

    TF2Bot bot;
    BotController controller;
    FieldOfView fov;
    HealthManager patient;
    float nextSearch, nextMove;
    static readonly Collider[] overlap = new Collider[64];
    LayerMask bodies;

    public void Init(TF2Bot owner, BotController ctrl)
    {
        bot = owner;
        controller = ctrl;
        fov = GetComponent<FieldOfView>();
        bodies = LayerMask.GetMask("HitBox", "Player", "Bot");
        if (obstructionMask == 0) obstructionMask = LayerMask.GetMask("Wall", "Ground");
    }

    bool Mine => bot != null && bot.photonView.IsMine && !bot.healthManager.death;

    static float Ratio(HealthManager h) => h.maxHealth > 0 ? h.currentHealth / (float)h.maxHealth : 1f;

    bool InSight(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        return !Physics.Raycast(a, d.normalized, d.magnitude, obstructionMask);
    }

    HealthManager FindPatient()
    {
        Vector3 eye = transform.position + Vector3.up * 1.4f;
        int count = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, overlap, bodies, QueryTriggerInteraction.Collide);
        HealthManager best = null; float bestRatio = hurtRatio;
        for (int i = 0; i < count; i++)
        {
            var h = overlap[i].transform.root.GetComponent<HealthManager>();
            if (h == null || h == bot.healthManager || h.death || h.team != bot.team) continue;
            float r = Ratio(h);
            if (r >= bestRatio) continue;
            if (!InSight(eye, h.transform.position + Vector3.up)) continue;
            bestRatio = r; best = h;
        }
        return best;
    }

    void Update()
    {
        if (!Mine) { if (Healing) StopHealing(); return; }

        if (Time.time >= nextSearch)
        {
            nextSearch = Time.time + 0.3f;
            // keep the current patient while he still needs it, else look for another one
            if (patient == null || patient.death || Ratio(patient) >= 0.999f || Vector3.Distance(transform.position, patient.transform.position) > searchRadius + 3f)
                patient = FindPatient();

            // enemy must stay in sight for a while (FieldOfView flickers frame to frame)
            bool seen = fov != null && fov.canSeePlayer;
            enemySeenFor = seen ? enemySeenFor + 0.3f : 0f;
            bool enemyInSight = enemySeenFor >= enemyConfirmTime;
            bool shouldHeal = patient != null && (!enemyInSight || Ratio(patient) < dangerRatio);
            // keep each state for a minimum time: no back-and-forth weapon / animation switching
            if (Time.time >= nextModeChange && shouldHeal != Healing)
            {
                if (shouldHeal) StartHealing(); else StopHealing();
                nextModeChange = Time.time + minModeTime;
            }
        }

        if (Healing && patient != null) Follow();
    }

    void StartHealing()
    {
        Healing = true;
        // BotMedic turns the body towards the patient; the agent must not turn it towards its path at the same time
        if (controller.agent != null) controller.agent.updateRotation = false;
        if (bot.behaviorTree != null) bot.behaviorTree.DisableBehavior(true);
        if (controller.weaponType != WeaponType.Secondary) controller.SwitchWeapon(WeaponType.Secondary);
    }

    void StopHealing()
    {
        Healing = false;
        patient = null;
        if (controller != null && controller.agent != null) controller.agent.updateRotation = true;
        if (controller != null && controller.currWeapon != null && controller.currWeapon.fire) controller.FinishFire();
        if (bot == null || bot.healthManager.death) return;
        if (controller.weaponType != controller.AttackWeapon) controller.SwitchWeapon(controller.AttackWeapon);
        if (bot.behaviorTree != null) bot.behaviorTree.EnableBehavior();
        if (controller.agent != null && controller.agent.isOnNavMesh) controller.agent.isStopped = false;
    }

    Vector3 strafeOffset;
    float nextStrafe;
    bool hasStrafe;

    /// <summary>Keeps up with the patient and moves around him while healing (never stands still like a turret):
    /// far away it chases him straight; close it picks a new spot around him every 1.2-2.2 s, 25-60 degrees to a side,
    /// 4-6 m away, and runs there while facing him (strafe animation).</summary>
    void Follow()
    {
        var agent = controller.agent;
        Vector3 me = transform.position, him = patient.transform.position;
        float dist = Vector3.Distance(me, him);
        if (agent != null && agent.isOnNavMesh && Time.time >= nextMove)
        {
            nextMove = Time.time + 0.25f;
            Vector3 desired;
            if (dist > followMax + 0.5f) // stay inside the medigun range (7 m)
            {
                // too far: run straight to him
                desired = him + (me - him).normalized * followMin;
                hasStrafe = false;
            }
            else
            {
                if (!hasStrafe || Time.time >= nextStrafe)
                {
                    Vector3 fromHim = me - him; fromHim.y = 0f;
                    if (fromHim.sqrMagnitude < 0.01f) fromHim = -patient.transform.forward;
                    float side = Random.value < 0.5f ? -1f : 1f;
                    var dir = Quaternion.Euler(0f, side * Random.Range(25f, 60f), 0f) * fromHim.normalized;
                    strafeOffset = dir * Random.Range(followMin, followMax);
                    nextStrafe = Time.time + Random.Range(1.2f, 2.2f);
                    hasStrafe = true;
                }
                desired = him + strafeOffset;   // follows the patient as he moves
            }
            if (UnityEngine.AI.NavMesh.SamplePosition(desired, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas)) desired = hit.position;
            else nextStrafe = 0f; // unreachable spot: pick another one next time
            agent.isStopped = false;
            agent.SetDestination(desired);
        }
        // keep the trigger down on the medigun
        if (controller.weaponType != WeaponType.Secondary) controller.SwitchWeapon(WeaponType.Secondary);
        if (controller.currWeapon != null && !controller.currWeapon.fire) controller.Fire();
    }

    void LateUpdate()
    {
        if (!Healing || patient == null || !Mine) return;
        // face the patient (after the agent's own rotation) so the medigun locks on him
        Vector3 d = patient.transform.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 12f * Time.deltaTime);
    }

    void OnDisable() { if (Healing) StopHealing(); }
}
