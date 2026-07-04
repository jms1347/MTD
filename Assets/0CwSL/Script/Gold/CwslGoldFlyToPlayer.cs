using System.Collections;
using System.Collections.Generic;

using AssetKits.ParticleImage;

using Unity.Netcode;

using UnityEngine;

using UnityEngine.UI;

using ParticlePlayMode = AssetKits.ParticleImage.Enumerations.PlayMode;



public static class CwslGoldFlyToPlayer

{

    private const float FlyCoinStartSize = 20f;
    private const float FlyCoinEmitterSize = 36f;
    internal const float SpreadDuration = 0.22f;
    private const float SpreadRadius = 16f;
    private const float FlyEmitRadius = 3f;

    private static readonly Vector3 PlayerTargetOffset = new(0f, 1.1f, 0f);



    private static RectTransform canvasRect;

    private static GameObject flyParticleTemplate;

    private static readonly Dictionary<ulong, MagnetFlySession> magnetSessions = new();



    private sealed class MagnetFlySession

    {

        public CwslGoldFlyAttractor Attractor;

        public GameObject ParticleObject;

    }



    public static void Play(Vector3 worldPosition, Transform playerTarget, int amount)

    {

        if (amount <= 0 || playerTarget == null)

            return;



        EnsureCanvas();

        if (canvasRect == null)

            return;



        if (!TryWorldToCanvasLocal(worldPosition + Vector3.up * 0.35f, out var startLocal))

            return;



        SpawnFlyBurst(startLocal, worldPosition, playerTarget, amount, playSound: true);

    }



    public static void BeginMagnetFly(
        ulong sessionId,
        Vector3 worldPosition,
        Transform playerTarget,
        int amount)

    {

        if (playerTarget == null)

            return;



        EndMagnetFly(sessionId);

        EnsureCanvas();

        if (canvasRect == null)

            return;



        if (!TryWorldToCanvasLocal(worldPosition + Vector3.up * 0.35f, out var startLocal))

            return;



        var attractor = CreateAttractor(playerTarget);

        if (attractor == null)

            return;



        attractor.RefreshPosition();

        GameObject particleObject = null;

        if (flyParticleTemplate == null)

            flyParticleTemplate = CwslGoldCoinVisual.FlyParticleTemplate;



        if (flyParticleTemplate != null)

        {

            var particle = TryCreateConfiguredParticle(startLocal, out particleObject);

            if (particle != null)
            {
                var runner = particleObject.AddComponent<CwslGoldMagnetFlyRunner>();
                runner.BeginMagnet(particle, attractor, amount, sessionId);
            }

            else

            {

                Object.Destroy(particleObject);

                particleObject = null;

            }

        }



        magnetSessions[sessionId] = new MagnetFlySession

        {

            Attractor = attractor,

            ParticleObject = particleObject

        };

    }



    public static void SetMagnetFlyTarget(ulong sessionId, Transform playerTarget)

    {

        if (playerTarget == null)

            return;



        if (!magnetSessions.TryGetValue(sessionId, out var session) || session.Attractor == null)

            return;



        session.Attractor.SetTarget(playerTarget);
        session.Attractor.RefreshPosition();

    }



    public static void EndMagnetFly(ulong sessionId)

    {

        if (!magnetSessions.TryGetValue(sessionId, out var session))

            return;



        if (session.ParticleObject != null)

            Object.Destroy(session.ParticleObject);

        if (session.Attractor != null)

            Object.Destroy(session.Attractor.gameObject);



        magnetSessions.Remove(sessionId);

    }



    /// <summary>
    /// 자석 연출 중 수집 시 — 새로 터뜨리지 않고 기존 코인이 플레이어 쪽으로 마무리.
    /// </summary>
    public static bool TryCompleteMagnetFly(ulong sessionId, Vector3 worldPosition, int amount)
    {
        if (!magnetSessions.TryGetValue(sessionId, out var session))
            return false;

        CwslGoldFeedback.PlayCoinSound(worldPosition);

        if (session.ParticleObject == null)
        {
            EndMagnetFly(sessionId);
            return true;
        }

        var particle = session.ParticleObject.GetComponent<ParticleImage>();
        if (particle != null)
        {
            var runner = session.ParticleObject.GetComponent<CwslGoldMagnetFlyRunner>();
            if (runner != null)
                runner.FinishMagnet(particle);
            else
                particle.loop = false;
        }

        return true;
    }



