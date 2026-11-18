using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JungleModeManager : MonoBehaviour
{
    public static JungleModeManager instan;
    
    public TF2Class bossType;

    [HideInInspector]
    public TF2Boss boss;

    private void Awake()
    {
        instan = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        ArenaManager.Instance.OnInitDone += InitBoss;
        ArenaManager.Instance.OnGameStarted += OnGameStarted;
        ArenaManager.Instance.OnGameFinish += OnGameFinish;
        UIMainHud.instan.CreateHUD<JungleBossHUD>();

        ArenaManager.Instance.delayShowScoreBoardEndGame = 5;
    }

    public void InitBoss()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CreateBoss();
        }
    }

    public void OnGameStarted()
    {
        if (PhotonNetwork.IsMasterClient)
        {

        }
    }

    public void OnGameFinish()
    {
        //if(ArenaManager.Instance.teamScores[Team.red] < ArenaManager.Instance.teamScores[Team.blue])
        //{
        CameraController.instan.SetFreeLook(true, boss.transform, boss.healthManager.centerBody);
        if (!boss.healthManager.death)
        {
            List<Taunt> taunts = new List<Taunt>();
            for (Taunt taunt = Taunt.None; taunt < Taunt.Max; taunt++)
            {
                if (taunt.ToString().StartsWith(boss.tF2Class.ToString()))
                {
                    taunts.Add(taunt);
                }
            }
            int ranTaunt = Random.Range(0, taunts.Count);
            boss.remotePlayer.playerAnimator.Animator.SetBool("GameFinished", true);
            boss.remotePlayer.PlayTaunt(taunts[ranTaunt]);
        }
        //}
    }

    public GameObject CreateBoss()
    {

        Transform teamPos = MapManager.instan.teamPos[1];
        Transform spawnPos = teamPos.GetChild(Random.Range(0, teamPos.childCount));

        var playerName = "Hale";
        object[] info = { bossType, Team.blue, playerName };
        GameObject bot = PhotonNetwork.Instantiate("Boss_Hale", spawnPos.position, spawnPos.rotation, 0, info);
        return bot;
    }
}
