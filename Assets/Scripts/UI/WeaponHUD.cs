using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHUD : UIPlayerHUDBase
{
    public WeaponHUDItem weaponHUDItem;
    private TF2Player mainPlayer;
    //public Image crosshair;

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
    }


    public void SetData(TF2Player player)
    {
        this.mainPlayer = player;
        mainPlayer.FPSPlayer.OnSwitchWeapon += OnWeaponSwitched;
        if (mainPlayer.FPSPlayer.currWeapon != null)
            OnWeaponSwitched(mainPlayer.FPSPlayer.currWeapon);
    }

    public void OnWeaponSwitched(Weapon _weapon)
    {
        if(_weapon is Gun && _weapon.weaponConfig.weaponStats.clipSize > 0)
        {
            weaponHUDItem.gameObject.SetActive(true);
            weaponHUDItem.SetWeapon((Gun)_weapon);
        }
        else
        {
            weaponHUDItem.gameObject.SetActive(false);
        }

        // if(_weapon.weaponConfig.crosshair != null)
        // {
        //     crosshair.sprite = _weapon.weaponConfig.crosshair;
        // }
    }
}
