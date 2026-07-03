using UnityEngine;

public class CwslBlockCoinPop : MonoBehaviour
{
    private enum Mode
    {
        FlyToPlayer,
        SpendFall
    }

    private const float FlyLifetime = 0.55f;
    private const float SpendLifetime = 0.7f;
    private const float ArcHeight = 1.4f;
    private const float SpendGravity = 18f;

    private Transform coinTransform;
    private Transform target;
    private Vector3 startPosition;
    private Vector3 controlOffset;
    private Vector3 velocity;
    private float elapsed;
    private float lifetime;
    private Mode mode;

    /// <summary>골드 획득 — 코인이 플레이어에게 모입니다.</summary>
    public static void Spawn(Vector3 spawnPosition, Transform preferredTarget = null)
    {
        var targetTransform = ResolveTarget(spawnPosition, preferredTarget);
        var pop = CreateCoin(spawnPosition);
        pop.mode = Mode.FlyToPlayer;
        pop.lifetime = FlyLifetime;
        pop.target = targetTransform;
        pop.startPosition = spawnPosition;
        pop.controlOffset = new Vector3(
            Random.Range(-0.6f, 0.6f),
            ArcHeight,
            Random.Range(-0.6f, 0.6f));
    }

    /// <summary>스킬 골드 소모 — 코인이 캐릭터에서 떨어져 나갑니다.</summary>
    public static void SpawnSpend(Vector3 playerPosition, int coinCount = 1)
    {
        coinCount = Mathf.Clamp(coinCount, 1, 4);
        for (var i = 0; i < coinCount; i++)
        {
            var spawnPosition = playerPosition + Vector3.up * 1.2f;
            var pop = CreateCoin(spawnPosition);
            pop.mode = Mode.SpendFall;
            pop.lifetime = SpendLifetime;
            pop.startPosition = spawnPosition;
            pop.target = null;
            pop.velocity = new Vector3(
                Random.Range(-3.2f, 3.2f),
                Random.Range(4.5f, 7f),
                Random.Range(-3.2f, 3.2f));
        }
    }

    private static CwslBlockCoinPop CreateCoin(Vector3 spawnPosition)
    {
        var root = new GameObject("CwslGoldCoinPop");
        root.transform.position = spawnPosition;

        var coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coin.name = "Coin";
        var collider = coin.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
        coin.transform.SetParent(root.transform, false);
        coin.transform.localScale = new Vector3(0.34f, 0.08f, 0.34f);

        var renderer = coin.GetComponent<Renderer>();
        CwslMaterialUtil.ApplyColor(renderer, new Color(1f, 0.85f, 0.25f));

        var pop = root.AddComponent<CwslBlockCoinPop>();
        pop.coinTransform = coin.transform;
        return pop;
    }

    private static Transform ResolveTarget(Vector3 fromPosition, Transform preferred)
    {
        if (CwslTargetQuery.TryGetNearestLivingPlayer(fromPosition, out var nearest, out _) && nearest != null)
            return nearest.transform;

        return preferred;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        var t = Mathf.Clamp01(elapsed / lifetime);

        if (mode == Mode.FlyToPlayer)
            TickFlyToPlayer(t);
        else
            TickSpendFall(t);

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }

    private void TickFlyToPlayer(float t)
    {
        var end = target != null
            ? target.position + Vector3.up * 1.2f
            : startPosition + Vector3.up * 0.5f;

        var mid = (startPosition + end) * 0.5f + controlOffset;
        var a = Vector3.Lerp(startPosition, mid, t);
        var b = Vector3.Lerp(mid, end, t);
        transform.position = Vector3.Lerp(a, b, t);
        ApplyCoinSpin(t);
    }

    private void TickSpendFall(float t)
    {
        velocity.y -= SpendGravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        ApplyCoinSpin(t);
    }

    private void ApplyCoinSpin(float t)
    {
        if (coinTransform == null)
            return;

        coinTransform.Rotate(540f * Time.deltaTime, 900f * Time.deltaTime, 0f, Space.Self);
        var scale = Mathf.Lerp(1f, 0.15f, t);
        coinTransform.localScale = new Vector3(0.34f * scale, 0.08f * scale, 0.34f * scale);
    }
}
