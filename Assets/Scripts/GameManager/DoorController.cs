using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Animator animator;
    public Collider collider;
    public GameObject doorObj;

    public void Open()
    {
        if (animator != null)
            animator.SetTrigger("Open");
        if (collider != null) collider.enabled = false;
        if (doorObj != null) doorObj.SetActive(false);
    }
}
