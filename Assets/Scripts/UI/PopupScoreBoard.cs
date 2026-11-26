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

    [Tooltip("Where the MREC (300x250) shows on the end-of-match board")]
    public AdCorner mrecPosition = AdCorner.Bottom;
    private bool mrecShown;

    private bool leaving;

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

        if (cantDismiss && !mrecShown && ConfigManager.showMrecScoreboard)
        {
            mrecShown = true;
            AdsManager.instan.ShowNavtiveAds(mrecPosition);
        }
    }

    void HideMrec()
    {
        if (!mrecShown) return;
        mrecShown = false;
        AdsManager.instan.HideNavtiveAds();
    }

    private void OnDisable() { HideMrec(); }

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

    /// <summary>The Home button (the only way out: the board no longer leaves on its own after 30 s, Nov 2026).</summary>
    public void GoHome()
    {
        if (leaving) return; // a double tap
        leaving = true;
        goHomeBtn.SetActive(false);
        HideMrec();
        AudioListener.volume = 0f;   // silent from the moment Home is chosen (back on in Home)
        // no ad here any more: the end-of-match interstitial shows when the reward is claimed (PopupReward.Dismiss)
        StartCoroutine(WaitLeaveRoom());
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
