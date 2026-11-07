using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignBones : MonoBehaviour
{
    public Transform m_bone;
    //[SerializeField]
    Transform[] bones;
    Transform[] ownBones;
    
   // public SkinnedMeshRenderer mesh;
    public bool needTurnOff;
    public List<string> ignorBones = new List<string>() { "Shotpoint", "ShotPoint" };

    Dictionary<Transform, Transform> equalBones = new Dictionary<Transform, Transform>();
    // Start is called before the first frame update

    //private void Awake()
    //{
    //    if (mesh != null)
    //        reds = mesh.materials;
    //}

    //public void SetTeam(int team)
    //{
    //    if (mesh == null) return;
    //    if (team == 1 && mesh != null && blues != null && blues.Length > 0)
    //    {
    //        mesh.materials = blues;
    //    }
    //    else if (team == 0 && mesh != null && reds != null && reds.Length > 0)
    //    {
    //        mesh.materials = reds;
    //    }
    //}

    public void Setup(Transform copyFromRootBone)
    {
        //SetTeam(team);

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
                //ownBonesHasControl.Add(t);
                t.parent = found;
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.Euler(0, 0, 0);
                t.localScale = found.transform.localScale;
            }
        }
        //gameObject.SetActive(true);

    }
    void Start()
    {

    }

    Transform FindInArray(string name)
    {
        foreach (Transform t in bones)
        {
            if (ignorBones.Contains(name)) continue;
            if (t.gameObject.name == name)
                return t;
        }
        return null;
    }

    // Update is called once per frame
    //void LateUpdate()
    //{
    //    //if (character != null && character.tPSLookAt.enabled && !character.death) return;

    //    FollowBone();
    //}

    //public void FollowBone()
    //{
    //    foreach (Transform t in ownBonesHasControl)
    //    {
    //        t.position = equalBones[t].position;
    //        t.rotation = equalBones[t].rotation;
    //    }
    //}

    private void OnDestroy()
    {
        foreach (var bone in ownBones)
        {
            Destroy(bone.gameObject);
        }
    }
}
