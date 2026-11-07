using DG.Tweening;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PopupSetting : PopupBase
{
    public Slider graphicSlider;
    private int graphicValue;
    private float sensivityValue;
    public Dropdown languageDropdown;
    public Slider sensivitySlider;
    public GameObject customUIButton;
    public Toggle fps30Toggle;
    public Toggle fps60Toggle;
    private int fpsTarget;

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);

        if (args.Length > 0)
        {
            bool onMatch = (bool)args[0];
            customUIButton.SetActive(onMatch);
        }
        else
        {
            customUIButton.SetActive(false);
        }


        graphicValue = SettingHelper.GetGraphicValue();
        graphicSlider.value = graphicValue;

        sensivityValue = SettingHelper.GetSensivityValue();
        sensivitySlider.value = sensivityValue;
        //soundSlider.value = SettingHelper.GetSoundValue();
        //txtSoundValue.text = soundSlider.value.ToString();

        graphicSlider.onValueChanged.AddListener((value) => { graphicValue = (int)value; });

        sensivitySlider.onValueChanged.AddListener((value) => { sensivityValue = value; });

        fpsTarget = SettingHelper.GetFPSValue();
        fps30Toggle.SetIsOnWithoutNotify(fpsTarget == 30);
        fps60Toggle.SetIsOnWithoutNotify(fpsTarget == 60);
        fps30Toggle.onValueChanged.AddListener(OnSelect30FPS);
        fps60Toggle.onValueChanged.AddListener(OnSelect60FPS);

        List<string> languages = new List<string>()
        {
            "English",
            "Russian",
            "Spanish",
            "Vietnamese",
            "Thai"
        };

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(languages);
        languageDropdown.SetValueWithoutNotify(languages.FindIndex(e => e.Equals(SettingHelper.GetLanguage())));
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
    }


    public void OnLanguageChanged(int value)
    {
        string language = "";
        switch (value)
        {
            case 0:
                language = "English";
                break;
            case 1:
                language = "Russian";
                break;
            case 2:
                language = "Spanish";
                break;
            case 3:
                language = "Vietnamese";
                break;
            case 4:
                language = "Thai";
                break;
            
        }

        LocalizationManager.Language = language;
        SettingHelper.SetLanguage(language);
    }

    public void OnCustomUIClick()
    {
        GUIManager.instan.CreatePopup<PopupCustomUI>();
        Dismiss();
    }


    public void Save()
    {
        int quanlityLevel = graphicValue;
        if (graphicValue == 4) quanlityLevel = 5;
        QualitySettings.SetQualityLevel(quanlityLevel);
        SettingHelper.SetGraphic(graphicValue);

        SettingHelper.SetSensivity(sensivityValue);
        SettingHelper.SetFpsTarget(fpsTarget);
        Dismiss();
    }

    public void OnSelect30FPS(bool select)
    {
        if (select)
        {
            //SettingHelper.SetFpsTarget(30);
            fpsTarget = 30;
        }
    }
    
    public void OnSelect60FPS(bool select)
    {
        if (select)
        {
            //SettingHelper.SetFpsTarget(60);
            fpsTarget = 60;
        }
    }

    public void OnRestoreClick()
    {
        Restore();
    }
    private PopupWaitingFocus popupLoading;
    private void Restore()
    {
        if (popupLoading != null) popupLoading.Dismiss();
        popupLoading = GUIManager.instan.CreatePopup<PopupWaitingFocus>();
        //GameManager.instan.RestorePurchase(ItemId.remove_ads, OnRestoreSuccess);
        IAPManager.instan.RestorePurchasesIOS(OnRestoreSuccess);
    }

    private void OnRestoreSuccess()
    {
        if (popupLoading != null) popupLoading.Dismiss();
        GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("restore_success"));
        if (HomeManager.instance != null)
        {
            HomeManager.instance.SetNoAdsBtn();
        }
    }

    public void OnRemoveAcountClick()
    {
        GUIManager.instan.CreatePopup<PopupConfirm>(LocalizationManager.Localize("remove_account_confirm"), (Action)ConfirmRemoveAcount);
    }

    private void ConfirmRemoveAcount()
    {
        if (popupLoading != null) popupLoading.Dismiss();
        popupLoading = GUIManager.instan.CreatePopup<PopupWaitingFocus>();
        GameManager.instan.RemoveAcount(LogOut);
    }

    public void OnLogOutClick()
    {
        GUIManager.instan.CreatePopup<PopupConfirm>(LocalizationManager.Localize("logout_confirm"), (Action)LogOutRequest);
    }

    public void LogOutRequest()
    {
        if (popupLoading != null) popupLoading.Dismiss();
        popupLoading = GUIManager.instan.CreatePopup<PopupWaitingFocus>();
        LogOut();
    }

    private void LogOut()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        GameManager.instan.userData = new UserData();
        PlayerPrefs.DeleteAll();
        DOVirtual.DelayedCall(0.2f, () =>
        {
            //if (popupLoading != null) popupLoading.Dismiss();
            SceneManager.LoadScene("FlashScene");
        });
    }
}
