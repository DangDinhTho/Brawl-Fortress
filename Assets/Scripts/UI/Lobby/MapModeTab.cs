using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One game-mode tab on the left of PopupSelectMap ("All" or a single GameMode).
/// Selecting it slides the tab out, fades in the bright background + pulsing glow and bounces the icon.
/// </summary>
public class MapModeTab : MonoBehaviour
{
    public bool isAll;
    public GameMode gameMode;

    [Header("Parts")]
    public Button button;
    public RectTransform body;
    public CanvasGroup selectedBg;
    public CanvasGroup glow;
    public RectTransform icon;
    public TMP_Text label;
    public TMP_Text count;

    [Header("Style")]
    public Color labelNormal = new Color(0.7f, 0.8f, 1f, 1f);
    public Color labelSelected = Color.white;
    public float selectedOffset = 16f;

    [HideInInspector] public PopupSelectMap popup;

    private Sequence seq;
    private Tween glowLoop;
    private Tween labelTween;

    private void Awake()
    {
        if (button) button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (popup) popup.SelectTab(this);
    }

    public bool Matches(UIMapIcon map)
    {
        return map != null && (isAll || map.gameMode == gameMode);
    }

    public void SetCount(int n)
    {
        if (count) count.text = n.ToString();
    }

    public void SetSelected(bool value, bool animate)
    {
        KillTweens();
        float x = value ? selectedOffset : 0f;
        float a = value ? 1f : 0f;
        Color lc = value ? labelSelected : labelNormal;

        if (!animate)
        {
            if (body) { body.anchoredPosition = new Vector2(x, body.anchoredPosition.y); body.localScale = Vector3.one; }
            if (selectedBg) selectedBg.alpha = a;
            if (glow) glow.alpha = a;
            if (label) label.color = lc;
            if (icon) { icon.localScale = Vector3.one; icon.localRotation = Quaternion.identity; }
            if (value) StartGlow();
            return;
        }

        seq = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        if (body)
        {
            body.localScale = Vector3.one;
            seq.Join(body.DOAnchorPosX(x, 0.28f).SetEase(value ? Ease.OutBack : Ease.OutQuad));
            if (value) seq.Join(body.DOPunchScale(Vector3.one * 0.08f, 0.3f, 6, 0.6f));
        }
        if (selectedBg) seq.Join(selectedBg.DOFade(a, 0.2f));
        if (glow) seq.Join(glow.DOFade(a, 0.25f));
        if (label)
        {
            Color from = label.color;
            labelTween = DOTween.To(() => from, c => { from = c; label.color = c; }, lc, 0.2f).SetUpdate(true).SetTarget(this);
        }
        if (value && icon)
        {
            icon.localScale = Vector3.one;
            icon.localRotation = Quaternion.identity;
            seq.Join(icon.DOPunchScale(Vector3.one * 0.3f, 0.4f, 6, 0.5f));
            seq.Join(icon.DOPunchRotation(new Vector3(0, 0, 15f), 0.45f, 8, 0.5f));
        }
        if (value) seq.OnComplete(StartGlow);
    }

    private void StartGlow()
    {
        if (!glow) return;
        glow.alpha = 1f;
        glowLoop = glow.DOFade(0.4f, 0.75f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this);
    }

    private void KillTweens()
    {
        seq?.Kill(true);
        glowLoop?.Kill();
        labelTween?.Kill();
        seq = null; glowLoop = null; labelTween = null;
    }

    private void OnDisable()
    {
        KillTweens();
    }
}
