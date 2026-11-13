using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HomeManager : MonoBehaviour
{
    public static HomeManager instance;

    [HideInInspector]
    public CharacterPref characterPref;
    public Action OnCharavterPrefChanged;
    public Transform characterContainer;
    [HideInInspector]
    public RemotePlayer remotePlayer;
    [HideInInspector]
    public Team team;

    public Image landingPage;
    public Image landingCharacter;

    public Image playerMedalIcon;
    public Text txtPlayerName;
    [Tooltip("Optional: shows the player level (ConfigManager.GetLevel)")]
    public Text txtPlayerLevel;
    [Tooltip("Optional: progress to the next level")]
    public Slider playerLevelProgress;
    public ResourceManager resourceManager;

    public CraftResultController craftResultController;
    public AudioSource audioSource;
    public GameObject removeAdsBtn;

    private void Awake()
    {
        instance = this;
    }
    
    

    private void Start()
    {
        if (ConfigManager.iap)
        {
            IAPManager.instan.InitializePurchasing();
        }
        //Sprite landing = Resources.Load<Sprite>($"Landing/Page/{ConfigManager.landingPage}");
        //if (landing != null)
        //    landingPage.sprite = landing;
        //Sprite ladingChar = Resources.Load<Sprite>($"Landing/Character/{ConfigManager.landingCharacter}");
        //if (ladingChar != null)
        //    landingCharacter.sprite = ladingChar;

        removeAdsBtn.SetActive(!GameManager.instan.userData.ads_removed);

        Medal playerMedal = ConfigManager.GetMedal(GameManager.instan.userData.score);
        playerMedalIcon.sprite = GUIManager.instan.iconAtlas.GetSprite(playerMedal.ToString());
        txtPlayerName.text = GameManager.instan.userData.userName;
        if (txtPlayerLevel != null) txtPlayerLevel.text = GameManager.instan.userData.Level.ToString(); // number inside the level star badge
        if (playerLevelProgress != null) playerLevelProgress.value = GameManager.instan.userData.LevelProgress;

        resourceManager.Init();

        team = CharacterSave.GetLastTeam();
        characterPref = CharacterSave.getMainCharacterPref();
        //ConfigManager.itemConfigs.Add(ItemId.demoman_bruces_bonnet, new ItemConfig() { itemId = ItemId.demoman_bruces_bonnet, slot = ItemType.helmet, tf2Classes = new List<TF2Class> { TF2Class.demoman } });
        CreateCharacter();

        //List<MedalExp> ms = new List<MedalExp>() { new MedalExp() { medal = Medal.iron_ugc, exp = 200 }, new MedalExp() { medal = Medal.iron_1, exp = 500 } };
        //Debug.Log(JsonConvert.SerializeObject(ms));
        StartCoroutine(CreatePopupLate());
        //Yodo1AdsManager.instan.ResetVideoShowed();
        AdsManager.instan.ResetAdsCount();
        AdsManager.instan.HideNavtiveAds();
        AdsManager.instan.LoadNavtiveAds();
    }
    
    

    IEnumerator CreatePopupLate()
    {
        yield return new WaitForSeconds(1f);
        //if (!GameManager.instan.QCShowed && ConfigManager.QcDatas != null)
        //{
            //GUIManager.instan.CreatePopup<PopupQC>();
            //GameManager.instan.QCShowed = true;
        //}
        if (GameManager.instan.userData.dayLast != GameManager.instan.toDayOfTheYear && GameManager.instan.userData.mocDaNhan < 7 && GameManager.instan.played)
        {
            GUIManager.instan.CreatePopup<PopupDailyReward>();
        }

        // Game modes unlocked by level since the last visit to Home (ModeUnlock): one popup each, on top of the others
        var unlocked = ModeUnlock.TakeNewlyUnlocked();
        if (unlocked.Count > 0) ModeUnlock.ShowUnlockedPopups(unlocked);
        
    }

    public void SetNoAdsBtn()
    {
        removeAdsBtn.SetActive(!GameManager.instan.userData.ads_removed);
    }


    public void OpenSelectClassPanelClick()
    {
        GUIManager.instan.CreatePopup<PopupSelectClass>();
    }

    public void SetCharacterClass(TF2Class _tf2Class)
    {
        //if (characterPref.tF2Class == _tf2Class) return;

        characterPref = CharacterSave.GetCharacterPref(_tf2Class);
        OnCharavterPrefChanged?.Invoke();

        CreateCharacter();
    }

    public void OpenLoadout()
    {
        GUIManager.instan.CreatePopup<PopupLoadout>();
    }

    public void BuyRemoveAdsClick()
    {
        IAPManager.instan.BuyProductID(ItemId.brawl_valley_remove_ads.ToString());
    }


    public void OpenWorkShop()
    {
        GUIManager.instan.CreatePopup<PopupWorkShop>();
        //ItemData itemData = new ItemData()
        //{
        //    itemId = ItemId.demoman_bruces_bonnet,
        //    quantity = 1
        //};

        //VirtualCurrncy currency = new VirtualCurrncy()
        //{
        //    currency = TF2Currency.MM,
        //    quantity = 10
        //};
        ////
        //// ItemProperties p = new ItemProperties()
        //// {
        ////     resources = new ItemData[] { itemData },
        ////     currncies = new VirtualCurrncy[] { currency },
        ////     chance = 80,
        ////     mainCurrency = TF2Currency.MM
        //// };
        ////
        //// string json = JsonConvert.SerializeObject(p);
        //// print(json);
        //RewardData rewardData = new RewardData()
        //{
        //    itemDatas = new List<ItemData>() { itemData },
        //    virtualCurrncies = new List<VirtualCurrncy> { currency },

        //};

       // GUIManager.instan.CreatePopup<PopupReward>(rewardData);
    }
    public void OpenCraft()
    {
        GUIManager.instan.CreatePopup<PopupCrafting>();
    }

    public void CreateCharacter()
    {
        if (remotePlayer != null) Destroy(remotePlayer.gameObject);
        remotePlayer = Instantiate(Resources.Load<GameObject>($"Characters/TPS/TPS_{characterPref.tF2Class}"), characterContainer).GetComponent<RemotePlayer>();
        remotePlayer.transform.localPosition = Vector3.zero;
        remotePlayer.SetMeshTeam(team);
        if (characterPref.itemLoadOut != null)
        {
            CharacterConfig characterConfig = ConfigManager.GetCharacterConfig(characterPref.tF2Class);
            ItemType itemWeaponDefault = (ItemType)Enum.Parse(typeof(ItemType), characterConfig.defaultWeapon.ToString());
            //remotePlayer.Equipt(characterPref.itemLoadOut[itemWeaponDefault]);
            
            foreach (var loadoutItem in characterPref.itemLoadOut)
            {
                //if(loadoutItem.Key >= ItemType.WEAPON) continue;
                remotePlayer.Equipt(loadoutItem.Value, loadoutItem.Key);
            }

            remotePlayer.SwitchWeaponItem(itemWeaponDefault);
        }
    }

    public void ResetCharacterRotate()
    {
        characterContainer.rotation = Quaternion.Euler(0, 0, 0);
    }

    public void OnQuickPlayerClick()
    {
        PopupSelectMap popupSelectMap = GUIManager.instan.CreatePopup<PopupSelectMap>();
        popupSelectMap.SetSinglePlayer();
    }

    public void OnWatchAdsClick()
    {
        GUIManager.instan.CreatePopup<PopupConfirm>(LocalizationManager.Localize("watch_ads_confirm"), (Action)WatchAds,
            GUIManager.instan.iconAtlas.GetSprite("ads"));
        //Dismiss();
    }

    public void WatchAds()
    {
        AdsManager.instan.ShowRewardAds(OnShowRewardAdsDone);
        //Yodo1AdsManager.instan.ShowGoldkeyHomeReward(OnShowRewardAdsDone);
        //     ItemId itemId = ItemId.pack_first_purchase;
        //     ItemConfig it = ConfigManager.GetItemConfig(itemId);
        //     //if(itemConfig == null) return;
        //     //CatalogItem it = itemConfig.catalogItem; //GameManager.instan.ca.Find(i => i.ItemId.Equals(e.purchasedProduct.definition.id));
        //     if (it != null && it.slot == ItemType.Pack)
        //     {
        //         PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        //         {

        //             GeneratePlayStreamEvent = true,
        //             FunctionName = "BuyPack",
        //             FunctionParameter = new { itemId = it.itemId.ToString() }

        //         }, result2 =>
        //         {
        //             GameManager.instan.GetUserInventory();
        //             RewardData rewardData = new RewardData()
        //             {
        //                 virtualCurrncies = new List<VirtualCurrncy>(it.itemProperties.packData.currncies),
        //                 itemDatas = new List<ItemData>(it.itemProperties.packData.itemDatas),
        //             };
        //             GUIManager.instan.CreatePopup<PopupReward>(rewardData);
        //             //GameManagerAll.instan.user.diamond += int.Parse(it.Description);
        //             //string _title = $"successfully purchase\n{it.DisplayName}";
        //             //HomeManager.instan.getPanel.Create(_title, LayoutManagerAll.instan.packIcon);
        //         },
        //error => Debug.Log(error.GenerateErrorReport()));
        //     }
    }

    public void OnShowRewardAdsDone()
    {
        GameManager.instan.AddRewardFromAds();
    }
}
