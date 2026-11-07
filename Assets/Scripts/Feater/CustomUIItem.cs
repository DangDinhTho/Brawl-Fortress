using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CustomUIItem : MonoBehaviour, IDragHandler
{
    private bool custom;
    //public GameObject icon;
    public string id;
    //private RectTransform rectTransform;
    private Vector2 offset;
    private RectTransform thisRect;
    private Vector2 defaultPostion;

    private void Awake()
    {
        thisRect = transform.parent.GetComponent<RectTransform>();
        // if (!PlayerPrefs.HasKey("SETBUTTON" + id))
        // {
        //     PlayerPrefs.SetFloat(id + "x", thisRect.anchoredPosition.x);
        //     PlayerPrefs.SetFloat(id + "y", thisRect.anchoredPosition.y);
        //     PlayerPrefs.SetInt("SETBUTTON" + id, 1);
        // }
    }
    public void Custom()
    {
        if (id.Equals("fire_button"))
            UICharacterInputController.instan.fireButton.enabled = false;
        custom = true;
        //this.enabled = true;
        gameObject.SetActive(true);
        offset = transform.parent.position - transform.position; 
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!custom) return;
        transform.parent.position = eventData.position + offset;
    }

    // Start is called before the first frame update
    void Start()
    {
        //rectTransform = GetComponentInParent<RectTransform>();
        if (!PlayerPrefs.HasKey("CustomButton" + id + "x"))
        {
            //PlayerPrefs.SetFloat(id + "x", thisRect.anchoredPosition.x);
            //PlayerPrefs.SetFloat(id + "y", thisRect.anchoredPosition.y);
            //PlayerPrefs.SetInt("SETBUTTON" + id, 1);
            defaultPostion = thisRect.anchoredPosition;
        }
        else
        {
            float x = PlayerPrefs.GetFloat("CustomButton" + id + "x");
            float y = PlayerPrefs.GetFloat("CustomButton" + id + "y");
            defaultPostion = new Vector2(x, y);
            thisRect.anchoredPosition = defaultPostion;
        }

        
        // if (id.Equals("joystick"))
        // {
        //     ArenaManager.instan.gUIController.moveJoystick.defaulPos = ArenaManager.instan.gUIController.moveJoystick.transform.position;
        // }
        gameObject.SetActive(false);
        //this.enabled = false;
    }

    public void Save()
    {
        custom = false;
        PlayerPrefs.SetFloat("CustomButton" + id+ "x", thisRect.anchoredPosition.x);
        PlayerPrefs.SetFloat("CustomButton" + id + "y", thisRect.anchoredPosition.y);
        
        if (id.Equals("fire_button"))
        {
            UICharacterInputController.instan.fireButton.enabled = true;
        }
        gameObject.SetActive(false);
        //this.enabled = false;
    }

    public void Cancel()
    {
        thisRect.anchoredPosition = defaultPostion;
        custom = false;
        gameObject.SetActive(false);
        
    }
}
