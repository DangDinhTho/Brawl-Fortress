using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Pun.Demo.Asteroids;
using UnityEngine;
using UnityEngine.UI;

using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;
using Cinemachine;
using Random = UnityEngine.Random;
using Guirao.UltimateTextDamage;

public class ArenaManager : MonoBehaviourPunCallbacks
{
    public static ArenaManager Instance = null;

    public Text InfoText;

    public Dictionary<GameState, int> gameStateTime = new Dictionary<GameState, int>();

    //public GameObject[] AsteroidPrefabs;
    //[HideInInspector]
    //public int battleTime = 180;

    public Dictionary<int, CharacterScore> characterScores = new Dictionary<int, CharacterScore>();
    public Dictionary<int, TF2Player> allRealPlayers = new Dictionary<int, TF2Player>();

    public TF2Player mainPlayer;
    public System.Action<TF2Player> OnMainPlayerSpawned;
    public int maxPlayer = 1;
    private Dictionary<Team, int> teamPlayerCount = new Dictionary<Team, int>() { { Team.red, 0 }, { Team.blue, 0 } };

    public Dictionary<Team, List<TF2Player>> teamMembers = new Dictionary<Team, List<TF2Player>>() { { Team.red, new List<TF2Player>() }, { Team.blue, new List<TF2Player>() } };
    public Dictionary<Team, int> teamScores = new Dictionary<Team, int>() { { Team.red, 0 }, { Team.blue, 0 } };
    
    public Map map;
    public GameMode gameMode;
    
    public Action OnInitDone;
    public Action OnGameStarted;
    public Action OnGameFinish;
    public Action OnTimeOut;

    public GameState gameState;

    [HideInInspector] public float delayShowScoreBoardEndGame;

    private PopupScoreBoard popupScoreBoard;
    public AudioSource audioSource;
    public AudioClip[] timeVoices;
    public AudioClip victoryVoice;
    public AudioClip loseVoice;
    public AudioClip doorOpenSound;
    public AudioClip begin_60;
    public AudioClip begin_30;
    public AudioClip begin_20;
    public AudioClip begin_10;
    public UltimateTextDamageManager textManager;
    [HideInInspector]
    public bool endGameSign;
    public TimerManager timerManager;

    private PopupLoading popupLoading;
    #region UNITY

