using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneCombiner
{
    private readonly Dictionary<int, Transform> _rootBoneDictionary = new Dictionary<int, Transform>();
    private readonly Transform[] _boneTransforms = new Transform[67];

    private readonly Transform _transform;

    public BoneCombiner(GameObject rootObj)
    {
        _transform = rootObj.transform;
        TraverseHierarchy(_transform);
    }



    public void AddLimb(GameObject bonedObj, List<string> boneNames)
    {
        ProcessBonedObject(bonedObj.GetComponentsInChildren<SkinnedMeshRenderer>(), boneNames);
        bonedObj.transform.SetParent(_transform);
        //return bonedObj.transform;
    }

    private void ProcessBonedObject(SkinnedMeshRenderer[] renderer, IReadOnlyList<string> boneNames)
    {
        //var bonedObject = new GameObject().transform;

        //var meshRenderer = renderer;// bonedObject.gameObject.AddComponent<SkinnedMeshRenderer>();

        //var bones = renderer.bones;
        for (var i = 0; i < boneNames.Count; i++)
        {
            _boneTransforms[i] = _rootBoneDictionary[boneNames[i].GetHashCode()];
        }

        for (int i = 0; i < renderer.Length; i++)
        {
            renderer[i].bones = _boneTransforms;
        }
        //meshRenderer.sharedMesh = renderer.sharedMesh;
        //meshRenderer.materials = renderer.sharedMaterials;

        //return bonedObject;
        //return renderer.transform.parent;

    }


    private void TraverseHierarchy(IEnumerable transform)
    {
        foreach (Transform child in transform)
        {
            if (_rootBoneDictionary.ContainsKey(child.name.GetHashCode())) continue;
            _rootBoneDictionary.Add(child.name.GetHashCode(), child);
            TraverseHierarchy(child);
        }
    }

}