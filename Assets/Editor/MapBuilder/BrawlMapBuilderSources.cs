using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Source scenes for the map builder WITHOUT additive loading (Tho's rule, 2026-11-13: a map scene is always
// opened alone, OpenSceneMode.Single). Before the map is opened, each source scene is opened Single, the needed
// root objects are cloned (internal references remapped) into a hidden preview scene, and its lighting values
// are captured. The build code then reads them through Source(path) / sourceLights as if the scene were loaded.
// Preview scenes are not part of the Hierarchy, have their own physics scene and are closed at the end.
public static partial class BrawlMapBuilder
{
    class LightInfo
    {
        public UnityEngine.Rendering.AmbientMode mode; public Color sky, eq, gr, amb; public float ai;
        public Material box; public bool fog; public Color fogC; public FogMode fogM; public float fs, fe, fd;
        public bool hasLight; public Color lColor; public float lIntensity; public LightShadows lShadows;
        public float lShadowStrength; public Quaternion lRotation;
    }

    static readonly Dictionary<string, Scene> sourceScenes = new Dictionary<string, Scene>();
    static readonly Dictionary<string, LightInfo> sourceLights = new Dictionary<string, LightInfo>();

    static readonly string[] PayloadRoots = { "BezierCurves", "BombCart", "Target", "MegaExplosionYellow", "Main Camera", "MapManager" };
    static readonly string[] MvmRoots = { "BezierCurves", "BombTank", "AtomBomb", "Target", "MegaExplosionYellow", "BasicZoneBlue", "DeployCam", "MapManager" };

    static void GuardUnsavedScenes()
    {
        var mapPaths = new HashSet<string>(AllDefs().Select(x => "Assets/Scenes/" + x.mapEnum + ".unity"));
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.isDirty) continue;
            bool generated = mapPaths.Contains(s.path);                       // rebuilt by this tool anyway
            bool emptyUntitled = string.IsNullOrEmpty(s.path) && s.rootCount == 0;
            if (!generated && !emptyUntitled)
                throw new Exception("Scene '" + (string.IsNullOrEmpty(s.path) ? "Untitled" : s.path) + "' has unsaved changes. Save or discard them before building maps.");
        }
    }

    static void BeginSources() { EndSources(); }

    static void EndSources()
    {
        foreach (var s in sourceScenes.Values) if (s.IsValid()) EditorSceneManager.ClosePreviewScene(s);
        sourceScenes.Clear();
        sourceLights.Clear();
    }

    static Scene Source(string path)
    {
        if (!sourceScenes.TryGetValue(path, out var s)) throw new Exception("Source scene not prefetched: " + path);
        return s;
    }

    static string LightingScene(Theme theme) => theme switch
    {
        Theme.Western => "Assets/Scenes/BrawlScene.unity",
        Theme.Bamboo => "Assets/Scenes/BrawlScene_2.unity",
        Theme.Cave => "Assets/Scenes/Map_Brawl_Payload.unity",
        Theme.Moon => "Assets/Scenes/Map_Brawl_ShowDown.unity",
        _ => null,
    };

    static void PrefetchSources(Def d)
    {
        var need = new Dictionary<string, HashSet<string>>();
        void Need(string path, params string[] roots)
        {
            if (path == null) return;
            if (!need.TryGetValue(path, out var set)) need[path] = set = new HashSet<string>();
            foreach (var x in roots) set.Add(x);
        }
        Need(LightingScene(d.theme));
        if (d.theme == Theme.Moon) Need("Assets/Scenes/Map_Brawl_ShowDown.unity", "Environment");
        switch (d.mode)
        {
            case GameMode.CaptureTheFlag: Need("Assets/Scenes/Map_Brawl_CTF.unity", "BriefCase", "BriefCase (1)", "BriefCase_Highlight", "BriefCase (1)_Highlight"); break;
            case GameMode.KingOfTheHill: Need("Assets/Scenes/Map_Brawl_Koth.unity", "MapManager", "Point", "PointBase"); break;
            case GameMode.Payload: Need("Assets/Scenes/Map_Brawl_Payload.unity", PayloadRoots); break;
            case GameMode.MannVsMachine: Need("Assets/Scenes/Map_Brawl_MvM.unity", MvmRoots); break;
        }
        foreach (var kv in need) FetchSource(kv.Key, kv.Value);
    }

    static void FetchSource(string path, HashSet<string> roots)
    {
        var src = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SceneManager.SetActiveScene(src);
        sourceLights[path] = CaptureLight(src);

        var pv = EditorSceneManager.NewPreviewScene();
        sourceScenes[path] = pv;
        var map = new Dictionary<Object, Object>();
        var clones = new List<GameObject>();
        foreach (var o in src.GetRootGameObjects())
        {
            if (!roots.Contains(o.name)) continue;
            var c = Object.Instantiate(o);
            c.name = o.name;
            SceneManager.MoveGameObjectToScene(c, pv);
            var ot = o.GetComponentsInChildren<Transform>(true);
            var ct = c.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < ot.Length && i < ct.Length; i++)
            {
                map[ot[i].gameObject] = ct[i].gameObject;
                var oc = ot[i].GetComponents<Component>();
                var cc = ct[i].GetComponents<Component>();
                for (int k = 0; k < oc.Length && k < cc.Length; k++) if (oc[k] && cc[k]) map[oc[k]] = cc[k];
            }
            clones.Add(c);
        }
        foreach (var c in clones) foreach (var comp in c.GetComponentsInChildren<Component>(true)) Remap(comp, map);
        var missing = roots.Where(x => clones.All(c => c.name != x)).ToList();
        if (missing.Count > 0) log += " [no " + string.Join(",", missing) + " in " + System.IO.Path.GetFileNameWithoutExtension(path) + "]";
    }

    static LightInfo CaptureLight(Scene src)
    {
        var li = new LightInfo
        {
            mode = RenderSettings.ambientMode, sky = RenderSettings.ambientSkyColor, eq = RenderSettings.ambientEquatorColor,
            gr = RenderSettings.ambientGroundColor, amb = RenderSettings.ambientLight, ai = RenderSettings.ambientIntensity,
            box = RenderSettings.skybox, fog = RenderSettings.fog, fogC = RenderSettings.fogColor, fogM = RenderSettings.fogMode,
            fs = RenderSettings.fogStartDistance, fe = RenderSettings.fogEndDistance, fd = RenderSettings.fogDensity,
        };
        var l = src.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).FirstOrDefault(x => x.type == LightType.Directional);
        if (l)
        {
            li.hasLight = true; li.lColor = l.color; li.lIntensity = l.intensity; li.lShadows = l.shadows;
            li.lShadowStrength = l.shadowStrength; li.lRotation = l.transform.rotation;
        }
        return li;
    }
}
