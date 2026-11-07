using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupLoading : PopupBase
{
    public Text txtTitle;
    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        if(args.Length > 0)
        {
            txtTitle.text = (string)args[0];
        }
        else
        {
            txtTitle.text = LocalizationManager.Localize("loading");
        }
    }
}
