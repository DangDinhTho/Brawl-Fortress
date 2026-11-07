using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICharacterSelectButton : MonoBehaviour
{
    public TF2Class tF2Class;

    public PopupLoadout popupLoadout;

    public void OnClickSelect()
    {
        popupLoadout.OpenLoadout(tF2Class);
    }
}
