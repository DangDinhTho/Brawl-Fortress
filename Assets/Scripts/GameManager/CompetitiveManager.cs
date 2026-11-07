using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompetitiveManager : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        ArenaManager.Instance.OnTimeOut += EndGame;
    }

    public void EndGame()
    {
        int[] scores = new int[2];

        if (PhotonNetwork.IsMasterClient)
        {
            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < ArenaManager.Instance.teamMembers[(Team)i].Count; j++)
                {
                    scores[i] += (int)ArenaManager.Instance.characterScores[ArenaManager.Instance.teamMembers[(Team)i][j].viewID].CaculateScore();
                }
                if (scores[i] < 0) scores[i] = 0;
            }
            ArenaNetworkManager.instan.EndGame(scores[0], scores[1]);
        }
       
    }
}
