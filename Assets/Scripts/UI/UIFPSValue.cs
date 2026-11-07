using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFPSValue : MonoBehaviour
{
    private long fpsCounter;
    private float fps;
    private float fpsTime;
    public Text fpsText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        fpsCounter++;
        if (Time.time - fpsTime >= 3f)
        {

            fps = Mathf.Round(fpsCounter / 3);
            fpsCounter = 0;
            fpsTime = Time.time;
        }
        
        fpsText.text = $"{fps} fps";
    }
}
