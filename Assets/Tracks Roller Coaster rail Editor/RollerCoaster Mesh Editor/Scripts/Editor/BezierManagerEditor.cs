using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BezierManager))]
public class BezierManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {

        //initialize process
        base.OnInspectorGUI();
        BezierManager manager = (BezierManager) target;

        
        if (GUILayout.Button("Add Point"))
        {
            manager.destroyMesh();
            manager.addPoint();
        }



        if (GUILayout.Button("Delete Point"))
        {
            manager.destroyMesh();
            manager.deletePoint();
        }

        if (GUILayout.Button(" APPLY MESH "))
        {

            manager.applymesh();
        }

        if (GUILayout.Button(" REMOVE MESH "))
        {

            manager.destroyMesh();
        }

        if (GUILayout.Button(" UNACTIVE LINE "))
        {

            manager.UnactiveLine();
        }

        if (GUILayout.Button(" ACTIVE LINE "))
        {

            manager.ActiveLine();
        }


    }




}