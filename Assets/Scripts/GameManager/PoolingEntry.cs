using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolingEntry : MonoBehaviour
{
    [HideInInspector]
    public bool ready;

    public virtual void OnCreated(params object[] args)
    {
        gameObject.SetActive(true);
        ready = true;
    }

    public virtual void OnFinish()
    {
        ready = false;
        gameObject.SetActive(false);
    }
}
