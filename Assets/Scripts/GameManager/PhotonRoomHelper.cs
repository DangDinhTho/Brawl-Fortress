using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonRoomHelper : MonoBehaviourPunCallbacks
{
    public static PhotonRoomHelper instan;

    private void Awake()
    {
        instan = this;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
    }

    public void SetProperties(string property, object value)
    {
        Hashtable props = new Hashtable
            {
                {property, value},

            };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }
}
