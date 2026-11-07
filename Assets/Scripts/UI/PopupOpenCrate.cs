using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupOpenCrate : PopupBase
{
    private CraftResultController craftResultController;
    public AudioClip[] openCrateSounds;
    ItemId itemId;
    public override void OnCreate(params object[] args)
    {
        base.OnCreate(args);
        itemId = (ItemId)args[0];
        
        
        craftResultController = HomeManager.instance.craftResultController;
        craftResultController.Hide();
        craftResultController.gameObject.SetActive(true);
        
        StartCoroutine(SoundEffect());
    }

    public override void OnDismiss()
    {
        craftResultController.gameObject.SetActive(false);
        base.OnDismiss();
    }

    IEnumerator SoundEffect()
    {
        HomeManager.instance.audioSource.Stop();
        yield return new WaitForSeconds(0.25f);
        HomeManager.instance.audioSource.PlayOneShot(openCrateSounds[0]);
        yield return new WaitForSeconds(0.55f);
        HomeManager.instance.audioSource.PlayOneShot(openCrateSounds[1]);
        yield return new WaitForSeconds(1.9f);
        HomeManager.instance.audioSource.PlayOneShot(openCrateSounds[2]);
        craftResultController.SetData(itemId);
    }
}
