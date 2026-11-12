#if Admob
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using System;
using UnityEngine;

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

    private string testInterId_iOS = "ca-app-pub-3940256099942544/4411468910";
    private string testRewardId_iOS = "ca-app-pub-3940256099942544/1712485313";
    private string testNavtiveId_iOS = "ca-app-pub-3940256099942544/2934735716";
    private string testBannerId_iOS = "ca-app-pub-3940256099942544/2934735716";

    public bool isTest;

    bool initStarted;
    bool sdkInitialized;

    private void Awake()
    {
        insan = this;
        DontDestroyOnLoad(gameObject);
    }

    static void OnMainThread(Action action)
    {
        MobileAdsEventExecutor.ExecuteInUpdate(action);
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
#elif UNITY_IOS
            _interAdUnitId = testInterId_iOS;
            _rewardAdUnitId = testRewardId_iOS;
            _bannerAdUnitId = testBannerId_iOS;
            _naviveAdUnitId = testNavtiveId_iOS;
#endif
        }
        else
        {
#if UNITY_IOS
            _interAdUnitId = _interAdUnitId_iOS;
            _rewardAdUnitId = _rewardAdUnitId_iOS;
            _bannerAdUnitId = _bannerAdUnitId_iOS;
            _naviveAdUnitId = _naviveAdUnitId_iOS;
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
            LoadInterstitialAd();
            LoadRewardedAd();
            if (!isTest)
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
            LoadInterstitialAd();
            var cb = interCallback; interCallback = null;
            cb?.Invoke();
            AdsManager.OnShowAds();
        });

        interstitialAd.OnAdFullScreenContentFailed += (AdError error) => OnMainThread(() =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content with error : " + error);
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
            LoadRewardedAd();
        });
        ad.OnAdFullScreenContentFailed += (AdError error) => OnMainThread(() =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content with error : " + error);
            LoadRewardedAd();
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

    public void ShowNavtive()
    {
        if (_naviveView != null) _naviveView.Show();
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
