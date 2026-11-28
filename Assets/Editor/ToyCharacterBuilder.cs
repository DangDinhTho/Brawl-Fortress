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

    [MenuItem("Tools/Characters/Build Toy Heavy")]
    public static void BuildHeavy()
    {
        Debug.Log(Build("ToyHeavy", "Assets/Resources/Characters/TPS/TPS_Heavy.prefab", "heavy_morphs_low",
            "TPS_Heavy_Toy", new[] { "ToyHands" }));
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
            // every character's json may carry its own whole-skeleton scale (the Heavy's mesh is 155 units tall so the old
            // pelvis sits at the new hips; the Demoman's is 188 with the default)
            float rootScale = data["root_scale"] != null ? (float)data["root_scale"] : RootScale;
            toy.skeletonRootScale = skeleton.localScale * rootScale;
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
            // hip offsets: given in Blender armature space (the posed figure), stored in the parent bone's space
            var ob = new List<Transform>(); var ov = new List<Vector3>();
            if (data["offsets"] is JObject offs)
            {
                foreach (var kv in offs)
                {
                    var t = all.FirstOrDefault(x => x.name == kv.Key);
                    if (t == null || t.parent == null || !boneIndex.TryGetValue(t.parent.name, out int pi2)) continue;
                    var a = (JArray)kv.Value;
                    Vector3 d = L.MultiplyVector(new Vector3((float)a[0], (float)a[1], (float)a[2]));
                    float parentScale = 1f;
                    for (Transform q = t.parent; q != null && q != skeleton; q = q.parent) { int qi = tb.IndexOf(q); if (qi >= 0) parentScale *= ts[qi]; }
                    ob.Add(t); ov.Add(bind[pi2].MultiplyVector(d) / parentScale);
                }
            }
            toy.offsetBones = ob.ToArray(); toy.offsets = ov.ToArray();
            toy.uprightBone = all.FirstOrDefault(x => x.name == "bip_spine_0");
            toy.uprightTo = all.FirstOrDefault(x => x.name == "bip_neck");
            toy.neckBone = toy.uprightTo;
            if (boneIndex.TryGetValue("bip_head", out int hi) && boneIndex.TryGetValue("bip_pelvis", out int pi))
            {
                Matrix4x4 restHead = bind[hi].inverse;
                Vector3 up = ((Vector3)restHead.GetColumn(3) - (Vector3)bind[pi].inverse.GetColumn(3)).normalized;
                toy.headBone = bones[hi];
                toy.headUpLocal = restHead.inverse.MultiplyVector(up).normalized;
                toy.pelvisBone = bones[pi];
                toy.pelvisUpLocal = bind[pi].MultiplyVector(up).normalized;
                toy.hipBones = new[] { "bip_hip_L", "bip_hip_R" }.Select(n => all.FirstOrDefault(x => x.name == n)).Where(x => x != null).ToArray();
            }
            // the prefab's own transforms are the rest pose: what "upright" means for this figure
            if (toy.uprightBone != null && toy.uprightTo != null)
                toy.uprightRestDir = root.transform.InverseTransformDirection(toy.uprightTo.position - toy.uprightBone.position).normalized;
            // rest local rotations straight from the prefab (its stored pose is the rest pose)
            var spines = new[] { "bip_spine_1", "bip_spine_2", "bip_spine_3" }.Select(n => all.FirstOrDefault(x => x.name == n)).Where(x => x != null).ToArray();
            toy.spineBones = spines; toy.spineRest = spines.Select(x => x.localRotation).ToArray();
            var waist = new[] { "bip_spine_0", "bip_spine_1" }.Select(n => all.FirstOrDefault(x => x.name == n)).Where(x => x != null).ToArray();
            toy.waistBones = waist; toy.waistRest = waist.Select(x => x.localRotation).ToArray();
            toy.upperBackBone = all.FirstOrDefault(x => x.name == "bip_spine_2");
            toy.palmLocal = Vector3.zero;
            if (data["hand_axes"] is JObject axes && axes["bip_hand_L"] is JObject leftHand && boneIndex.TryGetValue("bip_hand_L", out int lhi))
            {
                var pv = (JArray)leftHand["palm"];
                toy.palmLocal = bind[lhi].MultiplyVector(L.MultiplyVector(new Vector3((float)pv[0], (float)pv[1], (float)pv[2]))).normalized;
            }
            toy.blinkRenderer = made.TryGetValue("ToyBody", out GameObject bodyGo) ? bodyGo.GetComponent<SkinnedMeshRenderer>() : null;
            if (toy.neckBone != null) toy.neckRest = toy.neckBone.localRotation;
            var collars = new[] { "bip_collar_L", "bip_collar_R" }.Select(n => all.FirstOrDefault(x => x.name == n)).Where(x => x != null).ToArray();
            toy.collarBones = collars; toy.collarRest = collars.Select(x => x.localRotation).ToArray();
            var knees = new[] { "bip_knee_L", "bip_knee_R" }.Select(n => all.FirstOrDefault(x => x.name == n)).Where(x => x != null).ToArray();
            toy.kneeBones = knees; toy.kneeRest = knees.Select(x => x.localRotation).ToArray();
            toy.weaponPivot = all.FirstOrDefault(x => x.name == "weapon_bone");
            if (toy.weaponPivot != null)
                toy.weaponBones = all.Where(x => x.parent == toy.weaponPivot.parent && x.name.StartsWith("weapon_bone")).ToArray();
            toy.weaponBonesOther = all.Where(x => x.name == "weapon_bone_L" || x.name == "weapon_targe").ToArray();
            toy.gripOnWeapon = MeasureGrip(sourcePrefab, "stand_PRIMARY", out toy.gripDown);
            toy.gripOnWeaponMenu = MeasureGrip(sourcePrefab, "selectionmenu_idle", out toy.gripDownMenu);
            MeasureGripPerClip(sourcePrefab, toy);
            toy.gripUpperArm = all.FirstOrDefault(x => x.name == "bip_upperArm_L");
            toy.gripCollar = all.FirstOrDefault(x => x.name == "bip_collar_L");
            toy.gripLowerArm = all.FirstOrDefault(x => x.name == "bip_lowerArm_L");
            toy.gripHand = all.FirstOrDefault(x => x.name == "bip_hand_L");
            toy.holdUpperArm = all.FirstOrDefault(x => x.name == "bip_upperArm_R");
            toy.holdLowerArm = all.FirstOrDefault(x => x.name == "bip_lowerArm_R");
            toy.holdHand = all.FirstOrDefault(x => x.name == "bip_hand_R");
            if (toy.holdHand != null) toy.rightHandRest = toy.holdHand.localRotation;
            toy.leftArmRest = new[] { toy.gripUpperArm, toy.gripLowerArm, toy.gripHand }.Select(x => x != null ? x.localRotation : Quaternion.identity).ToArray();
            toy.keepOrientation = new[] { "bip_upperArm_L", "bip_upperArm_R" }
                .Select(n => all.FirstOrDefault(x => x.name == n)).Where(x => x != null).ToArray();

            // per-character values of ToyProportions fields (Tools/CharArt/export/<name>.tuning.json, field name -> value)
            // on top of the script's defaults, which are the Demoman's: the Heavy holds the minigun's carry handle from
            // above, so the palm-up / hand-forward tuning of the Demoman's launcher is switched off for him
            string tuningPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Tools/CharArt/export/" + name + ".tuning.json");
            if (File.Exists(tuningPath)) JsonUtility.FromJsonOverwrite(File.ReadAllText(tuningPath), toy);

            // where the fingers close: the middle of the first finger joints, on the old meshes and on the new ones
            var oldMeshes = oldRenderers.Where(x => x.sharedMesh != null).Select(x => (x.sharedMesh, x.bones)).ToList();
            var newMeshes = newRenderers.OfType<SkinnedMeshRenderer>().Select(x => (x.sharedMesh, x.bones)).ToList();
            // the new meshes are measured as the toy skeleton poses them (scales and offsets on): their rest space is
            // the old hand's by construction
            GripCentre(oldMeshes, "R", false, out toy.holdGripOld); GripCentre(oldMeshes, "L", false, out toy.gripGripOld);
            for (int i = 0; i < toy.offsetBones.Length; i++) toy.offsetBones[i].localPosition += toy.offsets[i];
            GripCentre(newMeshes, "R", true, out toy.holdGripMesh); GripCentre(newMeshes, "L", true, out toy.gripGripMesh);
            // the left hand's own axes from its knuckles (index -> ring = across the hand, wrist -> knuckles = along it)
            {
                Transform ix = all.FirstOrDefault(x => x.name == "bip_index_0_L"), rg = all.FirstOrDefault(x => x.name == "bip_ring_0_L"), md = all.FirstOrDefault(x => x.name == "bip_middle_0_L");
                if (ix != null && rg != null && md != null && toy.gripHand != null)
                {
                    Vector3 across = (rg.localPosition - ix.localPosition).normalized;
                    toy.handAcross = across;
                    Vector3 along = Vector3.ProjectOnPlane(toy.gripGripMesh, across).normalized;
                    Quaternion handAxes = Quaternion.LookRotation(along, Vector3.Cross(along, -across)); // z = fingers, x = -across... see below
                    // target: fingers +Z, across -X  ->  up = Z x (-X) = -Y... the hand's "up" (cross(along, -across)) goes to cross(Z, X) = +Y
                    toy.handleHandRot = Quaternion.LookRotation(Vector3.forward, Vector3.Cross(Vector3.forward, Vector3.right)) * Quaternion.Inverse(handAxes);
                    Debug.Log("[ToyCharacterBuilder] handle grip: palm goes to " + (toy.handleHandRot * toy.palmLocal).ToString("F2") + " (wanted about (0, -1, 0)), across to " + (toy.handleHandRot * across).ToString("F2"));
                }
            }
            for (int i = 0; i < toy.offsetBones.Length; i++) toy.offsetBones[i].localPosition -= toy.offsets[i];

            // the distances that tell "left hand on the gun" from "free hand" depend on the rig: measured on this one
            FitGripThresholds(root, toy);

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
    /// <summary>Sets the hold thresholds from the distance left hand - grip bone that this rig's two-handed stand clip
    /// gives (hand units), keeping the ratios tuned on the Demoman (0.0073 -> near 0.0095, far 0.0115, ...).</summary>
    static void FitGripThresholds(GameObject root, ToyProportions toy)
    {
        var go = UnityEngine.Object.Instantiate(root);
        go.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var animator = go.GetComponent<Animator>();
            var t = go.GetComponent<ToyProportions>();
            if (animator == null || animator.runtimeAnimatorController == null || t == null || t.weaponPivot == null || t.gripHand == null) return;
            var clips = animator.runtimeAnimatorController.animationClips;
            var clip = clips.FirstOrDefault(c => c.name.IndexOf("stand_PRIMARY", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? clips.FirstOrDefault(c => c.name.IndexOf("stand_SECONDARY", StringComparison.OrdinalIgnoreCase) >= 0);
            if (clip == null) return;
            clip.SampleAnimation(go, 0f);
            t.leftHandGrip = 0f; t.Apply();
            float unit = t.weaponPivot.parent.lossyScale.x;
            if (unit <= 1e-6f) return;
            float d = (t.gripHand.position - t.weaponPivot.position).magnitude / unit;
            if (d < 0.0005f) return;
            toy.gripAt = d * 0.97f;
            toy.gripWindow = d * 0.08f;
            toy.gripFade = d * 0.11f;
            toy.gripNear = d * 1.3f;
            toy.gripFar = d * 1.57f;
            toy.gripStill = d * 0.82f;
            Debug.Log("[ToyCharacterBuilder] two-handed distance on this rig " + d.ToString("F4") + " (" + clip.name + ")");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    /// <summary>The left hand's place on the gun for every clip of the old character that holds a weapon with both hands
    /// (hands as close as on the two-handed stand clip, give or take a third).</summary>
    static void MeasureGripPerClip(string sourcePrefab, ToyProportions toy)
    {
        var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefab));
        go.hideFlags = HideFlags.HideAndDontSave;
        var names = new List<string>(); var on = new List<Vector3>(); var downs = new List<Vector3>(); var rots = new List<Quaternion>();
        try
        {
            var animator = go.GetComponent<Animator>();
            var all = go.GetComponentsInChildren<Transform>(true);
            var pivot = all.FirstOrDefault(x => x.name == "weapon_bone"); var left = all.FirstOrDefault(x => x.name == "bip_hand_L");
            if (animator == null || animator.runtimeAnimatorController == null || pivot == null || left == null || pivot.parent == null) return;
            float reference = toy.gripOnWeapon.magnitude;
            foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
            {
                if (clip == null || names.Contains(clip.name)) continue;
                clip.SampleAnimation(go, 0f);
                float unit = pivot.parent.lossyScale.x;
                if (unit <= 1e-6f) continue;
                Vector3 world = left.position - pivot.position;
                Vector3 local = Quaternion.Inverse(pivot.rotation) * world / unit;
                if (local.magnitude < 0.0005f || reference <= 0f || local.magnitude > reference * 1.33f || local.magnitude < reference * 0.67f) continue;
                Vector3 up = go.transform.up;
                rots.Add(Quaternion.Inverse(pivot.rotation) * left.rotation);
                names.Add(clip.name); on.Add(local); downs.Add(Quaternion.Inverse(pivot.rotation) * (up * Vector3.Dot(world, up)) / unit);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        toy.gripClips = names.ToArray(); toy.gripClipOnWeapon = on.ToArray(); toy.gripClipDown = downs.ToArray(); toy.gripClipHandRot = rots.ToArray();
    }

    static Vector3 MeasureGrip(string sourcePrefab, string clipName, out Vector3 down)
    {
        down = Vector3.zero;
        var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefab));
        go.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var animator = go.GetComponent<Animator>();
            var clip = animator != null && animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.IndexOf(clipName, StringComparison.OrdinalIgnoreCase) >= 0) : null;
            var all = go.GetComponentsInChildren<Transform>(true);
            var pivot = all.FirstOrDefault(x => x.name == "weapon_bone"); var left = all.FirstOrDefault(x => x.name == "bip_hand_L");
            if (clip == null || pivot == null || left == null || pivot.parent == null) return Vector3.zero;
            clip.SampleAnimation(go, 0f);
            float unit = pivot.parent.lossyScale.x;
            if (unit <= 1e-6f) return Vector3.zero;
            Vector3 world = left.position - pivot.position;
            Vector3 up = go.transform.up;
            down = Quaternion.Inverse(pivot.rotation) * (up * Vector3.Dot(world, up)) / unit;
            return Quaternion.Inverse(pivot.rotation) * world / unit;
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    static bool GripCentre(List<(Mesh mesh, Transform[] bones)> meshes, string side, bool posed, out Vector3 centre)
    {
        centre = Vector3.zero; int count = 0;
        var fingers = new[] { "bip_index_0_", "bip_middle_0_", "bip_ring_0_", "bip_pinky_0_" }.Select(x => x + side).ToArray();
        foreach (var (mesh, bones) in meshes)
        {
            int hand = Array.FindIndex(bones, b => b != null && b.name == "bip_hand_" + side);
            if (hand < 0) continue;
            var isFinger = bones.Select(b => b != null && fingers.Contains(b.name)).ToArray();
            if (!isFinger.Any(x => x)) continue;
            var binds = mesh.bindposes;
            Matrix4x4 toHand = binds[hand];
            var vs = mesh.vertices; var bw = mesh.boneWeights;
            Matrix4x4[] skin = null;
            if (posed)
            {
                Matrix4x4 handInv = bones[hand].worldToLocalMatrix;
                skin = new Matrix4x4[bones.Length];
                for (int b = 0; b < bones.Length; b++) if (bones[b] != null) skin[b] = handInv * bones[b].localToWorldMatrix * binds[b];
            }
            for (int i = 0; i < vs.Length; i++)
            {
                float w = (isFinger[bw[i].boneIndex0] ? bw[i].weight0 : 0f) + (isFinger[bw[i].boneIndex1] ? bw[i].weight1 : 0f)
                        + (isFinger[bw[i].boneIndex2] ? bw[i].weight2 : 0f) + (isFinger[bw[i].boneIndex3] ? bw[i].weight3 : 0f);
                if (w < 0.5f) continue;
                if (!posed) centre += toHand.MultiplyPoint3x4(vs[i]);
                else centre += skin[bw[i].boneIndex0].MultiplyPoint3x4(vs[i]) * bw[i].weight0 + skin[bw[i].boneIndex1].MultiplyPoint3x4(vs[i]) * bw[i].weight1
                             + skin[bw[i].boneIndex2].MultiplyPoint3x4(vs[i]) * bw[i].weight2 + skin[bw[i].boneIndex3].MultiplyPoint3x4(vs[i]) * bw[i].weight3;
                count++;
            }
        }
        if (count == 0) return false;
        centre /= count;
        return true;
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
        if (part["shapes"] is JObject shapes)
        {
            foreach (var kv in shapes)
            {
                int[] idx = kv.Value["idx"].Select(v => (int)v).ToArray();
                float[] d = kv.Value["d"].Select(v => (float)v).ToArray();
                var deltas = new Vector3[n];
                for (int i = 0; i < idx.Length; i++) deltas[idx[i]] = L.MultiplyVector(new Vector3(d[i * 3], d[i * 3 + 1], d[i * 3 + 2]));
                mesh.AddBlendShapeFrame(kv.Key, 100f, deltas, null, null);
            }
        }
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

    /// <summary>Rebuilds the toy Demoman and renders the standard set of check poses to Tools/CharArt/preview/d3/&lt;prefix&gt;_N.png.</summary>
    public static string DemomanCheck(string prefix, bool rebuild = true)
    {
        if (!Application.dataPath.Contains("Brawl Fortress")) return "wrong project";
        string log = rebuild ? Build("ToyDemoman", "Assets/Resources/Characters/TPS/TPS_Demoman.prefab", "demo_morphs_low", "TPS_Demoman_Toy", new[] { "ToyGrenades", "ToyShoes" }) : "";
        string p = OutDir + "/TPS_Demoman_Toy.prefab";
        string dir = Directory.GetParent(Application.dataPath).FullName + "/Tools/CharArt/preview/d3/";
        string[] clips = { "stand_PRIMARY", "stand_PRIMARY", "stand_PRIMARY", "stand_SECONDARY", "stand_MELEE", "runN_PRIMARY", "runN_PRIMARY", "runN_PRIMARY", "runN_PRIMARY", "selectionmenu_idle", "selectionmenu_idle", "jumpfloat_PRIMARY" };
        float[] t = { 0f, 0f, 0f, 0f, 0f, 0f, 0.25f, 0.5f, 0.75f, 0f, 0f, 0.5f };
        float[] yaw = { 270f, 215f, 180f, 215f, 215f, 270f, 270f, 215f, 180f, 215f, 270f, 215f };
        string[] item = { "GrenadeLauncher", "GrenadeLauncher", "GrenadeLauncher", "StickybombLauncher", "Bottle", null, null, null, null, "GrenadeLauncher", null, null };
        for (int i = 0; i < clips.Length; i++)
            Preview(p, clips[i], t[i], false, item[i], dir + prefix + "_" + i + ".png", yaw[i], 800, 3f);
        return log;
    }

    /// <summary>Renders the toy prefab in a pose of one of its clips, optionally holding a workshop item, to a PNG.
    /// Nothing is left in the scene.</summary>
    public static string Preview(string prefabPath, string clipContains, float normalizedTime, bool blueTeam, string itemId, string outPng,
        float yaw = 205f, int size = 900, float pitch = 12f, string[] ignoreItemBones = null,
        string baseState = null, string armState = null, float armTime = 0f)
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
            if (!string.IsNullOrEmpty(baseState))
            {
                // the animator itself: a base state plus a state of the additive arm layer (firing, reloading)
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.Rebind();
                animator.Play(baseState, 0, normalizedTime);
                if (!string.IsNullOrEmpty(armState)) animator.Play(armState, 1, armTime);
                animator.Update(0.0001f);
                log += " states " + baseState + " + " + armState;
            }
            var toy = go.GetComponent<ToyProportions>();
            if (toy != null) { toy.clipHint = clip; toy.Apply(); }
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

    // ---------------------------------------------------------------------------------------------------------
    // Retargeted rig (trial, 28 Nov 2026): the bones sit where they are on the model (no scale table), the clips
    // are baked for that skeleton in Blender (Tools/CharArt/toy_retarget.py) and arrive as bone matrices per frame.

    [MenuItem("Tools/Characters/Build Toy Heavy (retargeted rig)")]
    public static void BuildHeavyRig()
    {
        Debug.Log(BuildRig("ToyHeavyRig", "ToyHeavy", "Assets/Resources/Characters/TPS/TPS_Heavy.prefab", "heavy_morphs_low",
            "TPS_Heavy_Toy", new[] { "ToyHands" }));
    }

    /// <summary>Indices of the keys to keep: first, last, and every key the straight line between its kept
    /// neighbours would miss (off(a, m, z): is key m off the line a - z?).</summary>
    static List<int> Thin(int count, Func<int, int, int, bool> off)
    {
        var keep = new List<int> { 0 };
        if (count < 2) return keep;
        int a = 0;
        while (a < count - 1)
        {
            int z = a + 1;
            // push the far end as long as every key in between stays on the line
            while (z + 1 < count)
            {
                bool ok = true;
                for (int m = a + 1; m <= z && ok; m++) ok = !off(a, m, z + 1);
                if (!ok) break;
                z++;
            }
            keep.Add(z); a = z;
        }
        return keep;
    }

    static AnimationCurve LinearCurve(List<int> keep, float fps, Func<int, float> value)
    {
        var keys = new Keyframe[keep.Count];
        for (int i = 0; i < keep.Count; i++) keys[i] = new Keyframe(keep[i] / fps, value(keep[i]));
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
        return curve;
    }

    /// <summary>The take's name of a clip: "heavy.qc_skeleton|stand_PRIMARY" and "demo.qc_skeleton_stand_PRIMARY"
    /// (a clip copied out of its FBX) are both "stand_PRIMARY".</summary>
    static string ClipKey(string clipName)
    {
        int bar = clipName.LastIndexOf('|');
        if (bar >= 0) return clipName.Substring(bar + 1);
        int at = clipName.IndexOf("qc_skeleton_", StringComparison.Ordinal);
        return at >= 0 ? clipName.Substring(at + "qc_skeleton_".Length) : clipName;
    }

    static Matrix4x4 ReadMatrix(JToken t)
    {
        var a = (JArray)t; var m = Matrix4x4.identity;
        for (int r = 0; r < 4; r++) for (int col = 0; col < 4; col++) m[r, col] = (float)a[r * 4 + col];
        return m;
    }

    [MenuItem("Tools/Characters/Build Toy Demoman (retargeted rig)")]
    public static void BuildDemomanRig()
    {
        Debug.Log(BuildRig("ToyDemomanRig", "ToyDemoman", "Assets/Resources/Characters/TPS/TPS_Demoman.prefab", "demo_morphs_low",
            "TPS_Demoman_Toy", new[] { "ToyGrenades", "ToyShoes" }));
    }

    static Matrix4x4 Rot3(Matrix4x4 m)
    {
        var r = Matrix4x4.identity;
        for (int col = 0; col < 3; col++)
        {
            Vector3 v = m.GetColumn(col); v.Normalize();
            r.SetColumn(col, new Vector4(v.x, v.y, v.z, 0));
        }
        return r;
    }

    static void SetLocal(Transform t, Matrix4x4 local)
    {
        Matrix4x4 r = Rot3(local);
        t.localPosition = local.GetColumn(3);
        t.localRotation = Quaternion.LookRotation(r.GetColumn(2), r.GetColumn(1));
        t.localScale = Vector3.one;
    }

    public static string BuildRig(string name, string textureName, string sourcePrefab, string refRenderer, string outPrefab, string[] bodyElementParts)
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
            Matrix4x4[] bindOld = refSmr.sharedMesh.bindposes;
            var boneIndex = new Dictionary<string, int>();
            for (int i = 0; i < bones.Length; i++) if (bones[i] != null) boneIndex[bones[i].name] = i;

            // Blender armature space -> the old mesh's space, fitted on the bones' rest positions
            var xs = new List<double[]>(); var ys = new List<Vector3>();
            foreach (var kv in (JObject)data["bones_rest"])
            {
                if (!boneIndex.TryGetValue(kv.Key, out int bi)) continue;
                var a = (JArray)kv.Value;
                xs.Add(new[] { (double)a[0], (double)a[1], (double)a[2], 1.0 });
                ys.Add(bindOld[bi].inverse.GetColumn(3));
            }
            double[,] A = FitAffine(xs, ys, out double residual);
            var L = new Matrix4x4();
            for (int r = 0; r < 3; r++) for (int col = 0; col < 4; col++) L[r, col] = (float)A[r, col];
            L[3, 3] = 1;
            var L3 = new Matrix4x4(L.GetColumn(0), L.GetColumn(1), L.GetColumn(2), new Vector4(0, 0, 0, 1));
            float det = L3.determinant;
            float unit = Mathf.Pow(Mathf.Abs(det), 1f / 3f);
            var L3n = Matrix4x4.identity;
            for (int col = 0; col < 3; col++) L3n.SetColumn(col, L3.GetColumn(col) / unit);

            // every transform of the skeleton by name; its rest frame in the renderer's space, from the prefab's own pose
            Transform skeleton = refSmr.rootBone.parent;
            var all = skeleton.GetComponentsInChildren<Transform>(true).Where(t => t != skeleton).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
            // the old prefabs may carry a saved bone scale (TPS_Demoman: bip_spine_1 at 1.5, the old "big chest"); the
            // retargeted rig has the model's own proportions and its clips key no scale, so it would stay for good
            foreach (Transform t in all.Values) if (t.name.StartsWith("bip_")) t.localScale = Vector3.one;
            Matrix4x4 toModel = refSmr.transform.worldToLocalMatrix;
            var oldRest = (JObject)data["old_rest"]; var rigRest = (JObject)data["rig_rest"];
            var corr = new Dictionary<string, Matrix4x4>();
            float poseDeviation = 0f;
            foreach (var kv in all)
            {
                if (oldRest[kv.Key] == null) continue;
                Matrix4x4 u = toModel * kv.Value.localToWorldMatrix;
                if (boneIndex.TryGetValue(kv.Key, out int bi))
                {
                    Matrix4x4 fromBind = bindOld[bi].inverse;
                    poseDeviation = Mathf.Max(poseDeviation, Quaternion.Angle(Quaternion.LookRotation(Rot3(u).GetColumn(2), Rot3(u).GetColumn(1)), Quaternion.LookRotation(Rot3(fromBind).GetColumn(2), Rot3(fromBind).GetColumn(1))));
                    u = fromBind;
                }
                // bone frame in Unity = (L * Blender frame) * corr, a constant per bone
                corr[kv.Key] = (L3n * Rot3(ReadMatrix(oldRest[kv.Key]))).inverse * Rot3(u);
            }
            Func<string, Matrix4x4, Matrix4x4> conv = (bone, n) =>
            {
                Matrix4x4 m = L3n * Rot3(n) * corr[bone];
                Vector3 pos = L.MultiplyPoint3x4(n.GetColumn(3));
                m.SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1));
                return m;
            };

            // check: the old skeleton's raw pose of the stand clip, converted, against what Unity plays
            string check = "";
            if (data["check_old"] is JObject checkOld)
            {
                var probe = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefab));
                probe.hideFlags = HideFlags.HideAndDontSave;
                try
                {
                    var pa = probe.GetComponent<Animator>();
                    string checkClip = data["check_clip"] != null ? (string)data["check_clip"] : "stand_PRIMARY";
                    var pc = pa.runtimeAnimatorController.animationClips.First(x => string.Equals(ClipKey(x.name), checkClip, StringComparison.OrdinalIgnoreCase));
                    pc.SampleAnimation(probe, 0f);
                    var pr = probe.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(x => x.name == refRenderer);
                    foreach (var kv in checkOld)
                    {
                        var t = probe.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == kv.Key);
                        if (t == null || !corr.ContainsKey(kv.Key)) continue;
                        Matrix4x4 played = pr.transform.worldToLocalMatrix * t.localToWorldMatrix;
                        Matrix4x4 mine = conv(kv.Key, ReadMatrix(kv.Value));
                        float ang = Quaternion.Angle(Quaternion.LookRotation(Rot3(played).GetColumn(2), Rot3(played).GetColumn(1)), Quaternion.LookRotation(mine.GetColumn(2), mine.GetColumn(1)));
                        check += $" {kv.Key} {((Vector3)played.GetColumn(3) - (Vector3)mine.GetColumn(3)).magnitude / unit:F2}u/{ang:F1}deg";
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(probe); }
            }

            // new rest frames and bind poses
            var rest = new Dictionary<string, Matrix4x4>();
            foreach (var kv in rigRest) if (corr.ContainsKey(kv.Key)) rest[kv.Key] = conv(kv.Key, ReadMatrix(kv.Value));
            var bind = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
                bind[i] = bones[i] != null && rest.TryGetValue(bones[i].name, out Matrix4x4 rm) ? rm.inverse : bindOld[i];

            Texture2D texRed = AssetDatabase.LoadAssetAtPath<Texture2D>(OutDir + "/" + textureName + "_red.png");
            Texture2D texBlue = AssetDatabase.LoadAssetAtPath<Texture2D>(OutDir + "/" + textureName + "_blue.png");
            Material red = MakeMaterial(refSmr.sharedMaterials[0], texRed, OutDir + "/" + textureName + "_red.mat");
            Material blue = MakeMaterial(refSmr.sharedMaterials[0], texBlue, OutDir + "/" + textureName + "_blue.mat");
            foreach (var old in oldRenderers) old.gameObject.SetActive(false);

            var made = new Dictionary<string, GameObject>();
            var newRenderers = new List<Renderer>();
            int totalVerts = 0;
            foreach (JObject part in (JArray)data["parts"])
            {
                string pn = (string)part["name"];
                string meshPath = OutDir + "/" + name + "_" + pn + ".asset";
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                bool isNew = mesh == null;
                if (isNew) mesh = new Mesh();
                BuildMesh(mesh, part, L, det < 0, boneIndex, bones, bind);
                mesh.name = name + "_" + pn;
                if (isNew) AssetDatabase.CreateAsset(mesh, meshPath); else EditorUtility.SetDirty(mesh);
                totalVerts += mesh.vertexCount;
                var go = new GameObject(pn);
                go.transform.SetParent(refSmr.transform.parent, false);
                go.transform.localPosition = refSmr.transform.localPosition;
                go.transform.localRotation = refSmr.transform.localRotation;
                go.transform.localScale = refSmr.transform.localScale;
                go.layer = refSmr.gameObject.layer;
                var smr = go.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = mesh; smr.bones = bones; smr.rootBone = refSmr.rootBone;
                smr.sharedMaterials = new[] { red };
                smr.updateWhenOffscreen = true;
                smr.shadowCastingMode = refSmr.shadowCastingMode; smr.receiveShadows = refSmr.receiveShadows;
                var mdc = go.AddComponent<MeshDynamicColor>();
                mdc.skinMesh = smr; mdc.redMats = new[] { red }; mdc.blueMats = new[] { blue };
                made[pn] = go; newRenderers.Add(smr);
            }
            var lodGroup = root.GetComponent<LODGroup>();
            var rp = root.GetComponent<RemotePlayer>();
            rp.spine1 = null;
            for (int i = 0; i < bodyElementParts.Length && i < rp.bodyElements.Length; i++)
                rp.bodyElements[i].lods = new[] { made[bodyElementParts[i]] };

            // local frames: against the parent bone, or against whatever the root bone hangs under
            var parents = (JObject)data["parents"];
            Func<string, Dictionary<string, Matrix4x4>, Matrix4x4> local = (bone, frames) =>
            {
                string parent = (string)parents[bone];
                if (!string.IsNullOrEmpty(parent) && frames.TryGetValue(parent, out Matrix4x4 pm)) return pm.inverse * frames[bone];
                return (toModel * all[bone].parent.localToWorldMatrix).inverse * frames[bone];
            };
            // the root's parent matrix must be read before any bone moves
            var rootParent = new Dictionary<string, Matrix4x4>();
            foreach (var kv in rest) { string par = (string)parents[kv.Key]; if (string.IsNullOrEmpty(par) || !rest.ContainsKey(par)) rootParent[kv.Key] = (toModel * all[kv.Key].parent.localToWorldMatrix).inverse; }
            Func<string, Dictionary<string, Matrix4x4>, Matrix4x4> localOf = (bone, frames) =>
                rootParent.TryGetValue(bone, out Matrix4x4 rpm) ? rpm * frames[bone] : frames[(string)parents[bone]].inverse * frames[bone];

            var restLocal = rest.Keys.ToDictionary(k => k, k => localOf(k, rest));
            foreach (var kv in restLocal) SetLocal(all[kv.Key], kv.Value);
            if (lodGroup != null) { lodGroup.SetLODs(new[] { new LOD(0f, newRenderers.ToArray()) }); lodGroup.RecalculateBounds(); }

            // clips: <name>.clips.json lists them, <name>.clips.bin holds a 3x4 matrix per frame and bone (float32)
            string clipBase = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Tools/CharArt/export/" + name);
            JObject index = JObject.Parse(File.ReadAllText(clipBase + ".clips.json"));
            byte[] raw = File.ReadAllBytes(clipBase + ".clips.bin");
            var clipBones = ((JArray)index["bones"]).Select(x => (string)x).ToArray();
            float fps = (float)index["fps"];
            var paths = clipBones.ToDictionary(b => b, b => all.ContainsKey(b) ? AnimationUtility.CalculateTransformPath(all[b], root.transform) : null);
            var animator = root.GetComponent<Animator>();
            var baseController = animator.runtimeAnimatorController;
            while (baseController is AnimatorOverrideController oc) baseController = oc.runtimeAnimatorController;
            string overridePath = OutDir + "/" + name + ".overrideController";
            var over = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
            if (over == null) { over = new AnimatorOverrideController(baseController); AssetDatabase.CreateAsset(over, overridePath); }
            else over.runtimeAnimatorController = baseController;
            string clipDir = OutDir + "/" + name + "_Clips";
            if (!AssetDatabase.IsValidFolder(clipDir)) AssetDatabase.CreateFolder(OutDir, name + "_Clips");
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            int clipCount = 0; long keysKept = 0, keysAll = 0;
            var used = clipBones.Where(b => paths[b] != null && corr.ContainsKey(b)).ToArray();
            var boneAt = clipBones.Select((b, bi) => new { b, bi }).ToDictionary(x => x.b, x => x.bi);
            string[] props = { "localPosition.x", "localPosition.y", "localPosition.z", "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };
            var row = new float[clipBones.Length * 12];
            foreach (JObject entry in (JArray)index["clips"])
            {
                string clipName = (string)entry["name"];
                AnimationClip original = baseController.animationClips.FirstOrDefault(x => string.Equals(ClipKey(x.name), clipName, StringComparison.OrdinalIgnoreCase));
                if (original == null) continue; // a take the controller does not use
                int frameCount = (int)entry["frames"]; long offset = (long)entry["offset"];
                var posKeys = used.ToDictionary(b => b, b => new Vector3[frameCount]);
                var rotKeys = used.ToDictionary(b => b, b => new Quaternion[frameCount]);
                var world = new Dictionary<string, Matrix4x4>();
                for (int f = 0; f < frameCount; f++)
                {
                    Buffer.BlockCopy(raw, (int)((offset + (long)f * row.Length) * 4), row, 0, row.Length * 4);
                    world.Clear();
                    foreach (string b in clipBones)
                    {
                        if (!corr.ContainsKey(b)) continue;
                        int at = boneAt[b] * 12;
                        var m = Matrix4x4.identity;
                        for (int r = 0; r < 3; r++) for (int col = 0; col < 4; col++) m[r, col] = row[at + r * 4 + col];
                        world[b] = conv(b, m);
                    }
                    foreach (string b in used)
                    {
                        Matrix4x4 lm = localOf(b, world);
                        Matrix4x4 lr = Rot3(lm);
                        Quaternion q = Quaternion.LookRotation(lr.GetColumn(2), lr.GetColumn(1));
                        if (f > 0 && Quaternion.Dot(rotKeys[b][f - 1], q) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                        posKeys[b][f] = lm.GetColumn(3); rotKeys[b][f] = q;
                    }
                }
                string clipPath = clipDir + "/" + name + "_" + clipName + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
                clip.ClearCurves();
                clip.frameRate = fps;
                foreach (string b in used)
                {
                    // a key is kept only where dropping it would move the bone: straight-line keys between the kept ones
                    Vector3[] pk = posKeys[b]; Quaternion[] rk = rotKeys[b];
                    float posTolerance = 0.0005f * Mathf.Max(restLocal[b].GetColumn(3).magnitude, 0.0001f) + 1e-7f;
                    var keepPos = Thin(frameCount, (a, m2, z) => (pk[m2] - Vector3.Lerp(pk[a], pk[z], (m2 - a) / (float)(z - a))).magnitude > posTolerance);
                    var keepRot = Thin(frameCount, (a, m2, z) => Quaternion.Angle(rk[m2], Quaternion.Slerp(rk[a], rk[z], (m2 - a) / (float)(z - a))) > 0.25f);
                    keysAll += frameCount * 7L; keysKept += keepPos.Count * 3L + keepRot.Count * 4L;
                    for (int i = 0; i < 3; i++)
                    {
                        int axis = i;
                        clip.SetCurve(paths[b], typeof(Transform), props[i], LinearCurve(keepPos, fps, k => pk[k][axis]));
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        int axis = i;
                        clip.SetCurve(paths[b], typeof(Transform), props[3 + i], LinearCurve(keepRot, fps, k => rk[k][axis]));
                    }
                }
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                var originalSettings = AnimationUtility.GetAnimationClipSettings(original);
                settings.loopTime = originalSettings.loopTime; settings.loopBlend = originalSettings.loopBlend;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AnimationUtility.SetAnimationEvents(clip, AnimationUtility.GetAnimationEvents(original));
                clip.name = name + "_" + clipName;
                EditorUtility.SetDirty(clip);
                pairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(original, clip));
                clipCount++;
            }
            // clips of the controller that no take was baked for keep playing the OLD skeleton's offsets: they must be listed
            var baked = new HashSet<AnimationClip>(pairs.Select(x => x.Key));
            string unmatched = string.Join(", ", baseController.animationClips.Where(x => x != null && !baked.Contains(x)).Select(x => x.name).Distinct());
            if (unmatched.Length > 0) Debug.LogWarning("[ToyCharacterBuilder] " + name + ": no retargeted clip for " + unmatched);
            over.ApplyOverrides(pairs);
            EditorUtility.SetDirty(over);
            animator.runtimeAnimatorController = over;

            // runtime: no scale table, no posture or hand corrections; the whole skeleton's scale and the gun's size stay
            var toy = root.GetComponent<ToyProportions>() ?? root.AddComponent<ToyProportions>();
            toy.skeletonRoot = skeleton;
            float rootScale = data["root_scale"] != null ? (float)data["root_scale"] : RootScale;
            toy.skeletonRootScale = skeleton.localScale * rootScale;
            toy.bones = new Transform[0]; toy.scales = new float[0];
            toy.offsetBones = new Transform[0]; toy.offsets = new Vector3[0];
            // posture as on the scaled rig (less hunch at the waist, head up); the arms are NOT kept in place while the
            // torso is straightened: both hands and the gun turn with the chest as one piece
            toy.upright = 0f; toy.spineRelax = 0f; toy.shoulderRelax = 0f; toy.kneeRelax = 0f;
            toy.keepOrientation = new Transform[0];
            Func<string, Transform> bone = n => all.TryGetValue(n, out Transform found) ? found : null;
            toy.pelvisBone = bone("bip_pelvis"); toy.headBone = bone("bip_head"); toy.neckBone = bone("bip_neck");
            toy.uprightBone = bone("bip_spine_0"); toy.uprightTo = toy.neckBone;
            if (toy.pelvisBone != null && toy.headBone != null)
            {
                // the prefab now stands in its rest pose: "up" is the line pelvis - head
                Vector3 upWorld = (toy.headBone.position - toy.pelvisBone.position).normalized;
                toy.headUpLocal = toy.headBone.InverseTransformDirection(upWorld).normalized;
                toy.pelvisUpLocal = toy.pelvisBone.InverseTransformDirection(upWorld).normalized;
            }
            toy.hipBones = new[] { bone("bip_hip_L"), bone("bip_hip_R") }.Where(x => x != null).ToArray();
            toy.waistBones = new[] { bone("bip_spine_0"), bone("bip_spine_1") }.Where(x => x != null).ToArray();
            toy.waistRest = toy.waistBones.Select(x => x.localRotation).ToArray();
            toy.spineBones = new Transform[0]; toy.spineRest = new Quaternion[0];
            toy.collarBones = new Transform[0]; toy.collarRest = new Quaternion[0];
            toy.kneeBones = new Transform[0]; toy.kneeRest = new Quaternion[0];
            toy.upperBackBone = bone("bip_spine_2");
            if (toy.neckBone != null) toy.neckRest = toy.neckBone.localRotation;
            toy.gripUpperArm = null; toy.gripLowerArm = null; toy.gripHand = bone("bip_hand_L"); toy.gripCollar = null;
            toy.holdUpperArm = null; toy.holdLowerArm = null; toy.holdHand = bone("bip_hand_R");
            toy.leftArmRest = new Quaternion[0];
            string tuningPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Tools/CharArt/export/" + name + ".tuning.json");
            if (File.Exists(tuningPath)) JsonUtility.FromJsonOverwrite(File.ReadAllText(tuningPath), toy);
            toy.leftHandGrip = 0f; toy.wristMax = 90f; toy.handleGrip = 0f; toy.weaponLevel = 0f; toy.weaponTurnToHand = 0f;
            toy.weaponScale = data["gun_scale"] != null ? (float)data["gun_scale"] : 1f;
            toy.weaponPivot = all.ContainsKey("weapon_bone") ? all["weapon_bone"] : null;
            // the clips place every weapon bone themselves: only the size is set here
            toy.weaponBones = new Transform[0];
            toy.weaponBonesOther = all.Values.Where(x => x.name.StartsWith("weapon_bone") && !x.name.EndsWith("_end") || x.name == "weapon_targe").ToArray();
            toy.blinkRenderer = made.TryGetValue("ToyBody", out GameObject bodyGo) ? bodyGo.GetComponent<SkinnedMeshRenderer>() : null;

            string outPath = OutDir + "/" + outPrefab + ".prefab";
            // again right before saving: the scale saved in the old prefab comes back while the clips are written
            foreach (Transform t in all.Values) if (t.name.StartsWith("bip_")) t.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(root, outPath);
            AssetDatabase.SaveAssets();
            return $"built {outPath}: {totalVerts} verts, {rest.Count} bones, {clipCount} clips, NOT retargeted: [{unmatched}], keys {keysKept} of {keysAll}, fit residual {residual:E2}, unit {unit:F4}, prefab pose vs bind {poseDeviation:F1} deg, clip check:{check}";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
