using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitcanFX : PoolingEntry
{
    public float timeLive;
    public override void OnCreated(params object[] args)
    {
        base.OnCreated(args);
        StartCoroutine(WaitDestroy());
    }

    IEnumerator WaitDestroy()
    {
        yield return new WaitForSeconds(timeLive);
        OnFinish();
    }
}
