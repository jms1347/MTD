using UnityEngine;

public class CoopPoisonBee : MonoBehaviour
{
    private const float ChaseSpeed = 11f;
    private const float FuseSeconds = 3f;
    private const float ExplosionRadius = 1.4f;

    private CoopGameSession session;
    private string attackerPlayerId;
    private Transform target;
    private float damage;
    private int penetration;
    private float fuseEndTime;

    public static void SpawnSwarm(
        CoopGameSession gameSession,
        string playerId,
        Vector3 origin,
        float damage,
        int pen,
        int count)
    {
        var targets = CoopSkillCombat.CollectEnemyTargets(origin, 18f, count);
        for (var i = 0; i < count; i++)
        {
            var beeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beeObject.name = $"CoopPoisonBee_{i}";
            beeObject.transform.localScale = Vector3.one * 0.35f;
            Object.Destroy(beeObject.GetComponent<Collider>());

            var renderer = beeObject.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.55f, 0.9f, 0.2f);

            var offset = Random.insideUnitCircle * 0.6f;
            beeObject.transform.position = origin + new Vector3(offset.x, 1.1f, offset.y);

            var bee = beeObject.AddComponent<CoopPoisonBee>();
            var target = targets.Count > 0 ? targets[i % targets.Count] : null;
            bee.Initialize(gameSession, playerId, target, damage, pen);
        }
    }

    private void Initialize(
        CoopGameSession gameSession,
        string playerId,
        Transform enemyTarget,
        float attackDamage,
        int pen)
    {
        session = gameSession;
        attackerPlayerId = playerId;
        target = enemyTarget;
        damage = attackDamage;
        penetration = pen;
        fuseEndTime = Time.time + FuseSeconds;
    }

    private void Update()
    {
        if (session == null || !session.IsHostAuthority)
            return;

        if (target == null || !target.gameObject.activeInHierarchy || !DefenseEnemyQuery.IsLivingEnemy(target.gameObject))
        {
            if (!CoopSkillCombat.TryFindNearestEnemy(transform.position, 18f, out target))
            {
                if (Time.time >= fuseEndTime)
                    Explode();
                return;
            }
        }

        var aim = target.position + Vector3.up * 0.8f;
        var flat = aim - transform.position;
        if (flat.sqrMagnitude <= 0.35f)
        {
            Explode();
            return;
        }

        transform.position += flat.normalized * (ChaseSpeed * Time.deltaTime);

        if (Time.time >= fuseEndTime)
            Explode();
    }

    private void Explode()
    {
        CoopSkillCombat.DamageEnemiesInRadius(
            session,
            transform.position,
            ExplosionRadius,
            damage,
            penetration,
            attackerPlayerId);

        Destroy(gameObject);
    }
}
