using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Egypt maps: an ancient dart trap found by the dig. A carved pressure plate lies in a corridor; anyone stepping on it
/// (either team) arms it: the plate sinks and glows, a red stripe marks the dart path and, armTime later, a volley of
/// darts shoots out of the carved stele in the wall along the corridor: everyone in the path loses damageFraction of
/// max health (once per volley). Then it rests 'cooldown' seconds. Lure enemies onto it, or run through fast.
/// The master client decides when it fires (MapMechanics.TrapEvent {id, clock time of the volley}) so every client
/// shows the same volley; each client damages the characters it owns. Only while the match is running.
/// The object stands at the wall face, +Z along the dart path; the plate is plateDistance along it. Plate, stele and
/// darts are built at runtime from the map's palette material ('props'). Placed by BrawlMapBuilder.PlaceDartTraps.
/// </summary>
public class DartTrap : MonoBehaviour
{
    public int id;
    public Material props;
    public float length = 7.5f;
    public float plateDistance = 3f;
    public float plateRadius = 0.7f;
    public float armTime = 0.6f;
    public float dartSpeed = 15f;
    public float halfWidth = 0.65f;
    [Range(0f, 1f)] public float damageFraction = 0.25f;
    public float cooldown = 5f;

    static readonly List<DartTrap> all = new List<DartTrap>();
    static readonly int AlphaId = Shader.PropertyToID("_Alpha"), FillId = Shader.PropertyToID("_Fill");

