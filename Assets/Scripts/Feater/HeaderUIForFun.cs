using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overhead name + health bar (world-space canvas on every character), Brawl Stars style:
///  - rounded pill bar with a thick dark border, dark inner back;
///  - bright team-coloured fill (mine green, allies blue, enemies red) with a light band on top and a darker base,
///    shrinking smoothly with rounded ends; a white "damage trail" shows the health just lost and catches up;
///  - health number in the bar, bold name above it in the team colour with a thick outline and a drop shadow.
/// The new look is built at runtime over the prefab's old bar (hidden), so every character prefab gets it.
/// Look / colours: Resources/UI/OverheadBarStyle.
/// </summary>
public class HeaderUIForFun : MonoBehaviour
{
    public Transform guiTransform;
    public Image image;
    private Transform mainCamera;
    public TF2Player ownerPlayer;
    public Image health;
    public TMP_Text txtName;

    // --- Brawl-style bar
    RectTransform fill, trail;
    Image fillImg;
    TMP_Text txtHp;
    float target = 1f, shown = 1f, trailShown = 1f, trailWait;
    int hp, hpMax;
    const float FillSpeed = 10f, TrailDelay = 0.35f, TrailSpeed = 1.1f;

    public void Start()
    {
        ownerPlayer.healthManager.OnDeath += OnPlayerDead;
        ownerPlayer.healthManager.OnRespawn += OnPlayerRespawn;
        ownerPlayer.healthManager.OnHealthChanged += OnHealthChanged;
        if (health) health.fillAmount = 1;
        mainCamera = CameraController.instan.GetCurrentCamera().transform;
        image.gameObject.SetActive(false);
        BuildBar();
        if (ownerPlayer is TF2Zombie && txtName != null) txtName.gameObject.SetActive(false); // zombies: health bar only
        hpMax = ownerPlayer.healthManager.maxHealth > 0 ? (int)ownerPlayer.healthManager.maxHealth : 0;
        hp = (int)ownerPlayer.healthManager.currentHealth;
        UpdateHpText();
        Invoke(nameof(InitLate), 1);
    }

    // ------------------------------------------------------------------ build

