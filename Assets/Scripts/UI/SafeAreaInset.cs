using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Keeps UI out of the notch (Screen.safeArea, left / right sides in landscape) on every screen-space canvas, popups
/// included. Added automatically to every root screen-space canvas (scene load + a scan every second for canvases created
/// at runtime). Popups are fitted in the frame they open (LateUpdate, before rendering).
///
/// Rules:
///  - full-screen objects (backgrounds, popup roots, containers) and big panels stretched both ways keep their size;
///    their content is checked one by one; full-screen pictures reach OverscanSize past the edges (no light seams);
///  - a full-width bar / panel that holds something moves into the safe area AS A WHOLE, so its buttons keep their place
///    on the bar art; its picture and its full-width decorations grow back out to the screen edges it really touches;
///  - anything the player reads, taps or scrolls (texts, icons, buttons, scroll lists...) moves inside the safe area;
///    a full-width scroll list with a background keeps its background stretched to the edges;
///  - children of a LayoutGroup are left to the layout; UI placed at world / touch positions is never moved.
/// It waits until the canvas is laid out (first frame after load), re-applies when the canvas size changes, and when
/// another script moves a fitted object it takes the new position as the design position.
/// Put SafeAreaIgnore on an object to leave it (and its children) alone.
/// </summary>
[DisallowMultipleComponent]
public class SafeAreaInset : MonoBehaviour
{
    struct Original { public Vector2 min, max; public Vector2 pos, dMin, dMax; }
    enum Kind { Decor, Container, Bar, Shift, Skip }

    /// <summary>Editor only: pretend the screen has a notch (iPhone 14 landscape: 5.6% left / right, 5.4% bottom) to check
    /// layouts without a device. Menu Tools/UI/Simulate Notch (Editor).</summary>
    public static bool SimulateNotch;
    public const string SimulatePref = "SafeAreaInset.SimulateNotch";

    /// <summary>Also keep UI out of the top / bottom insets (home indicator). Off: in landscape only the notch sides are
    /// applied, so rows of buttons keep their place relative to the panels above them.</summary>
    public static bool RespectVerticalInsets = false;

    /// <summary>How far full-screen pictures reach past the screen edges (canvas units): hides light seams.</summary>
    public const float OverscanSize = 6f;

    /// <summary>UI placed by code at world / touch positions (damage numbers, Engineer pie menu) must never be moved.</summary>
    static readonly string[] WorldFollowers = { "UltimateTextDamageManager", "UITextDamage", "PieMenuManager", "PieMenu" };

    readonly Dictionary<RectTransform, Original> originals = new Dictionary<RectTransform, Original>();
    readonly Dictionary<RectTransform, Original> applied = new Dictionary<RectTransform, Original>();
    readonly Dictionary<RectTransform, Kind> kinds = new Dictionary<RectTransform, Kind>();
    readonly Dictionary<RectTransform, List<RectTransform>> bleeds = new Dictionary<RectTransform, List<RectTransform>>();
    static readonly List<MonoBehaviour> scratch = new List<MonoBehaviour>();
    static readonly List<RectTransform> dead = new List<RectTransform>();

    RectTransform root;
    Rect lastSafeArea;
    Vector2Int lastScreen;
    Vector2 lastRootSize;
    int framesAlive;
    float nextPrune;
    float left, right, bottom, top;
    static float nextCanvasScan, scanUntil;
    int lastHierarchyCount = -1;
    bool dirty;
    static readonly List<SafeAreaInset> instances = new List<SafeAreaInset>();

    /// <summary>For scripts that move UI after it was created (custom HUD layouts...): fit again at the end of this frame.</summary>
    public static void RequestFit() { for (int i = 0; i < instances.Count; i++) instances[i].dirty = true; }

