using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDirection : MonoBehaviour
{
    [HideInInspector]
    public Transform target;
    [HideInInspector]
    public Transform mainPlayer;
    public Transform uiDirec;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null || mainPlayer == null) return;

        Vector3 direc = target.position;
        direc.y = mainPlayer.position.y;
        direc = direc - mainPlayer.position;

        direc = mainPlayer.InverseTransformDirection(direc);

        float angle = Mathf.Atan2(direc.z, direc.x) * Mathf.Rad2Deg - 90;
        uiDirec.rotation = Quaternion.Euler(0, 0, angle);

    }
}
