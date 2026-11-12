using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Firebase 14.x: Analytics + Remote Config.
/// </summary>
public class FirebaseController : MonoBehaviour
{
    DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
    protected bool firebaseInitialized = false;

    // Events logged before Firebase is ready are queued and flushed after init.
    readonly List<Action> pendingAnalytics = new List<Action>();

    #region Remote Config
    /// <summary>In-app default values. Keys must match the Remote Config console.</summary>
    public static readonly Dictionary<string, object> RemoteConfigDefaults = new Dictionary<string, object>
    {
        // { "show_banner", false },
    };

    /// <summary>Fired on the main thread after the first fetch+activate (success or not) and after every realtime update.</summary>
    public static event Action OnRemoteConfigUpdated;
    public bool RemoteConfigReady { get; private set; }
    bool realtimeListenerAdded;
    #endregion

    public virtual void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }

    void InitializeFirebase()
    {
        DebugLog("Enabling data collection.");
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        FirebaseAnalytics.SetSessionTimeoutDuration(new TimeSpan(0, 30, 0));
        firebaseInitialized = true;

        if (!PlayerPrefs.HasKey("FIRST_OPEN"))
        {
            LogEvent("first_open");
            PlayerPrefs.SetInt("FIRST_OPEN", 1);
        }

        foreach (var a in pendingAnalytics) a();
        pendingAnalytics.Clear();

        InitializeRemoteConfig();
    }

    void RunAnalytics(Action action)
    {
        if (firebaseInitialized) action();
        else pendingAnalytics.Add(action);
    }

    #region Analytics
    public void AnalyticsLogin()
    {
        RunAnalytics(() => FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLogin));
    }

    public void LogEvent(string eventName, string paramName, string value)
    {
        RunAnalytics(() => FirebaseAnalytics.LogEvent(eventName, paramName, value));
    }

    public void LogEvent(string eventName)
    {
        RunAnalytics(() => FirebaseAnalytics.LogEvent(eventName));
    }

    public void AnalyticsLevelUp()
    {
        RunAnalytics(() => FirebaseAnalytics.LogEvent(
          FirebaseAnalytics.EventLevelUp,
          new Parameter(FirebaseAnalytics.ParameterLevel, 5),
          new Parameter(FirebaseAnalytics.ParameterCharacter, "mrspoon"),
          new Parameter("hit_accuracy", 3.14f)));
    }

    public void ResetAnalyticsData()
    {
        RunAnalytics(FirebaseAnalytics.ResetAnalyticsData);
    }

    public Task<string> DisplayAnalyticsInstanceId()
    {
        return FirebaseAnalytics.GetAnalyticsInstanceIdAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled) DebugLog("App instance ID fetch was canceled.");
            else if (task.IsFaulted) DebugLog("Encounted an error fetching app instance ID " + task.Exception);
            else DebugLog("App instance ID: " + task.Result);
            return task;
        }).Unwrap();
    }
    #endregion

    #region Remote Config
    void InitializeRemoteConfig()
    {
        var rc = FirebaseRemoteConfig.DefaultInstance;
        var settings = new ConfigSettings
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            MinimumFetchIntervalInMilliseconds = 0,
#else
            MinimumFetchIntervalInMilliseconds = 60 * 60 * 1000,
#endif
            FetchTimeoutInMilliseconds = 10 * 1000
        };

        rc.SetConfigSettingsAsync(settings)
          .ContinueWithOnMainThread(_ => rc.SetDefaultsAsync(RemoteConfigDefaults))
          .Unwrap()
          .ContinueWithOnMainThread(_ => rc.FetchAndActivateAsync())
          .Unwrap()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted || task.IsCanceled)
                  DebugLog("Remote Config fetch failed, using cached/default values: " + task.Exception);
              else
                  DebugLog($"Remote Config fetched. Activated new values: {task.Result}. Last fetch: {rc.Info.LastFetchStatus}");

              RemoteConfigReady = true;
              OnRemoteConfigUpdated?.Invoke();

              if (!realtimeListenerAdded)
              {
                  rc.OnConfigUpdateListener += OnRealtimeConfigUpdate;
                  realtimeListenerAdded = true;
              }
          });
    }

    // Realtime Remote Config: called when values are published in the console.
    void OnRealtimeConfigUpdate(object sender, ConfigUpdateEventArgs args)
    {
        if (args.Error != RemoteConfigError.None)
        {
            DebugLog("Remote Config realtime update error: " + args.Error);
            return;
        }
        FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(_ =>
        {
            DebugLog("Remote Config updated keys: " + string.Join(", ", args.UpdatedKeys));
            OnRemoteConfigUpdated?.Invoke();
        });
    }

    private void OnDestroy()
    {
        if (realtimeListenerAdded)
            FirebaseRemoteConfig.DefaultInstance.OnConfigUpdateListener -= OnRealtimeConfigUpdate;
    }

    bool TryGetRemote(string key, out ConfigValue value)
    {
        value = default;
        if (!RemoteConfigReady) return false;
        value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        return value.Source != ValueSource.StaticValue;
    }

    public bool GetRemoteBool(string key, bool fallback = false) => TryGetRemote(key, out var v) ? v.BooleanValue : fallback;
    public long GetRemoteLong(string key, long fallback = 0) => TryGetRemote(key, out var v) ? v.LongValue : fallback;
    public double GetRemoteDouble(string key, double fallback = 0) => TryGetRemote(key, out var v) ? v.DoubleValue : fallback;
    public string GetRemoteString(string key, string fallback = "") => TryGetRemote(key, out var v) ? v.StringValue : fallback;
    #endregion

    public void DebugLog(string s)
    {
        Debug.Log($"Fire base: {s}");
    }
}
