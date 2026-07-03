using TMPro;
using UnityEngine;

[DefaultExecutionOrder(2000)]
public class CwslDamagePopup : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 1.65f;
    [SerializeField] private float lifetime = 1.05f;
    [SerializeField] private float worldScale = 0.011f;
    [SerializeField] private float startScaleMultiplier = 0.85f;
    [SerializeField] private float popScaleMultiplier = 1.35f;
    [SerializeField] private float endScaleMultiplier = 3.8f;

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
            damageText.text = kind == CwslDamagePopupKind.Blocked
                ? "BLOCK"
                : Mathf.CeilToInt(damage).ToString();
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

        if (damageText != null)
        {
            var color = baseColor;
            color.a = EvaluateAlpha(t);
            damageText.color = color;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = EvaluateAlpha(t);

        if (elapsed >= lifetime)
            CwslDamagePopupPool.Release(this);
    }

    private float EvaluateScaleMultiplier(float t)
    {
        const float popPhase = 0.12f;
        if (t < popPhase)
        {
            var popT = t / popPhase;
            return Mathf.Lerp(startScaleMultiplier, popScaleMultiplier, popT);
        }

        var growT = (t - popPhase) / (1f - popPhase);
        var eased = 1f - Mathf.Pow(1f - growT, 2.35f);
        return Mathf.Lerp(popScaleMultiplier, endScaleMultiplier, eased);
    }

    private static float EvaluateAlpha(float t)
    {
        return 1f - Mathf.SmoothStep(0f, 1f, Mathf.Pow(t, 1.15f));
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
            CwslDamagePopupKind.Player => new Color(1f, 0.35f, 0.35f, 1f),
            CwslDamagePopupKind.Monster => new Color(1f, 0.92f, 0.35f, 1f),
            CwslDamagePopupKind.Projectile => new Color(0.95f, 0.55f, 1f, 1f),
            CwslDamagePopupKind.Blocked => new Color(0.55f, 0.85f, 1f, 1f),
            _ => new Color(1f, 0.92f, 0.35f, 1f)
        };
    }
}
