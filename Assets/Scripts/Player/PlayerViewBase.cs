using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerViewBase : MonoBehaviour
{
    public Camera fpsCam;

    public LayerMask checkMark;
    protected TF2Player player;

    private void OnValidate()
    {
        fpsCam = GetComponent<FPSReferences>().fpsCam;
    }

    public void Start()
    {
        player = transform.root.GetComponent<TF2Player>();
    }

    // Update is called once per frame
    private void OnEnable()
    {
        StartCoroutine(CheckLookLoop());
    }

    IEnumerator CheckLookLoop()
    {
        while (true)
        {
            CheckLook();
            yield return new WaitForSeconds(0.3f);
        }
    }

    public virtual void CheckLook()
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
                    if(kitController.kitType == KitType.AmmoPack)
                    {
                        player.FPSPlayer.BuffWeapon(kitController.value);
                    }
                    else if(kitController.kitType == KitType.Medkit)
                    {
                        //Buff Health
                        player.healthManager.Buff(kitController.value);
                    }
                    ArenaNetworkManager.instan.PickupKit(kitController.id);
                    break;
                case "Flag":
                    FlagController flagController = hit.transform.root.GetComponent<FlagController>();
                    if(flagController != null && flagController.team != player.team && flagController.state != FlagState.moving)
                    {
                        flagController.Pickup(player);
                    }
                    break;
            }
        }

    }
}
