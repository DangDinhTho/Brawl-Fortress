using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brawl-Stars-style team ring under a character's feet (added to every TF2Player / TF2Bot / TF2Robot in CreateCharacter,
/// next to FakeShadow and BushStealth). Colours are relative to the local viewer (ArenaManager.mainPlayer):
/// local player green, allies blue, enemies red. Drawn after FakeShadow.
/// Size: diameter = the larger side of the ground projection of the model's bounds (LOD0 skinned renderers, weapons excluded;
/// UseRendererBounds = false uses the posed mesh vertices instead), times RadiusScale, clamped to Min/MaxRadius.
/// Measured a few times after spawn (MeasureTimes), the largest value is kept so the ring does not breathe with animations.
///  - The disc is a child of the character, so BushStealth hides it together with an enemy hidden in a bush.
///  - Spy: enemies see the ring in the team colour the model is drawn with (TF2Player.MeshTeam, set by SetMeshTeam
///    = a disguise), and do not see it while the Spy is cloaked (TF2Player.MeshInvisible).
///  - Local player only: reload progress arc around the ring (Gun.ReloadProgress), glow while a Medic beam is on him
///    or Ubercharged is set, dashed ring while hidden in a bush (BushStealth.InBush).
/// Material: Resources/Materials/FootRing.mat (shader BrawlFortress/FootRing). Tuning values are the static fields below.
/// </summary>
public class FootRing : MonoBehaviour
{
    /// <summary>Ring centre-line position in the shader's uv space (must match _RingR on the material).</summary>
    const float RingUv = 0.72f;
    /// <summary>Ring width (m), alpha, inner fill alpha.</summary>
    public static float Width = 0.08f, Alpha = 0.9f, FillAlpha = 0.12f;
    /// <summary>Multiplier on the measured radius, limits (m), and the radius used until the model is measured.</summary>
    public static float RadiusScale = 1f, MinRadius = 0.35f, MaxRadius = 2f, DefaultRadius = 0.5f;
    /// <summary>true (default) = ground projection of the renderers' bounds (SkinnedMeshRenderer.bounds, Tho's choice, roomier);
    /// false = tight projection of the posed mesh vertices.</summary>
    public static bool UseRendererBounds = true;
    /// <summary>Number of measurements after spawn and the delay before the first / between them (s).</summary>
    public static int MeasureTimes = 4;
    public static float FirstMeasureDelay = 0.3f, MeasureInterval = 0.5f;
    public static Color SelfColor = new Color(0.30f, 1f, 0.35f);
    public static Color AllyColor = new Color(0.25f, 0.6f, 1f);
    public static Color EnemyColor = new Color(1f, 0.25f, 0.25f);
    public static Color ReloadColor = new Color(1f, 1f, 1f, 0.95f);
    public static Color HealGlowColor = new Color(0.45f, 1f, 0.55f);
    public static Color UberGlowColor = new Color(1f, 0.85f, 0.3f);
    /// <summary>Height above the feet (m); FakeShadow uses 0.03.</summary>
    public static float HeightOffset = 0.035f;
    public static int DashCount = 14;

    /// <summary>Set by an Übercharge feature (the game has none yet): strong pulsing glow on the local player's ring.</summary>
    public bool Ubercharged { get; set; }

    /// <summary>Current ring radius (m).</summary>
    public float Radius => radius;

    static Mesh quad;
    static Material material;
    static Mesh bakeMesh;
    static readonly List<SkinnedMeshRenderer> skins = new List<SkinnedMeshRenderer>();
    static readonly List<Vector3> verts = new List<Vector3>();
    static readonly HashSet<Renderer> lodSkip = new HashSet<Renderer>();
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int FillId = Shader.PropertyToID("_FillAlpha");
    static readonly int RingRId = Shader.PropertyToID("_RingR");
    static readonly int WidthId = Shader.PropertyToID("_Width");
    static readonly int DashedId = Shader.PropertyToID("_Dashed");
    static readonly int DashCountId = Shader.PropertyToID("_DashCount");
    static readonly int ProgressId = Shader.PropertyToID("_Progress");
    static readonly int ArcColorId = Shader.PropertyToID("_ArcColor");
    static readonly int GlowId = Shader.PropertyToID("_Glow");
    static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");

