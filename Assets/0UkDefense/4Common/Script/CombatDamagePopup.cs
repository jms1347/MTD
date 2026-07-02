using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몬스터 머리 위 플로팅 데미지 숫자. 풀에서 재사용됩니다.
/// </summary>
[DefaultExecutionOrder(2000)]
public class CombatDamagePopup : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 1.35f;
    [SerializeField] private float lifetime = 0.85f;
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float popScale = 1.18f;
    [SerializeField] private float worldScale = 0.019f;

    private Text damageText;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Vector3 anchor;
    private float elapsed;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        EnsureBound();
    }

    public void Bind(Text text, CanvasGroup group)
    {
        damageText = text;
        canvasGroup = group;
        EnsureBound();
    }

    public void Play(float damage, DamageElement element, Vector3 worldAnchor)
    {
        EnsureBound();

        anchor = worldAnchor;
        elapsed = 0f;
        isPlaying = true;
        gameObject.SetActive(true);

        transform.position = anchor;

        var camera = DefenseBillboardCamera.Resolve();
        if (camera != null)
            transform.rotation = camera.transform.rotation;

        transform.localScale = Vector3.one * worldScale;

        if (damageText != null)
        {
            damageText.text = Mathf.CeilToInt(damage).ToString();
            damageText.color = ResolveColor(element);
            damageText.enabled = true;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.worldCamera = DefenseBillboardCamera.Resolve();
        }
    }

    private void LateUpdate()
    {
        if (!isPlaying)
            return;

        elapsed += Time.deltaTime;
        transform.position = anchor + Vector3.up * (floatSpeed * elapsed);

        if (canvas != null)
            canvas.worldCamera = DefenseBillboardCamera.Resolve();

        var camera = DefenseBillboardCamera.Resolve();
        if (camera != null)
            transform.rotation = camera.transform.rotation;

        float popT = Mathf.Clamp01(elapsed / lifetime);
        float scaleMul = popT < 0.15f
            ? Mathf.Lerp(1f, popScale, popT / 0.15f)
            : Mathf.Lerp(popScale, 0.92f, (popT - 0.15f) / 0.85f);
        transform.localScale = Vector3.one * (worldScale * scaleMul);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, popT);

        if (elapsed >= lifetime)
            CombatDamagePopupPool.Release(this);
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

    private void EnsureBound()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (damageText == null)
            damageText = GetComponentInChildren<Text>(true);
    }

    private static Color ResolveColor(DamageElement element)
    {
        return element switch
        {
            DamageElement.Fire => new Color(1f, 0.45f, 0.18f, 1f),
            DamageElement.Lightning => new Color(1f, 0.92f, 0.35f, 1f),
            DamageElement.Blue => new Color(0.45f, 0.85f, 1f, 1f),
            DamageElement.Green => new Color(0.45f, 0.95f, 0.45f, 1f),
            DamageElement.Pink or DamageElement.Meteor => new Color(0.78f, 0.35f, 0.95f, 1f),
            _ => new Color(1f, 0.92f, 0.35f, 1f)
        };
    }
}
