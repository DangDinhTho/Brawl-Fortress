using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;
using UnityEngine.UI;

public class UISniperHUD : UIPlayerHUDBase
{
    public TF2Player mainPlayer;

    private FPSReferences fpsReferences;
    public GameObject sniperAim;
    private bool sniping;
    private float defaultFOV;
    public GameObject snipButton;
    private Sniperrifle sniperrifle;
    //public LayerMask snipLayerMask;
    //private LayerMask defaultLayerMask;
    
    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        mainPlayer = ArenaManager.Instance.mainPlayer;
        //fpsReferences = mainPlayer.fPSReferences;
        defaultFOV = fpsReferences.fpsCam.fieldOfView;
        //defaultLayerMask = fpsReferences.fpsCam.cullingMask;

        mainPlayer.healthManager.OnDeath += ForceCloseSniper;
        mainPlayer.FPSPlayer.OnSwitchWeapon += OnSwitchWeapon;
        ((Gun)mainPlayer.FPSPlayer.weaponSlots[WeaponType.Primary]).OnShoot += OnShot;

    }

    public void OnSwitchWeapon(Weapon weapon)
    {
        if (weapon is Sniperrifle)
        {
            snipButton.SetActive(true);
            sniperrifle = (Sniperrifle)weapon;
        }
        else
        {
            if (sniping) ForceCloseSniper();
            snipButton.SetActive(false);
        }
        
    }


    public void OnShot()
    {
        if (sniping)
        {
            StartCoroutine(WaitReload());
        }
    }

    IEnumerator WaitReload()
    {
        sniping = false;
        sniperAim.SetActive(false);
        fpsReferences.fpsCam.fieldOfView = defaultFOV;
        fpsReferences.fpsCam.transform.GetChild(0).gameObject.SetActive(true);
        yield return new WaitForSeconds(1.65f);
        if (mainPlayer.healthManager.death) yield break;
        OnSnipClick();
    }

    public void OnSnipClick()
    {
        if (mainPlayer.FPSPlayer.weaponType != WeaponType.Primary) return;
        sniping = !sniping;
        sniperAim.SetActive(sniping);
        fpsReferences.fpsCam.fieldOfView = sniping ? 30 : defaultFOV;
        fpsReferences.fpsCam.transform.GetChild(0).gameObject.SetActive(!sniping);
        mainPlayer.FPSPlayer.Snip(sniping);
        
    }


    public void ForceCloseSniper()
    {
        sniping = false;
        sniperAim.SetActive(false);
        fpsReferences.fpsCam.fieldOfView = defaultFOV;
        fpsReferences.fpsCam.transform.GetChild(0).gameObject.SetActive(true);
        mainPlayer.FPSPlayer.Snip(false);
    }
    
    
}
