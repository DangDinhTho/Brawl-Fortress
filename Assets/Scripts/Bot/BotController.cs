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

    public void Init()
    {
        //behaviorTree = GetComponent<BehaviorTree>();
        SwitchWeapon(bot.characterConfig.defaultWeapon);
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
        SwitchWeapon(bot.characterConfig.defaultWeapon);
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

}
