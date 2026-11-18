using TMPro;
using UnityEngine;

/// <summary>
/// Look of the overhead health bar and name (Brawl Stars style). Loaded from Resources/UI/OverheadBarStyle.
/// </summary>
[CreateAssetMenu(menuName = "Brawl/Overhead Bar Style")]
public class OverheadBarStyle : ScriptableObject
{
    public TMP_FontAsset font;
    [Tooltip("White rounded pill, 9-sliced")] public Sprite pill;

    [Header("Bar size (canvas units)")]
    public Vector2 barSize = new Vector2(440f, 58f);
    public float outline = 7f;
    public float nameGap = 40f;

    [Header("Colours")]
    public Color mine = new Color(0.42f, 1f, 0.23f);
    public Color ally = new Color(0.21f, 0.71f, 1f);
    public Color enemy = new Color(1f, 0.24f, 0.24f);
    public Color back = new Color(0.16f, 0.12f, 0.2f);
    public Color border = new Color(0.05f, 0.04f, 0.07f);
    public Color trail = new Color(1f, 1f, 1f, 0.9f);

    [Header("Text")]
    public float nameSize = 76f;
    public float hpSize = 44f;

    static OverheadBarStyle cached;
    public static OverheadBarStyle Get()
    {
        if (cached == null) cached = Resources.Load<OverheadBarStyle>("UI/OverheadBarStyle");
        return cached;
    }
}
