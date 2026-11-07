using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class RobotHealth : HealthManager
{
    // public override void Die(int attacker, Vector3 direction)
    // {
    //     photonView.RPC(nameof(SyncDie), RpcTarget.Others, attacker, direction);
    //     SyncDie(attacker, direction);
    //     int random = Random.Range(0, 3);
    //     if (random == 0)
    //     {
    //         ArenaNetworkManager.instan.DropKit(transform.position);
    //     }
    // }

    public override IEnumerator WaitRespawn()
    {
        yield return new WaitForSeconds(3);
        
        if (photonView.IsMine)
            Respawn();
    }
}
