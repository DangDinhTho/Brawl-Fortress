using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    public GameObject explosion;


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("HitBox"))
        {
            Instantiate(explosion, transform.position, transform.rotation);
        }
    }
}
