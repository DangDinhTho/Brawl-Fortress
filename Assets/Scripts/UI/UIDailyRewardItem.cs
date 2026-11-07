using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIDailyRewardItem : MonoBehaviour
{
    public Image rewardIcon;
    public Text txtDay;
    public Text txtQuantity;
    public GameObject daNhan;
    public GameObject today;
    private DailyReward reward;

    private int mocDaNhan;
    public void Init(DailyReward reward, int mocDaNhan)
    {
        this.mocDaNhan = mocDaNhan;

        this.reward = reward;

        if (reward.quantity > 1)
        {
            txtQuantity.text = $"{reward.quantity} {reward.name}";
        }
        else
        {
            txtQuantity.text = reward.name;
        }
        if (string.IsNullOrEmpty(reward.iconName) || reward.iconName.Equals("GK") || reward.iconName.Equals("MM"))
            rewardIcon.sprite = GUIManager.instan.iconAtlas.GetSprite("GK");
        else
        {
            rewardIcon.sprite = GUIManager.instan.itemIconAtlas.GetSprite(reward.iconName);
        }
        this.daNhan.SetActive(reward.day <= mocDaNhan);
        bool isToday = (GameManager.instan.userData.dayLast != GameManager.instan.toDayOfTheYear && reward.day == mocDaNhan + 1)
            || (GameManager.instan.userData.dayLast == GameManager.instan.toDayOfTheYear && reward.day == mocDaNhan);

        today.SetActive(isToday);
        if (isToday)
        {
            txtDay.text = LocalizationManager.Localize("today");
        }
        else
        {
            txtDay.text = string.Format(LocalizationManager.Localize("daily_reward_day"), reward.day);
        }
    }

    public void Recive()
    {
        if (daNhan.activeInHierarchy) return;

        if (GameManager.instan.userData.dayLast == GameManager.instan.toDayOfTheYear || reward.day > mocDaNhan + 1)
        {
            GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("daily_reward_comback_next_day"));
            return;
        }
        else if (reward.day == mocDaNhan + 1)
        {
            GameManager.instan.RecieveDailyReward(GameManager.instan.toDayOfTheYear, reward.day, OnClambReward);
            daNhan.SetActive(true);
        }
    }

    public void OnClambReward(RewardData rewardData)
    {
        GUIManager.instan.CreatePopup<PopupReward>(rewardData);
    }
}

[System.Serializable]
public class DailyReward
{
    public string name;
    public string iconName;
    public int quantity;
    public int day;
}

