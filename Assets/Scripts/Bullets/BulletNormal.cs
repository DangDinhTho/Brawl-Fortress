using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletNormal : PoolingEntry
{
    public float timeAlive;
    public int direcDamage = 100;
    public float speed = 10;
    protected DamageObject damageObject;
    private Vector3 direction;
    public string impactFX;

    public virtual void OnTriggerEnter(Collider other)
    {
        if (!ready) return;
        ready = false;
        if (other.CompareTag("HitBox"))
        {
            if (damageObject.isOwner)
            {
                DoCollisionDamage(other.transform.root);
            }

        }

        OnFinish();
        
    }

    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (!ready) return;
    //    if (collision.gameObject.CompareTag("HitBox"))
    //    {
    //        ready = false;
    //        if (damageObject.isOwner)
    //        {
    //            //damageObject.damage = direcDamage;
    //            //direction = true;
    //            print("hit");
    //            DoCollisionDamage(collision.transform.root);
    //        }


    //    }

    //    OnFinish();
    //}

    void Update()
    {
        Move();
    }

    public void Move()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    public override void OnCreated(params object[] args)
    {
        base.OnCreated();
        if (args.Length > 0)
        {
            direction = (Vector3)args[0];
            transform.forward = direction;
        }

        if (args.Length > 1)
        {
            damageObject = (DamageObject)args[1];
        }

        //m_body.AddForce(transform.forward * force, ForceMode.Force);
        //m_body.AddTorque(Vector3.forward / 5);
        StartCoroutine(WaitDestroy());
    }

    public override void OnFinish()
    {
        base.OnFinish();
        if (!string.IsNullOrEmpty(impactFX))
            ObjectPooling.Instance.Instantiate(impactFX, transform.position, transform.rotation);

    }


    public virtual void DoCollisionDamage(Transform target)
    {
        var damageable = target.GetComponent<HealthBase>();
        if (damageable == null || damageable.death || damageable.team == damageObject.team) return;

        float distance = Vector3.Distance(transform.position, damageable.centerBody.position);
        if (distance < 0.2) distance = 0.2f;
        float heSo = Mathf.Clamp(1 / distance, 0.75f, 2.2f);
        Vector3 direction = damageable.centerBody.position - transform.position;
        if (direction.y <= 0)
            direction.y = 1;
        direction = direction.normalized * heSo;


        damageable.TakeDamage(damageObject, direction);
    }


    IEnumerator WaitDestroy()
    {
        yield return new WaitForSeconds(timeAlive);
        OnFinish();
    }
}