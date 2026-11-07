
using UnityEngine;

[ExecuteInEditMode]
public class BezierManager : MonoBehaviour
{
    GameObject prefabFragment;
    public int splinePoints=20;
    public float distance = 5;
    public GameObject prefabObjectMeshDef;
    public GameObject meshContainer;
    
    void Awake()
    {
       
    }

    void Update()
    {
        if (!Application.IsPlaying(gameObject))
        {


            prefabFragment = Resources.Load<GameObject>("Prefabs/fragment");

            //set the cubic/quadratic fragment
            for (int jj = 0; jj < transform.childCount; jj++)
            {
                BezierFragment bzScp = transform.GetChild(jj).GetComponent<BezierFragment>();

                if (bzScp != null)
                {

                }
            }
        }

    }

    public void addPoint()
    {

        //get the last transform in the child count
        BezierFragment lastBezier = transform.GetChild(transform.childCount-1).gameObject.GetComponent<BezierFragment>();

        //create an instance of the fragment
        GameObject instancePoint = Instantiate(prefabFragment,new Vector3(0,0,0), Quaternion.Euler(0,0,0));
        instancePoint.transform.SetParent(transform);
        instancePoint.transform.position = lastBezier.pointB.position;
        //reposition
        Transform newTF = transform.GetChild(transform.childCount - 1);
       

        //assig the variables
        BezierFragment fragScript = instancePoint.GetComponent<BezierFragment>();
        /*fragScript.pointA = lastTf.GetChild(1);
        fragScript.pointB = newTF.GetChild(1);*/
        fragScript.nbPoints = splinePoints;

       
       
        //bezier points
        fragScript.bezierPoint[1].gameObject.SetActive(true);
        fragScript.pointA.position = lastBezier.pointB.position;
        fragScript.bezierPoint[0].position = fragScript.pointA.position + lastBezier.pointB.forward * distance/4;
        fragScript.pointB.position = fragScript.pointA.position +lastBezier.pointB.forward * distance; 
        fragScript.bezierPoint[1].position = fragScript.pointB.position + -lastBezier.pointB.forward * distance/4; 


    }

    public void deletePoint()
    {
        if (transform.childCount > 1)
        {
            DestroyImmediate(transform.GetChild(transform.childCount - 1).gameObject);
        }
    }

    public void applymesh()
    {
        //generate the gameobjects and add the scripts of meshdeformer

        for (int jj = 0; jj < transform.childCount; jj++)
        {
            BezierFragment bzScp = transform.GetChild(jj).GetComponent<BezierFragment>();
            if (bzScp != null)
            {
         
                GameObject go = GameObject.Instantiate(prefabObjectMeshDef);
                go.tag = "meshDeformed";
                go.transform.SetParent(meshContainer.transform);
                MeshDeformer mshD = go.AddComponent<MeshDeformer>();
                mshD.prefabGo = prefabObjectMeshDef;
                mshD.bezier = bzScp;
            }
        }


    }
    public void destroyMesh()
    {
        //generate the gameobjects and add the scripts of meshdeformer
        GameObject[] meshes = GameObject.FindGameObjectsWithTag("meshDeformed");
        if (meshes == null) return;
 
        for (int jj = 0; jj < meshes.Length; jj++)
        {
            DestroyImmediate(meshes[jj].gameObject);
        }


    }

    public void UnactiveLine()
    {
        var meshs = transform.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mesh in meshs)
        {
            //DestroyImmediate(mesh);
            mesh.enabled = false;
        }

        var lines = transform.GetComponentsInChildren<LineRenderer>(true);
        foreach (LineRenderer line in lines)
        {
            //DestroyImmediate(line);
            line.enabled = false;
        }

        var sphares = transform.GetComponentsInChildren<SphereCollider>(true);
        foreach (SphereCollider sp in sphares)
        {
            //DestroyImmediate(line);
            sp.enabled = false;
        }
    }

    public void ActiveLine()
    {
        var meshs = transform.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mesh in meshs)
        {
            //DestroyImmediate(mesh);
            mesh.enabled = true;
        }

        var lines = transform.GetComponentsInChildren<LineRenderer>(true);
        foreach (LineRenderer line in lines)
        {
            //DestroyImmediate(line);
            line.enabled = true;
        }

        var sphares = transform.GetComponentsInChildren<SphereCollider>(true);
        foreach (SphereCollider sp in sphares)
        {
            //DestroyImmediate(line);
            sp.enabled = true;
        }
    }
}