using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupWaitingFocus : PopupBase
{
    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            Dismiss();
        }
    }
}
