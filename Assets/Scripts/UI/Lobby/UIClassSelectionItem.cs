using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIClassSelectionItem : MonoBehaviour
{
    public Toggle toggleSelect;
    public TF2Class tF2Class;
    public Image selectedImg;
    public Sprite[] selectedSprite;
    private bool readySelect;
    public PopupSelectClass popupSelectClass;
    public AudioClip effectSound;

    private void OnEnable()
    {
        readySelect = false;
    }

    public void Check()
    {
        selectedImg.sprite = selectedSprite[(int)HomeManager.instance.team];
        if (tF2Class == HomeManager.instance.characterPref.tF2Class)
        {
            Select(true);
        }
        readySelect = true;
    }

    public void Select(bool select)
    {
        //toggleSelect.SetIsOnWithoutNotify(select);
        toggleSelect.isOn = select;
        HomeManager.instance.CreateCharacter();
        
    }

    public void OnSeleted()
    {
        if (!readySelect) return;
        if (toggleSelect.isOn) 
        {
            HomeManager.instance.SetCharacterClass(tF2Class);
            popupSelectClass.OnSelectClass(tF2Class);
            HomeManager.instance.audioSource.PlayOneShot(effectSound);
        }
        
    }
}
