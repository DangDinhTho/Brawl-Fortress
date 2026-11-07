using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarItem : MonoBehaviour
{
    private TF2Class tF2Class;
    private int team;
    public Image avatar;
    public Image bgr;
    public Image deathBgr;
    public Color[] teamColor;
    public Sprite[] deathBgrSprite;
    public Image healthFill;
    public Text txtCountDown;
    public GameObject deathObj;

    public void SetAvatar(TF2Player player)
    {
        this.tF2Class = player.tF2Class;
        this.team = (int)player.team;
        avatar.sprite = GUIManager.instan.GetAvatar(player.tF2Class, player.team);
        bgr.color = teamColor[this.team];
        deathBgr.sprite = deathBgrSprite[this.team];
        if(player.team == ArenaManager.Instance.mainPlayer.team)
        {
            healthFill.gameObject.SetActive(true);
            player.healthManager.OnHealthChanged += OnHeathChanged;
        }
        else
        {
            healthFill.gameObject.SetActive(false);
        }
        
        player.healthManager.OnDeath += OnPlayerDeath;
        player.healthManager.OnRespawn += OnPlayerRespawn;
        player.healthManager.OnRespawnCountDownUpdate += OnCountDownUpdate;
        deathObj.SetActive(false);
    }

    public void OnHeathChanged(int currHealth, int maxHealth)
    {
        healthFill.fillAmount = currHealth * 1.0f / maxHealth;
    }

    public void OnPlayerDeath()
    {
        avatar.sprite = GUIManager.instan.GetAvatar(tF2Class, (Team)team, true);
        deathObj.SetActive(true);
    }

    public void OnPlayerRespawn()
    {
        avatar.sprite = GUIManager.instan.GetAvatar(tF2Class, (Team)team, false);
        deathObj.SetActive(false);
    }

    public void OnCountDownUpdate(int sec)
    {
        txtCountDown.text = sec.ToString();
    }
}
