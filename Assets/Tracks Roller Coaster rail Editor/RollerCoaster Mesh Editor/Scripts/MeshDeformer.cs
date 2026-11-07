using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class MeshDeformer : MonoBehaviour
{

    //this is the bezier curve data
    public BezierFragment bezier;
    //this is used to update automatically the mesh
    public bool autoUpdate = true;
    //prefab gameObject 
    public GameObject prefabGo;
    //tre shared mesh of the prefab
    Mesh deformingMesh;
    //original vertices of the mesh
    Vector3[] originalVertices;
    
        
    public void Update()
    {
        //only if is enabled
        if (autoUpdate)
        {
            if (!Application.IsPlaying(gameObject))
            {
                updateMesh();
            }
            
        }
    }


    public void updateMesh()
    {
       
        //only if the prefab is not null as well as the bezier script
        if (prefabGo != null && bezier != null)
        {
           
            //mesh deformartion update
            //mesh prefab information
            deformingMesh = transform.GetComponent<MeshFilter>().sharedMesh;
            originalVertices = prefabGo.transform.GetComponent<MeshFilter>().sharedMesh.vertices;

            //new displacements
            Vector3[] displacedVertices = new Vector3[originalVertices.Length];
            float max_X = -10000000000;
            float min_X = 100000000000000;

            //get the maximum value of the "X" value for the original vertices
            for (int i = 0; i < originalVertices.Length; i++)
            {
                max_X = Mathf.Max(max_X, originalVertices[i].x);
                min_X = Mathf.Min(min_X, originalVertices[i].x);

            }

            //loop through vertices
            for (int i = 0; i < originalVertices.Length; i++)
            {
                // we map the value between 0 and 1 --> parametric value of zero line
                float x = (originalVertices[i].x - min_X) / (max_X - min_X);

                //bezier curve
                float xmodif=0, ymodif=0, zmodif=0;
                float xnorm = 0, ynorm = 0, znorm = 0;

                    //bezier curve         
                    xmodif = (1 - x) * (1 - x) * (1 - x) * bezier.pointA.position.x + 3 * (1 - x) * (1 - x) * x * bezier.bezierPoint[0].position.x + 3 * (1 - x) * x * x * bezier.bezierPoint[1].position.x + x * x*x * bezier.pointB.position.x;
                    ymodif = (1 - x) * (1 - x) * (1 - x) * bezier.pointA.position.y + 3 * (1 - x) * (1 - x) * x * bezier.bezierPoint[0].position.y + 3 * (1 - x) * x * x * bezier.bezierPoint[1].position.y + x * x*x * bezier.pointB.position.y;
                    zmodif = (1 - x) * (1 - x) * (1 - x) * bezier.pointA.position.z + 3 * (1 - x) * (1 - x) * x * bezier.bezierPoint[0].position.z + 3 * (1 - x) * x * x * bezier.bezierPoint[1].position.z + x * x*x * bezier.pointB.position.z;

                
                    //derivate:                    
                    xnorm = (-3 * (1 - x) * (1 - x)) * bezier.pointA.position.x + 3 *(-2 * (1 - x) * x + (1 - x) * (1 - x)) * bezier.bezierPoint[0].position.x + 3* (-x * x + (1 - x) * 2 * x) * bezier.bezierPoint[1].position.x + (3*x*x)* bezier.pointB.position.x;
                    ynorm = (-3 * (1 - x) * (1 - x)) * bezier.pointA.position.y + 3 * (-2 * (1 - x) * x + (1 - x) * (1 - x)) * bezier.bezierPoint[0].position.y + 3 * (-x * x + (1 - x) * 2 * x) * bezier.bezierPoint[1].position.y + (3 * x * x) * bezier.pointB.position.y;
                    znorm = (-3 * (1 - x) * (1 - x)) * bezier.pointA.position.z + 3 * (-2 * (1 - x) * x + (1 - x) * (1 - x)) * bezier.bezierPoint[0].position.z + 3 * (-x * x + (1 - x) * 2 * x) * bezier.bezierPoint[1].position.z + (3 * x * x) * bezier.pointB.position.z;
                    
           

                // world cordinates of the mean line
                Vector3 worldMeshCoord_of_meanLine = new Vector3(xmodif, ymodif, zmodif);
                // local cordinates of the mean line
                Vector3 localMeshCoord_of_meanLine = transform.InverseTransformPoint( new Vector3(xmodif, ymodif, zmodif)); 
             
                //normal directions and quaternions
                Vector3 normal = new Vector3(xnorm, ynorm, znorm);  
                Quaternion quat = Quaternion.LookRotation(normal,Vector3.up);
                
                //displaced vertices in local and world corrdinates
                Vector3 displacedVertices_local = localMeshCoord_of_meanLine + new Vector3(0, originalVertices[i].y, -originalVertices[i].z); 
                Vector3 displacedVertices_world = transform.TransformPoint(displacedVertices_local);

                // inverse rotation to obtain the correct quaternion
                quat = Quaternion.Inverse(quat);

                //assign displaced vertices inn local space
                float rotZ = 0;
                if(bezier.pointA.localEulerAngles.z>=0 && bezier.pointB.localEulerAngles.z>=0)
                {
                    rotZ = Mathf.Lerp(bezier.pointA.localEulerAngles.z, bezier.pointB.localEulerAngles.z, x);
                }
                else if (bezier.pointA.localEulerAngles.z < 0 && bezier.pointB.localEulerAngles.z < 0)
                {
                    rotZ = Mathf.Lerp(-bezier.pointA.localEulerAngles.z, -bezier.pointB.localEulerAngles.z, x);
                }
               

                displacedVertices[i] = transform.InverseTransformPoint(RotatePointAroundPivot(displacedVertices_world, worldMeshCoord_of_meanLine, Quaternion.Euler(180- rotZ, 90-quat.eulerAngles.y, -quat.eulerAngles.x)));
                
            }

            //update mesh
            Mesh finalMesh = new Mesh();
            
            finalMesh.vertices = displacedVertices;
            finalMesh.triangles = deformingMesh.triangles;
            finalMesh.RecalculateBounds();
            finalMesh.RecalculateNormals();
            finalMesh.RecalculateTangents();
            
            transform.GetComponent<MeshFilter>();
            transform.GetComponent<MeshFilter>().mesh = finalMesh;
        }



    }

    //rotates a vector3 arround a pivot a quaternion value
    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion quat)
    {
        return quat * (point - pivot) + pivot;
    }



}




