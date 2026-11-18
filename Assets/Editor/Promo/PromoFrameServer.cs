using System.IO;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Serves Recordings/Promo/frames on http://localhost:8765/ (localhost only), so the browser can pick up rendered start
/// frames and clips (preview, upload to a video tool). Tools > Promo > Frame Server toggles it; it stops on domain reload.
/// </summary>
public static class PromoFrameServer
{
    const string Menu = "Tools/Promo/Frame Server (localhost:8765)";
    static HttpListener http;

    public static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Recordings", "Promo", "frames");
    public static bool Running => http != null && http.IsListening;

    [MenuItem(Menu, false, 20)]
    public static void Toggle()
    {
        if (Running) Stop(); else Start();
    }

    [MenuItem(Menu, true)]
    static bool ToggleValidate() { Menu_SetChecked(); return true; }
    static void Menu_SetChecked() => UnityEditor.Menu.SetChecked(Menu, Running);

    public static void Start()
    {
        if (Running) return;
        Directory.CreateDirectory(Dir);
        http = new HttpListener();
        http.Prefixes.Add("http://localhost:8765/");
        http.Start();
        var listener = http;
        new Thread(() => Loop(listener)) { IsBackground = true, Name = "PromoFrameServer" }.Start();
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        Debug.Log("[Promo] Frame server on http://localhost:8765/ serving " + Dir);
    }

    public static void Stop()
    {
        if (http == null) return;
        try { http.Stop(); http.Close(); } catch { }
        http = null;
        AssemblyReloadEvents.beforeAssemblyReload -= Stop;
    }

    static void Loop(HttpListener listener)
    {
        while (listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = listener.GetContext(); } catch { break; }
            try
            {
                var res = ctx.Response;
                res.AddHeader("Access-Control-Allow-Origin", "*");
                res.AddHeader("Access-Control-Allow-Private-Network", "true");
                res.AddHeader("Access-Control-Allow-Headers", "*");
                res.AddHeader("Cache-Control", "no-store");
                if (ctx.Request.HttpMethod == "OPTIONS") { res.StatusCode = 204; res.Close(); continue; }
                string name = Path.GetFileName(ctx.Request.Url.AbsolutePath);
                string file = Path.Combine(Dir, name);
                if (string.IsNullOrEmpty(name) || !File.Exists(file)) { res.StatusCode = 404; res.Close(); continue; }
                byte[] data = File.ReadAllBytes(file);
                res.ContentType = name.EndsWith(".png") ? "image/png" : name.EndsWith(".mp4") ? "video/mp4" : "image/jpeg";
                res.ContentLength64 = data.Length;
                res.OutputStream.Write(data, 0, data.Length);
                res.Close();
            }
            catch { }
        }
    }
}
