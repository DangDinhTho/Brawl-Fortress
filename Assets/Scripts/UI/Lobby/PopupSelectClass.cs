using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PopupSelectClass : PopupBase
{
    //public Transform selectBtnContainer;
    //public List<UIClassSelectionItem> listSelectBtn;
    private bool siglePlayer;
    private PopupLoading popupLoading;
    public Text txtClassHeader;
    public Text txtClassDescription;

    public UILoadoutSlot[] loadoutSlots;
    [HideInInspector]
    public List<UILoadoutSlot> uiLoadoutSlots = new List<UILoadoutSlot>();
    public Dictionary<ItemType, UILoadoutSlot> weaponSlots = new Dictionary<ItemType, UILoadoutSlot>();

    private void Init()
    {
        for (int i = 0; i < loadoutSlots.Length; i++)
        {
            UILoadoutSlot slot = loadoutSlots[i];

            if (slot.itemType > ItemType.WEAPON && slot.itemType < ItemType.END_WEAPON)
            {
                weaponSlots.Add(slot.itemType, slot);
            }
            else
            {
                uiLoadoutSlots.Add(slot);
            }
        }
    }

    public override void OnCreate(params object[] args)
    {
        Init();
        siglePlayer = false;
        if(args.Length > 0)
        {
            siglePlayer = (bool)args[0] && PhotonNetwork.CurrentRoom == null;
        }

        SetData(HomeManager.instance.characterPref.tF2Class);
        //if(listSelectBtn == null || listSelectBtn.Count == 0)
        //{
        //    listSelectBtn = new List<UIClassSelectionItem>();
        //    foreach(Transform child in selectBtnContainer)
        //    {
        //        UIClassSelectionItem item = child.GetComponent<UIClassSelectionItem>();
        //        if (item != null)
        //        {
        //            item.popupSelectClass = this;
        //            listSelectBtn.Add(item);
        //        }
        //    }
        //}

        //for(int i = 0; i < listSelectBtn.Count; i++)
        //{
        //    listSelectBtn[i].Check();
        //}

        OnSelectClass(HomeManager.instance.characterPref.tF2Class);
        
    }

    public void SetData(TF2Class _tf2Class)
    {
        //this.tf2Class = _tf2Class;
        CharacterPref characterPref = CharacterSave.GetCharacterPref(_tf2Class);
       // HomeManager.instance.SetCharacterClass(_tf2Class);

        foreach (var uiItem in loadoutSlots)
        {
            uiItem.SetItemViewOnly(ItemId.None);
        }


        //if (characterPref.itemLoadOut != null)
        //{
        int slotIndex = 0;
        for (int i = 0; i < characterPref.itemLoadOut.Count; i++)
        {
            var equiptData = characterPref.itemLoadOut.ElementAt(i);
            if (equiptData.Value != ItemId.None)
            {
                if (equiptData.Key > ItemType.WEAPON && equiptData.Key < ItemType.END_WEAPON)
                {
                    if (weaponSlots.ContainsKey(equiptData.Key))
                        weaponSlots[equiptData.Key].SetItemViewOnly(equiptData.Value);

                }
                else
                {
                    uiLoadoutSlots[slotIndex].SetItemViewOnly(equiptData.Value);
                    slotIndex++;
                }
                //slotsEquipted.Add(equiptData.Key);
            }
        }
        //foreach (var loadoutItem in characterPref.itemLoadOut)
        //{
        //    if (uiLoadoutSlots.ContainsKey(loadoutItem.Key))
        //        uiLoadoutSlots[loadoutItem.Key].SetItemViewOnly(loadoutItem.Value);
        //}

        //}

        //txtCharacterName.text = ">>  " + LocalizationManager.Localize(_tf2Class.ToString());
        HomeManager.instance.characterContainer.localRotation = Quaternion.Euler(0, 0, 0);
    }

    public void OnSelectClass(TF2Class tF2Class)
    {
        txtClassHeader.text = LocalizationManager.Localize(tF2Class.ToString().ToUpper());
        txtClassDescription.text = LocalizationManager.Localize($"{tF2Class}_desc");
        //StopAllCoroutines();
        //StartCoroutine(SwitchWeaponDelay(tF2Class));
        HomeManager.instance.remotePlayer.SwitchWeaponItem( ConfigManager.classMenuWeapon[tF2Class]);
        HomeManager.instance.remotePlayer.playerAnimator.Animator.SetTrigger("Menu");
        HomeManager.instance.ResetCharacterRotate();
        SetData(tF2Class);
    }

    IEnumerator SwitchWeaponDelay(TF2Class tF2Class)
    {
        yield return new WaitForSeconds(0.2f);
        
    }

    public void OpenLoadoutClick()
    {
       var popupLoadout =  GUIManager.instan.CreatePopup<PopupLoadout>();
        popupLoadout.OpenLoadout(HomeManager.instance.characterPref.tF2Class);
    }

    public void OnDoneClick()
    {
        if (siglePlayer)
        {
            if (singleStarting) return; // double tap on Done
            if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
            {
                singleStarting = true;
                LobbyManager.instance.singlePlayer = true;
                string roomName = null; // let the server generate a unique name (a fixed name could collide)

                 ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                {TF2GameProps.GameModeKey, LobbyManager.instance.sigleGameMode},
                {TF2GameProps.Map, LobbyManager.instance.sigleMap}

            };
                Debug.Log(LobbyManager.instance.sigleMap);

                RoomOptions options = new RoomOptions { MaxPlayers = (byte)ConfigManager.maxPlayerPerRoom, CustomRoomProperties = props, IsVisible = false };
                PhotonNetwork.CreateRoom(roomName, options, null);
                StartCoroutine(WaitPlaySingle());
                popupLoading = GUIManager.instan.CreatePopup<PopupLoading>(LocalizationManager.Localize("matching"));
                CharacterSave.SaveLastCharacter(HomeManager.instance.characterPref.tF2Class);
            }
            else
            {
                GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("disconected"), GUIManager.instan.iconAtlas.GetSprite("disconnected"), (System.Action)(() => PhotonNetwork.ConnectUsingSettings()));
            }
        }
        else
        {
            Dismiss();
        }
        
    }

    private bool singleStarting;

    IEnumerator WaitPlaySingle()
    {
        float timeOut = 0;
        while(PhotonNetwork.CurrentRoom == null)
        {
            timeOut += Time.deltaTime;
            if(timeOut > 10)
            {
                popupLoading.Dismiss();
                singleStarting = false;
                LobbyManager.instance.singlePlayer = false;
                GUIManager.instan.CreatePopup<PopupNotify>(LocalizationManager.Localize("disconected"));
                yield break;
            }
            yield return null;
        }

        yield return new WaitForSeconds(Random.Range(5f, 10f));
        SceneManager.LoadScene("Arena");
    }
}
