using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildableLevel : MonoBehaviour
{
    public Animator animator;

    public float buildTime;
    public BuildableEntry controller;
    public int level;

    public GameObject[] hideOnBuildCompleted;
    public GameObject[] activeOnBuildCompleted;
    public SentryLookAt sentryLookAt;
    private float[] speedUpgrades = new float[] { 3f, 2.25f, 2f };
    [HideInInspector]
    public float _buildTime;
    public Collider[] hitBoxs;

    public float StartBuild()
    {
        //animator.SetTrigger("Build");
        _buildTime = buildTime;
        if(level <= controller.level)
        {
            animator.speed = speedUpgrades[level - 1];
            _buildTime = buildTime / speedUpgrades[level - 1];
        }

        if (sentryLookAt != null)
            sentryLookAt.enabled = false;

        if (hideOnBuildCompleted != null)
            for (int i = 0; i < hideOnBuildCompleted.Length; i++)
            {
                hideOnBuildCompleted[i].SetActive(true);
            }

        if (activeOnBuildCompleted != null)
            for (int i = 0; i < activeOnBuildCompleted.Length; i++)
            {
                activeOnBuildCompleted[i].SetActive(false);
            }

        gameObject.SetActive(true);
        SetHitBoxEnable(false);
        StartCoroutine(Build(_buildTime));
        return _buildTime;
    }

    public void BuildCompleted()
    {
        animator.speed = 1f;
        if (hideOnBuildCompleted != null)
            for (int i = 0; i < hideOnBuildCompleted.Length; i++)
            {
                hideOnBuildCompleted[i].SetActive(false);
            }

        if (activeOnBuildCompleted != null)
            for (int i = 0; i < activeOnBuildCompleted.Length; i++)
            {
                activeOnBuildCompleted[i].SetActive(true);
            }

        if(sentryLookAt != null)
        {
            sentryLookAt.enabled = true;
        }
        controller.OnBuildCompleted(level);
        SetHitBoxEnable(true);
    }

    IEnumerator Build(float _time)
    {
        controller.timeBuildingRemain = _time;
        while(controller.timeBuildingRemain > 0)
        {
            controller.timeBuildingRemain -= Time.deltaTime;
            yield return null;
        }
        BuildCompleted();
    }

    public void SetHitBoxEnable(bool enable)
    {
        if (hitBoxs == null) return;
        for(int i = 0; i < hitBoxs.Length; i++)
        {
            hitBoxs[i].enabled = enabled;
        }
    }
}
