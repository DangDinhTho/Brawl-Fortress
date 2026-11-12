#if Admob
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Google Mobile Ads 11.x.
/// Ad events can be raised on a background thread, so every callback that touches
/// Unity / game code is marshalled with MobileAdsEventExecutor.ExecuteInUpdate.
/// </summary>
public class AdmobManager : MonoBehaviour
{
    public static AdmobManager insan;
    private string testInterId = "ca-app-pub-3940256099942544/1033173712";
    private string testRewardId = "ca-app-pub-3940256099942544/5224354917";
    private string testNavtiveId = "ca-app-pub-3940256099942544/6300978111";
    private string testBannerId = "ca-app-pub-3940256099942544/6300978111";
    private string testAppOpenId = "ca-app-pub-3940256099942544/9257395921";

    private string testInterId_iOS = "ca-app-pub-3940256099942544/4411468910";
    private string testRewardId_iOS = "ca-app-pub-3940256099942544/1712485313";
    private string testNavtiveId_iOS = "ca-app-pub-3940256099942544/2934735716";
    private string testBannerId_iOS = "ca-app-pub-3940256099942544/2934735716";
    private string testAppOpenId_iOS = "ca-app-pub-3940256099942544/5575463023";

    public bool isTest;

    bool initStarted;
    bool sdkInitialized;

    /// <summary>True while any full-screen ad (inter / rewarded / app open) is on screen.</summary>
    bool fullScreenAdShowing;
    DateTime lastFullScreenAdClosedUtc = DateTime.MinValue;

    private void Awake()
    {
        if (insan != null && insan != this)
        {
            Destroy(gameObject);
            return;
        }
        insan = this;
        DontDestroyOnLoad(gameObject);
        CountLaunch();
        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
    }

    private void OnDestroy()
    {
        if (insan == this) AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
    }

    static void OnMainThread(Action action)
    {
        MobileAdsEventExecutor.ExecuteInUpdate(action);
    }

    void OnFullScreenAdOpened()
    {
        fullScreenAdShowing = true;
    }

    void OnFullScreenAdDismissed()
    {
        fullScreenAdShowing = false;
        lastFullScreenAdClosedUtc = DateTime.UtcNow;
    }

    public void InitAds()
    {
        if (initStarted) return;
        initStarted = true;

        if (isTest)
        {
#if UNITY_ANDROID
            _interAdUnitId = testInterId;
            _rewardAdUnitId = testRewardId;
            _bannerAdUnitId = testBannerId;
            _naviveAdUnitId = testNavtiveId;
            _appOpenAdUnitId = testAppOpenId;
#elif UNITY_IOS
            _interAdUnitId = testInterId_iOS;
            _rewardAdUnitId = testRewardId_iOS;
            _bannerAdUnitId = testBannerId_iOS;
            _naviveAdUnitId = testNavtiveId_iOS;
            _appOpenAdUnitId = testAppOpenId_iOS;
#endif
        }
        else
        {
#if UNITY_IOS
            _interAdUnitId = _interAdUnitId_iOS;
            _rewardAdUnitId = _rewardAdUnitId_iOS;
            _bannerAdUnitId = _bannerAdUnitId_iOS;
            _naviveAdUnitId = _naviveAdUnitId_iOS;
            _appOpenAdUnitId = _appOpenAdUnitId_iOS;
#endif
        }

        // Gather UMP consent first (every launch), then start the SDK.
        if (GDPRScript.instan != null)
        {
            GDPRScript.instan.GatherConsent(canRequestAds =>
            {
                if (canRequestAds) StartMobileAds();
                else Debug.LogWarning("UMP: cannot request ads yet (no consent).");
            });
        }
        else
        {
            StartMobileAds();
        }
    }

    // Must be called on the main thread.
    void StartMobileAds()
    {
        if (sdkInitialized) return;
        sdkInitialized = true;

        MobileAds.Initialize(initStatus => OnMainThread(() =>
        {
            LoadAppOpenAd();
            LoadInterstitialAd();
            LoadRewardedAd();
            LoadNavtiveAd();
            LoadBannerAd();
        }));
    }


