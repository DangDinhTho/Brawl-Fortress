using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHUDItem : MonoBehaviour
{
    public Text txtCurrBullet; 
    public Text txtTotalBullet;

    private Gun gun;

    public void SetWeapon(Gun _gun)
    {
        this.gun = _gun;

        if(_gun.weaponConfig.weaponStats.inventorySize != 0)
        {
            //gun.OnFire -= OnBulletUpdate;
            //gun.OnReloadLoop -= OnBulletUpdate;
            //gun.OnReloadDone -= OnBulletUpdate;
            gun.OnBulletUpdate -= OnBulletUpdate;
            gun.OnInventoryUpdate -= OnBulletUpdate;
            txtTotalBullet.gameObject.SetActive(true);
            //gun.OnFire += OnBulletUpdate;
            //gun.OnReloadLoop += OnBulletUpdate;
            //gun.OnReloadDone += OnBulletUpdate;
            gun.OnBulletUpdate += OnBulletUpdate;
            gun.OnInventoryUpdate += OnBulletUpdate;
            OnBulletUpdate();
        }
        else
        {
            //gun.OnFire -= OnBulletUpdateStyle2;
            //gun.OnReloadLoop -= OnBulletUpdateStyle2;
            //gun.OnReloadDone -= OnBulletUpdateStyle2;
            gun.OnInventoryUpdate -= OnBulletUpdateStyle2;
            gun.OnBulletUpdate -= OnBulletUpdateStyle2;
            txtTotalBullet.gameObject.SetActive(false);
            //gun.OnFire += OnBulletUpdateStyle2;
            //gun.OnReloadLoop += OnBulletUpdateStyle2;
            //gun.OnReloadDone += OnBulletUpdateStyle2;
            gun.OnBulletUpdate += OnBulletUpdateStyle2;
            gun.OnInventoryUpdate += OnBulletUpdateStyle2;
            OnBulletUpdateStyle2();
        }

    }

    public void OnBulletUpdate()
    {
        txtCurrBullet.text = gun.bulletInClip.ToString();
        txtTotalBullet.text = gun.inventory.ToString();
    }

    public void OnBulletUpdateStyle2()
    {
        txtCurrBullet.text = gun.bulletInClip.ToString();
    }

    public void OnReloadClick()
    {
        gun.StartReload();
    }
}
