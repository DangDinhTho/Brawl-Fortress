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
        if (args.Length > 2 && args[2] is Sprite sprite && sprite != null)   // a null icon hides the icon (was a white box)
        {
            icon.gameObject.SetActive(true);
            icon.sprite = sprite;
        }
        else
        {
            icon.gameObject.SetActive(false);
        }
        
        if (args.Length > 3)
        {
            OnCancel = (Action)args[3];
        }
        else OnCancel = null;
        // optional button labels: args[4] confirm, args[5] cancel (default: the localized OK texts)
        confirmLabel = args.Length > 4 ? args[4] as string : null;
        cancelLabel = args.Length > 5 ? args[5] as string : null;
        ApplyLabels();
        if (gameObject.activeInHierarchy) StartCoroutine(ApplyLabelsNextFrame());   // after the labels localize themselves
    }

    string confirmLabel, cancelLabel;

    IEnumerator ApplyLabelsNextFrame() { yield return null; ApplyLabels(); }

    void ApplyLabels()
    {
        SetLabel("ConfirmBtn", confirmLabel ?? LocalizationManager.Localize("confirm_btn"));
        SetLabel("Cancel", cancelLabel ?? LocalizationManager.Localize("cancel_btn"));
    }

    void SetLabel(string buttonName, string text)
    {
        foreach (var b in GetComponentsInChildren<Button>(true))
        {
            if (b.name != buttonName) continue;
            var tmp = b.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp != null) tmp.text = text;
            else { var t = b.GetComponentInChildren<Text>(true); if (t != null) t.text = text; }
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