    static Rect SafeArea
    {
        get
        {
#if UNITY_EDITOR
            if (SimulateNotch) return new Rect(Screen.width * 0.056f, Screen.height * 0.054f, Screen.width * (1f - 0.112f), Screen.height * (1f - 0.054f));
#endif
            return Screen.safeArea;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
#if UNITY_EDITOR
        SimulateNotch = UnityEditor.EditorPrefs.GetBool(SimulatePref, false);
#endif
        SceneManager.sceneLoaded += (s, m) => { AttachToCanvases(); scanUntil = Time.unscaledTime + 3f; };
        AttachToCanvases();
        scanUntil = Time.unscaledTime + 3f;
    }

    static void AttachToCanvases()
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode != RenderMode.WorldSpace && c.GetComponent<SafeAreaInset>() == null && c.GetComponent<SafeAreaIgnore>() == null && !FollowsWorld(c))
                c.gameObject.AddComponent<SafeAreaInset>();
    }

    static bool FollowsWorld(Component c)
    {
        scratch.Clear();
        c.GetComponents(scratch);
        for (int i = 0; i < scratch.Count; i++)
            if (scratch[i] != null && System.Array.IndexOf(WorldFollowers, scratch[i].GetType().Name) >= 0) return true;
        return false;
    }

    void Awake() { root = (RectTransform)transform; }
    void OnEnable() { framesAlive = 0; lastHierarchyCount = -1; if (!instances.Contains(this)) instances.Add(this); }
    void OnDisable() { instances.Remove(this); }

    /// <summary>Event driven (device screens do not change): fits the canvas once after it is created, then again only
    /// when something new appears under it (popup, HUD, list items: the hierarchy count changes; O(1) to read), when a
    /// script asks (RequestFit) or when the safe area / resolution changes. A fit pass only touches new or changed
    /// elements; the rest is cached. Popups are fitted in the frame they open, before they are drawn.</summary>
    void LateUpdate()
    {
        // canvases created a little after a scene load (one instance does the scan, only for 3 s after the load)
        if (Time.unscaledTime < scanUntil && Time.unscaledTime >= nextCanvasScan) { nextCanvasScan = Time.unscaledTime + 0.5f; AttachToCanvases(); }

        // wait for the canvas to be laid out and for the scene's Start() code to run (first frame after load)
        if (framesAlive < 1) { framesAlive++; return; }
        Vector2 size = root.rect.size;
        if (size.x < 2f || size.y < 2f || Screen.width <= 0 || Screen.height <= 0) return;

        Rect s = SafeArea;
        bool changed = s != lastSafeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height || (size - lastRootSize).sqrMagnitude > 0.25f;
        int count = transform.hierarchyCount;
        if (!changed && !dirty && count == lastHierarchyCount) return;   // nothing new: no work this frame
        lastHierarchyCount = count; dirty = false;
        if (changed)
        {
            lastSafeArea = s; lastScreen = new Vector2Int(Screen.width, Screen.height); lastRootSize = size;
            left = s.xMin / Screen.width * size.x;
            right = (Screen.width - s.xMax) / Screen.width * size.x;
            // landscape game: only the notch sides. Moving bottom buttons up made them overlap the panels above them.
            bottom = RespectVerticalInsets ? s.yMin / Screen.height * size.y : 0f;
            top = RespectVerticalInsets ? (Screen.height - s.yMax) / Screen.height * size.y : 0f;
        }
        if (Time.unscaledTime >= nextPrune) { nextPrune = Time.unscaledTime + 5f; Prune(); }
        Visit(root, changed, 0);
    }

    void Prune()
    {
        dead.Clear();
        foreach (var k in kinds.Keys) if (k == null) dead.Add(k);
        foreach (var k in originals.Keys) if (k == null && !dead.Contains(k)) dead.Add(k);
        foreach (var k in dead) { kinds.Remove(k); originals.Remove(k); applied.Remove(k); bleeds.Remove(k); }
    }

    #region classification
    static bool IsFullScreen(RectTransform c) =>
        c.anchorMin.x <= 0.01f && c.anchorMin.y <= 0.01f && c.anchorMax.x >= 0.99f && c.anchorMax.y >= 0.99f &&
        // a few units in or out still counts (MedalScene's Bgr is drawn 4 units larger than the screen)
        Mathf.Abs(c.offsetMin.x) <= 12f && Mathf.Abs(c.offsetMin.y) <= 12f && Mathf.Abs(c.offsetMax.x) <= 12f && Mathf.Abs(c.offsetMax.y) <= 12f;

    static bool IsWide(RectTransform c) => c.anchorMin.x <= 0.01f && c.anchorMax.x >= 0.99f;
    static bool IsTall(RectTransform c) => c.anchorMin.y <= 0.01f && c.anchorMax.y >= 0.99f;
    static bool IsText(Component c) => c.GetComponent<Text>() != null || c.GetComponent<TMPro.TMP_Text>() != null;

    static bool HasOwnPicture(RectTransform c)
    {
        var g = c.GetComponent<Graphic>();
        return g != null && g.enabled && !(g is Text) && !(g is TMPro.TMP_Text) && g.color.a > 0.01f;
    }

    /// <summary>Something the player can touch: buttons, toggles, sliders, inputs, scroll views, joysticks, EventTriggers...</summary>
    static bool IsInteractive(Component c)
    {
        scratch.Clear();
        c.GetComponents(scratch);
        for (int i = 0; i < scratch.Count; i++)
            if (scratch[i] is UnityEngine.EventSystems.IEventSystemHandler) return true;
        return false;
    }

    static Kind Classify(RectTransform c)
    {
        if (FollowsWorld(c)) return Kind.Decor;
        // the parent's layout places it: moving it would fight the layout every frame
        var parent = c.parent;
        if (parent != null)
        {
            var lg = parent.GetComponent<LayoutGroup>();
            if (lg != null && lg.enabled) { var le = c.GetComponent<LayoutElement>(); if (le == null || !le.ignoreLayout) return Kind.Decor; }
        }
        if (IsFullScreen(c))
        {
            // a full-screen scroll list goes inside the safe area; a full-screen tap-to-close background stays full
            if (c.GetComponent<ScrollRect>() != null) return Kind.Shift;
            return c.childCount > 0 ? Kind.Container : Kind.Decor;
        }
        if (IsInteractive(c) || IsText(c)) return Kind.Shift;
        // big panels / backgrounds stretched both ways keep their size; only what they hold is checked
        if (IsWide(c) && IsTall(c)) return c.childCount > 0 ? Kind.Container : Kind.Decor;
        // full-width bars move as a whole, their picture grows to the edges
        if (IsWide(c)) return c.childCount > 0 ? Kind.Bar : Kind.Decor;
        return Kind.Shift;
    }
    #endregion

    void Visit(RectTransform parent, bool reapplyAll, int depth)
    {
        for (int ci = 0; ci < parent.childCount; ci++)   // for, not foreach: a Transform enumerator allocates
        {
            var c = parent.GetChild(ci) as RectTransform;
            if (c == null) continue;
            if (!kinds.TryGetValue(c, out var kind))
            {
                // ignored / bleed copies: decided once and cached (no GetComponent on later passes)
                if (c.GetComponent<SafeAreaIgnore>() != null || c.GetComponent<SafeAreaBleed>() != null) { kinds[c] = Kind.Skip; continue; }
                kind = Classify(c); kinds[c] = kind; Overscan(c, kind);
            }
            if (kind == Kind.Skip) continue;
            // a container that was empty when first seen (PopupContainer before any popup opened) holds things now
            else if (kind == Kind.Decor && c.childCount > 0 && IsFullScreen(c)) { kind = Classify(c); kinds[c] = kind; }
            if (kind == Kind.Decor) continue;
            if (kind == Kind.Container)
            {
                if (depth < 6) Visit(c, reapplyAll, depth + 1);
                continue;
            }

            bool known = originals.TryGetValue(c, out var o);
            bool force = reapplyAll;
            if (!known) { o = new Original { min = c.offsetMin, max = c.offsetMax }; originals[c] = o; force = true; }
            else if (applied.TryGetValue(c, out var a) && ((c.offsetMin - a.min).sqrMagnitude > 0.25f || (c.offsetMax - a.max).sqrMagnitude > 0.25f))
            {
                if ((c.anchoredPosition - a.pos).sqrMagnitude <= 0.25f)
                    // only resized in place (a HUD panel growing / shrinking): keep the design position, take the new size.
                    // Taking the shifted offsets as the design here added the inset again on every frame of a resize
                    // tween, so the panel drifted toward the middle of the screen.
                    o = new Original { min = c.offsetMin - a.dMin, max = c.offsetMax - a.dMax };
                else
                    // another script moved it (saved HUD layout, late layout code): that is the new design position
                    o = new Original { min = c.offsetMin, max = c.offsetMax };
                originals[c] = o; force = true;
            }
            if (!force) continue;

            Shift(c.anchorMin.x, c.anchorMax.x, left, right, out float xMin, out float xMax);
            Shift(c.anchorMin.y, c.anchorMax.y, bottom, top, out float yMin, out float yMax);
            var dMin = new Vector2(xMin, yMin);
            var dMax = new Vector2(xMax, yMax);
            c.offsetMin = o.min + dMin;
            c.offsetMax = o.max + dMax;
            applied[c] = new Original { min = c.offsetMin, max = c.offsetMax, pos = c.anchoredPosition, dMin = dMin, dMax = dMax };

            // bars, and any full-width thing with its own picture (a scroll list with a background...): the content
            // stays inside the safe area, the picture grows back out to the screen edges
            if (kind == Kind.Bar || (IsWide(c) && HasOwnPicture(c))) Bleed(c, o, dMin, dMax);
        }
    }

    /// <summary>A full-screen background picture reaches a little past the screen edges. A full-screen container with
    /// its own picture gets an overscanned copy behind its content (its children keep their layout).</summary>
    void Overscan(RectTransform c, Kind kind)
    {
        if (!IsFullScreen(c) || kind == Kind.Shift || c.GetComponent<SafeAreaIgnore>() != null) return;
        var g = c.GetComponent<Graphic>();
        if (g == null || !g.enabled || g is Text || g is TMPro.TMP_Text) return;
        var grow = new Vector2(OverscanSize, OverscanSize);
        if (kind == Kind.Decor) { c.offsetMin = -grow; c.offsetMax = grow; return; }
        var copy = MakeBleedCopy(c);
        if (copy != null) { copy.offsetMin = -grow; copy.offsetMax = grow; }
    }

    /// <summary>The bar moved by (dMin, dMax): its picture and its full-width decorations move with it (buttons stay on
    /// their spot of the art) and only grow back out towards the screen edges the bar really touches.</summary>
    void Bleed(RectTransform bar, Original barOriginal, Vector2 dMin, Vector2 dMax)
    {
        if (!bleeds.TryGetValue(bar, out var list))
        {
            list = new List<RectTransform>();
            var copy = MakeBleedCopy(bar);
            if (copy != null) list.Add(copy);
            foreach (Transform t in bar)
            {
                var d = t as RectTransform;
                if (d == null || d == copy || d.childCount > 0 || !IsWide(d) || IsText(d) || IsInteractive(d) || d.GetComponent<SafeAreaIgnore>() != null) continue;
                if (d.GetComponent<Graphic>() == null) continue;
                list.Add(d);
                originals[d] = new Original { min = d.offsetMin, max = d.offsetMax };
                kinds[d] = Kind.Decor; // handled here, not by Visit
            }
            bleeds[bar] = list;
        }
        var ob = barOriginal;
        // touches = anchored on that side and reaching the screen edge (or past it, like a bar drawn 2 units wider)
        bool tL = bar.anchorMin.x <= 0.01f && ob.min.x < 2f, tB = bar.anchorMin.y <= 0.01f && ob.min.y < 2f;
        bool tR = bar.anchorMax.x >= 0.99f && ob.max.x > -2f, tT = bar.anchorMax.y >= 0.99f && ob.max.y > -2f;
        Vector2 grow0 = new Vector2(tL ? dMin.x + OverscanSize : 0f, tB ? dMin.y + OverscanSize : 0f);
        Vector2 grow1 = new Vector2(tR ? dMax.x - OverscanSize : 0f, tT ? dMax.y - OverscanSize : 0f);
        foreach (var r in list)
        {
            if (r == null) continue;
            var o = originals.TryGetValue(r, out var ro) ? ro : new Original();
            r.offsetMin = o.min - grow0;
            r.offsetMax = o.max - grow1;
        }
    }

    /// <summary>A copy of the object's own Image / RawImage, behind its content, covering its original rect.</summary>
    RectTransform MakeBleedCopy(RectTransform bar)
    {
        var img = bar.GetComponent<Image>();
        var raw = img == null ? bar.GetComponent<RawImage>() : null;
        if ((img == null || !img.enabled) && (raw == null || !raw.enabled)) return null;
        var go = new GameObject(bar.name + "_SafeAreaBleed", typeof(RectTransform));
        go.layer = bar.gameObject.layer;
        var r = (RectTransform)go.transform;
        r.SetParent(bar, false);
        r.SetSiblingIndex(0);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.pivot = bar.pivot;
        r.localScale = Vector3.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        originals[r] = new Original { min = Vector2.zero, max = Vector2.zero };
        if (img != null)
        {
            var b = go.AddComponent<Image>();
            b.sprite = img.sprite; b.type = img.type; b.color = img.color; b.material = img.material;
            b.preserveAspect = img.preserveAspect; b.fillCenter = img.fillCenter; b.pixelsPerUnitMultiplier = img.pixelsPerUnitMultiplier;
            b.raycastTarget = false;
            go.AddComponent<SafeAreaBleed>().Init(img, b);
        }
        else
        {
            var b = go.AddComponent<RawImage>();
            b.texture = raw.texture; b.uvRect = raw.uvRect; b.color = raw.color; b.material = raw.material;
            b.raycastTarget = false;
            go.AddComponent<SafeAreaBleed>().Init(raw, b);
        }
        return r;
    }

    static void Shift(float anchorMin, float anchorMax, float lowInset, float highInset, out float dMin, out float dMax)
    {
        if (anchorMin <= 0.01f && anchorMax >= 0.99f) { dMin = lowInset; dMax = -highInset; }   // stretched: shrink
        else if (anchorMax <= 0.01f) { dMin = dMax = lowInset; }                                  // anchored low side
        else if (anchorMin >= 0.99f) { dMin = dMax = -highInset; }                                // anchored high side
        else { dMin = dMax = (lowInset - highInset) * 0.5f; }                                     // centred
    }
}

/// <summary>The bleed copy of a picture created by SafeAreaInset (never moved again). The original picture keeps receiving
/// touches but is not drawn (its CanvasRenderer alpha is 0), so a see-through background is not drawn twice and its
/// edge never shows; the copy follows the original's colour, sprite and enabled state every frame (fades still work).</summary>
public class SafeAreaBleed : MonoBehaviour
{
    Graphic source, copy;

    public void Init(Graphic src, Graphic cpy) { source = src; copy = cpy; Sync(); }

    void LateUpdate() { Sync(); }

    void Sync()
    {
        if (source == null) { if (copy != null) copy.enabled = false; return; }
        if (copy == null) return;
        copy.enabled = source.enabled;
        if (copy.color != source.color) copy.color = source.color;
        if (source is Image si && copy is Image ci && ci.sprite != si.sprite) ci.sprite = si.sprite;
        if (source is RawImage sr && copy is RawImage cr && cr.texture != sr.texture) cr.texture = sr.texture;
        if (source.canvasRenderer.GetAlpha() > 0f) source.canvasRenderer.SetAlpha(0f); // keep touches, stop drawing
    }

    void OnDestroy() { if (source != null) source.canvasRenderer.SetAlpha(1f); }
}
