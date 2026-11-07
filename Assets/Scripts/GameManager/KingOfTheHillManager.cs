using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;


public class KingOfTheHillManager : MonoBehaviour
{
    public static KingOfTheHillManager instan;
    public KothPoint kothPoint;
    public int maxTime;
    public Dictionary<Team, float> timeOfTeams = new Dictionary<Team, float>();
    public Action OnTimeChanged;
    public Team teamOwner;
    public Action<Team> OnTeamOwnerChanged;
    private PhotonView photonView;
    public Light pointLight;
    public Color[] teamOwnerColor;

    private void Awake()
    {
        instan = this;
    }

    public void Start()
    {
        timeOfTeams.Add(Team.red, maxTime);
        timeOfTeams.Add(Team.blue, maxTime);
        photonView = ArenaNetworkManager.instan.photonView;
        ArenaManager.Instance.OnTimeOut += OnExpretedTime;
        this.OnTeamOwnerChanged += OnTeamOwnerChange;
        UIMainHud.instan.CreateHUD<KingOfTheHillHUD>();
    }

    public void OnExpretedTime()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            kothPoint.enabled = false;
            ArenaNetworkManager.instan.EndGame(0, 0);
        }
    }

    public void OnTeamOwnerChange(Team team)
    {
        pointLight.color = teamOwnerColor[(int)team];
    }

    public void SetNoneOwnerOnMaster()
    {
        if(teamOwner == Team.None) return;
        teamOwner = Team.None;
        OnTeamOwnerChanged?.Invoke(teamOwner);

        photonView.RPC(nameof(RPC_UpdateNoneTeamOwner), RpcTarget.Others);

    }

    public void SetTeamOwnerOnMaster(Team team, float deltaTime)
    {
        if (team != teamOwner)
        {
            OnTeamOwnerChanged?.Invoke(team);
            teamOwner = team;
        }
        else
        {
            timeOfTeams[team] -= deltaTime;
            if (timeOfTeams[team] < 0) timeOfTeams[team] = 0;
            OnTimeChanged?.Invoke();
        }

        if (timeOfTeams[team] <= 0 && !ArenaManager.Instance.endGameSign)
        {
            int redTeam = 0;
            int blueTeam = 0;
            if (team == Team.red) redTeam = 1;
            else
            {
                blueTeam = 1;
            }
            ArenaNetworkManager.instan.EndGame(redTeam, blueTeam);
            kothPoint.enabled = false;
        }
        
        photonView.RPC(nameof(RPC_UpdateTime), RpcTarget.Others, team, (int)timeOfTeams[team]);

    }


    [PunRPC]
    public void RPC_UpdateTime(Team _team, int time)
    {
        if (_team == Team.None)
        {
            SetNoneOwnerOnMaster();
            return;
        }
        if (_team != teamOwner)
        {
            teamOwner = _team;
            OnTeamOwnerChanged?.Invoke(_team);
        }

        timeOfTeams[_team] = time;
        OnTimeChanged?.Invoke();
    }
    
    [PunRPC]
    public void RPC_UpdateNoneTeamOwner()
    {
        teamOwner = Team.None;
        OnTeamOwnerChanged?.Invoke(teamOwner);
    }
    
}
