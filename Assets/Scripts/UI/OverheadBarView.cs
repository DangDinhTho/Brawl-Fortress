using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The overhead health bar of a character, Brawl Stars style (built by HeaderUIForFun on the character's world canvas):
///  - chunky rounded bar: drop shadow, thick near-black border, dark inner back;
///  - saturated team fill (mine green, allies blue, enemies red) with a darker lower half and a light band, cut straight
///    at the health ratio (a mask), its left end rounded like the back;
///  - ruler ticks every BrawlHudKit.TickStep(max) health (25 for a 125-200 HP class, 50 for 300, 500 for a boss);
///  - white "damage trail" that holds a moment and slides down, white flash on each hit, pulse below 25%;
///  - big bold health number over the bar, bold name above it;
///  - the local player's ammo bar under it: one orange segment per bullet (clips up to 8; bigger clips: one bar with
///    quarter ticks), the segment being reloaded fills up in a lighter orange.
/// Works without a character (the editor preview in Tools/UI/Preview Overhead Bar builds one with test values).
/// </summary>
public class OverheadBarView : MonoBehaviour
{
    public const int MaxAmmoSegments = 8;

    OverheadBarStyle style;
    RectTransform bar, area, fillClip, trailClip, ticks, ammoRow;
    Image fill, fillDark, flash;
    TextMeshProUGUI hpText;
    TMP_Text nameText;
    float areaW, areaH;
    Color teamColor = Color.green;

    float target = 1f, shown = 1f, trailShown = 1f, trailWait, flashT;
    int hp = -1, hpMax = -1, tickStepBuilt = -1, tickMaxBuilt = -1;

    // ammo
    readonly List<RectTransform> ammoClips = new List<RectTransform>();
    readonly List<Image> ammoFills = new List<Image>();
    readonly List<Image> ammoFrames = new List<Image>();
    int ammoSegments = -1;
    RectTransform ammoTicks;
    float emptyFlash;

    const float FillSpeed = 10f, TrailDelay = 0.35f, TrailSpeed = 1.2f, FlashTime = 0.18f;

    public RectTransform Bar => bar;

    public static OverheadBarView Create(RectTransform parent, Vector2 at, OverheadBarStyle style, TMP_Text existingName, bool withAmmo)
    {
        var root = BrawlHudKit.Rect("OverheadBar", parent);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = style.barSize;
        root.anchoredPosition = at;
        root.localScale = Vector3.one * Mathf.Max(0.1f, style.scale);
        var v = root.gameObject.AddComponent<OverheadBarView>();
        v.style = style;
        v.bar = root;
        v.Build(existingName, withAmmo);
        return v;
    }

    void Build(TMP_Text existingName, bool withAmmo)
    {
        Vector2 size = style.barSize;
        int b = Mathf.RoundToInt(style.outline);
        int radius = Mathf.RoundToInt(style.cornerRadius);

        // drop shadow, border, back
        var shadow = BrawlHudKit.Box("Shadow", bar, radius + 2, new Color(0f, 0f, 0f, 0.38f)).rectTransform;
        BrawlHudKit.Stretch(shadow, -3f, -11f, -3f, 4f);
        BrawlHudKit.Stretch(BrawlHudKit.Box("Border", bar, radius, style.border).rectTransform, 0f);
        BrawlHudKit.Stretch(BrawlHudKit.Box("Back", bar, Mathf.Max(2, radius - b), style.back).rectTransform, b);

        area = BrawlHudKit.Rect("Area", bar);
        BrawlHudKit.Stretch(area, b);
        areaW = size.x - 2 * b; areaH = size.y - 2 * b;
        int innerR = Mathf.Max(2, radius - b);

        // trail (white) and fill: full-width rounded boxes inside left-anchored masks whose width = the ratio
        trailClip = Clip("TrailClip", area);
        var trail = BrawlHudKit.Box("Trail", trailClip, innerR, style.trail);
        BrawlHudKit.LeftFixed(trail.rectTransform, areaW);

        fillClip = Clip("FillClip", area);
        fill = BrawlHudKit.Box("Fill", fillClip, innerR, teamColor);
        BrawlHudKit.LeftFixed(fill.rectTransform, areaW);
        // darker lower half + light band: the chunky two-tone look
        // (a full-size box cut by a mask over the lower half: straight top edge, rounded bottom corners)
        var darkClip = BrawlHudKit.Rect("DarkClip", fill.rectTransform);
        darkClip.gameObject.AddComponent<RectMask2D>();
        BrawlHudKit.Stretch(darkClip, 0f, 0f, 0f, areaH * 0.52f);
        fillDark = BrawlHudKit.Box("FillDark", darkClip, innerR, BrawlHudKit.Shade(teamColor, 0.72f));
        fillDark.rectTransform.anchorMin = new Vector2(0f, 0f); fillDark.rectTransform.anchorMax = new Vector2(1f, 0f);
        fillDark.rectTransform.pivot = new Vector2(0.5f, 0f);
        fillDark.rectTransform.offsetMin = Vector2.zero; fillDark.rectTransform.offsetMax = new Vector2(0f, areaH);
        var band = BrawlHudKit.Box("Shine", fill.rectTransform, Mathf.Max(2, innerR / 2), new Color(1f, 1f, 1f, 0.38f));
        BrawlHudKit.Stretch(band.rectTransform, innerR * 0.6f, areaH * 0.62f, innerR * 0.6f, areaH * 0.12f);
        flash = BrawlHudKit.Box("Flash", fill.rectTransform, innerR, new Color(1f, 1f, 1f, 0f));
        BrawlHudKit.Stretch(flash.rectTransform, 0f);

        // ruler ticks over fill and back
        ticks = BrawlHudKit.Rect("Ticks", area);
        BrawlHudKit.Stretch(ticks, 0f);

        // inner top shadow: depth under the border
        var inner = BrawlHudKit.Plain("InnerShade", area, new Color(0f, 0f, 0f, 0.22f)).rectTransform;
        inner.anchorMin = new Vector2(0f, 1f); inner.anchorMax = new Vector2(1f, 1f); inner.pivot = new Vector2(0.5f, 1f);
        inner.offsetMin = new Vector2(innerR * 0.5f, -Mathf.Max(3f, areaH * 0.09f)); inner.offsetMax = new Vector2(-innerR * 0.5f, 0f);

        // health number, over the bar (a little high: it reads as sitting on the bar)
        hpText = BrawlHudKit.Text("HP", bar, style.font, style.hpSize, Color.white, 0.32f);
        BrawlHudKit.Stretch(hpText.rectTransform, -60f, -10f, -60f, -20f);
        hpText.rectTransform.anchoredPosition += new Vector2(0f, size.y * 0.06f);

        // name above
        nameText = existingName;
        if (nameText != null)
        {
            var nr = nameText.rectTransform;
            nr.SetParent(bar, false);
            nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 1f);
            nr.pivot = new Vector2(0.5f, 0f);
            nr.sizeDelta = new Vector2(1000f, style.nameSize * 1.3f);
            nr.anchoredPosition = new Vector2(0f, style.nameGap);
            BrawlHudKit.StyleText(nameText, style.font, style.nameSize, Color.white, 0.3f);
            nameText.alignment = TextAlignmentOptions.Bottom;
        }

        if (withAmmo)
        {
            ammoRow = BrawlHudKit.Rect("Ammo", bar);
            ammoRow.anchorMin = ammoRow.anchorMax = new Vector2(0.5f, 0f);
            ammoRow.pivot = new Vector2(0.5f, 1f);
            ammoRow.sizeDelta = new Vector2(size.x * style.ammoWidth, style.ammoHeight);
            ammoRow.anchoredPosition = new Vector2(0f, -style.ammoGap);
            ammoRow.gameObject.SetActive(false);
        }

        hpText.transform.SetAsLastSibling();
        if (nameText != null) nameText.transform.SetAsLastSibling();
        Apply();
    }

    /// <summary>Removes the children now (edit-mode preview) or at the end of the frame (play mode).</summary>
    static void Clear(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            var go = t.GetChild(i).gameObject;
            go.transform.SetParent(null, false);
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
    }

    static RectTransform Clip(string name, RectTransform parent)
    {
        var r = BrawlHudKit.Rect(name, parent);
        r.gameObject.AddComponent<RectMask2D>();
        r.anchorMin = new Vector2(0f, 0f); r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0f, 0.5f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }

    // ------------------------------------------------------------------ data

    public void SetTeamColor(Color c)
    {
        teamColor = c;
        if (fill != null) fill.color = c;
        if (fillDark != null) fillDark.color = BrawlHudKit.Shade(c, 0.72f);
    }

    public void SetName(string name, Color tint)
    {
        if (nameText == null) return;
        nameText.text = name;
        nameText.color = tint;
    }

    /// <summary>Health changed. 'instant' = no animation (spawn / respawn).</summary>
    public void SetHealth(int current, int max, bool instant = false)
    {
        max = Mathf.Max(1, max);
        float ratio = Mathf.Clamp01(current / (float)max);
        if (!instant && hp >= 0 && current < hp) { trailWait = TrailDelay; flashT = FlashTime; }
        target = ratio;
        if (instant) shown = trailShown = ratio;
        hp = current; hpMax = max;
        if (hpText != null) hpText.text = current > 0 ? current.ToString() : "";
        BuildTicks();
        Apply();
    }

    void BuildTicks()
    {
        int step = BrawlHudKit.TickStep(hpMax);
        if (step == tickStepBuilt && hpMax == tickMaxBuilt) return;
        tickStepBuilt = step; tickMaxBuilt = hpMax;
        Clear(ticks);
        float w = Mathf.Max(3f, style.tickWidth);
        for (int v = step; v < hpMax; v += step)
        {
            float x = areaW * v / hpMax;
            if (x < w * 2f || x > areaW - w * 2f) continue;
            var t = BrawlHudKit.Plain("Tick", ticks, style.tick).rectTransform;
            t.anchorMin = new Vector2(0f, 0f); t.anchorMax = new Vector2(0f, 1f);
            t.pivot = new Vector2(0.5f, 0.5f);
            // a ruler mark across the whole bar (the number sits on top of it)
            t.offsetMin = new Vector2(x - w * 0.5f, 0f); t.offsetMax = new Vector2(x + w * 0.5f, 0f);
        }
    }

    /// <summary>Local player's ammo: 'ammo' bullets (fractional while reloading) out of 'clip'. clip &lt;= 0 hides the bar.</summary>
    public void SetAmmo(float ammo, int clip)
    {
        if (ammoRow == null) return;
        bool show = clip > 0;
        if (ammoRow.gameObject.activeSelf != show) ammoRow.gameObject.SetActive(show);
        if (!show) return;
        int segs = clip <= MaxAmmoSegments ? clip : 1;
        if (segs != ammoSegments) BuildAmmo(segs, clip);
        float perSeg = segs == 1 ? clip : 1f;
        for (int i = 0; i < ammoSegments; i++)
        {
            float f = Mathf.Clamp01((ammo - i * perSeg) / perSeg);
            var clipR = ammoClips[i];
            clipR.anchorMax = new Vector2(f, 1f);
            // a full segment is bright orange, the one being reloaded a lighter, paler orange
            ammoFills[i].color = f >= 0.999f ? style.ammo : style.ammoReload;
        }
        // out of ammo: the frame blinks red
        if (ammo < 0.05f) emptyFlash += Time.deltaTime; else emptyFlash = 0f;
        Color frame = ammo < 0.05f && Mathf.Repeat(emptyFlash, 0.5f) < 0.25f ? new Color(0.75f, 0.1f, 0.1f) : (Color)style.border;
        for (int i = 0; i < ammoFrames.Count; i++) ammoFrames[i].color = frame;
    }

    void BuildAmmo(int segs, int clip)
    {
        ammoSegments = segs;
        ammoClips.Clear(); ammoFills.Clear(); ammoFrames.Clear();
        Clear(ammoRow);
        float w = ammoRow.sizeDelta.x, h = ammoRow.sizeDelta.y;
        float gap = segs > 1 ? style.ammoSegmentGap : 0f;
        float segW = (w - gap * (segs - 1)) / segs;
        int b = Mathf.RoundToInt(style.ammoOutline);
        int r = Mathf.RoundToInt(Mathf.Min(style.ammoCornerRadius, h * 0.5f));
        RectTransform lastInner = null;
        for (int i = 0; i < segs; i++)
        {
            var seg = BrawlHudKit.Rect("Seg" + i, ammoRow);
            seg.anchorMin = seg.anchorMax = new Vector2(0f, 0.5f);
            seg.pivot = new Vector2(0f, 0.5f);
            seg.sizeDelta = new Vector2(segW, h);
            seg.anchoredPosition = new Vector2(i * (segW + gap), 0f);
            var shadow = BrawlHudKit.Box("Shadow", seg, r + 1, new Color(0f, 0f, 0f, 0.35f)).rectTransform;
            BrawlHudKit.Stretch(shadow, -2f, -7f, -2f, 2f);
            var frame = BrawlHudKit.Box("Frame", seg, r, style.border);
            BrawlHudKit.Stretch(frame.rectTransform, 0f);
            ammoFrames.Add(frame);
            BrawlHudKit.Stretch(BrawlHudKit.Box("Back", seg, Mathf.Max(2, r - b), style.back).rectTransform, b);
            var inner = BrawlHudKit.Rect("Inner", seg);
            BrawlHudKit.Stretch(inner, b);
            lastInner = inner;
            var c = Clip("Clip", inner);
            float iw = segW - 2 * b, ih = h - 2 * b;
            var f = BrawlHudKit.Box("Fill", c, Mathf.Max(2, r - b), style.ammo);
            BrawlHudKit.LeftFixed(f.rectTransform, iw);
            var darkClip = BrawlHudKit.Rect("DarkClip", f.rectTransform);
            darkClip.gameObject.AddComponent<RectMask2D>();
            BrawlHudKit.Stretch(darkClip, 0f, 0f, 0f, ih * 0.5f);
            var dark = BrawlHudKit.Box("Dark", darkClip, Mathf.Max(2, r - b), new Color(0.55f, 0.22f, 0f, 0.45f));
            dark.rectTransform.anchorMin = new Vector2(0f, 0f); dark.rectTransform.anchorMax = new Vector2(1f, 0f);
            dark.rectTransform.pivot = new Vector2(0.5f, 0f);
            dark.rectTransform.offsetMin = Vector2.zero; dark.rectTransform.offsetMax = new Vector2(0f, ih);
            var shine = BrawlHudKit.Box("Shine", f.rectTransform, 2, new Color(1f, 1f, 1f, 0.4f));
            BrawlHudKit.Stretch(shine.rectTransform, 3f, ih * 0.6f, 3f, ih * 0.14f);
            ammoClips.Add(c); ammoFills.Add(f);
        }
        // one long bar (big clips): quarter ticks
        if (segs == 1)
        {
            float iw = segW - 2 * b;
            for (int q = 1; q < 4; q++)
            {
                var t = BrawlHudKit.Plain("Tick", lastInner, style.tick).rectTransform;
                t.anchorMin = new Vector2(0f, 0f); t.anchorMax = new Vector2(0f, 1f);
                float x = iw * q / 4f, tw = Mathf.Max(3f, style.tickWidth * 0.8f);
                t.offsetMin = new Vector2(x - tw * 0.5f, 0f); t.offsetMax = new Vector2(x + tw * 0.5f, 0f);
            }
        }
    }

    // ------------------------------------------------------------------ animation

    void LateUpdate() => Tick(Time.deltaTime);

    public void Tick(float dt)
    {
        if (fill == null) return;
        // fill: quick and smooth (heals grow the same way)
        shown = Mathf.MoveTowards(shown, target, Mathf.Max(0.02f, Mathf.Abs(target - shown)) * FillSpeed * dt);
        // trail: holds a moment after a hit, then slides down to the fill
        if (trailShown < shown) trailShown = shown;
        else if (trailWait > 0f) trailWait -= dt;
        else trailShown = Mathf.MoveTowards(trailShown, shown, TrailSpeed * dt);
        flashT = Mathf.Max(0f, flashT - dt);
        Apply();
    }

    void Apply()
    {
        if (fillClip == null) return;
        fillClip.anchorMax = new Vector2(Mathf.Clamp01(shown), 1f);
        trailClip.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(shown, trailShown)), 1f);
        trailClip.gameObject.SetActive(trailShown > shown + 0.003f);
        // low health: the fill pulses brighter
        Color c = teamColor;
        if (shown < 0.25f && shown > 0f)
        {
            float p = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
            c = Color.Lerp(teamColor, Color.white, 0.25f * p);
        }
        fill.color = c;
        if (flash != null) flash.color = new Color(1f, 1f, 1f, 0.75f * (flashT / FlashTime));
    }
}
