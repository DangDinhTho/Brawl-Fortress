using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TF2Zombie : TF2Bot
{
    public override void CreateCharacter()
    {
        viewID = photonView.ViewID;

        characterConfig = Resources.Load<CharacterConfig>($"Config/Characters/Zombie_{TF2Class.demoman}");
        characterConfig.characterStats = ConfigManager.characterStats[TF2Class.demoman];


        weapons.Add(WeaponType.Mele, WeaponId.Zombie_Attack);
       

        remotePlayer = Instantiate(Resources.Load<GameObject>($"Characters/Zombies/zombie_{tF2Class}"), transform).GetComponent<RemotePlayer>();
        //fPSReferences = Instantiate(Resources.Load<GameObject>($"Characters/FPS/FPS_{tF2Class}"), transform).GetComponent<FPSReferences>();
        remotePlayer.transform.localPosition = Vector3.down * 0.05f;
        //fPSReferences.transform.localPosition = Vector3.zero;

        //playerNetwork.Init();
        playerNetwork.Init();
        remotePlayer.Init(this);
        if (GetComponent<BushStealth>() == null) gameObject.AddComponent<BushStealth>();
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
        ArenaManager.Instance.OnGameStarted += OnGameStart;
    }

    private void OnDisable()
    {
        ArenaManager.Instance.OnGameStarted -= OnGameStart;
    }


    public void OnGameStart()
    {
        if (photonView.IsMine)
        {
            //agent.speed = characterConfig.characterStats.speed * 0.22f;
            StartCoroutine(WaitSpeed());
        }
    }

    IEnumerator WaitSpeed()
    {
            agent.speed = characterConfig.characterStats.speed * 1.25f;
        yield return new WaitForSeconds(15);
        if (tF2Class == TF2Class.scout)
        {
            agent.speed = characterConfig.characterStats.speed * 0.5f;
        }
        else
            agent.speed = characterConfig.characterStats.speed * 0.18f;
    }


    public void OnRespawn()
    {
        if (photonView.IsMine)
        {
            //agent.speed = characterConfig.characterStats.speed * 0.22f;
            StartCoroutine(WaitSpeed());
        }
    }
}
