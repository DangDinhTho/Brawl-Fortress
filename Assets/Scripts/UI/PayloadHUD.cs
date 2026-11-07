using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PayloadHUD : UIPlayerHUDBase
{
    public static PayloadHUD instan;

    private PayloadManager payloadManager;
    public UIDirection cartDirection;
   

    private void Awake()
    {
        instan = this;
    }

    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        payloadManager = PayloadManager.instance;
        if(ArenaManager.Instance.mainPlayer == null)
        {
            ArenaManager.Instance.OnMainPlayerSpawned += OnMainPlayerSpawned;
        }
        else
        {
            OnMainPlayerSpawned(ArenaManager.Instance.mainPlayer);
        }
    }

    public void OnMainPlayerSpawned(TF2Player mainPlayer)
    {
        cartDirection.target = payloadManager.bombCart;
        cartDirection.mainPlayer = ArenaManager.Instance.mainPlayer.transform;
        cartDirection.gameObject.SetActive(true);
    }
}
