using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupNotify : PopupBase
{
    public Action OnClose;

    public Text txtContent;
    public Image icon;

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        if (args.Length > 0)
        {
            txtContent.text = (string)args[0];
        }
        if (args.Length > 1)
        {
            icon.gameObject.SetActive(true);
            icon.sprite = (Sprite)args[1];
        }
        else
        {
            icon.gameObject.SetActive(false);
        }
        if (args.Length > 2)
        {
            OnClose = (Action)args[2];
        }
    }

    public void OnCloseClick()
    {
        OnClose?.Invoke();
        Dismiss();
    }
}
