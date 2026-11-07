using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualCube : MonoBehaviour
{
    public Color color;

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawCube(transform.position + Vector3.up * 0.5f, new Vector3(1, 1, 1));
    }
}
