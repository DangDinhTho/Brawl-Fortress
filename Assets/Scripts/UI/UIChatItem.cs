using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIChatItem : MonoBehaviour
{
    public Image characterAvatar;
    public Text txtName;
    public Text txtContent;

    public void Init(Player sender, string content)
    {
        txtName.text = sender.NickName;
        txtContent.text = content;
        TF2Class tF2Class = TF2Class.None;
        Team team = Team.None;
        if (sender.CustomProperties.TryGetValue(TF2GameProps.TeamKey, out var teamObj))
        {
            team = (Team)teamObj;
        }

        if (sender.CustomProperties.TryGetValue(TF2GameProps.ClassKey, out var tf2ClassObj))
        {
            tF2Class = (TF2Class)tf2ClassObj;
            if (characterAvatar)
                characterAvatar.sprite = GUIManager.instan.GetAvatar(tF2Class, team);
        }
        
        
        txtName.color = GUIManager.instan.teamColor[(int)team];
    }
}
