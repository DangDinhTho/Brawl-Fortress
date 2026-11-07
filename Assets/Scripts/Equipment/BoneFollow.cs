using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneFollow : MonoBehaviour
{
    public Transform m_bone;
    public Transform copyFromRootBone;
    //[SerializeField]
    Transform[] bones;
    Transform[] ownBones;
    Dictionary<Transform, Transform> equalBones = new Dictionary<Transform, Transform>();

    private void Awake()
    {
        bones = copyFromRootBone.GetComponentsInChildren<Transform>();
        ownBones = m_bone.transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in ownBones)
        {
            Transform found = FindInArray(t.gameObject.name);
            if (found)
            {
                //equalBones.Add(t, found);
                //t.transform.position = found.position;
                //t.transform.rotation = found.rotation;
                t.parent = found;
                t.transform.localPosition = Vector3.zero;
                t.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    Transform FindInArray(string name)
    {
        foreach (Transform t in bones)
        {
            if (t.gameObject.name == name)
                return t;
        }
        return null;
    }

    void LateUpdate()
    {
       
        //FollowBone();
    }

    public void FollowBone()
    {
        foreach (Transform t in ownBones)
        {
            if (equalBones.ContainsKey(t))
            {
                t.position = equalBones[t].position;
                t.rotation = equalBones[t].rotation;
            }
        }
    }
}
