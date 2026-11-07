using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILeaderboardItem : MonoBehaviour
{
    public Image sttIcon;
    public Image icon;

    public Text txtStt;

    public Text txtName;

    public Text txtRankName;

    public void SetData(PlayerLeaderboardEntry entry)
    {
        Medal rank = ConfigManager.GetMedal(entry.StatValue);
        txtName.text = entry.DisplayName;
        icon.sprite = GUIManager.instan.iconAtlas.GetSprite(rank.ToString());
        txtRankName.text = LocalizationManager.Localize(rank.ToString());

        if (entry.Position <= 16)
        {
            sttIcon.gameObject.SetActive(true);
            txtStt.gameObject.SetActive(false);
            sttIcon.sprite = GUIManager.instan.iconAtlas.GetSprite($"leaderboard_dom{entry.Position + 1}");
        }
        else
        {
            sttIcon.gameObject.SetActive(false);
            txtStt.gameObject.SetActive(true);
            txtStt.text = entry.Position.ToString();
        }
    }
}
