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
        txtStatus.text = "builded";
        buildedObj.SetActive(true);
    }

    public void InitEmpty()
    {
        this.entry = null;
        txtStatus.text = "+";
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
