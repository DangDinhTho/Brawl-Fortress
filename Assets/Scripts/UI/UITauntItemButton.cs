using Assets.SimpleLocalization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITauntItemButton : MonoBehaviour
{
    public Image icon;
    public Text txtTauntName;
    private Taunt taunt;
    private TauntConfig config;
    private PopupTaunts popupTaunts;

    public void Init(UserItem _taunt, PopupTaunts _popupTaunts)
    {
        this.taunt = ConfigManager.GetTauntFromItemId(_taunt.itemId);
        this.popupTaunts = _popupTaunts;
        config = ConfigManager.GetTauntConfig(taunt);
        icon.sprite = GUIManager.instan.GetItemIcon(_taunt.itemId);
        txtTauntName.text = _taunt.itemInstance.DisplayName;
    }

    public void ClickTauntInMatch()
    {
        ArenaManager.Instance.mainPlayer.FPSPlayer.PlayTaunt(taunt);
        if (popupTaunts)
        {
            popupTaunts.Dismiss();
        }
    }
    
}
