using System;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(Ragdoll))]
public class RemotePlayer : MonoBehaviour
{
    [HideInInspector]
    public TF2Player player;

    public PlayerAnimator playerAnimator;

    [HideInInspector]
    public WeaponType weaponType;
    //private WeaponType lastWeaponType;
    //private BoneCombiner boneCombiner;
    //public List<EquipmentObjectData> equipmentObjectDatas;
    public Transform remoteWeaponContainer;
    public Transform remoteBone;
    public Ragdoll ragdoll;

    [HideInInspector]
    public Weapon weapon;

    [HideInInspector]
    public Dictionary<WeaponType, Weapon> weaponSlots = new Dictionary<WeaponType, Weapon>();
    //public Weapon[] weaponsTest;

    //public GameObject boneOrg;
    //public TPSLookAt tPSLookAt;
    public AimPoint[] aimsPointInput;
    public Transform deployAim;
    private Dictionary<WeaponType, Transform> aimPoints = new Dictionary<WeaponType, Transform>();
    private Dictionary<ItemId, GameObject> equipmentItems = new Dictionary<ItemId, GameObject>();
    private Dictionary<ItemType, ItemId> itemEquipted = new Dictionary<ItemType, ItemId>();

    [HideInInspector]
    public Taunt currentTaunt;
    public Action OnTauntFinish;
    //public Action OnCurrClipEmpty;
    public BodyElement[] bodyElements;
    public Transform spine1;
    private float bodyScale = 1.5f;
    
    private void OnValidate()
    {
        playerAnimator = GetComponent<PlayerAnimator>();
        ragdoll = GetComponent<Ragdoll>();
        //tPSLookAt = GetComponent<TPSLookAt>();
    }

    private void Awake()
    {
        for(int i = 0; i < aimsPointInput.Length; i++)
        {
            aimPoints.Add(aimsPointInput[i].weaponType, aimsPointInput[i].aimPoint);
        }
        
        // boneCombiner = new BoneCombiner(remoteBone.gameObject);
        // for (int i = 0; i < equipmentObjectDatas.Count; i++)
        // {
        //     Equipt(equipmentObjectDatas[i]);
        // }
    }


    public void Init(TF2Player _player)
    {
        this.player = _player;
        
        //gameObject.SetActive(true);
        foreach (var weaponId in player.weapons)
        {
            if (weaponId.Value == WeaponId.None) continue;
            Weapon wp = Instantiate(Resources.Load<GameObject>($"Weapons/{weaponId.Value}"), remoteWeaponContainer).GetComponent<Weapon>();
            wp.transform.localPosition = Vector3.zero;
            wp.InitData(player, remoteBone, true);
            SetWeapon(wp);
            if (!weaponSlots.ContainsKey(weaponId.Key))
            {
                weaponSlots.Add(weaponId.Key, wp);
            }
            wp.gameObject.SetActive(false);
        }


        //if (!player.isMainPlayer)
        //    tPSLookAt.enabled = true;
        playerAnimator.Init(player);

        //weapon.gameObject.SetActive(true);
        player.healthManager.OnDeath += OnDeath;
        player.healthManager.OnRespawn += OnRespawn;
        this.enabled = true;
        if (!player.photonView.IsMine)
        {
            SwitchWeapon(player.characterConfig.defaultWeapon);
        }
        else
        {
            weapon = weaponSlots[player.characterConfig.defaultWeapon];
        }

        foreach (var itemEquipt in player.itemEquipts)
        {
            Equipt(itemEquipt.Value, itemEquipt.Key);
        }
    }

    public void SetWeapon(Weapon weapon)
    {
        //weapon.OnBeginFire += OnWeaponStartFire;
        weapon.OnFire += OnFire ;
        if(weapon as Gun)
        {
            Gun gun = (Gun)weapon;
            gun.OnStartReload += OnReload;
            gun.OnFinishReload += OnFinishReload;
            //gun.OnClipEmpty += OnClipEmpty;
        }
        //weapon.OnFinishFire += OnWeaponFinishFire;
    }

    // Update is called once per frame
   // void Update()
   // {
       // if(player == null || player.healthManager.death) return;
        //CheckWeaponChanged();
   // }



    public void SwitchWeapon(WeaponType _weaponType)
    {
        if (weaponType != WeaponType.None)
            weaponSlots[weaponType].HideWeapon();
        weaponType = _weaponType;
        weaponSlots[_weaponType].gameObject.SetActive(true);
        weapon = weaponSlots[_weaponType];
        playerAnimator.SwitchWeapon(_weaponType);
        //if (aimPoints.TryGetValue(_weaponType, out var target))
        //    tPSLookAt.aim = target;
        //else
        //    tPSLookAt.aim = aimPoints[WeaponType.Mele];
    }

