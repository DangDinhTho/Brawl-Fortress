using BehaviorDesigner.Runtime;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TF2Bot : TF2Player
{
    //public BotNetwork botNetwork;
    public BehaviorTree behaviorTree;
    public NavMeshAgent agent;
    public BotController botController;
    public FieldOfView fieldOfView;
    public BotPickup botPickup;

    public override void CreateCharacter()
    {
        viewID = photonView.ViewID;
        characterConfig = ConfigManager.GetCharacterConfig(tF2Class);

        // allWeapons = characterConfig.defaultWeapons;
        //
        // for (int i = 0; i < allWeapons.Length; i++)
        // {
        //     if (allWeapons[i] != WeaponId.None)
        //     {
        //         weapons.Add((WeaponType)(i + 1), allWeapons[i]);
        //     }
        // }
        agent.speed = characterConfig.characterStats.speed * 0.9f;

        remotePlayer = Instantiate(Resources.Load<GameObject>($"Characters/TPS/TPS_{tF2Class}"), transform).GetComponent<RemotePlayer>();
        //fPSReferences = Instantiate(Resources.Load<GameObject>($"Characters/FPS/FPS_{tF2Class}"), transform).GetComponent<FPSReferences>();
        remotePlayer.transform.localPosition = Vector3.zero;
        //fPSReferences.transform.localPosition = Vector3.zero;

        //playerNetwork.Init();
        playerNetwork.Init();
        remotePlayer.Init(this);
        healthManager.Init(this);
        if (photonView.IsMine)
        {
            botController.Init();
            botPickup.gameObject.SetActive(true);
        }
        else
        {
            botPickup.gameObject.SetActive(false);
        }

        fieldOfView.team = team;
        gameObject.SetActive(true);

        MeshDynamicColor[] meshDynamicColors = GetComponentsInChildren<MeshDynamicColor>(true);
        if (meshDynamicColors != null)
        {
            for (int i = 0; i < meshDynamicColors.Length; i++)
            {
                meshDynamicColors[i].SetTeamColor(team);
            }
        }

        if (ArenaManager.Instance)
        {
            ArenaManager.Instance.OnCharacterCreated(this);
            ArenaManager.Instance.OnGameStarted += OnGameStarted;
            ArenaManager.Instance.OnGameFinish += OnGameFinish;
            ArenaNetworkManager.instan.OnSwitchMasterPlayer += OnMasterSwitched;
        }

        if (photonView.IsMine)
        {
            behaviorTree.enabled = true;
        }

        StartCoroutine(WaitHeaderUI());
    }

    public void OnMasterSwitched()
    {
        //if (healthManager.death) return;
        remotePlayer.EndTaunt();
        if (PhotonNetwork.IsMasterClient)
        {
            if (playerNetwork.isMine) return;
            botController.Init();
            botPickup.gameObject.SetActive(true);
            behaviorTree.enabled = true;
        }
        else
        {
            if (!playerNetwork.isMine) return;
            botPickup.gameObject.SetActive(false);
            behaviorTree.enabled = false;
        }
        playerNetwork.Init();

    }

    public void OnGameStarted()
    {
        //behaviorTree.enabled = true;
        //behaviorTree.Start();
        //print("start");
    }
    
    public virtual void OnGameFinish()
    {
        if(!photonView.IsMine) return;
        botController.FinishFire();
        botController.enabled = false;
        behaviorTree.enabled = false;
        agent.isStopped = true;
        healthManager.enabled = false;
    }
    
    IEnumerator WaitHeaderUI()
    {
        yield return new WaitForSeconds(Random.Range(5f, 50f));
        if (!healthManager.death)
        {
            int ranIcon = Random.Range(0, UICharacterInputController.instan.chatIcons.Length);
            headerUIForFun.InitData(UICharacterInputController.instan.chatIcons[ranIcon]);
        }

        StartCoroutine(WaitHeaderUI());
    }

}
