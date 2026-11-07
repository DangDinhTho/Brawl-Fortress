using System.Collections;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorkShopJsonHelper))]
public class WorkShopHelperEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var component = (WorkShopJsonHelper) target;

        if (GUILayout.Button("Download"))
        {
            component.DownloadCSV();
        }
    }
}
