using TMPro;
using UnityEngine;

/// <summary>
/// UkDefense CombatDamagePopup 연출 — 스크린 오버레이(카메라 null에도 표시).
/// </summary>
[DefaultExecutionOrder(2000)]
public class CwslDamagePopup : MonoBehaviour
{
    private const float FloatSpeed = 2.5f;
    private const float Lifetime = 0.95f;
    private const float PopScale = 1.2f;
    private const float EndScale = 0.94f;
    private const float PopPhase = 0.15f;

    private TMP_Text damageText;
    private CanvasGroup canvasGroup;
    private RectTransform rect;
    private Vector3 worldAnchor;
    private Color baseColor = Color.white;
    private float elapsed;
    private bool isPlaying;

    public void Bind(TMP_Text text, CanvasGroup group, RectTransform popupRect)
    {
        damageText = text;
        canvasGroup = group;
        rect = popupRect;
    }

    public void Play(float damage, CwslDamagePopupKind kind, Vector3 worldAnchor)
    {
        EnsureBound();

        this.worldAnchor = worldAnchor;
        elapsed = 0f;
        isPlaying = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (damageText != null)
        {
            damageText.text = kind switch
            {
                CwslDamagePopupKind.Blocked => "BLOCK",
                CwslDamagePopupKind.Heal => $"+{Mathf.CeilToInt(damage)}",
                _ => Mathf.CeilToInt(damage).ToString()
            };
            baseColor = ResolveColor(kind);
            damageText.color = baseColor;
            damageText.alpha = 1f;
            damageText.enabled = true;
            damageText.gameObject.SetActive(true);
            CwslDamagePopupPool.TryApplyOutline(damageText);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.gameObject.SetActive(true);
        }

        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.gameObject.SetActive(true);
        }

        SyncScreenPosition();
    }

    private void LateUpdate()
    {
        if (!isPlaying)
            return;

        elapsed += Time.deltaTime;
        SyncScreenPosition();

        var popT = Mathf.Clamp01(elapsed / Lifetime);
        var scaleMul = popT < PopPhase
            ? Mathf.Lerp(1f, PopScale, popT / PopPhase)
            : Mathf.Lerp(PopScale, EndScale, (popT - PopPhase) / (1f - PopPhase));

        if (rect != null)
            rect.localScale = Vector3.one * scaleMul;

        var alpha = 1f - Mathf.SmoothStep(0f, 1f, popT);
        if (damageText != null)
        {
            var color = baseColor;
            color.a = alpha;
            damageText.color = color;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = alpha;

        if (elapsed >= Lifetime)
            CwslDamagePopupPool.Release(this);
    }

    private void SyncScreenPosition()
    {
        if (rect == null)
            return;

        var floatedWorld = worldAnchor + Vector3.up * (FloatSpeed * elapsed);
        if (CwslDamagePopupPool.TryWorldToScreenLocal(floatedWorld, out var localPoint))
        {
            rect.anchoredPosition = localPoint;
            return;
        }

        rect.anchoredPosition = Vector2.zero;
    }

    internal void StopAndHide()
    {
        EnsureBound();

        isPlaying = false;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (damageText != null)
        {
            damageText.text = string.Empty;
            damageText.alpha = 0f;
        }

        gameObject.SetActive(false);
    }

    private void EnsureBound()
    {
        if (rect == null)
            rect = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (damageText != null)
            return;

        damageText = GetComponentInChildren<TMP_Text>(true);
        if (damageText != null)
            CwslTmpFontUtil.ApplyFont(damageText);
    }

    private static Color ResolveColor(CwslDamagePopupKind kind)
    {
        return kind switch
        {
            CwslDamagePopupKind.Player => new Color(1f, 0.28f, 0.28f, 1f),
            CwslDamagePopupKind.Monster => new Color(1f, 0.92f, 0.35f, 1f),
            CwslDamagePopupKind.Projectile => new Color(0.78f, 0.35f, 0.95f, 1f),
            CwslDamagePopupKind.Blocked => new Color(0.45f, 0.85f, 1f, 1f),
            CwslDamagePopupKind.Poison => new Color(0.45f, 0.95f, 0.45f, 1f),
            CwslDamagePopupKind.Heal => new Color(0.45f, 0.95f, 0.45f, 1f),
            CwslDamagePopupKind.Structure => new Color(1f, 0.45f, 0.18f, 1f),
            _ => new Color(1f, 0.92f, 0.35f, 1f)
        };
    }
}
