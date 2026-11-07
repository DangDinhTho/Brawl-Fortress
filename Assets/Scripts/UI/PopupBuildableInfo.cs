using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupBuildableInfo : PopupBase
{
    private BuildableEntry entry;
    public UIBuildableInfoItem uiBuildableInfoItem;

    public GameObject moveBtn;
    public Text txtName;
    public Text txtLvl;
    
    
    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        if (args.Length > 0)
        {
            entry = (BuildableEntry)args[0];
            entry.OnBuildComplete += OnBuildCompleted;
            entry.OnStartBuild += OnStartBuild;
        }
        uiBuildableInfoItem.SetBuildable(entry);
        txtName.text = LocalizationManager.Localize(entry.buildableType.ToString());
        txtLvl.text = string.Format(LocalizationManager.Localize("level"), entry.level);
        UpdateEntryState();
    }


    public override void OnDismiss()
    {
        base.OnDismiss();
        if (entry == null) return;
        entry.OnBuildComplete -= OnBuildCompleted;
        entry.OnStartBuild -= OnStartBuild;
    }

    private void OnBuildCompleted(int _level)
    {
        UpdateEntryState();
    }

    private void OnStartBuild(float buildTime)
    {
        UpdateEntryState();
    }

    private void UpdateEntryState()
    {
        if (entry.owner.photonView.ViewID == ArenaManager.Instance.mainPlayer.viewID && entry.state == BuildableState.Idle)
        {
            moveBtn.SetActive(true);
        }
        else
        {
            moveBtn.SetActive(false);
        }
    }

    public void OnMoveClick()
    {
        PopupEngineerBuilder popupEngineerBuilder = GUIManager.instan.GetPopupDontDestroy<PopupEngineerBuilder>();
        if (popupEngineerBuilder != null && popupEngineerBuilder.gameObject.activeInHierarchy)
        {
            return;
        }

        if (entry == null || !entry.OnMove()) return;
        GUIManager.instan.CreatePopup<PopupEngineerBuilder>(entry);
        Dismiss();
    }
}
