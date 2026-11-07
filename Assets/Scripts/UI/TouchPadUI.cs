using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TouchPadUI : MonoBehaviour
{
    public static TouchPadUI instan;
    public float Vertical { get; private set; }
    public float Horizontal { get; private set; }

    private Touch m_Touch;
    //private int m_TouchID;

    private void Awake()
    {
        instan = this;
    }
    // Start is called before the first frame update
    //void Start()
    //{

    //}

    //// Update is called once per frame
    void Update()
    {
        CameraRotation();
    }

    void CameraRotation()
    {
        //m_TouchID = bl_TouchHelper.GetUsableTouch();
        //if (m_TouchID != -1)
        //{
        if (Input.touchCount > 0)
        {
            m_Touch = Input.touches[0];//.ToList<Touch>().Find(x => x.fingerId == m_TouchID);
            Debug.Log(m_Touch.phase);
            if (m_Touch.phase == TouchPhase.Moved)
            {
                Vector2 deltaPosition = m_Touch.deltaPosition;
                Vertical = deltaPosition.y;
                Horizontal = deltaPosition.x;
            }
            else
            {
                Vertical = 0;
                Horizontal = 0;
            }
        }
        else
        {
            Vertical = 0;
            Horizontal = 0;
        }
        //}
    }

    public Vector2 GetInput(float sensitivity)
    {
        float x = (Horizontal * sensitivity) * Time.deltaTime;
        float y = (Vertical * sensitivity) * Time.deltaTime;
        Vector2 v = new Vector2(x, y);
        return v;
    }
}
