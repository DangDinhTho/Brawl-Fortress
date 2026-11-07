using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PopupEngineerBuilder : PopupBase
{
    public UIBuilderItem[] allUIBuilderItems;
    private Dictionary<BuildableType, UIBuilderItem> uiBuilderItems = new Dictionary<BuildableType, UIBuilderItem>();
    private TF2Player mainPlayer;
    public GameObject selectionBuildablePanel;
    public GameObject buildPanel;
    public GameObject movePanel;
    [HideInInspector]
    public bool isMoveBuildable;
    private EngineerBlueprint engineerBlueprint;
    private WeaponType previousWeapon = WeaponType.Primary;
    private UIEngineerHUD uiEngineerHUD;
    private BuildableEntry moveEntry;
    private Wrench wrench;

    //private void Awake()
    //{
       
    //}

    public void Init()
    {
        for (int i = 0; i < allUIBuilderItems.Length; i++)
        {
            uiBuilderItems.Add(allUIBuilderItems[i].buildableType, allUIBuilderItems[i]);
        }
    }

    private void Start()
    {
       // Init

    }

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);

        if (uiBuilderItems.Count == 0) Init();

        mainPlayer = ArenaManager.Instance.mainPlayer;
        engineerBlueprint = mainPlayer.remotePlayer.GetComponentInChildren<EngineerBlueprint>();
        uiEngineerHUD = UIMainHud.instan.GetHUD<UIEngineerHUD>();

        wrench = (Wrench)mainPlayer.FPSPlayer.weaponSlots[WeaponType.Mele];

        if (args.Length > 0)
        {
            moveEntry = (BuildableEntry)args[0];
            isMoveBuildable = true;
        }
        else
        {
            isMoveBuildable = false;
        }

        if (isMoveBuildable)
        {
            SwitchMovePanel();
        }
        else
        {
            SwitchSelectBuildableTypePanel();
        }

        for(BuildableType buildableType = BuildableType.Sentry; buildableType <= BuildableType.TeleporterExit; buildableType++)
        {
            if(uiEngineerHUD.uiBuildableHUDItems[buildableType].buildableEntry != null && !uiEngineerHUD.uiBuildableHUDItems[buildableType].buildableEntry.health.death)
            {
                uiBuilderItems[buildableType].InitData(uiEngineerHUD.uiBuildableHUDItems[buildableType].buildableEntry);
            }
            else
            {
                uiBuilderItems[buildableType].InitEmpty();
            }
        }
        
        mainPlayer.healthManager.OnDeath += OnPlayerDie;
        mainPlayer.FPSPlayer.OnSwitchWeapon += OnWeaponSwitched;
    }

    public override void OnDismiss()
    {
        base.OnDismiss();
        mainPlayer.healthManager.OnDeath -= OnPlayerDie;
        mainPlayer.FPSPlayer.OnSwitchWeapon -= OnWeaponSwitched;
        if (!mainPlayer.healthManager.death)
            mainPlayer.FPSPlayer.SwitchWeapon(WeaponType.Mele);
    }

    public void OnWeaponSwitched(Weapon weapon)
    {
        if (!gameObject.activeInHierarchy) return;
        if (selectionBuildablePanel.gameObject.activeInHierarchy && mainPlayer.FPSPlayer.weaponType != WeaponType.PDA)
        {
            Dismiss();
            return;
        }
        if (buildPanel.gameObject.activeInHierarchy && mainPlayer.FPSPlayer.weaponType != WeaponType.Building)
        {
            engineerBlueprint.Cancel();
            Dismiss();
            return;
        }
        if (movePanel.gameObject.activeInHierarchy && mainPlayer.FPSPlayer.weaponType != WeaponType.Building)
        {
            if (moveEntry == null)
            {
                mainPlayer.FPSPlayer.SwitchWeapon(WeaponType.Building);
                return;
            }
            PosRot posRot = engineerBlueprint.Drop(moveEntry.buildableType);
            if (posRot == null)
            {
                mainPlayer.FPSPlayer.SwitchWeapon(WeaponType.Building);
                return;
            }
            Drop();
        }
    }

    public void SelectBuildableType(BuildableType type)
    {
        if(wrench.exp < engineerBlueprint.blueprints[type].expRequired)
        {
            return;
        }

        if (mainPlayer.FPSPlayer.weaponType != WeaponType.Building && mainPlayer.FPSPlayer.weaponType != WeaponType.PDA)
        {
            previousWeapon = mainPlayer.FPSPlayer.weaponType;
        }
        
        engineerBlueprint.SelectBuild(type);
        SwitchBuildPanel();
    }

    public void Build()
    {
        if (isMoveBuildable) return;
        var entry = engineerBlueprint.Build();
        uiEngineerHUD.uiBuildableHUDItems[entry.buildableType].SetBuildable(entry);
        wrench.UseExp(engineerBlueprint.blueprints[entry.buildableType].expRequired);
        Dismiss();
    }

    public void CancelBuild()
    {
        if (engineerBlueprint.currBuildable != BuildableType.None)
        {
            engineerBlueprint.Cancel();
            SwitchSelectBuildableTypePanel();
        }
    }

    public void Drop()
    {
        if (moveEntry == null) return;
        PosRot posRot = engineerBlueprint.Drop(moveEntry.buildableType);
        if (posRot == null) return;
        
        moveEntry.OnDrop(posRot.positon, posRot.rotation);
        moveEntry = null;
        Dismiss();
        mainPlayer.FPSPlayer.SwitchWeapon(WeaponType.Mele);
    }



    private void SwitchSelectBuildableTypePanel()
    {
        selectionBuildablePanel.gameObject.SetActive(true);
        buildPanel.SetActive(false);
        movePanel.SetActive(false);
        mainPlayer.FPSPlayer.SwitchWeapon(WeaponType.PDA);
    }
    
    private void SwitchBuildPanel()
    {
        selectionBuildablePanel.gameObject.SetActive(false);
        buildPanel.SetActive(true);
        movePanel.SetActive(false);
        mainPlayer.FPSPlayer.SwitchWeapon(WeaponType.Building);
    }
    
    private void SwitchMovePanel()
    {
        selectionBuildablePanel.gameObject.SetActive(false);
        buildPanel.SetActive(false);
        movePanel.SetActive(true);
        mainPlayer.FPSPlayer.SwitchWeapon(WeaponType.Building);
        engineerBlueprint.SelectMove(moveEntry.buildableType);
    }
    
    

    private void OnPlayerDie()
    {
        engineerBlueprint.Cancel();
        if (moveEntry != null)
        {
            engineerBlueprint.DiedOnMoving(moveEntry.buildableType);
            PhotonNetwork.Destroy(moveEntry.gameObject);
        }
        Dismiss();
    }
}
