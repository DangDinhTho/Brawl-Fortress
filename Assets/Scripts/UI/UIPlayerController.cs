using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPlayerController : MonoBehaviour
{
    public static UIPlayerController instan;
    private TF2Player mainPlayer;
    public UIPDAButton pdaButton;

    private void Awake()
    {
        instan = this;
    }

    public void InitData(TF2Player _mainPlayer)
    {
        this.mainPlayer = _mainPlayer;
        SetupLayout();
    }

    private void SetupLayout()
    {
        if (mainPlayer.weapons.TryGetValue(WeaponType.PDA, out var pda))
        {
            pdaButton.InitData(pda);
        }
        else
        {
            pdaButton.gameObject.SetActive(false);
        }
    }
}
