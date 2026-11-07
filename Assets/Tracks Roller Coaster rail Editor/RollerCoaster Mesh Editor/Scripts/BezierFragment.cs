using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class BezierFragment : MonoBehaviour
{
    // Start is called before the first frame update
    //this is the starting point of the bezier courve
    public Transform pointA;
    //this is the point that changes the shape
    public Transform[] bezierPoint;
    //this is the ending point of the bezier courve
    public Transform pointB;   
    //line renderer for displaying info in editor
    public LineRenderer linR;

    //public readable position and rotation
    public Vector3[] positions;
    public Quaternion[] rotations;

    //left and right renderes
    public LineRenderer lin_L;
    public LineRenderer lin_R;

    //the number of points of the generated linerenderer
    public int nbPoints = 20;

    public void Update()
    {
        if (!Application.IsPlaying(gameObject))
        {
            
            positions = new Vector3[nbPoints];
            rotations = new Quaternion[nbPoints];

            if (pointA != null && pointB != null && bezierPoint != null)
            {

                //set the line renderer
                linR = transform.GetComponent<LineRenderer>();
                linR.positionCount = nbPoints;
                lin_L.positionCount = 2;
                lin_R.positionCount = 2;

                //generate the curve         
                generateBezierFragmentCubic(pointA, bezierPoint[0], bezierPoint[1], pointB);

            }
        }
    }


    public void generateBezierFragmentCubic(Transform A, Transform Z, Transform Z2, Transform B)
    {

        //positions of each point
        Vector3 posA = A.position;
        Vector3 posB = B.position;
        Vector3 posZ = Z.position;
        Vector3 posZ2 = Z2.position;


        //set both points in linerender
        lin_L.SetPosition(0, posA);
        lin_L.SetPosition(1, posZ);

        lin_R.SetPosition(0, posZ2);
        lin_R.SetPosition(1, posB);

        
        //temp variables
        float x = 0;
        float y = 0;
        float z = 0;

        float nx = 0;
        float ny = 0;
        float nz = 0;


        //looping for the line renderer
        for (int ii = 0; ii < linR.positionCount; ii++)
        {
            float t = (float)ii / (float)(linR.positionCount - 1);

            x = (1 - t) * (1 - t)* (1 - t) * posA.x + 3 * (1 - t) * (1 - t) * t * posZ.x+ 3 * (1 - t) * t * t * posZ2.x + t * t*t * posB.x;
            y = (1 - t) * (1 - t) * (1 - t) * posA.y + 3 * (1 - t) * (1 - t) * t * posZ.y + 3 * (1 - t) * t * t * posZ2.y + t * t*t * posB.y;
            z = (1 - t) * (1 - t) * (1 - t) * posA.z + 3 * (1 - t) * (1 - t) * t * posZ.z + 3 * (1 - t) * t * t * posZ2.z + t * t *t* posB.z;

            nx = (-3 * (1 - t) * (1 - t)) * posA.x + 3 * (-2 * (1 - t) * t + (1 - t) * (1 - t)) * posZ.x + 3 * (-t * t + (1 - t) * 2 * t) * posZ2.x +3* t *  t * posB.x;
            ny = (-3 * (1 - t) * (1 - t)) * posA.y + 3 * (-2 * (1 - t) * t + (1 - t) * (1 - t)) * posZ.y + 3 * (-t * t + (1 - t) * 2 * t) * posZ2.y + 3* t * t * posB.y;
            nz = (-3 * (1 - t) * (1 - t)) * posA.z + 3 * (-2 * (1 - t) * t + (1 - t) * (1 - t)) * posZ.z + 3 * (-t * t + (1 - t) * 2 * t) * posZ2.z + 3* t * t * posB.z;

            //spline points positions
            positions[ii] = new Vector3(x, y, z);
            linR.SetPosition(ii, positions[ii]);

            //normal directions and quaternions
            Vector3 normal = new Vector3(nx,ny,nz);
            Quaternion quat = Quaternion.LookRotation(normal, Vector3.up);

            //assign displaced vertices inn local space
            float rotZ = 0;
            if (pointA.localEulerAngles.z >= 0 && pointB.localEulerAngles.z >= 0)
            {
                rotZ = Mathf.Lerp(pointA.localEulerAngles.z, pointB.localEulerAngles.z, t);
            }
            else if (pointA.localEulerAngles.z < 0 && pointB.localEulerAngles.z < 0)
            {
                rotZ = Mathf.Lerp(-pointA.localEulerAngles.z, -pointB.localEulerAngles.z, t);
            }

            
            rotations[ii] = quat*Quaternion.Euler(0,0,rotZ);

        }


    }

}

