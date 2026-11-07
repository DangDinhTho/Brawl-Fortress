using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIEngineerHUD : UIPlayerHUDBase
{
    public UIBuildableHUDItem[] allUIBuildableHUDItems;
    public Dictionary<BuildableType, UIBuildableHUDItem> uiBuildableHUDItems = new Dictionary<BuildableType, UIBuildableHUDItem>();

    private TF2Player mainPlayer;
    private EngineerBlueprint engineerBlueprint;
    public Text txtEngineerExp;
    
    
    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        for (int i = 0; i < allUIBuildableHUDItems.Length; i++)
        {
            uiBuildableHUDItems.Add(allUIBuildableHUDItems[i].buildableType, allUIBuildableHUDItems[i]);
        }

        mainPlayer = ArenaManager.Instance.mainPlayer;
        engineerBlueprint = mainPlayer.remotePlayer.GetComponentInChildren<EngineerBlueprint>();
        Wrench wrench = (Wrench)mainPlayer.FPSPlayer.weaponSlots[WeaponType.Mele];
        wrench.OnExpChange += OnExpChanged;
        OnExpChanged(wrench.exp);
    }

    public void OnExpChanged(int exp)
    {
        txtEngineerExp.text = exp.ToString();
    }

    public void OpenSelectBuildPanelClick()
    {
        PopupEngineerBuilder popupEngineerBuilder = GUIManager.instan.GetPopupDontDestroy<PopupEngineerBuilder>();
        if (popupEngineerBuilder != null && popupEngineerBuilder.gameObject.activeInHierarchy)
        {
            if (popupEngineerBuilder.isMoveBuildable) return;
            popupEngineerBuilder.CancelBuild();
        }
        GUIManager.instan.CreatePopup<PopupEngineerBuilder>();
    }
}
