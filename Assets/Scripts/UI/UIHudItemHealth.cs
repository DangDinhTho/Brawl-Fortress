using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHudItemHealth : MonoBehaviour
{
    public Image avatar;
    public Image bgr;
    public Image healthBgr;
    public Image fillHealth;
    public Text txtHealthValue;
    public Gradient gradient;
    public void SetData(TF2Player player)
    {
        player.healthManager.OnHealthChanged += SetHealthBar;
        
        string avatarIconName = $"class_{player.tF2Class}{player.team}";
        avatar.sprite = GUIManager.instan.GetAvatarSprite(avatarIconName);
        bgr.color = GUIManager.instan.teamColor[(int)player.team];
        SetHealthBar(player.healthManager.currentHealth, player.characterConfig.characterStats.maxHealth);
    }

    public void SetHealthBar(int currHealth, int maxHealth)
    {
        float value = currHealth * 1.0f / maxHealth;
        fillHealth.fillAmount = value;
        txtHealthValue.text = currHealth.ToString();
        fillHealth.color = gradient.Evaluate(value);
        healthBgr.color = gradient.Evaluate(value);
    }
}
