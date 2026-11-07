using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISpyHUD : UIPlayerHUDBase
{
    public static UISpyHUD instance;
    public TF2Player mainPlayer;

    private SpyController spyController;
    private bool isInvis;
    public Image expProcess;
    private float maxEXP;

    private void Awake()
    {
        instance = this;
    }

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);

        mainPlayer = ArenaManager.Instance.mainPlayer;
        spyController = mainPlayer.remotePlayer.GetComponent<SpyController>();

        maxEXP = spyController.invisWatch.maxEXP;
        OnInvisWatchExpUpdate(maxEXP);

        spyController.invisWatch.OnInvisibility += OnSpyInvisibility;
        spyController.invisWatch.OnShowAvailble += OnSpyShowAvailble;
        spyController.invisWatch.OnEXPUpdate += OnInvisWatchExpUpdate;

    }

    public void BuffWatch(float chance)
    {
        spyController.invisWatch.Buff(chance);
    }

    private void OnSpyInvisibility()
    {
        //mainPlayer.fPSReferences.animator.SetBool("Invis", true);
    }

    private void OnSpyShowAvailble()
    {
        //mainPlayer.fPSReferences.animator.SetBool("Invis", false);
        isInvis = false;
    }

    private void OnInvisWatchExpUpdate(float currExp)
    {
        expProcess.fillAmount = currExp / maxEXP;
    }

    public void OnVisibilityClick()
    {
        if (isInvis)
        {
            if (spyController.ShowAvailble())
            {
                isInvis = false;
            }
        }
        else
        {
            if (spyController.Invisibility())
            {
                isInvis = true;
            }
        }
    }
}
