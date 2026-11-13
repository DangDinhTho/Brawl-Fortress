using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brawl Stars style aim indicator, added only to the locally controlled player (TF2Player.CreateCharacter).
/// Shows a flat shape on the ground pointing along the aim joystick while aiming:
///  a hollow fan (annular sector) starting just outside the player's body.
/// Length = weaponConfig.range, width = weaponConfig.aimAngle, i.e. exactly where the shots can go.
/// Turns red when the clip is empty.
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

    TF2Player player;
    GameObject go;
    Mesh mesh;
    Material mat;
    float builtRange = -1f, builtAngle = -1f;
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
    }

    void OnDestroy()
    {
        if (go != null) Destroy(go);
        if (mesh != null) Destroy(mesh);
        if (mat != null) Destroy(mat);
    }

    void LateUpdate()
    {
        if (go == null) return;
        var ctrl = player != null ? player.FPSPlayer : null;
        var weapon = ctrl != null ? ctrl.currWeapon : null;
        bool show = weapon != null && weapon.weaponConfig != null && weapon.weaponConfig.range > 0f
                    && ctrl.isActiveAndEnabled && ctrl.IsAiming && !IsDead(weapon);
        if (!show) { if (go.activeSelf) go.SetActive(false); return; }

        var cfg = weapon.weaponConfig;
        if (!Mathf.Approximately(cfg.range, builtRange) || !Mathf.Approximately(cfg.aimAngle, builtAngle))
            Build(cfg.range, cfg.aimAngle);

        Vector3 dir = ctrl.AimWorldDirection; dir.y = 0f;
        if (dir.sqrMagnitude > 1e-4f) lastDir = dir.normalized;
        Vector3 pos = ctrl.transform.position; pos.y += 0.06f;
        go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(lastDir));

        var gun = weapon as Gun;
        mat.SetColor("_Tint", gun != null && gun.bulletInClip <= 0 ? EmptyTint : ReadyTint);
        if (!go.activeSelf) go.SetActive(true);
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

    void Build(float range, float angle)
    {
        builtRange = range; builtAngle = angle;
        v.Clear(); c.Clear(); t.Clear();
        float half = Mathf.Clamp(angle, 1f, 359f) * 0.5f;
        BuildSector(Mathf.Min(InnerRadius, range * 0.5f), range, half);
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
