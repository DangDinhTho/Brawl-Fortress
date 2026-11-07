using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PayloadManager : MonoBehaviour
{
    public static PayloadManager instance;
    public Transform bombCart;
    public GameObject deployCam;
    private float timeDelayFinish = 0.5f;
    public GameObject explosion;
    public BombCartNetwork bombCartNetwork;
    
    
    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        ArenaManager.Instance.OnTimeOut += OnTimeOut;
        ArenaManager.Instance.OnGameStarted += OnStartGame;
        ArenaManager.Instance.delayShowScoreBoardEndGame = timeDelayFinish + 3.5f;
        UIMainHud.instan.CreateHUD<PayloadHUD>();
    }

    public void OnStartGame()
    {
        bombCartNetwork.StartGame();
    }

    public void OnDeployBomb()
    {
        ArenaManager.Instance.mainPlayer.FPSPlayer.SwitchTPS();
        deployCam.SetActive(true);
        StartCoroutine(WaitExplosion(timeDelayFinish));
    }

    public void OnTimeOut()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ArenaNetworkManager.instan.EndGame(1, 0);
        }
    }
    
    IEnumerator WaitExplosion(float time)
    {
        yield return new WaitForSeconds(time);
        explosion.SetActive(true);
        bombCartNetwork.bombCartController.coasterfollowerAdv.canMove = false;
        yield return new WaitForSeconds(1);
        bombCartNetwork.HideCart();
    }
}
