using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupConfirm : PopupBase
{
    public Action OnConfirm;
    public Action OnCancel;

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
            OnConfirm = (Action)args[1];
        }
        if (args.Length > 2)
        {
            icon.gameObject.SetActive(true);
            icon.sprite = (Sprite)args[2];
        }
        else
        {
            icon.gameObject.SetActive(false);
        }
        
        if (args.Length > 3)
        {
            OnCancel = (Action)args[3];
        }
    }

    public void OnConfirmClick()
    {
        OnConfirm?.Invoke();
        Dismiss();
    }

    public void OnCancelClick()
    {
        OnCancel?.Invoke();
        Dismiss();
    }
}
