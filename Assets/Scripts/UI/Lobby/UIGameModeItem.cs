using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGameModeItem : MonoBehaviour
{
    public GameMode gameMode;
    public GameObject hideObj;
    public PopupSelectMap popupSelectMap;
    public Map[] maps;
    public Text gameModeName;
    public Text gameModeNameDes;

    private void Start()
    {
        gameModeName.text = LocalizationManager.Localize(gameMode.ToString());
        gameModeNameDes.text = LocalizationManager.Localize($"{gameMode}_des");
    }

    public void SetSelect(bool select)
    {
        hideObj.SetActive(!select);
    }
    
    public void OnSelectClick()
    {
        if (gameMode == GameMode.MannVsMachine)
        {
            if (PhotonNetwork.CurrentRoom != null &&
                    PhotonNetwork.CurrentRoom.MaxPlayers > ConfigManager.maxPlayerPerRoom / 2)
            {
                GUIManager.instan.CreatePopup<PopupNotify>(
                     string.Format(LocalizationManager.Localize("select_mvm_fail_out_of_player"), ConfigManager.maxPlayerPerRoom / 2));
                return;
            }
        }
        popupSelectMap.OnGameModeUpdate(gameMode);
    }
}
