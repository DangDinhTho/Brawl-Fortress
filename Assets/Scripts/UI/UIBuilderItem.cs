using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBuilderItem : MonoBehaviour
{
    [HideInInspector]
    public BuildableEntry entry;
    public BuildableType buildableType;
    //public Transform direcUI;
    public PopupEngineerBuilder popupEngineerBuilder;
    public Text txtStatus;
    public GameObject buildedObj;
    
    
    
    public void InitData(BuildableEntry _buildableEntry)
    {
        this.entry = _buildableEntry;
        txtStatus.text = "BUILT";
        txtStatus.color = new Color(0.45f, 1f, 0.3f);
        buildedObj.SetActive(true);
    }

    public void InitEmpty()
    {
        this.entry = null;
        txtStatus.text = "BUILD";
        txtStatus.color = new Color(1f, 0.85f, 0.25f);
        buildedObj.SetActive(false);
    }


    //private void Update()
    //{
    //    //if(entry == null) return;
    //    //direct
    //}

    public void OnSelectClick()
    {
        if(entry != null && !entry.health.death) return;
        popupEngineerBuilder.SelectBuildableType(this.buildableType);
    }
}
