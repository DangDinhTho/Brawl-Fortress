using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Team foot ring + Brawl-style health bar for Engineer buildings (sentry, dispenser, teleporters), the same look as the
/// characters: green = mine, blue = ally, red = enemy. Added by BuildableEntry.Start.
/// </summary>
[DisallowMultipleComponent]
public class BuildableMarker : MonoBehaviour
{
    BuildableEntry entry;
    HealthBase health;

    // ring
    Transform disc; MeshRenderer ringRend; MaterialPropertyBlock mpb;
    float radius = 0.8f;
    static Mesh quad; static Material ringMat;
    const float RingUv = 0.72f; // same as FootRing
    static readonly int ColorId = Shader.PropertyToID("_Color"), FillId = Shader.PropertyToID("_FillAlpha"), RingRId = Shader.PropertyToID("_RingR"),
        WidthId = Shader.PropertyToID("_Width"), DashedId = Shader.PropertyToID("_Dashed"), DashCountId = Shader.PropertyToID("_DashCount"),
        ProgressId = Shader.PropertyToID("_Progress"), GlowId = Shader.PropertyToID("_Glow"),
        CenterFillId = Shader.PropertyToID("_CenterFill"), FalloffId = Shader.PropertyToID("_Falloff"), RimAlphaId = Shader.PropertyToID("_RimAlpha");

    // sentry range ring (faint dashed circle at the shooting radius)
    Transform rangeDisc; MeshRenderer rangeRend; FieldOfView sentryFov;

    // bar
    Transform canvasT; RectTransform fill, trail; Image fillImg; TMP_Text txtHp, txtLevel;
    float target = 1f, shown = 1f, trailShown = 1f, trailWait, top = 1.4f;
    int lastLevel = -1;

    void Start()
    {
        entry = GetComponent<BuildableEntry>();
        health = entry != null ? entry.health : GetComponent<HealthBase>();
        Measure();
        BuildRing();
        BuildRangeRing();
        BuildBar();
        if (health != null) health.OnHealthChanged += OnHealthChanged;
        if (health != null && health.maxHealth > 0) { target = shown = trailShown = Mathf.Clamp01(health.currentHealth / (float)health.maxHealth); UpdateTexts(); }
    }

    void OnDestroy()
    {
        if (health != null) health.OnHealthChanged -= OnHealthChanged;
        if (disc != null) Destroy(disc.gameObject);
        if (rangeDisc != null) Destroy(rangeDisc.gameObject);
    }

    /// <summary>Footprint radius and top of the model from its renderers.</summary>
    void Measure()
    {
        var rs = GetComponentsInChildren<Renderer>(true);
        bool any = false; Bounds b = new Bounds(transform.position, Vector3.zero);
        foreach (var r in rs)
        {
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || !r.enabled) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        if (!any) return;
        radius = Mathf.Clamp(Mathf.Max(b.extents.x, b.extents.z) * 0.85f, 0.5f, 1.6f);
        top = Mathf.Clamp(b.max.y - transform.position.y, 0.6f, 3f);
    }

    // ---------------------------------------------------------------- ring
    void BuildRing()
    {
        if (ringMat == null) ringMat = Resources.Load<Material>("Materials/FootRing");
        if (ringMat == null) return;
        if (quad == null)
        {
            quad = new Mesh { name = "BuildableRingQuad" };
            quad.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quad.RecalculateNormals(); quad.RecalculateBounds();
        }
        var go = new GameObject("BuildableRing");
        go.layer = gameObject.layer;
        disc = go.transform;   // not parented: the building's scale must not squash it
        go.AddComponent<MeshFilter>().sharedMesh = quad;
        ringRend = go.AddComponent<MeshRenderer>();
        ringRend.sharedMaterial = ringMat;
        ringRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ringRend.receiveShadows = false;
        mpb = new MaterialPropertyBlock();
    }

    Color TeamColor(out bool mine)
    {
        var viewer = ArenaManager.Instance != null ? ArenaManager.Instance.mainPlayer : null;
        mine = entry != null && entry.owner != null && viewer != null && entry.owner == viewer;
        var style = OverheadBarStyle.Get();
        Team team = entry != null ? entry.team : (health != null ? health.team : Team.None);
        if (mine) return style != null ? style.mine : FootRing.SelfColor;
        if (viewer == null || team == viewer.team) return style != null ? style.ally : FootRing.AllyColor;
        return style != null ? style.enemy : FootRing.EnemyColor;
    }

