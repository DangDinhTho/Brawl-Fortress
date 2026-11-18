using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Grades the textures of a class's Workshop items with EXACTLY the pixel mapping that was applied to that class's body
/// texture, so cosmetics that merge with the body (arms, skin, cloth) keep matching colours at the seams:
///   V' = newMean + (V - oldMean) * k,  S' = S * sk   (HSV, alpha kept).
/// Originals are kept; graded copies go to Assets/Art/BrightPort/ItemTextures/&lt;Class&gt;/&lt;name&gt;_bright.png with the
/// original import settings, and the item materials are pointed at them.
/// </summary>
public static class ItemTextureGrade
{
    public const string OutRoot = "Assets/Art/BrightPort/ItemTextures";

    public static string Grade(string cls, float oldMean, float newMean, float k, float sk)
    {
        var folders = new List<string>();
        foreach (var f in new[] { "Assets/Models/WorkShopItems/" + cls, "Assets/Models/WorkShopItems/Taunt/" + cls })
            if (AssetDatabase.IsValidFolder(f)) folders.Add(f);
        if (folders.Count == 0) return cls + ": no item folders";

        // item materials of this class and the textures they use
        var mats = new List<Material>();
        foreach (var g in AssetDatabase.FindAssets("t:Material", folders.ToArray()))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (m != null && m.HasProperty("_MainTex") && m.GetTexture("_MainTex") is Texture2D) mats.Add(m);
        }
        var outDir = OutRoot + "/" + cls;
        Directory.CreateDirectory(outDir);
        var map = new Dictionary<Texture2D, string>();
        foreach (var m in mats)
        {
            var t = (Texture2D)m.GetTexture("_MainTex");
            var tp = AssetDatabase.GetAssetPath(t);
            if (map.ContainsKey(t) || tp.StartsWith(OutRoot) || tp.Contains("BrightPort")) continue;
            map[t] = outDir + "/" + Path.GetFileNameWithoutExtension(tp) + "_bright.png";
        }

