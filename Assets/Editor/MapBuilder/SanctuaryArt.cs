using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Meshes of the Greek sanctuary (a small ruined tholos, Nov 2026), made in code and saved as assets in
/// Assets/Art/Mechanics: the stepped round marble platform (krepidoma: two 7 cm steps; its top is a second submesh drawn
/// with BrawlFortress/Sanctuary, uv = xz / top radius), fluted Doric column stumps with broken tops, the altar (bomos:
/// plinth, body, terracotta frieze with a black key band, cornice, corner horns, bronze fire bowl) and the sacred flame
/// (crossed tongues on the lava_grad ramp, drawn with VolcanoGlow). Palette cells = PALETTE order in
/// Tools/IceArt/common.py (the props material, IcePropsGreek on Greek maps).
/// </summary>
public static class SanctuaryArt
{
    public const string Dir = "Assets/Art/Mechanics";
    public const float StepH = 0.07f, StepW = 0.25f;
    const int PBone = 30, PBoneDark = 31, PCream = 40, PTerra = 53, PBlack = 19, PGold = 38, PBronze = 39, PLava = 62;
    static Vector2 Cell(int i) => new Vector2((i % 8 + 0.5f) / 8f, 1f - (i / 8 + 0.5f) / 8f);
    static Vector2 Grad(int i, float t) => new Vector2((i % 8 + 0.5f) / 8f, (7 - i / 8) / 8f + (0.5f + Mathf.Clamp01(t) * 15f) / 128f);

