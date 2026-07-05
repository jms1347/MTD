using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 위치 주변에 UI 코인 여러 개를 흩뿌리고, 각 코인이 랜덤 타이밍에 플레이어로 수렴.
/// </summary>
public class CwslGoldFlyBurst : MonoBehaviour
{
    private const float SpreadRadius = 39f;
    private const float SpreadDuration = 0.48f;
    private const float CoinScaleMin = 0.52f;
    private const float CoinScaleMax = 0.68f;

    private readonly List<CwslGoldFlyCoin> coins = new();
    private RectTransform canvasRect;
    private Transform playerTarget;
    private Vector3 targetWorldOffset;
    private Action onAllFinished;
    private bool finishing;
    private bool isSession;
    private bool dropSpreadSession;
    private ulong sessionId;

    public void BeginOneShot(
        RectTransform canvas,
        Vector2 origin,
        Transform target,
        Vector3 worldOffset,
        int amount,
        Action finished)
    {
        canvasRect = canvas;
        playerTarget = target;
        targetWorldOffset = worldOffset;
        onAllFinished = finished;
        SpawnCoins(origin, amount, randomDelayMax: 0.38f);
    }

    public void BeginDropSpreadSession(
        ulong sid,
        RectTransform canvas,
        Vector3 worldStart,
        Vector3 worldEnd)
    {
        isSession = true;
        dropSpreadSession = true;
        sessionId = sid;
        canvasRect = canvas;

        var template = CwslGoldCoinVisual.FlyCoinTemplate;
        if (template == null || canvasRect == null)
        {
            CwslGoldFlyToPlayer.MarkVisualSessionComplete(sessionId);
            return;
        }

        var coinObject = Instantiate(template, canvasRect);
        coinObject.name = "CwslGoldFlyCoin";
        coinObject.SetActive(true);
        PrepareCoinRect(coinObject.GetComponent<RectTransform>());

        var coin = coinObject.GetComponent<CwslGoldFlyCoin>();
        if (coin == null)
        {
            Destroy(coinObject);
            CwslGoldFlyToPlayer.MarkVisualSessionComplete(sessionId);
            return;
        }

        coin.ConfigureDropSpread(
            canvasRect,
            worldStart,
            worldEnd,
            UnityEngine.Random.Range(CoinScaleMin, CoinScaleMax),
            HandleCoinFinished);
        coins.Add(coin);
    }

    public void BeginSession(
        ulong sid,
        RectTransform canvas,
        Vector2 origin,
        Transform target,
        Vector3 worldOffset,
        int amount)
    {
        isSession = true;
        sessionId = sid;
        canvasRect = canvas;
        playerTarget = target;
        targetWorldOffset = worldOffset;
        SpawnCoins(origin, amount, randomDelayMax: 0.65f);
    }

    public void SetTarget(Transform target)
    {
        playerTarget = target;
        for (var i = 0; i < coins.Count; i++)
            coins[i].SetTarget(target);
    }

    public void CompleteCollect(Transform target, Action finished)
    {
        finishing = true;
        dropSpreadSession = false;
        onAllFinished = finished;
        playerTarget = target;
        targetWorldOffset = new Vector3(0f, 1.1f, 0f);

        for (var i = 0; i < coins.Count; i++)
        {
            if (target != null)
                coins[i].SetTarget(target);
            coins[i].ForceMagnet();
        }

        if (coins.Count == 0)
            finished?.Invoke();
    }

    private void SpawnCoins(Vector2 origin, int amount, float randomDelayMax)
    {
        var template = CwslGoldCoinVisual.FlyCoinTemplate;
        if (template == null || canvasRect == null || playerTarget == null)
        {
            Debug.LogWarning("[CwSL] 골드 코인 연출 템플릿/캔버스/타겟이 없어 연출을 건너뜁니다.");
            onAllFinished?.Invoke();
            return;
        }

        var count = Mathf.Clamp(amount, 1, 8);
        for (var i = 0; i < count; i++)
        {
            var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            var radius = UnityEngine.Random.Range(0.45f, 1f);
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var spreadOffset = direction * (SpreadRadius * radius);
            var spreadVelocity = direction * (SpreadRadius * radius / SpreadDuration) * UnityEngine.Random.Range(0.85f, 1.2f);
            var spawnLocal = origin + spreadOffset * 0.08f;

            var coinObject = Instantiate(template, canvasRect);
            coinObject.name = "CwslGoldFlyCoin";
            coinObject.SetActive(true);
            PrepareCoinRect(coinObject.GetComponent<RectTransform>());

            var coin = coinObject.GetComponent<CwslGoldFlyCoin>();
            if (coin == null)
            {
                Destroy(coinObject);
                continue;
            }

            coin.Configure(
                canvasRect,
                spawnLocal,
                spreadVelocity,
                playerTarget,
                targetWorldOffset,
                UnityEngine.Random.Range(0.08f, randomDelayMax),
                UnityEngine.Random.Range(760f, 1180f),
                UnityEngine.Random.Range(CoinScaleMin, CoinScaleMax),
                HandleCoinFinished);

            coins.Add(coin);
        }

        if (coins.Count == 0)
            onAllFinished?.Invoke();
    }

    private static void PrepareCoinRect(RectTransform coinRect)
    {
        if (coinRect == null)
            return;

        coinRect.anchorMin = new Vector2(0.5f, 0.5f);
        coinRect.anchorMax = new Vector2(0.5f, 0.5f);
        coinRect.pivot = new Vector2(0.5f, 0.5f);
        coinRect.localScale = Vector3.one;
        coinRect.SetAsLastSibling();
    }

    private void HandleCoinFinished(CwslGoldFlyCoin coin)
    {
        coins.Remove(coin);
        if (coin != null)
            Destroy(coin.gameObject);

        if (coins.Count > 0)
            return;

        var callback = onAllFinished;
        onAllFinished = null;

        if (isSession && !finishing && !dropSpreadSession)
            CwslGoldFlyToPlayer.MarkVisualSessionComplete(sessionId);

        callback?.Invoke();

        if (!isSession && this != null)
            Destroy(gameObject);
    }
}
