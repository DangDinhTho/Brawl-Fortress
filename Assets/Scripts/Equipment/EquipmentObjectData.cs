using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentObjectData : MonoBehaviour
{
    public GameObject referance;
    public List<string> boneNames = new List<string>();
    public void DeteciveBoneNames()
    {
        boneNames.Clear();
        if (referance == null)
            return;
        if (!referance.GetComponent<SkinnedMeshRenderer>())
            return;

        var renderer = referance.GetComponent<SkinnedMeshRenderer>();
        var bones = renderer.bones;

        foreach (var t in bones)
        {
            boneNames.Add(t.name);
        }
    }
}
