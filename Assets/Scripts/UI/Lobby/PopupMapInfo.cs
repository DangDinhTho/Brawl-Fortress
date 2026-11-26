using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Map info popup, opened by the "i" button on a map card (UIMapIcon) in PopupSelectMap.
/// args: [0] GameMode, [1] Map, [2] Sprite (the card's picture).
/// Shows the big map picture in a frame of the theme's colour, the price (free / tickets), the map name, the mode, the
/// art theme with a short description of it, the mode's objective and the map's special terrain (MapInfo).
/// The prefab is the ticket popup's frame (Panel + close button); the content is built here at runtime with the HUD
/// kit (rounded boxes, Lilita One with a thick outline), so it needs no extra art.
/// </summary>
public class PopupMapInfo : PopupBase
{
    public RectTransform panel;
    public Button closeBtn;
    [Tooltip("Font of the texts (the map cards' Lilita One)")] public TMP_FontAsset font;
    [Tooltip("Ticket icon of the price badge")] public Sprite ticketSprite;

    Image picture, frameOuter;
    TextMeshProUGUI title, modeText, themeText, descText, featHeader, costText;
    Image costBadge, costIcon;
    RectTransform featureList;
    bool built;

    static readonly Color Yellow = new Color(1f, 0.86f, 0.3f);

