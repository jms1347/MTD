using TMPro;
using UnityEngine;

[DefaultExecutionOrder(2000)]
public class CwslDamagePopup : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 2.6f;
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private float worldScale = 0.022f;
    [SerializeField] private float startScaleMultiplier = 1.1f;
    [SerializeField] private float popScaleMultiplier = 2.2f;
    [SerializeField] private float endScaleMultiplier = 6.5f;

    private TextMeshProUGUI damageText;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Vector3 anchor;
    private Color baseColor = Color.white;
    private float elapsed;
    private bool isPlaying;

    public void Bind(TextMeshProUGUI text, CanvasGroup group, Canvas worldCanvas)
    {
        damageText = text;
        canvasGroup = group;
        canvas = worldCanvas;
    }

    public void Play(float damage, CwslDamagePopupKind kind, Vector3 worldAnchor)
    {
        anchor = worldAnchor;
        elapsed = 0f;
        isPlaying = true;
        gameObject.SetActive(true);
        transform.position = anchor;
        transform.localScale = Vector3.one * (worldScale * startScaleMultiplier);

        var camera = Camera.main;
        if (camera != null)
            transform.rotation = camera.transform.rotation;

        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.worldCamera = camera;
        }

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
            damageText.enabled = true;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void LateUpdate()
    {
        if (!isPlaying)
            return;

        elapsed += Time.deltaTime;
        var t = Mathf.Clamp01(elapsed / lifetime);

        transform.position = anchor + Vector3.up * (floatSpeed * elapsed);

        var camera = Camera.main;
        if (canvas != null)
            canvas.worldCamera = camera;
        if (camera != null)
            transform.rotation = camera.transform.rotation;

        var scaleMul = EvaluateScaleMultiplier(t);
        transform.localScale = Vector3.one * (worldScale * scaleMul);

        var alpha = EvaluateAlpha(t);
        if (damageText != null)
        {
            var color = baseColor;
            color.a = alpha;
            damageText.color = color;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = alpha;

        if (elapsed >= lifetime)
            CwslDamagePopupPool.Release(this);
    }

    private float EvaluateScaleMultiplier(float t)
    {
        const float popPhase = 0.14f;
        if (t < popPhase)
        {
            var popT = t / popPhase;
            // 초반에 크게 팝
            return Mathf.Lerp(startScaleMultiplier, popScaleMultiplier, 1f - Mathf.Pow(1f - popT, 3f));
        }

        var growT = (t - popPhase) / (1f - popPhase);
        var eased = 1f - Mathf.Pow(1f - growT, 2.1f);
        return Mathf.Lerp(popScaleMultiplier, endScaleMultiplier, eased);
    }

    private static float EvaluateAlpha(float t)
    {
        // 초반은 선명, 후반에 빠르게 페이드
        if (t < 0.35f)
            return 1f;
        return 1f - Mathf.SmoothStep(0f, 1f, (t - 0.35f) / 0.65f);
    }

    internal void StopAndHide()
    {
        isPlaying = false;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (damageText != null)
            damageText.text = string.Empty;
        gameObject.SetActive(false);
    }

    private static Color ResolveColor(CwslDamagePopupKind kind)
    {
        return kind switch
        {
            CwslDamagePopupKind.Player => new Color(1f, 0.28f, 0.28f, 1f),
            CwslDamagePopupKind.Monster => new Color(1f, 0.92f, 0.28f, 1f),
            CwslDamagePopupKind.Projectile => new Color(0.95f, 0.5f, 1f, 1f),
            CwslDamagePopupKind.Blocked => new Color(0.45f, 0.85f, 1f, 1f),
            CwslDamagePopupKind.Poison => new Color(0.35f, 1f, 0.35f, 1f),
            CwslDamagePopupKind.Heal => new Color(0.35f, 1f, 0.55f, 1f),
            _ => new Color(1f, 0.92f, 0.28f, 1f)
        };
    }
}
