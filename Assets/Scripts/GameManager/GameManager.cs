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
    [HideInInspector]
    public List<string> keyProductIds = new List<string>();
    [HideInInspector]
    public int toDayOfTheYear;
    [HideInInspector]
    public bool QCShowed;
    [HideInInspector]
    public bool played;

    [HideInInspector] public DateTime today;
    public FirebaseController firebaseController;

    const int MaxLoadAttempts = 3;
    const float RetryDelay = 2f;

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
    }

    #region Helpers
    void RetryLater(int attempt, Action retry, PlayFabError error, Action<PlayFabError> onFinalError)
    {
        if (attempt + 1 < MaxLoadAttempts)
        {
            Debug.LogWarning($"PlayFab call failed (attempt {attempt + 1}), retrying: {error.GenerateErrorReport()}");
            StartCoroutine(DelayCall(RetryDelay, retry));
        }
        else
        {
            Debug.LogError(error.GenerateErrorReport());
            onFinalError?.Invoke(error);
        }
    }

    static IEnumerator DelayCall(float delay, Action action)
    {
        yield return new WaitForSecondsRealtime(delay);
        action?.Invoke();
    }

    /// <summary>
    /// Reads "messageValue" from a CloudScript result. Returns false (and logs) when the script
    /// threw an error or did not return the expected object.
    /// </summary>
    public static bool TryGetCloudScriptMessage(ExecuteCloudScriptResult result, out object message)
    {
        message = null;
        if (result == null) return false;
        if (result.Error != null)
        {
            Debug.LogError($"CloudScript '{result.FunctionName}' error: {result.Error.Error} - {result.Error.Message}\n{result.Error.StackTrace}");
            return false;
        }
        if (result.FunctionResult is JsonObject json && json.TryGetValue("messageValue", out message) && message != null)
            return true;

        Debug.LogWarning($"CloudScript '{result.FunctionName}' returned no messageValue");
        return false;
    }

    static RewardData ParseReward(object json)
    {
        if (json == null) return null;
        try { return JsonConvert.DeserializeObject<RewardData>(json.ToString()); }
        catch (Exception e) { Debug.LogError("RewardData parse error: " + e.Message); return null; }
    }

    static int GetCurrency(Dictionary<string, int> vc, string code)
    {
        return vc != null && vc.TryGetValue(code, out var v) ? v : 0;
    }
    #endregion

    #region Inventory
    public void GetUserInventory(Action callBack = null) => GetUserInventory(callBack, null, 0);

    public void GetUserInventory(Action callBack, Action<PlayFabError> onError, int attempt = 0)
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            ApplyInventory(result.Inventory, result.VirtualCurrency);
            callBack?.Invoke();
        }, error => RetryLater(attempt, () => GetUserInventory(callBack, onError, attempt + 1), error, onError));
    }

    /// <summary>Also used with the combined login payload.</summary>
    public void ApplyInventory(List<ItemInstance> itemInstances, Dictionary<string, int> virtualCurrency)
    {
        userData.userItems.Clear();
        if (itemInstances != null)
        {
            foreach (var instance in itemInstances)
            {
                UserItem item = ConfigManager.CreateUserItem(instance);
                if (item == null) continue;
                if (item.itemId == ItemId.brawl_valley_remove_ads) userData.ads_removed = true;
                userData.userItems.Add(item);
            }
        }
        userData.currency.UpdateVitualCurrency(GetCurrency(virtualCurrency, "GK"), GetCurrency(virtualCurrency, "CM"), GetCurrency(virtualCurrency, "MM"));
        if (HomeManager.instance != null)
        {
            HomeManager.instance.SetNoAdsBtn();
        }
    }
    #endregion

    #region Catalog
    public void GetCatalogs() => GetCatalogs(null, null, 0);

    public void GetCatalogs(Action onDone, Action<PlayFabError> onError, int attempt = 0)
    {
        PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest(), result =>
        {
            foreach (var item in result.Catalog)
            {
                if (!Enum.TryParse(item.ItemId, true, out ItemId itemId)) continue;
                if (!Enum.TryParse(item.ItemClass, out ItemType itemType))
                {
                    Debug.LogWarning($"Catalog item '{item.ItemId}' has unknown ItemClass '{item.ItemClass}'");
                    continue;
                }

                List<TF2Class> tf2Classes = null;
                if (itemType == ItemType.Pack || itemType == ItemType.remove_ads)
                {
                    if (!keyProductIds.Contains(item.ItemId))
                        keyProductIds.Add(item.ItemId);
                }
                else if (itemType != ItemType.ticket)
                {
                    tf2Classes = new List<TF2Class>();
                    if (item.Tags != null)
                    {
                        foreach (var tag in item.Tags)
                        {
                            if (Enum.TryParse(tag, out TF2Class tf2cl)) tf2Classes.Add(tf2cl);
                            else Debug.LogWarning($"Catalog item '{item.ItemId}' has unknown class tag '{tag}'");
                        }
                    }
                }

                ItemProperties itemProperties = null;
                if (!string.IsNullOrEmpty(item.CustomData))
                {
                    try { itemProperties = JsonConvert.DeserializeObject<ItemProperties>(item.CustomData); }
                    catch (Exception e) { Debug.LogError($"Catalog item '{item.ItemId}' CustomData parse error: {e.Message}"); }
                }

                ConfigManager.itemConfigs[itemId] = new ItemConfig()
                {
                    itemId = itemId,
                    slot = itemType,
                    tf2Classes = tf2Classes,
                    catalogItem = item,
                    itemProperties = itemProperties
                };
            }

            if (LoginManager.instan != null)
            {
                LoginManager.instan.loadedCategory = true;
            }
            onDone?.Invoke();

        }, error => RetryLater(attempt, () => GetCatalogs(onDone, onError, attempt + 1), error, onError));
    }
    #endregion

    public void OnStartMatch()
    {
        matchCount++;
        if (matchCount == 1 || matchCount == 10 || matchCount == 50 || matchCount == 100)
        {
            firebaseController.LogEvent($"match_count_{matchCount}");
        }
        firebaseController.LogEvent("match", "number_match", matchCount.ToString());
    }

    #region User data / statistics
    public void GetUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest()
        {
            PlayFabId = userData.userId,
            Keys = new List<string> { "day_last", "moc_da_nhan" }
        }, result => ApplyUserData(result.Data),
        error =>
        {
            Debug.Log("Got error retrieving user data:");
            Debug.Log(error.GenerateErrorReport());
        });
    }

    public void ApplyUserData(Dictionary<string, UserDataRecord> data)
    {
        userData.dayLast = data != null && data.TryGetValue("day_last", out var dl) && int.TryParse(dl.Value, out var d) ? d : -1;
        userData.mocDaNhan = data != null && data.TryGetValue("moc_da_nhan", out var mc) && int.TryParse(mc.Value, out var m) ? m : 0;
    }

    public void GetMyStatistics()
    {
        var request = new GetPlayerStatisticsRequest
        {
            StatisticNames = new List<string>() { "TopPlayer" }
        };
        PlayFabClientAPI.GetPlayerStatistics(request, result => ApplyStatistics(result.Statistics), OnError);
    }

    public void ApplyStatistics(List<StatisticValue> statistics)
    {
        userData.score = 0;
        if (statistics == null) return;
        foreach (var s in statistics)
            if (s.StatisticName == "TopPlayer") userData.score = s.Value;
    }
    #endregion

    #region Time / daily reward
    private bool checkDailyrewarded;
    public void checkDailyReward()
    {
        if (!checkDailyrewarded)
        {
            checkDailyrewarded = true;
            PlayFabClientAPI.GetTime(new GetTimeRequest(), OnGetTimeSuccess, error =>
            {
                checkDailyrewarded = false; // allow a later retry
                LogFailure(error);
            });
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
            if (!TryGetCloudScriptMessage(result, out var rewardJson)) return;
            RewardData rewardData = ParseReward(rewardJson);
            if (rewardData != null) callBack?.Invoke(rewardData);
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
        result => { },
        error => Debug.LogError("SetDayLast failed: " + error.GenerateErrorReport()));
    }
    #endregion

    public void BuyItem(ItemId itemId, Action<ItemId> CallBack = null, Action<PlayFabError> onError = null)
    {
        ItemConfig itemConfig = ConfigManager.GetItemConfig(itemId);
        if (itemConfig?.catalogItem?.VirtualCurrencyPrices == null || !itemConfig.catalogItem.VirtualCurrencyPrices.TryGetValue("GK", out var price))
        {
            Debug.LogError($"BuyItem: no GK price for {itemId}");
            onError?.Invoke(null);
            return;
        }
        PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest()
        {
            ItemId = itemId.ToString(),
            Price = (int)price,
            VirtualCurrency = "GK"
        }, result =>
        {
            for (int i = 0; i < result.Items.Count; i++)
            {
                UserItem item = ConfigManager.CreateUserItem(result.Items[i]);
                if (item != null) userData.userItems.Add(item);
            }
            userData.currency.UpdateGoldKey(userData.currency.goldKey - (int)price);
            CallBack?.Invoke(itemId);
        }, error =>
        {
            Debug.Log(error.GenerateErrorReport());
            // Local balance may be stale (e.g. InsufficientFunds): resync it.
            GetUserInventory();
            onError?.Invoke(error);
        });
    }

    public void OnError(PlayFabError err)
    {
        Debug.LogWarning(err.GenerateErrorReport());
    }

    /// <param name="CallBack">true = crafted, false = craft roll failed (resources consumed by the server).</param>
    /// <param name="onError">Request/CloudScript error (nothing consumed). Falls back to CallBack(false) when null.</param>
    public void RequestCraft(ItemId itemId, int useCurrency, Action<bool> CallBack, Action onError = null)
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
            GetUserInventory();
            if (!TryGetCloudScriptMessage(result, out var success))
            {
                if (onError != null) onError(); else CallBack?.Invoke(false);
                return;
            }
            CallBack?.Invoke(success.ToString().Equals("success"));
        },
        error =>
        {
            Debug.Log(error.GenerateErrorReport());
            if (onError != null) onError(); else CallBack?.Invoke(false);
        });
    }

    public void RequestOnEndGame(GameMode gameMode, bool isWin, int score, Action<RewardData> CallBack, Action onError = null)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {
            GeneratePlayStreamEvent = false,
            FunctionName = "EndGame",
            FunctionParameter = new
            {
                gameMode = gameMode.ToString(),
                isWin = isWin ? 1 : 0,
                score = score.ToString()
            }
        }, result =>
        {
            GetUserInventory();
            RewardData rewardData = TryGetCloudScriptMessage(result, out var rewardJson) ? ParseReward(rewardJson) : null;
            if (rewardData != null) CallBack?.Invoke(rewardData);
            else onError?.Invoke();
        },
        error =>
        {
            Debug.Log(error.GenerateErrorReport());
            onError?.Invoke();
        });
    }

    public void RequestCrate(Action<ItemId> CallBack, Action CallBack2)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {
            GeneratePlayStreamEvent = false,
            FunctionName = "OpenCrate"
        }, result =>
        {
            if (!TryGetCloudScriptMessage(result, out var rewardJson))
            {
                CallBack?.Invoke(ItemId.None);
                return;
            }

            RewardData rewardData = ParseReward(rewardJson);
            ItemId rewardId = rewardData?.itemDatas != null && rewardData.itemDatas.Count > 0 ? rewardData.itemDatas[0].itemId : ItemId.None;

            if (rewardId != ItemId.None)
            {
                GetUserInventory(CallBack2);
            }
            else
            {
                userData.UseItem(ItemId.crate_box, 1);
                userData.currency.Spend(ConfigManager.pricePerCrate, 0, 0);
                CallBack2?.Invoke();
            }
            CallBack?.Invoke(rewardId);
        },
        error =>
        {
            Debug.Log(error.GenerateErrorReport());
            CallBack?.Invoke(ItemId.None);
        });
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
            if (TryGetCloudScriptMessage(result, out var success) && success.ToString().Equals("success"))
                GetUserInventory();
            else
                callBackIfFail?.Invoke();
        },
        error =>
        {
            Debug.Log(error.GenerateErrorReport());
            callBackIfFail?.Invoke();
        });
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
        PlayFabClientAPI.GetLeaderboardAroundPlayer(request, (result) =>
        {
            if (result.Leaderboard != null && result.Leaderboard.Count > 0)
                Callback(result.Leaderboard[0]);
        }, OnError);
    }

    /// <summary>Stores the raw Google receipt for support. Key is unique per purchase.</summary>
    public void AddPurchase(string itemID, string recipID)
    {
        string key = itemID + "-" + DateTime.UtcNow.Ticks;
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>()
            {
                {key, recipID}
            },
            Permission = UserDataPermission.Private
        },
        result => { },
        error =>
        {
            Debug.Log("AddPurchase: error saving receipt");
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
            if (!TryGetCloudScriptMessage(result, out var rewardJson)) return;
            RewardData rewardData = ParseReward(rewardJson);
            if (rewardData?.virtualCurrncies != null && rewardData.virtualCurrncies.Count > 0)
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

    public void RequestTransition(Transition transition, Action<RewardData> callBack, Action onError = null)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {
            GeneratePlayStreamEvent = true,
            FunctionName = "Trading",
            FunctionParameter = new { inputValue = transition.ID.ToString() }
        }, result =>
        {
            RewardData rewardData = TryGetCloudScriptMessage(result, out var rewardJson) ? ParseReward(rewardJson) : null;
            if (rewardData == null)
            {
                GetUserInventory(); // resync in case the server changed something
                onError?.Invoke();
                return;
            }

            if (rewardData.virtualCurrncies != null && rewardData.virtualCurrncies.Count > 0)
            {
                userData.currency.AddCurrency(rewardData.virtualCurrncies[0]);
            }

            if (transition.sourceCurrency != null)
            {
                userData.currency.SpendCurrency(transition.sourceCurrency);
            }
            else if (transition.sourceItem != null)
            {
                userData.RemoveUserItem(transition.sourceItem.itemId);
            }

            callBack?.Invoke(rewardData);
        },
        error =>
        {
            Debug.Log(error.GenerateErrorReport());
            onError?.Invoke();
        });
    }

    #region Rewarded-ad bonuses (CloudScript: ClaimDoubleReward / GetTicketByAds)
    public const string DoubleSourceEndGame = "endgame";
    public const string DoubleSourceDaily = "daily";

    /// <summary>
    /// Grants the last reward of <paramref name="source"/> a second time (after a rewarded ad).
    /// The server only accepts it once, for a reward saved a few minutes ago by EndGame / AddDailyReward.
    /// </summary>
    public void RequestDoubleReward(string source, Action<RewardData> onDone, Action onError)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {
            GeneratePlayStreamEvent = true,
            FunctionName = "ClaimDoubleReward",
            FunctionParameter = new { source }
        }, result =>
        {
            RewardData reward = TryGetCloudScriptMessage(result, out var json) ? ParseReward(json) : null;
            if (reward == null) { onError?.Invoke(); return; }
            firebaseController.LogEvent("reward_x2", "source", source);
            GetUserInventory(() => onDone?.Invoke(reward));
        },
        error =>
        {
            Debug.Log(error.GenerateErrorReport());
            onError?.Invoke();
        });
    }

    /// <summary>Grants the tickets needed for <paramref name="map"/> after a rewarded ad (daily cap on the server).</summary>
    /// <param name="onFail">true = daily limit reached, false = other error.</param>
    public void RequestTicketByAds(Map map, Action onDone, Action<bool> onFail)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {
            GeneratePlayStreamEvent = true,
            FunctionName = "GetTicketByAds",
            // amount = this map's ticket price (client default); CloudScript uses it when Title Data map_config does not list the map
            FunctionParameter = new { map = map.ToString(), mapId = (int)map, amount = Mathf.Max(1, ConfigManager.GetTicketCost(map)) }
        }, result =>
        {
            if (!TryGetCloudScriptMessage(result, out var msg)) { onFail?.Invoke(false); return; }
            string s = msg.ToString();
            if (s == "limit") { onFail?.Invoke(true); return; }
            if (s != "success") { onFail?.Invoke(false); return; }
            firebaseController.LogEvent("ticket_by_ads", "map", map.ToString());
            GetUserInventory(() => onDone?.Invoke());
        },
        error =>
        {
            Debug.Log(error.GenerateErrorReport());
            onFail?.Invoke(false);
        });
    }
    #endregion

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
            if (result.Error != null) Debug.LogError($"RestoreIap error: {result.Error.Error} - {result.Error.Message}");
            GetUserInventory(() => callback?.Invoke());
        },
        error =>
        {
            Debug.Log(error.GenerateErrorReport());
            callback?.Invoke();
        });
    }

    public void RemoveAcount(Action callback = null)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {
            GeneratePlayStreamEvent = false,
            FunctionName = "RemoveAccount",
        }, result =>
        {
            if (TryGetCloudScriptMessage(result, out var res))
            {
                Debug.Log($"Deleted: {res}");
                callback?.Invoke();
            }
        },
        error => Debug.Log(error.GenerateErrorReport()));
    }
}
