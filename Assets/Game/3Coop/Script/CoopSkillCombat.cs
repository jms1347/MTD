using System.Collections.Generic;
using UnityEngine;

public static class CoopSkillCombat
{
    public static void DamageEnemiesInRadius(
        CoopGameSession session,
        Vector3 center,
        float radius,
        float damage,
        int penetration,
        string attackerPlayerId)
    {
        var radiusSqr = radius * radius;
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy))
                continue;

            var flat = enemy.transform.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSqr)
                continue;

            ApplyDamage(session, enemy, damage, penetration, attackerPlayerId);
        }
    }

    public static void ApplyDamage(
        CoopGameSession session,
        GameObject enemy,
        float damage,
        int penetration,
        string attackerPlayerId)
    {
        var synced = enemy.GetComponent<CoopSyncedMonster>();
        if (synced != null)
        {
            synced.TakeDamage(damage, penetration, attackerPlayerId);
            return;
        }

        var actor = enemy.GetComponent<CoopEnemyActor>();
        if (actor != null)
        {
            actor.TakeDamage(damage, penetration, attackerPlayerId);
            return;
        }

        var health = enemy.GetComponent<Health>();
        if (health != null && health.IsAlive)
        {
            health.SetFlatDefenseReduction(penetration);
            health.TakeDamage(Mathf.Max(1f, damage));
        }
    }

    public static bool TryFindNearestEnemy(Vector3 origin, float range, out Transform enemy)
    {
        enemy = null;
        var best = range * range;

        foreach (var candidate in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(candidate))
                continue;

            var flat = candidate.transform.position - origin;
            flat.y = 0f;
            var sqr = flat.sqrMagnitude;
            if (sqr > best)
                continue;

            best = sqr;
            enemy = candidate.transform;
        }

        return enemy != null;
    }

    public static List<Transform> CollectEnemyTargets(Vector3 origin, float range, int maxCount)
    {
        var result = new List<Transform>(maxCount);
        var rangeSqr = range * range;

        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy))
                continue;

            var flat = enemy.transform.position - origin;
            flat.y = 0f;
            if (flat.sqrMagnitude > rangeSqr)
                continue;

            result.Add(enemy.transform);
            if (result.Count >= maxCount)
                break;
        }

        return result;
    }

    public static void SpawnFallingMeteor(
        Vector3 groundPoint,
        float damage,
        int penetration,
        string attackerPlayerId,
        CoopGameSession session)
    {
        var meteorObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        meteorObject.name = "CoopMeteorDrop";
        meteorObject.transform.localScale = Vector3.one * 0.8f;
        Object.Destroy(meteorObject.GetComponent<Collider>());

        var renderer = meteorObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(1f, 0.35f, 0.1f);

        meteorObject.transform.position = groundPoint + Vector3.up * 16f;
        meteorObject.AddComponent<CoopFallingMeteor>().Initialize(
            groundPoint,
            damage,
            penetration,
            attackerPlayerId,
            session);
    }
}

public class CoopFallingMeteor : MonoBehaviour
{
    private Vector3 groundPoint;
    private float damage;
    private int penetration;
    private string attackerPlayerId;
    private CoopGameSession session;

    public void Initialize(
        Vector3 targetGround,
        float strikeDamage,
        int pen,
        string playerId,
        CoopGameSession gameSession)
    {
        groundPoint = targetGround;
        damage = strikeDamage;
        penetration = pen;
        attackerPlayerId = playerId;
        session = gameSession;
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, groundPoint, 28f * Time.deltaTime);
        if ((transform.position - groundPoint).sqrMagnitude > 0.2f)
            return;

        CoopSkillCombat.DamageEnemiesInRadius(session, groundPoint, 1.2f, damage, penetration, attackerPlayerId);
        Destroy(gameObject);
    }
}
