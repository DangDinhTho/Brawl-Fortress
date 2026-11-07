using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshDeformer))]
public class MeshDeformerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        

        //initialize process
        base.OnInspectorGUI();
        MeshDeformer manager = (MeshDeformer)target;



        if (GUILayout.Button("UpdateMesh"))
        {
            manager.updateMesh();
        }

    }




}