    void BuildRangeRing()
    {
        if (entry == null || entry.buildableType != BuildableType.Sentry || ringMat == null || quad == null) return;
        sentryFov = GetComponent<FieldOfView>();
        if (sentryFov == null) return;
        var go = new GameObject("SentryRangeRing");
        go.layer = gameObject.layer;
        rangeDisc = go.transform;
        go.AddComponent<MeshFilter>().sharedMesh = quad;
        rangeRend = go.AddComponent<MeshRenderer>();
        rangeRend.sharedMaterial = ringMat;
        rangeRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rangeRend.receiveShadows = false;
    }

    // ---------------------------------------------------------------- bar
    static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var r = (RectTransform)go.transform; r.SetParent(parent, false);
        return r;
    }

    static Image Pill(string name, Transform parent, Sprite s, Color c)
    {
        var img = Rect(name, parent).gameObject.AddComponent<Image>();
        img.sprite = s; img.type = Image.Type.Sliced; img.color = c; img.raycastTarget = false;
        return img;
    }

    static void Stretch(RectTransform r, float inset) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(inset, inset); r.offsetMax = new Vector2(-inset, -inset); }

    static TMP_Text Label(string name, Transform parent, OverheadBarStyle style, float size)
    {
        var t = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (style.font != null) t.font = style.font;
        t.fontSize = size; t.color = Color.white; t.raycastTarget = false; t.alignment = TextAlignmentOptions.Center;
        t.outlineWidth = 0.3f; t.outlineColor = new Color32(14, 10, 18, 255);
        return t;
    }

    void BuildBar()
    {
        var style = OverheadBarStyle.Get();
        if (style == null || style.pill == null) return;
        var cgo = new GameObject("BuildableBar", typeof(RectTransform), typeof(Canvas));
        cgo.layer = gameObject.layer;
        canvasT = cgo.transform;
        canvasT.SetParent(transform, false);
        var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        var crt = (RectTransform)canvasT; crt.sizeDelta = new Vector2(600f, 200f);
        var ls = transform.lossyScale;
        canvasT.localScale = new Vector3(0.0016f / Mathf.Max(ls.x, 1e-4f), 0.0016f / Mathf.Max(ls.y, 1e-4f), 0.0016f / Mathf.Max(ls.z, 1e-4f));

        var size = new Vector2(style.barSize.x * 0.85f, style.barSize.y * 1.25f); // thicker than the characters' bar
        var bar = Rect("Bar", canvasT); bar.sizeDelta = size;
        float o = style.outline * 1.1f;
        Stretch(Pill("Border", bar, style.pill, style.border).rectTransform, 0f);
        Stretch(Pill("Back", bar, style.pill, style.back).rectTransform, o);
        var area = Rect("Area", bar); Stretch(area, o);
        trail = Pill("Trail", area, style.pill, style.trail).rectTransform;
        fillImg = Pill("Fill", area, style.pill, style.ally); fill = fillImg.rectTransform;
        foreach (var r in new[] { trail, fill }) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        var shade = Pill("Shade", fill, style.pill, new Color(0f, 0f, 0f, 0.22f)).rectTransform;
        shade.anchorMin = Vector2.zero; shade.anchorMax = new Vector2(1f, 0.42f); shade.offsetMin = shade.offsetMax = Vector2.zero;
        var shine = Pill("Shine", fill, style.pill, new Color(1f, 1f, 1f, 0.42f)).rectTransform;
        shine.anchorMin = new Vector2(0f, 0.55f); shine.anchorMax = new Vector2(1f, 0.92f); shine.offsetMin = new Vector2(8f, 0f); shine.offsetMax = new Vector2(-8f, 0f);
        txtHp = Label("HP", bar, style, style.hpSize * 1.05f); Stretch(txtHp.rectTransform, 0f);

        // level badge on the left end of the bar
        txtLevel = Label("Level", canvasT, style, style.hpSize * 0.75f);
        var lr = txtLevel.rectTransform; lr.sizeDelta = new Vector2(160f, 70f); lr.anchoredPosition = new Vector2(0f, size.y * 0.5f + 34f);
        txtLevel.color = new Color(1f, 0.92f, 0.4f);
        ApplyRatio(1f, 1f);
    }

    void ApplyRatio(float f, float t)
    {
        if (fill == null) return;
        fill.anchorMax = new Vector2(Mathf.Clamp01(f), 1f);
        trail.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(f, t)), 1f);
        fill.gameObject.SetActive(f > 0.02f);
        trail.gameObject.SetActive(t > f + 0.005f);
    }

    void OnHealthChanged(int cur, int max)
    {
        float r = max > 0 ? cur / (float)max : 0f;
        if (r < target) trailWait = 0.35f;
        target = r;
        UpdateTexts();
    }

    void UpdateTexts()
    {
        if (txtHp != null && health != null) txtHp.text = health.currentHealth > 0 ? health.currentHealth.ToString() : "";
        if (txtLevel != null && entry != null) { lastLevel = entry.level; txtLevel.text = "Lv" + Mathf.Max(1, entry.level); }
    }

    void LateUpdate()
    {
        bool dead = health != null && health.death;
        Color c = TeamColor(out bool mine);

        if (disc != null)
        {
            disc.gameObject.SetActive(!dead);
            if (!dead)
            {
                float half = radius / RingUv;
                Vector3 p = transform.position;
                disc.SetPositionAndRotation(new Vector3(p.x, p.y + FootRing.HeightOffset, p.z), Quaternion.identity);
                disc.localScale = new Vector3(2f * half, 1f, 2f * half);
                c.a = FootRing.Alpha;
                mpb.SetColor(ColorId, c);
                mpb.SetFloat(FillId, FootRing.FillAlpha);
                mpb.SetFloat(CenterFillId, FootRing.CenterFill);
                mpb.SetFloat(FalloffId, FootRing.Falloff);
                mpb.SetFloat(RimAlphaId, FootRing.RimAlpha);
                mpb.SetFloat(RingRId, RingUv);
                mpb.SetFloat(WidthId, FootRing.Width / half);
                mpb.SetFloat(DashedId, 0f);
                mpb.SetFloat(DashCountId, FootRing.DashCount);
                mpb.SetFloat(ProgressId, -1f);
                mpb.SetFloat(GlowId, 0f);
                ringRend.SetPropertyBlock(mpb);
            }
        }

        if (rangeDisc != null)
        {
            bool show = !dead && sentryFov != null && entry.state == BuildableState.Idle;
            rangeDisc.gameObject.SetActive(show);
            if (show)
            {
                float half = sentryFov.radius / RingUv;
                Vector3 p = transform.position;
                rangeDisc.SetPositionAndRotation(new Vector3(p.x, p.y + 0.03f, p.z), Quaternion.identity);
                rangeDisc.localScale = new Vector3(2f * half, 1f, 2f * half);
                Color rc = c; rc.a = 0.35f;                 // faint: a hint of the danger zone, not a wall of colour
                mpb.SetColor(ColorId, rc);
                mpb.SetFloat(FillId, 0.12f);
                mpb.SetFloat(CenterFillId, 0f);
                mpb.SetFloat(FalloffId, 5f);                // only a faint band inside the edge
                mpb.SetFloat(RimAlphaId, 0.8f);
                mpb.SetFloat(RingRId, RingUv);
                mpb.SetFloat(WidthId, 0.07f / half);
                mpb.SetFloat(DashedId, 1f);
                mpb.SetFloat(DashCountId, 56f);
                mpb.SetFloat(ProgressId, -1f);
                mpb.SetFloat(GlowId, 0f);
                rangeRend.SetPropertyBlock(mpb);
            }
        }

        if (canvasT == null) return;
        canvasT.gameObject.SetActive(!dead);
        if (dead) return;
        var cam = Camera.main;
        canvasT.position = transform.position + Vector3.up * (top + 0.45f);
        if (cam != null) canvasT.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        c.a = 1f;
        if (fillImg != null) fillImg.color = c;
        if (entry != null && entry.level != lastLevel) UpdateTexts();

        float dt = Time.deltaTime;
        shown = Mathf.MoveTowards(shown, target, Mathf.Max(0.02f, Mathf.Abs(target - shown)) * 10f * dt);
        if (trailShown < shown) trailShown = shown;
        else if (trailWait > 0f) trailWait -= dt;
        else trailShown = Mathf.MoveTowards(trailShown, shown, 1.1f * dt);
        ApplyRatio(shown, trailShown);
    }
}
