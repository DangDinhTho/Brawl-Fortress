using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PopupQC : PopupBase
{
    private QCData _qcData;
    public Image image;
    public Text content;
    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        if(ConfigManager.QcDatas == null) Dismiss();
        else
        {
            _qcData = ConfigManager.QcDatas[Random.Range(0, ConfigManager.QcDatas.Count)];
            content.text = _qcData.content;
            if (!string.IsNullOrEmpty(_qcData.imageUrl))
                StartCoroutine(LoadImageFromURL(_qcData.imageUrl));
        }
    }

    public void OnQCClick()
    {
        if (string.IsNullOrEmpty(_qcData.direction))
            GUIManager.instan.CreatePopup<PopupWorkShop>(_qcData.tf2Class);
        else
        {
            GUIManager.instan.CreatePopup(_qcData.direction);
        }
        Dismiss();
    }

    public IEnumerator LoadImageFromURL(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2());
            image.sprite = sprite;
        }
    }
}
