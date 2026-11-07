using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupBase : MonoBehaviour
{
    public bool dontDestroyOnDismiss;
    

    public virtual void OnCreate(params object[] args)
    {
        
    }

    public virtual void OnDismiss()
    {
        
        GUIManager.instan.OnPopupDismiss(this);
    }

    public virtual void Dismiss()
    {
        OnDismiss();
        if (dontDestroyOnDismiss)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
        
    }
}
