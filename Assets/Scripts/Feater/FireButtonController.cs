using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FireButtonController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public float TouchSensitivity_x = 1f;
    public bool invertX;
    public float TouchSensitivity_y = 1f;
    public bool invertY;
    public float TouchSensitivity_z = 1f;

    public Vector2 dragVector;
    private bool pressed;
    private bool dragged = false;

    private TF2Player mainPlayer;

    public Transform fireButton;

    private Vector3 defaultPosition;
//private List<UICamera.MouseOrTouch> touches = new List<UICamera.MouseOrTouch>();
// Start is called before the first frame update
    void Start()
    {
        UpdateSensivity(SettingHelper.GetSensivityValue());
        SettingHelper.OnSensitivityUpdate += UpdateSensivity;
        ArenaManager.Instance.OnMainPlayerSpawned += OnMainPlayerAdded;
        defaultPosition = fireButton.position;
    }

    private void OnDestroy()
    {
        SettingHelper.OnSensitivityUpdate -= UpdateSensivity;
    }

    public void OnMainPlayerAdded(TF2Player player)
    {
        mainPlayer = player;
        StartCoroutine(OnMainPlayerAddDelay());
    }

    IEnumerator OnMainPlayerAddDelay()
    {
        yield return new WaitForSeconds(0.5f);
    }

    public void UpdateSensivity(float value)
    {
        TouchSensitivity_x = invertX ? -value : value;
        TouchSensitivity_y = invertY ? -value : value;
    }

    private void OnDisable()
    {
        this.dragged = false;
        this.pressed = false;
        //mainPlayer.FPSPlayer.RotateLook(0, 0);
        //this.touches.Clear();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(mainPlayer.healthManager.death || !mainPlayer.FPSPlayer.currWeapon.gameObject.activeInHierarchy) return;
        this.pressed = true;
        mainPlayer.FPSPlayer.currWeapon.BeginFire();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        dragged = false;
        dragVector = Vector2.zero;
        mainPlayer.FPSPlayer.RotateLook(0, 0);
        mainPlayer.FPSPlayer.currWeapon.FinishFire();
        fireButton.position = defaultPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(mainPlayer.healthManager.death) return;
        dragged = true;
        if (pressed) { 
            dragVector += eventData.delta;
            mainPlayer.FPSPlayer.RotateLook(GetXAxis(), GetYAxis());
            fireButton.position = eventData.position;
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
