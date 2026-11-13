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
                TryPickupFlag(other);
                break;
        }
    }

    // The trigger sphere is bigger than the pickup distance, so keep checking while overlapping.
    public virtual void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Flag") || bot.healthManager.death) return;
        TryPickupFlag(other);
    }

    void TryPickupFlag(Collider other)
    {
        FlagController flagController = other.GetComponentInParent<FlagController>();
        if (flagController != null) flagController.TryPickup(bot);
    }
}
