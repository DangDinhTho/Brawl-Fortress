using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EquipmentObjectData))]
public class EquipmentObjDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EquipmentObjectData equipmentObjectData = (EquipmentObjectData)target;
        if(GUILayout.Button("Detective Bones Name"))
        {
            equipmentObjectData.DeteciveBoneNames();
        }
    }
}
