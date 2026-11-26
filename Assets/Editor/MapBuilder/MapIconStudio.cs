using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Map-select icons of the new themes (Nov 2026): every icon gets its own grading instead of one fixed formula (that made
/// the desert / savanna maps yellow and the dusk / lava / night maps too dark).
///  1. RenderRaw: a "hero" shot of the open map scene (camera behind one end, looking across the arena to the far gate
///     and its landmark: torii, temple gate, big top...), 800 px, no grading, cached in Library/MapIconRaw/&lt;icon&gt;.png.
///  2. Grade: adaptive per image (auto levels, exposure to a target brightness, partial grey-world white balance toward
///     the theme's tint, highlight shoulder so snow does not clip, contrast, clarity, saturation / vibrance, yellow
///     control, bloom on glowing themes, vignette), with the theme's settings and per-map overrides from
///     Assets/Editor/MapBuilder/MapIconGrades.json (edit it and run Regrade: no scene is opened).
///  3. 400 px PNG into Assets/UI/Image/MapIcon/&lt;icon&gt;.png (BrawlMapBuilder.RenderIcon calls this for new themes).
/// Menus: Tools/Maps/Icons/...
/// </summary>
public static class MapIconStudio
{
    public const string GradesPath = "Assets/Editor/MapBuilder/MapIconGrades.json";
    public const string IconDir = "Assets/UI/Image/MapIcon/";
    public static string RawDir => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "MapIconRaw");
    const int RawSize = 800, OutSize = 400;

    [Serializable]
    public class Grade
    {
        public float targetLum = 0.5f;     // mean brightness after grading (0 = keep)
        public float autoLevels = 0.4f;    // 0..1 share of the white-point stretch (99% of the luminance up to white)
        public float blackStretch = 0f;    // 0..1 share of the black-point stretch: keep 0 (it crushed shadows into black spots)
        public float floor = 0.05f;        // darkest output level (lifts pure black: no black spots)
        public float exposure = 0f;        // extra EV after the target
        public float wb = 0.35f;           // 0..1 grey-world white balance strength
        public float tintR = 1f, tintG = 1f, tintB = 1f;   // colour the white balance aims at (theme mood)
        public float saturation = 1.08f;
        public float vibrance = 0.2f;      // extra saturation for dull colours
        public float yellowSat = 1f;       // saturation of yellow-orange hues (desert / savanna: < 1)
        public float yellowHueShift = 0f;  // degrees, + toward red (sand reads less lemon)
        public float yellowLo = 30f, yellowHi = 75f;   // hue band (degrees) yellowSat / yellowHueShift act on (savanna: from 10 = orange too)
        public float contrast = 0.04f;     // S-curve amount
        public float clarity = 0f;         // local contrast (dark halos: keep 0 or tiny)
        public float shoulder = 0.35f;     // highlight compression (snow, white marble)
        public float shadows = 0.12f;      // shadow lift
        public float bloom = 0f;           // glow of bright pixels (lamps, lava, glowing mushrooms)
        public float bloomThreshold = 0.75f;
        public float vignette = 0.05f;
        public Grade Clone() => (Grade)MemberwiseClone();
    }

    [Serializable]
    public class Cam
    {
        public float height = 19f, back = 23f, lookZ = 3.5f, fov = 46f, x = 0f;
        public Cam Clone() => (Cam)MemberwiseClone();
    }

    [Serializable] public class ThemeEntry { public string key; public Grade grade = new Grade(); public Cam cam = new Cam(); }
    /// <summary>Per map: "field=value field=value" applied over the theme's grade / camera (cam.height=18 for the camera).</summary>
    [Serializable] public class MapEntry { public string key; public string set; }
    [Serializable] public class GradeFile { public List<ThemeEntry> themes = new List<ThemeEntry>(); public List<MapEntry> maps = new List<MapEntry>(); }

    static GradeFile Load()
    {
        if (!File.Exists(GradesPath)) return new GradeFile();
        return JsonUtility.FromJson<GradeFile>(File.ReadAllText(GradesPath)) ?? new GradeFile();
    }

    public static void Resolve(string theme, string mapEnum, bool night, out Grade grade, out Cam cam)
    {
        var f = Load();
        string key = night && f.themes.Any(t => t.key == theme + "Night") ? theme + "Night" : theme;
        var te = f.themes.FirstOrDefault(t => t.key == key) ?? f.themes.FirstOrDefault(t => t.key == "Default") ?? new ThemeEntry();
        grade = te.grade.Clone(); cam = te.cam.Clone();
        var me = f.maps.FirstOrDefault(m => m.key == mapEnum);
        if (me != null && !string.IsNullOrEmpty(me.set)) Apply(me.set, grade, cam);
    }

    static void Apply(string set, Grade g, Cam c)
    {
        foreach (var part in set.Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=');
            if (kv.Length != 2 || !float.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) continue;
            string name = kv[0]; object target = g;
            if (name.StartsWith("cam.")) { name = name.Substring(4); target = c; }
            var fi = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (fi != null && fi.FieldType == typeof(float)) fi.SetValue(target, v);
            else Debug.LogWarning("[MapIconStudio] unknown setting " + part);
        }
    }

    // ------------------------------------------------------------------ render

    /// <summary>Renders the open map scene's hero shot (no grading) into the raw cache.</summary>
    public static void RenderRaw(string icon, Cam c)
    {
        var go = new GameObject("_iconcam") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = c.fov; cam.farClipPlane = 400f;
            cam.transform.position = new Vector3(c.x, c.height, -c.back);
            cam.transform.LookAt(new Vector3(c.x * 0.3f, 0f, c.lookZ));
            var rt = new RenderTexture(RawSize, RawSize, 24) { antiAliasing = 8 };
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(RawSize, RawSize, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, RawSize, RawSize), 0, 0); tex.Apply();
            RenderTexture.active = null; cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Directory.CreateDirectory(RawDir);
            File.WriteAllBytes(Path.Combine(RawDir, icon + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>Grades the cached raw shot and writes the 400 px icon. Returns false without a raw shot.</summary>
    public static bool GradeIcon(string icon, Grade g, string outPath = null)
    {
        string raw = Path.Combine(RawDir, icon + ".png");
        if (!File.Exists(raw)) return false;
        var src = new Texture2D(2, 2, TextureFormat.RGB24, false);
        src.LoadImage(File.ReadAllBytes(raw));
        var px = src.GetPixels();
        int w = src.width, h = src.height;
        Object.DestroyImmediate(src);
        Process(px, w, h, g);
        var outPx = Downscale(px, w, h, OutSize);
        var outTex = new Texture2D(OutSize, OutSize, TextureFormat.RGB24, false);
        outTex.SetPixels(outPx); outTex.Apply();
        byte[] png = outTex.EncodeToPNG();
        Object.DestroyImmediate(outTex);
        string ip = outPath ?? IconDir + icon + ".png";
        WriteMapped(ip, png);
        return true;
    }

    // ------------------------------------------------------------------ grading

    static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    static void Process(Color[] px, int w, int h, Grade g)
    {
        int n = px.Length;
        // --- white balance: grey world, partial, toward the theme tint
        double mr = 0, mg = 0, mb = 0;
        for (int i = 0; i < n; i++) { mr += px[i].r; mg += px[i].g; mb += px[i].b; }
        mr /= n; mg /= n; mb /= n;
        double ml = (mr + mg + mb) / 3.0;
        float gr = Mathf.Lerp(1f, (float)(ml / Math.Max(mr, 1e-4)), g.wb) * g.tintR;
        float gg = Mathf.Lerp(1f, (float)(ml / Math.Max(mg, 1e-4)), g.wb) * g.tintG;
        float gb = Mathf.Lerp(1f, (float)(ml / Math.Max(mb, 1e-4)), g.wb) * g.tintB;
        float norm = 1f / (0.2126f * gr + 0.7152f * gg + 0.0722f * gb);
        gr *= norm; gg *= norm; gb *= norm;
        for (int i = 0; i < n; i++) { var c = px[i]; px[i] = new Color(c.r * gr, c.g * gg, c.b * gb); }

        // --- auto levels (1%..99% of the luminance)
        var hist = new int[256];
        for (int i = 0; i < n; i++) hist[Mathf.Clamp((int)(Luma(px[i]) * 255f), 0, 255)]++;
        float lo = Percentile(hist, n, 0.01f), hi = Percentile(hist, n, 0.99f);
        float a = g.autoLevels;
        float L0 = lo * g.blackStretch, L1 = Mathf.Lerp(1f, hi, a);
        float span = Mathf.Max(0.2f, L1 - L0);
        for (int i = 0; i < n; i++) { var c = px[i]; px[i] = new Color((c.r - L0) / span, (c.g - L0) / span, (c.b - L0) / span); }

        // --- exposure: a gamma that brings the mean to the target (keeps the range, unlike a gain), then extra EV
        if (g.targetLum > 0f)
        {
            double sum = 0; for (int i = 0; i < n; i++) sum += Mathf.Clamp01(Luma(px[i]));
            float mean = Mathf.Clamp((float)(sum / n), 0.05f, 0.95f);
            float gamma = Mathf.Clamp(Mathf.Log(g.targetLum) / Mathf.Log(mean), 0.45f, 2.2f);
            for (int i = 0; i < n; i++)
            {
                var c = px[i];
                px[i] = new Color(Mathf.Pow(Mathf.Max(c.r, 0f), gamma), Mathf.Pow(Mathf.Max(c.g, 0f), gamma), Mathf.Pow(Mathf.Max(c.b, 0f), gamma));
            }
        }
        float ev = Mathf.Pow(2f, g.exposure);

        // --- clarity: local contrast of the luminance (detail = L - blurred L)
        float[] lum = new float[n];
        for (int i = 0; i < n; i++) lum[i] = Luma(px[i]);
        float[] blur = g.clarity != 0f ? BoxBlur(lum, w, h, Mathf.Max(2, w / 60), 2) : null;
        // --- bloom source: bright parts, blurred wide
        float[] glow = null;
        if (g.bloom > 0f)
        {
            var bright = new float[n * 3];
            for (int i = 0; i < n; i++)
            {
                float k = Mathf.Max(0f, lum[i] - g.bloomThreshold) / Mathf.Max(0.05f, 1f - g.bloomThreshold);
                bright[i * 3] = px[i].r * k; bright[i * 3 + 1] = px[i].g * k; bright[i * 3 + 2] = px[i].b * k;
            }
            glow = BlurRgb(bright, w, h, Mathf.Max(4, w / 30));
        }

        float cx = w * 0.5f, cy = h * 0.5f;
        for (int i = 0; i < n; i++)
        {
            var c = px[i];
            float r = c.r * ev, gg2 = c.g * ev, b = c.b * ev;
            if (blur != null)
            {
                float d = (lum[i] - blur[i]) * g.clarity;
                r += d; gg2 += d; b += d;
            }
            // saturation / vibrance / yellow control
            float l = 0.2126f * r + 0.7152f * gg2 + 0.0722f * b;
            float mx = Mathf.Max(r, Mathf.Max(gg2, b)), mn = Mathf.Min(r, Mathf.Min(gg2, b));
            float sat = mx > 1e-4f ? (mx - mn) / mx : 0f;
            float hue = Hue(r, gg2, b);
            float yellow = HueBand(hue, g.yellowLo, g.yellowHi, 15f);
            float s = g.saturation * (1f + g.vibrance * (1f - sat)) * Mathf.Lerp(1f, g.yellowSat, yellow);
            r = l + (r - l) * s; gg2 = l + (gg2 - l) * s; b = l + (b - l) * s;
            if (g.yellowHueShift != 0f && yellow > 0f) RotateHue(ref r, ref gg2, ref b, -g.yellowHueShift * yellow);
            // contrast S-curve, shadow lift, highlight shoulder
            r = Tone(r, g); gg2 = Tone(gg2, g); b = Tone(b, g);
            // bloom
            if (glow != null) { r += glow[i * 3] * g.bloom; gg2 += glow[i * 3 + 1] * g.bloom; b += glow[i * 3 + 2] * g.bloom; }
            // vignette
            int x = i % w, y = i / w;
            float dx = (x - cx) / cx, dy = (y - cy) / cy;
            float vv = 1f - g.vignette * Mathf.Pow(Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.5f) / 0.7f), 1.6f);
            float fl = g.floor;
            px[i] = new Color(fl + Mathf.Clamp01(r * vv) * (1f - fl), fl + Mathf.Clamp01(gg2 * vv) * (1f - fl), fl + Mathf.Clamp01(b * vv) * (1f - fl));
        }
    }

    static float Tone(float v, Grade g)
    {
        v = Mathf.Max(0f, v);
        v += g.shadows * Mathf.Pow(1f - Mathf.Clamp01(v), 4f);
        float s = Mathf.Clamp01(v);
        float sc = s * s * (3f - 2f * s);
        v = v + (sc - s) * g.contrast * 2f;
        const float knee = 0.72f;
        if (v > knee && g.shoulder > 0f)
        {
            float t = v - knee;
            v = knee + t / (1f + g.shoulder * t / (1f - knee) * 1.6f);
        }
        return v;
    }

    static float Percentile(int[] hist, int n, float q)
    {
        int target = (int)(n * q), acc = 0;
        for (int i = 0; i < 256; i++) { acc += hist[i]; if (acc >= target) return i / 255f; }
        return 1f;
    }

    static float Hue(float r, float g, float b)
    {
        float mx = Mathf.Max(r, Mathf.Max(g, b)), mn = Mathf.Min(r, Mathf.Min(g, b)), d = mx - mn;
        if (d < 1e-5f) return -1f;
        float h;
        if (mx == r) h = 60f * (((g - b) / d) % 6f);
        else if (mx == g) h = 60f * ((b - r) / d + 2f);
        else h = 60f * ((r - g) / d + 4f);
        return h < 0f ? h + 360f : h;
    }

    /// <summary>1 inside [a, b] degrees, fading over 'soft' degrees outside.</summary>
    static float HueBand(float h, float a, float b, float soft)
    {
        if (h < 0f) return 0f;
        if (h >= a && h <= b) return 1f;
        float d = h < a ? a - h : h - b;
        return Mathf.Clamp01(1f - d / soft);
    }

    static void RotateHue(ref float r, ref float g, ref float b, float deg)
    {
        float a = deg * Mathf.Deg2Rad, cosA = Mathf.Cos(a), sinA = Mathf.Sin(a);
        float k = (1f - cosA) / 3f, s3 = Mathf.Sqrt(1f / 3f) * sinA;
        float m00 = cosA + k, m01 = k - s3, m02 = k + s3;
        float nr = r * m00 + g * m01 + b * m02;
        float ng = r * m02 + g * m00 + b * m01;
        float nb = r * m01 + g * m02 + b * m00;
        r = nr; g = ng; b = nb;
    }

    static float[] BoxBlur(float[] src, int w, int h, int radius, int passes)
    {
        var a = (float[])src.Clone(); var tmp = new float[a.Length];
        for (int p = 0; p < passes; p++) { BlurH(a, tmp, w, h, radius); BlurV(tmp, a, w, h, radius); }
        return a;
    }

    static float[] BlurRgb(float[] rgb, int w, int h, int radius)
    {
        int n = w * h;
        var res = new float[n * 3];
        for (int ch = 0; ch < 3; ch++)
        {
            var c = new float[n];
            for (int i = 0; i < n; i++) c[i] = rgb[i * 3 + ch];
            c = BoxBlur(c, w, h, radius, 3);
            for (int i = 0; i < n; i++) res[i * 3 + ch] = c[i];
        }
        return res;
    }

    static void BlurH(float[] s, float[] d, int w, int h, int r)
    {
        float inv = 1f / (2 * r + 1);
        for (int y = 0; y < h; y++)
        {
            int row = y * w; float acc = 0f;
            for (int x = -r; x <= r; x++) acc += s[row + Mathf.Clamp(x, 0, w - 1)];
            for (int x = 0; x < w; x++)
            {
                d[row + x] = acc * inv;
                acc += s[row + Mathf.Min(x + r + 1, w - 1)] - s[row + Mathf.Max(x - r, 0)];
            }
        }
    }

    static void BlurV(float[] s, float[] d, int w, int h, int r)
    {
        float inv = 1f / (2 * r + 1);
        for (int x = 0; x < w; x++)
        {
            float acc = 0f;
            for (int y = -r; y <= r; y++) acc += s[Mathf.Clamp(y, 0, h - 1) * w + x];
            for (int y = 0; y < h; y++)
            {
                d[y * w + x] = acc * inv;
                acc += s[Mathf.Min(y + r + 1, h - 1) * w + x] - s[Mathf.Max(y - r, 0) * w + x];
            }
        }
    }

    static Color[] Downscale(Color[] px, int w, int h, int size)
    {
        int f = w / size;
        var o = new Color[size * size];
        float inv = 1f / (f * f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = 0, g = 0, b = 0;
                for (int j = 0; j < f; j++)
                    for (int i = 0; i < f; i++) { var c = px[(y * f + j) * w + x * f + i]; r += c.r; g += c.g; b += c.b; }
                o[y * size + x] = new Color(r * inv, g * inv, b * inv);
            }
        return o;
    }

    // Unity keeps imported files memory-mapped (Win32 error 1224 on write): unload the asset and retry; still locked:
    // overwrite in place without truncating (only shrinking a mapped file is refused), the new PNG padded to the old
    // length with an ancillary chunk that decoders skip.
    static void WriteMapped(string path, byte[] bytes)
    {
        try { File.WriteAllBytes(path, bytes); return; } catch (IOException) { }
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o && !(o is GameObject) && !(o is Component)) Resources.UnloadAsset(o);
        EditorUtility.UnloadUnusedAssetsImmediate(); GC.Collect(); GC.WaitForPendingFinalizers();
        AssetDatabase.ReleaseCachedFileHandles();
        try { File.WriteAllBytes(path, bytes); return; } catch (IOException) { }
        try
        {
            long oldLen = new FileInfo(path).Length;
            byte[] data = bytes.Length < oldLen ? PadPng(bytes, (int)oldLen) : bytes;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Write(data, 0, data.Length);
                fs.Flush();
            }
        }
        catch (Exception e) { Debug.LogWarning("[MapIconStudio] " + path + " locked, old image kept: " + e.Message); }
    }

    /// <summary>The PNG grown to exactly 'length' bytes with a private ancillary chunk ("paDd") before IEND.</summary>
    static byte[] PadPng(byte[] png, int length)
    {
        const int iendLen = 12;                       // length(4) + "IEND" + crc(4)
        int pad = length - png.Length - 12;           // chunk overhead: length + type + crc
        if (pad < 0) pad = 0;
        var outB = new byte[png.Length + 12 + pad];
        int head = png.Length - iendLen;
        Buffer.BlockCopy(png, 0, outB, 0, head);
        int p = head;
        outB[p++] = (byte)(pad >> 24); outB[p++] = (byte)(pad >> 16); outB[p++] = (byte)(pad >> 8); outB[p++] = (byte)pad;
        int typeAt = p;
        outB[p++] = (byte)'p'; outB[p++] = (byte)'a'; outB[p++] = (byte)'D'; outB[p++] = (byte)'d';
        p += pad;                                      // zero bytes
        uint crc = Crc32(outB, typeAt, 4 + pad);
        outB[p++] = (byte)(crc >> 24); outB[p++] = (byte)(crc >> 16); outB[p++] = (byte)(crc >> 8); outB[p++] = (byte)crc;
        Buffer.BlockCopy(png, head, outB, p, iendLen);
        return outB;
    }

    static uint[] crcTable;
    static uint Crc32(byte[] b, int off, int len)
    {
        if (crcTable == null)
        {
            crcTable = new uint[256];
            for (uint n = 0; n < 256; n++) { uint c = n; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1; crcTable[n] = c; }
        }
        uint crc = 0xFFFFFFFFu;
        for (int i = 0; i < len; i++) crc = crcTable[(crc ^ b[off + i]) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }

    // ------------------------------------------------------------------ batch

    static bool IsNewTheme(BrawlMapBuilder.Theme t) => t >= BrawlMapBuilder.Theme.Ice;

    /// <summary>Selectable maps of the new themes (Ice and later), in definition order.</summary>
    public static List<BrawlMapBuilder.Def> NewThemeDefs() =>
        BrawlMapBuilder.AllDefs().Where(d => IsNewTheme(d.theme) && !BrawlMapBuilder.Retired.Contains(d.mapEnum)).ToList();

    /// <summary>BrawlMapBuilder.RenderIcon for a new-theme map: hero shot of the open scene, graded, reimported.</summary>
    public static void RenderAndGrade(BrawlMapBuilder.Def d)
    {
        Resolve(d.theme.ToString(), d.mapEnum, d.night, out var g, out var c);
        RenderRaw(d.icon, c);
        GradeIcon(d.icon, g);
        Reimport(d.icon);
    }

    /// <summary>Opens maps [start, start+count) of NewThemeDefs one by one and caches their raw shots (no saving).
    /// Refuses to run with unsaved scenes open. Returns a log line.</summary>
    public static string RenderRawBatch(int start, int count, string onlyTheme = null)
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return "wrong project";
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return "unsaved scene open: " + UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name;
        var defs = NewThemeDefs().Where(d => onlyTheme == null || d.theme.ToString() == onlyTheme).Skip(start).Take(count).ToList();
        int done = 0;
        foreach (var d in defs)
        {
            string path = "Assets/Scenes/" + d.mapEnum + ".unity";
            if (!File.Exists(path)) continue;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Resolve(d.theme.ToString(), d.mapEnum, d.night, out _, out var c);
            RenderRaw(d.icon, c);
            done++;
        }
        return "raw " + done + " (" + string.Join(" ", defs.Select(d => d.icon)) + ")";
    }

    /// <summary>Grades every cached raw shot of the new themes (optionally one theme / one map) into the icons.</summary>
    public static string RegradeAll(string onlyTheme = null, string onlyMap = null) => RegradeAll(onlyTheme, onlyMap, true);

    /// <summary>Grades (and with refresh, reimports once at the end). Icons Unity has loaded can be memory-mapped
    /// (Win32 1224, "locked, old image kept"): restart Unity, then run this before anything shows the icons.</summary>
    public static string RegradeAll(string onlyTheme, string onlyMap, bool refresh)
    {
        int done = 0, missing = 0;
        foreach (var d in NewThemeDefs())
        {
            if (onlyTheme != null && d.theme.ToString() != onlyTheme) continue;
            if (onlyMap != null && d.mapEnum != onlyMap) continue;
            Resolve(d.theme.ToString(), d.mapEnum, d.night, out var g, out _);
            if (GradeIcon(d.icon, g)) done++; else missing++;
        }
        if (refresh) AssetDatabase.Refresh();
        return "graded " + done + ", no raw shot " + missing;
    }

    [MenuItem("Tools/Maps/Icons/Render New Theme Icons (opens every map)")]
    static void RenderAllMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        int n = NewThemeDefs().Count;
        for (int i = 0; i < n; i += 6) Debug.Log("[MapIconStudio] " + RenderRawBatch(i, 6));
        Debug.Log("[MapIconStudio] " + RegradeAll());
    }

    [MenuItem("Tools/Maps/Icons/Regrade New Theme Icons (from cached shots)")]
    static void RegradeMenu()
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return;
        Debug.Log("[MapIconStudio] " + RegradeAll());
    }

    public static void Reimport(string icon)
    {
        string ip = IconDir + icon + ".png";
        AssetDatabase.ImportAsset(ip, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(ip);
        if (imp != null && imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
    }
}