    private static void SpawnFlyBurst(

        Vector2 startLocal,

        Vector3 worldPosition,

        Transform playerTarget,

        int amount,

        bool playSound)

    {

        if (flyParticleTemplate == null)

            flyParticleTemplate = CwslGoldCoinVisual.FlyParticleTemplate;



        if (flyParticleTemplate == null)

        {

            PlaySimpleFlyFallback(startLocal, playerTarget, amount);

            if (playSound)

                CwslGoldFeedback.PlayCoinSound(worldPosition);

            return;

        }



        var attractor = CreateAttractor(playerTarget);

        if (attractor == null)

        {

            PlaySimpleFlyFallback(startLocal, playerTarget, amount);

            if (playSound)

                CwslGoldFeedback.PlayCoinSound(worldPosition);

            return;

        }

        attractor.RefreshPosition();



        var particle = TryCreateConfiguredParticle(startLocal, out var particleObject);

        if (particle == null)

        {

            Object.Destroy(particleObject);

            Object.Destroy(attractor.gameObject);

            PlaySimpleFlyFallback(startLocal, playerTarget, amount);

            if (playSound)

                CwslGoldFeedback.PlayCoinSound(worldPosition);

            return;

        }



        if (playSound)
            CwslGoldFeedback.PlayCoinSound(worldPosition);

        var runner = particleObject.AddComponent<CwslGoldMagnetFlyRunner>();
        runner.BeginOneShot(particle, attractor, amount, particleObject, attractor.gameObject);
    }



    private static CwslGoldFlyAttractor CreateAttractor(Transform playerTarget)

    {

        var attractorObject = new GameObject("CwslGoldFlyAttractor", typeof(RectTransform), typeof(CwslGoldFlyAttractor));

        attractorObject.transform.SetParent(canvasRect, false);

        var attractorRect = attractorObject.GetComponent<RectTransform>();
        attractorRect.anchorMin = new Vector2(0.5f, 0.5f);
        attractorRect.anchorMax = new Vector2(0.5f, 0.5f);
        attractorRect.pivot = new Vector2(0.5f, 0.5f);
        attractorRect.sizeDelta = Vector2.zero;

        var attractor = attractorObject.GetComponent<CwslGoldFlyAttractor>();

        attractor.Initialize(canvasRect, playerTarget, PlayerTargetOffset);

        return attractor;

    }



    private static ParticleImage TryCreateConfiguredParticle(Vector2 startLocal, out GameObject particleObject)

    {

        particleObject = Object.Instantiate(flyParticleTemplate, canvasRect);

        particleObject.name = "CwslGoldCoinFlyParticle";



        var rect = particleObject.GetComponent<RectTransform>();

        if (rect != null)

        {

            rect.SetAsLastSibling();

            rect.anchorMin = new Vector2(0.5f, 0.5f);

            rect.anchorMax = new Vector2(0.5f, 0.5f);

            rect.pivot = new Vector2(0.5f, 0.5f);

            rect.anchoredPosition = startLocal;

            rect.localScale = Vector3.one;

            rect.sizeDelta = new Vector2(FlyCoinEmitterSize, FlyCoinEmitterSize);

        }



        var particle = particleObject.GetComponent<ParticleImage>();

        if (particle == null)
        {
            Object.Destroy(particleObject);
            particleObject = null;
            return null;
        }



        ResetParticleBeforeConfigure(particle);

        return particle;

    }



    private static void ResetParticleBeforeConfigure(ParticleImage particle)

    {

        particle.Stop(true);

        particle.PlayMode = ParticlePlayMode.None;

        particle.attractorEnabled = false;

        particle.attractorTarget = null;

    }



    internal static void ConfigureSpreadParticle(ParticleImage particle, int amount)
    {
        var coinCount = Mathf.Clamp(amount, 1, 6);

        ResetParticleBeforeConfigure(particle);
        particle.raycastTarget = false;
        particle.loop = false;
        particle.duration = 0.08f;
        particle.rateOverTime = 0f;
        particle.rateOverLifetime = 0f;
        particle.rateOverDistance = 0f;
        particle.circleRadius = SpreadRadius;
        particle.rectWidth = FlyCoinEmitterSize;
        particle.rectHeight = FlyCoinEmitterSize;
        particle.startSize = new SeparatedMinMaxCurve(FlyCoinStartSize);
        particle.attractorEnabled = false;
        particle.attractorTarget = null;
        particle.AddBurst(0f, coinCount);
    }



