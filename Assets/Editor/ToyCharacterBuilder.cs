using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds a toy (chibi) character from the data exported by Blender (Tools/CharArt/export/&lt;name&gt;.json: vertices in the
/// old skeleton's rest space, bone weights by bone name, bone scale table) onto a copy of the old character prefab:
/// same skeleton, same Animator, same clips. See ToyProportions.
/// </summary>
public static class ToyCharacterBuilder
{
    const string OutDir = "Assets/Art/Characters/ToyDemo";
    /// <summary>Whole-skeleton scale: the toy figure is 188 units tall in skeleton units, the old one ~97 in game.</summary>
    const float RootScale = 0.52f;

    [MenuItem("Tools/Characters/Build Toy Demoman")]
    public static void BuildDemoman()
    {
        Debug.Log(Build("ToyDemoman", "Assets/Resources/Characters/TPS/TPS_Demoman.prefab", "demo_morphs_low",
            "TPS_Demoman_Toy", new[] { "ToyGrenades", "ToyShoes" }));
    }

    const string SwitchMenu = "Tools/Characters/Use Toy Characters In Play Mode";

    [MenuItem(SwitchMenu)]
    static void ToggleSwitch() { EditorPrefs.SetBool(ToyCharacters.EditorSwitch, !EditorPrefs.GetBool(ToyCharacters.EditorSwitch, false)); }

    [MenuItem(SwitchMenu, true)]
    static bool ToggleSwitchValidate() { Menu.SetChecked(SwitchMenu, EditorPrefs.GetBool(ToyCharacters.EditorSwitch, false)); return true; }

    public static string Build(string name, string sourcePrefab, string refRenderer, string outPrefab, string[] bodyElementParts)
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return "wrong project";
        string jsonPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Tools/CharArt/export/" + name + ".json");
        JObject data = JObject.Parse(File.ReadAllText(jsonPath));

        GameObject root = PrefabUtility.LoadPrefabContents(sourcePrefab);
        try
        {
            var oldRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var refSmr = oldRenderers.First(s => s.name == refRenderer);
            Transform[] bones = refSmr.bones;
            Matrix4x4[] bind = refSmr.sharedMesh.bindposes;
            var boneIndex = new Dictionary<string, int>();
            for (int i = 0; i < bones.Length; i++) if (bones[i] != null) boneIndex[bones[i].name] = i;

            // Blender armature space -> the old mesh's space, fitted on the bones' rest positions
            var rest = (JObject)data["bones_rest"];
            var xs = new List<double[]>(); var ys = new List<Vector3>();
            foreach (var kv in rest)
            {
                if (!boneIndex.TryGetValue(kv.Key, out int bi)) continue;
                var a = (JArray)kv.Value;
                xs.Add(new[] { (double)a[0], (double)a[1], (double)a[2], 1.0 });
                ys.Add(bind[bi].inverse.GetColumn(3));
            }
            double[,] A = FitAffine(xs, ys, out double residual);
            var L = new Matrix4x4();
            for (int r = 0; r < 3; r++) for (int c = 0; c < 4; c++) L[r, c] = (float)A[r, c];
            L[3, 3] = 1;
            float det = new Matrix4x4(L.GetColumn(0), L.GetColumn(1), L.GetColumn(2), new Vector4(0, 0, 0, 1)).determinant;

            Texture2D texRed = AssetDatabase.LoadAssetAtPath<Texture2D>(OutDir + "/" + name + "_red.png");
            Texture2D texBlue = AssetDatabase.LoadAssetAtPath<Texture2D>(OutDir + "/" + name + "_blue.png");
            Material red = MakeMaterial(refSmr.sharedMaterials[0], texRed, OutDir + "/" + name + "_red.mat");
            Material blue = MakeMaterial(refSmr.sharedMaterials[0], texBlue, OutDir + "/" + name + "_blue.mat");

            foreach (var old in oldRenderers) old.gameObject.SetActive(false);

            var made = new Dictionary<string, GameObject>();
            var newRenderers = new List<Renderer>();
            int totalVerts = 0, totalTris = 0;
            foreach (JObject part in (JArray)data["parts"])
            {
                string pn = (string)part["name"];
                string meshPath = OutDir + "/" + name + "_" + pn + ".asset";
                // an existing asset is refilled through the Mesh API (CopySerialized leaves the editor drawing old data)
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                bool isNew = mesh == null;
                if (isNew) mesh = new Mesh();
                BuildMesh(mesh, part, L, det < 0, boneIndex, bones, bind);
                mesh.name = name + "_" + pn;
                if (isNew) AssetDatabase.CreateAsset(mesh, meshPath);
                else EditorUtility.SetDirty(mesh);
                totalVerts += mesh.vertexCount; totalTris += mesh.triangles.Length / 3;

                var go = new GameObject(pn);
                go.transform.SetParent(refSmr.transform.parent, false);
                go.transform.localPosition = refSmr.transform.localPosition;
                go.transform.localRotation = refSmr.transform.localRotation;
                go.transform.localScale = refSmr.transform.localScale;
                go.layer = refSmr.gameObject.layer;
                var smr = go.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = mesh;
                smr.bones = bones;
                smr.rootBone = refSmr.rootBone;
                smr.sharedMaterials = new[] { red };
                smr.updateWhenOffscreen = true;
                smr.shadowCastingMode = refSmr.shadowCastingMode;
                smr.receiveShadows = refSmr.receiveShadows;
                var mdc = go.AddComponent<MeshDynamicColor>();
                mdc.skinMesh = smr;
                mdc.redMats = new[] { red };
                mdc.blueMats = new[] { blue };
                made[pn] = go;
                newRenderers.Add(smr);
            }

            var lodGroup = root.GetComponent<LODGroup>();
            // one LOD that is never culled; the old group's size was measured on the old meshes (2.6 cm: the new
            // renderers fell under any threshold and were culled in Play mode), so it is measured again
            if (lodGroup != null)
            {
                lodGroup.SetLODs(new[] { new LOD(0f, newRenderers.ToArray()) });
                lodGroup.RecalculateBounds();
            }

            var rp = root.GetComponent<RemotePlayer>();
            rp.spine1 = null;
            for (int i = 0; i < bodyElementParts.Length && i < rp.bodyElements.Length; i++)
                rp.bodyElements[i].lods = new[] { made[bodyElementParts[i]] };

            var toy = root.GetComponent<ToyProportions>() ?? root.AddComponent<ToyProportions>();
            Transform skeleton = refSmr.rootBone.parent;
            toy.skeletonRoot = skeleton;
            toy.skeletonRootScale = skeleton.localScale * RootScale;
            var all = root.GetComponentsInChildren<Transform>(true);
            var tb = new List<Transform>(); var ts = new List<float>();
            foreach (var kv in (JObject)data["scales"])
            {
                var t = all.FirstOrDefault(x => x.name == kv.Key);
                if (t == null) continue;
                tb.Add(t); ts.Add((float)kv.Value);
            }
            // every bone of the chain is in the table (1 when the json does not list it): the old prefab keeps
            // bip_spine_1 at 1.5 and a clip without scale curves would leave that in
            foreach (string n in new[] { "bip_pelvis", "bip_spine_0", "bip_spine_1", "bip_spine_2", "bip_spine_3", "bip_neck", "bip_head" })
            {
                var t = all.FirstOrDefault(x => x.name == n);
                if (t == null) continue;
                if (!tb.Contains(t)) { tb.Add(t); ts.Add(1f); }
            }
            toy.bones = tb.ToArray(); toy.scales = ts.ToArray();
            for (int i = 0; i < tb.Count; i++) tb[i].localScale = Vector3.one * ts[i];
            toy.uprightBone = all.FirstOrDefault(x => x.name == "bip_spine_0");
            toy.uprightTo = all.FirstOrDefault(x => x.name == "bip_neck");
            toy.neckBone = toy.uprightTo;
            if (boneIndex.TryGetValue("bip_head", out int hi) && boneIndex.TryGetValue("bip_pelvis", out int pi))
            {
                Matrix4x4 restHead = bind[hi].inverse;
                Vector3 up = ((Vector3)restHead.GetColumn(3) - (Vector3)bind[pi].inverse.GetColumn(3)).normalized;
                toy.headBone = bones[hi];
                toy.headUpLocal = restHead.inverse.MultiplyVector(up).normalized;
            }
            // the prefab's own transforms are the rest pose: what "upright" means for this figure
            if (toy.uprightBone != null && toy.uprightTo != null)
                toy.uprightRestDir = root.transform.InverseTransformDirection(toy.uprightTo.position - toy.uprightBone.position).normalized;
            // rest local rotations straight from the prefab (its stored pose is the rest pose)
            var spines = new[] { "bip_spine_1", "bip_spine_2", "bip_spine_3" }.Select(n => all.FirstOrDefault(x => x.name == n)).Where(x => x != null).ToArray();
            toy.spineBones = spines; toy.spineRest = spines.Select(x => x.localRotation).ToArray();
            if (toy.neckBone != null) toy.neckRest = toy.neckBone.localRotation;
            var collars = new[] { "bip_collar_L", "bip_collar_R" }.Select(n => all.FirstOrDefault(x => x.name == n)).Where(x => x != null).ToArray();
            toy.collarBones = collars; toy.collarRest = collars.Select(x => x.localRotation).ToArray();
            toy.weaponPivot = all.FirstOrDefault(x => x.name == "weapon_bone");
            if (toy.weaponPivot != null)
                toy.weaponBones = all.Where(x => x.parent == toy.weaponPivot.parent && x.name.StartsWith("weapon_bone")).ToArray();
            toy.weaponBonesOther = all.Where(x => x.name == "weapon_bone_L" || x.name == "weapon_targe").ToArray();
            toy.gripOnWeapon = MeasureGrip(sourcePrefab);
            toy.gripUpperArm = all.FirstOrDefault(x => x.name == "bip_upperArm_L");
            toy.gripLowerArm = all.FirstOrDefault(x => x.name == "bip_lowerArm_L");
            toy.gripHand = all.FirstOrDefault(x => x.name == "bip_hand_L");
            toy.holdUpperArm = all.FirstOrDefault(x => x.name == "bip_upperArm_R");
            toy.holdLowerArm = all.FirstOrDefault(x => x.name == "bip_lowerArm_R");
            toy.holdHand = all.FirstOrDefault(x => x.name == "bip_hand_R");
            toy.keepOrientation = new[] { "bip_upperArm_L", "bip_upperArm_R" }
                .Select(n => all.FirstOrDefault(x => x.name == n)).Where(x => x != null).ToArray();

            string outPath = OutDir + "/" + outPrefab + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, outPath);
            AssetDatabase.SaveAssets();
            return $"built {outPath}: {newRenderers.Count} parts, {totalVerts} verts, {totalTris} tris, fit points {xs.Count}, residual {residual:E2}, det {det:E2}, scales {tb.Count}";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>Where the old character's left hand holds the two-handed weapon: left hand in the grip bone's space, in
    /// the right hand's units, on its stand clip.</summary>
    static Vector3 MeasureGrip(string sourcePrefab)
    {
        var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefab));
        go.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var animator = go.GetComponent<Animator>();
            var clip = animator != null && animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.IndexOf("stand_PRIMARY", StringComparison.OrdinalIgnoreCase) >= 0) : null;
            var all = go.GetComponentsInChildren<Transform>(true);
            var pivot = all.FirstOrDefault(x => x.name == "weapon_bone"); var left = all.FirstOrDefault(x => x.name == "bip_hand_L");
            if (clip == null || pivot == null || left == null || pivot.parent == null) return Vector3.zero;
            clip.SampleAnimation(go, 0f);
            float unit = pivot.parent.lossyScale.x;
            return unit > 1e-6f ? Quaternion.Inverse(pivot.rotation) * (left.position - pivot.position) / unit : Vector3.zero;
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    static Material MakeMaterial(Material source, Texture2D tex, string path)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(source);
            AssetDatabase.CreateAsset(m, path);
        }
        else
        {
            m.shader = source.shader;
            m.CopyPropertiesFromMaterial(source);
        }
        m.mainTexture = tex;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void BuildMesh(Mesh mesh, JObject part, Matrix4x4 L, bool flip, Dictionary<string, int> boneIndex, Transform[] bones, Matrix4x4[] bind)
    {
        float[] pos = part["pos"].Select(v => (float)v).ToArray();
        float[] nrm = part["nrm"].Select(v => (float)v).ToArray();
        float[] uv = part["uv"].Select(v => (float)v).ToArray();
        int[] tri = part["tri"].Select(v => (int)v).ToArray();
        string[] names = part["bones"].Select(v => (string)v).ToArray();
        int[] map = new int[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            if (!boneIndex.TryGetValue(names[i], out map[i]))
            {
                // a bone the old mesh does not use: fall back to its nearest parent that it does
                Transform t = bones.Where(b => b != null).SelectMany(b => b.GetComponentsInChildren<Transform>(true)).FirstOrDefault(x => x.name == names[i]);
                while (t != null && !boneIndex.ContainsKey(t.name)) t = t.parent;
                map[i] = t != null ? boneIndex[t.name] : 0;
                Debug.LogWarning("[ToyCharacterBuilder] bone " + names[i] + " is not in the old mesh, using " + (t != null ? t.name : "bone 0"));
            }
        }
        int n = pos.Length / 3;
        var verts = new Vector3[n]; var normals = new Vector3[n]; var uvs = new Vector2[n]; var bw = new BoneWeight[n];
        var weights = (JArray)part["bw"];
        for (int i = 0; i < n; i++)
        {
            verts[i] = L.MultiplyPoint3x4(new Vector3(pos[i * 3], pos[i * 3 + 1], pos[i * 3 + 2]));
            normals[i] = L.MultiplyVector(new Vector3(nrm[i * 3], nrm[i * 3 + 1], nrm[i * 3 + 2])).normalized;
            uvs[i] = new Vector2(uv[i * 2], uv[i * 2 + 1]);
            var w = (JArray)weights[i];
            var b = new BoneWeight();
            for (int k = 0; k < w.Count && k < 4; k++)
            {
                int bi = map[(int)w[k][0]]; float wt = (float)w[k][1];
                if (k == 0) { b.boneIndex0 = bi; b.weight0 = wt; }
                else if (k == 1) { b.boneIndex1 = bi; b.weight1 = wt; }
                else if (k == 2) { b.boneIndex2 = bi; b.weight2 = wt; }
                else { b.boneIndex3 = bi; b.weight3 = wt; }
            }
            bw[i] = b;
        }
        if (flip) for (int i = 0; i < tri.Length; i += 3) { int t = tri[i + 1]; tri[i + 1] = tri[i + 2]; tri[i + 2] = t; }
        mesh.Clear();
        mesh.indexFormat = n > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = verts; mesh.normals = normals; mesh.uv = uvs; mesh.triangles = tri;
        mesh.boneWeights = bw; mesh.bindposes = bind;
        mesh.RecalculateBounds();
    }

    /// <summary>Least squares y = A [x 1] (A is 3 x 4), normal equations solved in double precision.</summary>
    static double[,] FitAffine(List<double[]> xs, List<Vector3> ys, out double residual)
    {
        var N = new double[4, 4]; var B = new double[3, 4];
        for (int i = 0; i < xs.Count; i++)
        {
            double[] x = xs[i]; double[] y = { ys[i].x, ys[i].y, ys[i].z };
            for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) N[r, c] += x[r] * x[c];
            for (int r = 0; r < 3; r++) for (int c = 0; c < 4; c++) B[r, c] += y[r] * x[c];
        }
        double[,] inv = Invert4(N);
        var A = new double[3, 4];
        for (int r = 0; r < 3; r++) for (int c = 0; c < 4; c++) for (int k = 0; k < 4; k++) A[r, c] += B[r, k] * inv[k, c];
        residual = 0;
        for (int i = 0; i < xs.Count; i++)
        {
            double[] y = { ys[i].x, ys[i].y, ys[i].z };
            for (int r = 0; r < 3; r++)
            {
                double p = 0; for (int c = 0; c < 4; c++) p += A[r, c] * xs[i][c];
                residual = Math.Max(residual, Math.Abs(p - y[r]));
            }
        }
        return A;
    }

    static double[,] Invert4(double[,] m)
    {
        int n = 4; var a = new double[n, 2 * n];
        for (int i = 0; i < n; i++) { for (int j = 0; j < n; j++) a[i, j] = m[i, j]; a[i, n + i] = 1; }
        for (int c = 0; c < n; c++)
        {
            int p = c;
            for (int r = c + 1; r < n; r++) if (Math.Abs(a[r, c]) > Math.Abs(a[p, c])) p = r;
            for (int j = 0; j < 2 * n; j++) { double t = a[c, j]; a[c, j] = a[p, j]; a[p, j] = t; }
            double d = a[c, c];
            for (int j = 0; j < 2 * n; j++) a[c, j] /= d;
            for (int r = 0; r < n; r++)
            {
                if (r == c) continue;
                double f = a[r, c];
                for (int j = 0; j < 2 * n; j++) a[r, j] -= f * a[c, j];
            }
        }
        var inv = new double[n, n];
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) inv[i, j] = a[i, n + j];
        return inv;
    }

    /// <summary>Renders the toy prefab in a pose of one of its clips, optionally holding a workshop item, to a PNG.
    /// Nothing is left in the scene.</summary>
    public static string Preview(string prefabPath, string clipContains, float normalizedTime, bool blueTeam, string itemId, string outPng,
        float yaw = 205f, int size = 900, float pitch = 12f, string[] ignoreItemBones = null)
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return "wrong project";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var go = UnityEngine.Object.Instantiate(prefab, new Vector3(0, 2000, 0), Quaternion.identity);
        go.hideFlags = HideFlags.HideAndDontSave;
        GameObject item = null; GameObject camGo = null; RenderTexture rt = null;
        string log = "";
        try
        {
            var animator = go.GetComponent<Animator>();
            var clips = animator.runtimeAnimatorController.animationClips;
            var clip = string.IsNullOrEmpty(clipContains) ? null : clips.FirstOrDefault(c => c.name.IndexOf(clipContains, StringComparison.OrdinalIgnoreCase) >= 0);
            if (clip != null) { clip.SampleAnimation(go, clip.length * normalizedTime); log += "clip " + clip.name; }
            else if (string.IsNullOrEmpty(clipContains)) log += "rest pose";
            else log += "no clip '" + clipContains + "' among: " + string.Join(", ", clips.Select(c => c.name).Distinct().Take(40));
            var toy = go.GetComponent<ToyProportions>();
            if (toy != null) toy.Apply();
            var rp = go.GetComponent<RemotePlayer>();
            if (rp != null && rp.spine1 != null && toy == null) rp.spine1.localScale = Vector3.one * 1.5f;
            Team team = blueTeam ? Team.blue : Team.red;
            foreach (var mdc in go.GetComponentsInChildren<MeshDynamicColor>(false))
            {
                var mats = blueTeam ? mdc.blueMats : mdc.redMats;
                if (mdc.skinMesh != null && mats != null && mats.Length > 0) mdc.skinMesh.sharedMaterials = mats;
            }
            if (!string.IsNullOrEmpty(itemId))
            {
                var itemPrefab = Resources.Load<GameObject>("WorkShop/Items/" + itemId);
                if (itemPrefab != null)
                {
                    item = UnityEngine.Object.Instantiate(itemPrefab, go.transform);
                    item.hideFlags = HideFlags.HideAndDontSave;
                    item.transform.localPosition = Vector3.zero; item.transform.localRotation = Quaternion.identity;
                    var ab = item.GetComponent<AlignBones>();
                    if (ab != null && ignoreItemBones != null) ab.ignorBones.AddRange(ignoreItemBones);
                    if (ab != null) ab.Setup(rp.remoteBone);
                    log += ", item " + itemId;
                }
                else log += ", item not found " + itemId;
            }
            Bounds b = new Bounds(go.transform.position + Vector3.up, Vector3.one * 0.1f);
            foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                var baked = new Mesh(); r.BakeMesh(baked);
                var m = r.transform.localToWorldMatrix;
                // BakeMesh already applies the renderer's scale
                m = Matrix4x4.TRS(r.transform.position, r.transform.rotation, Vector3.one);
                foreach (var v in baked.vertices) b.Encapsulate(m.MultiplyPoint3x4(v));
                UnityEngine.Object.DestroyImmediate(baked);
            }
            log += $", bounds size {b.size.ToString("F2")} min y {(b.min.y - go.transform.position.y):F2}";
            camGo = new GameObject("ToyPreviewCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.36f, 0.42f, 0.52f);
            cam.orthographic = true; cam.orthographicSize = Mathf.Max(b.extents.y, b.extents.x, b.extents.z) * 1.25f;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 40f;
            Vector3 dir = Quaternion.Euler(pitch, yaw, 0) * Vector3.forward;
            cam.transform.position = b.center - dir * 15f; cam.transform.rotation = Quaternion.LookRotation(dir);
            rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0); tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(outPng));
            File.WriteAllBytes(outPng, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return log + " -> " + outPng;
        }
        finally
        {
            if (camGo != null) { camGo.GetComponent<Camera>().targetTexture = null; UnityEngine.Object.DestroyImmediate(camGo); }
            if (rt != null) rt.Release();
            if (item != null) UnityEngine.Object.DestroyImmediate(item);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
