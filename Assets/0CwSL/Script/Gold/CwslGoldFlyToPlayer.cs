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



    public static void BeginMagnetFly(ulong sessionId, Vector3 worldPosition, Transform playerTarget, int amount)

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



        GameObject particleObject = null;

        if (flyParticleTemplate == null)

            flyParticleTemplate = CwslGoldCoinVisual.FlyParticleTemplate;



        if (flyParticleTemplate != null)

        {

            particleObject = Object.Instantiate(flyParticleTemplate, canvasRect);

            particleObject.name = "CwslGoldMagnetFlyParticle";



            var rect = particleObject.GetComponent<RectTransform>();

            if (rect != null)

            {

                rect.SetAsLastSibling();

                rect.anchoredPosition = startLocal;

                rect.localScale = Vector3.one;

                rect.sizeDelta = new Vector2(FlyCoinEmitterSize, FlyCoinEmitterSize);

            }



            var particle = particleObject.GetComponent<ParticleImage>();

            if (particle != null)

            {

                ConfigureMagnetParticle(particle, attractor.Rect, amount);

                particle.Play();

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



        var particleObject = Object.Instantiate(flyParticleTemplate, canvasRect);

        particleObject.name = "CwslGoldCoinFlyParticle";



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

            Object.Destroy(particleObject);

            Object.Destroy(attractor.gameObject);

            PlaySimpleFlyFallback(startLocal, playerTarget, amount);

            if (playSound)

                CwslGoldFeedback.PlayCoinSound(worldPosition);

            return;

        }



        ConfigureCollectParticle(particle, attractor.Rect, amount);

        if (playSound)

            CwslGoldFeedback.PlayCoinSound(worldPosition);



        var completed = false;

        void CompleteOnce()

        {

            if (completed)

                return;



            completed = true;

            if (particleObject != null)

                Object.Destroy(particleObject);

            if (attractor != null)

                Object.Destroy(attractor.gameObject);

        }



        particle.onLastParticleFinished.RemoveAllListeners();

        particle.onParticleStop.RemoveAllListeners();

        particle.onLastParticleFinished.AddListener(CompleteOnce);

        particle.onParticleStop.AddListener(CompleteOnce);

        particle.Play();

    }



    private static CwslGoldFlyAttractor CreateAttractor(Transform playerTarget)

    {

        var attractorObject = new GameObject("CwslGoldFlyAttractor", typeof(RectTransform), typeof(CwslGoldFlyAttractor));

        attractorObject.transform.SetParent(canvasRect, false);



        var attractor = attractorObject.GetComponent<CwslGoldFlyAttractor>();

        attractor.Initialize(canvasRect, playerTarget, PlayerTargetOffset);

        return attractor;

    }



    private static void ConfigureCollectParticle(ParticleImage particle, RectTransform attractor, int amount)

    {

        var coinCount = Mathf.Clamp(amount, 1, 6);



        particle.raycastTarget = false;

        particle.PlayMode = ParticlePlayMode.None;

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

        particle.attractorTarget = attractor;

        particle.AddBurst(0f, coinCount);

    }



    private static void ConfigureMagnetParticle(ParticleImage particle, RectTransform attractor, int amount)

    {

        var coinCount = Mathf.Clamp(amount, 1, 4);



        particle.raycastTarget = false;

        particle.PlayMode = ParticlePlayMode.None;

        particle.loop = true;

        particle.duration = 0.05f;

        particle.rateOverTime = 0f;

        particle.rateOverLifetime = 0f;

        particle.rateOverDistance = 0f;

        particle.circleRadius = 6f;

        particle.rectWidth = FlyCoinEmitterSize;

        particle.rectHeight = FlyCoinEmitterSize;

        particle.startSize = new SeparatedMinMaxCurve(FlyCoinStartSize * 0.85f);

        particle.attractorEnabled = true;

        particle.attractorTarget = attractor;

        particle.AddBurst(0f, coinCount);

        particle.AddBurst(0.18f, 1);

        particle.AddBurst(0.36f, 1);

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



        var existing = GameObject.Find("CwslGoldFlyCanvas");

        if (existing != null)

        {

            canvasRect = existing.GetComponent<RectTransform>();

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



        canvasRect = canvasObject.GetComponent<RectTransform>();

    }



    private static bool TryWorldToCanvasLocal(Vector3 worldPosition, out Vector2 localPoint)

    {

        localPoint = default;

        if (canvasRect == null)

            return false;



        var canvas = canvasRect.GetComponent<Canvas>();

        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay

            ? canvas.worldCamera ?? Camera.main

            : Camera.main;



        var screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(

            canvasRect,

            screenPoint,

            cam,

            out localPoint);

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


