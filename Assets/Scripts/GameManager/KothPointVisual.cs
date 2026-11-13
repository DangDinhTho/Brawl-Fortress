using UnityEngine;

/// <summary>
/// Visuals of the King Of The Hill control point (same style as the CTF flag highlight):
/// the disc is tinted with the owner team colour (KingOfTheHillManager.teamOwnerColor),
/// a soft additive ring + light beam glow in that colour and pulse.
/// When nobody owns the point the softer <see cref="neutralColor"/> is used instead of pure white.
/// </summary>
public class KothPointVisual : MonoBehaviour
{
    [Tooltip("Opaque disc renderers (FlatKit) tinted with _Color/_ColorDim.")]
    public Renderer[] renderers;
    [Tooltip("Additive glow renderers (ring, beam) tinted with _TintColor.")]
    public Renderer[] glowRenderers;
    public Transform ring;
    public Transform beam;

    public Color neutralColor = new Color(0.62f, 0.6f, 0.75f, 1f);
    [Range(0f, 1.5f)] public float discBrightness = 0.8f;
    [Range(0f, 1f)] public float glowAlpha = 0.6f;
    public float ringPulse = 0.06f;
    public float pulseSpeed = 2.5f;

    private MaterialPropertyBlock block;
    private Color glowColor = Color.white;
    private Vector3 ringScale;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int ColorDimId = Shader.PropertyToID("_ColorDim");
    private static readonly int OutlineId = Shader.PropertyToID("_OutlineColor");
    private static readonly int TintId = Shader.PropertyToID("_TintColor");

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        if (ring) ringScale = ring.localScale;
    }

    private void Start()
    {
        var mgr = KingOfTheHillManager.instan;
        if (mgr == null) { Apply(Team.None); return; }
        mgr.OnTeamOwnerChanged += Apply;
        Apply(mgr.teamOwner);
    }

    private void OnDestroy()
    {
        if (KingOfTheHillManager.instan != null) KingOfTheHillManager.instan.OnTeamOwnerChanged -= Apply;
    }

    private Color TeamColor(Team team)
    {
        if (team == Team.None) return neutralColor;
        var mgr = KingOfTheHillManager.instan;
        int i = (int)team;
        if (mgr != null && mgr.teamOwnerColor != null && i >= 0 && i < mgr.teamOwnerColor.Length) return mgr.teamOwnerColor[i];
        return neutralColor;
    }

    private void Apply(Team team)
    {
        Color c = TeamColor(team);
        Color disc = c * discBrightness; disc.a = 1f;
        Color dim = c * (discBrightness * 0.62f); dim.a = 1f;
        Color outline = Color.Lerp(c, Color.white, 0.45f); outline.a = 1f;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(ColorId, disc);
            block.SetColor(ColorDimId, dim);
            block.SetColor(OutlineId, outline);
            r.SetPropertyBlock(block);
        }
        glowColor = c; glowColor.a = glowAlpha;
    }

    private void LateUpdate()
    {
        float t = Time.time;
        if (ring) ring.localScale = ringScale * (1f + ringPulse * Mathf.Sin(t * pulseSpeed));
        if (beam)
        {
            Camera cam = Camera.main;
            if (cam)
            {
                Vector3 d = cam.transform.position - beam.position;
                d.y = 0;
                if (d.sqrMagnitude > 0.001f) beam.rotation = Quaternion.LookRotation(-d);
            }
        }
        if (glowRenderers == null) return;
        Color g = glowColor;
        g.a *= 0.8f + 0.2f * Mathf.Sin(t * pulseSpeed);
        for (int i = 0; i < glowRenderers.Length; i++)
        {
            var r = glowRenderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(TintId, g);
            r.SetPropertyBlock(block);
        }
    }
}
