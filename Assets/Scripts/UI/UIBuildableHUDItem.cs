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
        OnHelthChange(buildableEntry.health.maxHealth, buildableEntry.health.currentHealth);
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

    public void OnHelthChange(int maxHealth, int health)
    {
        txtEntryHealth.text = $"{health}/{maxHealth}";
        entryHealthUI.fillAmount = health * 1.0f / maxHealth;
    }

    public void OnDied()
    {
        buildingProcessUI.transform.parent.gameObject.SetActive(false);
        entryHealthUI.transform.parent.gameObject.SetActive(false);
        txtEntryHealth.gameObject.SetActive(false);
    }

    IEnumerator Building(float time)
    {
        entryHealthUI.transform.parent.gameObject.SetActive(false);
        float _timeRun = 0;
        buildingProcessUI.transform.parent.gameObject.SetActive(true);
        while (_timeRun < time)
        {
            buildingProcessUI.fillAmount = _timeRun / time;
            _timeRun += Time.deltaTime;
            yield return null;
        }
        buildingProcessUI.transform.parent.gameObject.SetActive(false);
        entryHealthUI.transform.parent.gameObject.SetActive(true);
        yield break;
    }
}
