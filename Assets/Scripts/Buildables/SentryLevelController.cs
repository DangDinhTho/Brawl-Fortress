using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentryLevelController : MonoBehaviour
{
    public int level;
    public float interval = 0.2f;
    public Transform[] shotPoints;
    private bool firing;
    private DamageObject damageObject = new DamageObject();
    private float minDamage = 30;
    private float maxDamage = 40;
    private SentryController sentryController;
    private BulletId _bulletId = BulletId.MinigunBullet;
    private int currentShotpoint = 0;
    public Animator animator;
    
    
    public Transform rocketShotpoint;
    public float intervalRocket = 5f;
    private float rocketMinDamage = 80;
    private float rocketMaxDamage = 100;

    public int clipSize = 50;
    public int bulletRemain;

    private void Awake()
    {
        sentryController = GetComponentInParent<SentryController>();
        
    }

    private void Start()
    {
        StartCoroutine(InitLate());
        bulletRemain = clipSize;
        StartCoroutine(WaitBuffBullet());
    }

    public bool CheckCanFire()
    {
        return bulletRemain > 0;
    }

    IEnumerator InitLate()
    {
        yield return new WaitForSeconds(0.2f);
        damageObject = new DamageObject()
        {
            team = sentryController.team,
            ownerId = sentryController.owner.viewID,
            owner = sentryController.owner
        };
    }

    public void Buff(float chance)
    {

    }

    public void StartFire()
    {
        if(firing || bulletRemain <= 0) return;
        firing = true;
        StartCoroutine(Fire());
        //if (level == 3)
        //{
        //    StartCoroutine(RocketFire());
        //}
        animator.SetBool("Fire", true);
    }

    public void FinishFire()
    {
        firing = false;
        animator.SetBool("Fire", false);
    }

    IEnumerator Fire()
    {
        float nextTimeShot = 0;
        while (firing)
        {
            if (Time.time > nextTimeShot)
            {
                if (bulletRemain <= 0)
                {
                    sentryController.SyncFire(false);
                    yield break;
                }
                damageObject.damage = UnityEngine.Random.Range(minDamage, maxDamage);
                damageObject.isOwner = sentryController.photonView.IsMine;
                PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(_bulletId.ToString(),
                    shotPoints[currentShotpoint].position, shotPoints[currentShotpoint].rotation,
                    shotPoints[currentShotpoint].forward, damageObject);
                currentShotpoint = currentShotpoint >= shotPoints.Length - 1 ? 0 : currentShotpoint + 1;
                nextTimeShot = Time.time + interval;
                sentryController.audioSource.PlayOneShot(sentryController.sentryShootSound);
                bulletRemain--;
            }

            yield return null;
        }
    }
    

    IEnumerator WaitBuffBullet()
    {
        int buff = (int)(clipSize / 15f);
        while (gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(1);
            
            if (!firing && bulletRemain < clipSize)
            {
                if (bulletRemain <= 0)
                {
                    yield return new WaitForSeconds(5);
                }
                bulletRemain += buff;
                if (bulletRemain > clipSize) bulletRemain = clipSize;
            }
        }
    }
    
    IEnumerator RocketFire()
    {
        float nextTimeShot = 0;
        while (firing)
        {
            if (Time.time > nextTimeShot)
            {
                damageObject.damage = UnityEngine.Random.Range(rocketMinDamage, rocketMaxDamage);
                damageObject.isOwner = sentryController.photonView.IsMine;
                PoolingEntry currentBullet = ObjectPooling.Instance.Instantiate(BulletId.SentryRocket.ToString(),
                    rocketShotpoint.position, rocketShotpoint.rotation,
                    rocketShotpoint.forward, damageObject);
                nextTimeShot = Time.time + intervalRocket;
            }

            yield return null;
        }
    }
}
