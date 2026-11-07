using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawLineRender : MonoBehaviour
{
    public Transform Point1;
    public Transform Point2;
    public Transform Point3;
    public LineRenderer linerenderer;
    public float vertexCount = 12;
    //private float Point2Ypositio = 2;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        //Point2 = new Vector3((Point1.transform.position.x + Point3.transform.position.x), Point1.position.y, (Point1.transform.position.z + Point3.transform.position.z) / 2);

        //Point2 = Point1.position + new Vector3((Point3.position.x - Point1.position.x) / 2, 0, (Point3.position.z - Point1.position.z) * 1f / 5);
        var pointList = new List<Vector3>();

        for (float ratio = 0; ratio <= 1; ratio += 1 / vertexCount)
        {
            var tangent1 = Vector3.Lerp(Point1.position, Point2.position, ratio);
            var tangent2 = Vector3.Lerp(Point2.position, Point3.position, ratio);
            var curve = Vector3.Lerp(tangent1, tangent2, ratio);

            pointList.Add(curve);
        }

        linerenderer.positionCount = pointList.Count;
        linerenderer.SetPositions(pointList.ToArray());

        //Point2 = transform.position + transform.forward * 6;
        //var pointList = new List<Vector3>();

        //for (float ratio = 0; ratio <= 1; ratio += 1 / vertexCount)
        //{
        //    var tangent1 = Vector3.Lerp(Point1.position, Point2, ratio * 3);
        //    var tangent2 = Vector3.Lerp(Point2, Point3.position, ratio);
        //    var curve = Vector3.Lerp(tangent1, tangent2, ratio);

        //    pointList.Add(curve);
        //}

        //linerenderer.positionCount = pointList.Count;
        //linerenderer.SetPositions(pointList.ToArray());
    }
}
