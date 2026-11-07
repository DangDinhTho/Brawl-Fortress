using System.Collections;

using UnityEngine;
using System.Collections;

public class CoasterfollowerAdv : MonoBehaviour {

    //TIME EVOLVING PARAMETER
    public float elapsed;
    
    //the object that moves
    public Transform seatObject;
    
    //starting at a given curve
    public int startingPositon;

    //distance to next point to calculate sound effect
    float distanceToNextPoint;

    //indices
    int indexCurrent, indexNext;
    
    //the fragments
    public GameObject[] fragments;
    
    //the bezier curves
    BezierFragment[] bzS;

    //public movement
    public bool canMove=true;
    public float speedFactor=0.1f;
    public float maxHeight=20;
    public int numberOfRails;
    public int numberOfPoints;
    public float powFactor;

    //where the positions and rotations are stored
    public Quaternion[] rotationss;
    public Vector3[] positions;
   


    void Start()
    {

        //get the rails
        //fragments = GameObject.FindGameObjectsWithTag("fragment");

        numberOfRails = fragments.Length;

        float minDistance = float.MaxValue;

        //get the bezier fragments
        bzS = new BezierFragment[numberOfRails];
        // lets find all the rails vertex (geometry)
        for (int jj = 0; jj < numberOfRails; jj++)
        {
            bzS[jj] = fragments[jj].GetComponent<BezierFragment>();

        }

        //calculate the number of points
        numberOfPoints = bzS[0].nbPoints * numberOfRails;

        positions = new Vector3[numberOfPoints];
        rotationss = new Quaternion[numberOfPoints];

        //set positions and rotations
        int indx = 0;
        for (int kk = 0; kk < numberOfRails; kk++)
        {
            for (int ii = 0; ii < bzS[0].nbPoints; ii++)
            {
                rotationss[indx] = bzS[kk].rotations[ii];
                positions[indx] = bzS[kk].positions[ii];

                float distance = Vector3.Distance(seatObject.position, positions[indx]);
                if(distance < minDistance)
                {
                    minDistance = distance;
                    startingPositon = indx;
                }

                indx += 1;

            }
        }


        //initial positionning
        seatObject.position = positions[startingPositon];
        seatObject.rotation = rotationss[startingPositon];

        //set the evolving parameters
        indexCurrent = startingPositon;
        indexNext = startingPositon + 1;
        distanceToNextPoint = (positions[indexNext] - positions[indexCurrent]).magnitude;

    }

    public void SetNew()
    {
        float minDistan = float.MaxValue;
        int index = 0;

        for(int i = 0; i < positions.Length; i++)
        {
            if(Vector3.Distance(positions[i], seatObject.position) < minDistan)
            {
                minDistan = Vector3.Distance(positions[i], seatObject.position);
                index = i;
            }
        }

        ////initial positionning
        //seatObject.position = positions[index];
        //seatObject.rotation = rotationss[index];

        //set the evolving parameters
        indexCurrent = index;
        indexNext = index + 1;
        distanceToNextPoint = (positions[indexNext] - positions[indexCurrent]).magnitude;
        canMove = true;
    }


    // Update is called once per frame
    void FixedUpdate()
    {

        if (canMove == true)
        {
            //increase evolving parameter
            elapsed += speedFactor * Mathf.Pow(maxHeight - seatObject.position.y, powFactor) / distanceToNextPoint;

            // contition to change of indices
            //float distanceToNextPoint= (railPoints[indexNext]-seatObject.position).magnitude;
            if (elapsed >= 1)
            {
                indexCurrent += 1;
                //check looping

                if (indexCurrent == numberOfPoints - 1)
                {
                    indexNext = 0;
                }
                else
                {
                    indexNext = indexCurrent + 1;
                }

                if(indexCurrent==numberOfPoints-1)
                {
                    indexCurrent = 0;
                }

                //obtain elapsed for lerp using KINETIC ENERGY
                elapsed = 0;

                //error check
                if (elapsed>1e8)
                {
                    elapsed = 0.001f;
                }
                //recalculate distance to travel to next point
                distanceToNextPoint = (positions[indexNext] - positions[indexCurrent]).magnitude;

            }


            // movement towars new point
            seatObject.position = Vector3.Lerp(positions[indexCurrent], positions[indexNext], elapsed);
            seatObject.rotation = Quaternion.Lerp(rotationss[indexCurrent], rotationss[indexNext], elapsed);
        }

    }





}
