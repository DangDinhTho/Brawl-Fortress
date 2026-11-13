using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using DG.Tweening;
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif
using System;

public class LoginManager : MonoBehaviour
{
    public static LoginManager instan;
    public InputField userNameInput;
    public InputField passwordInput;
    public InputField newUsernameInput;
    public InputField newUsernameInputID;
    public InputField newPasswordInput;
    public InputField passwordInputConfirm;
    public GameObject loginPanel;
    public GameObject setUserNamePanel;
    public GameObject signupPanel;
    public GameObject loginHidePanel;
    public GameObject registerHidePanel;

    public Text txtLoginNotifical;
    public bool logined;
    public bool fistTime;
    public GameObject noInternetPanel;
    public GameObject loadingPanel;
    public GameObject policyPanel;

    private string CUSTOMID = "CustomID";
    private int loginCase;

    private string LOGINCASE = "LoginCase";
    private string USERNAME = "UserName";
    private string PASSWORD = "PASSWORD";
    private string GRAPHICKEY = "GRAPHICSETTING";

    private string _username;
    private string _password;

    private string customId;
    [HideInInspector]
    public bool loadedCategory;

    // Data required before entering Home
    bool titleDataLoaded;
    bool inventoryLoaded;
    bool loginDoneStarted;

    const int MaxLoadAttempts = 3;

    public Slider slider;

    private void Awake()
    {
        instan = this;
    }

    public void Start()
    {
        if (!PlayerPrefs.HasKey(GRAPHICKEY))
        {
            PlayerPrefs.SetInt(GRAPHICKEY, 4);
        }
        QualitySettings.SetQualityLevel(PlayerPrefs.GetInt(GRAPHICKEY));

        if (PlayerPrefs.HasKey(LOGINCASE))
        {
            loginCase = PlayerPrefs.GetInt(LOGINCASE);
        }
        else
        {
            loadingPanel.SetActive(false);
        }

        _username = PlayerPrefs.GetString(USERNAME, null);
        _password = PlayerPrefs.GetString(PASSWORD, null);
        customId = PlayerPrefs.GetString(CUSTOMID, null);

        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
        {
            PlayFabSettings.staticSettings.TitleId = "F08BD";
        }
        slider.value = 0;

        policyPanel.SetActive(false);
#if UNITY_IOS
        AskAtt();
#endif

        StartCoroutine(CheckConenctInternet());
    }

#if UNITY_IOS
    private void AskAtt()
    {
        var attStatus = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
        if (attStatus == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking();
        }
    }
#endif

    public void _Retry()
    {
        noInternetPanel.SetActive(false);
        if (logined)
            LoadMissingData(); // already logged in: only reload what failed
        else
            StartCoroutine(CheckConenctInternet());
    }

