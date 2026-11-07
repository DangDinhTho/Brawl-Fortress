//using System;
//using Photon.Pun;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Yodo1.MAS;

//public class Yodo1AdsManager : MonoBehaviour
//{
//    public static Yodo1AdsManager instan;
//    private int typeReward;

//    private Action InterAdsCallBack;
//    private Action RewardAdsCallBack;
//    private Yodo1U3dNativeAdView nativeAdView;
//    //private Yodo1U3dBannerAdView bannerAdView;
//    [HideInInspector]
//    public bool inited;
//    private int videoShowed = 0;

//    private void Awake()
//    {
//        instan = this;
//        inited = false;
//        DontDestroyOnLoad(gameObject);
//    }

//    public void StartInitYodo()
//    {
//        Yodo1AdBuildConfig config =
//        new Yodo1AdBuildConfig().enableUserPrivacyDialog(true);
//        Yodo1U3dMas.SetAdBuildConfig(config);

//        Yodo1U3dMasCallback.OnSdkInitializedEvent += (success, error) =>
//        {
//            Debug.Log("[Yodo1 Mas] OnSdkInitializedEvent, success:" + success + ", error: " + error.ToString());
//            if (success)
//            {
//                OnYoDoInited();
//                Debug.Log("[Yodo1 Mas] The initialization has succeeded");
//            }
//            else
//            {
//                Debug.Log("[Yodo1 Mas] The initialization has failed");
//            }
//        };

//        Yodo1U3dMas.InitializeMasSdk();

        
//    }

//    public void ResetVideoShowed()
//    {
//        videoShowed = 0;
//    }


//    public void OnYoDoInited()
//    {
//        inited = true;

//        this.InitializeInterstitial();
//        this.InitializeRewardedAds();

//        this.RequestRewardedAds();
//        this.RequestInterstitial();
//        //RequestNative();
//        //RequestBanner();
//    }

//    // public void RequestAdsOnStart()
//    // {
//    //     RequestBanner();
//    // }

//    #region InterAds
//    private void InitializeInterstitial()
//    {
//        // Instantiate
//        Yodo1U3dInterstitialAd.GetInstance();

//        // Ad Events
//        Yodo1U3dInterstitialAd.GetInstance().OnAdLoadedEvent += OnInterstitialAdLoadedEvent;
//        Yodo1U3dInterstitialAd.GetInstance().OnAdLoadFailedEvent += OnInterstitialAdLoadFailedEvent;
//        Yodo1U3dInterstitialAd.GetInstance().OnAdOpenedEvent += OnInterstitialAdOpenedEvent;
//        Yodo1U3dInterstitialAd.GetInstance().OnAdOpenFailedEvent += OnInterstitialAdOpenFailedEvent;
//        Yodo1U3dInterstitialAd.GetInstance().OnAdClosedEvent += OnInterstitialAdClosedEvent;
//    }

//    private void RequestInterstitial()
//    {
//        Yodo1U3dInterstitialAd.GetInstance().LoadAd();
//    }

//    public void ShowInterstitial(Action callback = null)
//    {
//        if (GameManager.instan.userData.ads_removed)
//        {
//            callback?.Invoke();
//            return;
//        }

        
//        bool isLoaded = Yodo1U3dInterstitialAd.GetInstance().IsLoaded();

//        if (isLoaded)
//        {
//            this.InterAdsCallBack = callback;
//            Yodo1U3dInterstitialAd.GetInstance().ShowAd();
//        }
//        else
//        {
//            callback?.Invoke();
//        }
//    }

//    private void OnInterstitialAdLoadedEvent(Yodo1U3dInterstitialAd ad)
//    {
//        Debug.Log("[Yodo1 Mas] OnInterstitialAdLoadedEvent event received");
//    }

//    private void OnInterstitialAdLoadFailedEvent(Yodo1U3dInterstitialAd ad, Yodo1U3dAdError adError)
//    {
//        Debug.Log("[Yodo1 Mas] OnInterstitialAdLoadFailedEvent event received with error: " + adError.ToString());
//    }

//    private void OnInterstitialAdOpenedEvent(Yodo1U3dInterstitialAd ad)
//    {
//        Debug.Log("[Yodo1 Mas] OnInterstitialAdOpenedEvent event received");
//    }

//    private void OnInterstitialAdOpenFailedEvent(Yodo1U3dInterstitialAd ad, Yodo1U3dAdError adError)
//    {
//        Debug.Log("[Yodo1 Mas] OnInterstitialAdOpenFailedEvent event received with error: " + adError.ToString());
//        // Load the next ad
//        this.RequestInterstitial();
//        InterAdsCallBack?.Invoke();
//    }

