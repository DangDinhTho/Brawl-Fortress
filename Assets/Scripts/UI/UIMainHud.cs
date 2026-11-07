using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMainHud : MonoBehaviour
{
    public static UIMainHud instan;
    private Dictionary<string, UIPlayerHUDBase> uiPlayerHUDs = new Dictionary<string, UIPlayerHUDBase>();
    public Transform hudContainer;

    public UIHudItemHealth healthHUD;
    public WeaponHUD weaponHUD;


    private void Awake()
    {
        instan = this;
    }

    private void Start()
    {
        foreach (Transform popup in hudContainer)
        {
            Destroy(popup.gameObject);
        }

    }

    public void Init(TF2Player player)
    {
        healthHUD.SetData(player);
        weaponHUD.SetData(player);
        switch (player.tF2Class)
        {
            case TF2Class.engineer:
                CreateHUD<UIEngineerHUD>();
                break;
            case TF2Class.spy:
                CreateHUD<UISpyHUD>();
                break;
            //case TF2Class.sniper:
                //CreateHUD<UISniperHUD>();
                //break;
        }
    }


    public T CreateHUD<T>(params object[] args) where T : UIPlayerHUDBase
    {
        return (T)CreateHud(typeof(T).ToString(), args);
    }

    public T GetHUD<T>(params object[] args) where T : UIPlayerHUDBase
    {
        if(uiPlayerHUDs.TryGetValue(typeof(T).ToString(), out var hud))
        {
            return (T)hud;
        }
        else
        {
            return CreateHUD<T>(args);
        }
    }
    
    public UIPlayerHUDBase CreateHud(string hudName, params object[] args)
    {
        if (!uiPlayerHUDs.TryGetValue(hudName, out var hud))
        {
            GameObject pref = GetHudPref(hudName);
            if (pref != null)
            {
                GameObject obj = Instantiate(pref, this.hudContainer.transform) as GameObject;
                hud = obj.GetComponent<UIPlayerHUDBase>();
                hud.name = pref.name;

                hud.transform.localPosition = Vector3.zero;
                hud.transform.localScale = Vector3.one;
                hud.gameObject.SetActive(true);
                hud.OnCreate(args);

                uiPlayerHUDs.Add(hudName, hud);

                return hud;
            }
            return null;
        }
        else
        {
            return hud;
        }

       
    }

    public GameObject GetHudPref(string hubName)
    {
        return Resources.Load("HUDs/" + hubName) as GameObject;
    }

    
}
