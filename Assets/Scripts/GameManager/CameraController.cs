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

    [Header("Brawl Stars style view (Nov 2026)")]
    [Tooltip("Camera pitch (deg). Was 45: Brawl Stars looks down much more steeply")]
    public float pitch = 58f;
    // FOV 28 (was 38 at 19 m): less perspective stretch, same ground in view. Distance and screen Y were solved so the
    // view still reaches 11.06 m ahead of the character and 6.12 m behind it (same depth and ground area; width at the
    // character 30.8 m instead of 30.1 at 19.5:9, the near edge a little wider and the far edge a little narrower).
    [Tooltip("Vertical field of view (deg). Was 40, then 38 at 19 m")]
    public float fieldOfView = 28f;
    [Tooltip("Distance from the followed point along the view (m). Was 15, then 19 with FOV 38")]
    public float cameraDistance = 27.06f;
    [Tooltip("Screen height of the followed point (FramingTransposer Screen Y: 0 top, 0.5 centre, 1 bottom). Slightly " +
             "below the centre so the narrower lens still sees as far ahead / behind as FOV 38 did")]
    public float screenY = 0.532f;
    [Tooltip("The audio listener stays this far from the followed point (m), on the camera's view line: the 3D sounds " +
             "fade linearly to 0 at 35-60 m and were tuned with the camera ~20 m away. 0 = on the camera")]
    public float listenerDistance = 19f;
    [Tooltip("Stop the camera at the arena's edges (ArenaCameraClamp) instead of always centring the character")]
    public bool clampToArena = true;

    AudioListener listener;

    public void InitCam(Team team)
    {
        freeLook.transform.localRotation = Quaternion.Euler(pitch, team == Team.blue ? 180f : 0f, 0f);
        var lens = freeLook.m_Lens; lens.FieldOfView = fieldOfView; freeLook.m_Lens = lens;
        var framing = freeLook.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framing != null) { framing.m_CameraDistance = cameraDistance; framing.m_ScreenY = screenY; }
        PlaceListener();
        var clamp = freeLook.GetComponent<ArenaCameraClamp>();
        if (clamp == null && clampToArena) clamp = freeLook.gameObject.AddComponent<ArenaCameraClamp>();
        if (clamp != null) clamp.enabled = clampToArena;
        ArenaBounds.Reset();
        SetFreeLook(true);
    }

    /// <summary>Moves the match's audio listener from the camera to a child on its view line, listenerDistance from
    /// the followed point, so the farther camera does not make every 3D sound quieter.</summary>
    void PlaceListener()
    {
        if (freeCam == null) return;
        float forward = listenerDistance > 0f ? Mathf.Max(0f, cameraDistance - listenerDistance) : 0f;
        if (listener == null)
        {
            var onCam = freeCam.GetComponent<AudioListener>();
            if (onCam == null) return;   // no listener in this scene: leave it alone
            onCam.enabled = false;       // first, so two listeners are never on together
            var go = new GameObject("AudioListener");
            go.transform.SetParent(freeCam.transform, false);
            listener = go.AddComponent<AudioListener>();
        }
        listener.transform.localPosition = Vector3.forward * forward;
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
