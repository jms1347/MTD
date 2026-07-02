using UnityEngine;

public class CoopTankMissile : MonoBehaviour
{
    private const float Speed = 28f;
    private const float HitDistance = 0.35f;
    private const float MaxLifetime = 4f;

    private Transform target;
    private float damage;
    private int penetration;
    private string attackerPlayerId;
    private CoopGameSession session;
    private float spawnTime;

    public static void Fire(
        Vector3 origin,
        Quaternion rotation,
        GameObject targetEnemy,
        float attackDamage,
        int pen,
        string playerId,
        CoopGameSession gameSession)
    {
        if (targetEnemy == null || gameSession == null)
            return;

        CoopCombatVfxCache.EnsureInitialized();

        var targetId = ResolveEnemyId(targetEnemy);
        var targetPos = targetEnemy.transform.position;

        var missileObject = new GameObject("CoopTankMissile");
        missileObject.transform.position = origin;
        missileObject.transform.rotation = rotation;
        CoopCombatVfxCache.AttachMissileVisual(missileObject.transform);

        var missile = missileObject.AddComponent<CoopTankMissile>();
        missile.Initialize(targetEnemy.transform, attackDamage, pen, playerId, gameSession);

        gameSession.BroadcastFx(new CoopFxEventPayload
        {
            fxKind = CoopGameProtocol.FxMissile,
            x = origin.x,
            y = origin.y,
            z = origin.z,
            tx = targetPos.x,
            ty = targetPos.y + 0.6f,
            tz = targetPos.z,
            targetEnemyId = targetId
        });
    }

    private static int ResolveEnemyId(GameObject targetEnemy)
    {
        var actor = targetEnemy.GetComponent<CoopEnemyActor>();
        if (actor != null)
            return actor.NetworkId;

        var synced = targetEnemy.GetComponent<CoopSyncedMonster>();
        if (synced != null)
            return synced.NetworkId;

        return -1;
    }

    private void Initialize(
        Transform enemyTarget,
        float attackDamage,
        int pen,
        string playerId,
        CoopGameSession gameSession)
    {
        target = enemyTarget;
        damage = attackDamage;
        penetration = pen;
        attackerPlayerId = playerId;
        session = gameSession;
        spawnTime = Time.time;
    }

    private void Update()
    {
        if (session == null)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null || !DefenseEnemyQuery.IsLivingEnemy(target.gameObject))
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time - spawnTime > MaxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        var aimPoint = target.position + Vector3.up * 0.6f;
        var next = Vector3.MoveTowards(transform.position, aimPoint, Speed * Time.deltaTime);
        transform.position = next;

        var flat = aimPoint - transform.position;
        if (flat.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

        if ((aimPoint - next).sqrMagnitude <= HitDistance * HitDistance)
        {
            CoopTankAttack.ApplyDamageToEnemy(session, target.gameObject, damage, penetration, attackerPlayerId);
            CoopCombatVfxCache.PlayImpact(aimPoint, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
