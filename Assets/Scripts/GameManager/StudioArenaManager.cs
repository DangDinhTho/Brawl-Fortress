using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class StudioArenaManager : MonoBehaviour
{
    public bool offlineMode = true;
    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.OfflineMode = offlineMode;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