    static Medicgun[] medics = new Medicgun[0];
    static float nextMedicScan;

    TF2Player player;
    BushStealth stealth;
    Transform disc;
    MeshRenderer rend;
    MaterialPropertyBlock mpb;
    float visible = 1f, glow;
    float radius;
    int measures;
    bool measured;
    float nextMeasure;

    void Awake() { player = GetComponent<TF2Player>(); radius = DefaultRadius; }

    void Start()
    {
        stealth = GetComponent<BushStealth>();
        if (material == null) material = Resources.Load<Material>("Materials/FootRing");
        if (material == null) { enabled = false; return; }
        if (quad == null) quad = BuildQuad();
        var go = new GameObject("FootRing");
        go.layer = gameObject.layer;
        disc = go.transform;
        disc.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = quad;
        rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = material;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        mpb = new MaterialPropertyBlock();
        nextMeasure = Time.time + FirstMeasureDelay; // let the animator pose the body first
    }

    Vector3 Feet => player != null && player.remotePlayer != null ? player.remotePlayer.transform.position : transform.position;
    bool Dead => player == null || player.healthManager == null || player.healthManager.death;

    static TF2Player Viewer => ArenaManager.Instance != null ? ArenaManager.Instance.mainPlayer : null;

    /// <summary>Half of the larger side of the model's ground projection (m), in the character's facing frame. -1 if nothing to measure.</summary>
    float MeasureRadius()
    {
        if (player == null || player.remotePlayer == null) return -1f;
        Transform root = player.remotePlayer.transform;
        Quaternion inv = Quaternion.Inverse(Quaternion.Euler(0f, root.eulerAngles.y, 0f));
        Vector3 o = root.position;
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        bool any = false;
        if (bakeMesh == null) bakeMesh = new Mesh { name = "FootRingBake" };
        skins.Clear();
        lodSkip.Clear();
        var groups = root.GetComponentsInChildren<LODGroup>(false);
        for (int g = 0; g < groups.Length; g++)
        {
            var lods = groups[g].GetLODs();
            for (int l = 1; l < lods.Length; l++)
                foreach (var r in lods[l].renderers) if (r != null) lodSkip.Add(r);
        }
        root.GetComponentsInChildren(false, skins);
        for (int s = 0; s < skins.Count; s++)
        {
            var skin = skins[s];
            if (skin == null || !skin.enabled || skin.sharedMesh == null) continue;
            if (skin.GetComponentInParent<Weapon>() != null || lodSkip.Contains(skin)) continue; // weapons stick out; LOD1+ duplicate LOD0
            if (UseRendererBounds)
            {
                Bounds b = skin.bounds; // world AABB Unity keeps for the skinned renderer
                Vector3 bmin = b.min - o, bmax = b.max - o;
                if (bmin.x < minX) minX = bmin.x; if (bmax.x > maxX) maxX = bmax.x;
                if (bmin.z < minZ) minZ = bmin.z; if (bmax.z > maxZ) maxZ = bmax.z;
                any = true;
                continue;
            }
            skin.BakeMesh(bakeMesh, false); // posed vertices in world units relative to the renderer (its scale already applied: the FBX parts have scale 100), so only position + rotation below
            bakeMesh.GetVertices(verts);
            Transform t = skin.transform;
            Vector3 tp = t.position; Quaternion tr = t.rotation;
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 p = inv * (tp + tr * verts[i] - o);
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.z < minZ) minZ = p.z; if (p.z > maxZ) maxZ = p.z;
                any = true;
            }
        }
        if (!any) return -1f;
        return 0.5f * Mathf.Max(maxX - minX, maxZ - minZ);
    }

    void LateUpdate()
    {
        if (disc == null || player == null) return;

        if (measures < MeasureTimes && Time.time >= nextMeasure && !Dead)
        {
            measures++;
            nextMeasure = Time.time + MeasureInterval;
            float r = MeasureRadius();
            if (r > 0f)
            {
                r = Mathf.Clamp(r * RadiusScale, MinRadius, MaxRadius);
                radius = measured ? Mathf.Max(radius, r) : r;
                measured = true;
            }
        }

        var viewer = Viewer;
        bool self = viewer != null ? viewer == player : player.isMainPlayer;
        bool enemy = !self && viewer != null && player.team != viewer.team;

        bool show = !Dead && !(enemy && player.MeshInvisible);
        visible = Mathf.MoveTowards(visible, show ? 1f : 0f, Time.deltaTime * 6f);
        if (visible <= 0f) { if (rend.enabled) rend.enabled = false; return; }
        if (!rend.enabled) rend.enabled = true;

        float half = radius / RingUv; // quad half-size so the ring centre-line sits at 'radius'
        Vector3 feet = Feet;
        disc.position = new Vector3(feet.x, feet.y + HeightOffset, feet.z);
        disc.rotation = Quaternion.identity;
        var ps = transform.lossyScale;
        disc.localScale = new Vector3(2f * half / Mathf.Max(ps.x, 1e-4f), 1f / Mathf.Max(ps.y, 1e-4f), 2f * half / Mathf.Max(ps.z, 1e-4f));

        Color c;
        if (self) c = SelfColor;
        else if (viewer == null) c = AllyColor;
        else
        {
            Team shown = player.team;
            if (enemy && player.MeshTeam != Team.None) shown = player.MeshTeam; // disguise
            c = shown == viewer.team ? AllyColor : EnemyColor;
        }
        c.a = Alpha * visible;

        float progress = -1f, glowTarget = 0f;
        bool dashed = false, uber = false;
        if (self)
        {
            var gun = player.FPSPlayer != null ? player.FPSPlayer.currWeapon as Gun : null;
            if (gun != null) progress = gun.ReloadProgress;
            dashed = stealth != null && stealth.InBush;
            uber = Ubercharged;
            if (uber || IsHealed(player)) glowTarget = 1f;
        }
        glow = Mathf.MoveTowards(glow, glowTarget, Time.deltaTime * 4f);
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * (uber ? 9f : 5f));

        mpb.SetColor(ColorId, c);
        mpb.SetFloat(FillId, FillAlpha);
        mpb.SetFloat(RingRId, RingUv);
        mpb.SetFloat(WidthId, Width / half);
        mpb.SetFloat(DashedId, dashed ? 1f : 0f);
        mpb.SetFloat(DashCountId, DashCount);
        mpb.SetFloat(ProgressId, progress);
        var rc = ReloadColor; rc.a *= visible;
        mpb.SetColor(ArcColorId, rc);
        mpb.SetFloat(GlowId, glow * pulse * visible);
        mpb.SetColor(GlowColorId, uber ? UberGlowColor : HealGlowColor);
        rend.SetPropertyBlock(mpb);
    }

    /// <summary>True when another character's Medigun beam is on p (local, bot and remote medics).</summary>
    static bool IsHealed(TF2Player p)
    {
        if (Time.unscaledTime >= nextMedicScan)
        {
            nextMedicScan = Time.unscaledTime + 0.5f;
            medics = FindObjectsOfType<Medicgun>();
        }
        for (int i = 0; i < medics.Length; i++)
        {
            var m = medics[i];
            if (m == null || m.playerOwner == p || m.target == null || m.shotPoints == null || m.shotPoints.Length == 0 || m.shotPoints[0] == null) continue;
            if (m.target.parent == p.transform && m.shotPoints[0].gameObject.activeInHierarchy) return true;
        }
        return false;
    }

    static Mesh BuildQuad()
    {
        var m = new Mesh { name = "FootRingQuad" };
        m.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) };
        m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    void OnDestroy() { if (disc != null) Destroy(disc.gameObject); }
}
