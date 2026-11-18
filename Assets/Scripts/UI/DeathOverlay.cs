using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shown while the local player is dead: a dark translucent overlay, a skull icon and the death texts ("killed by ...",
/// respawn countdown) restyled to read clearly (red, bold, bigger, outlined). Built at runtime around the two existing
/// texts of UICharacterInputController, so no scene / prefab change is needed. Does not block touches.
/// </summary>
public class DeathOverlay : MonoBehaviour
{
    public static readonly Color DimColor = new Color(0.07f, 0f, 0f, 0.6f);
    public static readonly Color ReasonColor = new Color(1f, 0.2f, 0.16f);
    public static readonly Color TimerColor = new Color(1f, 0.62f, 0.56f);
    const float FadeTime = 0.35f;

    CanvasGroup group;
    RectTransform skull;
    Coroutine anim;

    /// <summary>Creates the overlay once (around the given texts) and returns it.</summary>
    public static DeathOverlay Ensure(Text reason, Text timer)
    {
        var canvas = reason.canvas != null ? reason.canvas.rootCanvas : null;
        if (canvas == null) return null;
        var existing = canvas.GetComponentInChildren<DeathOverlay>(true);
        if (existing != null) return existing;

        var go = new GameObject("DeathOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(DeathOverlay));
        go.layer = reason.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        Stretch(rt);
        rt.SetAsLastSibling();
        var overlay = go.GetComponent<DeathOverlay>();
        overlay.group = go.GetComponent<CanvasGroup>();
        overlay.group.blocksRaycasts = false;
        overlay.group.interactable = false;
        overlay.group.alpha = 0f;

        // dim layer
        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.layer = go.layer;
        var dimRt = (RectTransform)dim.transform;
        dimRt.SetParent(rt, false);
        Stretch(dimRt);
        var dimImg = dim.GetComponent<Image>();
        dimImg.color = DimColor;
        dimImg.raycastTarget = false;

        // skull
        var sk = new GameObject("Skull", typeof(RectTransform), typeof(Image));
        sk.layer = go.layer;
        overlay.skull = (RectTransform)sk.transform;
        overlay.skull.SetParent(rt, false);
        overlay.skull.anchorMin = overlay.skull.anchorMax = new Vector2(0.5f, 0.5f);
        overlay.skull.sizeDelta = new Vector2(160f, 160f);
        overlay.skull.anchoredPosition = new Vector2(0f, 120f);
        var skImg = sk.GetComponent<Image>();
        skImg.sprite = Resources.Load<Sprite>("UI/icon_skull");
        skImg.preserveAspect = true;
        skImg.raycastTarget = false;

        // the two existing texts move into the overlay, centred under the skull
        Place(reason, rt, new Vector2(0f, -5f), 1.35f, ReasonColor);
        Place(timer, rt, new Vector2(0f, -70f), 1.15f, TimerColor);

        go.SetActive(false);
        return overlay;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        r.localScale = Vector3.one;
    }

    static void Place(Text t, RectTransform parent, Vector2 pos, float sizeScale, Color color)
    {
        if (t == null) return;
        var r = t.rectTransform;
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(1100f, Mathf.Max(60f, t.fontSize * sizeScale * 1.6f));
        r.anchoredPosition = pos;
        r.localScale = Vector3.one;
        t.fontSize = Mathf.RoundToInt(t.fontSize * sizeScale);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.color = color;
        t.raycastTarget = false;
        var outline = t.GetComponent<Outline>();
        if (outline == null) outline = t.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);
        if (t.GetComponents<Shadow>().Length < 2)
        {
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -5f);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Appear());
    }

    public void Hide()
    {
        if (anim != null) StopCoroutine(anim);
        anim = null;
        gameObject.SetActive(false);
    }

    IEnumerator Appear()
    {
        float t = 0f;
        while (t < FadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / FadeTime);
            group.alpha = k;
            // small pop of the skull: 0.6 -> 1.12 -> 1
            float s = k < 0.7f ? Mathf.Lerp(0.6f, 1.12f, k / 0.7f) : Mathf.Lerp(1.12f, 1f, (k - 0.7f) / 0.3f);
            if (skull) skull.localScale = Vector3.one * s;
            yield return null;
        }
        group.alpha = 1f;
        if (skull) skull.localScale = Vector3.one;
        anim = null;
    }
}
