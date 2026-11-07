using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class GUIManager : MonoBehaviour
{
    public static GUIManager instan;
    private TF2Player mainPlayer;
    
    public Color[] teamColor;
    public SpriteAtlas avatarAtlas;
    public SpriteAtlas classIconAtlas;
    public SpriteAtlas iconAtlas;
    public SpriteAtlas itemIconAtlas;
    public SpriteAtlas mapIconAtlas;
    public MatchAvatarGroup matchAvatarGroup;
    public Transform popupContainer;
    private Dictionary<string, PopupBase> dontDestroyedPopups = new Dictionary<string, PopupBase>();
    public Color mineColor;
    public Color teamMateColor;
    public Color enemyColor;

    private void Awake()
    {
        instan = this;
        
    }

    private void Start()
    {
        foreach (Transform popup in popupContainer)
        {
            Destroy(popup.gameObject);
        }
    }

    public void InitMainPlayer(TF2Player tf2Player)
    {
        this.mainPlayer = tf2Player;
        UIPlayerController.instan.InitData(tf2Player);
    }
    
    public Sprite GetAvatar(TF2Class tf2Class, Team team, bool die = false)
    {
        string t = team == Team.red ? "" : "_blue";
        string d = die ? "_grey" : "";
        string avatarName = $"{tf2Class}{t}{d}";
        return avatarAtlas.GetSprite(avatarName);
    }

    public Sprite GetMapIcon(Map map)
    {
        Sprite s = mapIconAtlas.GetSprite(map.ToString());
        if (s == null) return GetItemIcon(ItemId.None);
        return s;
    }
    
    public Sprite GetClassIcon(string spriteName)
    {
        return classIconAtlas.GetSprite(spriteName);
    }
    
    public Sprite GetAvatarSprite(string spriteName)
    {
        return avatarAtlas.GetSprite(spriteName);
    }

    public Sprite GetItemIcon(ItemId itemId)
    {
        return itemIconAtlas.GetSprite(itemId.ToString());
    }

    public T CreatePopup<T>(params object[] args) where T : PopupBase
	{
        return (T)CreatePopup(typeof(T).ToString(), args);
    }

    public PopupBase CreatePopup(string popupName, params object[] args)
    {
        
        GameObject pref = GetPopupPref(popupName);
        if (pref != null)
        {
            if (!dontDestroyedPopups.TryGetValue(popupName, out var popup))
            {
                GameObject obj = Instantiate(pref, this.popupContainer.transform) as GameObject;
                popup = obj.GetComponent<PopupBase>();
                popup.name = pref.name;
                if (popup.dontDestroyOnDismiss)
                {
                    dontDestroyedPopups.Add(popupName, popup);
                }
            }

            popup.transform.localPosition = Vector3.zero;
            popup.transform.localScale = Vector3.one;
            popup.gameObject.SetActive(true);
            popup.OnCreate(args);
            
            return popup;
        }
        return null;
    }
    
    public GameObject GetPopupPref(string popupName)
    {
        return Resources.Load("Popups/" + popupName) as GameObject;
    }

    public void OnPopupDismiss(PopupBase popupBase)
    {
        
    }

    public T GetPopupDontDestroy<T>() where T: PopupBase
    {
        if (dontDestroyedPopups.TryGetValue(typeof(T).ToString(), out var popup))
        {
            return popup.GetComponent<T>();
        }
        return null;
    }

    public T CreateListItem<T>(Transform container, int index)
    {
        if(index < container.childCount)
        {
            GameObject item = container.GetChild(index).gameObject;
            item.SetActive(true);
            return item.GetComponent<T>();
        }
        else
        {
            GameObject item = Instantiate(container.GetChild(0).gameObject, container);
            item.SetActive(true);
            return item.GetComponent<T>();
        }
    }

    public void CleanListItem(Transform container, int count)
    {
        if (count < container.childCount)
        {
            for (int i = count; i < container.childCount; i++)
            {
                container.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}
