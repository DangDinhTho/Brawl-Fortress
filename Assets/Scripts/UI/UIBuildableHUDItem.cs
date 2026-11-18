using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBuildableHUDItem : MonoBehaviour
{
    public BuildableType buildableType;
    [HideInInspector]
    public BuildableEntry buildableEntry;
    public Image entryHealthUI;
    public Text txtEntryHealth;
    public Image entryEXPUI;
    public Text txtEntryEXP;
    public Image buildingProcessUI;
    private int level;
    public Image entryAvatar;
    
    private void Start()
    {
        buildingProcessUI.transform.parent.gameObject.SetActive(false);
        entryHealthUI.transform.parent.gameObject.SetActive(false);
        txtEntryHealth.gameObject.SetActive(false);
    }

    public void SetBuildable(BuildableEntry entry)
    {
        this.buildableEntry = entry;
        buildableEntry.OnStartBuild += OnBuilding;
        buildableEntry.OnBuildComplete += OnBuildCompleted;
        buildableEntry.health.OnHealthChanged += OnHelthChange;
        buildableEntry.health.OnDeath += OnDied;
        OnHelthChange(buildableEntry.health.currentHealth, buildableEntry.health.maxHealth);
        if(buildableEntry.state == BuildableState.Building)
        {
            StartCoroutine(Building(buildableEntry.timeBuildingRemain));
        }
        txtEntryHealth.gameObject.SetActive(true);
    }


    private void OnDisable()
    {
        if (buildableEntry == null) return;
        buildableEntry.OnStartBuild -= OnBuilding;
        buildableEntry.OnBuildComplete -= OnBuildCompleted;
        buildableEntry.health.OnHealthChanged -= OnHelthChange;
        buildableEntry.health.OnDeath -= OnDied;
    }

    public void OnBuilding(float time)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(Building(time));
    }

    public void OnBuildCompleted(int _level)
    {
        this.level = _level;
    }

    /// <summary>Same order as HealthBase.OnHealthChanged: (current, max). Was reversed, so the HUD showed max/current.</summary>
    public void OnHelthChange(int health, int maxHealth)
    {
        txtEntryHealth.text = $"{health}/{maxHealth}";
        SetFill(entryHealthUI, maxHealth > 0 ? health * 1.0f / maxHealth : 0f);
    }

    public void OnDied()
    {
        buildingProcessUI.transform.parent.gameObject.SetActive(false);
        entryHealthUI.transform.parent.gameObject.SetActive(false);
        txtEntryHealth.gameObject.SetActive(false);
    }

    /// <summary>Sliced (rounded) bars shrink by width so their rounded ends keep their shape; Filled images use fillAmount.</summary>
    static void SetFill(Image img, float v)
    {
        if (img == null) return;
        v = Mathf.Clamp01(v);
        if (img.type != Image.Type.Sliced) { img.fillAmount = v; return; }
        var r = img.rectTransform;
        r.anchorMax = new Vector2(v, r.anchorMax.y);
        img.enabled = v > 0.04f; // a rounded bar thinner than its height looks broken
    }

    IEnumerator Building(float time)
    {
        entryHealthUI.transform.parent.gameObject.SetActive(false);
        float _timeRun = 0;
        buildingProcessUI.transform.parent.gameObject.SetActive(true);
        while (_timeRun < time)
        {
            SetFill(buildingProcessUI, _timeRun / time);
            _timeRun += Time.deltaTime;
            yield return null;
        }
        buildingProcessUI.transform.parent.gameObject.SetActive(false);
        entryHealthUI.transform.parent.gameObject.SetActive(true);
        yield break;
    }
}