//    private void OnInterstitialAdClosedEvent(Yodo1U3dInterstitialAd ad)
//    {
//        Debug.Log("[Yodo1 Mas] OnInterstitialAdClosedEvent event received");
//        // Load the next ad
//        this.RequestInterstitial();
//        InterAdsCallBack?.Invoke();
//    }
//    #endregion

//    #region RewardAds
//    private void InitializeRewardedAds()
//    {
//        // Instantiate
//        Yodo1U3dRewardAd.GetInstance();

//        // Ad Events
//        Yodo1U3dRewardAd.GetInstance().OnAdLoadedEvent += OnRewardAdLoadedEvent;
//        Yodo1U3dRewardAd.GetInstance().OnAdLoadFailedEvent += OnRewardAdLoadFailedEvent;
//        Yodo1U3dRewardAd.GetInstance().OnAdOpenedEvent += OnRewardAdOpenedEvent;
//        Yodo1U3dRewardAd.GetInstance().OnAdOpenFailedEvent += OnRewardAdOpenFailedEvent;
//        Yodo1U3dRewardAd.GetInstance().OnAdClosedEvent += OnRewardAdClosedEvent;
//        Yodo1U3dRewardAd.GetInstance().OnAdEarnedEvent += OnRewardAdEarnedEvent;
//    }

    
//    public void ShowGoldkeyHomeReward(Action callback)
//    {
//        if(videoShowed > ConfigManager.maxVideoPerDay)
//        {
//            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("reward_video_max"));
//            return;
//        }
//        this.RewardAdsCallBack = callback;
//        ShowRewardedAds();
//    }
    
//    public void ShowTradingReward(Action callback)
//    {
//#if UNITY_EDITOR
//        callback?.Invoke();
//        return;
//#endif
//        if (GameManager.instan.userData.ads_removed)
//        {
//            RewardAdsCallBack?.Invoke();
//            return;
//        }
//        this.RewardAdsCallBack = callback;
//        ShowRewardedAds();
//    }
    

//    public void ShowRewardOther(Action callback)
//    {
//        this.RewardAdsCallBack = callback;
//        ShowRewardedAds();
//    }

//    private void RequestRewardedAds()
//    {
//        Yodo1U3dRewardAd.GetInstance().LoadAd();
//    }

//    private void ShowRewardedAds()
//    {
        
//        bool isLoaded = Yodo1U3dRewardAd.GetInstance().IsLoaded();

//        if (isLoaded) Yodo1U3dRewardAd.GetInstance().ShowAd();
//        else
//        {
//            RewardAdsCallBack = null;
//            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("no_reward_availble"));
//        }
//    }

//    private void OnRewardAdLoadedEvent(Yodo1U3dRewardAd ad)
//    {
//        Debug.Log("[Yodo1 Mas] OnRewardAdLoadedEvent event received");
//    }

//    private void OnRewardAdLoadFailedEvent(Yodo1U3dRewardAd ad, Yodo1U3dAdError adError)
//    {
//        Debug.Log("[Yodo1 Mas] OnRewardAdLoadFailedEvent event received with error: " + adError.ToString());
//    }

//    private void OnRewardAdOpenedEvent(Yodo1U3dRewardAd ad)
//    {
//        Debug.Log("[Yodo1 Mas] OnRewardAdOpenedEvent event received");
//    }

//    private void OnRewardAdOpenFailedEvent(Yodo1U3dRewardAd ad, Yodo1U3dAdError adError)
//    {
//        Debug.Log("[Yodo1 Mas] OnRewardAdOpenFailedEvent event received with error: " + adError.ToString());
//        // Load the next ad
//        this.RequestRewardedAds();
//        RewardAdsCallBack = null;
//        GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("no_reward_availble"));
//    }

//    private void OnRewardAdClosedEvent(Yodo1U3dRewardAd ad)
//    {
//        Debug.Log("[Yodo1 Mas] OnRewardAdClosedEvent event received");
//        // Load the next ad
//        this.RequestRewardedAds();
//    }

//    private void OnRewardAdEarnedEvent(Yodo1U3dRewardAd ad)
//    {
//        Debug.Log("[Yodo1 Mas] OnRewardAdEarnedEvent event received");
//        // Add your reward code here
//        RewardAdsCallBack?.Invoke();
//        RewardAdsCallBack = null;
//        videoShowed++;
        
//        if (!PhotonNetwork.IsConnected)
//        {
//            PhotonNetwork.ConnectUsingSettings();
//        }
//    }
//    #endregion

