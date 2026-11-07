using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionPoolingEntry : PoolingEntry
{
    public float liveTime;
    private Coroutine count;
    public override void OnCreated(params object[] args)
    {
        base.OnCreated(args);
        if (count != null) StopCoroutine(count);
        count = StartCoroutine(TurnOff());
    }

    IEnumerator TurnOff()
    {
        yield return new WaitForSeconds(liveTime);
        count = null;
        OnFinish();
    }
}