    internal static void EnableFlyPhase(ParticleImage particle, RectTransform attractor, bool loop)
    {
        particle.loop = loop;
        particle.circleRadius = FlyEmitRadius;
        particle.attractorEnabled = true;
        particle.attractorTarget = attractor;
    }



    private static void PlaySimpleFlyFallback(Vector2 startLocal, Transform playerTarget, int amount)

    {

        var attractor = CreateAttractor(playerTarget);

        if (attractor == null)

            return;



        var coinObject = new GameObject("CwslGoldCoinFlyFallback", typeof(RectTransform), typeof(Image));

        coinObject.transform.SetParent(canvasRect, false);



        var rect = coinObject.GetComponent<RectTransform>();

        rect.SetAsLastSibling();

        rect.sizeDelta = new Vector2(18f, 18f);

        rect.anchoredPosition = startLocal;



        var image = coinObject.GetComponent<Image>();

        image.color = new Color(1f, 0.84f, 0.2f);

        image.raycastTarget = false;



        var runner = coinObject.AddComponent<CwslGoldSimpleFlyRunner>();

        runner.Begin(rect, attractor.Rect, coinObject, attractor.gameObject);

    }



    private static void EnsureCanvas()

    {

        if (canvasRect != null)

            return;



        var hudCanvas = GameObject.Find("CwslGameHudCanvas");

        if (hudCanvas != null)

        {

            canvasRect = hudCanvas.GetComponent<RectTransform>();

            if (canvasRect != null)

                return;

        }



        var existing = GameObject.Find("CwslGoldFlyCanvas");

        if (existing != null)

        {

            canvasRect = existing.GetComponent<RectTransform>();

            ConfigureOverlayCanvasRect(canvasRect);

            return;

        }



        var canvasObject = new GameObject(

            "CwslGoldFlyCanvas",

            typeof(RectTransform),

            typeof(Canvas),

            typeof(CanvasScaler),

            typeof(GraphicRaycaster));



        var canvas = canvasObject.GetComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = 120;



        var scaler = canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920f, 1080f);

        scaler.matchWidthOrHeight = 0.5f;



        canvasRect = canvasObject.GetComponent<RectTransform>();

        ConfigureOverlayCanvasRect(canvasRect);

    }



    private static void ConfigureOverlayCanvasRect(RectTransform rect)

    {

        if (rect == null)

            return;



        rect.anchorMin = Vector2.zero;

        rect.anchorMax = Vector2.one;

        rect.offsetMin = Vector2.zero;

        rect.offsetMax = Vector2.zero;

        rect.pivot = new Vector2(0.5f, 0.5f);

    }



    internal static bool TryWorldToCanvasLocal(Vector3 worldPosition, out Vector2 localPoint)

    {

        localPoint = default;

        if (canvasRect == null)

            return false;



        var canvas = canvasRect.GetComponent<Canvas>();
        var worldCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera ?? Camera.main
            : Camera.main;

        if (worldCam == null)
            return false;

        var viewport = worldCam.WorldToViewportPoint(worldPosition);

        if (viewport.z <= 0f)
            return false;

        var screenPoint = RectTransformUtility.WorldToScreenPoint(worldCam, worldPosition);
        var eventCam = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : worldCam;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            eventCam,
            out localPoint);

    }

}



internal class CwslGoldMagnetFlyRunner : MonoBehaviour
{
    private ParticleImage particle;
    private CwslGoldFlyAttractor attractor;
    private ulong sessionId;
    private bool isMagnetSession;
    private GameObject particleObject;
    private GameObject attractorObject;
    private Coroutine routine;
    private bool flyPhaseActive;
    private float flyBurstTimer;
    private const float FlyTrailBurstInterval = 0.28f;

    public void BeginMagnet(ParticleImage p, CwslGoldFlyAttractor att, int amount, ulong sid)
    {
        particle = p;
        attractor = att;
        sessionId = sid;
        isMagnetSession = true;
        particleObject = p.gameObject;
        routine = StartCoroutine(RunMagnetPhases(amount));
    }

    public void BeginOneShot(
        ParticleImage p,
        CwslGoldFlyAttractor att,
        int amount,
        GameObject particleGo,
        GameObject attractorGo)
    {
        particle = p;
        attractor = att;
        isMagnetSession = false;
        particleObject = particleGo;
        attractorObject = attractorGo;
        routine = StartCoroutine(RunOneShotPhases(amount));
    }

