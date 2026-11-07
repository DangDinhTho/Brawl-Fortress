using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupTaunts : PopupBase
{
    //public GameObject tauntItemPref;
    public Transform tauntsContainer;
    private List<UserItem> allTaunt;
    private bool inited;
    
    public override void OnCreate(params object[] args)
    {
        //tauntItemPref.SetActive(false);
        base.OnCreate(args);
        
        if (!inited)
        {
            int i = 0;
            //allTaunt = CheatAllTaunt(ArenaManager.Instance.mainPlayer.tF2Class);
            allTaunt = GameManager.instan.userData.GetUserTaunts();
            if(allTaunt == null || allTaunt.Count == 0)
            {

            }
            else
            {
                for (; i < allTaunt.Count; i++)
                {
                    var item = GUIManager.instan.CreateListItem<UITauntItemButton>(tauntsContainer, i);
                    item.Init(allTaunt[i], this);
                }
            }
            GUIManager.instan.CleanListItem(tauntsContainer, i);
            inited = true;
        }
        
    }

    //private Taunt[] CheatAllTaunt(TF2Class mainPlayerClass)
    //{
    //    List<Taunt> taunts = new List<Taunt>();
    //    for (Taunt taunt = Taunt.None; taunt < Taunt.Max; taunt++)
    //    {
    //        if (taunt.ToString().StartsWith(mainPlayerClass.ToString()))
    //        {
    //            taunts.Add(taunt);
    //        }
    //    }

    //    return taunts.ToArray();
    //}
}