    double fireAt = -1000;
    float nextScan;
    Transform plate;
    MeshRenderer stripe, plateGlow;
    readonly List<Transform> darts = new List<Transform>();
    readonly HashSet<int> hit = new HashSet<int>();
    MaterialPropertyBlock mpb;
    bool volleyFx;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        Part("DartStele", Stele(), Vector3.zero);
        plate = Part("DartPlate", Plate(), new Vector3(0f, 0f, plateDistance));
        stripe = MechMesh.Child(transform, "DartPath", MechMesh.Stripe(0.3f, length, halfWidth, new Color(1f, 0.22f, 0.12f, 0.7f)), MechMesh.Decal, 0.03f);
        stripe.enabled = false;
        plateGlow = MechMesh.Child(transform, "PlateGlow", MechMesh.SoftDisc(plateRadius * 1.5f, new Color(1f, 0.3f, 0.12f, 0.8f)), MechMesh.Decal, 0.09f);
        plateGlow.transform.localPosition = new Vector3(0f, 0.09f, plateDistance);
        plateGlow.enabled = false;
        var dm = Dart();
        float[] hy = { 0.62f, 1.0f, 1.36f }, hx = { -0.14f, 0.16f, -0.02f };
        for (int i = 0; i < 3; i++)
        {
            var d = Part("Dart" + i, dm, new Vector3(hx[i], hy[i], 0f));
            d.gameObject.SetActive(false);
            darts.Add(d);
        }
    }

    void OnEnable() { all.Add(this); MapMechanics.On(MapMechanics.TrapEvent, OnTrap); }
    void OnDisable() { all.Remove(this); }

    Transform Part(string name, Mesh m, Vector3 local)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = local;
        go.AddComponent<MeshFilter>().sharedMesh = m;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = props;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    void Update()
    {
        double now = MapMechanics.Clock;
        float s = (float)(now - fireAt);                 // < 0 armed, >= 0 volley
        bool armed = s >= -armTime && s < 0f;
        float flight = length / dartSpeed;
        bool volley = s >= 0f && s < flight + 0.1f;

        // plate: sinks while armed / just fired, rises back during the rest
        float sink = armed || volley ? 1f : Mathf.Clamp01(1f - (s - flight) / 0.6f);
        if (plate) plate.localPosition = new Vector3(0f, -0.035f * sink, plateDistance);
        if (stripe.enabled != (armed || volley)) stripe.enabled = armed || volley;
        if (plateGlow.enabled != armed) plateGlow.enabled = armed;
        if (armed || volley)
        {
            stripe.GetPropertyBlock(mpb);
            mpb.SetFloat(FillId, armed ? Mathf.Clamp01((s + armTime) / armTime) : 1f);
            mpb.SetFloat(AlphaId, armed ? 0.75f + 0.25f * Mathf.Sin(Time.time * 18f) : Mathf.Clamp01(1f - s / (flight + 0.1f)));
            stripe.SetPropertyBlock(mpb);
        }
        if (armed)
        {
            plateGlow.GetPropertyBlock(mpb);
            mpb.SetFloat(AlphaId, 0.6f + 0.4f * Mathf.Sin(Time.time * 18f));
            plateGlow.SetPropertyBlock(mpb);
        }

        // darts
        float front = s * dartSpeed;
        for (int i = 0; i < darts.Count; i++)
        {
            float z = front - i * 0.35f;
            bool show = volley && z > 0.2f && z < length;
            if (darts[i].gameObject.activeSelf != show) darts[i].gameObject.SetActive(show);
            if (show) { var lp = darts[i].localPosition; darts[i].localPosition = new Vector3(lp.x, lp.y, z); }
        }
        if (volley && !volleyFx) { volleyFx = true; BrawlFx.Burst(transform.TransformPoint(new Vector3(0f, 1f, 0.3f)), new Color(0.9f, 0.8f, 0.6f, 1f), new Color(1f, 0.6f, 0.3f, 1f), 8, 3f); }
        if (!volley) volleyFx = false;
        if (volley) Hurt(front);

        // the master client watches the plate
        if (!MapMechanics.Authority || !MapMechanics.Running || s < flight + cooldown || Time.time < nextScan) return;
        nextScan = Time.time + 0.1f;
        Vector3 pc = transform.TransformPoint(new Vector3(0f, 0f, plateDistance));
        foreach (var p in MapMechanics.Characters())
        {
            if (!MapMechanics.Alive(p)) continue;
            Vector3 d = p.transform.position - pc; float up = d.y; d.y = 0f;
            if (up < -0.8f || up > 1.2f || d.sqrMagnitude > plateRadius * plateRadius) continue;
            MapMechanics.Send(MapMechanics.TrapEvent, new object[] { id, now + armTime });
            break;
        }
    }

    void Hurt(float front)
    {
        if (!MapMechanics.Running) return;
        foreach (var p in MapMechanics.Characters())
        {
            if (p.photonView == null || !p.photonView.IsMine || !MapMechanics.Alive(p)) continue;
            if (hit.Contains(p.photonView.ViewID)) continue;
            Vector3 l = transform.InverseTransformPoint(p.transform.position);
            if (Mathf.Abs(l.x) > halfWidth || l.y < -0.8f || l.y > 1.5f || l.z < 0f || l.z > length) continue;
            if (l.z > front + 0.3f || l.z < front - 1.2f) continue;   // the darts reach it now
            hit.Add(p.photonView.ViewID);
            MapMechanics.Hurt(p.healthManager, damageFraction, transform.forward * 2f + Vector3.up);
        }
    }

    static void OnTrap(object[] d)
    {
        if (d == null || d.Length < 2) return;
        int tid = (int)d[0];
        for (int i = 0; i < all.Count; i++)
            if (all[i].id == tid) { all[i].fireAt = (double)d[1]; all[i].hit.Clear(); }
    }

    // ------------------------------------------------------------------ meshes (palette cells of Tools/IceArt/common.py)
    const int PSand = 48, PSandDark = 49, PLapis = 51, PTurq = 52, PGold = 38, PBlack = 19, PWoodDark = 11, PMetal = 20, PRed = 16;
    static Vector2 Cell(int i) => new Vector2((i % 8 + 0.5f) / 8f, 1f - (i / 8 + 0.5f) / 8f);

    class Builder
    {
        public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int cell, Vector3 hint)
        {
            var nr = Vector3.Cross(b - a, d - a).normalized;
            if (Vector3.Dot(nr, hint) < 0) { var tmp = b; b = d; d = tmp; nr = -nr; }
            int i = v.Count; var u = Cell(cell);
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int k = 0; k < 4; k++) { n.Add(nr); uv.Add(u); }
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }
        /// <summary>axis-aligned box from min to max; top / side cells</summary>
        public void Box(Vector3 mn, Vector3 mx, int top, int side, int front = -1)
        {
            Vector3 P(float x, float y, float z) => new Vector3(x, y, z);
            Quad(P(mn.x, mx.y, mn.z), P(mx.x, mx.y, mn.z), P(mx.x, mx.y, mx.z), P(mn.x, mx.y, mx.z), top, Vector3.up);
            Quad(P(mx.x, mn.y, mn.z), P(mx.x, mn.y, mx.z), P(mx.x, mx.y, mx.z), P(mx.x, mx.y, mn.z), side, Vector3.right);
            Quad(P(mn.x, mn.y, mn.z), P(mn.x, mn.y, mx.z), P(mn.x, mx.y, mx.z), P(mn.x, mx.y, mn.z), side, Vector3.left);
            Quad(P(mn.x, mn.y, mx.z), P(mx.x, mn.y, mx.z), P(mx.x, mx.y, mx.z), P(mn.x, mx.y, mx.z), front >= 0 ? front : side, Vector3.forward);
            Quad(P(mn.x, mn.y, mn.z), P(mx.x, mn.y, mn.z), P(mx.x, mx.y, mn.z), P(mn.x, mx.y, mn.z), side, Vector3.back);
        }
        public Mesh Mesh(string name)
        {
            var m = new Mesh { name = name };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>Carved stele set in the wall: sandstone, a gold band, a lapis cap, three dark dart holes facing +Z.</summary>
    static Mesh Stele()
    {
        var b = new Builder();
        b.Box(new Vector3(-0.38f, 0f, -0.35f), new Vector3(0.38f, 1.62f, 0.06f), PSand, PSandDark, PSand);
        b.Box(new Vector3(-0.42f, 1.62f, -0.38f), new Vector3(0.42f, 1.74f, 0.1f), PLapis, PLapis);
        b.Box(new Vector3(-0.4f, 1.18f, -0.36f), new Vector3(0.4f, 1.26f, 0.075f), PGold, PGold);
        b.Box(new Vector3(-0.4f, 0.32f, -0.36f), new Vector3(0.4f, 0.4f, 0.075f), PTurq, PTurq);
        float[] hy = { 0.62f, 1.0f, 1.36f }, hx = { -0.14f, 0.16f, -0.02f };
        for (int i = 0; i < 3; i++) b.Box(new Vector3(hx[i] - 0.07f, hy[i] - 0.07f, 0.06f), new Vector3(hx[i] + 0.07f, hy[i] + 0.07f, 0.075f), PBlack, PBlack);
        return b.Mesh("DartStele");
    }

    /// <summary>Pressure plate: a sandstone slab with a gold frame and a lapis / turquoise glyph, its top 6 cm up.</summary>
    static Mesh Plate()
    {
        var b = new Builder();
        b.Box(new Vector3(-0.62f, 0f, -0.62f), new Vector3(0.62f, 0.05f, 0.62f), PSandDark, PSandDark);
        b.Box(new Vector3(-0.54f, 0.05f, -0.54f), new Vector3(0.54f, 0.06f, 0.54f), PGold, PGold);
        b.Box(new Vector3(-0.48f, 0.06f, -0.48f), new Vector3(0.48f, 0.065f, 0.48f), PSand, PSand);
        b.Box(new Vector3(-0.2f, 0.065f, -0.2f), new Vector3(0.2f, 0.07f, 0.2f), PLapis, PLapis);
        b.Box(new Vector3(-0.07f, 0.07f, -0.34f), new Vector3(0.07f, 0.074f, 0.34f), PTurq, PTurq);
        b.Box(new Vector3(-0.34f, 0.07f, -0.07f), new Vector3(0.34f, 0.074f, 0.07f), PTurq, PTurq);
        return b.Mesh("DartPlate");
    }

    /// <summary>A dart flying along +Z: dark wooden shaft, metal tip in front, red fletching behind.</summary>
    static Mesh Dart()
    {
        var b = new Builder();
        b.Box(new Vector3(-0.025f, -0.025f, -0.4f), new Vector3(0.025f, 0.025f, 0.08f), PWoodDark, PWoodDark);
        b.Box(new Vector3(-0.04f, -0.04f, 0.08f), new Vector3(0.04f, 0.04f, 0.2f), PMetal, PMetal);
        b.Box(new Vector3(-0.1f, -0.008f, -0.42f), new Vector3(0.1f, 0.008f, -0.28f), PRed, PRed);
        b.Box(new Vector3(-0.008f, -0.1f, -0.42f), new Vector3(0.008f, 0.1f, -0.28f), PRed, PRed);
        return b.Mesh("Dart");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(new Vector3(0f, 1f, length * 0.5f), new Vector3(halfWidth * 2f, 2f, length));
        Gizmos.DrawWireSphere(new Vector3(0f, 0f, plateDistance), plateRadius);
    }
#endif
}
