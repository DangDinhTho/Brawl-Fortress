using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineerCheckView : PlayerViewBase
{
    private PopupBuildableInfo popupBuildableInfo;
    private bool infoShowed;
    
    
    

    public override void CheckLook()
    {
        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //Just a ray through the middle of your current view
        RaycastHit hit;

        //check if ray hits something
        if (Physics.Raycast(ray, out hit, 1.75f, checkMark))
        {

            switch (hit.transform.root.tag)
            {
                case "Kit":
                    KitBase kitController = hit.transform.root.GetComponent<KitBase>();
                    if (kitController.kitType == KitType.AmmoPack)
                    {
                        player.FPSPlayer.BuffWeapon(kitController.value);
                    }
                    else if (kitController.kitType == KitType.Medkit)
                    {
                        player.healthManager.Buff(kitController.value);
                    }

                    ArenaNetworkManager.instan.PickupKit(kitController.id);

                    break;
                case "Buildable":
                    if (infoShowed) return;
                    var buildable = hit.transform.root.GetComponent<BuildableEntry>();

                    if (buildable != null && buildable.team == player.team)
                    {
                        if (!infoShowed)
                        {
                            popupBuildableInfo = GUIManager.instan.CreatePopup<PopupBuildableInfo>(buildable);
                            infoShowed = true;
                            return;
                        }
                    }
                    break;
                case "Flag":
                    FlagController flagController = hit.transform.root.GetComponent<FlagController>();
                    if (flagController != null && flagController.team != player.team && flagController.state != FlagState.moving)
                    {
                        flagController.Pickup(player);
                    }
                    break;
            }


            
        }

        if (infoShowed)
        {
            popupBuildableInfo.Dismiss();
            popupBuildableInfo = null;
            infoShowed = false;
        }
    }
}
