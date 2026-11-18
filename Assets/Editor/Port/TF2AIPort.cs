using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-off helper for porting the Boss / Zombie modes from E:\UnityProjects\TF2_AI: copies a set of root assets and every
/// file they reference (by GUID) that Brawl Fortress does not have yet, keeping paths and GUIDs. Runs on a background
/// thread (the GUID scan of two big projects takes minutes); progress / result in %TEMP%\tf2ai_port_result.txt.
/// Scripts are listed, not copied. Run Assets > Refresh after it reports DONE.
/// </summary>
public static class TF2AIPort
{
    public const string SourceAssets = @"E:\UnityProjects\TF2_AI\Assets";
    public static string ResultFile => Path.Combine(Path.GetTempPath(), "tf2ai_port_result.txt");

    static readonly string[] BossRoots =
    {
        @"Resources\Weapons\HalePunch.prefab", @"Resources\Weapons\YetiPunch.prefab",
        @"Resources\Config\Characters\Hale.asset", @"Resources\Config\Characters\Yeti.asset",
        @"Resources\Config\Taunts\hale_taunt_1.asset", @"Resources\Config\Taunts\hale_taunt_2.asset",
        @"Resources\Config\Taunts\yeti_taunt_1.asset", @"Resources\Config\Taunts\yeti_taunt_2.asset",
    };

    [MenuItem("Tools/Port/Copy TF2_AI Boss Assets")]
    public static void CopyBossAssets() => StartCopy(BossRoots);

    public static void StartCopy(string[] relativeRoots)
    {
        string target = Application.dataPath.Replace('/', '\\');
        File.WriteAllText(ResultFile, "RUNNING");
        new Thread(() => Run(relativeRoots, target)) { IsBackground = true, Name = "TF2AIPort" }.Start();
        Debug.Log("[Port] copy started, result in " + ResultFile);
    }

    static Dictionary<string, string> GuidMap(string root)
    {
        var map = new Dictionary<string, string>();
        foreach (var meta in Directory.GetFiles(root, "*.meta", SearchOption.AllDirectories))
        {
            try
            {
                using (var r = new StreamReader(meta))
                    for (int i = 0; i < 3; i++)
                    {
                        var l = r.ReadLine(); if (l == null) break;
                        if (l.StartsWith("guid: ")) { map[l.Substring(6).Trim()] = meta.Substring(0, meta.Length - 5); break; }
                    }
            }
            catch { }
        }
        return map;
    }

    static void Run(string[] relativeRoots, string B)
    {
        var log = new List<string>();
        try
        {
            string A = SourceAssets;
            var aMap = GuidMap(A); var bMap = GuidMap(B);
            var textExt = new HashSet<string> { ".prefab", ".asset", ".mat", ".controller", ".overridecontroller", ".anim", ".mask", ".playable" };
            var visited = new HashSet<string>(); var need = new List<string>(); var q = new Queue<string>();
            foreach (var r in relativeRoots) q.Enqueue(Path.Combine(A, r));
            while (q.Count > 0)
            {
                var f = q.Dequeue();
                if (!visited.Add(f) || !File.Exists(f)) continue;
                if (File.Exists(B + f.Substring(A.Length))) continue;
                need.Add(f);
                if (!textExt.Contains(Path.GetExtension(f).ToLower())) continue;
                string t = File.ReadAllText(f);
                if (!t.StartsWith("%YAML")) continue;
                int i = 0;
                while ((i = t.IndexOf("guid: ", i)) >= 0)
                {
                    if (i + 38 > t.Length) break;
                    string g = t.Substring(i + 6, 32); i += 38;
                    if (bMap.ContainsKey(g)) continue;
                    if (aMap.TryGetValue(g, out var dep) && !visited.Contains(dep)) q.Enqueue(dep);
                }
            }
            int copied = 0; double mb = 0;
            foreach (var src in need)
            {
                string rel = src.Substring(A.Length + 1);
                if (src.EndsWith(".cs")) { log.Add("SCRIPT (not copied): " + rel); continue; }
                string dst = Path.Combine(B, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(src, dst);
                if (File.Exists(src + ".meta")) File.Copy(src + ".meta", dst + ".meta", true);
                string dA = Path.GetDirectoryName(src), dB = Path.GetDirectoryName(dst);
                while (dB.Length > B.Length && !File.Exists(dB + ".meta") && File.Exists(dA + ".meta"))
                {
                    File.Copy(dA + ".meta", dB + ".meta");
                    dA = Path.GetDirectoryName(dA); dB = Path.GetDirectoryName(dB);
                }
                copied++; mb += new FileInfo(src).Length / 1048576.0;
                log.Add("copied: " + rel);
            }
            log.Insert(0, "DONE copied " + copied + " files, " + mb.ToString("F1") + " MB");
        }
        catch (System.Exception ex) { log.Insert(0, "ERROR " + ex.Message); }
        File.WriteAllLines(ResultFile, log.ToArray());
    }
}
