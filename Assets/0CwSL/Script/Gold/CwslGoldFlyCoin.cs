using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 주변에서 퍼졌다가 랜덤 딜레이 후 플레이어 쪽으로 빨려 들어가는 UI 코인 1개.
/// </summary>
public class CwslGoldFlyCoin : MonoBehaviour
{
    private const float SpreadDuration = 0.48f;
    private const float SpreadDrag = 6f;
    private const float ArriveDistance = 10f;
    private const float MagnetTimeout = 2.5f;
    private static readonly Vector3 WorldVisualOffset = new(0f, 0.35f, 0f);

    private RectTransform rect;
    private CwslGoldFlyCoinTrail trail;
    private RectTransform canvasRect;
    private Transform playerTarget;
    private Vector3 targetWorldOffset;
    private Vector3 spreadWorldStart;
    private Vector3 spreadWorldEnd;
    private Vector2 velocity;
    private Vector2 lastTargetLocal;
    private bool hasTargetLocal;
    private bool useWorldSpreadTarget;
    private bool waitingCollect;
    private float phaseTimer;
    private float magnetDelay;
    private float magnetSpeed;
    private float magnetTimer;
    private bool spreadDone;
    private bool magnetizing;
    private Action<CwslGoldFlyCoin> onFinished;

    public void ConfigureDropSpread(
        RectTransform canvas,
        Vector3 worldStart,
        Vector3 worldEnd,
        float scale,
        Action<CwslGoldFlyCoin> finished)
    {
        rect = GetComponent<RectTransform>();
        trail = GetComponentInChildren<CwslGoldFlyCoinTrail>(true);
        canvasRect = canvas;
        playerTarget = null;
        spreadWorldStart = worldStart;
        spreadWorldEnd = worldEnd;
        useWorldSpreadTarget = true;
        waitingCollect = false;
        onFinished = finished;
        phaseTimer = 0f;
        spreadDone = false;
        magnetizing = false;

        rect.localScale = Vector3.one * scale;
        rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-25f, 25f));
        EnsureSprite();
        trail?.Begin(rect, canvasRect);
        SyncRectToWorld(spreadWorldStart);
    }

    public void Configure(
        RectTransform canvas,
        Vector2 origin,
        Vector2 spreadVelocity,
        Transform target,
        Vector3 worldOffset,
        float delayBeforeMagnet,
        float speed,
        float scale,
        Action<CwslGoldFlyCoin> finished)
    {
        rect = GetComponent<RectTransform>();
        trail = GetComponentInChildren<CwslGoldFlyCoinTrail>(true);
        canvasRect = canvas;
        playerTarget = target;
        targetWorldOffset = worldOffset;
        velocity = spreadVelocity;
        magnetDelay = delayBeforeMagnet;
        magnetSpeed = speed;
        onFinished = finished;
        phaseTimer = 0f;
        magnetTimer = 0f;
        spreadDone = false;
        magnetizing = false;
        waitingCollect = false;
        useWorldSpreadTarget = false;
        hasTargetLocal = false;

        rect.anchoredPosition = origin;
        rect.localScale = Vector3.one * scale;
        rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-30f, 30f));
        EnsureSprite();
        trail?.Begin(rect, canvasRect);
    }

    public void SetTarget(Transform target)
    {
        playerTarget = target;
        hasTargetLocal = false;
    }

    public void ForceMagnet()
    {
        waitingCollect = false;
        spreadDone = true;
        magnetizing = true;
        phaseTimer = magnetDelay;
        magnetTimer = 0f;
    }

    private void Update()
    {
        if (!spreadDone)
        {
            UpdateSpreadPhase();
            return;
        }

        if (waitingCollect)
        {
            SyncRectToWorld(spreadWorldEnd);
            return;
        }

        if (playerTarget == null)
        {
            Finish();
            return;
        }

        if (!magnetizing)
        {
            phaseTimer += Time.deltaTime;
            if (phaseTimer >= magnetDelay)
            {
                magnetizing = true;
                magnetTimer = 0f;
            }

            return;
        }

        UpdateMagnetPhase();
    }

    private void UpdateSpreadPhase()
    {
        phaseTimer += Time.deltaTime;

        if (useWorldSpreadTarget)
        {
            var t = Mathf.Clamp01(phaseTimer / SpreadDuration);
            var eased = 1f - (1f - t) * (1f - t);
            var worldPos = Vector3.Lerp(spreadWorldStart, spreadWorldEnd, eased);
            SyncRectToWorld(worldPos);
            if (t >= 1f)
            {
                spreadDone = true;
                waitingCollect = true;
                phaseTimer = 0f;
            }

            return;
        }

        rect.anchoredPosition += velocity * Time.deltaTime;
        velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime * SpreadDrag);
        if (phaseTimer >= SpreadDuration)
        {
            spreadDone = true;
            phaseTimer = 0f;
        }
    }

    private void UpdateMagnetPhase()
    {
        magnetTimer += Time.deltaTime;
        if (TryGetTargetLocal(out var targetLocal))
        {
            lastTargetLocal = targetLocal;
            hasTargetLocal = true;
        }
        else if (!hasTargetLocal)
        {
            return;
        }

        var goal = hasTargetLocal ? lastTargetLocal : rect.anchoredPosition;
        var toTarget = goal - rect.anchoredPosition;
        if (toTarget.sqrMagnitude <= ArriveDistance * ArriveDistance || magnetTimer >= MagnetTimeout)
        {
            rect.anchoredPosition = goal;
            Finish();
            return;
        }

        var step = toTarget.normalized * (magnetSpeed * Time.deltaTime);
        if (step.sqrMagnitude > toTarget.sqrMagnitude)
            rect.anchoredPosition = goal;
        else
            rect.anchoredPosition += step;

        var scale = rect.localScale.x;
        rect.localScale = Vector3.one * Mathf.MoveTowards(scale, 0.28f, Time.deltaTime * 2.5f);
    }

    private void SyncRectToWorld(Vector3 worldPosition)
    {
        if (CwslGoldFlyToPlayer.TryWorldToCanvasLocal(worldPosition + WorldVisualOffset, out var local))
            rect.anchoredPosition = local;
    }

    private void EnsureSprite()
    {
        var image = GetComponent<Image>();
        if (image != null && image.sprite == null)
            image.sprite = CwslGoldCoinVisual.GetCoinSprite();
    }

    private bool TryGetTargetLocal(out Vector2 local)
    {
        return CwslGoldFlyToPlayer.TryWorldToCanvasLocal(
            playerTarget.position + targetWorldOffset,
            out local);
    }

    private void Finish()
    {
        trail?.Stop();
        onFinished?.Invoke(this);
    }

    private void OnDestroy()
    {
        trail?.Stop();
    }
}
