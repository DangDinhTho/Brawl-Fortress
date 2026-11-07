using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerScoreItem : MonoBehaviour
{
    public Image classIcon;
    public Text txtPlayerName;
    public Text txtKill;
    public Text txtDeath;
    public Text txtDamage;

    public void SetData(CharacterScore score)
    {
        txtPlayerName.text = score.playerName;
        txtKill.text = score.kills.ToString();
        txtDeath.text = score.deaths.ToString();
        txtDamage.text = score.damages.ToString();
        classIcon.sprite = GUIManager.instan.GetClassIcon($"leaderboard_class_{score.classId}");
    }

    public void SetColor(Team team)
    {
        Color teamColor = GUIManager.instan.teamColor[(int)team];
        txtPlayerName.color = teamColor;
        txtKill.color = teamColor;
        txtDeath.color = teamColor;
        txtDamage.color = teamColor;
    }
}
