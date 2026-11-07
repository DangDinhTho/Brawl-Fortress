using System;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManvsMachineManager : MonoBehaviour
{
    public static ManvsMachineManager instan;
    public AtomBomb bomb;

    public TankBomb tankBomb;
    //public 
    [HideInInspector]
    public Transform currentBomb;
    public Transform target;
    [HideInInspector]
    public int wave;
    private bool gameFinish;

    public GameObject deployCam;
    public GameObject explosion;
    public DoorController tankDoor;
    public Action<int> OnStartWave;
    public float timeDelayAtomBomb = 15;
    public float timeDelayTankBomb = 5;
    [HideInInspector]
    public bool tankBombStated;
    public float atomBombWaveDuration = 60;

    private void Awake()
    {
        instan = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        ArenaManager.Instance.OnInitDone += InitMachine;
        ArenaManager.Instance.OnGameStarted += OnGameStarted;
        currentBomb = bomb.transform;
        UIMainHud.instan.CreateHUD<MannVsMachineHUD>();
    }

    private void Update()
    {
        if(!PhotonNetwork.IsMasterClient || gameFinish) return;
        if (Vector3.Distance(currentBomb.position, target.position) < 2f)
        {
            gameFinish = true;
            
            //ArenaNetworkManager.instan.EndGame(0, 1);
            switch (wave)
            {
                case  1: bomb.Deploy();
                    break;
                case 2: tankBomb.tankBombNetwork.SyncDeployBomb();
                    break;
            }
        }
    }

    public void InitMachine()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CreateRobot();
        }
    }

    public void OnGameStarted()
    {
        StartCoroutine(WaitCreateBotHoldBomb());
        if (PhotonNetwork.IsMasterClient)
        {
            
        }
    }

    IEnumerator WaitCreateBotHoldBomb()
    {
        yield return new WaitForSeconds(10);
        if (PhotonNetwork.IsMasterClient && wave < 1)
        {
            var botHoldBomb = CreateRobot(0);

            yield return new WaitForSeconds(0.5f);
            TF2Robot tF2Robot = botHoldBomb.GetComponent<TF2Robot>();
            // tF2Robot.behaviorTree.enabled = false;
            bomb.Pickup(tF2Robot);
            //yield return new WaitForSeconds(0.5f);
            //tF2Robot.behaviorTree.enabled = true;
        }

        yield return new WaitForSeconds(atomBombWaveDuration);
        if (PhotonNetwork.IsMasterClient)
        {
            bomb.DestroyBomb();
            if (bomb.owner != null)
            {
                TF2Robot tF2Robot = (TF2Robot)bomb.owner;
                tF2Robot.behaviorTree.enabled = false;
                yield return new WaitForSeconds(0.2f);
                tF2Robot.behaviorTree.enabled = true;
            }
        }

        yield return new WaitForSeconds(5);
        if (PhotonNetwork.IsMasterClient && wave < 2)
        {
            StartTankBomb();
        }
    }

    public GameObject CreateRobot(int index)
    {

        int randomClass = UnityEngine.Random.Range(0, ConfigManager.robotCanHoldBomb.Count);
        TF2Class tf2Class = ConfigManager.robotCanHoldBomb[randomClass];
        // if (tf2Class == TF2Class.medic) tf2Class = TF2Class.soldier;

        Transform teamPos = MapManager.instan.teamPos[1];
        Transform spawnPos = teamPos.GetChild(index % teamPos.childCount);

        string playerName = "Robot";
        //short[] equipment = { (short)EquipmentID.iron_helmet, (short)EquipmentID.none };
        object[] info = { tf2Class, Team.blue, playerName };
        GameObject bot = PhotonNetwork.Instantiate("Robot", spawnPos.position, spawnPos.rotation,
            0, info);
        return bot;
    }

    public void SetCurrentBomb(Transform crrbomb, int wave)
    {
        currentBomb = crrbomb;
        if (this.wave != wave)
        {
            OnStartWave?.Invoke(wave);
            StartCoroutine(WaitFirst(wave));
        }
        else
            this.wave = wave;
    }

    IEnumerator WaitFirst(int wave)
    {
        if(wave == 2)
        {
            tankBombStated = true;
            this.wave = 0;
            StartCoroutine(CountDownTankWave());
            
        }
        else
        {
            yield return new WaitForSeconds(timeDelayAtomBomb);
            this.wave = wave;
        }
        
       
    }

    IEnumerator CountDownTankWave()
    {
        ArenaManager.Instance.audioSource.PlayOneShot(ArenaManager.Instance.begin_10);
        yield return new WaitForSeconds(5);
        int count = 5;
        while(count >= 0)
        {
            ArenaManager.Instance.audioSource.PlayOneShot(ArenaManager.Instance.timeVoices[count]);
            count--;
            yield return new WaitForSeconds(1);
        }
        tankBomb.tankBombNetwork.StartRun();
        yield return new WaitForSeconds(3);
        if (tankDoor)
            tankDoor.Open();
        Debug.Log("Open");
        yield return new WaitForSeconds(timeDelayTankBomb);
        wave = 2;
    }

    public void CreateRobot()
    {
        for (int i = 0; i < 6; i++)
        {
            CreateRobot(i);
        }
    }

    

    public void StartTankBomb()
    {
        tankBomb.tankBombNetwork.StartWave();
    }

    public void OnDeploy(float timeDelay)
    {
        ArenaManager.Instance.mainPlayer.FPSPlayer.SwitchTPS();
        deployCam.SetActive(true);
        ArenaManager.Instance.delayShowScoreBoardEndGame = timeDelay;
        ArenaManager.Instance.EndGame(0, 1);
        StartCoroutine(WaitExplosion(timeDelay - 0.6f));
    }

    IEnumerator WaitExplosion(float time)
    {
        yield return new WaitForSeconds(time);
        explosion.SetActive(true);
    }
    
}
