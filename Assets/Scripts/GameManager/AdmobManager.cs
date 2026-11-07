#if Admob
using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    private void Awake()
    {
        insan = this;
        DontDestroyOnLoad(gameObject);
    }

    public void InitAds()
    {
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

        MobileAds.Initialize(initStatus => {
            LoadInterstitialAd();
            LoadRewardedAd();
            if (!isTest)
                LoadNavtiveAd();
            LoadBannerAd();
        });
    }


#region Inter
    // These ad units are configured to always serve test ads.
//#if UNITY_ANDROID
    public string _interAdUnitId = "ca-app-pub-8668381871889419/7411427403";
//#elif UNITY_IPHONE
  public string _interAdUnitId_iOS = "ca-app-pub-3940256099942544/4411468910";
//#else
  //private string _adUnitId = "unused";
//#endif

    private InterstitialAd _interstitialAd;

  /// <summary>
  /// Loads the interstitial ad.
  /// </summary>
  public void LoadInterstitialAd()
    {
        // Clean up the old ad before loading a new one.
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        Debug.Log("Loading the interstitial ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        InterstitialAd.Load(_interAdUnitId, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
              // if error is not null, the load request failed.
              if (error != null || ad == null)
                {
                    Debug.LogError("interstitial ad failed to load an ad " +
                                   "with error : " + error);
                    return;
                }

                Debug.Log("Interstitial ad loaded with response : "
                          + ad.GetResponseInfo());

                _interstitialAd = ad;
                RegisterReloadInterHandler(ad);
            });
    }

    /// <summary>
    /// Shows the interstitial ad.
    /// </summary>
    /// 
    private Action interCallback;
    public void ShowInterstitialAd(Action callback)
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            Debug.Log("Showing interstitial ad.");
            this.interCallback = callback;
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
        // Raised when the ad closed full screen content.
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial Ad full screen content closed.");

            // Reload the ad so that we can show another as soon as possible.
            LoadInterstitialAd();
            interCallback?.Invoke();
            AdsManager.OnShowAds();
        };

        // Raised when the ad failed to open full screen content.
        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content " +
                           "with error : " + error);

            // Reload the ad so that we can show another as soon as possible.
            LoadInterstitialAd();
            interCallback?.Invoke();
        };
    }

#endregion

#region RewardAds

    // These ad units are configured to always serve test ads.
//#if UNITY_ANDROID
    public string _rewardAdUnitId = "ca-app-pub-8668381871889419/3437589033";
//#elif UNITY_IPHONE
  public string _rewardAdUnitId_iOS = "ca-app-pub-3940256099942544/1712485313";
//#else
 // private string _adUnitId = "unused";
//#endif

    private RewardedAd _rewardedAd;

    /// <summary>
    /// Loads the rewarded ad.
    /// </summary>
    public void LoadRewardedAd()
    {
        // Clean up the old ad before loading a new one.
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        Debug.Log("Loading the rewarded ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        RewardedAd.Load(_rewardAdUnitId, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
              // if error is not null, the load request failed.
              if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad " +
                                   "with error : " + error);
                    return;
                }

                Debug.Log("Rewarded ad loaded with response : "
                          + ad.GetResponseInfo());

                _rewardedAd = ad;
                RegisterReloadVideoHandler(ad);
            });
    }

    public bool RewardVideosReady()
    {
        return _rewardedAd != null && _rewardedAd.CanShowAd();
    }

    public void ShowRewardedAd(Action callback)
    {
        //const string rewardMsg =
            //"Rewarded ad rewarded the user. Type: {0}, amount: {1}.";

        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((Reward reward) =>
            {
                // TODO: Reward the user.
                //Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
                callback?.Invoke();
                AdsManager.OnShowAds();
            });
        }
    }

    private void RegisterReloadVideoHandler(RewardedAd ad)
    {
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded Ad full screen content closed.");

            // Reload the ad so that we can show another as soon as possible.
            LoadRewardedAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content " +
                           "with error : " + error);

            // Reload the ad so that we can show another as soon as possible.
            LoadRewardedAd();
        };
    }

#endregion


#region Navtive
    // These ad units are configured to always serve test ads.
//#if UNITY_ANDROID
    public string _naviveAdUnitId = "ca-app-pub-8668381871889419/3663754086";
//#elif UNITY_IPHONE
  public string _naviveAdUnitId_iOS = "ca-app-pub-3940256099942544/2934735716";
//#else
 // private string _adUnitId = "unused";
//#endif

    BannerView _naviveView;

    /// <summary>
    /// Creates a 320x50 banner view at top of the screen.
    /// </summary>
    public void CreateNavtiveView()
    {
        Debug.Log("Creating banner view");

        // If we already have a banner, destroy the old one.
        if (_naviveView != null)
        {
            DestroyNaviveView();
        }

        // Create a 320x50 banner at top of the screen
        AdSize size = AdSize.MediumRectangle;
        _naviveView = new BannerView(_naviveAdUnitId, size, AdPosition.TopLeft);
    }

    /// <summary>
    /// Creates the banner view and loads a banner ad.
    /// </summary>
    public void LoadNavtiveAd()
    {
        // create an instance of a banner view first.
        if (_naviveView == null)
        {
            CreateNavtiveView();
        }

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        Debug.Log("Loading banner ad.");
        _naviveView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner view loaded an ad with response : "
                + _naviveView.GetResponseInfo());
            HideNavtive();
        };
        _naviveView.LoadAd(adRequest);
    }

    public void ShowNavtive()
    {
        if (_naviveView != null)
        {
            _naviveView.Show();
        }
    }

    public void HideNavtive()
    {
        if (_naviveView != null)
        {
            _naviveView.Hide();
        }
    }

    /// <summary>
    /// Destroys the banner view.
    /// </summary>
    public void DestroyNaviveView()
    {
        if (_naviveView != null)
        {
            Debug.Log("Destroying banner view.");
            _naviveView.Destroy();
            _naviveView = null;
        }
    }
#endregion

#region Banner

    // These ad units are configured to always serve test ads.
//#if UNITY_ANDROID
    public string _bannerAdUnitId = "ca-app-pub-8668381871889419/3863064634";
//#elif UNITY_IPHONE
  public string _bannerAdUnitId_iOS = "ca-app-pub-3940256099942544/2934735716";
//#else
  //private string _adUnitId = "unused";
//#endif

    BannerView _bannerView;

    /// <summary>
    /// Creates a 320x50 banner view at top of the screen.
    /// </summary>
    public void CreateBannerView()
    {
        Debug.Log("Creating banner view");

        // If we already have a banner, destroy the old one.
        if (_bannerView != null)
        {
            DestroyBannerView();
        }

        // Create a 320x50 banner at top of the screen
        _bannerView = new BannerView(_bannerAdUnitId, AdSize.Banner, AdPosition.TopLeft);
    }

    /// <summary>
    /// Creates the banner view and loads a banner ad.
    /// </summary>
    public void LoadBannerAd()
    {
        if (!ConfigManager.showBanner) return;
        // create an instance of a banner view first.
        if (_bannerView == null)
        {
            CreateBannerView();
        }

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        Debug.Log("Loading banner ad.");
        _bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner view loaded an ad with response : "
                + _bannerView.GetResponseInfo());
            HideBanner();
        };
        _bannerView.LoadAd(adRequest);
    }

    public void ShowBanner()
    {
        if (_bannerView != null)
        {
            _bannerView.Show();
        }
    }

    public void HideBanner()
    {
        if (_bannerView != null)
        {
            _bannerView.Hide();
        }
    }

    /// <summary>
    /// Destroys the banner view.
    /// </summary>
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