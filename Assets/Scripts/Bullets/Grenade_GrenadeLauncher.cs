using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade_GrenadeLauncher : PoolingEntry
{
    public Rigidbody m_body;
    public float force;
    public float timeAlive;
    public BulletData bulletData;

    public int direcDamage = 100;
    public string explosion;

    private DamageObject damageObject;
    private bool bounced;
    private bool direction;
    public MeshDynamicColor meshDynamicColor;
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!ready) return;
        if (collision.gameObject.CompareTag("HitBox"))
        {
            ready = false;
            if (!bounced && damageObject.isOwner)
            {
                damageObject.damage = direcDamage;
                direction = true;
                DoCollisionDamage(collision.transform.root);
            }

            OnFinish();
        }
        //else if(collision.gameObject.CompareTag("Player"))
        //{
        //    ready = false;
        //    OnFinish();
        //}

        bounced = true;
    }

    public void AddForce(Vector3 forceDirect)
    {
        m_body.AddForce(forceDirect * force, ForceMode.Impulse);
        m_body.AddTorque(transform.forward + transform.right);
    }

    public override void OnCreated(params object[] args)
    {
        base.OnCreated();
        m_body.isKinematic = false;
        bounced = false;
        direction = false;
        if(args.Length > 0)
        {
            Vector3 direct = (Vector3)args[0];
            transform.forward = direct;
            AddForce(direct);
        }

        if (args.Length > 1)
        {
            damageObject = (DamageObject)args[1];
            meshDynamicColor.SetTeamColor(damageObject.team);
        }
        
        //m_body.AddForce(transform.forward * force, ForceMode.Force);
        //m_body.AddTorque(Vector3.forward / 5);
        StartCoroutine(WaitDestroy());
    }

    public override void OnFinish()
    {
        
        base.OnFinish();
        m_body.isKinematic = true;
        //Instantiate(explosion, transform.position, transform.rotation);
        if (direction)
        {
            ObjectPooling.Instance.Instantiate(explosion, transform.position, transform.rotation);
        }
        else
        {
            ObjectPooling.Instance.Instantiate(explosion, transform.position, transform.rotation, damageObject);
        }
        

    }
    
    
    void DoCollisionDamage(Transform target)
    {
            var damageable = target.GetComponent<HealthBase>();
            if (damageable == null || damageable.team == damageObject.team) return;

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
