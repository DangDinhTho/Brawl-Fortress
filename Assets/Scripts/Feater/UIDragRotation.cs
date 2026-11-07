using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragRotation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Transform target;
    public float speed = 1;

    private Vector2 dragVector;
    private bool pressed;

    // Start is called before the first frame update
    void Start()
    {
        if (target == null) target = HomeManager.instance.characterContainer;
    }

    // Update is called once per frame
    //void Update()
    //{
    //    if(Input.GetMouseButton(0))
    //    {
    //        target.Rotate(0f, -Input.GetAxis("Mouse X") * speed, 0f);
    //    }
    //}

    public void OnPointerDown(PointerEventData eventData)
    {
        this.pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        //dragged = false;
        dragVector = Vector2.zero;

    }

    public void OnDrag(PointerEventData eventData)
    {
        //dragged = true;
        if (pressed)
        {
            dragVector += eventData.delta;
            target.Rotate(0f, -GetXAxis(), 0f);
        }
    }

    public float GetXAxis()
    {
        float x = dragVector.x / Screen.height;
        dragVector.x = 0;
        return x * speed;
    }
}
