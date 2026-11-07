using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPDAButton : MonoBehaviour
{
    private WeaponId pdaType;

    public void InitData(WeaponId pda)
    {
        this.pdaType = pda;
        gameObject.SetActive(true);
    }

    public void OnClickThis()
    {
        switch (pdaType)
        {
            case WeaponId.BuilderPDA:
                //GUIManager.instan.CreatePopup<PopupEngineerBuilder>(false);
                break;
        }
    }
}
