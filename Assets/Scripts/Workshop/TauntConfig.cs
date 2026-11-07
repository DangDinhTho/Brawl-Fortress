using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TauntConfig", menuName = "ScriptableObjects/TauntConfig")]
public class TauntConfig : ScriptableObject
{
    public Taunt taunt;
    public ItemId equipItemId;
    public WeaponId WeaponId;
    public TF2Class character;
    public float time;
    public float finishTime;
    public bool loop;
    public AudioClip audio;
}

public enum Taunt : byte
{
    None = 0,
    demoman_true_scotsmans_call,
    demoman_taunt_the_pooped_deck,
    demoman_taunt_shipwheel,
    demoman_taunt_scotsmans_stagger,
    engineer_bucking_bronco,
    engineer_taunt_bumpkins_banjo,
    engineer_taunt_texas_truckin,
    engineer_taunt_jackhammer_rodeo,

    medic_taunt_time_out_therapy,
    medic_taunt_surgeons_squeezebox,
    medic_taunt_doctors_defibrillators,
    medic_taunt_the_mannbulance,
    heavy_taunt_bare_knuckle_beatdown,
    heavy_taunt_soviet_strongarm,
    heavy_taunt_soviet_showoff,
    heavy_taunt_heavy_workout,
    pyro_taunt_spring_rider,
    pyro_taunt_the_grilled_gunman,
    pyro_taunt_the_hot_wheeler,
    pyro_taunt_skating_scorcher,
    pyro_taunt_scorchers_solo,

    scout_taunt_runners_rhythm,
    scout_taunt_the_boston_boarder,
    scout_taunt_the_boston_breakdance,
    scout_taunt_the_bunnyhopper,
    scout_taunt_the_homerunners_hobby,
    scout_taunt_spintowin,
    scout_taunt_the_trackmans_touchdown,

    sniper_taunt_didgeridrongo,
    sniper_taunt_killer_solo,
    sniper_taunt_most_wanted,
    sniper_taunt_shooters_stakeout,

    soldier_taunt_fumblers_fanfare,
    soldier_taunt_the_profane_puppeteer,
    soldier_taunt_rocket_jockey,
    soldier_taunt_maggots_condolence,

    spy_taunt_spy_boxtrot,
    spy_taunt_disco_fever,
    spy_taunt_luxury_lounge,
    spy_taunt_the_travel_agent,


    taunt_texas_twirl_em,
    taunt_shanty_shipmate,
    taunt_drunk_manns_cannon,
    taunt_russian,
    
    taunt_0,
    taunt_1,
    taunt_2,
    taunt_3,
    taunt_4,
    taunt_5,

    Max
}
