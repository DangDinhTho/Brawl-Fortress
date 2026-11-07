using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIWorkShopRandomItem : MonoBehaviour
{
    public Image icon;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ShowRandomIcon());
    }

    IEnumerator ShowRandomIcon()
    {
        List<ItemConfig> itemConfigs = ConfigManager.itemConfigs.Values.ToList();
        while (true)
        {
            int ran = Random.Range(0, ConfigManager.itemConfigs.Count);
            if (itemConfigs[ran].itemId < ItemId.IgnoreShow)
            {
                icon.sprite = GUIManager.instan.GetItemIcon(itemConfigs[ran].itemId);
            }
            yield return new WaitForSeconds(Random.Range(4f, 6f));
        }
    }
}