        int written = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var kv in map)
            {
                if (File.Exists(kv.Value)) continue;
                var src = kv.Key;
                var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(src)) as TextureImporter;
                int w = src.width, h = src.height;
                var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(src, rt);
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                t.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                var px = t.GetPixels32();
                for (int i = 0; i < px.Length; i++)
                {
                    Color c = px[i];
                    Color.RGBToHSV(c, out float hh, out float s, out float v);
                    v = Mathf.Clamp01(newMean + (v - oldMean) * k);
                    s = Mathf.Clamp01(s * sk);
                    var o = Color.HSVToRGB(hh, s, v); o.a = c.a;
                    px[i] = o;
                }
                t.SetPixels32(px); t.Apply();
                File.WriteAllBytes(kv.Value, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                written++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();

        // copy import settings, then repoint the materials
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var kv in map)
            {
                var srcImp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(kv.Key)) as TextureImporter;
                var dstImp = AssetImporter.GetAtPath(kv.Value) as TextureImporter;
                if (srcImp == null || dstImp == null) continue;
                var settings = new TextureImporterSettings();
                srcImp.ReadTextureSettings(settings);
                dstImp.SetTextureSettings(settings);
                dstImp.maxTextureSize = srcImp.maxTextureSize;
                dstImp.textureCompression = srcImp.textureCompression;
                foreach (var plat in new[] { "Android", "iPhone", "Standalone" })
                {
                    var ps = srcImp.GetPlatformTextureSettings(plat);
                    ps.name = plat;
                    dstImp.SetPlatformTextureSettings(ps);
                }
                dstImp.SaveAndReimport();
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }

        int repointed = 0;
        foreach (var m in mats)
        {
            var t = m.GetTexture("_MainTex") as Texture2D;
            if (t == null || !map.TryGetValue(t, out var dst)) continue;
            var nt = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
            if (nt == null) continue;
            m.SetTexture("_MainTex", nt);
            EditorUtility.SetDirty(m);
            repointed++;
        }
        AssetDatabase.SaveAssets();
        return cls + ": " + map.Count + " textures (" + written + " written), " + repointed + " materials repointed";
    }
    /// <summary>Weapons do not merge with skin, so each texture is graded on its own like the class textures were:
    /// mean value raised to at least 0.30, contrast up to x1.3 (towards a 0.28 spread). Effect textures are skipped.</summary>
    public static string GradeWeapons()
    {
        var folders = new[] { "Assets/Models/Weapon/Workshop", "Assets/TextureCompled/weapon", "Assets/Models/Weapon" };
        var mats = new List<Material>();
        foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Weapons" }))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.shader.name.StartsWith("FlatKit") && m.GetTexture("_MainTex") is Texture2D && !mats.Contains(m)) mats.Add(m);
            }
        }
        var outDir = OutRoot + "/Weapons";
        Directory.CreateDirectory(outDir);
        var map = new Dictionary<Texture2D, string>();
        foreach (var m in mats)
        {
            var t = (Texture2D)m.GetTexture("_MainTex");
            var tp = AssetDatabase.GetAssetPath(t);
            bool inFolder = false; foreach (var f in folders) if (tp.StartsWith(f + "/")) inFolder = true;
            if (!inFolder || map.ContainsKey(t) || tp.Contains("BrightPort")) continue;
            map[t] = outDir + "/" + Path.GetFileNameWithoutExtension(tp) + "_bright.png";
        }
        int written = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var kv in map)
            {
                if (File.Exists(kv.Value)) continue;
                var src = kv.Key; int w = src.width, h = src.height;
                var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(src, rt);
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                t.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                var px = t.GetPixels32();
                double sum = 0, sum2 = 0; int n = 0;
                for (int i = 0; i < px.Length; i += 3) { Color c = px[i]; if (c.a < 0.5f) continue; Color.RGBToHSV(c, out _, out _, out float v); sum += v; sum2 += v * v; n++; }
                float mean = n > 0 ? (float)(sum / n) : 0.5f, std = n > 0 ? Mathf.Sqrt(Mathf.Max(1e-6f, (float)(sum2 / n) - mean * mean)) : 0.25f;
                float newMean = Mathf.Max(mean, 0.30f), k = Mathf.Clamp(0.28f / std, 1f, 1.3f);
                for (int i = 0; i < px.Length; i++)
                {
                    Color c = px[i];
                    Color.RGBToHSV(c, out float hh, out float sat, out float v);
                    v = Mathf.Clamp01(newMean + (v - mean) * k);
                    var o = Color.HSVToRGB(hh, sat, v); o.a = c.a; px[i] = o;
                }
                t.SetPixels32(px); t.Apply();
                File.WriteAllBytes(kv.Value, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                written++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var kv in map)
            {
                var srcImp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(kv.Key)) as TextureImporter;
                var dstImp = AssetImporter.GetAtPath(kv.Value) as TextureImporter;
                if (srcImp == null || dstImp == null) continue;
                var settings = new TextureImporterSettings(); srcImp.ReadTextureSettings(settings); dstImp.SetTextureSettings(settings);
                dstImp.maxTextureSize = srcImp.maxTextureSize; dstImp.textureCompression = srcImp.textureCompression;
                foreach (var plat in new[] { "Android", "iPhone", "Standalone" }) { var ps = srcImp.GetPlatformTextureSettings(plat); ps.name = plat; dstImp.SetPlatformTextureSettings(ps); }
                dstImp.SaveAndReimport();
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        int repointed = 0;
        foreach (var m in mats)
        {
            var t = m.GetTexture("_MainTex") as Texture2D;
            if (t == null || !map.TryGetValue(t, out var dst)) continue;
            var nt = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
            if (nt == null) continue;
            m.SetTexture("_MainTex", nt); EditorUtility.SetDirty(m); repointed++;
        }
        AssetDatabase.SaveAssets();
        return "Weapons: " + map.Count + " textures (" + written + " written), " + repointed + " materials repointed";
    }
    /// <summary>Grades every FlatKit main texture used under a prefab folder (renderers AND the red / blue materials of
    /// MeshDynamicColor), each texture on its own like the weapons: mean value at least 0.30, contrast up to x1.3.
    /// Copies go to ItemTextures/&lt;outSub&gt;; the materials are repointed.</summary>
    public static string GradePrefabTextures(string prefabsRoot, string outSub)
    {
        var mats = new List<Material>();
        foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { prefabsRoot }))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                foreach (var m in r.sharedMaterials) if (m != null && !mats.Contains(m)) mats.Add(m);
            }
            foreach (var c in go.GetComponentsInChildren<MeshDynamicColor>(true))
            {
                if (c.redMats != null) foreach (var m in c.redMats) if (m != null && !mats.Contains(m)) mats.Add(m);
                if (c.blueMats != null) foreach (var m in c.blueMats) if (m != null && !mats.Contains(m)) mats.Add(m);
            }
        }
        mats.RemoveAll(m => !m.shader.name.StartsWith("FlatKit") || !(m.GetTexture("_MainTex") is Texture2D));
        var outDir = OutRoot + "/" + outSub;
        Directory.CreateDirectory(outDir);
        var map = new Dictionary<Texture2D, string>();
        foreach (var m in mats)
        {
            var t = (Texture2D)m.GetTexture("_MainTex");
            var tp = AssetDatabase.GetAssetPath(t);
            if (map.ContainsKey(t) || tp.Contains("BrightPort") || !tp.StartsWith("Assets/")) continue;
            map[t] = outDir + "/" + Path.GetFileNameWithoutExtension(tp) + "_bright.png";
        }
        int written = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var kv in map)
            {
                if (File.Exists(kv.Value)) continue;
                var src = kv.Key; int w = src.width, h = src.height;
                var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(src, rt);
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                t.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                var px = t.GetPixels32();
                double sum = 0, sum2 = 0; int n = 0;
                for (int i = 0; i < px.Length; i += 3) { Color c = px[i]; if (c.a < 0.5f) continue; Color.RGBToHSV(c, out _, out _, out float v); sum += v; sum2 += v * v; n++; }
                float mean = n > 0 ? (float)(sum / n) : 0.5f, std = n > 0 ? Mathf.Sqrt(Mathf.Max(1e-6f, (float)(sum2 / n) - mean * mean)) : 0.25f;
                float newMean = Mathf.Max(mean, 0.30f), k = Mathf.Clamp(0.28f / std, 1f, 1.3f);
                for (int i = 0; i < px.Length; i++)
                {
                    Color c = px[i];
                    Color.RGBToHSV(c, out float hh, out float sat, out float v);
                    v = Mathf.Clamp01(newMean + (v - mean) * k);
                    var o = Color.HSVToRGB(hh, sat, v); o.a = c.a; px[i] = o;
                }
                t.SetPixels32(px); t.Apply();
                File.WriteAllBytes(kv.Value, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                written++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var kv in map)
            {
                var srcImp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(kv.Key)) as TextureImporter;
                var dstImp = AssetImporter.GetAtPath(kv.Value) as TextureImporter;
                if (srcImp == null || dstImp == null) continue;
                var settings = new TextureImporterSettings(); srcImp.ReadTextureSettings(settings); dstImp.SetTextureSettings(settings);
                dstImp.maxTextureSize = srcImp.maxTextureSize; dstImp.textureCompression = srcImp.textureCompression;
                foreach (var plat in new[] { "Android", "iPhone", "Standalone" }) { var ps = srcImp.GetPlatformTextureSettings(plat); ps.name = plat; dstImp.SetPlatformTextureSettings(ps); }
                dstImp.SaveAndReimport();
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        int repointed = 0;
        foreach (var m in mats)
        {
            var t = m.GetTexture("_MainTex") as Texture2D;
            if (t == null || !map.TryGetValue(t, out var dst)) continue;
            var nt = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
            if (nt == null) continue;
            m.SetTexture("_MainTex", nt); EditorUtility.SetDirty(m); repointed++;
        }
        AssetDatabase.SaveAssets();
        return outSub + ": " + map.Count + " textures (" + written + " written), " + repointed + " materials repointed";
    }
}
