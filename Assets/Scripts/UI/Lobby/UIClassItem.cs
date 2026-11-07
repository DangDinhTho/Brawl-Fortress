using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIClassItem : MonoBehaviour
{
    public TF2Class tF2Class;
    public PopupSelectClass popupSelectClass;
    public TMP_Text txtPlayCount;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        txtPlayCount.text = PlayerPrefs.GetInt($"ClassPlayCount_{tF2Class}", 0).ToString();
    }

    public void OnSelectClick()
    {
        HomeManager.instance.SetCharacterClass(tF2Class);
        popupSelectClass.OnSelectClass(tF2Class);
        //HomeManager.instance.audioSource.PlayOneShot(effectSound);
    }
}
