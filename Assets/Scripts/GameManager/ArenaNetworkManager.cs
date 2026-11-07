using System;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
using UnityEngine;
using Random = UnityEngine.Random;

public class ArenaNetworkManager : MonoBehaviourPunCallbacks
{
    public static ArenaNetworkManager instan;
    public PhotonView photonView;
    private int idStack;
    public event Action OnSwitchMasterPlayer;

    public Dictionary<int, GameObject> kits = new Dictionary<int, GameObject>();

    private void Awake()
    {
        instan = this;
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        OnSwitchMasterPlayer?.Invoke();
    }

    public void EndGame(int redScore, int blueScore)
    {
        RPC_EndGame(redScore, blueScore);
        SyncEndGame(redScore, blueScore);
    }

    public void DropKit(Vector3 position)
    {
        KitType kitType = (KitType)Random.Range(1, 3);
        
        int ranSize = Random.Range(0, 10);
        if (ranSize == 9)
        {
            ranSize = 2;
        }
        else
        {
            ranSize = ranSize % 2;
        }

        KitSize size = (KitSize)ranSize;

        SyncDropKit(kitType, size, idStack, position);
        RPC_DropKit(kitType, size, idStack, position);
        
    }

    public void PickupKit(int id)
    {
        RPC_PickupKit(id);
        SyncPickupKit(id);
    }
    
    
    private void SyncEndGame(int redScore, int blueScore) => photonView.RPC(nameof(RPC_EndGame), RpcTarget.Others, redScore, blueScore);
    [PunRPC]
    void RPC_EndGame(int redScore, int blueScore)
    {
        ArenaManager.Instance.EndGame(redScore, blueScore);
    }

    private void SyncDropKit(KitType kitType, KitSize kitSize, int id, Vector3 position) => photonView.RPC(nameof(RPC_DropKit), RpcTarget.Others, kitType, kitSize, id, position);
    [PunRPC]
    void RPC_DropKit(KitType kitType, KitSize kitSize, int id, Vector3 position)
    {
        if (kits.ContainsKey(id)) return;
        GameObject kit = Instantiate(Resources.Load<GameObject>($"Kits/{kitType}_{kitSize}"), position, Quaternion.identity);
        kit.GetComponent<KitBase>().id = id;
        kits.Add(id, kit);
        idStack = id + 1;
    }

    private void SyncPickupKit(int id) => photonView.RPC(nameof(RPC_PickupKit), RpcTarget.Others, id);
    [PunRPC]
    void RPC_PickupKit(int id)
    {
        if (kits.ContainsKey(id))
        {
            Destroy(kits[id]);
            kits.Remove(id);
        }
    }
    
    public void SendChat(string content, bool chatAll)
    {
        photonView.RPC(nameof(RPC_Chat), RpcTarget.All, content, chatAll);
    }

    [PunRPC]
    void RPC_Chat(string content, bool chatAll, PhotonMessageInfo info)
    {
        UICharacterInputController.instan.OnReceiveChat(info.Sender, content, chatAll);
    }
}


public enum KitType: byte
{
    None = 0,
    AmmoPack,
    Medkit
}

public enum KitSize : byte
{
    Small = 0,
    Medium,
    Large
}