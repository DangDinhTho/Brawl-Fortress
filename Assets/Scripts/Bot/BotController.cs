using BehaviorDesigner.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BotController : MonoBehaviour
{
    public NavMeshAgent agent;
    //[HideInInspector]
    //public Vector3 Velocity;
    [HideInInspector]
    public PlayerState State;
    [HideInInspector] public PlayerFPState fpsState;
    [HideInInspector]
    public WeaponType weaponType;
    public TF2Bot bot;
    //public FindEnemy findEnemy;
    //private FieldOfView fieldOfView;
    //public Gun primaryGun;
    //public BehaviorTree behaviorTree;
    public Weapon currWeapon;
    [HideInInspector]
    public bool blockAttack;

    /// <summary>Weapon the bot fights with. A medic's default (what a player spawns with) is the medigun, but a medic bot
    /// fights with its primary and only takes the medigun out to heal (BotMedic).</summary>
    public WeaponType AttackWeapon => IsHealerMedic ? WeaponType.Primary : bot.characterConfig.defaultWeapon;
    /// <summary>A medic bot with the medigun + primary loadout (not a zombie medic, which only has a melee attack).</summary>
    bool IsHealerMedic => bot != null && bot.tF2Class == TF2Class.medic && !(bot is TF2Zombie)
        && bot.remotePlayer != null && bot.remotePlayer.weaponSlots.ContainsKey(WeaponType.Primary) && bot.remotePlayer.weaponSlots.ContainsKey(WeaponType.Secondary);
    BotMedic medic;

    public void Init()
    {
        //behaviorTree = GetComponent<BehaviorTree>();
        // spy bots cloak now and then (see BotSpy)
        if (bot != null && bot.tF2Class == TF2Class.spy && !(bot is TF2Zombie))
        {
            var spyBot = GetComponent<BotSpy>();
            if (spyBot == null) spyBot = gameObject.AddComponent<BotSpy>();
            spyBot.Init(this);
        }
        // engineer bots build a Sentry, then a Dispenser, when no enemy is around (see BotEngineer)
        if (bot != null && bot.tF2Class == TF2Class.engineer && !(bot is TF2Zombie))
        {
            var engineer = GetComponent<BotEngineer>();
            if (engineer == null) engineer = gameObject.AddComponent<BotEngineer>();
            engineer.Init(this);
        }
        if (IsHealerMedic)
        {
            medic = GetComponent<BotMedic>();
            if (medic == null) medic = gameObject.AddComponent<BotMedic>();
            medic.Init(bot, this);
        }
        SwitchWeapon(AttackWeapon);
        //SwitchWeapon(WeaponType.Secondary);
        bot.healthManager.OnDeath += OnDeath;
        bot.healthManager.OnRespawn += OnSpawn;
        //primaryGun = (Gun)bot.remotePlayer.weapon;
    }

    private void OnEnable()
    {
        bot.healthManager.OnDeath -= OnDeath;
        bot.healthManager.OnRespawn -= OnSpawn;
    }
    //
    // private void Update()
    // {
    //     if ((bool)behaviorTree.GetVariable("hasEnemy").GetValue())
    //     {
    //         bot.targetLook = ((Transform)behaviorTree.GetVariable("enemy").GetValue()).position + Vector3.up * (bot.remotePlayer.tPSLookAt.aim.position.y - transform.position.y);
    //     }
    //     else
    //         bot.targetLook = transform.forward * 2 + bot.remotePlayer.tPSLookAt.aim.position;
    // }



    public void OnDeath()
    {
        if (!bot.photonView.IsMine) return;
        agent.isStopped = true;
        bot.behaviorTree.StopAllCoroutines();
        bot.behaviorTree.enabled = false;
        bot.botPickup.gameObject.SetActive(false);
    }

    public void OnSpawn()
    {
        if (!bot.photonView.IsMine) return;
        BuffWeapon(1, true);
        agent.isStopped = false;
        bot.behaviorTree.enabled = true;
        SwitchWeapon(AttackWeapon);
        bot.botPickup.gameObject.SetActive(true);
    }

    public void BuffWeapon(float rance, bool constrainClip = false)
    {
        foreach (Weapon wp in bot.remotePlayer.weaponSlots.Values)
        {
            if (wp as Gun)
            {
                ((Gun)wp).Buff(rance, constrainClip);
            }
        }
    }

    public void SwitchWeapon(WeaponType _weaponType)
    {
        weaponType = _weaponType;
        blockAttack = true;
        bot.remotePlayer.SwitchWeapon(_weaponType);
        bot.playerNetwork.SyncSwitchWeapon(_weaponType);
        // StartCoroutine(SwitchWeaponDelay());
        currWeapon = bot.remotePlayer.weapon;
        if (currWeapon is Gun)
        {
            Gun currGun = (Gun)currWeapon;
            currGun.OnClipEmpty -= OnClipEmpty;
            currGun.OnClipEmpty += OnClipEmpty;
        }
        blockAttack = false;
        fpsState = PlayerFPState.Idle;

    }

    IEnumerator WaitSwitchWeapon(WeaponType _weaponType)
    {
        yield return new WaitForSeconds(1);
        if (bot.healthManager.death) yield break;
        SwitchWeapon(_weaponType);
    }

    //IEnumerator SwitchWeaponDelay()
    //{
    //    yield return new WaitForSeconds(0.5f);
    //    if (bot.healthManager.death) yield break;
    //    currWeapon = bot.remotePlayer.weapon;
    //    if (currWeapon is Gun)
    //    {
    //        Gun currGun = (Gun)currWeapon;
    //        currGun.OnClipEmpty -= OnClipEmpty;
    //        currGun.OnClipEmpty += OnClipEmpty;
    //    }
    //    blockAttack = false;
    //}

    [Tooltip("A bot holding the trigger with no enemy in sight releases it after this many seconds")]
    public float releaseFireWithoutTarget = 0.5f;
    float noTargetFireTime;
    FieldOfView fov;

    /// <summary>Safety net for every AI task that pulls the trigger: continuous weapons (flamethrower, minigun...) stop
    /// once no enemy has been in sight (FieldOfView; enemies hidden in a bush count as not seen) for a short while.</summary>
    private void LateUpdate()
    {
        if (bot == null || currWeapon == null || !currWeapon.fire || !bot.photonView.IsMine) { noTargetFireTime = 0f; return; }
        if (fov == null) fov = GetComponent<FieldOfView>();
        if (fov != null && fov.canSeePlayer) { noTargetFireTime = 0f; return; }
        if (medic != null && medic.Healing) { noTargetFireTime = 0f; return; } // healing a teammate: no enemy needed
        noTargetFireTime += Time.deltaTime;
        if (noTargetFireTime >= releaseFireWithoutTarget) { noTargetFireTime = 0f; FinishFire(); }
    }

    public void Fire()
    {

        currWeapon.BeginFire();
    }

    public void FinishFire()
    {
        currWeapon.FinishFire();
    }

    public void OnClipEmpty()
    {
        Gun currGun = (Gun)currWeapon;
        if (currGun.inventory <= 0)
        {
            if (bot.tF2Class == TF2Class.medic)
            {
                StartCoroutine(WaitSwitchWeapon(WeaponType.Mele));
            }
            else
                StartCoroutine(WaitSwitchWeapon(weaponType + 1));
        }
    }


    /// <summary>Boss dodge roll (BossFollowEnemy / BossMoveToTarget). Ported from TF2_AI.</summary>
    [Header("Boss roll")]
    [Tooltip("Distance covered by one dodge roll (m)")]
    public float rollDistance = 5f;
    bool rolling;
    float rollClipLength = -1f;

    /// <summary>Dodge roll: plays the roll animation AND dashes the body along the current path (or forward), front-loaded
    /// speed for the length of the roll clip, through NavMeshAgent.Move so it never leaves the NavMesh. Path following is
    /// paused meanwhile so the two motions do not fight.</summary>
    public void Roll()
    {
        if (rolling || bot == null || bot.remotePlayer == null) return;
        StartCoroutine(RollRoutine());
    }

    System.Collections.IEnumerator RollRoutine()
    {
        rolling = true;
        var nav = GetComponent<UnityEngine.AI.NavMeshAgent>();
        Vector3 dir = nav != null && nav.hasPath ? nav.steeringTarget - transform.position : transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir);
        bot.remotePlayer.playerAnimator.Roll();

        float duration = RollDuration();
        bool wasStopped = nav != null && nav.isOnNavMesh && nav.isStopped;
        if (nav != null && nav.isOnNavMesh) nav.isStopped = true;
        float t = 0f;
        while (t < duration)
        {
            float dt = Time.deltaTime;
            // speed 2*D/T at the start falling linearly to 0: covers exactly rollDistance
            float speed = 2f * rollDistance / duration * (1f - t / duration);
            if (nav != null && nav.isOnNavMesh) nav.Move(dir * speed * dt);
            else transform.position += dir * speed * dt;
            t += dt;
            yield return null;
        }
        if (nav != null && nav.isOnNavMesh) nav.isStopped = wasStopped;
        rolling = false;
    }

    float RollDuration()
    {
        if (rollClipLength > 0f) return rollClipLength;
        rollClipLength = 0.7f;
        var anim = bot.remotePlayer.playerAnimator.Animator;
        if (anim != null && anim.runtimeAnimatorController != null)
            foreach (var clip in anim.runtimeAnimatorController.animationClips)
                if (clip != null && clip.name.ToLower().Contains("roll")) { rollClipLength = Mathf.Clamp(clip.length * 0.55f, 0.4f, 1.2f); break; } // the clip is roll + get-up-to-run (in place): dash only during the roll part
        return rollClipLength;
    }
}
