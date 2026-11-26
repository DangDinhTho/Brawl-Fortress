using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What a torn grass clump leaves behind (BushArea.Shatter), like Brawl Stars: a few tufts of stalks cut off low.
/// TuftsMin..TuftsMax tufts over the clump's footprint, each StalksMin..StalksMax thin stalks packed close together (the middle
/// ones taller, all leaning a little outwards, like a cut bunch of grass). Each stalk is a five-sided prism (8-22 cm)
/// with a flat cut top in a lighter colour. (Tho: not one squashed clump: a cabbage; not single stalks spread evenly:
/// tufts.) Drawn with the clump's own material: the stalks take the palette uvs of the root of that grass model's
/// leaves and the cut tops a slightly higher (lighter) uv (BushLeafColors.stubbleUV, baked by Tools/Bush/Bake Leaf
/// Colours), so they keep the theme's colours and the night / desert tints. Variant meshes per grass model, cached.
/// </summary>
public static class BushStubble
{
    public static int TuftsMin = 2, TuftsMax = 3, StalksMin = 8, StalksMax = 11;
    public static float MinHeight = 0.08f, MaxHeight = 0.22f, MinRadius = 0.02f, MaxRadius = 0.03f;
    /// <summary>Radius (m) of one tuft, and how far apart tufts stay.</summary>
    public static float TuftRadius = 0.11f, TuftSpacing = 0.26f;
    /// <summary>Footprint radius the meshes are built for (m); scaled to each clump's size.</summary>
    const float BaseFootprint = 0.45f;
    const int Variants = 4, Sides = 5;

    static readonly Dictionary<string, Mesh[]> cache = new Dictionary<string, Mesh[]>();

