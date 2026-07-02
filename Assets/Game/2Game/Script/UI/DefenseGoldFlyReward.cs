using AssetKits.ParticleImage;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 처치 골드 — ParticleImage 코인이 HUD 골드 아이콘으로 흡수됩니다.
/// </summary>
public class DefenseGoldFlyReward : MonoBehaviour
{
    private const float FlyCoinStartSize = 20f;
    private const float FlyCoinEmitterSize = 36f;

    private static DefenseGoldFlyReward instance;

    private DefenseGoldUI goldUi;
    private RectTransform canvasRect;
    private GameObject flyParticleTemplate;

    public static void Bind(DefenseGoldUI ui)
    {
        if (ui == null)
            return;

        instance = ui.GetComponent<DefenseGoldFlyReward>();
        if (instance == null)
            instance = ui.gameObject.AddComponent<DefenseGoldFlyReward>();

        instance.goldUi = ui;
        ui.EnsureUiElements();
        instance.canvasRect = ResolveCanvasRect(ui);
        instance.flyParticleTemplate = DefenseGoldCoinVisual.FlyParticleTemplate;
    }

    public static void Play(Vector3 worldPosition, long amount)
    {
        if (amount <= 0)
            return;

        if (instance == null || instance.goldUi == null)
        {
            var ui = Object.FindFirstObjectByType<DefenseGoldUI>();
            if (ui != null)
                Bind(ui);
        }

        if (instance == null || instance.goldUi == null)
        {
            GameManager.Instance?.AddMoney(amount);
            return;
        }

        instance.StartFly(worldPosition, amount);
    }

    private void StartFly(Vector3 worldPosition, long amount)
    {
        goldUi.EnsureUiElements();

        if (canvasRect == null)
            canvasRect = ResolveCanvasRect(goldUi);

        if (flyParticleTemplate == null)
            flyParticleTemplate = DefenseGoldCoinVisual.FlyParticleTemplate;

        if (!TryWorldToCanvasLocal(worldPosition, out Vector2 startLocal))
        {
            CompleteCollect(amount);
            return;
        }

        if (goldUi.FlyTargetIcon == null || flyParticleTemplate == null)
        {
            PlaySimpleFlyFallback(startLocal, amount);
            return;
        }

        var particleObject = Instantiate(flyParticleTemplate, canvasRect);
        particleObject.name = "GoldCoinFlyParticle";

        var rect = particleObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.SetAsLastSibling();
            rect.anchoredPosition = startLocal;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(FlyCoinEmitterSize, FlyCoinEmitterSize);
        }

        var particle = particleObject.GetComponent<ParticleImage>();
        if (particle == null)
        {
            Destroy(particleObject);
            PlaySimpleFlyFallback(startLocal, amount);
            return;
        }

        ConfigureParticle(particle, amount);
        FarmGoldAudio.PlayCoin(worldPosition);

        bool completed = false;
        void CompleteOnce()
        {
            if (completed)
                return;

            completed = true;
            CompleteCollect(amount);
            if (particleObject != null)
                Destroy(particleObject);
        }

        particle.onLastParticleFinished.RemoveAllListeners();
        particle.onParticleStop.RemoveAllListeners();
        particle.onLastParticleFinished.AddListener(CompleteOnce);
        particle.onParticleStop.AddListener(CompleteOnce);
        particle.Play();
    }

    private void PlaySimpleFlyFallback(Vector2 startLocal, long amount)
    {
        if (goldUi == null || canvasRect == null || goldUi.FlyTargetIcon == null)
        {
            CompleteCollect(amount);
            return;
        }

        var coinObject = new GameObject("GoldCoinFlyFallback", typeof(RectTransform), typeof(Image));
        coinObject.transform.SetParent(canvasRect, false);

        var rect = coinObject.GetComponent<RectTransform>();
        rect.SetAsLastSibling();
        rect.sizeDelta = new Vector2(18f, 18f);
        rect.anchoredPosition = startLocal;

        var image = coinObject.GetComponent<Image>();
        DefenseGoldCoinVisual.ApplyHudIcon(image);
        if (image.sprite == null)
        {
            image.sprite = DefenseUISprites.White;
            image.color = new Color(1f, 0.84f, 0.2f);
        }
        image.raycastTarget = false;

        StartCoroutine(AnimateSimpleFly(rect, goldUi.FlyTargetIcon, coinObject, amount));
    }

    private System.Collections.IEnumerator AnimateSimpleFly(
        RectTransform coinRect,
        RectTransform targetIcon,
        GameObject coinObject,
        long amount)
    {
        Vector2 start = coinRect.anchoredPosition;
        Vector2 end = canvasRect.InverseTransformPoint(targetIcon.position);
        float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            coinRect.anchoredPosition = Vector2.Lerp(start, end, eased);
            float scale = Mathf.Lerp(1f, 0.55f, eased);
            coinRect.localScale = Vector3.one * scale;
            yield return null;
        }

        if (coinObject != null)
            Destroy(coinObject);

        CompleteCollect(amount);
    }

    private void ConfigureParticle(ParticleImage particle, long amount)
    {
        int coinCount = Mathf.Clamp((int)amount, 1, 6);

        particle.raycastTarget = false;
        particle.PlayMode = AssetKits.ParticleImage.Enumerations.PlayMode.None;
        particle.loop = false;
        particle.duration = 0.05f;
        particle.rateOverTime = 0f;
        particle.rateOverLifetime = 0f;
        particle.rateOverDistance = 0f;
        particle.circleRadius = 6f;
        particle.rectWidth = FlyCoinEmitterSize;
        particle.rectHeight = FlyCoinEmitterSize;
        particle.startSize = new SeparatedMinMaxCurve(FlyCoinStartSize);
        particle.attractorEnabled = true;
        particle.attractorTarget = goldUi.FlyTargetIcon;
        particle.AddBurst(0f, coinCount);
    }

    private void CompleteCollect(long amount)
    {
        goldUi?.ApplyCollectedGold(amount);
    }

    private static RectTransform ResolveCanvasRect(DefenseGoldUI ui)
    {
        var canvas = ui.GetComponent<Canvas>() ?? ui.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.GetComponent<RectTransform>() : null;
    }

    private bool TryWorldToCanvasLocal(Vector3 worldPosition, out Vector2 localPoint)
    {
        localPoint = default;
        if (canvasRect == null)
            return false;

        var canvas = canvasRect.GetComponent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam ?? Camera.main, worldPosition);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            cam,
            out localPoint);
    }
}
