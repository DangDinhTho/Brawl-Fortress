using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Default MFPS pooling system
/// If you want to use your custom pool system, simply inherited your script from bl_ObjectPoolingBase.cs
/// And make sure you served the default prefabs from the default pool system with the same key/identifiers.
/// </summary>
public class ObjectPooling : ObjectPoolingBase
{
    [SerializeField, FormerlySerializedAs("RegistreOnStart")]
    private PreRegister[] pooledPrefabs;

    private Dictionary<string, PoolObject> pools = new Dictionary<string, PoolObject>();

    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        for (int i = 0; i < pooledPrefabs.Length; i++)
        {
            RegisterObject(pooledPrefabs[i].Name, pooledPrefabs[i].Prefab, pooledPrefabs[i].Lenght);
        }
    }

    /// <summary>
    /// Add a new pooled prefab
    /// </summary>
    /// <param name="poolName">Identifier of this pool</param>
    /// <param name="prefab"></param>
    /// <param name="count"></param>
    public void RegisterObject(string poolName, GameObject prefab, int count)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Can't pooled the prefab for: " + poolName + " because the prefab has not been assigned.");
            return;
        }

        var p = new PoolObject();
        p.Name = poolName;
        p.Prefab = prefab;
        GameObject g;
        p.PoolList = new PoolingEntry[count];

        for (int i = 0; i < count; i++)
        {
            g = Instantiate(prefab, transform) as GameObject;
            p.PoolList[i] = g.GetComponent<PoolingEntry>();
            g.SetActive(false);
        }
        pools.Add(poolName, p);
    }

    /// <summary>
    /// Instantiate a pooled prefab
    /// use this instead of GameObject.Instantiate(...)
    /// </summary>
    /// <returns></returns>
    public override PoolingEntry Instantiate(string objectName, Vector3 position, Quaternion rotation, params object[] args)
    {
        PoolObject pool = pools[objectName];
        if (pool != null)
        {
            PoolingEntry pe = pool.GetCurrent();
            if (pe == null)//in case a pool object get destroyed, replace it 
            {
                var g = Instantiate(pool.Prefab, transform);
                pool.ReplaceCurrent(g.GetComponent<PoolingEntry>());
            }
            pe.transform.SetPositionAndRotation(position, rotation);
            //g.SetActive(true);
            pe.OnCreated(args);
            pool.SetNext();
            return pe;
        }
        else
        {
            Debug.LogError(string.Format("Object {0} has not been register for pooling.", objectName));
            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class PoolObject
    {
        public string Name;
        public GameObject Prefab;
        public PoolingEntry[] PoolList;
        public int CurrentPool;

        public PoolingEntry GetCurrent() 
        {
            //PoolingEntry poolingEntry = null;
            //int count = 0;
            //while (count >= PoolList.Length)
            //{
            //    poolingEntry = PoolList[CurrentPool];
            //    if (!poolingEntry.ready) SetNext();
            //    else return poolingEntry.gameObject;
            //    count++;
            //}
            return PoolList[CurrentPool];


        }
        public void SetNext() { CurrentPool = (CurrentPool + 1) % PoolList.Length; }
        public void ReplaceCurrent(PoolingEntry g) => PoolList[CurrentPool] = g;
    }


    [Serializable]
    public class PreRegister
    {
        public string Name;
        public GameObject Prefab;
        public int Lenght = 10;
    }
}