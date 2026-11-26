using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared look of the in-match HUD (overhead bars, ammo bar), Brawl Stars style: chunky rounded rectangles with a thick
/// near-black border, saturated fills, bold Lilita One text with a thick dark outline and a drop shadow.
/// Sprites are generated at runtime (no texture assets): rounded rectangles drawn at 4x and shown with
/// pixelsPerUnitMultiplier 4, so corners stay smooth on the world-space canvases.
/// </summary>
public static class BrawlHudKit
{
    public const int SpriteScale = 4;
    public static readonly Color32 OutlineColor = new Color32(12, 8, 18, 255);

    static readonly Dictionary<int, Sprite> roundRects = new Dictionary<int, Sprite>();

    /// <summary>White 9-sliced rounded rectangle with corners of 'radius' canvas units (at the default 100 ppu).</summary>
    public static Sprite RoundRect(int radius)
    {
        radius = Mathf.Max(1, radius);
        if (roundRects.TryGetValue(radius, out var s) && s != null) return s;
        int r = radius * SpriteScale;
        int size = r * 2 + 4;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "BrawlRoundRect" + radius, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[size * size];
        float c0 = r, c1 = size - r;   // corner circle centres (inner square edges)
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float dx = fx < c0 ? c0 - fx : fx > c1 ? fx - c1 : 0f;
                float dy = fy < c0 ? c0 - fy : fy > c1 ? fy - c1 : 0f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);           // distance into the corner arc
                float a = Mathf.Clamp01(r - d + 0.5f);            // 1 px anti-aliased edge
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        float b = r + 1;
        s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        s.name = tex.name;
        roundRects[radius] = s;
        return s;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        return r;
    }

    /// <summary>Rounded rectangle image (sliced). 'radius' in canvas units.</summary>
    public static Image Box(string name, Transform parent, int radius, Color color)
    {
        var r = Rect(name, parent);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = RoundRect(radius);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = SpriteScale;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Plain rectangle (no sprite).</summary>
    public static Image Plain(string name, Transform parent, Color color)
    {
        var r = Rect(name, parent);
        var img = r.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static void Stretch(RectTransform r, float inset) => Stretch(r, inset, inset, inset, inset);

    public static void Stretch(RectTransform r, float left, float bottom, float right, float top)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>Anchored to the left edge of its parent, full height, 'width' units wide (for fills clipped by a mask).</summary>
    public static void LeftFixed(RectTransform r, float width)
    {
        r.anchorMin = new Vector2(0f, 0f); r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0f, 0.5f);
        r.offsetMin = new Vector2(0f, 0f); r.offsetMax = new Vector2(width, 0f);
    }

    static readonly Dictionary<long, Material> hudMaterials = new Dictionary<long, Material>();

    /// <summary>Shared material for HUD text in 'font' with a dark outline of 'outline' (SDF units) and a soft drop
    /// shadow. One per font + outline, so every overhead name / number batches together; set directly on the material
    /// (the TMP outlineWidth setter would make a material per text).</summary>
    public static Material HudMaterial(TMP_FontAsset font, float outline)
    {
        if (font == null || font.material == null) return null;
        long key = ((long)font.GetInstanceID() << 20) ^ Mathf.RoundToInt(outline * 1000f);
        if (hudMaterials.TryGetValue(key, out var m) && m != null) return m;
        m = new Material(font.material) { name = font.name + " HUD " + outline.ToString("0.00") };
        m.SetFloat(ShaderUtilities.ID_OutlineWidth, outline);
        m.SetColor(ShaderUtilities.ID_OutlineColor, OutlineColor);
        m.SetFloat(ShaderUtilities.ID_FaceDilate, 0.05f);
        m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.65f));
        m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.1f);
        m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.75f);
        m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.2f);
        m.SetFloat(ShaderUtilities.ID_UnderlayDilate, outline);
        hudMaterials[key] = m;
        return m;
    }

    /// <summary>Bold HUD text: thick dark outline + soft drop shadow (shared HudMaterial). font null = keep the text's.</summary>
    public static void StyleText(TMP_Text t, TMP_FontAsset font, float size, Color color, float outline = 0.3f)
    {
        if (font == null) font = t.font;
        if (font != null)
        {
            t.font = font;
            var m = HudMaterial(font, outline);
            if (m != null) t.fontSharedMaterial = m;
        }
        t.fontSize = size;
        t.fontStyle = FontStyles.Normal;
        t.color = color;
        t.raycastTarget = false;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, float size, Color color, float outline = 0.3f)
    {
        var t = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        StyleText(t, font, size, color, outline);
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    public static float DamageNumberScale = 1.55f;
    public static Color DamageTop = new Color(1f, 0.66f, 0.52f), DamageBottom = new Color(1f, 0.14f, 0.1f);

    /// <summary>Floating damage number (UltimateTextDamage item): bold Lilita One, red vertical gradient (light top),
    /// thick dark outline and drop shadow, ~1.5x the old size. Applied once per pooled item.</summary>
    public static void StyleDamageNumber(TMP_Text t)
    {
        if (t == null) return;
        var style = OverheadBarStyle.Get();
        StyleText(t, style != null ? style.font : null, t.fontSize * DamageNumberScale, Color.white, 0.34f);
        t.enableVertexGradient = true;
        t.colorGradient = new VertexGradient(DamageTop, DamageTop, DamageBottom, DamageBottom);
        t.alignment = TextAlignmentOptions.Center;
    }

    /// <summary>Darker version of a colour (same alpha).</summary>
    public static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

    /// <summary>A health step giving 3..10 segments: 10, 25, 50, 100, 250, 500, 1000...</summary>
    public static int TickStep(int maxHealth)
    {
        int[] steps = { 10, 25, 50, 100, 200, 250, 500, 1000, 2000, 2500, 5000, 10000 };
        foreach (int s in steps) if (maxHealth / (float)s <= 10f) return s;
        return Mathf.Max(1, maxHealth / 10);
    }
}
