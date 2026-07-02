using UnityEngine;

public static class CoopFxReplayer
{
    public static void Play(CoopFxEventPayload fx)
    {
        if (fx == null || string.IsNullOrEmpty(fx.fxKind))
            return;

        CoopCombatVfxCache.EnsureInitialized();

        switch (fx.fxKind)
        {
            case CoopGameProtocol.FxMissile:
                CoopMissileVisual.Spawn(
                    new Vector3(fx.x, fx.y, fx.z),
                    fx.targetEnemyId,
                    new Vector3(fx.tx, fx.ty, fx.tz));
                break;
            case CoopGameProtocol.FxExplosion:
                CoopEnemyExplosionVfx.Spawn(
                    new Vector3(fx.x, fx.y, fx.z),
                    fx.radius,
                    fx.heavy);
                break;
            case CoopGameProtocol.FxImpact:
                CoopCombatVfxCache.PlayImpact(new Vector3(fx.x, fx.y, fx.z), Quaternion.identity);
                break;
        }
    }
}

public class CoopMissileVisual : MonoBehaviour
{
    private const float Speed = 28f;
    private const float HitDistance = 0.35f;
    private const float MaxLifetime = 4f;

    private int targetEnemyId = -1;
    private Vector3 fallbackTarget;
    private float spawnTime;

    public static void Spawn(Vector3 origin, int enemyId, Vector3 targetPosition)
    {
        var missileObject = new GameObject("CoopMissileVisual");
        missileObject.transform.position = origin;
        CoopCombatVfxCache.AttachMissileVisual(missileObject.transform);

        var missile = missileObject.AddComponent<CoopMissileVisual>();
        missile.Initialize(enemyId, targetPosition);
    }

    private void Initialize(int enemyId, Vector3 targetPosition)
    {
        targetEnemyId = enemyId;
        fallbackTarget = targetPosition + Vector3.up * 0.6f;
        spawnTime = Time.time;
    }

    private void Update()
    {
        if (Time.time - spawnTime > MaxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        var aimPoint = fallbackTarget;
        if (targetEnemyId >= 0
            && CoopWorldView.Instance != null
            && CoopWorldView.Instance.TryGetMirroredEnemy(targetEnemyId, out var enemyTransform)
            && enemyTransform != null)
        {
            aimPoint = enemyTransform.position + Vector3.up * 0.6f;
        }

        var next = Vector3.MoveTowards(transform.position, aimPoint, Speed * Time.deltaTime);
        transform.position = next;

        var flat = aimPoint - transform.position;
        if (flat.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

        if ((aimPoint - next).sqrMagnitude <= HitDistance * HitDistance)
        {
            CoopCombatVfxCache.PlayImpact(aimPoint, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
