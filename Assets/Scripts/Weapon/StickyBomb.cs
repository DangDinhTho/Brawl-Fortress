using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickyBomb : PoolingEntry
{
    public Rigidbody m_body;
    public float force;
    public float timeAlive;
    public float explosionRadius = 3f;

    public string explosion;

    private DamageObject damageObject;
    public MeshDynamicColor[] meshDynamicColor;
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("HitBox"))
        {
            
        }
        else
        {
            m_body.isKinematic = true;
        }
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
        if (args.Length > 0)
        {
            Vector3 direct = (Vector3)args[0];
            transform.forward = direct;
            AddForce(direct);
        }

        if (args.Length > 1)
        {
            damageObject = (DamageObject)args[1];
            for(int i = 0; i < meshDynamicColor.Length; i++)
            {
                meshDynamicColor[i].SetTeamColor(damageObject.team);
            }
            
        }

        //m_body.AddForce(transform.forward * force, ForceMode.Force);
        //m_body.AddTorque(Vector3.forward / 5);
        StartCoroutine(WaitDestroy());
    }

    public override void OnFinish()
    {

        base.OnFinish();
        m_body.isKinematic = true;

        ObjectPooling.Instance.Instantiate(explosion, transform.position, transform.rotation, damageObject, explosionRadius);



    }

    IEnumerator WaitDestroy()
    {
        yield return new WaitForSeconds(timeAlive);
        OnFinish();
    }
}