    public void FinishMagnet(ParticleImage p)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        flyPhaseActive = false;

        if (p == null)
        {
            if (isMagnetSession)
                CwslGoldFlyToPlayer.EndMagnetFly(sessionId);
            else
                CleanupOneShot();
            return;
        }

        if (attractor != null)
        {
            attractor.RefreshPosition();
            p.attractorEnabled = true;
            p.attractorTarget = attractor.Rect;
        }

        p.loop = false;
        RegisterCompletion(() =>
        {
            if (isMagnetSession)
                CwslGoldFlyToPlayer.EndMagnetFly(sessionId);
            else
                CleanupOneShot();
        });
    }

    private IEnumerator RunMagnetPhases(int amount)
    {
        attractor?.RefreshPosition();
        CwslGoldFlyToPlayer.ConfigureSpreadParticle(particle, amount);
        particle.Play();

        yield return new WaitForSeconds(CwslGoldFlyToPlayer.SpreadDuration);

        if (particle == null)
            yield break;

        attractor?.RefreshPosition();
        CwslGoldFlyToPlayer.EnableFlyPhase(particle, attractor.Rect, loop: true);
        flyPhaseActive = true;
        flyBurstTimer = FlyTrailBurstInterval;
        routine = null;
    }

    private IEnumerator RunOneShotPhases(int amount)
    {
        attractor?.RefreshPosition();
        CwslGoldFlyToPlayer.ConfigureSpreadParticle(particle, amount);
        particle.Play();

        yield return new WaitForSeconds(CwslGoldFlyToPlayer.SpreadDuration);

        if (particle == null)
            yield break;

        attractor?.RefreshPosition();
        CwslGoldFlyToPlayer.EnableFlyPhase(particle, attractor.Rect, loop: false);

        routine = null;
        RegisterCompletion(CleanupOneShot);
    }

    private void Update()
    {
        if (!flyPhaseActive || !isMagnetSession || particle == null)
            return;

        flyBurstTimer += Time.deltaTime;
        if (flyBurstTimer < FlyTrailBurstInterval)
            return;

        flyBurstTimer = 0f;
        particle.AddBurst(0f, 1);
    }

    private void RegisterCompletion(System.Action onComplete)
    {
        if (particle == null)
        {
            onComplete?.Invoke();
            return;
        }

        var completed = false;
        void CompleteOnce()
        {
            if (completed)
                return;

            completed = true;
            onComplete?.Invoke();
        }

        particle.onLastParticleFinished.RemoveAllListeners();
        particle.onParticleStop.RemoveAllListeners();
        particle.onLastParticleFinished.AddListener(CompleteOnce);
        particle.onParticleStop.AddListener(CompleteOnce);
    }

    private void CleanupOneShot()
    {
        if (particleObject != null)
            Destroy(particleObject);
        if (attractorObject != null)
            Destroy(attractorObject);
        Destroy(this);
    }

    private void OnDestroy()
    {
        if (routine != null)
            StopCoroutine(routine);
    }
}



internal class CwslGoldSimpleFlyRunner : MonoBehaviour

{

    private RectTransform coinRect;

    private RectTransform targetRect;

    private GameObject coinObject;

    private GameObject attractorObject;

    private float elapsed;

    private Vector2 start;

    private Vector2 end;

    private const float Duration = 0.45f;



    public void Begin(RectTransform coin, RectTransform target, GameObject coinGo, GameObject attractorGo)

    {

        coinRect = coin;

        targetRect = target;

        coinObject = coinGo;

        attractorObject = attractorGo;

        start = coin.anchoredPosition;

        end = target.anchoredPosition;

        elapsed = 0f;

    }



    private void Update()

    {

        if (coinRect == null || targetRect == null)

        {

            Cleanup();

            return;

        }



        end = targetRect.anchoredPosition;

        elapsed += Time.deltaTime;

        var t = Mathf.Clamp01(elapsed / Duration);

        var eased = 1f - Mathf.Pow(1f - t, 3f);

        coinRect.anchoredPosition = Vector2.Lerp(start, end, eased);

        coinRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, eased);



        if (t >= 1f)

            Cleanup();

    }



    private void Cleanup()

    {

        if (coinObject != null)

            Destroy(coinObject);

        if (attractorObject != null)

            Destroy(attractorObject);

        Destroy(this);

    }

}


