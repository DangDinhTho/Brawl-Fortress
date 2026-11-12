using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PopupScoreBoard : PopupBase
{
    public Transform[] teamScore;
    public UIPlayerScoreItem playerScoreItemPref;
    private Dictionary<int, UIPlayerScoreItem> scoreBoard = new Dictionary<int, UIPlayerScoreItem>();
    private bool cantDismiss;
    public Text[] txtTeamScores;
    public GameObject goHomeBtn;

    [Tooltip("Seconds before leaving automatically once the result is shown")]
    public float autoQuitDelay = 30f;

    private bool leaving;
    private Coroutine autoQuitRoutine;

    private void Awake()
    {
        playerScoreItemPref.gameObject.SetActive(false);
    }

    public override void OnCreate(params object[] args)
    {
        if (args.Length > 0)
        {
            cantDismiss = (bool)args[0];
        }

        txtTeamScores[0].text = ArenaManager.Instance.teamScores[Team.red].ToString();
        txtTeamScores[1].text = ArenaManager.Instance.teamScores[Team.blue].ToString();

        InitData();
    }

    private void InitData()
    {
        foreach (var playerScore in ArenaManager.Instance.characterScores)
        {
            UIPlayerScoreItem item;
            if (!scoreBoard.TryGetValue(playerScore.Key, out item))
            {
                GameObject obj = Instantiate(playerScoreItemPref.gameObject, teamScore[(int)playerScore.Value.team]);
                obj.SetActive(true);
                item = obj.GetComponent<UIPlayerScoreItem>();
                item.SetColor(playerScore.Value.team);
                scoreBoard.Add(playerScore.Key, item);
            }
            item.SetData(playerScore.Value);
        }
    }

    public override void Dismiss()
    {
        if (cantDismiss) return;
        base.Dismiss();
    }

    public void GoHome()
    {
        if (leaving) return; // button + auto-quit could both fire
        leaving = true;
        if (autoQuitRoutine != null) StopCoroutine(autoQuitRoutine);
        goHomeBtn.SetActive(false);
        StartCoroutine(WaitLeaveRoom());
    }

    public void StartAutoQuit()
    {
        if (autoQuitRoutine == null && !leaving)
            autoQuitRoutine = StartCoroutine(WaitAutoQuickGame());
    }

    public IEnumerator WaitAutoQuickGame()
    {
        yield return new WaitForSeconds(autoQuitDelay);
        autoQuitRoutine = null;
        GoHome();
    }

    IEnumerator WaitLeaveRoom()
    {
        GUIManager.instan.CreatePopup<PopupLoading>();
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            float timeout = 10f;
            while (PhotonNetwork.CurrentRoom != null && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }
        SceneManager.LoadScene("MedalScene");
    }
}
