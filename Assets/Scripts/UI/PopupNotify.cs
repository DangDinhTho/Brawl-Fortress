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
        // args[1] may be a null Sprite (no icon available): hide the icon instead of showing a white box
        Sprite sprite = args.Length > 1 ? args[1] as Sprite : null;
        icon.gameObject.SetActive(sprite != null);
        if (sprite != null) icon.sprite = sprite;
        OnClose = args.Length > 2 ? args[2] as Action : null;
    }

    public void OnCloseClick()
    {
        // Dismiss first: the callback may open another PopupNotify (chained popups), which must not be closed right away
        var cb = OnClose;
        OnClose = null;
        Dismiss();
        cb?.Invoke();
    }
}
