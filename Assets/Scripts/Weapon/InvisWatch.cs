using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InvisWatch : MonoBehaviour
{
    public int maxEXP = 100;
    private float currentEXP;
    private float speedDown = 5;
    public SpyController spyController;

    public Material[] invisiblityMats;
    private bool invisibility;

    public Action OnInvisibility;
    public Action OnShowAvailble;
    public Action<float> OnEXPUpdate;

    //public Image expProcess;


    // Start is called before the first frame update
    void Start()
    {
        currentEXP = maxEXP;
       // StartCoroutine(InitDelay());
    }

    //public IEnumerator InitDelay()
    //{
    //    yield return new WaitForSeconds(1.2f);
    //    if (spyController.player.isMainPlayer)
    //    {
    //        spyController.player.FPSPlayer.OnSwitchCam += OnSwitchCam;
    //    }
    //    else
    //    {
    //        spyController.player.healthManager.OnDeath += ForceShowAvailble;
    //    }
    //}

    public bool Invisibility()
    {
        if (invisibility || currentEXP <= 0) return false;
        invisibility = true;
        if (spyController.player.isMainPlayer)
        {
            spyController.player.FPSPlayer.OnSwitchCam += OnSwitchCam;
        }
        else
        {
            spyController.player.healthManager.OnDeath += ForceShowAvailble;
        }
        StartCoroutine(CountDown());
       
        OnInvisibility?.Invoke();
        StartCoroutine(WaitAction(() =>
        {
            spyController.player.SetMeshInvsibility();
            spyController.player.playerNetwork.SyncInvisibility(true);
        }));

        return true;
    }

    public void Buff(float chance)
    {
        if(currentEXP >= maxEXP) return;
        currentEXP += maxEXP * chance;
        if (currentEXP > maxEXP) currentEXP = maxEXP;
        OnEXPUpdate?.Invoke(currentEXP);
    }

    public void ForceShowAvailble()
    {
        ShowAvailble(true);
    }

    public void OnSwitchCam(bool isFree)
    {
        if (isFree)
        {
            ForceShowAvailble();
        }
    }

    public bool ShowAvailble(bool force = false)
    {
        if (!invisibility) return false;
        invisibility = false;
        if (spyController.player.isMainPlayer)
        {
            spyController.player.FPSPlayer.OnSwitchCam -= OnSwitchCam;
        }
        else
        {
            spyController.player.healthManager.OnDeath -= ForceShowAvailble;
        }
        OnShowAvailble?.Invoke();
        if (force)
        {
            spyController.player.SetMeshTeam();
            spyController.player.playerNetwork.SyncInvisibility(false);
        }
        else
        {
            StartCoroutine(WaitAction(() =>
            {
                spyController.player.SetMeshTeam();
                spyController.player.playerNetwork.SyncInvisibility(false);
            }));
        }
        return true;
    }

    IEnumerator WaitAction(Action callBack)
    {
        yield return new WaitForSeconds(0.75f);
        callBack?.Invoke();
    }

    private void OnExpUpdate()
    {
        //expProcess.fillAmount = currentEXP / maxEXP;
        OnEXPUpdate?.Invoke(currentEXP);
    }

    IEnumerator CountDown()
    {
        while(invisibility && currentEXP > 0)
        {
            yield return null;
            currentEXP -= speedDown * Time.deltaTime;
            OnExpUpdate();
        }

        if (invisibility)
            ShowAvailble();
    }
}
