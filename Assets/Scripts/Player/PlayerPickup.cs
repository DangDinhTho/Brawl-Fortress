using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    public TF2Player player;

    public virtual void OnTriggerEnter(Collider other)
    {
        if (player.healthManager.death) return;
        switch (other.tag)
        {
            case "Kit":
                KitBase kitController = other.transform.root.GetComponent<KitBase>();
                if (kitController.kitType == KitType.AmmoPack)
                {
                    player.FPSPlayer.BuffWeapon(0.5f, false);
                    print("buff");
                }
                else if (kitController.kitType == KitType.Medkit)
                {
                    //Buff Health
                    player.healthManager.Buff(kitController.value);
                }
                ArenaNetworkManager.instan.PickupKit(kitController.id);

                break;
            case "Flag":
                FlagController flagController = other.transform.root.GetComponent<FlagController>();
                if (flagController != null && flagController.team != player.team && flagController.state != FlagState.moving)
                {
                    flagController.Pickup(player);
                }
                break;
        }
    }
}