    void Awake()
    {
        if (closeBtn) closeBtn.onClick.AddListener(Dismiss);
        Build();
    }

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        Build();
        var mode = args.Length > 0 && args[0] is GameMode gm ? gm : GameMode.Competitive;
        var map = args.Length > 1 && args[1] is Map mp ? mp : Map.BrawlScene;
        var sprite = args.Length > 2 ? args[2] as Sprite : null;
        Fill(mode, map, sprite);
    }

    // ------------------------------------------------------------------ layout

    TextMeshProUGUI Label(string name, Transform parent, float size, Color color, TextAlignmentOptions align, float outline = 0.25f)
    {
        var t = BrawlHudKit.Text(name, parent, font, size, color, outline);
        t.alignment = align;
        return t;
    }

    static void Place(RectTransform r, Vector2 center, Vector2 size)
    {
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = center;
    }

    void Build()
    {
        if (built || panel == null) return;
        built = true;
        if (font == null) { var s = OverheadBarStyle.Get(); if (s != null) font = s.font; }

        // title
        title = Label("Title", panel, 52f, Color.white, TextAlignmentOptions.Center, 0.3f);
        Place(title.rectTransform, new Vector2(0f, 228f), new Vector2(820f, 70f));

        // picture: drop shadow, theme-coloured outer frame, dark inner border, rounded picture (mask)
        var pic = BrawlHudKit.Rect("Picture", panel);
        Place(pic, new Vector2(-232f, -6f), new Vector2(372f, 372f));
        var shadow = BrawlHudKit.Box("Shadow", pic, 30, new Color(0f, 0f, 0f, 0.45f)).rectTransform;
        BrawlHudKit.Stretch(shadow, -4f, -14f, -4f, 4f);
        frameOuter = BrawlHudKit.Box("Frame", pic, 30, Yellow);
        BrawlHudKit.Stretch(frameOuter.rectTransform, 0f);
        BrawlHudKit.Stretch(BrawlHudKit.Box("Border", pic, 24, BrawlHudKit.OutlineColor).rectTransform, 8f);
        var maskImg = BrawlHudKit.Box("Mask", pic, 18, Color.white);
        BrawlHudKit.Stretch(maskImg.rectTransform, 14f);
        var mask = maskImg.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
        picture = BrawlHudKit.Plain("Image", maskImg.rectTransform, Color.white);
        BrawlHudKit.Stretch(picture.rectTransform, 0f);
        picture.preserveAspect = false;

        // price badge, half over the picture's lower edge
        costBadge = BrawlHudKit.Box("Price", pic, 22, BrawlHudKit.OutlineColor);
        var cb = costBadge.rectTransform;
        cb.anchorMin = cb.anchorMax = new Vector2(0.5f, 0f); cb.pivot = new Vector2(0.5f, 0.5f);
        cb.sizeDelta = new Vector2(190f, 60f); cb.anchoredPosition = new Vector2(0f, 4f);
        var cbIn = BrawlHudKit.Box("Fill", cb, 16, new Color(0.25f, 0.75f, 0.2f));
        BrawlHudKit.Stretch(cbIn.rectTransform, 5f);
        costIcon = BrawlHudKit.Plain("Ticket", cb, Color.white);
        var ci = costIcon.rectTransform;
        ci.anchorMin = ci.anchorMax = new Vector2(0f, 0.5f); ci.pivot = new Vector2(0f, 0.5f);
        ci.sizeDelta = new Vector2(52f, 52f); ci.anchoredPosition = new Vector2(14f, 2f);
        costIcon.preserveAspect = true;
        costText = Label("Text", cb, 34f, Color.white, TextAlignmentOptions.Center, 0.3f);
        BrawlHudKit.Stretch(costText.rectTransform, 0f);

        // right column
        float colX = 222f, colW = 430f;
        modeText = Label("Mode", panel, 36f, Yellow, TextAlignmentOptions.Left, 0.28f);
        Place(modeText.rectTransform, new Vector2(colX, 158f), new Vector2(colW, 48f));
        themeText = Label("Theme", panel, 30f, Color.white, TextAlignmentOptions.Left, 0.26f);
        Place(themeText.rectTransform, new Vector2(colX, 116f), new Vector2(colW, 42f));
        descText = Label("Desc", panel, 22f, Color.white, TextAlignmentOptions.TopLeft, 0.22f);
        descText.enableWordWrapping = true;
        Place(descText.rectTransform, new Vector2(colX, 22f), new Vector2(colW, 150f));
        featHeader = Label("FeaturesTitle", panel, 28f, Yellow, TextAlignmentOptions.Left, 0.26f);
        Place(featHeader.rectTransform, new Vector2(colX, -78f), new Vector2(colW, 40f));
        featureList = BrawlHudKit.Rect("Features", panel);
        Place(featureList, new Vector2(colX, -175f), new Vector2(colW, 150f));
        var vl = featureList.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment = TextAnchor.UpperLeft; vl.spacing = 6f;
        vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

        if (closeBtn != null) closeBtn.transform.SetAsLastSibling();
    }

    void AddFeature(MapInfo.Feature f)
    {
        var row = BrawlHudKit.Rect("Feature", featureList);
        var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = 34f;
        var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 12f; hl.childAlignment = TextAnchor.UpperLeft;
        hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
        // coloured dot with a dark ring
        var dot = BrawlHudKit.Box("Dot", row, 11, BrawlHudKit.OutlineColor);
        var dle = dot.gameObject.AddComponent<LayoutElement>(); dle.minWidth = dle.preferredWidth = 24f; dle.minHeight = dle.preferredHeight = 24f;
        var inner = BrawlHudKit.Box("Fill", dot.rectTransform, 8, f.color);
        BrawlHudKit.Stretch(inner.rectTransform, 4f);
        var t = Label("Text", row, 22f, Color.white, TextAlignmentOptions.TopLeft, 0.22f);
        t.enableWordWrapping = true;
        t.text = f.text;
        var tle = t.gameObject.AddComponent<LayoutElement>(); tle.flexibleWidth = 1f; tle.preferredWidth = 380f;
    }

    // ------------------------------------------------------------------ data

    void Fill(GameMode mode, Map map, Sprite sprite)
    {
        Color accent = MapInfo.ThemeColor(map);
        title.text = LocalizationManager.Localize(map.ToString());
        modeText.text = MapInfo.ModeName(mode);
        themeText.text = MapInfo.ThemeName(map);
        themeText.color = Color.Lerp(accent, Color.white, 0.25f);
        string themeDesc = MapInfo.ThemeDescription(map);
        descText.text = (string.IsNullOrEmpty(themeDesc) ? "" : themeDesc + "\n") + MapInfo.ModeObjective(mode);
        featHeader.text = LocalizationManager.Localize("map_info_features");
        frameOuter.color = accent;
        picture.sprite = sprite;
        picture.color = sprite != null ? Color.white : new Color(0.2f, 0.2f, 0.25f);

        // price: free (default or event) or tickets
        int cost = ConfigManager.GetTicketCost(map);
        bool free = cost <= 0;
        costIcon.sprite = ticketSprite;
        costIcon.gameObject.SetActive(!free && ticketSprite != null);
        costText.text = free ? LocalizationManager.Localize(ConfigManager.IsFreeByEvent(map) ? "map_info_free_event" : "map_info_free") : "x" + cost;
        costText.rectTransform.offsetMin = new Vector2(!free && ticketSprite != null ? 58f : 0f, 0f);
        var fill = costBadge.transform.Find("Fill").GetComponent<Image>();
        fill.color = free ? new Color(0.25f, 0.75f, 0.2f) : new Color(0.95f, 0.55f, 0.1f);

        for (int i = featureList.childCount - 1; i >= 0; i--) Destroy(featureList.GetChild(i).gameObject);
        foreach (var f in MapInfo.Features(mode, map)) AddFeature(f);
    }
}
