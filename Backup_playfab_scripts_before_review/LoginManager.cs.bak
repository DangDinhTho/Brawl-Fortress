using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;
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

        if (PlayerPrefs.HasKey(USERNAME))
        {
            _username = PlayerPrefs.GetString(USERNAME);
        }

        if (PlayerPrefs.HasKey(PASSWORD))
        {
            _password = PlayerPrefs.GetString(PASSWORD);
        }

        if (PlayerPrefs.HasKey(CUSTOMID))
        {
            customId = PlayerPrefs.GetString(CUSTOMID);
        }

        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
        {
            /*
            Please change the titleId below to your own titleId from PlayFab Game Manager.
            If you have already set the value in the Editor Extensions, this can be skipped.
            */
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
        ATTrackingStatusBinding.AuthorizationTrackingStatus attStatus = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
        Debug.Log("ThoDD: " + attStatus.ToString());
        if (attStatus == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking();
            Debug.Log("ThoDD: Request Att");
        }
    }
#endif


    public void _Retry()
    {
        noInternetPanel.SetActive(false);
        StartCoroutine(CheckConenctInternet());
    }

    IEnumerator CheckConenctInternet()
    {
        WWW www = new WWW("https://www.google.com");
        yield return www;
        if (www.isDone && www.bytesDownloaded > 0)
        {
            noInternetPanel.SetActive(false);
            AdsManager.instan.InitAds();
            //Yodo1AdsManager.instan.StartInitYodo();
            Login();
            yield break;
        }
        else if (www.isDone && www.bytesDownloaded == 0)
        {
            noInternetPanel.SetActive(true);
            yield break;
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
                    //loginPanel.SetActive(true);
                    loadingPanel.SetActive((false));
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
                    //loginPanel.SetActive(true);
                    loadingPanel.SetActive((false));
                }
                break;
        }
    }


    public void LoginWithUserNameAndPasswordClick()
    {
        txtLoginNotifical.gameObject.SetActive(false);
        if (userNameInput.text.Length < 4)
        {
            txtLoginNotifical.text = "Username must longer 3 characters";
            txtLoginNotifical.gameObject.SetActive(true);
            return;
        }
        else if (passwordInput.text.Length < 6)
        {
            txtLoginNotifical.text = "Password must be 6 or more characters";
            txtLoginNotifical.gameObject.SetActive(true);
            return;
        }
        _username = userNameInput.text;
        _password = passwordInput.text;
        loginHidePanel.SetActive(true);
        LoginWithUserNameAndPassword();

    }

    public void LoginWithUserNameAndPassword()
    {
        if (logined) return;
        loginCase = 0;
        var request = new LoginWithPlayFabRequest { Username = _username, Password = _password };
        PlayFabClientAPI.LoginWithPlayFab(request, OnLoginWithPassSuccess, OnLoginFailure);
    }

    public void OnLoginWithPassSuccess(LoginResult result)
    {
        //if (!PlayerPrefs.HasKey(USERNAME) || )
        //{
        PlayerPrefs.SetString(USERNAME, _username);
        PlayerPrefs.SetInt(LOGINCASE, 0);
        PlayerPrefs.SetString(PASSWORD, _password);
        //}

        GameManager.instan.userData.userId = result.PlayFabId;
        //fistTime = false;
        if (loginPanel)
        {
            loginPanel.SetActive(false);
            signupPanel.SetActive(false);
        }
        logined = true;
        fistTime = false;
        GetPlayerProfile();

    }

    private void OnLoginFailure(PlayFabError error)
    {
        txtLoginNotifical.gameObject.SetActive(true);
        loginPanel.SetActive(true);
        txtLoginNotifical.text = error.ErrorMessage;
        loginHidePanel.SetActive(false);
        loadingPanel.SetActive((false));
    }


    public void RegisterWithUserNameAndPasswordClick()
    {
        txtLoginNotifical.gameObject.SetActive(false);
        if (newUsernameInput.text.Length < 4)
        {
            txtLoginNotifical.gameObject.SetActive(true);
            txtLoginNotifical.text = "Username must longer 3 characters";
            return;
        }
        else if (newPasswordInput.text.Length < 6)
        {
            txtLoginNotifical.gameObject.SetActive(true);
            txtLoginNotifical.text = "Password must be 6 or more characters";
            return;
        }
        else if (!passwordInputConfirm.text.Equals(newPasswordInput.text))
        {
            txtLoginNotifical.gameObject.SetActive(true);
            txtLoginNotifical.text = "Confirm password was wrong";
            return;
        }
        //else if (GameManagerAll.instan.names.Contains(newUsernameInput.text))
        //{
        //    registerNotify.SetActive(true);
        //    txtRegisterNotifical.text = "Name is exis";
        //    return;
        //}

        _username = newUsernameInput.text;
        _password = newPasswordInput.text;

        OpenPolicy(() =>
        {
            registerHidePanel.SetActive(true);
            RegisterWithUserNameAndPassword();
        });
        //loginHidePanel.SetActive(true);

    }

    public void RegisterWithUserNameAndPassword()
    {
        var request = new RegisterPlayFabUserRequest { Username = _username, Password = _password, DisplayName = _username, RequireBothUsernameAndEmail = false };
        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSucess, OnRegisterFall);
    }

    public void OnRegisterFall(PlayFabError err)
    {
        txtLoginNotifical.gameObject.SetActive(true);
        txtLoginNotifical.text = err.ErrorMessage;
        registerHidePanel.SetActive(false);
    }

    public void OnRegisterSucess(RegisterPlayFabUserResult userResult)
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);

        PlayerPrefs.SetString(USERNAME, _username);
        PlayerPrefs.SetInt(LOGINCASE, 0);
        PlayerPrefs.SetString(PASSWORD, _password);

        GameManager.instan.userData.userId = userResult.PlayFabId;
        fistTime = true;
        GetPlayerProfile();

    }

    public void LoginWithDeviceIdClick()
    {
        OpenPolicy(() =>
        {
            customId = "Brawl_" + SystemInfo.deviceUniqueIdentifier;
            LoginWithCustomId();
            loginHidePanel.SetActive(true);
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
        var request = new LoginWithCustomIDRequest { CustomId = customId, CreateAccount = true };
        PlayFabClientAPI.LoginWithCustomID(request, result => {
            Debug.Log("Congratulations, you made your first successful API call!");
            GameManager.instan.userData.userId = result.PlayFabId;
            PlayerPrefs.SetInt(LOGINCASE, 1);
            PlayerPrefs.SetString(CUSTOMID, customId);
            fistTime = result.NewlyCreated;
            GetPlayerProfile();

        }, OnLoginFailure1);
    }



    private void OnLoginFailure1(PlayFabError error)
    {
        //Debug.LogWarning("Something went wrong with your first API call.  :(");
        //Debug.LogError("Here's some debug information:");
        //Debug.LogError(error.ErrorMessage);
        loginHidePanel.SetActive(false);
        loadingPanel.SetActive((false));
        //if (error.ErrorMessage == "User not found")
        //{
        //    // Active SignUp panel
        //    loginPanel.SetActive(true);
        //    loginHidePanel.SetActive(false);
        //    fistTime = true;
        //}

    }


    public void UpdateUserNameClick()
    {
        txtLoginNotifical.gameObject.SetActive(false);
        loginHidePanel.SetActive(true);
        if (newUsernameInputID.text.Length < 4)
        {
            txtLoginNotifical.text = "The name must longer 3 characters";
            txtLoginNotifical.gameObject.SetActive(true);
            loginHidePanel.SetActive(false);
            return;
        }
        else if (newUsernameInputID.text.Contains(" "))
        {
            txtLoginNotifical.text = "The name cannot contain spaces";
            txtLoginNotifical.gameObject.SetActive(true);
            loginHidePanel.SetActive(false);
            return;
        }
        //else if (GameManagerAll.instan.names.Contains(newUsernameInputID.text))
        //{
        //    txtUserNameNotifical.text = "Name is exis";
        //    loginHidePanel.SetActive(false);
        //    usernameNotifi.SetActive(true);
        //    return;
        //}

        _username = newUsernameInputID.text;
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
            print("Set username success!");
            LoginDone();

        }, error =>
        {
            print(error.ErrorMessage);
            loginHidePanel.SetActive(false);
            if (error.ErrorMessage == "Name not available")
            {
                txtLoginNotifical.gameObject.SetActive(true);
                txtLoginNotifical.text = "Name is exis";

            }
        });

    }


    public void GetPlayerProfile()
    {
        ClientGetTitleData();
        //if (fistTime)
        //{
        //    GameManagerAll.instan.GrandItemOnCreate();
        //}
        //else
        //{
        GameManager.instan.GetUserInventory();
        //}
        GameManager.instan.GetCatalogs();
        GameManager.instan.GetUserData();
        GameManager.instan.checkDailyReward();
        PlayFabClientAPI.GetPlayerProfile(new GetPlayerProfileRequest()
        {
            PlayFabId = GameManager.instan.userData.userId,
            ProfileConstraints = new PlayerProfileViewConstraints()
            {
                ShowDisplayName = true
            }

        },
        result => {

            if (result.PlayerProfile.DisplayName == null)
            {
                setUserNamePanel.SetActive(true);
                loginHidePanel.SetActive(false);
            }
            else
            {
                GameManager.instan.userData.userName = result.PlayerProfile.DisplayName;
                setUserNamePanel.SetActive(false);
                GameManager.instan.GetMyStatistics();

                LoginDone();
                print("Login Success!");
            }

        },
        error => Debug.LogError(error.GenerateErrorReport()));
    }



    public void ClientGetTitleData()
    {
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(),
            result => {
                if (result.Data == null) Debug.Log("No MonsterName");
                else
                {
                    string[] temp = result.Data["recipt_code_accept"].Split('-');
                    ConfigManager.reciptCodeAccept = new List<string>();
                    foreach (var t in temp)
                    {
                        ConfigManager.reciptCodeAccept.Add(t);
                    }

                    List<MedalExp> medalExps = JsonConvert.DeserializeObject<List<MedalExp>>(result.Data["medal_exp"]);
                    ConfigManager.medals = new Dictionary<Medal, MedalExp>();
                    foreach (var medal in medalExps)
                    {
                        ConfigManager.medals.Add(medal.medal, medal);
                    }

                    if (result.Data.ContainsKey("map_configs"))
                    {
                        List<MapConfig> mapConfigs = JsonConvert.DeserializeObject<List<MapConfig>>(result.Data["map_configs"]);
                        foreach (var mapCfg in mapConfigs)
                        {
                            ConfigManager.mapConfigs.Add(mapCfg.map, mapCfg);
                        }
                    }

                    ConfigManager.battleTimes = JsonConvert.DeserializeObject<Dictionary<Map, int>>(result.Data["battle_map_times"]);

                    ConfigManager.discordLink = result.Data["discord_link"];
                    ConfigManager.landingPage = result.Data["lnading_page"];
                    ConfigManager.landingCharacter = result.Data["lnading_character"];
                    //GameManager.instan.iap = bool.Parse(result.Data["iap"]);

                    string jsonDailyReward = result.Data["daily_rewards"];
                    ConfigManager.dailyRewards = JsonConvert.DeserializeObject<List<DailyReward>>(jsonDailyReward);

                    string jsonQC = result.Data["qc_data"];
                    ConfigManager.QcDatas = JsonConvert.DeserializeObject<List<QCData>>(jsonQC);

                    if (result.Data.ContainsKey("price_per_crate"))
                    {
                        ConfigManager.pricePerCrate = int.Parse(result.Data["price_per_crate"]);
                    }

                    if (result.Data.ContainsKey("can_get_ticket_by_ads"))
                    {
                        ConfigManager.canGetTicketByAds = bool.Parse(result.Data["can_get_ticket_by_ads"]);
                    }
                    if (result.Data.ContainsKey("show_banner"))
                    {
                        ConfigManager.showBanner = bool.Parse(result.Data["show_banner"]);
                    }

                    if (result.Data.ContainsKey("multiplay"))
                    {
                        ConfigManager.multiplay = bool.Parse(result.Data["multiplay"]);
                    }

                    if (result.Data.ContainsKey("show_inter_back_home"))
                    {
                        ConfigManager.showInterBackHome = bool.Parse(result.Data["show_inter_back_home"]);
                    }

                    if (result.Data.ContainsKey("show_inter_die"))
                    {
                        ConfigManager.showInterDie = bool.Parse(result.Data["show_inter_die"]);
                    }

                    if (result.Data.ContainsKey("map_config"))
                    {
                        List<MapConfig> mapConfigs = JsonConvert.DeserializeObject<List<MapConfig>>(result.Data["map_config"]);
                        ConfigManager.mapConfigs.Clear();
                        foreach (var mapConfig in mapConfigs)
                        {
                            ConfigManager.mapConfigs.Add(mapConfig.map, mapConfig);
                        }
                    }

                    if (result.Data.ContainsKey("trading_data"))
                    {
                        ConfigManager.tradingData =
                            JsonConvert.DeserializeObject<TradingData>(result.Data["trading_data"]);
                    }
                    
                    if (result.Data.ContainsKey("character_stats"))
                    {
                        ConfigManager.characterStats =
                            JsonConvert.DeserializeObject<Dictionary<TF2Class, CharacterStats>>(result.Data["character_stats"]);
                    }
                    
                    if (result.Data.ContainsKey("weapon_stats"))
                    {
                        ConfigManager.weaponStats =
                            JsonConvert.DeserializeObject<Dictionary<WeaponId, WeaponStats>>(result.Data["weapon_stats"]);
                    }

                    ConfigManager.maxVideoPerDay = int.Parse(result.Data["max_reward_video_per_season"]);

                    //VirtualCurrncy v1 = new VirtualCurrncy() { currency = TF2Currency.CM, quantity = 10000 };
                    //VirtualCurrncy v2 = new VirtualCurrncy() { currency = TF2Currency.GK, quantity = 10 };
                    //ItemData it = new ItemData() { itemId = ItemId.demoman_bomber_knight, quantity = 1 };

                    //Transition transition = new Transition() { ID = 1, sourceCurrency = v1, sourceItem = it, targetCurrency = v2 };

                    //TradingData tr = new TradingData() { openDay = System.DayOfWeek.Wednesday, transitions = new Transition[] { transition } };
                    //string js = JsonConvert.SerializeObject(tr);
                    //Debug.Log(js);
                }
            },
            error => {
                Debug.Log("Got error getting titleData:");
                Debug.Log(error.GenerateErrorReport());
            }
        );
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
        loadingPanel.SetActive(true);
        slider.DOValue(1, 2f);
        StartCoroutine(WaitAllLoad());
    }

    IEnumerator WaitAllLoad()
    {
        yield return new WaitForSeconds(2);
        while (!loadedCategory)
        {
            yield return null;
        }
        SceneManager.LoadScene("Home");
    }
}
