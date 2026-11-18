#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

/// <summary>
/// Promo-video director (editor only), created on Play when Tools > Promo > Promo Mode is on.
/// Keys while playing:
///   1 game camera · 2 close follow · 3 orbit · 4 crane up to the whole map · 5 top-down overview · 6 low hero · 7 fly over the map
///   Tab next character to film · H hide screen HUD · J hide health/name bars · B bots passive · T slow motion
///   R start/stop recording (Unity Recorder, 1920x1080 60 fps MP4 in Recordings/Promo) · F1 help
/// The cinematic shots use their own Cinemachine virtual camera (priority 100), so the game's camera is never touched.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PromoDirector : MonoBehaviour
{
    public static PromoDirector Instance;
    /// <summary>Set by the editor tools: start/stop the Unity Recorder take. Argument = file label.</summary>
    public static Action<string> ToggleRecording;
    public static Func<bool> IsRecording;

    public enum Shot { Game = 1, Follow, Orbit, Crane, Overview, Hero, FlyOver }

    [Header("Tuning")]
    public float followDistance = 5.5f, followHeight = 3.2f;
    public float orbitRadius = 5f, orbitHeight = 2.4f, orbitSpeed = 18f;
    public float craneTime = 5f;
    public Vector3 mapCenter = Vector3.zero;
    public float overviewHeight = 36f, overviewBack = 17f;
    public float cinematicFov = 38f;
    public float slowScale = 0.5f;

    public Shot shot = Shot.Game;
    public string shotLabel = "take";

    CinemachineVirtualCamera vcam;
    readonly List<TF2Player> cast = new List<TF2Player>();
    int targetIndex = -1;
    float shotStart;
    Vector3 vel;
    bool hideHud, hideWorldUi, slow, help = true;
    float baseFixedDelta;
    readonly List<Canvas> hiddenCanvases = new List<Canvas>();
    float nextHudSweep;

    void Awake()
    {
        Instance = this;
        PromoHooks.Active = true;
        baseFixedDelta = Time.fixedDeltaTime;
        var go = new GameObject("PromoVCam");
        go.transform.SetParent(transform, false);
        vcam = go.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 0;
        vcam.m_Lens.FieldOfView = cinematicFov;
        vcam.m_Lens.NearClipPlane = 0.1f;
        vcam.m_Lens.FarClipPlane = 500f;
    }

    void OnDestroy()
    {
        PromoHooks.Active = false;
        PromoHooks.BotsPassive = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDelta;
        RestoreCanvases();
        if (Instance == this) Instance = null;
    }

    Transform Target
    {
        get
        {
            if (targetIndex >= 0 && targetIndex < cast.Count && cast[targetIndex] != null) return cast[targetIndex].transform;
            var am = ArenaManager.Instance;
            return am != null && am.mainPlayer != null ? am.mainPlayer.transform : null;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetShot(Shot.Game);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetShot(Shot.Follow);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetShot(Shot.Orbit);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetShot(Shot.Crane);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetShot(Shot.Overview);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetShot(Shot.Hero);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SetShot(Shot.FlyOver);
        if (Input.GetKeyDown(KeyCode.Tab)) NextTarget();
        if (Input.GetKeyDown(KeyCode.H)) { hideHud = !hideHud; if (!hideHud) RestoreCanvases(); }
        if (Input.GetKeyDown(KeyCode.J)) { hideWorldUi = !hideWorldUi; if (!hideWorldUi) RestoreCanvases(); }
        if (Input.GetKeyDown(KeyCode.B)) PromoHooks.BotsPassive = !PromoHooks.BotsPassive;
        if (Input.GetKeyDown(KeyCode.T)) SetSlow(!slow);
        if (Input.GetKeyDown(KeyCode.F1)) help = !help;
        if (Input.GetKeyDown(KeyCode.R)) ToggleRecording?.Invoke(shotLabel);

        if ((hideHud || hideWorldUi) && Time.unscaledTime >= nextHudSweep)
        {
            nextHudSweep = Time.unscaledTime + 0.3f; // popups and bars spawned later get hidden too
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!c.enabled || !c.isRootCanvas) continue;
                bool world = c.renderMode == RenderMode.WorldSpace;
                if ((world && hideWorldUi) || (!world && hideHud)) { c.enabled = false; hiddenCanvases.Add(c); }
            }
        }
    }

    public void SetShot(Shot s)
    {
        shot = s;
        shotStart = Time.time;
        vel = Vector3.zero;
        vcam.Priority = s == Shot.Game ? 0 : 100;
        if (s != Shot.Game) PlaceNow();
    }

    void SetSlow(bool on)
    {
        slow = on;
        Time.timeScale = on ? slowScale : 1f;
        Time.fixedDeltaTime = baseFixedDelta * Time.timeScale;
    }

    void NextTarget()
    {
        cast.Clear();
        foreach (var p in FindObjectsByType<TF2Player>(FindObjectsSortMode.None))
            if (p != null && p.healthManager != null && !p.healthManager.death) cast.Add(p);
        cast.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
        if (cast.Count == 0) { targetIndex = -1; return; }
        targetIndex = (targetIndex + 1) % cast.Count;
    }

    void RestoreCanvases()
    {
        foreach (var c in hiddenCanvases)
        {
            if (c == null) continue;
            bool world = c.renderMode == RenderMode.WorldSpace;
            if ((world && !hideWorldUi) || (!world && !hideHud)) c.enabled = true;
        }
        hiddenCanvases.RemoveAll(c => c == null || c.enabled);
    }

    // Where the camera wants to be for the current shot.
    bool Desired(out Vector3 pos, out Vector3 look)
    {
        pos = look = Vector3.zero;
        var tr = Target;
        float t = Time.time - shotStart;
        Vector3 c = mapCenter;
        Vector3 back = new Vector3(0, overviewHeight, -overviewBack);
        // the blue team plays from the other side: keep their base at the bottom of the frame
        var am = ArenaManager.Instance;
        if (am != null && am.mainPlayer != null && am.mainPlayer.team == Team.blue) back = new Vector3(0, overviewHeight, overviewBack);
        switch (shot)
        {
            case Shot.Follow:
                if (tr == null) return false;
                pos = tr.position - tr.forward * followDistance + Vector3.up * followHeight;
                look = tr.position + Vector3.up * 1.2f;
                return true;
            case Shot.Orbit:
                if (tr == null) return false;
                float a = t * orbitSpeed * Mathf.Deg2Rad;
                pos = tr.position + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * orbitRadius + Vector3.up * orbitHeight;
                look = tr.position + Vector3.up * 1.0f;
                return true;
            case Shot.Crane:
            {
                if (tr == null) return false;
                float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / craneTime));
                Vector3 p0 = tr.position - tr.forward * 4f + Vector3.up * 1.8f, p1 = c + back;
                pos = Vector3.Lerp(p0, p1, k);
                look = Vector3.Lerp(tr.position + Vector3.up * 1.2f, c, k);
                return true;
            }
            case Shot.Overview:
                pos = c + back + new Vector3(Mathf.Sin(t * 0.15f) * 1.5f, 0, 0); // a slow drift so the frame is never frozen
                look = c;
                return true;
            case Shot.Hero:
                if (tr == null) return false;
                pos = tr.position + tr.forward * 3.2f + tr.right * 0.9f + Vector3.up * 0.7f;
                look = tr.position + Vector3.up * 1.4f;
                return true;
            case Shot.FlyOver:
            {
                float sgn = back.z < 0 ? 1f : -1f, z = Mathf.Lerp(-20f, 20f, Mathf.Clamp01(t / 16f)) * sgn;
                pos = c + new Vector3(Mathf.Sin(t * 0.3f) * 4f, 9f, z);
                look = c + new Vector3(0, 0, z + 12f * sgn);
                return true;
            }
        }
        return false;
    }

    void PlaceNow()
    {
        if (Desired(out var p, out var l)) vcam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(l - p));
    }

    void LateUpdate()
    {
        if (shot == Shot.Game) return;
        if (!Desired(out var pos, out var look)) return;
        float smooth = shot == Shot.Follow || shot == Shot.Hero ? 0.25f : 0.08f;
        var p = Vector3.SmoothDamp(vcam.transform.position, pos, ref vel, smooth, Mathf.Infinity, Time.unscaledDeltaTime);
        vcam.transform.SetPositionAndRotation(p, Quaternion.Slerp(vcam.transform.rotation, Quaternion.LookRotation(look - p), 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime)));
    }

    void OnGUI()
    {
        bool rec = IsRecording != null && IsRecording();
        if (rec || !help) return; // nothing of this panel may end up on the footage
        var tr = Target;
        GUI.Box(new Rect(10, 10, 380, 230),
            "PROMO MODE   (F1 hide)\n" +
            "Shot: " + shot + "   Label: " + shotLabel + "\n" +
            "Target: " + (tr != null ? tr.name : "-") + "   (Tab next)\n" +
            "1 game  2 follow  3 orbit  4 crane  5 overview  6 hero  7 fly\n" +
            "H hud " + (hideHud ? "OFF" : "on") + "   J bars " + (hideWorldUi ? "OFF" : "on") + "\n" +
            "B bots " + (PromoHooks.BotsPassive ? "PASSIVE" : "normal") + "   T slow " + (slow ? "x" + slowScale : "off") + "\n" +
            "R record (panel hides while recording)\n" +
            "Ads are off while Promo Mode runs.");
    }
}
#endif
