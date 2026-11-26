using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overhead name + health bar (world-space canvas on every character), Brawl Stars style: the look is OverheadBarView
/// (chunky bordered bar with ruler ticks, white damage trail and hit flash, big bold health number, bold name in the
/// team colour; the local player also gets the orange ammo bar under it). It is built at runtime over the prefab's old
/// bar (hidden), so every character prefab gets it. Look / colours / sizes: Resources/UI/OverheadBarStyle.
/// </summary>
public class HeaderUIForFun : MonoBehaviour
{
    public Transform guiTransform;
    public Image image;
    private Transform mainCamera;
    public TF2Player ownerPlayer;
    public Image health;
    public TMP_Text txtName;

    OverheadBarView view;

    public void Start()
    {
        ownerPlayer.healthManager.OnDeath += OnPlayerDead;
        ownerPlayer.healthManager.OnRespawn += OnPlayerRespawn;
        ownerPlayer.healthManager.OnHealthChanged += OnHealthChanged;
        if (health) health.fillAmount = 1;
        mainCamera = CameraController.instan.GetCurrentCamera().transform;
        image.gameObject.SetActive(false);
        BuildBar();
        if (ownerPlayer is TF2Zombie && txtName != null) txtName.gameObject.SetActive(false); // zombies: health bar only
        if (ownerPlayer is TF2Boss) CreateBossIcon();
        if (view != null) view.SetHealth((int)ownerPlayer.healthManager.currentHealth, (int)ownerPlayer.healthManager.maxHealth, true);
        InitLate();                     // team colour right away when the local player is known (no green flash on enemies)
        Invoke(nameof(InitLate), 1);    // and again once every character has spawned
    }

    void BuildBar()
    {
        var style = OverheadBarStyle.Get();
        if (style == null || health == null) return;
        var oldBar = health.transform.parent as RectTransform;   // "Health" (background) holding "Fill"
        var canvas = (oldBar != null ? oldBar.parent : health.transform.parent) as RectTransform;
        Vector2 at = oldBar != null ? oldBar.anchoredPosition : Vector2.zero;
        if (oldBar != null) oldBar.gameObject.SetActive(false);
        view = OverheadBarView.Create(canvas, at, style, txtName, !ownerPlayer.isBot);
    }

    // ------------------------------------------------------------------ data

    public void InitLate()
    {
        var style = OverheadBarStyle.Get();
        Color c;
        if (ownerPlayer.isMainPlayer) c = style != null ? style.mine : GUIManager.instan.mineColor;
        else if (ArenaManager.Instance != null && ArenaManager.Instance.mainPlayer != null && ownerPlayer.team == ArenaManager.Instance.mainPlayer.team) c = style != null ? style.ally : GUIManager.instan.teamMateColor;
        else c = style != null ? style.enemy : GUIManager.instan.enemyColor;

        if (health) health.color = c;
        if (view != null)
        {
            view.SetTeamColor(c);
            // name in a lighter tint of the team colour, readable on any background
            view.SetName(ownerPlayer.playerName, Color.Lerp(c, Color.white, style != null ? style.nameLighten : 0.3f));
        }
        else if (txtName) txtName.text = ownerPlayer.playerName;
    }

    public void OnPlayerDead()
    {
        gameObject.SetActive(false);
    }

    public void OnPlayerRespawn()
    {
        gameObject.SetActive(true);
        if (health) health.fillAmount = 1;
        if (view != null) view.SetHealth((int)ownerPlayer.healthManager.maxHealth, (int)ownerPlayer.healthManager.maxHealth, true);
    }

    public void OnHealthChanged(int crHealth, int maxHealth)
    {
        if (health) health.fillAmount = maxHealth > 0 ? crHealth * 1.0f / maxHealth : 0f;
        if (view != null) view.SetHealth(crHealth, maxHealth);
    }

    /// <summary>Bullets shown in the ammo bar: the clip, and while reloading the share already loaded (smooth).</summary>
    static float AmmoShown(Gun gun, int clip)
    {
        float a = gun.bulletInClip;
        if (gun.IsReloading && gun.maxReloadLoop > 0)
            a = Mathf.Max(a, clip - gun.maxReloadLoop + Mathf.Max(0f, gun.ReloadProgress) * gun.maxReloadLoop);
        return Mathf.Min(a, clip);
    }

    public void InitData(Sprite sprite)
    {
        if (ownerPlayer.healthManager.death)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        image.gameObject.SetActive(true);
        mainCamera = CameraController.instan.GetCurrentCamera().transform;

        image.sprite = sprite;
        StopAllCoroutines();
        StartCoroutine(WaitForOff());
    }

    IEnumerator WaitForOff()
    {
        yield return new WaitForSeconds(5);
        image.gameObject.SetActive(false);
    }

    // ---- boss: the Boss Battle tab icon above the head, bobbing
    RectTransform bossIcon; Vector2 bossIconPos;

    void CreateBossIcon()
    {
        var sprite = Resources.Load<Sprite>("UI/boss_mode_icon");
        var parent = image != null ? image.rectTransform.parent : null;
        if (sprite == null || parent == null) return;
        var go = new GameObject("BossIcon", typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        bossIcon = (RectTransform)go.transform; bossIcon.SetParent(parent, false);
        bossIcon.anchorMin = bossIcon.anchorMax = image.rectTransform.anchorMin;
        bossIconPos = image.rectTransform.anchoredPosition;
        float h = Mathf.Max(image.rectTransform.rect.height, 100f) * 1.6f;
        bossIcon.sizeDelta = new Vector2(h * sprite.rect.width / sprite.rect.height, h);
        bossIconPos += Vector2.up * h * 1.05f;   // above the head: the medic-call slot sits at head height and the icon covered the hat
        bossIcon.anchoredPosition = bossIconPos;
        var img = go.GetComponent<Image>(); img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false;
        var sh = go.AddComponent<Shadow>(); sh.effectColor = new Color(0f, 0f, 0f, 0.5f); sh.effectDistance = new Vector2(0f, -h * 0.05f);
        bossIcon.SetAsLastSibling();
    }

    void LateUpdate()
    {
        guiTransform.LookAt(guiTransform.position + mainCamera.forward);
        if (bossIcon != null) bossIcon.anchoredPosition = bossIconPos + Vector2.up * (Mathf.Sin(Time.time * 2.5f) * bossIcon.sizeDelta.y * 0.08f);

        // the local player's ammo bar (melee and building tools: none)
        if (view != null && ownerPlayer.isMainPlayer)
        {
            var gun = ownerPlayer.FPSPlayer != null ? ownerPlayer.FPSPlayer.currWeapon as Gun : null;
            int clip = gun != null && gun.weaponConfig != null ? gun.weaponConfig.weaponStats.clipSize : 0;
            view.SetAmmo(clip > 0 ? AmmoShown(gun, clip) : 0f, clip);
        }
    }
}
