using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flat meshes for the map mechanics' ground marks, built at runtime (no assets, nothing for the static batcher):
/// rings, discs and stripes drawn with BrawlFortress/MechDecal (vertex colour x _Color; uv.x = 0..1 around a ring or
/// along a stripe, which _Fill reveals), and irregular patches (BrawlFortress/SlickIce: vertex alpha 1 inside, 0 at
/// the rim). All lie in the XZ plane of their object, facing up.
/// </summary>
public static class MechMesh
{
    static Material decal, ice;
    public static Material Decal => decal != null ? decal : decal = Resources.Load<Material>("Materials/MechDecal");
    public static Material Ice => ice != null ? ice : ice = Resources.Load<Material>("Materials/SlickIce");

    /// <summary>A ring between radii r0 and r1, uv.x = angle 0..1 (clockwise from +Z seen from above).</summary>
    public static Mesh Ring(float r0, float r1, Color c, int seg = 48)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var t = new List<int>();
        for (int i = 0; i <= seg; i++)
        {
            float u = i / (float)seg, a = u * Mathf.PI * 2f;
            var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            v.Add(d * r0); v.Add(d * r1);
            uv.Add(new Vector2(u, 0f)); uv.Add(new Vector2(u, 1f));
            col.Add(c); col.Add(c);
            if (i < seg) { int k = i * 2; t.AddRange(new[] { k, k + 1, k + 3, k, k + 3, k + 2 }); }
        }
        return Build("MechRing", v, uv, col, t);
    }

    /// <summary>A disc of concentric bands: colours[i] fills radius radii[i-1]..radii[i] (radii ascending).</summary>
    public static Mesh Bands(float[] radii, Color[] colours, int seg = 48)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var t = new List<int>();
        float r0 = 0f;
        for (int b = 0; b < radii.Length; b++)
        {
            float r1 = radii[b]; Color c = colours[b];
            int start = v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float u = i / (float)seg, a = u * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                v.Add(d * r0); v.Add(d * r1);
                uv.Add(new Vector2(u, 0f)); uv.Add(new Vector2(u, 1f));
                col.Add(c); col.Add(c);
                if (i < seg) { int k = start + i * 2; t.AddRange(new[] { k, k + 1, k + 3, k, k + 3, k + 2 }); }
            }
            r0 = r1;
        }
        return Build("MechBands", v, uv, col, t);
    }

    /// <summary>A soft disc: colour c in the middle fading to transparent at radius r.</summary>
    public static Mesh SoftDisc(float r, Color c, int seg = 32)
    {
        var v = new List<Vector3> { Vector3.zero }; var uv = new List<Vector2> { Vector2.zero };
        var col = new List<Color> { c }; var t = new List<int>();
        var edge = new Color(c.r, c.g, c.b, 0f);
        for (int i = 0; i <= seg; i++)
        {
            float u = i / (float)seg, a = u * Mathf.PI * 2f;
            v.Add(new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r); uv.Add(new Vector2(u, 1f)); col.Add(edge);
            if (i < seg) t.AddRange(new[] { 0, i + 1, i + 2 });
        }
        return Build("MechSoftDisc", v, uv, col, t);
    }

    /// <summary>A stripe along +Z from z0 to z1, half width w, uv.x = 0 at z0 .. 1 at z1, alpha fading at the long edges.</summary>
    public static Mesh Stripe(float z0, float z1, float w, Color c)
    {
        var edge = new Color(c.r, c.g, c.b, 0f);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var t = new List<int>();
        int n = 8;
        for (int i = 0; i <= n; i++)
        {
            float u = i / (float)n, z = Mathf.Lerp(z0, z1, u);
            v.Add(new Vector3(-w, 0f, z)); v.Add(new Vector3(0f, 0f, z)); v.Add(new Vector3(w, 0f, z));
            uv.Add(new Vector2(u, 0f)); uv.Add(new Vector2(u, 0.5f)); uv.Add(new Vector2(u, 1f));
            col.Add(edge); col.Add(c); col.Add(edge);
            if (i < n) { int k = i * 3; t.AddRange(new[] { k, k + 1, k + 4, k, k + 4, k + 3, k + 1, k + 2, k + 5, k + 1, k + 5, k + 4 }); }
        }
        return Build("MechStripe", v, uv, col, t);
    }

    /// <summary>An irregular round patch of mean radius r (wobble 0..1 of r), vertex alpha 1 inside to 0 at the rim,
    /// the same shape for the same seed.</summary>
    public static Mesh Patch(float r, float wobble, int seed, int seg = 40)
    {
        var rnd = new System.Random(seed);
        float p1 = (float)rnd.NextDouble() * 6.28f, p2 = (float)rnd.NextDouble() * 6.28f, p3 = (float)rnd.NextDouble() * 6.28f;
        var v = new List<Vector3> { Vector3.zero }; var uv = new List<Vector2> { Vector2.zero };
        var col = new List<Color> { Color.white }; var t = new List<int>();
        for (int ring = 1; ring <= 3; ring++)
        {
            float k = ring == 1 ? 0.55f : ring == 2 ? 0.85f : 1f;
            float alpha = ring == 1 ? 1f : ring == 2 ? 0.55f : 0f;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float rr = r * (1f + wobble * (0.55f * Mathf.Sin(a * 2f + p1) + 0.3f * Mathf.Sin(a * 3f + p2) + 0.15f * Mathf.Sin(a * 5f + p3)));
                v.Add(new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * rr * k);
                uv.Add(new Vector2(i / (float)seg, k)); col.Add(new Color(1f, 1f, 1f, alpha));
            }
        }
        for (int i = 0; i < seg; i++) t.AddRange(new[] { 0, 1 + i, 1 + (i + 1) % seg });
        for (int ring = 0; ring < 2; ring++)
        {
            int a0 = 1 + ring * seg, b0 = 1 + (ring + 1) * seg;
            for (int i = 0; i < seg; i++)
            {
                int j = (i + 1) % seg;
                t.AddRange(new[] { a0 + i, b0 + i, b0 + j, a0 + i, b0 + j, a0 + j });
            }
        }
        return Build("MechPatch", v, uv, col, t);
    }

    static Mesh Build(string name, List<Vector3> v, List<Vector2> uv, List<Color> col, List<int> t)
    {
        var m = new Mesh { name = name };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(col);
        // the triangles above wind clockwise seen from below for some strips: draw both sides (the shaders Cull Off)
        m.SetTriangles(t, 0);
        var n = new Vector3[v.Count]; for (int i = 0; i < n.Length; i++) n[i] = Vector3.up;
        m.normals = n;
        m.RecalculateBounds();
        return m;
    }

    /// <summary>A child object drawing mesh m with material mat (a fresh instance when instance is true), y above the parent.</summary>
    public static MeshRenderer Child(Transform parent, string name, Mesh m, Material mat, float y, bool instance = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, y, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = instance && mat != null ? new Material(mat) : mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return r;
    }
}
