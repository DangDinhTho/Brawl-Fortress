using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchAvatarGroup : MonoBehaviour
{
    public Transform[] teamAvts;
    public AvatarItem avatarItemPref;
    private Dictionary<int, AvatarItem> matchAvts = new Dictionary<int, AvatarItem>();

    private void Awake()
    {
        avatarItemPref.gameObject.SetActive(false);
    }

    public void CreateCharacterAvatar(TF2Player player)
    {
        int team = (int)player.team;
        GameObject obj = Instantiate(avatarItemPref.gameObject, teamAvts[team]);
        obj.SetActive(true);
        AvatarItem item = obj.GetComponent<AvatarItem>();
        item.SetAvatar(player);
        if(matchAvts.TryGetValue(player.viewID, out var avtItem))
        {
            matchAvts[player.viewID] = item;
        }
        else
        {
            matchAvts.Add(player.viewID, item);
        }
    }
}
