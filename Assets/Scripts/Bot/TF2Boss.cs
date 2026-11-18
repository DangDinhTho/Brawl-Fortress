using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TF2Boss : TF2Bot
{
    public override void CreateCharacter()
    {
        viewID = photonView.ViewID;

        characterConfig = ConfigManager.GetCharacterConfig(tF2Class);
        
        var allWeapons = characterConfig.defaultWeapons;

        for (int i = 0; i < allWeapons.Length; i++)
        {
            if (allWeapons[i] != WeaponId.None)
            {
                weapons.Add((WeaponType)(i + 1), allWeapons[i]);
            }
        }
        agent.speed = characterConfig.characterStats.speed;
        
        remotePlayer = Instantiate(Resources.Load<GameObject>($"Characters/Boss/Boss_{tF2Class}"), transform).GetComponent<RemotePlayer>();
        //fPSReferences = Instantiate(Resources.Load<GameObject>($"Characters/FPS/FPS_{tF2Class}"), transform).GetComponent<FPSReferences>();
        remotePlayer.transform.localPosition = Vector3.down * 0.05f;
        //fPSReferences.transform.localPosition = Vector3.zero;

        //playerNetwork.Init();
        playerNetwork.Init();
        remotePlayer.Init(this);
        if (GetComponent<FakeShadow>() == null) gameObject.AddComponent<FakeShadow>();
        if (GetComponent<FootRing>() == null) gameObject.AddComponent<FootRing>();
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
            //ArenaManager.Instance.OnCharacterCreated(this);
            ArenaManager.Instance.OnGameStarted += OnGameStarted;
            ArenaManager.Instance.OnGameFinish += OnGameFinish;
            ArenaNetworkManager.instan.OnSwitchMasterPlayer += OnMasterSwitched;
        }

        if (photonView.IsMine)
        {
            behaviorTree.enabled = true;
        }

        healthManager.OnRespawn += OnRespawn;

        JungleModeManager.instan.boss = this;
    }


    public void OnRespawn()
    {
        if (photonView.IsMine)
        {
            agent.speed = characterConfig.characterStats.speed * 0.9f;
        }
    }

}
