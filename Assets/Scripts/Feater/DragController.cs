using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public float TouchSensitivity_x = 1f;
    public bool invertX;
    public float TouchSensitivity_y = 1f;
    public bool invertY;
    public float TouchSensitivity_z = 1f;

    public Vector2 dragVector;
    private bool pressed;
    private bool dragged = false;

    [HideInInspector] public bool TPSEnable;
    //private TF2Player mainPlayer;

    public Transform freeCamButton;

    private Vector3 defaultPosition;
//private List<UICamera.MouseOrTouch> touches = new List<UICamera.MouseOrTouch>();
// Start is called before the first frame update
    void Start()
    {
        UpdateSensivity(SettingHelper.GetSensivityValue());
        SettingHelper.OnSensitivityUpdate += UpdateSensivity;
        //ArenaManager.Instance.OnMainPlayerSpawned += OnMainPlayerAdded;
        defaultPosition = freeCamButton.position;
    }

    private void OnDestroy()
    {
        SettingHelper.OnSensitivityUpdate -= UpdateSensivity;
    }

    // public void OnMainPlayerAdded(TF2Player player)
    // {
    //     mainPlayer = player;
    //     StartCoroutine(OnMainPlayerAddDelay());
    // }

    IEnumerator OnMainPlayerAddDelay()
    {
        yield return new WaitForSeconds(0.5f);
    }

    public void UpdateSensivity(float value)
    {
        TouchSensitivity_x = invertX ? -value / 2 : value / 2;
        TouchSensitivity_y = invertY ? -value / 2 : value / 2;
    }

    private void OnDisable()
    {
        this.dragged = false;
        this.pressed = false;
        //this.touches.Clear();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        this.pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!dragged) UICharacterInputController.instan.SwitchCam();
        pressed = false;
        dragged = false;
        dragVector = Vector2.zero;
        freeCamButton.position = defaultPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        dragged = true;
        if (!TPSEnable) return;
        if (pressed) { 
            dragVector += eventData.delta;
            freeCamButton.position = eventData.position;
        }
    }
    
    public float GetXAxis()
    {
        float x = dragVector.x / Screen.height;
        dragVector.x = 0;
        return x * TouchSensitivity_x;
    }
    public float GetYAxis()
    {
        float y = dragVector.y / Screen.height;
        dragVector.y = 0;
        return y * TouchSensitivity_y;
    }
// public float GetZAxis()
// {
//     var a =this.zoomAxis / (Screen.height);
//     this.zoomAxis = 0;
//     return a;
// }
}
