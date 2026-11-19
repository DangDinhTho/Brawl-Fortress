using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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

    [Header("Width")]
    [Tooltip("Width while nothing is built (just the avatar)")] public float collapsedWidth = 80f;
    [Tooltip("Width while building / built (avatar + bar + health)")] public float expandedWidth = 230f;
    public float resizeTime = 0.2f;
    RectTransform rt;

    /// <summary>Short when empty (avatar only), long when the building / health bar is shown. Left edge stays put (pivot on the left).</summary>
    void Resize(bool expanded, bool animate)
    {
        if (rt == null) rt = (RectTransform)transform;
        var size = new Vector2(expanded ? expandedWidth : collapsedWidth, rt.sizeDelta.y);
        rt.DOKill();
        if (animate && gameObject.activeInHierarchy) rt.DOSizeDelta(size, resizeTime).SetEase(Ease.OutCubic).SetUpdate(true);
        else rt.sizeDelta = size;
    }
    
    private void Start()
    {
        buildingProcessUI.transform.parent.gameObject.SetActive(false);
        entryHealthUI.transform.parent.gameObject.SetActive(false);
        txtEntryHealth.gameObject.SetActive(false);
        Resize(buildableEntry != null, false);
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
        else entryHealthUI.transform.parent.gameObject.SetActive(true); // already built (moved / re-attached): was left hidden
        txtEntryHealth.gameObject.SetActive(true);
        Resize(true, true);
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
        Resize(false, true);
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
        Resize(true, true);
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
