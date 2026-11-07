using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AlignBones))]
public class EquipmentItem : MonoBehaviour
{
    public AlignBones alignBones;

    private void OnValidate()
    {
        this.alignBones = GetComponent<AlignBones>();
    }

    public void Init(Transform bone, Team team)
    {
        alignBones.Setup(bone);
        
        MeshDynamicColor[] meshDynamicColors = GetComponentsInChildren<MeshDynamicColor>(true);
        if (meshDynamicColors != null)
        {
            for (int i = 0; i < meshDynamicColors.Length; i++)
            {
                meshDynamicColors[i].SetTeamColor(team);
            }
        }
    }
}
