using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupCustomUI : PopupBase
{
    private CustomUIItem[] customUIItems;
    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        customUIItems = UICharacterInputController.instan.customUIItems;
        for (int i = 0; i < customUIItems.Length; i++)
        {
            customUIItems[i].Custom();
        }
    }

    public void SaveClick()
    {
        for (int i = 0; i < customUIItems.Length; i++)
        {
            customUIItems[i].Save();
        }
        Dismiss();
    }
    
    public void CancelClick()
    {
        for (int i = 0; i < customUIItems.Length; i++)
        {
            customUIItems[i].Cancel();
        }
        Dismiss();
    }
}
