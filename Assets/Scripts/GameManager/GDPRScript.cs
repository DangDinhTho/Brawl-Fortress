using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;

/// <summary>
/// Google UMP consent (GoogleMobileAds 11.x).
/// Call GatherConsent on every app launch, before MobileAds.Initialize.
/// </summary>
public class GDPRScript : MonoBehaviour
{
    public static GDPRScript instan;

    [HideInInspector]
    public bool consenDone { get => PlayerPrefs.GetInt("CONSEN", 0) == 1; set => PlayerPrefs.SetInt("CONSEN", value ? 1 : 0); }
    [HideInInspector]
    public bool consenShowed;

    [Tooltip("Hashed device IDs that see the EEA consent form in Editor / Development builds.")]
    public List<string> testDeviceHashedIds = new List<string> { "965E4A26737DF85475A353251709C315" };

    private void Awake()
    {
        instan = this;
    }

    /// <summary>True when the user must be able to reopen the consent form (e.g. a Settings button).</summary>
    public bool IsPrivacyOptionsRequired =>
        ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

    public bool CanRequestAds => ConsentInformation.CanRequestAds();

    /// <summary>
    /// Updates consent info and shows the form only if required.
    /// onComplete(canRequestAds) is invoked on the Unity main thread.
    /// </summary>
    public void GatherConsent(Action<bool> onComplete)
    {
        var request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
        };
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        request.ConsentDebugSettings = new ConsentDebugSettings
        {
            DebugGeography = DebugGeography.EEA,
            TestDeviceHashedIds = testDeviceHashedIds,
        };
#endif

        ConsentInformation.Update(request, updateError =>
        {
            if (updateError != null)
            {
                Debug.LogError("UMP update error: " + updateError.Message);
                Finish(onComplete);
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                if (formError != null)
                    Debug.LogError("UMP form error: " + formError.Message);
                else
                    consenDone = true;
                consenShowed = true;
                Finish(onComplete);
            });
        });
    }

    void Finish(Action<bool> onComplete)
    {
        MobileAdsEventExecutor.ExecuteInUpdate(() => onComplete?.Invoke(ConsentInformation.CanRequestAds()));
    }

    /// <summary>Re-open the consent form from a Settings button.</summary>
    public void ShowPrivacyOptions(Action onClosed = null)
    {
        ConsentForm.ShowPrivacyOptionsForm(error =>
        {
            if (error != null) Debug.LogError("UMP privacy options error: " + error.Message);
            MobileAdsEventExecutor.ExecuteInUpdate(() => onClosed?.Invoke());
        });
    }

    // Kept for backward compatibility.
    public void InitConsen()
    {
        GatherConsent(null);
    }
}
