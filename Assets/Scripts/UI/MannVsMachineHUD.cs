using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MannVsMachineHUD : UIPlayerHUDBase
{
    private ManvsMachineManager manvsMachineManager;

    public UIDirection atomBombDirec;
    public GameObject tankBombWave;

    public Image tankHealthFill;
    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        manvsMachineManager = ManvsMachineManager.instan;
        manvsMachineManager.OnStartWave += OnStartWave;
    }

    public void OnStartWave(int wave)
    {
        if(wave == 1)
        {
            atomBombDirec.target = manvsMachineManager.bomb.transform;
            atomBombDirec.mainPlayer = ArenaManager.Instance.mainPlayer.transform;
            atomBombDirec.gameObject.SetActive(true);
            tankBombWave.SetActive(false);
        }
        else if(wave == 2)
        {
            tankHealthFill.fillAmount = 1;
            manvsMachineManager.tankBomb.tankHealth.OnHealthChanged += OnTankHealthChange;
            atomBombDirec.gameObject.SetActive(false);
            tankBombWave.SetActive(true);
        }
    }

    public void OnTankHealthChange(int crrHealth, int maxHealth)
    {
        tankHealthFill.fillAmount = crrHealth * 1.0f / maxHealth;
    }
}
