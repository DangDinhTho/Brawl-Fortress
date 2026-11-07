using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blueprint : MonoBehaviour
{
    public BuildableType buildableType;
    public LayerMask LayerMask;
    public Transform[] drawnPoints;
    public float distanceRay = 3;
    private RaycastHit[] hits = new RaycastHit[4];
    public float doDoc;
    public Animator animator;
    [HideInInspector]
    public bool reject, touch;
    public int expRequired;

    public AudioClip goOnUpVoice;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!touch)
        {
            if (Physics.Raycast(drawnPoints[0].position, Vector3.down, out hits[0], distanceRay, LayerMask)
                && Physics.Raycast(drawnPoints[1].position, Vector3.down, out hits[1], distanceRay, LayerMask)
                && Physics.Raycast(drawnPoints[2].position, Vector3.down, out hits[2], distanceRay, LayerMask)
                && Physics.Raycast(drawnPoints[3].position, Vector3.down, out hits[3], distanceRay, LayerMask)
                )
            {
                if (Mathf.Abs(hits[0].point.y - hits[1].point.y) < doDoc
                    && Mathf.Abs(hits[1].point.y - hits[2].point.y) < doDoc
                    && Mathf.Abs(hits[0].point.y - hits[3].point.y) < doDoc)
                {
                    if (reject)
                    {
                        reject = false;
                        animator.SetBool("Reject", reject);
                    }
                    return;
                }
            }
        }

        if (!reject)
        {
            reject = true;
            animator.SetBool("Reject", reject);
        }

    }

    private bool CheckLayerMark(GameObject gameObject, int layerMarks)
    {
        return (layerMarks == (layerMarks | (1 << gameObject.layer)));
    }

    private void OnTriggerStay(Collider other)
    {
        if (CheckLayerMark(other.gameObject, LayerMask))
        {
            touch = true;
            print(touch);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (CheckLayerMark(other.gameObject, LayerMask))
        {
            touch = false;
        }
    }
}
