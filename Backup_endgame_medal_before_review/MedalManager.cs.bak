using System.Collections;
using System.Collections.Generic;
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
    private float newProcess;
    public float step = 1.5f;

    public Animator animator;
    public GameObject goHomeBtn;
    public GameObject loadingPanel;

    public AudioSource audioSource;
    public AudioClip loadProcessClip, rankUpClip;
    public Image[] comboMedals;

    //private Medal minMedal;
    //private Medal maxMedal;

    public void Start()
    {
        currMedal = ConfigManager.GetMedal(GameManager.instan.userData.oldScore);

        ShowMedal();
        
    }

    public void ShowMedal()
    {
        currentMedalIcon.sprite = GUIManager.instan.iconAtlas.GetSprite(currMedal.ToString());
        txtCurrentMedal.text = LocalizationManager.Localize(currMedal.ToString());
        SetComboMedal();
        StartCoroutine(Effect());
    }

    public void SetComboMedal()
    {
        //if (currMedal == Medal.Newbie) minMedal = Medal.Newbie;
        //else
            //minMedal = (Medal)(((int)(currMedal - 1) / 4 * 4) + 1);
        //maxMedal = (Medal)minMedal + 3;
        for(int i = 0; i < 2; i++)
        {
            comboMedals[i].sprite = GUIManager.instan.iconAtlas.GetSprite(((Medal)(currMedal + i)).ToString());
        }

        int previousMoc = ConfigManager.medals[currMedal].exp;
        proccess.value = ((GameManager.instan.userData.oldScore - previousMoc) * 1.0f) / (ConfigManager.medals[currMedal + 1].exp - previousMoc);
        newProcess = ((GameManager.instan.userData.score - previousMoc) * 1.0f) / (ConfigManager.medals[currMedal + 1].exp - previousMoc);

    }

    IEnumerator Effect()
    {
        yield return new WaitForSeconds(1);
        audioSource.PlayOneShot(loadProcessClip);
        float speed = (newProcess - proccess.value) / step;
        while (proccess.value < newProcess && proccess.value < 0.99f)
        {
            proccess.value += speed * Time.deltaTime;
            yield return null;
        }

        if (newProcess >= 1)
        {
            currMedal = currMedal + 1;
            GameManager.instan.userData.oldScore = ConfigManager.medals[currMedal].exp + 1;
            ShowMedal();

            Medal newMedal = ConfigManager.GetMedal(GameManager.instan.userData.score);

            nextMedalIcon.sprite = GUIManager.instan.iconAtlas.GetSprite(newMedal.ToString());
            txtNextMedal.text = LocalizationManager.Localize(newMedal.ToString());
            animator.SetTrigger("MedalUp");
            audioSource.PlayOneShot(rankUpClip);

            while (proccess.value < newProcess && proccess.value < 0.99f)
            {
                proccess.value += speed * Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(1);

        }

        
        goHomeBtn.SetActive(true);
        

    }


    public void GoHome()
    {
        loadingPanel.SetActive(true);
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
