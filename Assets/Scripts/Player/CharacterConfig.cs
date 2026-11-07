using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClassConfig", menuName = "ScriptableObjects/ClassConfig")]
public class CharacterConfig : ScriptableObject
{
    public TF2Class characterClass;
    public WeaponType defaultWeapon;
    //public int maxHealth;
    //public float speed;
    //public float jumpForce = 8f;
    //public bool doubleJump;

    public CharacterStats characterStats;
    
    public WeaponId[] defaultWeapons;
    public AudioClip[] voices;
    public AudioClip[] funVoices;
    public AudioClip[] takeDamageVoices;
}

public class CharacterStats
{
    public TF2Class characterClass;
    public int maxHealth;
    public float speed;
    public float jumpForce = 8f;
    public bool doubleJump;
}

public enum TF2Class : byte
{
    None = 0,
    demoman,
    engineer,
    heavy,
    medic,
    pyro,
    scout,
    sniper,
    soldier,
    spy
}
