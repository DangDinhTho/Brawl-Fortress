using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MedalManager : MonoBehaviour
{
    public Image currentMedalIcon;
    public Image nextMedalIcon;

    public Text txtCurrentMedal;
    public Text txtNextMedal;

    public Slider proccess;

    private Medal currMedal;
    [Tooltip("Seconds to fill the bar for one medal step")]
    public float step = 1.5f;

    public Animator animator;
    public GameObject goHomeBtn;
    public GameObject loadingPanel;

    public AudioSource audioSource;
    public AudioClip loadProcessClip, rankUpClip;
    public Image[] comboMedals;

    bool goingHome;

    [Tooltip("Where the MREC (300x250) shows in MedalScene")]
    public AdCorner mrecPosition = AdCorner.Bottom;

    public void Start()
    {
        goHomeBtn.SetActive(false);
        var user = GameManager.instan.userData;
        int oldScore = Mathf.Min(user.oldScore, user.score);
        int newScore = user.score;
        StartCoroutine(Play(oldScore, newScore));
        AdsManager.instan.ShowNavtiveAds(mrecPosition);
    }

    private void OnDestroy()
    {
        if (AdsManager.instan != null) AdsManager.instan.HideNavtiveAds();
    }

    #region Medal helpers
    static bool TryGetNext(Medal medal, out Medal next)
    {
        next = medal + 1;
        return ConfigManager.medals.ContainsKey(next);
    }

    /// <summary>Progress (0..n) of a score inside the given medal. At max rank the bar is full.</summary>
    static float Progress(int score, Medal medal)
    {
        if (!ConfigManager.medals.TryGetValue(medal, out var current)) return 1f;
        if (!TryGetNext(medal, out var next)) return 1f;
        int range = ConfigManager.medals[next].exp - current.exp;
        if (range <= 0) return 1f;
        return Mathf.Max(0f, (score - current.exp) / (float)range);
    }

    Sprite GetMedalSprite(Medal medal) => GUIManager.instan.iconAtlas.GetSprite(medal.ToString());

    void ShowMedal(Medal medal)
    {
        currentMedalIcon.sprite = GetMedalSprite(medal);
        txtCurrentMedal.text = LocalizationManager.Localize(medal.ToString());

        Medal right = TryGetNext(medal, out var next) ? next : medal;
        if (comboMedals.Length > 0) comboMedals[0].sprite = GetMedalSprite(medal);
        if (comboMedals.Length > 1) comboMedals[1].sprite = GetMedalSprite(right);
    }
    #endregion

    IEnumerator FillTo(float target)
    {
        target = Mathf.Clamp01(target);
        float speed = 1f / Mathf.Max(0.01f, step);
        while (proccess.value < target)
        {
            proccess.value = Mathf.MoveTowards(proccess.value, target, speed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator Play(int oldScore, int newScore)
    {
        currMedal = ConfigManager.GetMedal(oldScore);
        ShowMedal(currMedal);
        proccess.value = Mathf.Clamp01(Progress(oldScore, currMedal));

        yield return new WaitForSeconds(1);
        if (newScore > oldScore && loadProcessClip) audioSource.PlayOneShot(loadProcessClip);

        // Handles several rank-ups in one match.
        while (true)
        {
            float target = Progress(newScore, currMedal);
            yield return FillTo(target);

            if (target < 1f || !TryGetNext(currMedal, out var next) || ConfigManager.medals[next].exp > newScore)
                break;

            currMedal = next;
            nextMedalIcon.sprite = GetMedalSprite(currMedal);
            txtNextMedal.text = LocalizationManager.Localize(currMedal.ToString());
            if (animator) animator.SetTrigger("MedalUp");
            if (rankUpClip) audioSource.PlayOneShot(rankUpClip);

            yield return new WaitForSeconds(1);
            ShowMedal(currMedal);
            proccess.value = 0;
        }

        // Prevents replaying the same progress next time the scene opens.
        GameManager.instan.userData.oldScore = newScore;
        goHomeBtn.SetActive(true);
    }

    public void GoHome()
    {
        if (goingHome) return;
        goingHome = true;
        loadingPanel.SetActive(true);
        AdsManager.instan.HideNavtiveAds();
        if (ConfigManager.showInterBackHome)
        {
            AdsManager.instan.ShowInterAds(LoadSceneHome);
        }
        else
        {
            LoadSceneHome();
        }
    }

    private void LoadSceneHome()
    {
        SceneManager.LoadScene("Home");
    }
}
