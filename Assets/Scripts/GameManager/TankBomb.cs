using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class TankBomb : MonoBehaviour
{
    public Team team = Team.blue;
    public CoasterfollowerAdv coasterfollowerAdv;
    public TankBombNetwork tankBombNetwork;
    public HealthBase tankHealth;
    public GameObject explosion;
    
    public Animator[] animators;
    public GameObject bomb;
    private bool deployed;
    public AudioClip explosionSound;
    public AudioClip deploySound;
    public AudioSource audioSource;
 
    
    // Start is called before the first frame update
    void Start()
    {
        //InitOnNonMaster();
        
        
    }



    public void InitOnMaster()
    {
        coasterfollowerAdv.enabled = true;
        this.enabled = true;
        coasterfollowerAdv.canMove = true;
    }

    public void InitOnNonMaster()
    {
        coasterfollowerAdv.enabled = false;
        this.enabled = false;
        coasterfollowerAdv.canMove = false;
        
    }

    public void StartRun()
    {
        bomb.SetActive(false);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].SetTrigger("Move");
        }
        audioSource.Play();
        tankHealth.OnDeath += OnDie;
    }

    public void Deploy()
    {
        coasterfollowerAdv.canMove = false;
        deployed = true;
        
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].SetTrigger("Deploy");
        }
        bomb.SetActive(true);
        audioSource.Stop();
        audioSource.PlayOneShot(deploySound);
        tankHealth.OnDeath -= OnDie;
    }

    public void OnDie()
    {
        ArenaManager.Instance.delayShowScoreBoardEndGame = 3;
        if (PhotonNetwork.IsMasterClient)
            ArenaNetworkManager.instan.EndGame(1, 0);
        audioSource.PlayOneShot(explosionSound, 0.75f);
        Instantiate(explosion, transform.position, Quaternion.identity);
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        Debug.Log("Tank Destroyed");
    }
}
