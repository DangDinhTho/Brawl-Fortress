using System;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using DG.Tweening;

public class TF2Player : MonoBehaviour
{
    //[HideInInspector]
    public CharacterConfig characterConfig;
    [HideInInspector]
    public Vector3 targetLook;
    [HideInInspector]
    public RemotePlayer remotePlayer;
    public PlayerFPSController FPSPlayer;
    public PlayerNetwork playerNetwork;
    //public WeaponId[] allWeapons;
    [HideInInspector] public Dictionary<WeaponType, WeaponId> weapons = new Dictionary<WeaponType, WeaponId>();
    public HealthManager healthManager;
    //public FPSReferences fPSReferences;
    public Team team;
    public int viewID;
    public TF2Class tF2Class;
    public PhotonView photonView;
    public string playerName;
    public bool isMainPlayer;
    public bool isBot;
    public AudioSource audioSource;
    protected Vector3 defaultPosition;
    public Dictionary<ItemType, ItemId> itemEquipts = new Dictionary<ItemType, ItemId>();
    public AudioClip[] stepSounds;
    //public GameObject blockShadow;
    //[HideInInspector]
    //public bool usingBlockShadow;
    public HeaderUIForFun headerUIForFun;

    private void Start()
    {
        if (photonView.InstantiationData != null)
        {
            tF2Class = (TF2Class)photonView.InstantiationData[0];
            team = (Team)photonView.InstantiationData[1];
            playerName = (string)photonView.InstantiationData[2];
            if (photonView.InstantiationData.Length > 3)
            {
                ItemType[] equipSlot = (ItemType[])photonView.InstantiationData[3];
                ItemId[] itemIds = (ItemId[])photonView.InstantiationData[4];
                for (int i = 0; i < equipSlot.Length; i++)
                {
                    if (equipSlot[i] < ItemType.WEAPON)
                    {
                        itemEquipts.Add(equipSlot[i], itemIds[i]);
                    }
                    else if(equipSlot[i] < ItemType.CURRENCY)
                    {
                        WeaponType weaponType = (WeaponType)Enum.Parse(typeof(WeaponType), equipSlot[i].ToString());
                        WeaponId weaponId = (WeaponId)Enum.Parse(typeof(WeaponId), itemIds[i].ToString());
                        weapons.Add(weaponType, weaponId);
                    }
                }
                Debug.Log(tF2Class);
            }

        }

        defaultPosition = transform.position;
        CreateCharacter();
        
        if(this is not TF2Robot) 
        {
            StartCoroutine(WaitPlayRandomVoice());
        }

        //SettingHelper.OnGraphicSettingUpdate += OnGraphicSettingChanged;
        //int graphicQuality = SettingHelper.GetGraphicValue();
        //OnGraphicSettingChanged(graphicQuality);
        healthManager.OnTakeDamage += OnPlayerTakeDamage;
    }

    //private void OnDestroy()
    //{
    //    SettingHelper.OnGraphicSettingUpdate -= OnGraphicSettingChanged;
    //}

    //public void OnGraphicSettingChanged(int quality)
    //{
    //    if (quality < 3)
    //    {
    //        usingBlockShadow = true;
    //        SetShadow(true);
    //    }
    //    else
    //    {
    //        usingBlockShadow = false;
    //        SetShadow(false);
    //    }
    //}

    //public void SetShadow(bool state)
    //{
    //    if (!usingBlockShadow)
    //    {
    //        if(blockShadow.activeSelf) blockShadow.SetActive(false);
    //        return;
    //    }
    //    blockShadow.SetActive(state);
    //}

    public void PlayFootStep()
    {
        if (healthManager.death) return;
        audioSource.PlayOneShot(stepSounds[Random.Range(0, stepSounds.Length)], 0.5f);
    }

    public virtual void CreateCharacter()
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