    public void OnDeployed()
    {
        playerAnimator.WeaponDeployedAnimator();
        //if(deployAim != null)
        //{
        //    tPSLookAt.aim = deployAim;
        //}
    }

    public void OnFinishDeploy()
    {
        playerAnimator.WeaponDeployFinishAnimator();
        //tPSLookAt.aim = aimPoints[weaponType];
    }

    public void OnDeath()
    {
        //if(!gameObject.activeSelf) return;
        weapon.OnDeath();
    }

    public void OnRespawn()
    {
        //if(!gameObject.activeSelf) return;
        weapon.OnRespawn();
    }


    //For Bot
    public void OnFire()
    {
        playerAnimator.FireAnimator();
    }

    public void OnReload()
    {
        if(player as TF2Bot)
        {
            ((TF2Bot)player).botController.fpsState = PlayerFPState.Reloading;
        }
        playerAnimator.ReloadAnimator();
    }

    public void OnFinishReload()
    {
        if (player as TF2Bot)
        {
            ((TF2Bot)player).botController.fpsState = PlayerFPState.Idle;
        }
        //playerAnimator.ReloadAnimator();
    }
    

    public void Equipt(ItemId itemId, ItemType slot = ItemType.None)
    {
        ItemConfig itemConfig = ConfigManager.GetItemConfig(itemId);
        if (slot == ItemType.None)
        {
            if (itemConfig != null)
                slot = itemConfig.slot;
        }

        
        if (itemEquipted.TryGetValue(slot, out var otherItem) && otherItem != itemId)
        {
            Unequip(otherItem);
        }
        
        if (equipmentItems.TryGetValue(itemId, out var item))
        {
            item.SetActive(true);
        }
        else
        {
            GameObject itemPref = Resources.Load<GameObject>($"WorkShop/Items/{itemId}");
            if (itemPref == null) return;
            item = Instantiate(itemPref, remoteWeaponContainer);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.Euler(0, 0, 0);
           // item.transform.localScale = Vector3.one * bodyScale;
            EquipmentItem equipmentItem = item.GetComponent<EquipmentItem>();

            Team team = player != null ? player.team : HomeManager.instance.team;
            equipmentItem.Init(remoteBone, team);
            equipmentItems.Add(itemId, item);
            
        }
        
        if (itemEquipted.ContainsKey(slot))
        {
            itemEquipted[slot] = itemId;
        }
        else
        {
            itemEquipted.Add(slot, itemId);
        }

        if (itemConfig != null && itemConfig.itemProperties != null && itemConfig.itemProperties.off != null)
        {
            for(int i = 0; i < itemConfig.itemProperties.off.Length; i++)
            {
                if(bodyElements.Length > itemConfig.itemProperties.off[i])
                {
                    bodyElements[itemConfig.itemProperties.off[i]].SetActtive(false);
                }

            }
            
        }
    }

    public void Unequip(ItemId itemId, bool dontDestroy = true)
    {
        if (equipmentItems.ContainsKey(itemId))
        {
            if (dontDestroy)
            {
                equipmentItems[itemId].SetActive(false);
            }
            else
            {
                Destroy(equipmentItems[itemId]);
                equipmentItems.Remove(itemId);
            }
        }

        ItemConfig itemConfig = ConfigManager.GetItemConfig(itemId);
        if (itemConfig != null && itemConfig.itemProperties != null && itemConfig.itemProperties.off != null)
        {
            for (int i = 0; i < itemConfig.itemProperties.off.Length; i++)
            {
                if (bodyElements.Length > itemConfig.itemProperties.off[i])
                {
                    bodyElements[itemConfig.itemProperties.off[i]].SetActtive(true);
                }
            }

        }
    }

    public void Unequip(ItemType slot, bool dontDestroy = true)
    {
        if (itemEquipted.TryGetValue(slot, out var otherItem))
        {
            Unequip(otherItem, dontDestroy);
        }
    }

    private bool tauntEnding;
    private Taunt nextTaunt;
    private Action nexttauntCallBack;
    private void PlayNextTaunt()
    {
        if (nextTaunt == Taunt.None || currentTaunt != Taunt.None) return;
        PlayTaunt(nextTaunt, nexttauntCallBack);
        nextTaunt = Taunt.None;
        nexttauntCallBack = null;
    }


    public void PlayTaunt(Taunt taunt, Action CallOnFinish = null)
    {
        if (taunt == currentTaunt) return;
        if (currentTaunt != Taunt.None)
        {
            nextTaunt = taunt;
            nexttauntCallBack = CallOnFinish;
            if (tauntEnding) return;
            OnTauntFinish += PlayNextTaunt;
            EndTaunt();
            return;
        }
        
        this.currentTaunt = taunt;
        //tPSLookAt.enabled = false;

        if (CallOnFinish != null) OnTauntFinish += CallOnFinish;
        if (player != null)
        {
            player.healthManager.OnDeath += EndTaunt;

            if (player.photonView.IsMine)
            {
                player.playerNetwork.SyncTaunt(true, taunt);
            }
        }

        StartCoroutine(PlayTauntLate(taunt));
    }