    static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        return r;
    }

    static Image Pill(string name, Transform parent, Sprite sprite, Color color)
    {
        var r = Rect(name, parent);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = sprite; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false;
        img.pixelsPerUnitMultiplier = 1f;
        return img;
    }

    static void Stretch(RectTransform r, float inset)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset); r.offsetMax = new Vector2(-inset, -inset);
    }

    void BuildBar()
    {
        var style = OverheadBarStyle.Get();
        if (style == null || style.pill == null || health == null) return;
        var oldBar = health.transform.parent as RectTransform;   // "Health" (background) holding "Fill"
        var canvas = oldBar != null ? oldBar.parent : health.transform.parent;
        Vector2 at = oldBar != null ? oldBar.anchoredPosition : Vector2.zero;
        if (oldBar != null) oldBar.gameObject.SetActive(false);

        var bar = Rect("BrawlBar", canvas);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
        bar.sizeDelta = style.barSize;
        bar.anchoredPosition = at;

        float o = style.outline;
        Stretch(Pill("Border", bar, style.pill, style.border).rectTransform, 0f);
        var inner = Pill("Back", bar, style.pill, style.back).rectTransform;
        Stretch(inner, o);

        // the moving parts live in an area inset by the border; their width follows the health ratio
        var area = Rect("Area", bar);
        Stretch(area, o);
        trail = Pill("Trail", area, style.pill, style.trail).rectTransform;
        fillImg = Pill("Fill", area, style.pill, style.mine);
        fill = fillImg.rectTransform;
        foreach (var r in new[] { trail, fill }) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; r.pivot = new Vector2(0f, 0.5f); }

        // depth on the fill: dark base + light band on top
        var shade = Pill("Shade", fill, style.pill, new Color(0f, 0f, 0f, 0.22f)).rectTransform;
        shade.anchorMin = new Vector2(0f, 0f); shade.anchorMax = new Vector2(1f, 0.42f); shade.offsetMin = shade.offsetMax = Vector2.zero;
        var shine = Pill("Shine", fill, style.pill, new Color(1f, 1f, 1f, 0.42f)).rectTransform;
        shine.anchorMin = new Vector2(0f, 0.55f); shine.anchorMax = new Vector2(1f, 0.92f);
        shine.offsetMin = new Vector2(8f, 0f); shine.offsetMax = new Vector2(-8f, 0f);

        // health number
        txtHp = Rect("HP", bar).gameObject.AddComponent<TextMeshProUGUI>();
        Stretch(txtHp.rectTransform, 0f);
        StyleText(txtHp, style, style.hpSize, Color.white, 0.32f);
        txtHp.alignment = TextAlignmentOptions.Center;

        // name above the bar
        if (txtName != null)
        {
            var nr = txtName.rectTransform;
            nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0.5f);
            nr.pivot = new Vector2(0.5f, 0f);
            nr.sizeDelta = new Vector2(900f, style.nameSize * 1.3f);
            nr.anchoredPosition = at + new Vector2(0f, style.barSize.y * 0.5f + style.nameGap * 0.25f);
            StyleText(txtName, style, style.nameSize, Color.white, 0.3f);
            txtName.alignment = TextAlignmentOptions.Bottom;
            txtName.enableWordWrapping = false;
            txtName.overflowMode = TextOverflowModes.Overflow;
            txtName.transform.SetAsLastSibling();
        }
        txtHp.transform.SetAsLastSibling();
        ApplyRatio(1f, 1f);
    }

    static void StyleText(TMP_Text t, OverheadBarStyle style, float size, Color color, float outline)
    {
        if (style.font != null) t.font = style.font;
        t.fontSize = size;
        t.fontStyle = FontStyles.Normal;
        t.color = color;
        t.raycastTarget = false;
        // thick dark outline + soft drop shadow (per-text material instance)
        t.outlineWidth = outline;
        t.outlineColor = new Color32(14, 10, 18, 255);
        var m = t.fontMaterial;
        m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.6f));
        m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.15f);
        m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
        m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);
        m.SetFloat(ShaderUtilities.ID_UnderlayDilate, outline);
    }

    void ApplyRatio(float f, float t)
    {
        if (fill != null) fill.anchorMax = new Vector2(Mathf.Clamp01(f), 1f);
        if (trail != null) trail.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(f, t)), 1f);
        // a pill narrower than its height looks broken: hide the sliver
        if (fill != null) fill.gameObject.SetActive(f > 0.02f);
        if (trail != null) trail.gameObject.SetActive(t > f + 0.005f);
    }

    void UpdateHpText()
    {
        if (txtHp != null) txtHp.text = hp > 0 ? hp.ToString() : "";
    }

    // ------------------------------------------------------------------ data

    public void InitLate()
    {
        var style = OverheadBarStyle.Get();
        Color c;
        if (ownerPlayer.isMainPlayer) c = style != null ? style.mine : GUIManager.instan.mineColor;
        else if (ArenaManager.Instance != null && ArenaManager.Instance.mainPlayer != null && ownerPlayer.team == ArenaManager.Instance.mainPlayer.team) c = style != null ? style.ally : GUIManager.instan.teamMateColor;
        else c = style != null ? style.enemy : GUIManager.instan.enemyColor;

        if (health) health.color = c;
        if (fillImg != null) fillImg.color = c;
        if (txtName)
        {
            txtName.text = ownerPlayer.playerName;
            // name in a lighter tint of the team colour, readable on any background
            txtName.color = Color.Lerp(c, Color.white, 0.35f);
        }
    }

    public void OnPlayerDead()
    {
        gameObject.SetActive(false);
    }

    public void OnPlayerRespawn()
    {
        gameObject.SetActive(true);
        if (health) health.fillAmount = 1;
        target = shown = trailShown = 1f;
        hp = hpMax;
        ApplyRatio(1f, 1f);
        UpdateHpText();
    }

    public void OnHealthChanged(int crHealth, int maxHealth)
    {
        float ratio = maxHealth > 0 ? crHealth * 1.0f / maxHealth : 0f;
        if (health) health.fillAmount = ratio;
        if (ratio < target) trailWait = TrailDelay;      // lost health: the trail waits, then follows
        target = ratio;
        hp = crHealth; hpMax = maxHealth;
        UpdateHpText();
    }

    public void InitData(Sprite sprite)
    {
        if (ownerPlayer.healthManager.death)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        image.gameObject.SetActive(true);
        mainCamera = CameraController.instan.GetCurrentCamera().transform;

        image.sprite = sprite;
        StopAllCoroutines();
        StartCoroutine(WaitForOff());
    }

    IEnumerator WaitForOff()
    {
        yield return new WaitForSeconds(5);
        image.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        guiTransform.LookAt(guiTransform.position + mainCamera.forward);

        if (fill == null) return;
        float dt = Time.deltaTime;
        // fill: quick and smooth; healing grows it the same way
        shown = Mathf.MoveTowards(shown, target, Mathf.Max(0.02f, Mathf.Abs(target - shown)) * FillSpeed * dt);
        // trail: holds a moment on damage, then slides down to the fill
        if (trailShown < shown) trailShown = shown;
        else if (trailWait > 0f) trailWait -= dt;
        else trailShown = Mathf.MoveTowards(trailShown, shown, TrailSpeed * dt);
        ApplyRatio(shown, trailShown);
    }
}
