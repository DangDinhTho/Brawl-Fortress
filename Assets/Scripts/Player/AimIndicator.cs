using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brawl Stars style aim indicator, added only to the locally controlled player (TF2Player.CreateCharacter).
/// Shows a flat shape on the ground pointing along the aim joystick while aiming:
///  a hollow fan (annular sector) starting just outside the player's body.
/// Length = weaponConfig.range, width = weaponConfig.aimAngle, i.e. exactly where the shots can go.
/// Flamethrower: from its nozzle to nozzle + range (the flame does not reach the area next to the body).
/// Turns red when the clip is empty.
/// Also flashes toward an auto-aimed tap shot and stays on while a hold weapon fires at the auto target
/// (PlayerFPSController.IndicatorVisible), with a pulsing ring under the target (red: enemy, green: Medigun teammate).
/// </summary>
public class AimIndicator : MonoBehaviour
{
    public const float InnerRadius = 0.7f;    // m, hollow centre around the player
    public const float Border = 0.09f;        // m
    public const int ArcSteps = 28;
    static readonly Color FillColor = new Color(1f, 1f, 1f, 0.1f);
    static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.35f);
    static readonly Color ReadyTint = Color.white;
    static readonly Color EmptyTint = new Color(1f, 0.35f, 0.3f, 1f);

    public const float RingInner = 0.55f, RingOuter = 0.78f;   // m, ring under the auto-aim target
    static readonly Color EnemyRing = new Color(1f, 0.32f, 0.25f, 0.85f);
    static readonly Color FriendRing = new Color(0.35f, 1f, 0.45f, 0.85f);

    TF2Player player;
    GameObject go;
    Mesh mesh;
    Material mat;
    GameObject ringGo;
    Mesh ringMesh;
    Material ringMat;
    float builtRange = -1f, builtAngle = -1f, builtInner = -1f;
    Vector3 lastDir = Vector3.forward;

    void Start()
    {
        player = GetComponent<TF2Player>();
        // material lives in Resources so the shader is included in builds
        var src = Resources.Load<Material>("Materials/AimIndicator");
        if (src == null) { enabled = false; return; }
        mat = new Material(src);
        mesh = new Mesh { name = "AimIndicator" };
        mesh.MarkDynamic();
        go = new GameObject("AimIndicator");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        go.SetActive(false);

        // ring under the auto-aim target: own material with its own stencil ref, so the fan does not hide it
        ringMat = new Material(src);
        ringMat.SetFloat("_StencilRef", 78f);
        ringMat.renderQueue = src.renderQueue + 1;
        ringMesh = BuildRing();
        ringGo = new GameObject("AutoAimRing");
        ringGo.AddComponent<MeshFilter>().sharedMesh = ringMesh;
        var rr = ringGo.AddComponent<MeshRenderer>();
        rr.sharedMaterial = ringMat;
        rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rr.receiveShadows = false;
        ringGo.SetActive(false);
    }

    void OnDestroy()
    {
        if (go != null) Destroy(go);
        if (mesh != null) Destroy(mesh);
        if (mat != null) Destroy(mat);
        if (ringGo != null) Destroy(ringGo);
        if (ringMesh != null) Destroy(ringMesh);
        if (ringMat != null) Destroy(ringMat);
    }

    void LateUpdate()
    {
        if (go == null) return;
        var ctrl = player != null ? player.FPSPlayer : null;
        var weapon = ctrl != null ? ctrl.currWeapon : null;
        UpdateRing(ctrl, weapon);
        Vector3 dir = Vector3.zero;
        bool show = weapon != null && weapon.weaponConfig != null && weapon.weaponConfig.range > 0f
                    && ctrl.isActiveAndEnabled && ctrl.IndicatorVisible(out dir) && !IsDead(weapon);
        // the medigun locked on a teammate: the range fan is not needed any more
        if (show && weapon is Medicgun medigun && medigun.HasTarget) show = false;
        if (!show) { if (go.activeSelf) go.SetActive(false); return; }

        var cfg = weapon.weaponConfig;
        dir.y = 0f;
        if (dir.sqrMagnitude > 1e-4f) lastDir = dir.normalized;

        float inner = Mathf.Min(InnerRadius, cfg.range * 0.5f), outer = cfg.range;
        // Flamethrower: the flame (and its damage) starts at the nozzle, far in front of the body, and is 'range' long.
        // The fan covers exactly that: from the nozzle to the end of the flame (it used to start at the body).
        if (weapon is Flamethrower ft && ft.shotPoints != null && ft.shotPoints.Length > 0 && ft.shotPoints[0] != null)
        {
            Vector3 rel = ft.shotPoints[0].position - ctrl.transform.position; rel.y = 0f;
            float ahead = Mathf.Round(Mathf.Max(0f, Vector3.Dot(rel, lastDir)) * 20f) / 20f;   // 5 cm steps: no rebuild every frame
            inner = Mathf.Max(inner, ahead);
            outer = ahead + cfg.range;
        }
        if (!Mathf.Approximately(outer, builtRange) || !Mathf.Approximately(cfg.aimAngle, builtAngle) || !Mathf.Approximately(inner, builtInner))
            Build(inner, outer, cfg.aimAngle);
        Vector3 pos = ctrl.transform.position; pos.y += 0.06f;
        go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(lastDir));

        var gun = weapon as Gun;
        mat.SetColor("_Tint", gun != null && gun.bulletInClip <= 0 ? EmptyTint : ReadyTint);
        if (!go.activeSelf) go.SetActive(true);
    }

    void UpdateRing(PlayerFPSController ctrl, Weapon weapon)
    {
        if (ringGo == null) return;
        var target = ctrl != null && ctrl.isActiveAndEnabled && weapon != null && !IsDead(weapon) ? ctrl.AutoTarget : null;
        if (target == null) { if (ringGo.activeSelf) ringGo.SetActive(false); return; }
        Vector3 p = target.transform.position; p.y += 0.07f;
        float size = Mathf.Clamp(target.transform.lossyScale.x, 1f, 2.5f) * (1f + 0.07f * Mathf.Sin(Time.time * 12f));
        ringGo.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, Time.time * 90f, 0f));
        ringGo.transform.localScale = new Vector3(size, 1f, size);
        ringMat.SetColor("_Tint", target.team == player.team ? FriendRing : EnemyRing);
        if (!ringGo.activeSelf) ringGo.SetActive(true);
    }

    /// <summary>Flat ring (white, tinted by the material) with 4 gaps, so it reads as a target mark and its turn shows.</summary>
    static Mesh BuildRing()
    {
        var verts = new List<Vector3>(); var cols = new List<Color>(); var tris = new List<int>();
        const int segs = 48;
        for (int i = 0; i < segs; i++)
        {
            if (i % 12 >= 10) continue;   // gap
            float a0 = i / (float)segs * Mathf.PI * 2f, a1 = (i + 1) / (float)segs * Mathf.PI * 2f;
            Vector3 d0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)), d1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1));
            int b = verts.Count;
            verts.Add(d0 * RingInner); verts.Add(d0 * RingOuter); verts.Add(d1 * RingOuter); verts.Add(d1 * RingInner);
            for (int k = 0; k < 4; k++) cols.Add(Color.white);
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2); tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
        }
        var m = new Mesh { name = "AutoAimRing" };
        m.SetVertices(verts); m.SetColors(cols); m.SetTriangles(tris, 0); m.RecalculateBounds();
        return m;
    }

    static bool IsDead(Weapon w)
    {
        var o = w.playerOwner;
        return o == null || o.healthManager == null || o.healthManager.death;
    }

    // ---------------------------------------------------------------- mesh

    readonly List<Vector3> v = new List<Vector3>();
    readonly List<Color> c = new List<Color>();
    readonly List<int> t = new List<int>();

    void Build(float inner, float range, float angle)
    {
        builtRange = range; builtAngle = angle; builtInner = inner;
        v.Clear(); c.Clear(); t.Clear();
        float half = Mathf.Clamp(angle, 1f, 359f) * 0.5f;
        BuildSector(inner, range, half);
        mesh.Clear();
        mesh.SetVertices(v);
        mesh.SetColors(c);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateBounds();
    }

    int V(Vector3 p, Color col) { v.Add(p); c.Add(col); return v.Count - 1; }
    void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Color col)   // a-b-c-d around
    {
        int i0 = V(a, col), i1 = V(b, col), i2 = V(cc, col), i3 = V(d, col);
        t.Add(i0); t.Add(i1); t.Add(i2); t.Add(i0); t.Add(i2); t.Add(i3);
    }

    // Borders are emitted first: with the stencil, the fill never draws over them.
    void BuildSector(float r0, float r, float half)
    {
        int steps = Mathf.Max(6, Mathf.CeilToInt(ArcSteps * half / 60f));
        float b = Border;
        // outer and inner arc borders
        for (int i = 0; i < steps; i++)
        {
            Vector3 d0 = Dir(Mathf.Lerp(-half, half, i / (float)steps)), d1 = Dir(Mathf.Lerp(-half, half, (i + 1) / (float)steps));
            Quad(d0 * (r - b), d0 * r, d1 * r, d1 * (r - b), BorderColor);
            Quad(d0 * r0, d0 * (r0 + b), d1 * (r0 + b), d1 * r0, BorderColor);
        }
        // side borders (skipped for a full circle)
        if (half < 179f)
        {
            foreach (float a in new[] { -half, half })
            {
                Vector3 d = Dir(a), n = Vector3.Cross(Vector3.up, d) * (a < 0 ? 1f : -1f) * b; // inward
                Quad(d * r0, d * r, d * r + n, d * r0 + n, BorderColor);
            }
        }
        // fill: ring strip between r0 and r
        for (int i = 0; i < steps; i++)
        {
            Vector3 d0 = Dir(Mathf.Lerp(-half, half, i / (float)steps)), d1 = Dir(Mathf.Lerp(-half, half, (i + 1) / (float)steps));
            Quad(d0 * r0, d0 * r, d1 * r, d1 * r0, FillColor);
        }
    }

    static Vector3 Dir(float deg) { float a = deg * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)); }
}
