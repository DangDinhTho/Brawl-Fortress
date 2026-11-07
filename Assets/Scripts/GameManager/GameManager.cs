using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instan;
    [HideInInspector]
    public UserData userData = new UserData();
    //[HideInInspector]
    //public List<CatalogItem> allItems;
    [HideInInspector]
    public List<string> keyProductIds;
    //[HideInInspector]
    //public List<string> reciptCodeAccept = new List<string>();
    [HideInInspector]
    public int toDayOfTheYear;
    [HideInInspector]
    public bool QCShowed;
    [HideInInspector]
    public bool played;

    [HideInInspector] public DateTime today;
    public FirebaseController firebaseController;

    public static int matchCount
    {
        get => PlayerPrefs.GetInt("MATCH_COUNT", 0);
        set => PlayerPrefs.SetInt("MATCH_COUNT", value);
    }
    private void Awake()
    {
        instan = this;
        DontDestroyOnLoad(this.gameObject);
    }
    
    public void SetFPSTarget(int value)
    {
        Application.targetFrameRate = value;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        int quanlityLevel = SettingHelper.GetGraphicValue();
        if (quanlityLevel == 4) quanlityLevel = 5;
        QualitySettings.SetQualityLevel(quanlityLevel);
        SetFPSTarget(SettingHelper.GetFPSValue());
        SettingHelper.OnFPSUpdate += SetFPSTarget;
        
        LocalizationManager.Read();
        string language = SettingHelper.GetLanguage();
        LocalizationManager.Language = language;
        Debug.Log(DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds());
    }

    public void GetUserInventory(Action callBack = null)
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            List<ItemInstance> itemInstances = result.Inventory;
            userData.userItems.Clear();
            for (int i = 0; i < itemInstances.Count; i++)
            {
                UserItem item = ConfigManager.CreateUserItem(itemInstances[i]);
                if (item.itemId == ItemId.brawl_valley_remove_ads) userData.ads_removed = true;
                userData.userItems.Add(item);
            }
            userData.currency.UpdateVitualCurrency(result.VirtualCurrency["GK"], result.VirtualCurrency["CM"], result.VirtualCurrency["MM"]);
            if (HomeManager.instance != null)
            {
                HomeManager.instance.SetNoAdsBtn();
            }
            callBack?.Invoke();

        }, error => Debug.Log(error.GenerateErrorReport()));
    }

    public void GetCatalogs()
    {
        PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest(), result =>
        {
            var allItems = result.Catalog;
            //allItems.Sort((a, b) => (a.ItemId.CompareTo(b.ItemId)));

            foreach (var item in allItems)
            {
                ItemId itemId;
                if (Enum.TryParse(item.ItemId, true, out itemId))
                {

                    ItemType itemType = (ItemType)Enum.Parse(typeof(ItemType), item.ItemClass);
                    List<TF2Class> tf2Classes = null;
                    if (itemType == ItemType.Pack || itemType == ItemType.remove_ads)
                    {
                        keyProductIds.Add(item.ItemId);
                    }
                    else if (itemType == ItemType.ticket)
                    {

                    }
                    else
                    {
                        tf2Classes = new List<TF2Class>();
                        for (int i = 0; i < item.Tags.Count; i++)
                        {
                            TF2Class tf2cl = (TF2Class)Enum.Parse(typeof(TF2Class), item.Tags[i]);
                            tf2Classes.Add(tf2cl);
                        }
                    }

                    ItemProperties itemProperties = null;
                    if (!string.IsNullOrEmpty(item.CustomData)) itemProperties = JsonConvert.DeserializeObject<ItemProperties>(item.CustomData);
                    var itemCfg = new ItemConfig()
                    {
                        itemId = itemId, //(ItemId)Enum.Parse(typeof(ItemId), item.ItemId),
                        slot = (ItemType)Enum.Parse(typeof(ItemType), item.ItemClass),
                        tf2Classes = tf2Classes,
                        catalogItem = item,
                        itemProperties = itemProperties
                    };

                    if (!ConfigManager.itemConfigs.ContainsKey(itemCfg.itemId))
                        ConfigManager.itemConfigs.Add(itemCfg.itemId, itemCfg);
                }
            }

            //for(ItemId i = ItemId.WEAPON; i < ItemId.END_WEAPON; i++)
            //{
            //    var itemCfg = new ItemConfig()
            //    {
            //        itemId = i,
            //        slot = ItemType.WEAPON
            //    };

            //    ConfigManager.itemConfigs.Add(itemCfg.itemId, itemCfg);
            //}

            //IAPManager.instan.InitializePurchasing();

            if (LoginManager.instan != null)
            {
                LoginManager.instan.loadedCategory = true;
            }

        }, error => Debug.Log(error.GenerateErrorReport()));
    }


    public void OnStartMatch()
    {
        matchCount++;
        if (matchCount == 1 || matchCount == 10 || matchCount == 50 || matchCount == 100)
        {
            firebaseController.LogEvent($"match_count_{matchCount}");
        }
        firebaseController.LogEvent("match", "number_match", matchCount.ToString());
    }

    public void GetUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest()
        {
            PlayFabId = userData.userId,
            Keys = null
        }, result =>
        {
            if (result.Data.ContainsKey("day_last"))
            {
                userData.dayLast = int.Parse(result.Data["day_last"].Value);
            }
            else
            {
                userData.dayLast = -1;
            }

            if (result.Data.ContainsKey("moc_da_nhan"))
            {
                userData.mocDaNhan = int.Parse(result.Data["moc_da_nhan"].Value);

            }
            else
            {
                userData.mocDaNhan = 0;
            }
        }, (error) =>
        {
            Debug.Log("Got error retrieving user data:");
            Debug.Log(error.GenerateErrorReport());
        });
    }

    private bool checkDailyrewarded;
    public void checkDailyReward()
    {
        if (!checkDailyrewarded)
        {
            PlayFabClientAPI.GetTime(new GetTimeRequest(), OnGetTimeSuccess, LogFailure);
            checkDailyrewarded = true;
        }
    }

    public void GetServerTime(Action<DateTime> callback)
    {
        PlayFabClientAPI.GetTime(new GetTimeRequest(),
            (result) =>
            {
                OnGetTimeSuccess(result);
                callback?.Invoke(result.Time);
            } 
            , LogFailure);
    }

    void OnGetTimeSuccess(GetTimeResult result)
    {
        Debug.Log("The time is: " + result.Time.DayOfYear);
        toDayOfTheYear = result.Time.DayOfYear;
        today = result.Time;
    }

    void LogFailure(PlayFabError error)
    {
        Debug.Log("There was a problem getting the time. Error: " + error.GenerateErrorReport());
    }
    public void RequestAddDailyReward(int mocNhan, Action<RewardData> callBack)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {

            GeneratePlayStreamEvent = true,
            FunctionName = "AddDailyReward",
            FunctionParameter = new { inputValue = mocNhan.ToString() }

        }, result =>
        {
            GetUserInventory();
            JsonObject jsonResult = (JsonObject)result.FunctionResult;
            object rewardJson;
            jsonResult.TryGetValue("messageValue", out rewardJson);
            Debug.Log(rewardJson);

            RewardData rewardData = JsonConvert.DeserializeObject<RewardData>(rewardJson.ToString());
            callBack?.Invoke(rewardData);
        },
       error => Debug.Log(error.GenerateErrorReport()));

    }

    public void RecieveDailyReward(int dayLast, int mocNhan, Action<RewardData> callBack)
    {
        userData.dayLast = dayLast;
        userData.mocDaNhan = mocNhan;

        SetDayLast(dayLast, mocNhan);
        RequestAddDailyReward(mocNhan, callBack);
    }

    private void SetDayLast(int daylast, int mocDaNhan)
    {
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>()
            {
                {"day_last", daylast.ToString()},
                {"moc_da_nhan", mocDaNhan.ToString()}
            }
        },
        result =>
        {
        },
        error =>
        {

        });
    }
    
    


    public void BuyItem(ItemId itemId, Action<ItemId> CallBack = null)
    {
        ItemConfig itemConfig = ConfigManager.GetItemConfig(itemId);
        PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest()
        {
            ItemId = itemId.ToString(),
            Price = (int)itemConfig.catalogItem.VirtualCurrencyPrices["GK"],
            VirtualCurrency = "GK"
        }, result =>
        {
            for(int i = 0; i < result.Items.Count; i++)
            {
                UserItem item = ConfigManager.CreateUserItem(result.Items[i]);
                userData.userItems.Add(item);
            }
            userData.currency.UpdateGoldKey(userData.currency.goldKey - (int)itemConfig.catalogItem.VirtualCurrencyPrices["GK"]);
            CallBack?.Invoke(itemId);

        }, error => { Debug.Log(error.GenerateErrorReport());});
    }

    public void GetMyStatistics()
    {
        var request = new GetPlayerStatisticsRequest
        {
            StatisticNames = new List<string>() { "TopPlayer" }
        };
        PlayFabClientAPI.GetPlayerStatistics(request, (result) =>
        {
            if (result.Statistics != null && result.Statistics.Count > 0)
                userData.score = result.Statistics[0].Value;
            else
                userData.score = 0;
        }, OnError);
    }
    public void OnError(PlayFabError err)
    {
        print(err);
    }


    public void RequestCraft(ItemId itemId, int useCurrency, Action<bool> CallBack)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {

            GeneratePlayStreamEvent = false,
            FunctionName = "Craft",
            FunctionParameter = new
            {
                itemId = itemId.ToString(),
                useCurrency = useCurrency.ToString()
            }

        }, result =>
        {
            JsonObject jsonResult = (JsonObject)result.FunctionResult;
            object success;
            jsonResult.TryGetValue("messageValue", out success);

            GetUserInventory();
            
            print(success);
            if (success.ToString().Equals("success"))
            {
                CallBack(true);
            }
            else
            {
                CallBack(false);
            }

        },
       error => Debug.Log(error.GenerateErrorReport()));
    }
    
    
    public void RequestOnEndGame(GameMode gameMode, bool isWin, int score, Action<RewardData> CallBack)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
            {

                GeneratePlayStreamEvent = false,
                FunctionName = "EndGame",
                FunctionParameter = new
                {
                    gameMode = gameMode.ToString(),
                    isWin = isWin? 1: 0,
                    score = score.ToString()
                }

            }, result =>
            {
                JsonObject jsonResult = (JsonObject)result.FunctionResult;
                object rewardJson;
                jsonResult.TryGetValue("messageValue", out rewardJson);
                Debug.Log(rewardJson);

                RewardData rewardData = JsonConvert.DeserializeObject<RewardData>(rewardJson.ToString());
                GetUserInventory();
                CallBack?.Invoke(rewardData);

            },
            error => Debug.Log(error.GenerateErrorReport()));
    }
    
    
    public void RequestCrate(Action<ItemId> CallBack, Action CallBack2)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
            {

                GeneratePlayStreamEvent = false,
                FunctionName = "OpenCrate"

            }, result =>
            {
                JsonObject jsonResult = (JsonObject)result.FunctionResult;
                if (jsonResult != null)
                {
                    object rewardJson;
                    if (jsonResult.TryGetValue("messageValue", out rewardJson))
                    {
                        Debug.Log(rewardJson);
                        RewardData rewardData = JsonConvert.DeserializeObject<RewardData>(rewardJson.ToString());
                        if (rewardData != null)
                        {
                            if (rewardData.itemDatas[0].itemId != ItemId.None)
                                GetUserInventory(CallBack2);
                            else
                            {
                                userData.UseItem(ItemId.crate_box, 1);
                                userData.currency.Spend(ConfigManager.pricePerCrate, 0, 0);
                                CallBack2?.Invoke();
                            }
                            CallBack?.Invoke(rewardData.itemDatas[0].itemId);
                        }
                        else
                        {
                            CallBack?.Invoke(ItemId.None);
                            userData.UseItem(ItemId.crate_box, 1);
                            userData.currency.Spend(ConfigManager.pricePerCrate, 0, 0);
                            CallBack2?.Invoke();
                        }
                    }
                }
                else
                {
                    CallBack?.Invoke(ItemId.None);
                }
                

            },
            error => Debug.Log(error.GenerateErrorReport()));
    }

    public void RequestUseTicket(ItemId ticketId, int quantity, Action callBackIfFail)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
            {

                GeneratePlayStreamEvent = false,
                FunctionName = "UseTicket",
                FunctionParameter = new
                {
                    ticketId = ticketId,
                    quantity = quantity.ToString()
                }

            }, result =>
            {
                JsonObject jsonResult = (JsonObject)result.FunctionResult;
                object success;
                jsonResult.TryGetValue("messageValue", out success);
                if (success.ToString().Equals("success"))
                {
                    GetUserInventory();
                }
                else
                {
                    callBackIfFail?.Invoke();
                }


            },
            error => Debug.Log(error.GenerateErrorReport()));
    }

    public void GetLeaderBoard(Action<GetLeaderboardResult> Callback)
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "TopPlayer",
            StartPosition = 0,
            MaxResultsCount = 10
        };
        PlayFabClientAPI.GetLeaderboard(request, Callback, OnError);
    }

    public void GetMyLeaderBoard(Action<PlayerLeaderboardEntry> Callback)
    {
        var request = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = "TopPlayer",
            MaxResultsCount = 1
        };
        PlayFabClientAPI.GetLeaderboardAroundPlayer(request, (result) => {
            Callback(result.Leaderboard[0]);
        }, OnError);
    }

    
    public void AddPurchase(string itemID, string recipID)
    {
        string key = itemID + "-" + UnityEngine.Random.Range(0, 100000);
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest()
            {
                Data = new Dictionary<string, string>()
                {
                    {key, recipID}
                }
            },
            result =>
            {
                print("addd");
                //SceneManager.LoadScene(1 + gameMode);
                //HomeManager.instan.SwitchScene();
            },
            error =>
            {
                //user.matchs[gameMode]--;
                //HomeManager.instan._CancelBattle();
                Debug.Log("Got error setting user data Ancestor to Arthur");
                Debug.Log(error.GenerateErrorReport());
            });
    }
    
    public void AddRewardFromAds()
    {
        
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
            {

                GeneratePlayStreamEvent = true,
                FunctionName = "AddRewardFromAds",

            }, result =>
            {

                JsonObject jsonResult = (JsonObject)result.FunctionResult;
                object rewardJson;
                jsonResult.TryGetValue("messageValue", out rewardJson);
                Debug.Log(rewardJson);
                
                RewardData rewardData = JsonConvert.DeserializeObject<RewardData>(rewardJson.ToString());
                if (rewardData != null)
                {
                    switch (rewardData.virtualCurrncies[0].currency)
                    {
                        case TF2Currency.GK:
                            userData.currency.AddCurrency(rewardData.virtualCurrncies[0].quantity, 0, 0);
                            break;
                        case TF2Currency.MM:
                            userData.currency.AddCurrency(0, 0, rewardData.virtualCurrncies[0].quantity);
                            break;
                    }
                    
                    GUIManager.instan.CreatePopup<PopupReward>(rewardData);
                }

            },
            error => Debug.Log(error.GenerateErrorReport()));
    }
    
    
    public void RequestTransition(Transition transition, Action<RewardData> callBack)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
            {

                GeneratePlayStreamEvent = true,
                FunctionName = "Trading",
                FunctionParameter = new { inputValue = transition.ID.ToString() }

            }, result =>
            {
                //GetUserInventory();
                JsonObject jsonResult = (JsonObject)result.FunctionResult;
                object rewardJson;
                jsonResult.TryGetValue("messageValue", out rewardJson);
                Debug.Log(rewardJson);
                RewardData rewardData = JsonConvert.DeserializeObject<RewardData>(rewardJson.ToString());
                if (rewardData != null && rewardData.virtualCurrncies != null)
                {
                    userData.currency.AddCurrency(rewardData.virtualCurrncies[0]);
                }

                if (transition.sourceCurrency != null)
                {
                    userData.currency.SpendCurrency(transition.sourceCurrency);
                }
                else if(transition.sourceItem != null)
                {
                    userData.RemoveUserItem(transition.sourceItem.itemId);
                }

                callBack?.Invoke(rewardData);
            },
            error => Debug.Log(error.GenerateErrorReport()));

    }


    public void RestorePurchase(ItemId itemId, Action callback = null)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {

            GeneratePlayStreamEvent = false,
            FunctionName = "RestoreIap",
            FunctionParameter = new
            {
                itemId = itemId.ToString(),
            }

        }, result =>
        {
            GetUserInventory(() => {
                callback?.Invoke();

            });


        },
            error => Debug.Log(error.GenerateErrorReport()));
    }

    public void RemoveAcount(Action callback = null)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {

            GeneratePlayStreamEvent = false,
            FunctionName = "RemoveAccount",

        }, result =>
        {
            JsonObject jsonResult = (JsonObject)result.FunctionResult;
            object res;
            jsonResult.TryGetValue("messageValue", out res);
            Debug.Log($"Deleted: {res}");
            callback?.Invoke();


        },
            error => Debug.Log(error.GenerateErrorReport()));
    }
}


