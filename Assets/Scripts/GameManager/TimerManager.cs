using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;
using UnityEngine.UI;

public class TimerManager : MonoBehaviourPunCallbacks
{
    public static TimerManager instance;
    private int timeRemaining;
    public GameState gameState;
    private int timeStart;
    public Text text;
    private int countDownTime;
    private bool timeIsRunning;
    public Action<GameState> OnCountDownFinish;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        TrygetTimeStart();
    }

    public void StartNewState(GameState gameState)
    {
        timeStart = (int)PhotonNetwork.ServerTimestamp;
        countDownTime = ArenaManager.Instance.gameStateTime[gameState];
        Hashtable props = new Hashtable
            {
                {TF2GameProps.GameStateKey, (byte)gameState},
                {TF2GameProps.TimeStartKey, (int)PhotonNetwork.ServerTimestamp}
            };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
    }

    private void TrygetTimeStart()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TF2GameProps.GameStateKey, out var gameStateObj))
        {
            gameState = (GameState)gameStateObj;
            countDownTime = ArenaManager.Instance.gameStateTime[gameState];
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TF2GameProps.TimeStartKey, out var timeStartObj))
        {
            timeStart = (int)timeStartObj;
        }

        timeIsRunning = GetTimeRemaining() > 0;
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        object gameStateObj;
        if (propertiesThatChanged.TryGetValue(TF2GameProps.GameStateKey, out gameStateObj))
        {
            gameState = (GameState)gameStateObj;
            countDownTime = ArenaManager.Instance.gameStateTime[gameState];
        }

        if (propertiesThatChanged.TryGetValue(TF2GameProps.TimeStartKey, out var timeStartObj))
        {
            timeStart = (int)timeStartObj;
        }

        timeIsRunning = GetTimeRemaining() > 0;
    }


    private int timeVoicePlayed = 5;
    private bool played_begin_60;
    private bool played_begin_30;
    //private bool played_begin_20;
    private bool played_begin_10;
    private void Update()
    {
        if (!timeIsRunning) return;

        float countdown = GetTimeRemaining();
        this.text.text = UtilityHelper.FormatTime(countdown);

        if (gameState == GameState.CountdownWaitStart)
        {
            if (!played_begin_60 && (int)countdown == 60)
            {
                ArenaManager.Instance.audioSource.PlayOneShot(ArenaManager.Instance.begin_60);
                played_begin_60 = true;
            }
            else if (!played_begin_30 && (int)countdown == 30)
            {
                ArenaManager.Instance.audioSource.PlayOneShot(ArenaManager.Instance.begin_30);
                played_begin_30 = true;
            }
            //else if (!played_begin_20 && (int)countdown == 20)
            //{
            //    ArenaManager.Instance.audioSource.PlayOneShot(ArenaManager.Instance.begin_20);
            //    played_begin_20 = true;
            //}
            else if (!played_begin_10 && (int)countdown == 10)
            {
                ArenaManager.Instance.audioSource.PlayOneShot(ArenaManager.Instance.begin_10);
                played_begin_10 = true;
            }
        }

        if (countdown <= 5)
        {
            if(timeVoicePlayed > 0 && (int)countdown == timeVoicePlayed)
            {
                ArenaManager.Instance.audioSource.PlayOneShot(ArenaManager.Instance.timeVoices[timeVoicePlayed]);
                timeVoicePlayed--;
            }
            else if(timeVoicePlayed == 0 && countdown < 0.1f)
            {
                
            }
        }
        

        if (countdown > 0.0f) return;

        CountDownFinish();
    }



    private int GetTimeRemaining()
    {
        int timer = PhotonNetwork.ServerTimestamp - this.timeStart;
        return this.countDownTime - (int)(timer / 1000f);
    }

    public int GetDeathTime()
    {
        float timer = (PhotonNetwork.ServerTimestamp - this.timeStart) / 1000f;
        float dt = ConfigManager.baseDeathTime + (ConfigManager.maxDeathTime - ConfigManager.baseDeathTime) * (timer / this.countDownTime);
        return (int)dt;
    }

    private void CountDownFinish()
    {
        timeIsRunning = false;
        if (gameState == GameState.CountdownWaitStart)
        {
            ArenaManager.Instance.audioSource.PlayOneShot(ArenaManager.Instance.doorOpenSound);
            ArenaManager.Instance.audioSource.PlayOneShot(ArenaManager.Instance.timeVoices[0]);
        }
        timeVoicePlayed = 5;
        OnCountDownFinish?.Invoke(this.gameState);
    }
}

public enum GameState : byte
{
    None = 0,
    CountdownWaitStart,
    BattleCountTime,
    Started,
    EndGame
}