        remotePlayer = Instantiate(Resources.Load<GameObject>($"Characters/TPS/TPS_{tF2Class}"), transform).GetComponent<RemotePlayer>();
        //fPSReferences = Instantiate(Resources.Load<GameObject>($"Characters/FPS/FPS_{tF2Class}"), transform).GetComponent<FPSReferences>();
        remotePlayer.transform.localPosition = Vector3.zero;
        //fPSReferences.transform.localPosition = Vector3.zero;

        playerNetwork.Init();
        remotePlayer.Init(this);

        if (!isBot)
        {
            BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.enabled = !photonView.IsMine;
            }
        }

        healthManager.Init(this);

        if(FPSPlayer != null)
        {
            FPSPlayer.Init();
        }

        gameObject.SetActive(true);
        MeshDynamicColor[] meshDynamicColors = GetComponentsInChildren<MeshDynamicColor>(true);
        if(meshDynamicColors != null)
        {
            for(int i = 0; i < meshDynamicColors.Length; i++)
            {
                meshDynamicColors[i].SetTeamColor(team);
            }
        }

        if (ArenaManager.Instance) 
            ArenaManager.Instance.OnCharacterCreated(this);

    }

    public void BackToDefaultPos()
    {
        transform.position = defaultPosition;
    }


    public void SetMeshTeam(Team _team = Team.None)
    {
        if (_team == Team.None) _team = team;
        MeshDynamicColor[] meshDynamicColors = GetComponentsInChildren<MeshDynamicColor>(true);
        if (meshDynamicColors != null)
        {
            for (int i = 0; i < meshDynamicColors.Length; i++)
            {
                meshDynamicColors[i].SetTeamColor(_team);
            }
        }
    }

    public void SetMeshInvsibility()
    {
        Material[] materials = new Material[1];
        if (team == ArenaManager.Instance.mainPlayer.team)
        {
            materials[0] = Resources.Load<Material>("Materials/InvisibilityMat_mate");
        }
        else
        {
            materials[0] = Resources.Load<Material>("Materials/InvisibilityMat");
        }
        MeshDynamicColor[] meshDynamicColors = GetComponentsInChildren<MeshDynamicColor>(true);
        if (meshDynamicColors != null)
        {
            for (int i = 0; i < meshDynamicColors.Length; i++)
            {
                meshDynamicColors[i].SetInvisibility(materials);
            }
        }
    }

    public virtual void OnGameFinish()
    {
        if(!photonView.IsMine) return;
        FPSPlayer.currWeapon.FinishFire();
        FPSPlayer.enabled = false;
    }

    IEnumerator WaitPlayRandomVoice()
    {
        yield return new WaitForSeconds(Random.Range(1f, 30f));
        if (!healthManager.death)
        {
            AudioClip voice = characterConfig.voices[Random.Range(0, characterConfig.voices.Length)];
            audioSource.PlayOneShot(voice);
        }

        StartCoroutine(WaitPlayRandomVoice());
    }

    public void InitMainPlayer()
    {
        healthManager.OnDeath += OnPlayerDeath;
        healthManager.OnRespawn += OnPlayerRespawn;
    }

    public void OnPlayerDeath()
    {
        FPSPlayer.enabled = false;
        if (ConfigManager.showInterDie)
        {
            DOVirtual.DelayedCall(3, () => AdsManager.instan.ShowInterAds(null, true));
        }
        else
            AdsManager.instan.ShowNavtiveAds();
    }

    public void OnPlayerRespawn()
    {
        FPSPlayer.enabled = true;
        AdsManager.instan.HideNavtiveAds();
    }

    public void OnPlayerTakeDamage(Vector3 direction)
    {
        if (characterConfig.takeDamageVoices != null)
            audioSource.PlayOneShot(characterConfig.takeDamageVoices[Random.Range(0, characterConfig.takeDamageVoices.Length)], 1.5f);
    }
}

[System.Serializable]
public class WeaponSlot
{
    public WeaponType weaponType;
    public WeaponId weaponId;
}