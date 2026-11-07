using System;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineerBlueprint : MonoBehaviour
{
    public Blueprint[] allBlueprints;
    public Dictionary<BuildableType, Blueprint> blueprints = new Dictionary<BuildableType, Blueprint>();
    private TF2Player player;
    [HideInInspector]
    public BuildableType currBuildable;

    private Dictionary<BuildableType, BuildableEntry> builded = new Dictionary<BuildableType, BuildableEntry>();
    private bool isMoving = false;
    private void Awake()
    {
        player = transform.root.GetComponent<TF2Player>();
        for(int i = 0; i < allBlueprints.Length; i++)
        {
            blueprints.Add(allBlueprints[i].buildableType, allBlueprints[i]);
            allBlueprints[i].gameObject.SetActive(false);
        }
        currBuildable = BuildableType.None;
    }


    public void SelectBuild(BuildableType type)
    {
        currBuildable = type;
        blueprints[type].gameObject.SetActive(true);
    }

    public void DiedOnMoving(BuildableType type)
    {
        blueprints[type].gameObject.SetActive(false);
        isMoving = false;
    }
    public void SelectMove(BuildableType type)
    {
        blueprints[type].gameObject.SetActive(true);
        isMoving = true;
    }

    public PosRot Drop(BuildableType type)
    {
        if (blueprints[type].reject) return null;
        blueprints[type].gameObject.SetActive(false);
        player.audioSource.PlayOneShot(blueprints[type].goOnUpVoice);
        return new PosRot(blueprints[type].transform.position, blueprints[type].transform.rotation);
        isMoving = false;
    }

    public BuildableEntry Build()
    {
        if (currBuildable == BuildableType.None || blueprints[currBuildable].reject) return null;
        object[] info = {player.viewID};
        if (currBuildable == BuildableType.TeleporterEnter &&
            builded.TryGetValue(BuildableType.TeleporterExit, out var exitTeleport))
        {
            if (exitTeleport == null)
            {
                builded.Remove(BuildableType.TeleporterExit);
            }
            else
            {
                info = new object[] {player.viewID, exitTeleport.photonView.ViewID};
            }
        }
        if (currBuildable == BuildableType.TeleporterExit &&
            builded.TryGetValue(BuildableType.TeleporterEnter, out var enterTeleport))
        {
            if (enterTeleport == null)
            {
                builded.Remove(BuildableType.TeleporterEnter);
            }
            else
            {
                info = new object[] {player.viewID, enterTeleport.photonView.ViewID};
            }
        }

        player.audioSource.PlayOneShot(blueprints[currBuildable].goOnUpVoice);
        GameObject obj = PhotonNetwork.Instantiate($"Buildables/{currBuildable}", blueprints[currBuildable].transform.position, blueprints[currBuildable].transform.rotation, 0, info);
        blueprints[currBuildable].gameObject.SetActive(false);
        if (builded.ContainsKey(currBuildable)) builded.Remove(currBuildable);
        builded.Add(currBuildable, obj.GetComponent<BuildableEntry>());
        currBuildable = BuildableType.None;
        return obj.GetComponent<BuildableEntry>();
    }

    public void Cancel()
    {
        if(currBuildable == BuildableType.None) return;
        blueprints[currBuildable].gameObject.SetActive(false);
        currBuildable = BuildableType.None;
        isMoving = false;
    }
}

public class PosRot
{
    public Vector3 positon;
    public Quaternion rotation;

    public PosRot(Vector3 _pos, Quaternion _rot)
    {
        this.positon = _pos;
        this.rotation = _rot;
    }
}