    #region Inter
    public string _interAdUnitId = "ca-app-pub-8668381871889419/7411427403";
    public string _interAdUnitId_iOS = "ca-app-pub-3940256099942544/4411468910";

    private InterstitialAd _interstitialAd;
    private Action interCallback;

    public void LoadInterstitialAd()
    {
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        Debug.Log("Loading the interstitial ad.");
        var adRequest = new AdRequest();

        InterstitialAd.Load(_interAdUnitId, adRequest, (InterstitialAd ad, LoadAdError error) => OnMainThread(() =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("interstitial ad failed to load an ad with error : " + error);
                return;
            }

            Debug.Log("Interstitial ad loaded with response : " + ad.GetResponseInfo());
            _interstitialAd = ad;
            RegisterReloadInterHandler(ad);
        }));
    }

    public void ShowInterstitialAd(Action callback)
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            Debug.Log("Showing interstitial ad.");
            interCallback = callback;
            OnFullScreenAdOpened();
            _interstitialAd.Show();
        }
        else
        {
            callback?.Invoke();
            Debug.LogError("Interstitial ad is not ready yet.");
        }
    }

    private void RegisterReloadInterHandler(InterstitialAd interstitialAd)
    {
        interstitialAd.OnAdFullScreenContentClosed += () => OnMainThread(() =>
        {
            Debug.Log("Interstitial Ad full screen content closed.");
            OnFullScreenAdDismissed();
            LoadInterstitialAd();
            var cb = interCallback; interCallback = null;
            cb?.Invoke();
            AdsManager.OnShowAds();
        });

        interstitialAd.OnAdFullScreenContentFailed += (AdError error) => OnMainThread(() =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content with error : " + error);
            OnFullScreenAdDismissed();
            LoadInterstitialAd();
            var cb = interCallback; interCallback = null;
            cb?.Invoke();
        });
    }
    #endregion

    #region RewardAds
    public string _rewardAdUnitId = "ca-app-pub-8668381871889419/3437589033";
    public string _rewardAdUnitId_iOS = "ca-app-pub-3940256099942544/1712485313";

    private RewardedAd _rewardedAd;

    public void LoadRewardedAd()
    {
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        Debug.Log("Loading the rewarded ad.");
        var adRequest = new AdRequest();

        RewardedAd.Load(_rewardAdUnitId, adRequest, (RewardedAd ad, LoadAdError error) => OnMainThread(() =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
                return;
            }

            Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());
            _rewardedAd = ad;
            RegisterReloadVideoHandler(ad);
        }));
    }

    public bool RewardVideosReady()
    {
        return _rewardedAd != null && _rewardedAd.CanShowAd();
    }

    public void ShowRewardedAd(Action callback)
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            OnFullScreenAdOpened();
            _rewardedAd.Show((Reward reward) => OnMainThread(() =>
            {
                callback?.Invoke();
                AdsManager.OnShowAds();
            }));
        }
    }

    private void RegisterReloadVideoHandler(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () => OnMainThread(() =>
        {
            Debug.Log("Rewarded Ad full screen content closed.");
            OnFullScreenAdDismissed();
            LoadRewardedAd();
        });
        ad.OnAdFullScreenContentFailed += (AdError error) => OnMainThread(() =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content with error : " + error);
            OnFullScreenAdDismissed();
            LoadRewardedAd();
        });
    }
    #endregion

    #region App Open
    public string _appOpenAdUnitId = "ca-app-pub-3940256099942544/9257395921";      // TODO: real Android unit id
    public string _appOpenAdUnitId_iOS = "ca-app-pub-3940256099942544/5575463023";  // TODO: real iOS unit id

    [Header("App Open")]
    [Tooltip("App open ads are shown from this app launch onward (1 = first launch).")]
    public int appOpenFromLaunch = 2;
    [Tooltip("Minimum seconds in background before an app open ad is shown on return.")]
    public float appOpenMinBackgroundSeconds = 30f;
    [Tooltip("Cold start: show only if the ad is ready within this many seconds after launch.")]
    public float appOpenColdStartWindow = 8f;

    const string LaunchCountKey = "APP_LAUNCH_COUNT";
    static bool launchCounted;
    int launchCount;
    float appStartTime;
    bool coldStartPending;

    private AppOpenAd _appOpenAd;
    private DateTime _appOpenLoadedUtc;
    private bool appOpenLoading;
    private DateTime? backgroundSinceUtc;
    private bool suppressNextAppOpen;

    void CountLaunch()
    {
        appStartTime = Time.realtimeSinceStartup;
        if (!launchCounted)
        {
            launchCounted = true;
            launchCount = PlayerPrefs.GetInt(LaunchCountKey, 0) + 1;
            PlayerPrefs.SetInt(LaunchCountKey, launchCount);
            PlayerPrefs.Save();
        }
        else
        {
            launchCount = PlayerPrefs.GetInt(LaunchCountKey, 1);
        }
        coldStartPending = launchCount >= appOpenFromLaunch;
    }

    /// <summary>Call before leaving the app on purpose (IAP sheet, restore, external link...)
    /// so returning to the game does not show an app open ad.</summary>
    public void SuppressNextAppOpen()
    {
        suppressNextAppOpen = true;
    }

    bool IsAppOpenAdAvailable =>
        _appOpenAd != null && _appOpenAd.CanShowAd() && (DateTime.UtcNow - _appOpenLoadedUtc).TotalHours < 4;

    public void LoadAppOpenAd()
    {
        if (appOpenLoading || string.IsNullOrEmpty(_appOpenAdUnitId)) return;
        if (_appOpenAd != null)
        {
            _appOpenAd.Destroy();
            _appOpenAd = null;
        }

        appOpenLoading = true;
        AppOpenAd.Load(_appOpenAdUnitId, new AdRequest(), (AppOpenAd ad, LoadAdError error) => OnMainThread(() =>
        {
            appOpenLoading = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning("App open ad failed to load: " + error);
                return;
            }

            _appOpenAd = ad;
            _appOpenLoadedUtc = DateTime.UtcNow;
            RegisterAppOpenHandlers(ad);

            // Cold start (launch >= appOpenFromLaunch): show it if it is ready quickly enough.
            if (coldStartPending)
            {
                coldStartPending = false;
                if (Time.realtimeSinceStartup - appStartTime <= appOpenColdStartWindow)
                    TryShowAppOpenAd("cold start");
            }
        }));
    }

    private void RegisterAppOpenHandlers(AppOpenAd ad)
    {
        ad.OnAdFullScreenContentClosed += () => OnMainThread(() =>
        {
            OnFullScreenAdDismissed();
            LoadAppOpenAd();
        });
        ad.OnAdFullScreenContentFailed += (AdError error) => OnMainThread(() =>
        {
            Debug.LogWarning("App open ad failed to show: " + error);
            OnFullScreenAdDismissed();
            LoadAppOpenAd();
        });
    }

    bool CanShowAppOpenNow(out string reason)
    {
        reason = null;
        if (GameManager.instan != null && GameManager.instan.userData.ads_removed) { reason = "ads removed"; return false; }
        if (fullScreenAdShowing) { reason = "another full screen ad is showing"; return false; }
        if ((DateTime.UtcNow - lastFullScreenAdClosedUtc).TotalSeconds < 2) { reason = "an ad just closed"; return false; }
        if (ArenaManager.Instance != null) { reason = "in a match"; return false; }
        if (!IsAppOpenAdAvailable) { reason = "not loaded"; return false; }
        return true;
    }

    void TryShowAppOpenAd(string trigger)
    {
        if (!CanShowAppOpenNow(out var reason))
        {
            Debug.Log($"App open ({trigger}) skipped: {reason}");
            if (reason == "not loaded") LoadAppOpenAd();
            return;
        }
        Debug.Log($"Showing app open ad ({trigger})");
        OnFullScreenAdOpened();
        _appOpenAd.Show();
    }

    private void OnAppStateChanged(AppState state)
    {
        OnMainThread(() =>
        {
            if (state == AppState.Background)
            {
                // Leaving because of our own full screen ad does not count as "lost focus".
                backgroundSinceUtc = fullScreenAdShowing ? (DateTime?)null : DateTime.UtcNow;
                return;
            }

            // Foreground
            var since = backgroundSinceUtc;
            backgroundSinceUtc = null;
            if (suppressNextAppOpen)
            {
                suppressNextAppOpen = false;
                return;
            }
            if (!sdkInitialized || since == null) return;
            if (launchCount < appOpenFromLaunch) return;
            if ((DateTime.UtcNow - since.Value).TotalSeconds < appOpenMinBackgroundSeconds) return;

            TryShowAppOpenAd("resume");
        });
    }
    #endregion


    #region Navtive (MREC banner)
    public string _naviveAdUnitId = "ca-app-pub-8668381871889419/3663754086";
    public string _naviveAdUnitId_iOS = "ca-app-pub-3940256099942544/2934735716";

    BannerView _naviveView;

    public void CreateNavtiveView()
    {
        Debug.Log("Creating MREC view");
        if (_naviveView != null)
        {
            DestroyNaviveView();
        }

        _naviveView = new BannerView(_naviveAdUnitId, AdSize.MediumRectangle, AdPosition.TopLeft);
        var view = _naviveView;
        view.OnBannerAdLoaded += () => OnMainThread(() =>
        {
            Debug.Log("MREC view loaded an ad with response : " + view.GetResponseInfo());
            HideNavtive();
        });
        view.OnBannerAdLoadFailed += (LoadAdError error) => OnMainThread(() =>
            Debug.LogError("MREC view failed to load an ad with error : " + error));
    }

    public void LoadNavtiveAd()
    {
        if (_naviveView == null)
        {
            CreateNavtiveView();
        }
        Debug.Log("Loading MREC ad.");
        _naviveView.LoadAd(new AdRequest());
    }

    public void ShowNavtive() => ShowNavtive(AdCorner.TopLeft);

    public void ShowNavtive(AdCorner position)
    {
        if (_naviveView == null) return;
        _naviveView.SetPosition(ToAdPosition(position));
        _naviveView.Show();
    }

    static AdPosition ToAdPosition(AdCorner c)
    {
        switch (c)
        {
            case AdCorner.Top: return AdPosition.Top;
            case AdCorner.Bottom: return AdPosition.Bottom;
            case AdCorner.TopRight: return AdPosition.TopRight;
            case AdCorner.BottomLeft: return AdPosition.BottomLeft;
            case AdCorner.BottomRight: return AdPosition.BottomRight;
            case AdCorner.Center: return AdPosition.Center;
            default: return AdPosition.TopLeft;
        }
    }

    public void HideNavtive()
    {
        if (_naviveView != null) _naviveView.Hide();
    }

    public void DestroyNaviveView()
    {
        if (_naviveView != null)
        {
            Debug.Log("Destroying MREC view.");
            _naviveView.Destroy();
            _naviveView = null;
        }
    }
    #endregion

    #region Banner
    public string _bannerAdUnitId = "ca-app-pub-8668381871889419/3863064634";
    public string _bannerAdUnitId_iOS = "ca-app-pub-3940256099942544/2934735716";

    BannerView _bannerView;

    public void CreateBannerView()
    {
        Debug.Log("Creating banner view");
        if (_bannerView != null)
        {
            DestroyBannerView();
        }

        _bannerView = new BannerView(_bannerAdUnitId, AdSize.Banner, AdPosition.TopLeft);
        var view = _bannerView;
        view.OnBannerAdLoaded += () => OnMainThread(() =>
        {
            Debug.Log("Banner view loaded an ad with response : " + view.GetResponseInfo());
            HideBanner();
        });
        view.OnBannerAdLoadFailed += (LoadAdError error) => OnMainThread(() =>
            Debug.LogError("Banner view failed to load an ad with error : " + error));
    }

    public void LoadBannerAd()
    {
        if (!ConfigManager.showBanner) return;
        if (_bannerView == null)
        {
            CreateBannerView();
        }
        Debug.Log("Loading banner ad.");
        _bannerView.LoadAd(new AdRequest());
    }

    public void ShowBanner()
    {
        if (_bannerView != null) _bannerView.Show();
    }

    public void HideBanner()
    {
        if (_bannerView != null) _bannerView.Hide();
    }

    public void DestroyBannerView()
    {
        if (_bannerView != null)
        {
            Debug.Log("Destroying banner view.");
            _bannerView.Destroy();
            _bannerView = null;
        }
    }
    #endregion
}

#endif
