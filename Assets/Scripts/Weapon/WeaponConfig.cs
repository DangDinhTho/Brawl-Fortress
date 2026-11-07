using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "ScriptableObjects/WeaponConfig")]
public class WeaponConfig: ScriptableObject
{
    public WeaponId weaponId;
    public int weaponAnim;
    //public WeaponType weaponType;
    public BulletId bulletId;
    //public float interval = 1f;
    //public float delayFire = 0;
    //public float delayFinishFire = 0;
    //public float delayReload = 0;
    //public float timeReload;
    //public float delayFinishReload;
    //public int clipSize;
    //public int inventorySize;
    //public float damage;
    //public float minDamage;
    //public float maxDamage;
    //public float critDamage;
    public Sprite crosshair;
    public AudioClip shotSound;
    public AudioClip beginFireSound;
    public AudioClip finishFireSound;
    public AudioClip reloadSound;
    public WeaponStats weaponStats;

    public void LoadStats()
    {
        weaponStats = ConfigManager.weaponStats[weaponId];
    }
}

public class WeaponStats
{
    public WeaponId weaponId;
    public float interval = 1f;
    public float delayFire = 0;
    public float delayFinishFire = 0;
    public float delayReload = 0;
    public float timeReload;
    public float delayFinishReload;
    public int clipSize;
    public int inventorySize;
    public float damage;
    public float minDamage;
    public float maxDamage;
    public float critDamage;
}

public enum WeaponType : byte
{
    None = 0,
    Primary,
    Secondary,
    Mele,
    Building,
    PDA,
    Item1,
    Item2,
    
}


public enum WeaponState
{
    Idle = 0,
    //DelayStartFire,
    Firing,
    //DelayFinishFire,
    //DelayStartReload,
    Reloading,
    //DelayFinishReload
}

public enum WeaponId : byte
{
    None = 0,
    GrenadeLauncher,
    Bottle,
    StickybombLauncher,
    Shotgun,
    Pistol,
    Wrench,
    Minigun,
    Fists,
    SyringeGun,
    MedicGun,
    BoneSaw,
    Flamethrower,
    FireAxe,
    Scattergun,
    Bat,
    Sniperrifle,
    Smg,
    Kukri,
    RocketLauncher,
    Shovel,
    Knife,
    Revolver,
    ToolBox,
    BuilderPDA,
    BlackBox,
    Powerjack,
    SharpDresser,
    Amputator,
    AirStrike,
    BlackRose,
    WarriorSpirit,
    Atomizer,
    CrusaderCrossbow,
    Degreaser,
    Phlogistinator,
    EurekaEffect,
    SouthernHospitality,
    Jag,
    PanicAttack,
    RescueRanger,
    HolidayPunch,
    IronCurtain,
    LibertyLauncher,
    BeggarBazooka,
    Ham,
    Batsaber,
    ShootingStar,
    HitmanHeatmaker,
    SydneySleeper,
    Vaccinator,
    PrettyBoyPocketPistol,
    BabyFaceBlaster,
    SodaPopper,
    ReserveShooter,
    demoman_primary_0,
    demoman_primary_1,
    demoman_primary_2,
    demoman_primary_3,
    demoman_primary_4,
    demoman_secondary_0,
    demoman_secondary_1,
    demoman_secondary_2,
    demoman_secondary_3,
}

public enum BulletId
{
    None = 0,
    Grenade_GrenadeLauncher,
    StickyBomb,
    MinigunBullet,
    ShotgunBullet,
    Rocket,
    SyringeBullet,
    SentryRocket,
}
