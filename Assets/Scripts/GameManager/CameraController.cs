using System;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instan;
   // public CinemachineFreeLook freeLook;
    public CinemachineVirtualCamera freeLook;
    public Camera freeCam;
    private Vector2 tweenTarget;
    [HideInInspector]
    public bool tweening;

    public DragController freeCamDragController;

    //public PlayerDragController fpsDragController;

    private void Awake()
    {
        instan = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        CinemachineCore.GetInputAxis += HandleAxisInput;
        //SetFreeLook(false);
    }

    private void TweenToAxis(Vector2 target)
    {
        tweening = true;
        target.x = Mathf.Repeat(target.x + 180f, 360) - 180f;
        this.tweenTarget = target;
    }

    public Camera GetCurrentCamera()
    {
        //if (freeCam.gameObject.activeInHierarchy)
        //{
        //    return freeCam;
        //}
        //else
        //{
        //    //return ArenaManager.Instance.mainPlayer.fPSReferences.fpsCam;
        //}
        return freeCam;
    }

    // Update is called once per frame
    void Update()
    {
        //if (tweening)
        //{
        //    {
        //        float speed = 4f;
        //        this.freeLook.m_YAxis.Value = Mathf.Lerp(this.freeLook.m_YAxis.Value, tweenTarget.y, Time.deltaTime * speed);
        //        //Debug.Log(this.freeLook.m_XAxis.Value + " " + tweenTarget.x);
        //        this.freeLook.m_XAxis.Value = Mathf.Lerp(this.freeLook.m_XAxis.Value, tweenTarget.x, Time.deltaTime * speed);
        //        if (Mathf.Abs(this.freeLook.m_YAxis.Value - tweenTarget.y) < 0.1f && Mathf.Abs(this.freeLook.m_XAxis.Value - tweenTarget.x) < 0.1f)
        //        {
        //            tweening = false;
        //        }
        //    }
        //}

    }

    public void InitCam(Team team)
    {
        if (team == Team.blue)
        {
            freeLook.transform.localRotation = Quaternion.Euler(45, 180, 0);
        }
        SetFreeLook(true);
    }

    public void SetFreeLook(bool state, Transform target = null, Transform lookAt = null)
    {
        freeLook.gameObject.SetActive(state);
        //freeLook.enabled = state;
        freeCam.gameObject.SetActive(state);
        //fpsDragController.gameObject.SetActive(!state);
        freeCamDragController.TPSEnable = state;
        
        
        if (state)
        {
            if(target == null) target = ArenaManager.Instance.mainPlayer.transform;
            if(lookAt == null) lookAt = ArenaManager.Instance.mainPlayer.transform;
            freeLook.Follow = target;
            freeLook.LookAt = lookAt;
            //freeLook.ForceCameraPosition(this.freeCam.transform.position, this.freeCam.transform.rotation);
            TweenToAxis(new Vector2(target.transform.localRotation.eulerAngles.y, 0.5f));
        }

    }


    private float HandleAxisInput(string axisName)
    {
        //TODO: touch control
        switch (axisName)
        {
            case "Mouse X":
                float x= freeCamDragController.GetXAxis();
                if (x != 0) CameraController.instan.tweening = false;
                return x;
            case "Mouse Y":
                float y = freeCamDragController.GetYAxis();
                if (y != 0) CameraController.instan.tweening = false;
                return y;
            // case "Mouse ScrollWheel":
            //     if (CameraController.Instance.IsFreeCamEnabled()  )
            //     {
            //         if (GUIManager.instance.CheckUIRaycast(new Vector3(Screen.width/2, Screen.height/2), out var collider))
            //         {
            //             if (collider.gameObject != dragController.gameObject)
            //             {
            //                 //has popup 
            //                 return 0;
            //             }
            //         }
            //         // if (Input.GetAxis("Mouse ScrollWheel") != 0)
            //         // {
            //         //     player.StopPathMove();
            //         // }
            //         var axis = Input.GetAxis("Mouse ScrollWheel");
            //         return axis;
            //     }
            //     break;
            // case "Pinch Zoom":
            //     return dragController.GetZAxis();
            //     if (Input.touchCount == 2 && CameraController.Instance.IsFreeCamEnabled())
            //     {
            //         Touch touchZero = Input.GetTouch(0);
            //         Touch touchOne = Input.GetTouch(1);
            //
            //         Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            //         Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
            //
            //         float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            //         float curMagnitude = (touchZero.position - touchOne.position).magnitude;
            //
            //         float difference = curMagnitude - prevMagnitude;
            //         // if (difference != 0)
            //         // {
            //         //     player.StopPathMove();
            //         // }
            //
            //         //TODO: check if value correct
            //         return Mathf.Clamp(difference, CameraController.Instance.zAxis.m_MinValue,
            //             CameraController.Instance.zAxis.m_MaxValue);
            //     }
            //     break;
        }

        return 0f;
    }
}
