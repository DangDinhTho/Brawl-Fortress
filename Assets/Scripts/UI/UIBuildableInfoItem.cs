using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBuildableInfoItem : MonoBehaviour
{
    public BuildableType buildableType;
    private BuildableEntry buildableEntry;
    public Image entryHealthUI;
    public Text txtEntryHealth;
    //public Image entryEXPUI;
    public Text txtEntryEXP;
    public Image buildingProcessUI;
    private int level;
    public Image entryAvatar;

    private void Start()
    {
        buildingProcessUI.transform.parent.gameObject.SetActive(false);
    }

    public void SetBuildable(BuildableEntry entry)
    {
        this.buildableEntry = entry;
        buildableEntry.OnStartBuild += OnBuilding;
        buildableEntry.OnBuildComplete += OnBuildCompleted;
        buildableEntry.OnExpChanged += OnEXPChange;
        if (buildableEntry.state == BuildableState.Building)
        {
            float buildTime = 1;
            if (buildableEntry.levelBuilding < buildableEntry.ReferenceLevels.Length)
                buildTime = buildableEntry.ReferenceLevels[buildableEntry.levelBuilding]._buildTime;
            else
                buildTime = buildableEntry.ReferenceLevels[buildableEntry.ReferenceLevels.Length - 1]._buildTime;
            StartCoroutine(Building(buildTime, buildTime - buildableEntry.timeBuildingRemain));
        }
        else
        {
            buildingProcessUI.transform.parent.gameObject.SetActive(false);
        }

        if (buildableEntry.level >= 1)
            OnEXPChange(buildableEntry.currentExp, buildableEntry.upgradeExp[buildableEntry.level - 1]);
        else
        {
            txtEntryEXP.text = "-/-";
        }
    }

    private void OnDisable()
    {
        if (buildableEntry == null) return;
        buildableEntry.OnStartBuild -= OnBuilding;
        buildableEntry.OnBuildComplete -= OnBuildCompleted;
        buildableEntry.OnExpChanged -= OnEXPChange;
    }

    public void OnBuilding(float toatalTime)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(Building(toatalTime, 0));
    }

    public void OnBuildCompleted(int _level)
    {
        this.level = _level;
        if (level < 3)
            OnEXPChange(buildableEntry.currentExp, buildableEntry.upgradeExp[buildableEntry.level - 1]);
        else
            OnEXPChange(buildableEntry.upgradeExp[buildableEntry.level - 1], buildableEntry.upgradeExp[buildableEntry.level - 1]);
    }

    public void OnEXPChange(int crrExp, int maxExp)
    {
        txtEntryEXP.text = $"{crrExp}/{maxExp}";
    }

    IEnumerator Building(float totalTime, float time)
    {
        float _timeRun = time;
        buildingProcessUI.transform.parent.gameObject.SetActive(true);
        while (_timeRun < totalTime)
        {
            buildingProcessUI.fillAmount = _timeRun / totalTime;
            _timeRun += Time.deltaTime;
            yield return null;
        }
        buildingProcessUI.transform.parent.gameObject.SetActive(false);
        yield break;
    }
}
