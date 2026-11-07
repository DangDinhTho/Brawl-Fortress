using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager instan;
    public TMP_Text txtGoldKey;
    public TMP_Text txtMMetal;
    public TMP_Text txtCMetal;
    [HideInInspector]
    public bool visible;


    public void Init()
    {
        instan = this;
        HideResource();
    }

    private void OnEnable()
    {
        GameManager.instan.userData.currency.OnValueChange += UpdateValue;
        UpdateValue();
    }

    private void OnDisable()
    {
        GameManager.instan.userData.currency.OnValueChange -= UpdateValue;
    }


    public void UpdateValue()
    {
        txtGoldKey.text = GameManager.instan.userData.GetUserCurrency(TF2Currency.GK).ToString();
        txtMMetal.text = GameManager.instan.userData.GetUserCurrency(TF2Currency.MM).ToString();
        txtCMetal.text = GameManager.instan.userData.GetUserCurrency(TF2Currency.CM).ToString();
    }

    public void ShowResource()
    {
        visible = true;
        gameObject.SetActive(true);
    }

    public void HideResource()
    {
        visible = false;
        gameObject.SetActive(false);
    }

    public void OnResourceClick()
    {
        PopupStore popupStore = GUIManager.instan.GetPopupDontDestroy<PopupStore>();
        if(popupStore != null && popupStore.gameObject.activeInHierarchy) return;
        GUIManager.instan.CreatePopup<PopupStore>();
    }
}