    public static GameObject Create(Renderer clump, Transform parent)
    {
        if (clump == null) return null;
        string name = BushLeafColors.GrassName(clump);
        if (!BushLeafColors.TryGetStubbleUV(name, out Vector2 stem, out Vector2 cut)) { stem = cut = new Vector2(0.5f, 0.5f); }
        Vector3 c = clump.bounds.center;
        int seed = Mathf.Abs(Mathf.RoundToInt(c.x * 73.1f) * 31 + Mathf.RoundToInt(c.z * 91.7f));
        Mesh mesh = Get(name ?? "", stem, cut, seed % Variants);
        var go = new GameObject("Stubble");
        go.layer = clump.gameObject.layer;
        go.transform.SetParent(parent, true);
        float r = Mathf.Clamp(0.5f * (clump.bounds.size.x + clump.bounds.size.z) * 0.5f * 0.8f, 0.2f, 0.9f);
        float k = r / BaseFootprint;
        go.transform.SetPositionAndRotation(new Vector3(c.x, clump.transform.position.y, c.z), Quaternion.Euler(0f, (seed * 37) % 360, 0f));
        Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
        go.transform.localScale = new Vector3(k / ps.x, 1f / ps.y, k / ps.z);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        var mats = clump.sharedMaterials;
        mr.sharedMaterial = mats.Length > 0 ? mats[0] : null;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    static Mesh Get(string name, Vector2 stem, Vector2 cut, int variant)
    {
        if (!cache.TryGetValue(name, out var arr)) { arr = new Mesh[Variants]; cache[name] = arr; }
        if (arr[variant] == null) arr[variant] = Build(stem, cut, 1000 + variant * 7919 + name.GetHashCode() % 1000);
        return arr[variant];
    }

    static Mesh Build(Vector2 stem, Vector2 cut, int seed)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        Vector2 mid = Vector2.Lerp(stem, cut, 0.35f);
        // tuft centres over the footprint, apart from each other
        var tufts = new List<Vector2>();
        int tuftCount = rnd.Next(TuftsMin, TuftsMax + 1);
        for (int tries = 0; tufts.Count < tuftCount && tries < 100; tries++)
        {
            float a = R(0f, Mathf.PI * 2f), d = Mathf.Sqrt(R(0f, 1f)) * (BaseFootprint - TuftRadius);
            var p = new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
            bool near = false; foreach (var q in tufts) if ((q - p).sqrMagnitude < TuftSpacing * TuftSpacing) { near = true; break; }
            if (!near) tufts.Add(p);
        }
        var stalks = new List<(Vector2 pos, Vector2 centre)>();
        foreach (var tc in tufts)
        {
            int count = rnd.Next(StalksMin, StalksMax + 1);
            var inTuft = new List<Vector2>();
            for (int tries = 0; inTuft.Count < count && tries < 80; tries++)
            {
                float a = R(0f, Mathf.PI * 2f), d = Mathf.Sqrt(R(0f, 1f)) * TuftRadius;
                var p = tc + new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
                bool near = false; foreach (var q in inTuft) if ((q - p).sqrMagnitude < 0.04f * 0.04f) { near = true; break; }
                if (!near) { inTuft.Add(p); stalks.Add((p, tc)); }
            }
        }
        foreach (var (p, tc) in stalks)
        {
            // taller in the middle of the tuft, shorter and leaning out at its edge
            Vector2 off = p - tc;
            float edge = Mathf.Clamp01(off.magnitude / TuftRadius);
            float h = Mathf.Lerp(MaxHeight, MinHeight, edge * 0.8f) * R(0.8f, 1.1f), rad = R(MinRadius, MaxRadius);
            Vector3 b0 = new Vector3(p.x, -0.02f, p.y);
            Vector2 outDir = off.sqrMagnitude > 1e-6f ? off.normalized : new Vector2(R(-1f, 1f), R(-1f, 1f)).normalized;
            Vector3 lean = new Vector3(outDir.x, 0f, outDir.y) * (0.008f + 0.025f * edge) + new Vector3(R(-0.006f, 0.006f), 0f, R(-0.006f, 0.006f));
            Vector3 t0 = new Vector3(p.x, h, p.y) + lean;
            float spin = R(0f, Mathf.PI * 2f);
            // sides: a bottom and a top ring, smooth normals
            int baseIdx = v.Count;
            for (int i = 0; i < Sides; i++)
            {
                float ang = spin + i * Mathf.PI * 2f / Sides;
                var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                v.Add(b0 + dir * rad * 1.15f); n.Add(dir); uv.Add(stem);
                v.Add(t0 + dir * rad); n.Add(dir); uv.Add(mid);
            }
            for (int i = 0; i < Sides; i++)
            {
                int j = (i + 1) % Sides;
                int bi = baseIdx + i * 2, ti = bi + 1, bj = baseIdx + j * 2, tj = bj + 1;
                Vector3 outward = (v[bi] + v[bj]) * 0.5f - new Vector3(b0.x, (v[bi].y + v[bj].y) * 0.5f, b0.z);
                AddTri(v, tri, bi, bj, tj, outward);
                AddTri(v, tri, bi, tj, ti, outward);
            }
            // flat cut top, slanted a little, lighter
            int capC = v.Count;
            Vector3 slant = new Vector3(R(-0.012f, 0.012f), 0f, R(-0.012f, 0.012f));
            v.Add(t0 + Vector3.up * 0.004f); n.Add(Vector3.up); uv.Add(cut);
            for (int i = 0; i < Sides; i++)
            {
                float ang = spin + i * Mathf.PI * 2f / Sides;
                var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                v.Add(t0 + dir * rad + Vector3.up * Vector3.Dot(dir, slant) * 10f * rad); n.Add(Vector3.up); uv.Add(cut);
            }
            for (int i = 0; i < Sides; i++) AddTri(v, tri, capC, capC + 1 + i, capC + 1 + (i + 1) % Sides, Vector3.up);
        }
        var m = new Mesh { name = "Stubble" };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Adds a triangle wound so it faces 'outward' (Unity: clockwise seen from the front).</summary>
    static void AddTri(List<Vector3> v, List<int> tri, int a, int b, int c, Vector3 outward)
    {
        Vector3 nrm = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (Vector3.Dot(nrm, outward) < 0f) { int t = b; b = c; c = t; }
        tri.Add(a); tri.Add(b); tri.Add(c);
    }
}
