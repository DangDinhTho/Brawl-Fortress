using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Item icons (WorkShop/ItemIcon) are now pre-styled: brighter, cel-shaded, with a baked black outline.
/// So an Image showing an item icon must NOT use the old toon/outline material or the UI Outline/Shadow effect.
/// Images that switch between item icons and other sprites (currency, weapon type...) get their original
/// material/effects back via SetOther().
/// </summary>
public static class ItemIconStyle
{
    class Cache : MonoBehaviour
    {
        public Material material;
        public Shadow[] effects;
        public bool[] effectsEnabled;
    }

    static Cache GetCache(Image img)
    {
        var c = img.GetComponent<Cache>();
        if (c != null) return c;
        c = img.gameObject.AddComponent<Cache>();
        c.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
        c.material = img.material == img.defaultMaterial ? null : img.material;
        c.effects = img.GetComponents<Shadow>(); // Outline derives from Shadow
        c.effectsEnabled = new bool[c.effects.Length];
        for (int i = 0; i < c.effects.Length; i++) c.effectsEnabled[i] = c.effects[i].enabled;
        return c;
    }

    /// <summary>Call after assigning an item icon sprite.</summary>
    public static void SetItem(Image img)
    {
        if (img == null) return;
        var c = GetCache(img);
        img.material = null;
        foreach (var e in c.effects) if (e) e.enabled = false;
    }

    /// <summary>Call after assigning a non-item sprite (currency, weapon type...).</summary>
    public static void SetOther(Image img)
    {
        if (img == null) return;
        var c = img.GetComponent<Cache>();
        if (c == null) return; // never switched, still original
        img.material = c.material;
        for (int i = 0; i < c.effects.Length; i++) if (c.effects[i]) c.effects[i].enabled = c.effectsEnabled[i];
    }

    /// <summary>For popups that receive an arbitrary sprite: detects whether it comes from the item icon atlas.</summary>
    public static void Auto(Image img)
    {
        if (img == null) return;
        var s = img.sprite;
        bool isItem = false;
        if (s != null && GUIManager.instan != null && GUIManager.instan.itemIconAtlas != null)
        {
            var probe = GUIManager.instan.itemIconAtlas.GetSprite(s.name.Replace("(Clone)", ""));
            isItem = probe != null && probe.texture == s.texture;
            if (probe != null) Object.Destroy(probe);
        }
        if (isItem) SetItem(img); else SetOther(img);
    }
}
