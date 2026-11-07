using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectPoolingBase : MonoBehaviour
{
    /// <summary>
    /// Get a pooled prefab instance.
    /// </summary>
    /// <param name="prefabName"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    public abstract PoolingEntry Instantiate(string prefabName, Vector3 position, Quaternion rotation, params object[] args);

    /// <summary>
    /// 
    /// </summary>
    private static ObjectPoolingBase _instance;
    public static ObjectPoolingBase Instance
    {
        get
        {
            if (_instance == null) { _instance = FindObjectOfType<ObjectPoolingBase>(); }
            return _instance;
        }
    }
}
