using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdsManager : MonoBehaviour
{
    public static AdsManager instan;
    //public delegate void CallBack();

    //private CallBack interAdsCallBack;
    //private CallBack rewardAdsCallBack;
    private Action rewardAdsCallBack;
    // private CallBack tradingAdsCallBack;
    //private CallBack navtiveAdsCallBack;
    private int rewardAdsCount;
    public static int showAdsCount { get => PlayerPrefs.GetInt("SHOW_ADS_COUNT", 0); set => PlayerPrefs.SetInt("SHOW_ADS_COUNT", value); }
    public static int showRewardAdsCount { get => PlayerPrefs.GetInt("SHOW_REWARD_ADS_COUNT", 0); set => PlayerPrefs.SetInt("SHOW_REWARD_ADS_COUNT", value); }

    private void Awake()
    {
        if(instan != null)
        {
            Destroy(instan.gameObject);
        }
        instan = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public static void OnShowAds()
    {
        showAdsCount++;
        if(showAdsCount <= 10 || showAdsCount == 20 || showAdsCount == 50 || showAdsCount == 100)
        {
            GameManager.instan.firebaseController.LogEvent($"paid_ad_impression_{showAdsCount}");
        }
    }

    public static void OnShowRewardAds()
    {
        showRewardAdsCount++;
        if (showRewardAdsCount <= 10 || showRewardAdsCount == 15 || showRewardAdsCount == 20 || showRewardAdsCount == 50)
        {
            GameManager.instan.firebaseController.LogEvent($"complete_rwd_{showRewardAdsCount}");
        }
    }

    public void InitAds()
    {
#if YODO1
        Yodo1AdsManager.instan.StartInitYodo();
#endif

#if Admob
        AdmobManager.insan.InitAds();
#endif
    }


    public void ResetAdsCount()
    {
        rewardAdsCount = 0;
    }


    public void LoadNavtiveAds()
    {
        if (GameManager.instan.userData.ads_removed) return;
#if YODO1
        Yodo1AdsManager.instan.RequestNative();
#endif

#if Admob
        AdmobManager.insan.LoadNavtiveAd();
#endif
    }


    public void ShowInterAds(Action callBack, bool resume = false)
    {
        if (GameManager.instan.userData.ads_removed)
        {
#if YODO1
            OnInterAdsDone();
#endif

#if Admob
            callBack?.Invoke();
#endif
        }
        else
        {
#if YODO1
interAdsCallBack = callBack;
            Yodo1AdsManager.instan.ShowInterstitial();
#endif

#if Admob
            AdmobManager.insan.ShowInterstitialAd(callBack);
            if (resume)
            {
                Time.timeScale = 1;
            }
#endif
        }
    }

    public void ShowRewardAds(Action callBack)
    {
        //if (rewardAdsCount > ConfigManager.maxVideoPerDay)
        //{
        //    GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("reward_video_max"));
        //    return;
        //}
        rewardAdsCallBack = callBack;
#if YODO1

        Yodo1AdsManager.instan.ShowRewardAds();
#endif

#if Admob
        if (AdmobManager.insan.RewardVideosReady())
            AdmobManager.insan.ShowRewardedAd(OnRewardAdsDone);
        else
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("no_reward_availble"));
#endif
    }

    public void ShowTradingAds(Action callBack)
    {
        rewardAdsCallBack = callBack;
        if (GameManager.instan.userData.ads_removed)
        {
            OnRewardAdsDone();
        }
        else
        {
#if YODO1
            Yodo1AdsManager.instan.ShowRewardAds();
#endif

#if Admob
            if (AdmobManager.insan.RewardVideosReady())
                AdmobManager.insan.ShowRewardedAd(OnRewardAdsDone);
            else
                GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("no_reward_availble"));
#endif
        }
    }

    public void ShowNavtiveAds()
    {
        if (GameManager.instan.userData.ads_removed)
        {
            return;
        }
        else
        {
#if YODO1
            Yodo1AdsManager.instan.ShowNavtiveAds();
#endif

#if Admob
            AdmobManager.insan.ShowNavtive();
#endif
        }
    }

    public void HideNavtiveAds()
    {
#if YODO1
        Yodo1AdsManager.instan.HideNavtiveAds();
#endif

#if Admob
        AdmobManager.insan.HideNavtive();
#endif
    }

    public void ShowBanner()
    {
        if (GameManager.instan.userData.ads_removed || !ConfigManager.showBanner)
        {
            return;
        }
#if Admob
            AdmobManager.insan.ShowBanner();
#endif
    }

    public void HideBanner()
    {
#if Admob
        AdmobManager.insan.HideBanner();
#endif
    }


#if YODO1
    public void OnInterAdsDone()
    {
        interAdsCallBack?.Invoke();
        interAdsCallBack = null;
    }
#endif

    public void OnRewardAdsDone()
    {
        rewardAdsCount++;
        rewardAdsCallBack?.Invoke();
        rewardAdsCallBack = null;
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }




}
