using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPingItem : MonoBehaviour
{
    public Image pingIcon;
    public Text txtPing;
    public Sprite[] icons;

    private void OnEnable()
    {
        StartCoroutine(TrackPing());
    }

    public void SetPing(int value)
    {
        txtPing.text = $"{value}ms";
        if(value < 50)
        {
            pingIcon.sprite = icons[0];
        }
        else if (value < 150)
        {
            pingIcon.sprite = icons[1];
        }
        else if (value < 250)
        {
            pingIcon.sprite = icons[2];
        }
        else
        {
            pingIcon.sprite = icons[3];
        }
    }

    public void SetDisConnected()
    {
        pingIcon.sprite = icons[3];
        txtPing.text = LocalizationManager.Localize("disconnected");
    }

    IEnumerator TrackPing()
    {
        while (true)
        {
            if (PhotonNetwork.IsConnected)
            {
                SetPing(PhotonNetwork.GetPing());
            }
            else
            {
                SetDisConnected();
            }
            yield return new WaitForSeconds(1);
        }
    }
}
