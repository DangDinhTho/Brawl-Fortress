using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieModeManager : MonoBehaviour
{
    public static ZombieModeManager instan;
   
    private void Awake()
    {
        instan = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        ArenaManager.Instance.OnInitDone += InitZombie;
        ArenaManager.Instance.OnGameStarted += OnGameStarted;
        ArenaManager.Instance.OnTimeOut += OnTimeout;
    }


    public void InitZombie()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CreateRobot();
        }
    }

    public void OnGameStarted()
    {
        if (PhotonNetwork.IsMasterClient)
        {

        }
    }


    public void CreateRobot()
    {
        for (int i = 0; i < 15; i++)
        {
            CreateZombie(i);
        }
    }

    public GameObject CreateZombie(int index)
    {

        TF2Class tf2Class = (TF2Class)UnityEngine.Random.Range(1, 10);
        // if (tf2Class == TF2Class.medic) tf2Class = TF2Class.soldier;

        Transform teamPos = MapManager.instan.teamPos[1];
        Transform spawnPos = teamPos.GetChild(index % teamPos.childCount);

        string playerName = $"{tf2Class} Zombie";
        //short[] equipment = { (short)EquipmentID.iron_helmet, (short)EquipmentID.none };
        object[] info = { tf2Class, Team.blue, playerName };
        GameObject bot = PhotonNetwork.Instantiate("Zombie", spawnPos.position, spawnPos.rotation,
            0, info);
        return bot;
    }

    public void OnTimeout()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ArenaNetworkManager.instan.EndGame(0, 1);
        }
    }




}
