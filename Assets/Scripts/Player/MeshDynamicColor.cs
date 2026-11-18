using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MeshDynamicColor : MonoBehaviour
{
    public SkinnedMeshRenderer skinMesh;
    public Material[] blueMats;
    public Material[] redMats;

    /// <summary>Editor helper: fills redMats / blueMats from the renderer. It never overwrites an assignment that is already
    /// complete (same count as the renderer, no empty slot), so hand-set team materials survive prefab saves. When it has to
    /// build the lists, the blue material is found next to the red one by replacing the 'red' token of its name anywhere
    /// (demoman_red_flat -> demoman_blue_flat, hvyweapon_red_sheen -> hvyweapon_blue_sheen, x_red -> x_blue / x_blu).</summary>
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;
        if (skinMesh == null) skinMesh = GetComponent<SkinnedMeshRenderer>();
        if (skinMesh == null) return;
        var shared = skinMesh.sharedMaterials;
        if (Complete(redMats, shared.Length) && Complete(blueMats, shared.Length)) return;

        redMats = shared;
        blueMats = new Material[redMats.Length];
        for (int i = 0; i < redMats.Length; i++) blueMats[i] = FindBlue(redMats[i]) ?? redMats[i];
#endif
    }

#if UNITY_EDITOR
    static bool Complete(Material[] mats, int count)
    {
        if (mats == null || mats.Length != count) return false;
        foreach (var m in mats) if (m == null) return false;
        return true;
    }

    static Material FindBlue(Material red)
    {
        if (red == null) return null;
        string path = AssetDatabase.GetAssetPath(red);
        if (string.IsNullOrEmpty(path)) return null;
        string dir = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = red.name;
        int i = name.ToLowerInvariant().LastIndexOf("red");
        if (i < 0) return null;
        foreach (var blue in new[] { "blue", "blu" })
        {
            string candidate = dir + "/" + name.Substring(0, i) + blue + name.Substring(i + 3) + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(candidate);
            if (m != null) return m;
        }
        return null;
    }
#endif

    // public void LoadMaterials()
    // {
    //     
    // }

    public void SetTeamColor(Team team)
    {
        if (team == Team.red)
        {
            skinMesh.materials = redMats;
        }
        else if(team == Team.blue)
        {
            skinMesh.materials = blueMats;
        }
    }

    public void SetInvisibility(Material[] material)
    {

        skinMesh.materials = material;

    }
}
