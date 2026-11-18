using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps UI out of the notch / rounded corners (Screen.safeArea) on every screen-space canvas, popups included.
/// Added automatically to each root screen-space canvas when a scene loads (the GUIManager popup canvas too).
/// Full-screen stretch objects (backgrounds, dimmers, containers) keep covering the whole screen; their content is
/// moved inside the safe area instead: left-anchored things shift right, right-anchored shift left, stretched things
/// shrink, centred things shift by half the difference. New children (popups opened later) are picked up within 0.25 s.
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

    static bool IsFullScreen(RectTransform c) =>
        c.anchorMin == Vector2.zero && c.anchorMax == Vector2.one && c.offsetMin.sqrMagnitude < 1f && c.offsetMax.sqrMagnitude < 1f;

    void Visit(RectTransform parent, float left, float right, float bottom, float top, bool reapplyAll, int depth)
    {
        foreach (Transform t in parent)
        {
            var c = t as RectTransform;
            if (c == null || c.GetComponent<SafeAreaIgnore>() != null) continue;
            bool known = originals.TryGetValue(c, out var o);
            if (!known)
            {
                if (IsFullScreen(c))
                {
                    // background (no children) stays full screen; a container: fit what it holds
                    if (c.childCount > 0 && depth < 3) Visit(c, left, right, bottom, top, reapplyAll, depth + 1);
                    continue;
                }
                o = new Original { min = c.offsetMin, max = c.offsetMax };
                originals[c] = o;
            }
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
