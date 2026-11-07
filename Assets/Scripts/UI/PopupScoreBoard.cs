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

        //goHomeBtn.SetActive(cantDismiss);

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
        StartCoroutine(WaitLeaveRoom());
       
    }

    public IEnumerator WaitAutoQuickGame()
    {
        yield return new WaitForSeconds(30);
        GoHome();
    }

    IEnumerator WaitLeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        GUIManager.instan.CreatePopup<PopupLoading>();
        while (PhotonNetwork.CurrentRoom != null)
        {
            yield return null;
        }
        SceneManager.LoadScene("MedalScene");
    }
}
