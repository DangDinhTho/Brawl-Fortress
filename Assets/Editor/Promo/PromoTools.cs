using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>
/// Promo-video tools (editor only). Tools > Promo > Promo Mode adds a PromoDirector on Play (cinematic cameras, hide HUD,
/// passive bots, slow motion, no ads) and records takes with Unity Recorder: 1920x1080, 60 fps, MP4 (H.264, high quality,
/// game audio) into <project>/Recordings/Promo/<shot label>_<time>.mp4. Tools > Promo > Shot List follows the storyboard.
/// </summary>
[InitializeOnLoad]
public static class PromoTools
{
    const string PrefEnabled = "Promo.Enabled";
    const string MenuMode = "Tools/Promo/Promo Mode";
    static RecorderController controller;
    static string lastFile;

    public static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefEnabled, false);
        set => EditorPrefs.SetBool(PrefEnabled, value);
    }

    public static string OutputDir => Path.Combine(Directory.GetCurrentDirectory(), "Recordings", "Promo");

    static PromoTools()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem(MenuMode, false, 1)]
    static void ToggleMode()
    {
        Enabled = !Enabled;
        Debug.Log("[Promo] Promo Mode " + (Enabled ? "ON: press Play, F1 shows the keys" : "OFF"));
    }

    [MenuItem(MenuMode, true)]
    static bool ToggleModeValidate()
    {
        Menu.SetChecked(MenuMode, Enabled);
        return true;
    }

    [MenuItem("Tools/Promo/Shot List", false, 2)]
    static void OpenShots() => PromoShotListWindow.Open();

    [MenuItem("Tools/Promo/Open Recordings Folder", false, 3)]
    static void OpenFolder()
    {
        Directory.CreateDirectory(OutputDir);
        EditorUtility.RevealInFinder(OutputDir + Path.DirectorySeparatorChar);
    }

    static void OnPlayModeChanged(PlayModeStateChange s)
    {
        if (s == PlayModeStateChange.EnteredPlayMode && Enabled)
        {
            var go = new GameObject("PromoDirector");
            UnityEngine.Object.DontDestroyOnLoad(go);
            var d = go.AddComponent<PromoDirector>();
            d.shotLabel = PromoShotListWindow.CurrentLabel;
            PromoDirector.ToggleRecording = ToggleRecording;
            PromoDirector.IsRecording = IsRecording;
            Debug.Log("[Promo] Director ready. F1 = keys, R = record. Current shot: " + d.shotLabel);
        }
        else if (s == PlayModeStateChange.ExitingPlayMode)
        {
            StopRecording();
        }
    }

    public static bool IsRecording() => controller != null && controller.IsRecording();

    public static void ToggleRecording(string label)
    {
        if (IsRecording()) StopRecording();
        else StartRecording(label);
    }

    static string Clean(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return string.IsNullOrWhiteSpace(s) ? "take" : s.Replace(' ', '_');
    }

    public static void StartRecording(string label)
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("[Promo] Recording only works in Play mode."); return; }
        Directory.CreateDirectory(OutputDir);

        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Promo Movie";
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
        };
        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
        movie.AudioInputSettings.PreserveAudio = true;
        lastFile = Path.Combine(OutputDir, Clean(label) + "_" + DateTime.Now.ToString("HHmmss"));
        movie.OutputFile = lastFile;

        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        settings.AddRecorderSettings(movie);
        settings.SetRecordModeToManual();
        settings.FrameRate = 60f;
        settings.FrameRatePlayback = FrameRatePlayback.Constant;
        settings.CapFrameRate = true;

        controller = new RecorderController(settings);
        controller.PrepareRecording();
        controller.StartRecording();
        Debug.Log("[Promo] ● REC " + lastFile + ".mp4");
    }

    public static void StopRecording()
    {
        if (!IsRecording()) return;
        controller.StopRecording();
        Debug.Log("[Promo] ■ Saved " + lastFile + ".mp4");
    }
}

/// <summary>The storyboard's shots as a checklist; the selected shot names the next recorded file.</summary>
public class PromoShotListWindow : EditorWindow
{
    struct ShotInfo { public string label, title, how; }

