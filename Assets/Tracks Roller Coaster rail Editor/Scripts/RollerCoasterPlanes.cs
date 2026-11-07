using UnityEngine;
using System.Collections;

public class RollerCoasterPlanes : MonoBehaviour {

	// Use this for initialization
	public float elapsed;
	public Transform seatObject;

	public float speedFactor=0.1f;
	public Transform[] railPoints;
	public float maxHeight=27;
	public bool hidepoints=false;
	public bool showPath=true;
	public bool canMove=false;
	public Transform pathcontainer;
	public Material matLin;
	public int startingPositon;

	public float railWidht=1f;

	float distanceToNextPoint;
	public int numberOfRails;

	int indexCurrent=0,indexNext=1;

	void Start () 
	{

		// lets find all the rails vertex (geomety)

		numberOfRails=transform.childCount;
		railPoints=new Transform[numberOfRails];
		for(int jj=0; jj<numberOfRails;jj++)
		{
			railPoints[jj]=transform.GetChild(jj).transform;

			// hide the markers
			if(hidepoints==true)
			{
				for(int kk=0; kk<3;kk++)
				{
					transform.GetChild(jj).transform.GetChild(kk).GetComponent<Renderer>().enabled=false;
				}
			}


		}

		if(showPath==true)
		{
			drawGeometry();
		}

		elapsed=0;


		//initial positionning
		seatObject.position= railPoints[startingPositon].position;
		seatObject.rotation= railPoints[startingPositon].rotation;
		indexNext=startingPositon+1;
		distanceToNextPoint= (railPoints[indexNext].position-railPoints[indexCurrent].position).magnitude;


	}

	void drawGeometry()
	{


		GameObject child;

		MeshFilter tempMeshF;
		MeshRenderer meshRender;

		Vector3[] points=new Vector3[4];

		for(int jj=0; jj<numberOfRails-1;jj++)
		{
			child = new GameObject("rail");
			child.transform.parent=pathcontainer;
		    tempMeshF= child.gameObject.AddComponent<MeshFilter>();
			meshRender= child.gameObject.AddComponent<MeshRenderer>();
			meshRender.material=matLin;

		  	

			points[0] = railPoints[jj].transform.position -railPoints[jj].transform.forward*railWidht;
			points[1] = railPoints[jj].transform.position +railPoints[jj].transform.forward*railWidht;
			points[2] = railPoints[jj+1].transform.position -railPoints[jj+1].transform.forward*railWidht;
			points[3] = railPoints[jj+1].transform.position +railPoints[jj+1].transform.forward*railWidht;


			createGeometry(points[0], points[1], points[2],points[3] ,tempMeshF);


		}

		child = new GameObject("rail");
		child.transform.parent=pathcontainer;
		tempMeshF= child.gameObject.AddComponent<MeshFilter>();

		points[0] = railPoints[numberOfRails-1].transform.position -railPoints[numberOfRails-1].transform.forward*railWidht;
		points[1] = railPoints[numberOfRails-1].transform.position +railPoints[numberOfRails-1].transform.forward*railWidht;
		points[2] = railPoints[0].transform.position -railPoints[0].transform.forward*railWidht;
		points[3] = railPoints[0].transform.position +railPoints[0].transform.forward*railWidht;
		meshRender= child.gameObject.AddComponent<MeshRenderer>();
		meshRender.material=matLin;

		createGeometry(points[0], points[1], points[2],points[3] ,tempMeshF);


		
	}


	// Update is called once per frame
	void FixedUpdate () 
	{

		if(canMove==true)
		{
			//increase evolving parameter
			elapsed=elapsed +  speedFactor*Mathf.Pow(maxHeight-seatObject.position[1],0.5f) /distanceToNextPoint;


			// contition to change of indices
			//float distanceToNextPoint= (railPoints[indexNext]-seatObject.position).magnitude;
			if(elapsed>=1)
			{
				indexCurrent+=1;
				//check looping
				if(indexCurrent==numberOfRails)
				{
					indexCurrent=0;
				}

				if(indexCurrent==numberOfRails-1)
				{
					indexNext=0;
				}
				else
				{
					indexNext=indexCurrent+1;
				}

				elapsed= speedFactor*Mathf.Pow(maxHeight-seatObject.position[1],0.2f) /distanceToNextPoint;
				distanceToNextPoint= (railPoints[indexNext].position-railPoints[indexCurrent].position).magnitude;

				Debug.Log("setting new point");

			}


			// movement towars new point
			seatObject.position=Vector3.Lerp(railPoints[indexCurrent].position+railPoints[indexCurrent].up*0.5f,railPoints[indexNext].position+railPoints[indexNext].up*0.5f,elapsed);
			seatObject.rotation=Quaternion.Lerp(railPoints[indexCurrent].rotation,railPoints[indexNext].rotation,elapsed);
		}
		
	}





	void createGeometry(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,  MeshFilter mf)
	{

		Mesh mesh= new Mesh();
		mf.mesh=mesh;

		//vertices
		Vector3[] vertices =new Vector3[4];

		/*
		             2     3

		             0     1

		*/
		vertices[0]= p1;
		vertices[1]= p2;
		vertices[2]= p3;
		vertices[3]= p4;



		// normal vector
		Vector3[] normals=new Vector3[4];
		normals[0]=-Vector3.forward;
		normals[1]=-Vector3.forward;
		normals[2]=-Vector3.forward;
		normals[3]=-Vector3.forward;



		// triangles indices
		int[] tri=new int[6];

		tri[0]=0;
		tri[1]=2;
		tri[2]=1;
		tri[3]=2;
		tri[4]=3;
		tri[5]=1;

		// texture uv
		Vector2[] uv=new Vector2[4];

		uv[0]=new Vector2(0,0);
		uv[1]=new Vector2(0,1);
		uv[2]=new Vector2(1,0);
		uv[3]=new Vector2(1,1);

		mesh.vertices=vertices;
		mesh.triangles=tri;
		mesh.normals=normals;
		mesh.uv=uv;


	}

}