    IEnumerator CheckConenctInternet()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            noInternetPanel.SetActive(true);
            yield break;
        }

        using (var req = UnityWebRequest.Head("https://www.google.com"))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                noInternetPanel.SetActive(false);
                AdsManager.instan.InitAds();
                Login();
            }
            else
            {
                Debug.LogWarning("Internet check failed: " + req.error);
                noInternetPanel.SetActive(true);
            }
        }
    }

    public void SelectRegister()
    {
        signupPanel.SetActive(true);
        loginPanel.SetActive(false);
        txtLoginNotifical.gameObject.SetActive(false);
    }

    public void SelectLogin()
    {
        signupPanel.SetActive(false);
        loginPanel.SetActive(true);
        txtLoginNotifical.gameObject.SetActive(false);
    }

    /// <summary>Everything needed before Home is returned in the login response (1 call instead of 5).</summary>
    static GetPlayerCombinedInfoRequestParams InfoParams => new GetPlayerCombinedInfoRequestParams
    {
        GetPlayerProfile = true,
        ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true },
        GetTitleData = true,
        GetUserInventory = true,
        GetUserVirtualCurrency = true,
        GetUserData = true,
        UserDataKeys = new List<string> { "day_last", "moc_da_nhan" },
        GetPlayerStatistics = true,
        PlayerStatisticNames = new List<string> { "TopPlayer" },
    };

    public void Login()
    {
        switch (loginCase)
        {
            case 0:
                if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                {
                    loginHidePanel.SetActive(true);
                    LoginWithUserNameAndPassword();
                }
                else
                {
                    loadingPanel.SetActive(false);
                }
                break;
            case 1:
                if (!string.IsNullOrEmpty(customId))
                {
                    loginHidePanel.SetActive(true);
                    LoginWithCustomId();
                }
                else
                {
                    loadingPanel.SetActive(false);
                }
                break;
        }
    }

    public void LoginWithUserNameAndPasswordClick()
    {
        txtLoginNotifical.gameObject.SetActive(false);
        if (userNameInput.text.Length < 4)
        {
            ShowNotify("Username must longer 3 characters");
            return;
        }
        else if (passwordInput.text.Length < 6)
        {
            ShowNotify("Password must be 6 or more characters");
            return;
        }
        _username = userNameInput.text.Trim();
        _password = passwordInput.text;
        loginHidePanel.SetActive(true);
        LoginWithUserNameAndPassword();
    }

    public void LoginWithUserNameAndPassword()
    {
        if (logined) return;
        loginCase = 0;
        var request = new LoginWithPlayFabRequest
        {
            Username = _username,
            Password = _password,
            InfoRequestParameters = InfoParams
        };
        PlayFabClientAPI.LoginWithPlayFab(request, OnLoginWithPassSuccess, OnLoginFailure);
    }

    public void OnLoginWithPassSuccess(LoginResult result)
    {
        PlayerPrefs.SetString(USERNAME, _username);
        PlayerPrefs.SetInt(LOGINCASE, 0);
        PlayerPrefs.SetString(PASSWORD, _password);

        if (loginPanel)
        {
            loginPanel.SetActive(false);
            signupPanel.SetActive(false);
        }
        OnLoggedIn(result.PlayFabId, false, result.InfoResultPayload);
    }

    private void OnLoginFailure(PlayFabError error)
    {
        ShowNotify(error.ErrorMessage);
        loginPanel.SetActive(true);
        loginHidePanel.SetActive(false);
        loadingPanel.SetActive(false);
    }

    public void RegisterWithUserNameAndPasswordClick()
    {
        txtLoginNotifical.gameObject.SetActive(false);
        if (newUsernameInput.text.Length < 4)
        {
            ShowNotify("Username must longer 3 characters");
            return;
        }
        else if (newPasswordInput.text.Length < 6)
        {
            ShowNotify("Password must be 6 or more characters");
            return;
        }
        else if (!passwordInputConfirm.text.Equals(newPasswordInput.text))
        {
            ShowNotify("Confirm password was wrong");
            return;
        }

        _username = newUsernameInput.text.Trim();
        _password = newPasswordInput.text;

        OpenPolicy(() =>
        {
            registerHidePanel.SetActive(true);
            RegisterWithUserNameAndPassword();
        });
    }

    public void RegisterWithUserNameAndPassword()
    {
        var request = new RegisterPlayFabUserRequest
        {
            Username = _username,
            Password = _password,
            DisplayName = _username,
            RequireBothUsernameAndEmail = false
        };
        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSucess, OnRegisterFall);
    }

    public void OnRegisterFall(PlayFabError err)
    {
        ShowNotify(err.ErrorMessage);
        registerHidePanel.SetActive(false);
    }

    public void OnRegisterSucess(RegisterPlayFabUserResult userResult)
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);

        PlayerPrefs.SetString(USERNAME, _username);
        PlayerPrefs.SetInt(LOGINCASE, 0);
        PlayerPrefs.SetString(PASSWORD, _password);

        // Register does not return the combined payload: load data with separate calls.
        // DisplayName was set at registration.
        OnLoggedIn(userResult.PlayFabId, true, null, _username);
    }

    public void LoginWithDeviceIdClick()
    {
        OpenPolicy(() =>
        {
            customId = "Brawl_" + SystemInfo.deviceUniqueIdentifier;
            loginHidePanel.SetActive(true);
            LoginWithCustomId();
        });
    }

    private void OpenPolicy(Action callback)
    {
        OnConfirmPolicy = callback;
        policyPanel.SetActive(true);
    }

    public void OpenPolicyLink()
    {
        Application.OpenURL("https://brawlvalley.apppage.net/privacy-policy");
    }

    public void OpenTermLink()
    {
        Application.OpenURL("https://brawlvalley.apppage.net/terms");
    }

    private Action OnConfirmPolicy;
    public void ClickConfirmPolicy()
    {
        policyPanel.SetActive(false);
        OnConfirmPolicy?.Invoke();
    }

    public void LoginWithCustomId()
    {
        if (logined) return;
        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true,
            InfoRequestParameters = InfoParams
        };
        PlayFabClientAPI.LoginWithCustomID(request, result =>
        {
            PlayerPrefs.SetInt(LOGINCASE, 1);
            PlayerPrefs.SetString(CUSTOMID, customId);
            OnLoggedIn(result.PlayFabId, result.NewlyCreated, result.InfoResultPayload);
        }, OnLoginFailure1);
    }

    private void OnLoginFailure1(PlayFabError error)
    {
        Debug.LogWarning("LoginWithCustomID failed: " + error.GenerateErrorReport());
        ShowNotify(error.ErrorMessage);
        loginHidePanel.SetActive(false);
        loadingPanel.SetActive(false);
    }

    #region After login
    void OnLoggedIn(string playFabId, bool newlyCreated, GetPlayerCombinedInfoResultPayload payload, string knownDisplayName = null)
    {
        GameManager.instan.userData.userId = playFabId;
        logined = true;
        fistTime = newlyCreated;

        StartDataLoad(payload);

        if (!string.IsNullOrEmpty(knownDisplayName))
        {
            OnDisplayNameResolved(knownDisplayName);
        }
        else if (payload != null)
        {
            // PlayerProfile is null for a newly created player.
            OnDisplayNameResolved(payload.PlayerProfile?.DisplayName);
        }
        else
        {
            GetPlayerProfile();
        }
    }

    void StartDataLoad(GetPlayerCombinedInfoResultPayload payload)
    {
        var gm = GameManager.instan;

        if (payload?.TitleData != null && payload.TitleData.Count > 0)
        {
            ApplyTitleData(payload.TitleData);
            titleDataLoaded = true;
        }
        else ClientGetTitleData();

        if (payload?.UserInventory != null)
        {
            gm.ApplyInventory(payload.UserInventory, payload.UserVirtualCurrency);
            inventoryLoaded = true;
        }
        else gm.GetUserInventory(() => inventoryLoaded = true, OnDataLoadFailed);

        if (payload?.UserData != null) gm.ApplyUserData(payload.UserData);
        else gm.GetUserData();

        if (payload?.PlayerStatistics != null) gm.ApplyStatistics(payload.PlayerStatistics);
        else gm.GetMyStatistics();

        if (!loadedCategory) gm.GetCatalogs(null, OnDataLoadFailed);

        gm.checkDailyReward();
    }

    void LoadMissingData()
    {
        if (!titleDataLoaded) ClientGetTitleData();
        if (!inventoryLoaded) GameManager.instan.GetUserInventory(() => inventoryLoaded = true, OnDataLoadFailed);
        if (!loadedCategory) GameManager.instan.GetCatalogs(null, OnDataLoadFailed);
    }

    void OnDataLoadFailed(PlayFabError error)
    {
        // All retries failed: let the player retry instead of hanging on the loading screen.
        noInternetPanel.SetActive(true);
    }

    void OnDisplayNameResolved(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            setUserNamePanel.SetActive(true);
            loginHidePanel.SetActive(false);
            return;
        }
        GameManager.instan.userData.userName = displayName;
        setUserNamePanel.SetActive(false);
        LoginDone();
    }

    /// <summary>Fallback when the login response has no profile (register flow).</summary>
    public void GetPlayerProfile()
    {
        PlayFabClientAPI.GetPlayerProfile(new GetPlayerProfileRequest()
        {
            PlayFabId = GameManager.instan.userData.userId,
            ProfileConstraints = new PlayerProfileViewConstraints() { ShowDisplayName = true }
        },
        result => OnDisplayNameResolved(result.PlayerProfile?.DisplayName),
        error =>
        {
            Debug.LogError(error.GenerateErrorReport());
            loginHidePanel.SetActive(false);
            noInternetPanel.SetActive(true);
        });
    }
    #endregion

    public void UpdateUserNameClick()
    {
        txtLoginNotifical.gameObject.SetActive(false);
        loginHidePanel.SetActive(true);
        string name = newUsernameInputID.text.Trim();
        if (name.Length < 4)
        {
            ShowNotify("The name must longer 3 characters");
            loginHidePanel.SetActive(false);
            return;
        }
        else if (name.Contains(" "))
        {
            ShowNotify("The name cannot contain spaces");
            loginHidePanel.SetActive(false);
            return;
        }

        _username = name;
        SetUserName();
    }

    public void SetUserName()
    {
        PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = _username
        }, result =>
        {
            GameManager.instan.userData.userName = result.DisplayName;
            setUserNamePanel.SetActive(false);
            LoginDone();
        }, error =>
        {
            loginHidePanel.SetActive(false);
            if (error.Error == PlayFabErrorCode.NameNotAvailable)
                ShowNotify("Name is exis");
            else
                ShowNotify(error.ErrorMessage);
        });
    }

    #region Title data
    public void ClientGetTitleData() => ClientGetTitleData(0);

    void ClientGetTitleData(int attempt)
    {
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(),
            result =>
            {
                ApplyTitleData(result.Data);
                titleDataLoaded = true;
            },
            error =>
            {
                if (attempt + 1 < MaxLoadAttempts)
                {
                    Debug.LogWarning("GetTitleData failed, retrying: " + error.GenerateErrorReport());
                    StartCoroutine(Delay(2f, () => ClientGetTitleData(attempt + 1)));
                }
                else
                {
                    Debug.LogError("GetTitleData failed: " + error.GenerateErrorReport());
                    OnDataLoadFailed(error);
                }
            });
    }

    static IEnumerator Delay(float seconds, Action action)
    {
        yield return new WaitForSecondsRealtime(seconds);
        action();
    }

    static T ParseJson<T>(Dictionary<string, string> data, string key) where T : class
    {
        if (!data.TryGetValue(key, out var json) || string.IsNullOrEmpty(json)) return null;
        try { return JsonConvert.DeserializeObject<T>(json); }
        catch (Exception e)
        {
            Debug.LogError($"TitleData '{key}' parse error: {e.Message}");
            return null;
        }
    }

    static void ReadBool(Dictionary<string, string> data, string key, ref bool target)
    {
        if (data.TryGetValue(key, out var s) && bool.TryParse(s, out var v)) target = v;
    }

    static void ReadInt(Dictionary<string, string> data, string key, ref int target)
    {
        if (data.TryGetValue(key, out var s) && int.TryParse(s, out var v)) target = v;
    }

    static void ReadString(Dictionary<string, string> data, string key, ref string target)
    {
        if (data.TryGetValue(key, out var s)) target = s;
    }

    /// <summary>Every key is optional: a missing or malformed key keeps the default value.</summary>
    void ApplyTitleData(Dictionary<string, string> data)
    {
        if (data == null)
        {
            Debug.LogWarning("No title data");
            ConfigManager.reciptCodeAccept = ConfigManager.reciptCodeAccept ?? new List<string>();
            return;
        }

        string codes = data.TryGetValue("recipt_code_accept", out var c) ? c : "";
        ConfigManager.reciptCodeAccept = new List<string>(codes.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries));

        var medalExps = ParseJson<List<MedalExp>>(data, "medal_exp");
        if (medalExps != null)
        {
            ConfigManager.medals = new Dictionary<Medal, MedalExp>();
            foreach (var medal in medalExps) ConfigManager.medals[medal.medal] = medal;
        }

        if (data.TryGetValue("score_per_level", out var spl) && int.TryParse(spl, out var splValue) && splValue > 0)
            ConfigManager.scorePerLevel = splValue;

        var modeUnlock = ParseJson<Dictionary<GameMode, int>>(data, "mode_unlock_level");
        if (modeUnlock != null)
            foreach (var kv in modeUnlock) ConfigManager.modeUnlockLevels[kv.Key] = kv.Value;

        var mapConfigsOld = ParseJson<List<MapConfig>>(data, "map_configs");
        if (mapConfigsOld != null)
            foreach (var mapCfg in mapConfigsOld) ConfigManager.mapConfigs[mapCfg.map] = mapCfg;

        var mapConfigs = ParseJson<List<MapConfig>>(data, "map_config");
        if (mapConfigs != null)
        {
            ConfigManager.mapConfigs.Clear();
            foreach (var mapCfg in mapConfigs) ConfigManager.mapConfigs[mapCfg.map] = mapCfg;
        }

        var battleTimes = ParseJson<Dictionary<Map, int>>(data, "battle_map_times");
        if (battleTimes != null) ConfigManager.battleTimes = battleTimes;

        ReadString(data, "discord_link", ref ConfigManager.discordLink);
        ReadString(data, "lnading_page", ref ConfigManager.landingPage);
        ReadString(data, "lnading_character", ref ConfigManager.landingCharacter);

        var dailyRewards = ParseJson<List<DailyReward>>(data, "daily_rewards");
        if (dailyRewards != null) ConfigManager.dailyRewards = dailyRewards;

        var qc = ParseJson<List<QCData>>(data, "qc_data");
        if (qc != null) ConfigManager.QcDatas = qc;

        ReadInt(data, "price_per_crate", ref ConfigManager.pricePerCrate);
        ReadBool(data, "can_get_ticket_by_ads", ref ConfigManager.canGetTicketByAds);
        ReadBool(data, "show_banner", ref ConfigManager.showBanner);
        ReadBool(data, "multiplay", ref ConfigManager.multiplay);
        ReadBool(data, "show_inter_back_home", ref ConfigManager.showInterBackHome);
        ReadBool(data, "show_inter_die", ref ConfigManager.showInterDie);
        ReadInt(data, "max_reward_video_per_season", ref ConfigManager.maxVideoPerDay);

        var trading = ParseJson<TradingData>(data, "trading_data");
        if (trading != null) ConfigManager.tradingData = trading;

        var characterStats = ParseJson<Dictionary<TF2Class, CharacterStats>>(data, "character_stats");
        if (characterStats != null) ConfigManager.characterStats = characterStats;

        var weaponStats = ParseJson<Dictionary<WeaponId, WeaponStats>>(data, "weapon_stats");
        if (weaponStats != null) ConfigManager.weaponStats = weaponStats;
    }
    #endregion

    void ShowNotify(string message)
    {
        txtLoginNotifical.text = message;
        txtLoginNotifical.gameObject.SetActive(true);
    }

    public void CloseLoginPanelClick()
    {
        loginPanel.SetActive(false);
    }

    public void CloseSignupClick()
    {
        signupPanel.SetActive(false);
    }

    public void LoginDone()
    {
        if (loginDoneStarted) return;
        loginDoneStarted = true;
        loadingPanel.SetActive(true);
        slider.DOValue(1, 2f);
        StartCoroutine(WaitAllLoad());
    }

    IEnumerator WaitAllLoad()
    {
        yield return new WaitForSeconds(2);
        // Home needs title data (IAP receipt codes, ads flags...), the catalog and the inventory.
        while (!(loadedCategory && titleDataLoaded && inventoryLoaded))
        {
            yield return null;
        }
        SceneManager.LoadScene("Home");
    }
}