    static readonly ShotInfo[] Shots =
    {
        new ShotInfo { label = "01_ambush", title = "Ph\u1ee5c k\u00edch t\u1eeb b\u1ee5i (0:00\u20130:03)", how = "Map_Brawl_BambooTemple, Competitive. B bots passive, n\u1ea5p v\u00e0o b\u1ee5i, ch\u1edd bot \u0111i ngang (Tab \u0111\u1ec3 theo bot). T\u1eaft B r\u1ed3i lao ra. Camera 6 (hero) ho\u1eb7c 2 (follow). H + J t\u1eaft HUD." },
        new ShotInfo { label = "02_reveal", title = "K\u00e9o l\u00ean to\u00e0n map (0:03\u20130:05)", how = "C\u00f9ng map. Camera 4 (crane) t\u1eeb nh\u00e2n v\u1eadt l\u00ean to\u00e0n c\u1ea3nh. H + J t\u1eaft HUD. Ch\u1eeb tr\u1ed1ng gi\u1eefa khung cho logo." },
        new ShotInfo { label = "03_aim", title = "Ng\u1eafm v\u00e0 b\u1eafn (0:05\u20130:09)", how = "Camera 1 (game), GI\u1eee HUD \u0111\u1ec3 th\u1ea5y qu\u1ea1t ng\u1eafm. Soldier b\u1eafn ch\u1ea1m (Tap), r\u1ed3i Heavy x\u1ea3 gi\u1eef (Hold)." },
        new ShotInfo { label = "04_classes", title = "L\u1edbp nh\u00e2n v\u1eadt (0:09\u20130:14)", how = "M\u1ed7i l\u1edbp 1\u20132 gi\u00e2y: Pyro phun l\u1eeda, Medic tia h\u1ed3i m\u00e1u, Engineer \u0111\u1eb7t sentry, Sniper, Spy t\u00e0ng h\u00ecnh. Camera 6 ho\u1eb7c 3 (orbit), T slow-mo tu\u1ef3 ch\u1ecdn." },
        new ShotInfo { label = "05_modes", title = "Ch\u1ebf \u0111\u1ed9 ch\u01a1i (0:14\u20130:19)", how = "Map_Brawl_CTF c\u01b0\u1edbp vali, Map_Brawl_Koth chi\u1ebfm \u0111i\u1ec3m, Map_Brawl_Payload \u0111\u1ea9y xe, Map_Brawl_MvM robot. Camera 2 ho\u1eb7c 5 (overview)." },
        new ShotInfo { label = "06_maps", title = "B\u1ed1n ch\u1ee7 \u0111\u1ec1 map (0:19\u20130:22)", how = "Camera 7 (fly over) tr\u00ean DustyCanyon (Western), BambooTemple, Cavern (Cave), MoonMaze (Moon). H + J t\u1eaft HUD, B bots passive." },
        new ShotInfo { label = "07_progress", title = "Th\u1eafng, l\u00ean c\u1ea5p, m\u1edf kho\u00e1 (0:22\u20130:25)", how = "Camera 1, gi\u1eef UI: m\u00e0n th\u1eafng tr\u1eadn, MedalScene, popup m\u1edf kho\u00e1 ch\u1ebf \u0111\u1ed9 \u1edf Home." },
        new ShotInfo { label = "08_endcard", title = "N\u1ec1n end card (0:25\u20130:30)", how = "Camera 5 (overview) tr\u00ean map \u0111\u1eb9p nh\u1ea5t, H + J t\u1eaft HUD, quay 8\u201310 gi\u00e2y \u0111\u1ec3 l\u00e0m n\u1ec1n m\u1edd sau logo." },
    };

    const string PrefCurrent = "Promo.CurrentShot";
    Vector2 scroll;

    public static string CurrentLabel => Shots[Mathf.Clamp(EditorPrefs.GetInt(PrefCurrent, 0), 0, Shots.Length - 1)].label;

    public static void Open()
    {
        var w = GetWindow<PromoShotListWindow>("Promo Shots");
        w.minSize = new Vector2(420, 420);
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Ch\u1ecdn c\u1ea3nh \u0111ang quay (t\u00ean file). B\u1eadt Tools > Promo > Promo Mode, b\u1ea5m Play, F1 xem ph\u00edm, R b\u1eaft \u0111\u1ea7u / d\u1eebng ghi.", MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            bool on = PromoTools.Enabled;
            bool now = GUILayout.Toggle(on, " Promo Mode", "Button");
            if (now != on) PromoTools.Enabled = now;
            if (GUILayout.Button("Open Recordings Folder"))
            {
                Directory.CreateDirectory(PromoTools.OutputDir);
                EditorUtility.RevealInFinder(PromoTools.OutputDir + Path.DirectorySeparatorChar);
            }
        }
        if (EditorApplication.isPlaying)
            EditorGUILayout.LabelField(PromoTools.IsRecording() ? "\u25cf \u0110ANG GHI" : "Kh\u00f4ng ghi", EditorStyles.boldLabel);

        int cur = EditorPrefs.GetInt(PrefCurrent, 0);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < Shots.Length; i++)
        {
            var s = Shots[i];
            using (new EditorGUILayout.VerticalScope(i == cur ? "SelectionRect" : "box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string doneKey = "Promo.Done." + s.label;
                    bool done = EditorPrefs.GetBool(doneKey, false), nd = EditorGUILayout.Toggle(done, GUILayout.Width(18));
                    if (nd != done) EditorPrefs.SetBool(doneKey, nd);
                    if (GUILayout.Button(s.label + "  " + s.title, i == cur ? EditorStyles.boldLabel : EditorStyles.label))
                    {
                        EditorPrefs.SetInt(PrefCurrent, i);
                        if (PromoDirector.Instance != null) PromoDirector.Instance.shotLabel = s.label;
                    }
                }
                EditorGUILayout.LabelField(s.how, EditorStyles.wordWrappedMiniLabel);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void OnInspectorUpdate() => Repaint();
}
