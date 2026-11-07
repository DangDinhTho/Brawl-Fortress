using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.SimpleLocalization
{
	/// <summary>
	/// Localize text component.
	/// </summary>
    //[RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string LocalizationKey;

        public void Start()
        {
            Localize();
            LocalizationManager.LocalizationChanged += Localize;
        }

        public void OnDestroy()
        {
            LocalizationManager.LocalizationChanged -= Localize;
        }

        private void Localize()
        {
            Text text = GetComponent<Text>();
            if(text != null)
            {
                text.text = LocalizationManager.Localize(LocalizationKey);
            }
            else
            {
                GetComponent<TMP_Text>().text = LocalizationManager.Localize(LocalizationKey);
            }
        }
    }
}