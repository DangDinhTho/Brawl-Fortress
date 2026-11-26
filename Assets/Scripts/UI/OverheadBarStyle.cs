using TMPro;
using UnityEngine;

/// <summary>
/// Look of the overhead health bar, name and ammo bar (Brawl Stars style, OverheadBarView). Loaded from
/// Resources/UI/OverheadBarStyle. Sizes are in the character canvas's units (1 unit = 2 mm in the world).
/// </summary>
[CreateAssetMenu(menuName = "Brawl/Overhead Bar Style")]
public class OverheadBarStyle : ScriptableObject
{
    public TMP_FontAsset font;
    [Tooltip("Old pill sprite (the bar now draws its own rounded boxes, BrawlHudKit)")] public Sprite pill;

    [Tooltip("Scale of the whole overhead group (name, bar, ammo): Brawl Stars bars read from far away")]
    public float scale = 1.55f;

    [Header("Health bar (canvas units, before Scale)")]
    public Vector2 barSize = new Vector2(540f, 106f);
    [Tooltip("Thickness of the dark border")] public float outline = 11f;
    public float cornerRadius = 22f;
    [Tooltip("Name baseline above the top of the bar")] public float nameGap = 14f;
    public float tickWidth = 6f;
    public Color tick = new Color(0.03f, 0.02f, 0.05f, 0.55f);

    [Header("Colours")]
    public Color mine = new Color(0.45f, 0.93f, 0.16f);
    public Color ally = new Color(0.16f, 0.62f, 1f);
    public Color enemy = new Color(1f, 0.2f, 0.2f);
    public Color back = new Color(0.17f, 0.13f, 0.22f);
    public Color border = new Color(0.047f, 0.031f, 0.071f);
    public Color trail = new Color(1f, 1f, 1f, 0.92f);
    [Tooltip("How much the name is lightened from the team colour")] [Range(0f, 1f)] public float nameLighten = 0.3f;

    [Header("Text")]
    public float nameSize = 86f;
    public float hpSize = 72f;

    [Header("Ammo bar (local player)")]
    [Tooltip("Width as a share of the health bar's width")] public float ammoWidth = 0.9f;
    public float ammoHeight = 56f;
    [Tooltip("Gap under the health bar")] public float ammoGap = 10f;
    public float ammoSegmentGap = 10f;
    public float ammoOutline = 7f;
    public float ammoCornerRadius = 14f;
    public Color ammo = new Color(1f, 0.6f, 0.06f);
    public Color ammoReload = new Color(1f, 0.85f, 0.5f);

    static OverheadBarStyle cached;
    public static OverheadBarStyle Get()
    {
        if (cached == null) cached = Resources.Load<OverheadBarStyle>("UI/OverheadBarStyle");
        return cached;
    }
}
