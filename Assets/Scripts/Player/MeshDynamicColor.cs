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

    private void OnValidate()
    {
        skinMesh = GetComponent<SkinnedMeshRenderer>();
        redMats = skinMesh.sharedMaterials;
        blueMats = new Material[redMats.Length];
        redMats.CopyTo(blueMats, 0);

#if UNITY_EDITOR
        for (int i = 0; i < blueMats.Length; i++)
        {
            if(blueMats[i].name.EndsWith("_red"))
            {
                string path = AssetDatabase.GetAssetPath(blueMats[i]);
                path = path.Remove(path.Length - 7) + "blue.mat";// path.Replace("_red", "_blue");
                Material material = (Material)AssetDatabase.LoadAssetAtPath(path,
                    typeof(Material));
                if (material != null)
                {
                    blueMats[i] = material;
                }
                
            }
        }
#endif
    }

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
