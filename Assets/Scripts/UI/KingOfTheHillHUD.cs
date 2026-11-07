using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KingOfTheHillHUD : UIPlayerHUDBase
{
    public static KingOfTheHillHUD instan;
    public Text[] teamTimesText;
    private Dictionary<Team, Text> txtTeamTimes = new Dictionary<Team, Text>();
    public Image pointOwner;
    public Sprite[] pointIcons;
    
    private void Awake()
    {
        instan = this;
    }

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        txtTeamTimes.Add(Team.red, teamTimesText[0]);
        txtTeamTimes.Add(Team.blue, teamTimesText[1]);
        KingOfTheHillManager.instan.OnTimeChanged += UpdateTeamTime;
        KingOfTheHillManager.instan.OnTeamOwnerChanged += UpdatePointOwner;
        UpdateTeamTime();
    }

    public void UpdateTeamTime()
    {
        for (Team team = Team.red; team <= Team.blue; team++)
        {
            txtTeamTimes[team].text = UtilityHelper.FormatTime((int)KingOfTheHillManager.instan.timeOfTeams[team]);
        }
    }

    public void UpdatePointOwner(Team team)
    {
        pointOwner.sprite = pointIcons[(int)team];
    }
}
