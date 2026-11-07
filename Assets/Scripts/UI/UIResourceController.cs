using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIResourceController : MonoBehaviour
{
    private bool resourceShowedPrevious;
    public bool forceHideResource;

    private void OnEnable()
    {
        
        resourceShowedPrevious = ResourceManager.instan.visible;
        if (!resourceShowedPrevious)
        {
            ResourceManager.instan.ShowResource();
        }
        else if (forceHideResource)
        {
            ResourceManager.instan.HideResource();
        }

    }

    private void OnDisable()
    {
        if (!resourceShowedPrevious)
        {
            ResourceManager.instan.HideResource();
        }
        else if (forceHideResource)
        {
            ResourceManager.instan.ShowResource();
        }
    }
}
