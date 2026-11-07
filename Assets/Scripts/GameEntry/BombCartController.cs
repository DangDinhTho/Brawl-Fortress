using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombCartController : MonoBehaviour
{
    public Team team = Team.blue;
    public EntryFieldOfView entryFieldOfView;
    public CoasterfollowerAdv coasterfollowerAdv;
    public Transform target;
    public BombCartNetwork bombCartNetwork;
    public float[] speeds;

    private Team enemyTeam;
    private bool deployed;
    // Start is called before the first frame update
    void Start()
    {
        enemyTeam = ConfigManager.GetEnemyTeam(team);
        coasterfollowerAdv.speedFactor = 0;
    }

    public void Update()
    {
        if (deployed)
        {
            coasterfollowerAdv.canMove = true;
            coasterfollowerAdv.speedFactor = speeds[3];
            return;
        }

        if(Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            deployed = true;
            ArenaNetworkManager.instan.EndGame(0, 1);
            bombCartNetwork.SyncDeployBomb();
            return;
        }

        int teamDetect = entryFieldOfView.detectTeams[team].Count;
        if (teamDetect == 0 || entryFieldOfView.detectTeams[enemyTeam].Count > 0)
        {
            coasterfollowerAdv.canMove = false;
        }
        else if(teamDetect > 0)
        {
            teamDetect = teamDetect <= 3 ? teamDetect : 3;
            coasterfollowerAdv.canMove = true;
            coasterfollowerAdv.speedFactor = speeds[teamDetect];
        }
    }



    public void InitOnMaster()
    {
        entryFieldOfView.enabled = true;
        coasterfollowerAdv.enabled = true;
        this.enabled = true;
    }

    public void InitOnNonMaster()
    {
        entryFieldOfView.enabled = false;
        coasterfollowerAdv.enabled = false;
        this.enabled = false;
    }


}
