using System.Collections;

using UnityEngine;
using System.Collections;

public class Coasterfollower : MonoBehaviour {

    // Use this for initialization
    public float elapsed,elapsed2;
    public Transform seatObject;
    

    public int startingPositon;

    float distanceToNextPoint;

    int indexCurrent = 0, indexNext = 1;

    public Transform[] railPoints;

    //this is the coaster script
    public RollerCoasterPlanes rollerCoaster;

    void Awake()
    {

        Invoke("loadDataCoaster", 0.002f);
       

    }

    void loadDataCoaster()
    {
        railPoints = new Transform[rollerCoaster.transform.childCount];
        // lets find all the rails vertex (geometry)
        for (int jj = 0; jj < rollerCoaster.numberOfRails; jj++)
        {
            railPoints[jj] = rollerCoaster.transform.GetChild(jj).transform;

        }


        //initial positionning
        seatObject.position = railPoints[startingPositon].position;
        seatObject.rotation = railPoints[startingPositon].rotation;
        indexCurrent = startingPositon;
        indexNext = startingPositon + 1;
        distanceToNextPoint = (railPoints[indexNext].position - railPoints[indexCurrent].position).magnitude;

    }


    // Update is called once per frame
    void FixedUpdate()
    {
        elapsed2 += Time.fixedDeltaTime;

        if (rollerCoaster.canMove == true && elapsed2>0.1f)
        {
            //increase evolving parameter
            elapsed = elapsed + rollerCoaster.speedFactor * Mathf.Pow(rollerCoaster.maxHeight - seatObject.position[1], 0.5f) / distanceToNextPoint;


            // contition to change of indices
            //float distanceToNextPoint= (railPoints[indexNext]-seatObject.position).magnitude;
            if (elapsed >= 1)
            {
                indexCurrent += 1;
                //check looping
                if (indexCurrent == rollerCoaster.numberOfRails)
                {
                    indexCurrent = 0;
                }

                if (indexCurrent == rollerCoaster.numberOfRails - 1)
                {
                    indexNext = 0;
                }
                else
                {
                    indexNext = indexCurrent + 1;
                }

                elapsed = rollerCoaster.speedFactor * Mathf.Pow(rollerCoaster.maxHeight - seatObject.position[1], 0.2f) / distanceToNextPoint;
                distanceToNextPoint = (railPoints[indexNext].position - railPoints[indexCurrent].position).magnitude;

                //Debug.Log("setting new point");

            }


            // movement towars new point
            seatObject.position = Vector3.Lerp(railPoints[indexCurrent].position + railPoints[indexCurrent].up * 0.5f, railPoints[indexNext].position + railPoints[indexNext].up * 0.5f, elapsed);
            seatObject.rotation = Quaternion.Lerp(railPoints[indexCurrent].rotation, railPoints[indexNext].rotation, elapsed);
        }

    }





}
