using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JungleBossHUD : UIPlayerHUDBase
{
    private JungleModeManager jungleManager;

    public Image bossHealthFill;
    public Text bossName;

    private TF2Boss boss;

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        jungleManager = JungleModeManager.instan;
        ArenaManager.Instance.OnGameStarted += OnGameStart;
        ArenaManager.Instance.OnTimeOut += OnTimeout;

        bossName.text = LocalizationManager.Localize($"Boss_{jungleManager.bossType}");
    }

    public void OnGameStart()
    {
        boss = jungleManager.boss;

        bossHealthFill.fillAmount = 1;
        boss.healthManager.OnHealthChanged += OnTankHealthChange;
        boss.healthManager.OnDeath += OnBossDead;
    }

    public void OnTankHealthChange(int crrHealth, int maxHealth)
    {
        bossHealthFill.fillAmount = crrHealth * 1.0f / maxHealth;
    }

    public void OnBossDead()
    {
        //Win
        if (PhotonNetwork.IsMasterClient)
        {
            ArenaNetworkManager.instan.EndGame(1, 0);
        }
       
    }

    public void OnTimeout()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ArenaNetworkManager.instan.EndGame(0, 1);
        }
    }
}
