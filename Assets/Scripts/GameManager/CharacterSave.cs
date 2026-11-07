using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CharacterSave : MonoBehaviour
{
    private static string CharacterSaveKey = "CharacterPref";
    private static string LastCharacter = "LastCharacterPref";
    private static string LastTeam = "LastTeamPref";

    public static CharacterPref getMainCharacterPref()
    {
        TF2Class tf2Class = TF2Class.None;
        if (PlayerPrefs.HasKey(LastCharacter))
        {
            tf2Class = (TF2Class)PlayerPrefs.GetInt(LastCharacter);
            
        }
        else
        {
            tf2Class = (TF2Class)Random.Range(1, 10);
            SaveLastCharacter(tf2Class);
        }
        return GetCharacterPref(tf2Class);
    }
    
    public static CharacterPref GetCharacterPref(TF2Class tf2Class)
    {
        string key = $"{CharacterSaveKey}_{tf2Class}";
        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            CharacterPref characterPref = JsonConvert.DeserializeObject<CharacterPref>(json);

            bool changed = false;
            List<ItemType> removeItems = new List<ItemType>();
            foreach(var item in characterPref.itemLoadOut)
            {
                if(item.Value > ItemId.WORKSHOP && GameManager.instan.userData.GetUserItem(item.Value) == null)
                {
                    removeItems.Add(item.Key);
                    changed = true;
                }
            }

            if (changed)
            {
                for(int i = 0; i < removeItems.Count; i++)
                {
                    characterPref.itemLoadOut.Remove(removeItems[i]);
                }
                SaveCharacterPref(characterPref);
            }

            return characterPref;
        }
        else
        {
            CharacterConfig characterConfig = ConfigManager.GetCharacterConfig(tf2Class);
            Dictionary<ItemType, ItemId> items = new Dictionary<ItemType, ItemId>();
            for (int i = 0; i < characterConfig.defaultWeapons.Length; i++)
            {
                if (characterConfig.defaultWeapons[i] != WeaponId.None)
                {
                    ItemType itemType = (ItemType)Enum.Parse(typeof(ItemType), ((WeaponType)(i + 1)).ToString());
                    ItemId itemId = (ItemId)Enum.Parse(typeof(ItemId), characterConfig.defaultWeapons[i].ToString());
                    items.Add(itemType, itemId);
                }
            }

            return new CharacterPref()
            {
                tF2Class = tf2Class,
                itemLoadOut = items
            };
        }
    }


    

    public static Team GetLastTeam()
    {
        if (PlayerPrefs.HasKey(LastTeam))
        {
            Team team = (Team)PlayerPrefs.GetInt(LastTeam);
            return team;
        }
        else
        {
            return (Team)Random.Range(0, 2);
        }
    }

    public static void SaveTeam(Team team)
    {
        PlayerPrefs.SetInt(LastTeam, (int)team);
    }

    public static void SaveLastCharacter(TF2Class tf2Class)
    {
        PlayerPrefs.SetInt(LastCharacter, (int)tf2Class);
    }

    public static void SaveCharacterPref(CharacterPref characterPref)
    {
        string json = JsonConvert.SerializeObject(characterPref);
        string key = $"{CharacterSaveKey}_{characterPref.tF2Class}";
        PlayerPrefs.SetString(key, json);
    }
}

[System.Serializable]
public class CharacterPref
{
    public TF2Class tF2Class;
    public Dictionary<ItemType, ItemId> itemLoadOut = new Dictionary<ItemType, ItemId>();
    //public Dictionary<WeaponType, WeaponId> weaponLoadOut = new Dictionary<WeaponType, WeaponId>();
}