    public void EndTaunt()
    {
        if(currentTaunt == Taunt.None || tauntEnding) return;
        tauntEnding = true;
        TauntConfig config = ConfigManager.GetTauntConfig(currentTaunt);
        playerAnimator.EndTaunt();
        
        

        var audioSource = transform.GetComponentInChildren<AudioSource>();
        if (audioSource != null) audioSource.Stop();

        if (gameObject.activeInHierarchy)
            StartCoroutine(EndTauntLate(config));

        if (player != null)
        {
            player.healthManager.OnDeath -= EndTaunt;
            if (player.photonView.IsMine)
            {
                player.playerNetwork.SyncTaunt(false, currentTaunt);
            }
        }
        
    }

    IEnumerator PlayTauntLate(Taunt taunt)
    {
        yield return new WaitForSeconds(0.1f);
        TauntConfig config = ConfigManager.GetTauntConfig(taunt);
        if (config.equipItemId != ItemId.None)
        {
            Equipt(config.equipItemId);
        }

        if (config.WeaponId == WeaponId.None)
        {
            if (weapon != null)
                weapon.HideWeapon();
            if(player == null)
            {
                HideWeaponItem();
            }
        }
        else
        {
            //wea = config.WeaponId;
        }
        playerAnimator.PlayTaunt(taunt);
        if (player != null && config.audio != null)
        {
            player.audioSource.Stop();
            player.audioSource.PlayOneShot(config.audio);
            player.audioSource.loop = config.loop;
        }
        if (!config.loop)
        {
            yield return new WaitForSeconds(config.time);
            EndTaunt();
        }
    }

    IEnumerator EndTauntLate(TauntConfig config)
    {
        if (player == null || !player.healthManager.death)
        {
            if (config.finishTime < 1)
            {
                if (config.equipItemId != ItemId.None)
                {
                    Unequip(config.equipItemId);
                }
                yield return new WaitForSeconds(config.finishTime);
            }
            else
            {
                yield return new WaitForSeconds(config.finishTime - 0.5f);
                if (config.equipItemId != ItemId.None)
                {
                    Unequip(config.equipItemId);
                }
                yield return new WaitForSeconds(0.5f);
            }
            
        }
        else if (config.equipItemId != ItemId.None)
        {
            Unequip(config.equipItemId);
        }

        if (config.WeaponId == WeaponId.None)
        {
            if (player != null && weapon != null)
                weapon.ShowWeapon();
            else if (player == null)
                SwitchWeaponItem(itemWeaponShow);
        }

        if(player != null && config.audio != null)
        {
            player.audioSource.Stop();
        }

        currentTaunt = Taunt.None;
        //if (player != null && !player.isMainPlayer)
        //    tPSLookAt.enabled = true;
        OnTauntFinish?.Invoke();
        OnTauntFinish = null;
        tauntEnding = false;
    }

    private ItemType itemWeaponShow;
    public void SwitchWeaponItem(ItemType itemType)
    {
        itemWeaponShow = itemType;
        WeaponType wpt = (WeaponType)Enum.Parse(typeof(WeaponType), itemType.ToString()); 
        playerAnimator.SwitchWeapon(wpt);
        foreach(var item in itemEquipted)
        {
            if(item.Key > ItemType.WEAPON)
            {
                var itObj = equipmentItems[item.Value];
                itObj.SetActive(item.Key == itemType);
            }
        }
    }

    public void HideWeaponItem()
    {
        foreach (var item in itemEquipted)
        {
            if (item.Key > ItemType.WEAPON)
            {
                var itObj = equipmentItems[item.Value];
                itObj.SetActive(false);
            }
        }
    }

    public void SetMeshTeam(Team _team)
    {
        MeshDynamicColor[] meshDynamicColors = GetComponentsInChildren<MeshDynamicColor>(true);
        if (meshDynamicColors != null)
        {
            for (int i = 0; i < meshDynamicColors.Length; i++)
            {
                meshDynamicColors[i].SetTeamColor(_team);
            }
        }
    }

    private void LateUpdate()
    {
        if (spine1)
        {
            spine1.localScale = Vector3.one * bodyScale;
        }
    }
}

[System.Serializable]
public class AimPoint
{
    public WeaponType weaponType;
    public Transform aimPoint;
}

[System.Serializable]
public class BodyElement
{
    public GameObject[] lods;
    public void SetActtive(bool active)
    {
        for(int i = 0; i < lods.Length; i++)
        {
            lods[i].SetActive(active);
        }
    }
}