//    #region NavtiveAds
//    public void RequestNative()
//    {
//        //Clean up native before reusing
//        if (nativeAdView != null)
//        {
//            nativeAdView.Destroy();
//            nativeAdView = null;
//        }

//        //Create a 375x200 native at top of the screen
//       nativeAdView = new Yodo1U3dNativeAdView(Yodo1U3dNativeAdPosition.NativeLeft | Yodo1U3dNativeAdPosition.NativeTop, 0, 0, 600, 500);

//        //Ad Events
//        nativeAdView.OnAdLoadedEvent += OnNativeAdLoadedEvent;
//        nativeAdView.OnAdFailedToLoadEvent += OnNativeAdFailedToLoadEvent;

//        //Load native ads, the native ad will be displayed automatically after loaded
//        nativeAdView.LoadAd();
//    }

//    private void OnNativeAdLoadedEvent(Yodo1U3dNativeAdView adView)
//    {
//        //Native ad is ready to be shown.
//        Debug.Log("[Yodo1 Mas] OnNativeAdLoadedEvent event received");
//        nativeAdView.Hide();
//    }

//    private void OnNativeAdFailedToLoadEvent(Yodo1U3dNativeAdView adView, Yodo1U3dAdError adError)
//    {
//        Debug.Log("[Yodo1 Mas] OnNativeAdFailedToLoadEvent event received with error: " + adError.ToString());
//    }

//    public void ShowNavtiveAds()
//    {
//        if (GameManager.instan.userData.ads_removed) return;
//        nativeAdView.Show();
//    }

//    public void HideNavtiveAds()
//    {
//        if (nativeAdView == null) return;
//        nativeAdView.Hide();
//    }

//    #endregion

//    //#region BannerAds
//    //public void RequestBanner()
//    //{
//    //    // Clean up banner before reusing
//    //    if (bannerAdView != null)
//    //    {
//    //        bannerAdView.Destroy();
//    //        bannerAdView = null;
//    //    }

//    //    // Create a 320x50 banner at top of the screen
//    //    bannerAdView = new Yodo1U3dBannerAdView(Yodo1U3dBannerAdSize.AdaptiveBanner, Yodo1U3dBannerAdPosition.BannerBottom | Yodo1U3dBannerAdPosition.BannerHorizontalCenter);

//    //    // Ad Events
//    //    bannerAdView.OnAdLoadedEvent += OnBannerAdLoadedEvent;
//    //    bannerAdView.OnAdFailedToLoadEvent += OnBannerAdFailedToLoadEvent;
//    //    bannerAdView.OnAdOpenedEvent += OnBannerAdOpenedEvent;
//    //    bannerAdView.OnAdFailedToOpenEvent += OnBannerAdFailedToOpenEvent;
//    //    bannerAdView.OnAdClosedEvent += OnBannerAdClosedEvent;

//    //    // Load banner ads, the banner ad will be displayed automatically after loaded
//    //    bannerAdView.LoadAd();
//    //}

//    //private void OnBannerAdLoadedEvent(Yodo1U3dBannerAdView adView)
//    //{
//    //    // Banner ad is ready to be shown.
//    //    Debug.Log("[Yodo1 Mas] OnBannerAdLoadedEvent event received");
//    //    bannerAdView.Hide();
//    //}

//    //private void OnBannerAdFailedToLoadEvent(Yodo1U3dBannerAdView adView, Yodo1U3dAdError adError)
//    //{
//    //    Debug.Log("[Yodo1 Mas] OnBannerAdFailedToLoadEvent event received with error: " + adError.ToString());
//    //}

//    //private void OnBannerAdOpenedEvent(Yodo1U3dBannerAdView adView)
//    //{
//    //    Debug.Log("[Yodo1 Mas] OnBannerAdOpenedEvent event received");
//    //}

//    //private void OnBannerAdFailedToOpenEvent(Yodo1U3dBannerAdView adView, Yodo1U3dAdError adError)
//    //{
//    //    Debug.Log("[Yodo1 Mas] OnBannerAdFailedToOpenEvent event received with error: " + adError.ToString());
//    //}

//    //private void OnBannerAdClosedEvent(Yodo1U3dBannerAdView adView)
//    //{
//    //    Debug.Log("[Yodo1 Mas] OnBannerAdClosedEvent event received");
//    //}

//    //public void ShowBanner()
//    //{
//    //    //if (!GameManagerAll.instan.IsShowAds()) return;
//    //    bannerAdView.Show();
//    //}

//    //public void HideBanner()
//    //{
//    //    if (bannerAdView == null) return;
//    //    bannerAdView.Hide();
//    //}

//    //#endregion
//}
