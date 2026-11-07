using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIShortCutButton : MonoBehaviour
{
    public PopupType type;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickBtn);
    }

    public void OnClickBtn()
    {
        switch (type)
        {
            case PopupType.PopupScoreBoard:
                GUIManager.instan.CreatePopup<PopupScoreBoard>();
                break;
            default:
                GUIManager.instan.CreatePopup(type.ToString());
                break;
               
        }
    }
}

public enum PopupType
{
    None,
    PopupScoreBoard,
    PopupLoadout,
    PopupStore,
    PopupCrafting,
    PopupWorkShop,
    PopupLeaderboard,
    PopupTaunts,
    PopupSetting
}
