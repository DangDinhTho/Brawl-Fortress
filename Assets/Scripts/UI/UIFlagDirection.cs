using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class UIFlagDirection : MonoBehaviour
{
    public Image uiFlagState;
    public Transform uiDirec;
    public FlagController flagController;
    public Team team;
    private Transform mainPlayer;
    private FlagState state;
    private Vector3 enemyHomePos;
    
    public void SetFlag(FlagController flag)
    {
        this.flagController = flag;
        flag.OnstateUpdate += UpdateFlagState;
        uiFlagState.sprite = GUIManager.instan.iconAtlas.GetSprite($"objectives_flagpanel_ico_flag_home");

        enemyHomePos = CaptureTheFlagManager.instan.homePositions[(int)ConfigManager.GetEnemyTeam(team)];
        enemyHomePos.y += 1f;
        
    }

    public void StartUpdate()
    {
        mainPlayer = ArenaManager.Instance.mainPlayer.transform;
        this.enabled = true;
    }


    public void UpdateFlagState(FlagState state)
    {
        this.state = state;
        uiFlagState.sprite = GUIManager.instan.iconAtlas.GetSprite($"objectives_flagpanel_ico_flag_{state}");
    }

    private void Update()
    {
       Vector3  direc = flagController.transform.position;
        direc.y = mainPlayer.position.y;
        direc = direc - mainPlayer.position;

        direc = mainPlayer.InverseTransformDirection(direc);

        float angle = Mathf.Atan2(direc.z, direc.x) * Mathf.Rad2Deg - 90;
        uiDirec.rotation = Quaternion.Euler(0, 0, angle);
        
        if(!PhotonNetwork.IsMasterClient) return;
        if (state == FlagState.moving && Vector3.Distance( flagController.transform.position, enemyHomePos) < 1.5f)
        {
            flagController.Sync_BackHome(true);
            state = FlagState.home;
        }
    }
}
