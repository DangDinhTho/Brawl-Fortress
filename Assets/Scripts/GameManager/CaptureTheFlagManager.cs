using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CaptureTheFlagManager : MonoBehaviour
{
    public static CaptureTheFlagManager instan;

    public FlagController[] flagControllers;
    [HideInInspector]
    public Vector3[] homePositions;

    public Dictionary<Team, int> scores = new Dictionary<Team, int>(){ { Team.red , 0}, { Team.blue , 0}};
    public int targetScore = 3;
    private CaptureTheFlagHUD captureTheFlagHUD;
    
    private void Awake()
    {
        instan = this;
        homePositions = new Vector3[2];
        homePositions[0] = flagControllers[0].transform.position;
        homePositions[1] = flagControllers[1].transform.position;
    }
    // Start is called before the first frame update
    void Start()
    {
        captureTheFlagHUD = UIMainHud.instan.CreateHUD<CaptureTheFlagHUD>(flagControllers[0], flagControllers[1], targetScore);
        
    }

  

    public void AddScore(Team team)
    {
        scores[team]++;
        ArenaManager.Instance.teamScores[team] = scores[team];
        captureTheFlagHUD.UpdateScore();

        if (scores[team] >= targetScore)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ArenaNetworkManager.instan.EndGame(scores[Team.red], scores[Team.blue]);
            }
        }
    }

}
