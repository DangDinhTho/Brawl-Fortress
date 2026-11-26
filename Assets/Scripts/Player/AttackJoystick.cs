using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Brawl Stars style attack stick (the right-hand button in a match, UICharacterInputController.aimingJoystick).
///  - Tap (press and release without dragging): auto-aim attack at the nearest enemy in range (AutoAim).
///  - Drag: manual aim (the aim indicator shows), release: attack that way.
///  - Drag back to the centre and release: the attack is cancelled (the indicator hides there).
/// The drag is measured from where the finger went down, not from the button's centre: a tap anywhere on the button is
/// a tap (with the old fixed stick a touch near the rim already counted as aiming toward the rim).
/// Hold weapons (minigun, flamethrower, melee...) read IsPressed / Aimed / Direction every frame instead (PlayerFPSController).
/// </summary>
public class AttackJoystick : Joystick
{
    public enum Release { None, Tap, Aim, Cancel }

    [Tooltip("Drag from the touch point, as a share of the stick radius, after which the press becomes manual aiming")]
    public float aimStart = 0.3f;
    [Tooltip("Released closer to the centre than this (share of the radius) after aiming = cancelled. Same value as " +
             "PlayerFPSController.AimDeadZone, below which the aim indicator hides, so what the player sees is what happens")]
    public float cancelRadius = 0.2f;
    [Tooltip("Full aim (direction length 1) after dragging this many times the stick radius")]
    public float dragRange = 1f;
    [Tooltip("A release older than this (s) and a few frames is dropped: e.g. made while the player was dead")]
    public float releaseLifetime = 0.3f;

    /// <summary>A finger is on the stick.</summary>
    public bool IsPressed { get; private set; }
    /// <summary>This press was dragged past aimStart: manual aiming (stays true until release, even back in the centre).</summary>
    public bool Aimed { get; private set; }
    public float PressTime { get; private set; }

    Vector2 pressPos;
    Release lastRelease;
    Vector2 lastReleaseDir;
    float lastReleaseTime;
    int lastReleaseFrame;

    public override void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
        Aimed = false;
        PressTime = Time.time;
        pressPos = eventData.position;
        input = Vector2.zero;
        if (handle) handle.anchoredPosition = Vector2.zero;
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (!IsPressed || canvas == null || background == null) return;
        float radius = Mathf.Max(1f, background.sizeDelta.x * 0.5f * canvas.scaleFactor * Mathf.Max(0.1f, dragRange));
        Vector2 v = (eventData.position - pressPos) / radius;
        if (v.magnitude > 1f) v = v.normalized;
        if (!Aimed && v.magnitude >= aimStart) Aimed = true;
        input = Aimed ? v : Vector2.zero;   // no direction (no indicator) before the press turns into aiming
        if (handle) handle.anchoredPosition = input * background.sizeDelta.x * 0.5f * HandleRange;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!IsPressed) return;
        Vector2 dir = input;
        bool aimed = Aimed;
        IsPressed = false;
        Aimed = false;
        base.OnPointerUp(eventData);   // input and handle back to zero
        lastRelease = !aimed ? Release.Tap : dir.magnitude >= cancelRadius ? Release.Aim : Release.Cancel;
        lastReleaseDir = dir;
        lastReleaseTime = Time.unscaledTime;
        lastReleaseFrame = Time.frameCount;
    }

    /// <summary>The last release (tap / aimed / cancelled) and the stick direction at that moment, once: it is cleared.</summary>
    public Release TakeRelease(out Vector2 dir)
    {
        dir = lastReleaseDir;
        var r = lastRelease;
        lastRelease = Release.None;
        // stale = old in time AND in frames: one long frame (a hitch) must not eat the tap
        if (r != Release.None && Time.unscaledTime - lastReleaseTime > releaseLifetime && Time.frameCount - lastReleaseFrame > 3)
            r = Release.None;
        return r;
    }

    void OnDisable()
    {
        // the stick was hidden mid-press (death, end of match): forget the press, no attack
        IsPressed = false;
        Aimed = false;
        input = Vector2.zero;
        lastRelease = Release.None;
        if (handle) handle.anchoredPosition = Vector2.zero;
    }
}
