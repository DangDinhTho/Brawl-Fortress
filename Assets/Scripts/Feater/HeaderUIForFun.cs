using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeaderUIForFun : MonoBehaviour
{
    public Transform guiTransform;
    public Image image;
    private Transform mainCamera;
    public TF2Player ownerPlayer;
    public Image health;
    public TMP_Text txtName;

    public void Start()
    {
        ownerPlayer.healthManager.OnDeath += OnPlayerDead;
        ownerPlayer.healthManager.OnRespawn += OnPlayerRespawn;
        ownerPlayer.healthManager.OnHealthChanged += OnHealthChanged;
        health.fillAmount = 1;
        mainCamera = CameraController.instan.GetCurrentCamera().transform;
        image.gameObject.SetActive(false);
        Invoke(nameof(InitLate), 1);
    }

    public void InitLate()
    {
        if (ownerPlayer.isMainPlayer)
        {
            health.color = GUIManager.instan.mineColor;
        }
        else if(ownerPlayer.team == ArenaManager.Instance.mainPlayer.team)
        {
            health.color = GUIManager.instan.teamMateColor;
        }
        else
        {
            health.color = GUIManager.instan.enemyColor;
        }

        if (txtName)
        {
            txtName.text = ownerPlayer.playerName;
        }
    }

    public void OnPlayerDead()
    {
        gameObject.SetActive(false);
    }

    public void OnPlayerRespawn()
    {
        gameObject.SetActive(true);
        health.fillAmount = 1;
    }

    public void OnHealthChanged(int crHealth, int maxHealth)
    {
        health.fillAmount = crHealth * 1.0f / maxHealth;
    }

    public void InitData(Sprite sprite)
    {
        if (ownerPlayer.healthManager.death)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        image.gameObject.SetActive(true);
        mainCamera = CameraController.instan.GetCurrentCamera().transform;
        
        image.sprite = sprite;
        StopAllCoroutines();
        StartCoroutine(WaitForOff());
    }

    IEnumerator WaitForOff()
    {
        yield return new WaitForSeconds(5);
        image.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        guiTransform.LookAt(guiTransform.position + mainCamera.forward);
    }
}
