using DamageNumbersPro;
using DamageNumbersPro.Demo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTextGUI : MonoBehaviour
{
    public static DamageTextGUI instance;
    RectTransform canvasRect;
    public DamageNumber prefab;
    private DNP_PrefabSettings settings;

    void Awake()
    {
        instance = this;
        canvasRect = GetComponent<RectTransform>();
        settings = prefab.GetComponent<DNP_PrefabSettings>();
    }

    private void Start()
    {
        
    }



    void TextDamage(int number)
    {
        //Select Damage Number:
        //DamageNumber prefab = DNP_DemoManager.instance.GetCurrent();
        //DNP_UIArea.OnSpawn();

        ////Number:
        //float number = 1 + Mathf.Pow(Random.value, 2.2f) * settings.numberRange;
        //if (prefab.digitSettings.decimals == 0)
        //{
        //    number = Mathf.Floor(number);
        //}

        //Get Parent:
        //RectTransform rectParent = DNP_UIArea.GetRect();
        //if (rectParent == null)
        //{
        //    rectParent = canvasRect;
        //}

        //Create Damage Number:
        DamageNumber newDamageNumber = prefab.Spawn(Vector3.zero, number);
        newDamageNumber.SetToMousePosition(canvasRect, Camera.main);

        //if (rectParent != canvasRect)
        //{
        //    newDamageNumber.enableFollowing = true;
        //    newDamageNumber.followedTarget = rectParent;
        //}

        //Apply Demo Settings:
        settings.Apply(newDamageNumber);
    }
}

