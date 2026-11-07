using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotPickUp : BotPickup
{
    public override void OnTriggerEnter(Collider other)
    {
        if (bot.healthManager.death) return;
        int r = Random.Range(0, 5);
        if (r != 0) return;
        switch (other.tag)
        {
            case "Kit":
                KitBase kitController = other.transform.root.GetComponent<KitBase>();
                if (kitController.kitType == KitType.AmmoPack)
                {
                    bot.botController.BuffWeapon(0.5f);
                    print("buff");
                }
                else if (kitController.kitType == KitType.Medkit)
                {
                    //Buff Health
                    bot.healthManager.Buff(kitController.value);
                }
                ArenaNetworkManager.instan.PickupKit(kitController.id);

                break;
            case "AtomBomb":
                if (!ConfigManager.robotCanHoldBomb.Contains(bot.tF2Class)) break;
                AtomBomb atomBomb = other.transform.root.GetComponent<AtomBomb>();
                if (atomBomb != null && atomBomb.state != FlagState.moving)
                {
                    atomBomb.Pickup(bot);
                }
                break;
        }
    }
}
