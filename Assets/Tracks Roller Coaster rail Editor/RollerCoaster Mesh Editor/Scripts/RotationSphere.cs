using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum typeOfBeier {left, right };

[ExecuteInEditMode]
public class RotationSphere : MonoBehaviour
{
    // Start is called before the first frame update

    // the bezier point
    public Transform origin;
    //the linerenderer
    LineRenderer linR;

    //the angle of rotation to retrieve
    public float angle;

    //the bezier script of the fragment
    public BezierFragment bzS;
    public typeOfBeier type;

    public float radii = 0.8f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        //obtain direction
        Vector3 dir=new Vector3();
        Vector3 final = new Vector3();
        if(type==typeOfBeier.left)
        {
            dir =bzS.bezierPoint[0].position-bzS.pointA.position;
            final = bzS.bezierPoint[0].position;
        }

        else if (type == typeOfBeier.right)
        {
            dir = -bzS.bezierPoint[1].position + bzS.pointB.position;
            final = bzS.bezierPoint[1].position;
        }

        //get quaternion facing that direction
        Quaternion quat=Quaternion.LookRotation(dir);
        transform.rotation = quat;
        

        // impose position using the angle use angle to rotate ball in that direction
        if(angle<0)
        {
            angle += 360;
        }
        if(angle>180)
        {
            angle -= 360;
        }

        transform.position = final+ radii*Mathf.Cos(angle*Mathf.PI/180)* transform.right+ radii * Mathf.Sin(angle * Mathf.PI / 180) * transform.up;

        //the user direction
        Vector3 userdir = transform.position - origin.position;

        //set linerenderer points
        linR = gameObject.GetComponent<LineRenderer>();
        linR.SetPosition(0, origin.position);
        linR.SetPosition(1, transform.position);

        //change rotation of marker point
        if (type == typeOfBeier.left)
        {
            bzS.pointA.rotation = Quaternion.LookRotation(dir, Vector3.Cross(dir, userdir));
        }
        else if (type == typeOfBeier.right)
        {
            bzS.pointB.rotation = Quaternion.LookRotation(dir, Vector3.Cross(dir,userdir));
        }
    }



}