    public void Awake()
    {
        Instance = this;
        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            gameStateTime.Add(GameState.CountdownWaitStart, 30);
        }
        else
        {
            gameStateTime.Add(GameState.CountdownWaitStart, 10);
        }
    }

    

    public void Start()
    {
        
        PhotonNetwork.AutomaticallySyncScene = false;

        popupLoading = GUIManager.instan.CreatePopup<PopupLoading>(LocalizationManager.Localize("waiting_other_player"));

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TF2GameProps.GameModeKey, out var gameModeObj))
        {
            gameMode = (GameMode)gameModeObj;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TF2GameProps.Map, out var gameMapObj))
        {
            map = (Map)gameMapObj;
        }

        if (!ConfigManager.CheckSelectMap(map))
        {
            GUIManager.instan.CreatePopup<PopupNotify>(
                string.Format(LocalizationManager.Localize("not_enough_ticket")), GUIManager.instan.iconAtlas.GetSprite("ticket"), (Action)ShowAdsQuitGame);
            return;
        }

        if (ConfigManager.mapConfigs.TryGetValue(map, out var mapConfig))
        {
            if (mapConfig.requireStick)
            {
                UserItem ticket = GameManager.instan.userData.GetAnyStickerWithMap(map, mapConfig.stickPerPlay);
                GameManager.instan.RequestUseTicket(ticket.itemId, mapConfig.stickPerPlay, (Action)(() =>
                {
                    GUIManager.instan.CreatePopup<PopupNotify>(string.Format(LocalizationManager.Localize("not_enough_ticket")), GUIManager.instan.iconAtlas.GetSprite("ticket"), (Action)ShowAdsQuitGame);
                    return;
                }));
            }
        }
        //Yodo1AdsManager.instan.RequestNative();
        LoadMap();
        TimerManager.instance.OnCountDownFinish += OnCountDownFinish;

        if(ConfigManager.battleTimes != null && ConfigManager.battleTimes.TryGetValue(map, out var time))
        {

        }
        else
        {
            time = 300;
        }
        gameStateTime.Add(GameState.BattleCountTime, time);
        GameManager.instan.played = true;
        GameManager.instan.OnStartMatch();
    }

    public void LoadMap()
    {
        // if(PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TF2GameProps.Map, out var _map))
        // {
        //     this.map = (Map)_map;
        // }

        StartCoroutine(WaitLoadMap());
    }

    IEnumerator WaitLoadMap()
    {
        AsyncOperation loadingOperation = SceneManager.LoadSceneAsync(map.ToString(), LoadSceneMode.Additive);
        while (!loadingOperation.isDone)
        {
            yield return null;
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(map.ToString()));
        Hashtable props = new Hashtable
        {
            { TF2GameProps.PLAYER_LOADED_LEVEL, true }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        OnInitDone?.Invoke();
        StartCoroutine(WaitSpawnBot());
    }

    public override void OnDisable()
    {
        base.OnDisable();

        // TimerManager.OnCountdownTimerHasExpired -= OnCountdownTimerIsExpired;
        TimerManager.instance.OnCountDownFinish -= OnCountDownFinish;
    }

    #endregion

    public void SpawnBots(bool force = false)
    {
        //if (PhotonNetwork.CurrentRoom.ma.CustomProperties.TryGetValue(TF2GameProps.MaxPlayerKey, out var maxPlayerObj))
        //{
        //    maxPlayer = (byte)maxPlayerObj;
        //}
        if (gameMode == GameMode.MannVsMachine)
        {
            maxPlayer = ConfigManager.maxPlayerPerRoom;
        }
        else
        {
            maxPlayer = PhotonNetwork.CurrentRoom.MaxPlayers;
        }
        StartCoroutine(SpawnBot(force));
    }

    public GameObject CreateBot(Team _team, int index)
    {
        int randomClass = UnityEngine.Random.Range(1, 10);
        TF2Class tf2Class = (TF2Class)randomClass;
       
        Transform teamPos = MapManager.instan.teamPos[(int)_team];
        Transform spawnPos = teamPos.GetChild(index % teamPos.childCount);

        string playerName = ConfigManager.GetRandomBotName();

        //var equipment = CharacterSave.GetCharacterPref()
        //ushort[] equipment = { (short)EquipmentID.iron_helmet, (short)EquipmentID.none };

        var equipments = ConfigManager.GetBotPref(tf2Class).itemLoadOut;

        for (ItemType slot = ItemType.slot_0; slot < ItemType.slot_5; slot++)
        {
            if(equipments.ContainsKey(slot)) continue;
            int random = Random.Range(0, 10);
            if (random == 0)
            {
                var items = ConfigManager.GetItemConfigsByClassAndType(slot, tf2Class);
                if (items.Count > 0)
                {
                    int ranItem = Random.Range(0, items.Count);
                    
                    equipments.Add(slot, items[ranItem].itemId);
                }
            }
        }

        List<byte> slotEquipts = new List<byte>();
        List<int> itemEquipts = new List<int>();
        foreach (var equipment in equipments)
        {
            slotEquipts.Add((byte)equipment.Key);
            itemEquipts.Add((int)equipment.Value);
        }

        object[] info = { tf2Class, _team, playerName, slotEquipts.ToArray(), itemEquipts.ToArray()};
        // Room object: owned by the room (not by the master), so it is not destroyed when the master leaves
        // and control passes to the new master automatically (TF2Bot.OnMasterSwitched re-inits the AI).
        GameObject bot = PhotonNetwork.InstantiateRoomObject($"Bot_{gameMode}", spawnPos.position, spawnPos.rotation,
            0, info);
        return bot;
    }
    
    #region COROUTINES

    private IEnumerator SpawnBot(bool force)
    {
        //Team teamMax = gameMode == GameMode.MannVsMachine ? Team.red : Team.blue;

        if (gameMode != GameMode.MannVsMachine)
        {
            int[] botTeams = new int[] { teamPlayerCount[(Team)0], teamPlayerCount[(Team)1] };
            Team _team = Team.red;
            int spawned = PhotonNetwork.CurrentRoom.PlayerCount;
            while (spawned < maxPlayer && (botTeams[0] < maxPlayer / 2 || botTeams[1] < maxPlayer / 2))
            {
                if (botTeams[(int)_team] >= maxPlayer / 2) _team = ConfigManager.GetEnemyTeam(_team);
                if (!force)
                    yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.5f));
                yield return CreateBot(_team, botTeams[(int)_team]);
                botTeams[(int)_team]++; 
                _team = ConfigManager.GetEnemyTeam(_team);
                spawned++;
            }
        }
        else
        {
            Team _team = Team.red;
            for (int i = teamPlayerCount[_team]; i < maxPlayer / 2; i++)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.5f));
                yield return CreateBot(_team, i);
            }
        }



    }

    private IEnumerator EndOfGame(string winner, int score)
    {
        float timer = 5.0f;

        while (timer > 0.0f)
        {
            InfoText.text =
                string.Format("Player {0} won with {1} points.\n\n\nReturning to login screen in {2} seconds.", winner,
                    score, timer.ToString("n2"));

            yield return new WaitForEndOfFrame();

            timer -= Time.deltaTime;
        }

        PhotonNetwork.LeaveRoom();
    }

    #endregion

    #region PUN CALLBACKS

    public override void OnDisconnected(DisconnectCause cause)
    {
        //UnityEngine.SceneManagement.SceneManager.LoadScene("DemoAsteroids-LobbyScene");
    }

    public override void OnLeftRoom()
    {
        // Stay connected to the master server: Home reuses the connection instead of reconnecting.
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
        {
            //StartCoroutine(SpawnAsteroid());
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // The leaving player's character is destroyed by Photon (CleanupCacheOnLeave): drop our references to it.
        if (allRealPlayers.TryGetValue(otherPlayer.ActorNumber, out var left))
        {
            allRealPlayers.Remove(otherPlayer.ActorNumber);
            if (left != null && teamPlayerCount.ContainsKey(left.team))
                teamPlayerCount[left.team] = Mathf.Max(0, teamPlayerCount[left.team] - 1);
        }
        foreach (var members in teamMembers.Values)
        {
            members.RemoveAll(m => m == null || (!m.isBot && m.photonView != null && m.photonView.OwnerActorNr == otherPlayer.ActorNumber));
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        //if (changedProps.ContainsKey(TF2GameProps.PLAYER_LIVES))
        //{
        //    CheckEndOfGame();
        //    return;
        //}
        
        if (!PhotonNetwork.IsMasterClient)
        {
            if (changedProps.ContainsKey(TF2GameProps.PLAYER_LOADED_LEVEL))
            {
                if (mainPlayer == null && CheckAllPlayerLoadedLevel())
                {
                    CreatePlayer();
                }
            }
            return;
        }


        // if there was no countdown yet, the master client (this one) waits until everyone loaded the level and sets a timer start
        int startTimestamp;
       // bool startTimeIsSet = TimerManager.TryGetStartTime(out startTimestamp);

        if (changedProps.ContainsKey(TF2GameProps.PLAYER_LOADED_LEVEL))
        {
            if (mainPlayer == null && CheckAllPlayerLoadedLevel())
            {
                //if (!startTimeIsSet)
                // {
                CreatePlayer();
                TimerManager.instance.StartNewState(GameState.CountdownWaitStart);
                   // TimerManager.SetStartTime(TimerEvent.CountdownWaitStart);
               // }
            }
            else
            {
                // not all players loaded yet. wait:
                Debug.Log("setting text waiting for players! ", this.InfoText);
                InfoText.text = "Waiting for other players...";
            }
        }

    }

    #endregion

    public void CreatePlayer()
    {
        
        object teamId;
        Team playerTeam = Team.red;
        if (gameMode != GameMode.MannVsMachine)
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(TF2GameProps.TeamKey, out teamId))
            {
                playerTeam = (Team)teamId;
            }
            else
            {
                playerTeam = (Team)UnityEngine.Random.Range(0, 1);
            }
        }

        object tf2ClassObj;
        TF2Class tf2Class = TF2Class.pyro;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(TF2GameProps.ClassKey, out tf2ClassObj))
        {
            tf2Class = (TF2Class)tf2ClassObj;
        }

        Transform teamPos = MapManager.instan.teamPos[(int)playerTeam];

        int playerNumber = PhotonNetwork.LocalPlayer.ActorNumber >= 0 ? PhotonNetwork.LocalPlayer.ActorNumber : 0;

        int posIndex = Mathf.Min(maxPlayer / 2, teamPos.childCount);
        Transform spawnPos = teamPos.GetChild(posIndex);
        if (posIndex > teamPos.childCount)
            spawnPos = teamPos.GetChild(playerNumber % posIndex);



        string playerName = PhotonNetwork.LocalPlayer.NickName;

        var equipments = CharacterSave.GetCharacterPref(tf2Class).itemLoadOut;
        List<byte> slotEquipts = new List<byte>();
        List<int> itemEquipts = new List<int>();
        foreach (var equipment in equipments)
        {
            slotEquipts.Add((byte)equipment.Key);
            itemEquipts.Add((int)equipment.Value);
        }

        //short[] equipment = { (short)EquipmentID.iron_helmet, (short)EquipmentID.none };
        object[] info = { tf2Class, playerTeam, playerName, slotEquipts.ToArray(), itemEquipts.ToArray()};
        GameObject player = PhotonNetwork.Instantiate("Player", spawnPos.position, spawnPos.rotation,
            0, info); // avoid this call on rejoin (ship was network instantiated before)
        mainPlayer = player.GetComponent<TF2Player>();
        mainPlayer.isMainPlayer = true;
        mainPlayer.InitMainPlayer();

        int playCount = PlayerPrefs.GetInt($"ClassPlayCount_{tf2Class}", 0);
        playCount++;
        PlayerPrefs.SetInt($"ClassPlayCount_{tf2Class}", playCount);
        popupLoading.Dismiss();
    }

    


    // called by OnCountdownTimerIsExpired() when the timer ended
    IEnumerator StartGame()
    {
        Debug.Log("StartGame!");
        
        if (PhotonNetwork.IsMasterClient)
        {
            // StartCoroutine(SpawnAsteroid());
            // TimerManager.SetStartTime(TimerEvent.BattleCountTime);
            yield return new WaitForSeconds(1);
            TimerManager.instance.StartNewState(GameState.BattleCountTime);
        }
        gameState = GameState.Started;
        OnGameStarted?.Invoke();
    }

    private bool CheckAllPlayerLoadedLevel()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object playerLoadedLevel;

            if (p.CustomProperties.TryGetValue(TF2GameProps.PLAYER_LOADED_LEVEL, out playerLoadedLevel))
            {
                if ((bool)playerLoadedLevel)
                {
                    continue;
                }
            }

            return false;
        }

        return true;
    }

    //private void CheckEndOfGame()
    //{
    //    bool allDestroyed = true;

    //    foreach (Player p in PhotonNetwork.PlayerList)
    //    {
    //        object lives;
    //        if (p.CustomProperties.TryGetValue(TF2GameProps.PLAYER_LIVES, out lives))
    //        {
    //            if ((int)lives > 0)
    //            {
    //                allDestroyed = false;
    //                break;
    //            }
    //        }
    //    }

    //    if (allDestroyed)
    //    {
    //        if (PhotonNetwork.IsMasterClient)
    //        {
    //            StopAllCoroutines();
    //        }

    //        string winner = "";
    //        int score = -1;

    //        foreach (Player p in PhotonNetwork.PlayerList)
    //        {
    //            if (p.GetScore() > score)
    //            {
    //                winner = p.NickName;
    //                score = p.GetScore();
    //            }
    //        }

    //        StartCoroutine(EndOfGame(winner, score));
    //    }
    //}

    private void OnCountDownFinish(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.CountdownWaitStart:
                StartCoroutine(StartGame());
                break;
            case GameState.BattleCountTime:
                OnTimeOut?.Invoke();
                break;
        }
    }

    private bool botSpawned = false;
    public void OnCharacterCreated(TF2Player player)
    {
        if (!characterScores.ContainsKey(player.viewID))
        {
            CharacterScore characterScore = new CharacterScore()
            {
                classId = player.characterConfig.characterClass,
                team = player.team,
                viewId = player.viewID,
                playerName = player.playerName
            };
            characterScores.Add(player.viewID, characterScore);
        }

        if (!player.isBot && !allRealPlayers.ContainsKey(player.photonView.Owner.ActorNumber))
        {
            allRealPlayers.Add(player.photonView.Owner.ActorNumber, player);
        }

        teamMembers[player.team].Add(player);

        GUIManager.instan.matchAvatarGroup.CreateCharacterAvatar(player);
        if (player.isMainPlayer)
        {
            OnMainPlayerSpawned?.Invoke(player);
            UIMainHud.instan.Init(player);
            //textManager.theCamera = player.fPSReferences.fpsCam;
        }
        
        teamPlayerCount[player.team]++;
        if (!player.isBot && teamPlayerCount[Team.red] + teamPlayerCount[Team.blue] == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            botSpawned = true;
            if (PhotonNetwork.IsMasterClient)
                SpawnBots();
        }
    }

    IEnumerator WaitSpawnBot()
    {
        yield return new WaitForSeconds(gameStateTime[GameState.CountdownWaitStart] - 3);
        if (PhotonNetwork.IsMasterClient && !botSpawned && teamPlayerCount[Team.red] + teamPlayerCount[Team.blue] <
            PhotonNetwork.CurrentRoom.PlayerCount)
        {
            botSpawned = true;
            SpawnBots(true);
        }
    }


    public void OpenScoreBoard()
    {
        GUIManager.instan.CreatePopup<PopupScoreBoard>();
    }
    

    public void EndGame(int redScore, int blueScore)
    {
        if (endGameSign) return;
        endGameSign = true;
        timerManager.enabled = false;
        this.teamScores[Team.red] = redScore;
        this.teamScores[Team.blue] = blueScore;
        StartCoroutine(WaitShowScoreBoard());
        OnGameFinish?.Invoke();
    }

    IEnumerator WaitShowScoreBoard()
    {
        yield return new WaitForSeconds(delayShowScoreBoardEndGame + 0.2f);
        if((teamScores[Team.red] > teamScores[Team.blue] && mainPlayer.team == Team.red) || (teamScores[Team.red] < teamScores[Team.blue] && mainPlayer.team == Team.blue))
            {
                audioSource.PlayOneShot(victoryVoice);
            }
        else if ((teamScores[Team.red] > teamScores[Team.blue] && mainPlayer.team == Team.blue) || (teamScores[Team.red] < teamScores[Team.blue] && mainPlayer.team == Team.red))
        {
            audioSource.PlayOneShot(loseVoice);
        }
        popupScoreBoard = GUIManager.instan.CreatePopup<PopupScoreBoard>(true);
        yield return new WaitForSeconds(2);
        ShowAdsEndGame();
    }

    public int CaculatePlayerScore()
    {
        float score = 0;
        int cPlayer = 0;
        int maxP = maxPlayer;
        if (gameMode != GameMode.MannVsMachine)
        {
            if (teamMembers.TryGetValue(mainPlayer.team, out var myTeam) &&
                teamMembers.TryGetValue(ConfigManager.GetEnemyTeam(mainPlayer.team), out var enemyTeam))
                cPlayer = myTeam.Count - enemyTeam.Count;
        }
        else
        {
            maxP = ConfigManager.maxPlayerPerRoom;
        }
        if (maxP <= 0) maxP = ConfigManager.maxPlayerPerRoom; // avoid division by zero

        Team teamWin = GetTeamWin();
        if (teamWin == Team.None) score += 2 * maxP;          // draw
        else if (mainPlayer.team == teamWin) score += 3 * maxP; // win
        else score += maxP;                                     // lose

        if (characterScores.TryGetValue(mainPlayer.viewID, out var playerScore))
        {
            score += playerScore.CaculateScore();
        }

        // Bonus/penalty for unbalanced teams
        score *= ((maxP / 2f) - cPlayer) / (maxP / 2f);
        if (float.IsNaN(score) || score < 0) score = 0;
        return Mathf.RoundToInt(score);
    }
    
    public void ShowReward(RewardData rewardData)
    {
        GameManager.instan.userData.oldScore = GameManager.instan.userData.score;
        GameManager.instan.userData.score += CaculatePlayerScore();
        GUIManager.instan.CreatePopup<PopupReward>(rewardData);

        popupScoreBoard.goHomeBtn.SetActive(true);
        popupScoreBoard.StartCoroutine(popupScoreBoard.WaitAutoQuickGame());
    }

    public Team GetTeamWin()
    {
        if (teamScores[Team.red] == teamScores[Team.blue]) return Team.None;
        Team teamWin = teamScores[Team.red] > teamScores[Team.blue] ? Team.red : Team.blue;
        return teamWin;
    }

    public void OnShowAdsEndGameDone()
    {
        bool isWin =  mainPlayer.team == GetTeamWin();
        GameManager.instan.RequestOnEndGame(gameMode, isWin, CaculatePlayerScore(), ShowReward);
    }

    public void ShowAdsEndGame()
    {
        //Yodo1AdsManager.instan.ShowInterstitial(OnShowAdsEndGameDone);
        AdsManager.instan.ShowInterAds(OnShowAdsEndGameDone);
    }

    public void ShowAdsQuitGame()
    {
        //Yodo1AdsManager.instan.ShowInterstitial(OnShowAdsQuitMatchDone);
        AdsManager.instan.ShowInterAds(OnShowAdsQuitMatchDone);
    }

    public void OnShowAdsQuitMatchDone()
    {
        StartCoroutine(WaitLeaveRoom());
    }
    IEnumerator WaitLeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        GUIManager.instan.CreatePopup<PopupLoading>();
        while (PhotonNetwork.CurrentRoom != null)
        {
            yield return null;
        }
        SceneManager.LoadScene("Home");
    }

    public void OnQuitGameClick()
    {
        GUIManager.instan.CreatePopup<PopupConfirm>(LocalizationManager.Localize("confirm_quit_game"), (Action)ShowAdsQuitGame);
    }

    


    public void ShowTextDamage(float damage, Transform overrideTransform)
    {
        textManager.AddStack(damage, overrideTransform);
    }

    public void AddDamageForPlayer(int id, int damage)
    {
        if (characterScores.TryGetValue(id, out var playerScore))
        {
            playerScore.AddDamage(damage);
        }
    }
    public void AddKillForPlayer(int id, int kill)
    {
        if (characterScores.TryGetValue(id, out var playerScore))
        {
            playerScore.AddKill(kill);
        }
    }
    
    public void AddDeathForPlayer(int id, int death)
    {
        if (characterScores.TryGetValue(id, out var playerScore))
        {
            playerScore.AddDeath(death);
        }
    }

    private int teammateIndex;
    public TF2Player GetNextTeamMateAlive()
    {
        teammateIndex++;
        if (teammateIndex >= teamMembers[mainPlayer.team].Count) teammateIndex = 0;
        for(int i = teammateIndex; i < teamMembers[mainPlayer.team].Count; i++)
        {
            if (!teamMembers[mainPlayer.team][i].healthManager.death)
            {
                return teamMembers[mainPlayer.team][i];
            }
        }

        return mainPlayer;
    }

    
}


public class CharacterScore
{
    public TF2Class classId;
    public Team team;
    public string playerName;
    public int viewId;
    public int damages;
    public int kills;
    public int deaths;


    public void AddDamage(int damage)
    {
        this.damages += damage;
    }

    public void AddKill(int kill)
    {
        this.kills += kill;
    }

    public void AddDeath(int death)
    {
        this.deaths += death;
    }

    public float CaculateScore()
    {
        return (kills + damages * 1.0f / 100) / 2f - deaths;
    }
}

// public enum EquipmentID : short
// {
//     none = 0,
//     iron_helmet,
// }


