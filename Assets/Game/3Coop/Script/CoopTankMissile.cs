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

        var missileObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        missileObject.name = "CoopTankMissile";
        missileObject.transform.position = origin;
        missileObject.transform.rotation = rotation;
        missileObject.transform.localScale = new Vector3(0.14f, 0.28f, 0.14f);

        var collider = missileObject.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        var renderer = missileObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(1f, 0.82f, 0.2f);

        var missile = missileObject.AddComponent<CoopTankMissile>();
        missile.Initialize(targetEnemy.transform, attackDamage, pen, playerId, gameSession);
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
            SpawnImpactFx(aimPoint);
            Destroy(gameObject);
        }
    }

    private static void SpawnImpactFx(Vector3 position)
    {
        var fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fx.name = "CoopMissileImpact";
        fx.transform.position = position;
        fx.transform.localScale = Vector3.one * 0.45f;
        Object.Destroy(fx.GetComponent<Collider>());

        var renderer = fx.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(1f, 0.55f, 0.1f, 0.85f);

        Object.Destroy(fx, 0.25f);
    }
}
