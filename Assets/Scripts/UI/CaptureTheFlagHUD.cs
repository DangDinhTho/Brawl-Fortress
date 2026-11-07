using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class CaptureTheFlagHUD : UIPlayerHUDBase
{
    public static CaptureTheFlagHUD instan;

    public Dictionary<Team, FlagController> flags = new Dictionary<Team, FlagController>();
    public UIFlagDirection[] uIFlagDirections;

    public GameObject pickupMain;
    public Image holdTeam;
    public Text[] txtScore;
    public Text txtTargetScore;
    
    private void Awake()
    {
        instan = this;
    }

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        //FlagController[] flagControllers = (FlagController[])args[0];
        for (int i = 0; i < 2; i++)
        {
            FlagController flag = (FlagController)args[i];
            InitFlag(flag);
        }

        if(ArenaManager.Instance.gameState == GameState.Started)
        {
            OnGameStated();
        }
        else
        {
            ArenaManager.Instance.OnGameStarted += OnGameStated;
        }

        txtTargetScore.text = string.Format(LocalizationManager.Localize("ctf_play_to"), (int)args[2]);
        UpdateScore();
    }


    public void UpdateScore()
    {
        txtScore[0].text = CaptureTheFlagManager.instan.scores[Team.red].ToString();
        txtScore[1].text = CaptureTheFlagManager.instan.scores[Team.blue].ToString();
    }

    public void OnGameStated()
    {
        holdTeam.sprite = GUIManager.instan.iconAtlas.GetSprite($"objectives_flagpanel_carried_{ConfigManager.GetEnemyTeam(ArenaManager.Instance.mainPlayer.team)}");

        for (int i = 0; i < uIFlagDirections.Length; i++)
        {
            uIFlagDirections[i].StartUpdate();
        }
    }

    public void InitFlag(FlagController flag)
    {
        flags.Add(flag.team, flag);
        uIFlagDirections[(int)flag.team].SetFlag(flag);
    }

    public void OnMainPlayerHold(TF2Player mainPlayer)
    {
        pickupMain.SetActive(true);
    }

    public void OnMainPlayerDrop(TF2Player mainPlayer)
    {
        pickupMain.SetActive(false);
    }
}
