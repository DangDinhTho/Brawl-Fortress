using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class UICharacterInputController : MonoBehaviour
{
    public static UICharacterInputController instan;
    public Joystick joystick;
    public Joystick aimingJoystick;
    public FireButtonController fireButton;
    [HideInInspector]
    public bool jump;
    public Dropdown weaponDropdown;
    private TF2Player mainPlayer;
    private Dictionary<int, WeaponType> weaponSelector = new Dictionary<int, WeaponType>();
    private bool freeLookCam;
    public Text txtDeathReason;
    public Text txtTimeSpawn;
    public GameObject chatInMatchItemPref;
    public Transform chatContainer;
    private List<GameObject> chatStack = new List<GameObject>();
    public int maxChatCache = 6;
    public InputField chatInput;

    private Coroutine countHideChat;
    public CustomUIItem[] customUIItems;
    public Toggle chatAllToggle;
    public Text txtChatDirec;
    public Sprite[] chatIcons;
    
    private void Awake()
    {
        instan = this;
    }

    private void Start()
    {
        ArenaManager.Instance.OnMainPlayerSpawned += OnPlayerCreate;

        int chatDirect = 1;
        if (PlayerPrefs.HasKey("ChatDirection"))
        {
            chatDirect = PlayerPrefs.GetInt("ChatDirection");
        }

        if (chatDirect == 1)
        {
            chatAllToggle.isOn = true;
            OnChatAllChanged(true);
        }
        else
        {
            chatAllToggle.isOn = false;
            OnChatAllChanged(false);
        }

        chatAllToggle.onValueChanged.AddListener(OnChatAllChanged);
        chatInput.gameObject.SetActive(ConfigManager.multiplay);

    }

    public void OnPlayerCreate(TF2Player player)
    {
        mainPlayer = player;
        StartCoroutine(SetupPlayerDelay());
    }

    IEnumerator SetupPlayerDelay()
    {
        yield return new WaitForSeconds(0.2f);
        List<Dropdown.OptionData> optionDatas = new List<Dropdown.OptionData>();
        for (int i = 0; i < mainPlayer.FPSPlayer.weaponSlots.Count; i++)
        {
            WeaponType weaponSlot = mainPlayer.FPSPlayer.weaponSlots.ElementAt(i).Key;
            weaponSelector.Add(i, weaponSlot);
            Dropdown.OptionData option = new Dropdown.OptionData()
            {
                text = LocalizationManager.Localize(mainPlayer.FPSPlayer.weaponSlots.ElementAt(i).Value.weaponId.ToString()),
                image = GUIManager.instan.itemIconAtlas.GetSprite(mainPlayer.FPSPlayer.weaponSlots.ElementAt(i).Value.weaponId.ToString())
            };
            optionDatas.Add(option);
        }
        weaponDropdown.ClearOptions();
        weaponDropdown.AddOptions(optionDatas);
        weaponDropdown.onValueChanged.AddListener(OnWeaponChange);
        mainPlayer.healthManager.OnDeathReason += OnPlayerDeath;
        mainPlayer.healthManager.OnRespawn += OnPlayerRespawn;
        mainPlayer.healthManager.OnRespawnCountDownUpdate += OnTimeRespawnUpdate;
    }

    public void SetWeaponTypeWithoutNotify(WeaponType weaponType)
    {
        foreach (var weapon in weaponSelector)
        {
            if (weapon.Value == weaponType && weaponDropdown.value != weapon.Key)
            {
                weaponDropdown.SetValueWithoutNotify(weapon.Key);
            }
        }
    }

    public void OnPlayerDeath(int attacker)
    {
        if (ArenaManager.Instance.characterScores.TryGetValue(attacker, out var playerScore))
        {
            
            txtDeathReason.text = string.Format(LocalizationManager.Localize("death_reason"), playerScore.playerName);
        }
        else
        {
            txtDeathReason.text = LocalizationManager.Localize("death_no_reason");
        }
        txtDeathReason.gameObject.SetActive(true);
        txtTimeSpawn.gameObject.SetActive(true);
    }

    public void OnPlayerRespawn()
    {
        txtDeathReason.gameObject.SetActive(false);
        txtTimeSpawn.gameObject.SetActive(false);
    }
    
    public void OnTimeRespawnUpdate(int time)
    {
        txtTimeSpawn.text = string.Format(LocalizationManager.Localize("time_respawn_remaining"), time);
    }
    
    public void JumpInput(bool value)
    {
        jump = value;
        mainPlayer.FPSPlayer.jumpInput = value;
    }

    public void OnWeaponChange(int value)
    {
        WeaponType slot = weaponSelector[value];
        mainPlayer.FPSPlayer.SwitchWeapon(slot);
    }

    public void SwitchCam()
    {
        if(mainPlayer.healthManager.death) return;

        if(freeLookCam && mainPlayer.remotePlayer.currentTaunt != Taunt.None)
        {
            mainPlayer.remotePlayer.EndTaunt();
            return;
        }

        SwitchCamSuccess();
        
    }

    public void SwitchCamSuccess()
    {
        freeLookCam = !freeLookCam;
        if (freeLookCam)
            mainPlayer.FPSPlayer.SwitchTPS();
        else
        {
            mainPlayer.FPSPlayer.SwitchFPS();
        }


        CameraController.instan.SetFreeLook(freeLookCam);
    }

    public void ReloadClick()
    {
        mainPlayer.FPSPlayer.ReloadClick();
    }

    public void OnTauntBtnClick()
    {
        if(mainPlayer.remotePlayer.currentTaunt != Taunt.None)
        {
            mainPlayer.FPSPlayer.EndTaunt();
        }
        else
        {
            GUIManager.instan.CreatePopup<PopupTaunts>();
        }
    }

    public void OpenSettingClick()
    {
        GUIManager.instan.CreatePopup<PopupSetting>(true);
    }


    public void OnChatAllChanged(bool isOn)
    {
        if (isOn)
        {
            txtChatDirec.text = LocalizationManager.Localize("chat_all");
        }
        else
        {
            txtChatDirec.text = LocalizationManager.Localize("chat_team");
        }

        PlayerPrefs.SetInt("ChatDirection", isOn ? 1 : 0);
    }
    
    public void SendChat()
    {
        string s = chatInput.text;
        if (s.Length > 50) { chatInput.text = ""; return; }
        ArenaNetworkManager.instan.SendChat(s, chatAllToggle.isOn);
        chatInput.text = "";
    }

    public void OnReceiveChat(Player sender, string content, bool chatAll)
    {
        if (!chatAll)
        {
            if (sender.CustomProperties.TryGetValue(TF2GameProps.TeamKey, out var teamObj))
            {
                Team team = (Team)teamObj;
                if(team != mainPlayer.team) return;
            }
        }

        if (int.TryParse(content, out var numIcon))
        {
            if (numIcon < chatIcons.Length && ArenaManager.Instance.allRealPlayers.TryGetValue(sender.ActorNumber, out var player))
            {
                player.headerUIForFun.InitData(chatIcons[numIcon]);
                if (player.characterConfig.funVoices != null && numIcon < player.characterConfig.funVoices.Length)
                {
                    player.audioSource.PlayOneShot(player.characterConfig.funVoices[numIcon]);
                }
            }
            return;
        }
        
        chatContainer.gameObject.SetActive(true);
        GameObject chatItem = Instantiate(chatInMatchItemPref, chatContainer);
        chatItem.SetActive(true);
        chatItem.GetComponent<UIChatItem>().Init(sender, content);
        chatStack.Add(chatItem);
        if(chatStack.Count > maxChatCache)
        {
            Destroy(chatStack[0]);
            chatStack.RemoveAt(0);
        }
        if(countHideChat != null) StopCoroutine(countHideChat);
        countHideChat = StartCoroutine(HideChat());
    }

    IEnumerator HideChat()
    {
        yield return new WaitForSeconds(10);
        chatContainer.gameObject.SetActive(false);
        countHideChat = null;
    }
}
