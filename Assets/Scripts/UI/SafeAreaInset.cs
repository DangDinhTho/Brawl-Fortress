using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps UI out of the notch / rounded corners (Screen.safeArea) on every screen-space canvas, popups included.
/// Added automatically to each root screen-space canvas when a scene loads (the GUIManager popup canvas too).
/// Only decoration reaches into the notch (full-screen backgrounds, full-width bars); everything the player reads or
/// uses (text, titles, icons, buttons, panels, scroll views) is moved inside the safe area. A bar keeps its full width
/// and only its content moves. Moved things: left-anchored shift right, right-anchored shift left, stretched ones
/// shrink, centred ones shift by half the difference. New children (popups opened later) are picked up within 0.25 s.
/// Put SafeAreaIgnore on an object to leave it (and its children) alone.
/// </summary>
[DisallowMultipleComponent]
public class SafeAreaInset : MonoBehaviour
{
    struct Original { public Vector2 min, max; }

    readonly Dictionary<RectTransform, Original> originals = new Dictionary<RectTransform, Original>();
    RectTransform root;
    Rect lastSafeArea;
    Vector2Int lastScreen;
    float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) => AttachToCanvases();
        AttachToCanvases();
    }

    static void AttachToCanvases()
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode != RenderMode.WorldSpace && c.GetComponent<SafeAreaInset>() == null && c.GetComponent<SafeAreaIgnore>() == null)
                c.gameObject.AddComponent<SafeAreaInset>();
    }

    void Awake() { root = (RectTransform)transform; }

    void LateUpdate()
    {
        bool changed = Screen.safeArea != lastSafeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height;
        if (!changed && Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.25f;
        if (changed) { lastSafeArea = Screen.safeArea; lastScreen = new Vector2Int(Screen.width, Screen.height); }
        Apply(changed);
    }

    void Apply(bool reapplyAll)
    {
        if (Screen.width <= 0 || Screen.height <= 0 || root == null) return;
        Vector2 size = root.rect.size;
        Rect s = Screen.safeArea;
        float left = s.xMin / Screen.width * size.x;
        float right = (Screen.width - s.xMax) / Screen.width * size.x;
        float bottom = s.yMin / Screen.height * size.y;
        float top = (Screen.height - s.yMax) / Screen.height * size.y;
        Visit(root, left, right, bottom, top, reapplyAll, 0);
    }

    enum Kind { Decor, Container, Shift }
    readonly Dictionary<RectTransform, Kind> kinds = new Dictionary<RectTransform, Kind>();
    static readonly List<MonoBehaviour> scratch = new List<MonoBehaviour>();

    /// <summary>Something the player can touch: buttons, toggles, sliders, inputs, scroll views, joysticks, EventTriggers...</summary>
    static bool IsInteractive(Component c)
    {
        scratch.Clear();
        c.GetComponents(scratch);
        for (int i = 0; i < scratch.Count; i++)
            if (scratch[i] is UnityEngine.EventSystems.IEventSystemHandler) return true;
        return false;
    }

    /// <summary>Only decoration may reach into the notch: full-screen backgrounds and full-width bars / stripes. A
    /// full-screen object or a full-width bar that holds something keeps its own size and its content is checked one
    /// by one. Everything the player reads or uses (text and titles, icons, buttons, panels, scroll views) is moved
    /// inside the safe area.</summary>
    Kind Classify(RectTransform c)
    {
        if (IsFullScreen(c)) return c.childCount > 0 ? Kind.Container : Kind.Decor;
        bool wide = c.anchorMin.x <= 0.01f && c.anchorMax.x >= 0.99f;
        if (wide && !IsInteractive(c) && !IsText(c))
            return c.childCount > 0 ? Kind.Container : Kind.Decor;   // bar: stays full width, its content moves
        return Kind.Shift;
    }

    static bool IsFullScreen(RectTransform c) =>
        c.anchorMin.x <= 0.01f && c.anchorMin.y <= 0.01f && c.anchorMax.x >= 0.99f && c.anchorMax.y >= 0.99f && c.offsetMin.sqrMagnitude < 4f && c.offsetMax.sqrMagnitude < 4f;

    static bool IsText(Component c) =>
        c.GetComponent<UnityEngine.UI.Text>() != null || c.GetComponent<TMPro.TMP_Text>() != null;

    void Visit(RectTransform parent, float left, float right, float bottom, float top, bool reapplyAll, int depth)
    {
        foreach (Transform t in parent)
        {
            var c = t as RectTransform;
            if (c == null || c.GetComponent<SafeAreaIgnore>() != null) continue;
            if (!kinds.TryGetValue(c, out var kind)) { kind = Classify(c); kinds[c] = kind; }
            if (kind == Kind.Decor) continue;
            if (kind == Kind.Container)
            {
                if (depth < 5) Visit(c, left, right, bottom, top, reapplyAll, depth + 1);
                continue;
            }
            bool known = originals.TryGetValue(c, out var o);
            if (!known) { o = new Original { min = c.offsetMin, max = c.offsetMax }; originals[c] = o; }
            else if (!reapplyAll) continue;

            Shift(c.anchorMin.x, c.anchorMax.x, left, right, out float xMin, out float xMax);
            Shift(c.anchorMin.y, c.anchorMax.y, bottom, top, out float yMin, out float yMax);
            c.offsetMin = o.min + new Vector2(xMin, yMin);
            c.offsetMax = o.max + new Vector2(xMax, yMax);
        }
    }

    static void Shift(float anchorMin, float anchorMax, float lowInset, float highInset, out float dMin, out float dMax)
    {
        if (anchorMin <= 0.01f && anchorMax >= 0.99f) { dMin = lowInset; dMax = -highInset; }   // stretched: shrink
        else if (anchorMax <= 0.01f) { dMin = dMax = lowInset; }                                  // anchored low side
        else if (anchorMin >= 0.99f) { dMin = dMax = -highInset; }                                // anchored high side
        else { dMin = dMax = (lowInset - highInset) * 0.5f; }                                     // centred
    }
}