    class B
    {
        public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int>[] t = { new List<int>(), new List<int>() };
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 hint, int sub = 0, bool smooth = false)
        {
            var nr = Vector3.Cross(b - a, d - a).normalized;
            if (Vector3.Dot(nr, hint) < 0) { (b, d) = (d, b); (ub, ud) = (ud, ub); nr = -nr; }
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
            for (int k = 0; k < 4; k++) n.Add(smooth ? hint.normalized : nr);
            t[sub].AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int cell, Vector3 hint) { var u = Cell(cell); Quad(a, b, c, d, u, u, u, u, hint); }
        public void Tri(Vector3 a, Vector3 b, Vector3 c, int cell, Vector3 hint, int sub = 0)
        {
            var nr = Vector3.Cross(b - a, c - a).normalized;
            if (Vector3.Dot(nr, hint) < 0) { (b, c) = (c, b); nr = -nr; }
            int i = v.Count; var u = Cell(cell);
            v.Add(a); v.Add(b); v.Add(c); uv.Add(u); uv.Add(u); uv.Add(u); n.Add(nr); n.Add(nr); n.Add(nr);
            t[sub].AddRange(new[] { i, i + 1, i + 2 });
        }
        public void Box(Vector3 mn, Vector3 mx, int top, int side)
        {
            Vector3 P(float x, float y, float z) => new Vector3(x, y, z);
            Quad(P(mn.x, mx.y, mn.z), P(mx.x, mx.y, mn.z), P(mx.x, mx.y, mx.z), P(mn.x, mx.y, mx.z), top, Vector3.up);
            Quad(P(mx.x, mn.y, mn.z), P(mx.x, mn.y, mx.z), P(mx.x, mx.y, mx.z), P(mx.x, mx.y, mn.z), side, Vector3.right);
            Quad(P(mn.x, mn.y, mn.z), P(mn.x, mn.y, mx.z), P(mn.x, mx.y, mx.z), P(mn.x, mx.y, mn.z), side, Vector3.left);
            Quad(P(mn.x, mn.y, mx.z), P(mx.x, mn.y, mx.z), P(mx.x, mx.y, mx.z), P(mn.x, mx.y, mx.z), side, Vector3.forward);
            Quad(P(mn.x, mn.y, mn.z), P(mx.x, mn.y, mn.z), P(mx.x, mx.y, mn.z), P(mn.x, mx.y, mn.z), side, Vector3.back);
        }
        public Mesh Mesh(string name, int subs)
        {
            var m = new Mesh { name = name };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv);
            m.subMeshCount = subs;
            for (int s = 0; s < subs; s++) m.SetTriangles(t[s], s);
            m.RecalculateBounds();
            return m;
        }
    }

    static Mesh Save(Mesh m)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Art", "Mechanics");
        string p = Dir + "/" + m.name + ".asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(p);
        if (old != null)
        {   // write into the existing asset (keeps references; CopySerialized left stale vertex data once)
            old.Clear();
            old.SetVertices(new List<Vector3>(m.vertices)); old.SetNormals(new List<Vector3>(m.normals)); old.SetUVs(0, new List<Vector2>(m.uv));
            old.subMeshCount = m.subMeshCount;
            for (int s = 0; s < m.subMeshCount; s++) old.SetTriangles(m.GetTriangles(s), s);
            old.RecalculateBounds();
            EditorUtility.SetDirty(old);
            return old;
        }
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    /// <summary>Two steps around a round top of radius r - StepW; submesh 0 = the stone (palette), 1 = the top (floor shader).</summary>
    public static Mesh Platform(float r)
    {
        var b = new B();
        int seg = 48;
        float r1 = r - StepW, h1 = StepH, h2 = StepH * 2f;
        for (int i = 0; i < seg; i++)
        {
            float a0 = i / (float)seg * Mathf.PI * 2f, a1 = (i + 1) / (float)seg * Mathf.PI * 2f;
            Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
            Vector3 mid = (d0 + d1).normalized;
            int blockA = (i / 2) % 2 == 0 ? PBone : PCream;     // blocks of two segments, alternating tone
            int blockB = ((i + 1) / 2) % 2 == 0 ? PBone : PCream;
            // lower step: riser and tread
            b.Quad(d0 * r, d1 * r, d1 * r + Vector3.up * h1, d0 * r + Vector3.up * h1, PBoneDark, mid);
            b.Quad(d0 * r + Vector3.up * h1, d1 * r + Vector3.up * h1, d1 * r1 + Vector3.up * h1, d0 * r1 + Vector3.up * h1, blockA, Vector3.up);
            // upper step riser
            b.Quad(d0 * r1 + Vector3.up * h1, d1 * r1 + Vector3.up * h1, d1 * r1 + Vector3.up * h2, d0 * r1 + Vector3.up * h2, blockB == PBone ? PBoneDark : PBone, mid);
            // the top (floor shader): fan, uv = xz / r1
            var c = Vector3.up * h2;
            int k = b.v.Count;
            b.v.Add(c); b.v.Add(d0 * r1 + c); b.v.Add(d1 * r1 + c);
            b.uv.Add(Vector2.zero); b.uv.Add(new Vector2(d0.x, d0.z)); b.uv.Add(new Vector2(d1.x, d1.z));
            b.n.Add(Vector3.up); b.n.Add(Vector3.up); b.n.Add(Vector3.up);
            b.t[1].AddRange(new[] { k, k + 2, k + 1 });
        }
        return Save(b.Mesh("SanctuaryPlatform_" + Mathf.RoundToInt(r * 100f), 2));
    }

    /// <summary>A fluted Doric column stump of height h (radius 0.27 at the foot), broken off unevenly at the top.</summary>
    public static Mesh Stump(float h, int seed)
    {
        var rnd = new System.Random(seed);
        var b = new B();
        int flutes = 16, seg = flutes * 2;
        float rb = 0.27f, rt = 0.24f;
        var top = new Vector3[seg];
        for (int i = 0; i < seg; i++) top[i] = Vector3.up * (h - 0.12f + (float)rnd.NextDouble() * 0.16f);
        Vector3 P(int i, float y, float k)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            float rr = Mathf.Lerp(rb, rt, y / Mathf.Max(h, 0.01f)) * (i % 2 == 0 ? 1f : 0.9f) * k;   // flutes: every other edge set in
            return new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr);
        }
        for (int i = 0; i < seg; i++)
        {
            int j = (i + 1) % seg;
            float ya = top[i].y, yb = top[j].y;
            Vector3 a0 = P(i, 0f, 1f), b0 = P(j, 0f, 1f), a1 = P(i, ya, 1f), b1 = P(j, yb, 1f);
            Vector3 outward = (a0 + b0); outward.y = 0f;
            b.Quad(a0, b0, b1, a1, PBone, outward);
            // broken top: towards a lower, off-centre core
            Vector3 core = new Vector3(0.04f, h - 0.16f, -0.03f);
            b.Tri(a1, b1, core, PCream, Vector3.up);
        }
        // a worn foot drum (annulus band slightly wider)
        for (int i = 0; i < seg; i++)
        {
            int j = (i + 1) % seg;
            Vector3 a0 = P(i, 0f, 1.08f), b0 = P(j, 0f, 1.08f), a1 = P(i, 0.08f, 1.08f), b1 = P(j, 0.08f, 1.08f);
            Vector3 outward = (a0 + b0); outward.y = 0f;
            b.Quad(a0, b0, b1, a1, PBoneDark, outward);
            b.Quad(a1, b1, P(j, 0.08f, 1f), P(i, 0.08f, 1f), PBone, Vector3.up);
        }
        return Save(b.Mesh("SanctuaryStump_" + Mathf.RoundToInt(h * 100f) + "_" + seed, 1));
    }

    /// <summary>The altar (bomos), s = scale (1 = 1 m plinth), its top 0.72 m up, with a bronze fire bowl.</summary>
    public static Mesh Altar(float s)
    {
        var b = new B();
        Vector3 V(float x, float y, float z) => new Vector3(x, y, z) * s;
        b.Box(V(-0.5f, 0f, -0.5f), V(0.5f, 0.1f, 0.5f), PBoneDark, PBoneDark);          // plinth
        b.Box(V(-0.4f, 0.1f, -0.4f), V(0.4f, 0.48f, 0.4f), PBone, PBone);              // body
        b.Box(V(-0.42f, 0.48f, -0.42f), V(0.42f, 0.6f, 0.42f), PTerra, PTerra);        // frieze
        // black key band on the frieze: small blocks along each face
        for (int f = 0; f < 4; f++)
        {
            var rot = Quaternion.Euler(0f, f * 90f, 0f);
            for (int k = 0; k < 5; k++)
            {
                float x0 = -0.34f + k * 0.15f;
                Vector3 mn = rot * new Vector3(x0, 0.5f, 0.42f), mx = rot * new Vector3(x0 + 0.08f, 0.58f, 0.43f);
                Vector3 lo = Vector3.Min(mn, mx), hi = Vector3.Max(mn, mx);
                b.Box(lo * s, hi * s, PBlack, PBlack);
                Vector3 mn2 = rot * new Vector3(x0 + 0.08f, 0.5f, 0.42f), mx2 = rot * new Vector3(x0 + 0.11f, 0.54f, 0.43f);
                b.Box(Vector3.Min(mn2, mx2) * s, Vector3.Max(mn2, mx2) * s, PBlack, PBlack);
            }
        }
        b.Box(V(-0.47f, 0.6f, -0.47f), V(0.47f, 0.68f, 0.47f), PCream, PBone);         // cornice
        foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })   // corner horns
            b.Box(V(x * 0.43f - 0.05f, 0.68f, z * 0.43f - 0.05f), V(x * 0.43f + 0.05f, 0.8f, z * 0.43f + 0.05f), PCream, PBone);
        // bronze fire bowl: an octagonal dish
        int seg = 8;
        for (int i = 0; i < seg; i++)
        {
            float a0 = i / (float)seg * Mathf.PI * 2f, a1 = (i + 1) / (float)seg * Mathf.PI * 2f;
            Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
            Vector3 lo0 = (d0 * 0.18f + Vector3.up * 0.68f) * s, lo1 = (d1 * 0.18f + Vector3.up * 0.68f) * s;
            Vector3 hi0 = (d0 * 0.3f + Vector3.up * 0.82f) * s, hi1 = (d1 * 0.3f + Vector3.up * 0.82f) * s;
            b.Quad(lo0, lo1, hi1, hi0, PBronze, d0 + d1);
            b.Quad(hi0, hi1, (d1 * 0.26f + Vector3.up * 0.8f) * s, (d0 * 0.26f + Vector3.up * 0.8f) * s, PGold, Vector3.up);
        }
        return Save(b.Mesh("SanctuaryAltar_" + Mathf.RoundToInt(s * 100f), 1));
    }

    /// <summary>The sacred flame: three crossed tongues (lava_grad ramp: deep red at the foot, yellow-white at the tip), 0.7 m.</summary>
    public static Mesh Flame()
    {
        var b = new B();
        for (int k = 0; k < 3; k++)
        {
            var rot = Quaternion.Euler(0f, k * 60f, 0f);
            float w = 0.22f - k * 0.03f, h = 0.7f - k * 0.12f;
            // a tongue: wide at the foot, pinched at the tip, slightly bent
            var pts = new[] { (0f, w), (0.35f, w * 0.85f), (0.7f, w * 0.45f), (1f, 0.02f) };
            for (int i = 0; i < pts.Length - 1; i++)
            {
                var (t0, w0) = pts[i]; var (t1, w1) = pts[i + 1];
                float bend0 = Mathf.Sin(t0 * 2.4f) * 0.05f, bend1 = Mathf.Sin(t1 * 2.4f) * 0.05f;
                Vector3 a = rot * new Vector3(-w0, t0 * h, bend0), bb = rot * new Vector3(w0, t0 * h, bend0);
                Vector3 c = rot * new Vector3(w1, t1 * h, bend1), d = rot * new Vector3(-w1, t1 * h, bend1);
                b.Quad(a, bb, c, d, Grad(PLava, t0 * 0.9f), Grad(PLava, t0 * 0.9f), Grad(PLava, t1 * 0.95f + 0.05f), Grad(PLava, t1 * 0.95f + 0.05f), rot * Vector3.forward);
            }
        }
        return Save(b.Mesh("SanctuaryFlame", 1));
    }
}
