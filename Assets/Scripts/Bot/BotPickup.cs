using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotPickup : MonoBehaviour
{
    public TF2Bot bot;

    public virtual void OnTriggerEnter(Collider other)
    {
        if (bot.healthManager.death) return;
        switch (other.tag)
        {
            case "Kit":
                int r = Random.Range(0, 3);
                if (r != 0) return;
                KitBase kitController = other.transform.root.GetComponent<KitBase>();
                if (kitController.kitType == KitType.AmmoPack)
                {
                    bot.botController.BuffWeapon(0.5f, true);
                    print("buff");
                }
                else if (kitController.kitType == KitType.Medkit)
                {
                    //Buff Health
                    bot.healthManager.Buff(kitController.value);
                }
                ArenaNetworkManager.instan.PickupKit(kitController.id);
                
                break;
            case "Flag":
                FlagController flagController = other.transform.root.GetComponent<FlagController>();
                if (flagController != null && flagController.team != bot.team && flagController.state != FlagState.moving)
                {
                    flagController.Pickup(bot);
                }
                break;
        }
    }
}
