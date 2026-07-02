using System;
using UnityEngine;

public class CoopEnemyActor : MonoBehaviour
{
    public int NetworkId { get; private set; }
    public bool IsBoss { get; private set; }
    public int GoldReward { get; private set; }
    public int Defense { get; private set; }
    public CoopEnemyArchetype Archetype { get; private set; }

    private Health health;
    private float moveSpeed = 3f;
    private float contactDamage = 6f;
    private float explosionRadius;
    private float explosionDamage;
    private float touchRadius = 1.35f;
    private float meleeInterval = 1.4f;
    private float meleeTimer;
    private string monsterCode;
    private CoopGameSession session;
    private CoopEnemySlimeVisual slimeVisual;
    private bool killedByCombat;
    private bool hasDetonated;

    public float MoveSpeed => moveSpeed;

    public void Initialize(
        CoopGameSession gameSession,
        int networkId,
        Vector3 position,
        CoopSlimeWaveStats stats)
    {
        session = gameSession;
        NetworkId = networkId;
        IsBoss = stats.isBoss;
        GoldReward = stats.goldReward;
        Defense = stats.defense;
        Archetype = stats.archetype;
        moveSpeed = stats.moveSpeed;
        contactDamage = stats.contactDamage;
        explosionRadius = stats.explosionRadius;
        explosionDamage = stats.explosionDamage;
        touchRadius = stats.touchRadius;
        meleeInterval = stats.meleeInterval;
        monsterCode = stats.slimeKey;

        gameObject.name = stats.isBoss
            ? $"Boss_{CoopEnemyArchetypeUtil.ToId(stats.archetype)}_{networkId}"
            : $"Enemy_{CoopEnemyArchetypeUtil.ToId(stats.archetype)}_{networkId}";
        gameObject.tag = "Enemy";
        transform.position = position;

        health = gameObject.GetComponent<Health>();
        if (health == null)
            health = gameObject.AddComponent<Health>();
        health.Initialize(stats.maxHp, 0.2f, stats.defense);
        health.OnDeath += HandleDeath;

        var healthBar = gameObject.GetComponent<HealthBarUI>();
        if (healthBar == null)
            healthBar = gameObject.AddComponent<HealthBarUI>();
        healthBar.ConfigureAsEnemy();
        healthBar.RefreshForSpawn();

        slimeVisual = CoopSlimeVisualFactory.Build(
            transform, stats.slimeKey, stats.archetype, moveSpeed, stats.isBoss, out var bodyCollider);

        if (stats.explosionRadius > 0f)
            explosionRadius = stats.explosionRadius + ResolveBodyRadius(bodyCollider) * 0.75f;
    }

    private static float ResolveBodyRadius(CapsuleCollider collider)
        => collider != null ? collider.radius : 0.55f;

    private float GetSurfaceGapTo(Vector3 targetPos)
    {
        var flat = targetPos - transform.position;
        flat.y = 0f;
        var bodyRadius = ResolveBodyRadius(GetComponent<CapsuleCollider>());
        const float tankBodyRadius = 0.7f;
        return flat.magnitude - bodyRadius - tankBodyRadius;
    }

    private bool IsInMeleeRange(float surfaceGap) => surfaceGap <= touchRadius;

    private void Update()
    {
        if (session == null || !session.IsHostAuthority || health == null || !health.IsAlive || hasDetonated)
            return;

        var target = FindMoveTarget();
        var flat = target - transform.position;
        flat.y = 0f;
        var distance = flat.magnitude;
        var surfaceGap = GetSurfaceGapTo(target);

        if (Archetype.IsSuicide())
        {
            if (IsInMeleeRange(surfaceGap))
            {
                Detonate();
                return;
            }
        }
        else if (Archetype == CoopEnemyArchetype.Tank || Archetype == CoopEnemyArchetype.Grunt)
        {
            if (IsInMeleeRange(surfaceGap))
            {
                TickTankMelee();
                return;
            }
        }
        else if (IsInMeleeRange(surfaceGap))
        {
            StrikeAndExpire();
            return;
        }

        if (distance < 0.05f)
            return;

        transform.position += flat.normalized * (moveSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(flat.normalized),
            Time.deltaTime * 8f);
    }

    public void SyncPosition(Vector3 position)
    {
        if (session != null && session.IsHostAuthority)
            return;

        transform.position = position;
    }

    public void SyncHealth(float hp, float maxHp)
    {
        if (health == null || session == null || session.IsHostAuthority)
            return;

        if (Mathf.Abs(health.MaxHealth - maxHp) > 0.1f)
            health.Initialize(maxHp, 0.2f, Defense);
        health.ApplyAuthoritativeHealth(hp, maxHp);
    }

    public void TakeDamage(float attack, int penetration, string attackerPlayerId)
    {
        if (health == null || !health.IsAlive || hasDetonated)
            return;

        health.SetFlatDefenseReduction(penetration);
        health.TakeDamage(Mathf.Max(1f, attack));
        session?.RegisterEnemyDamaged(NetworkId, health.CurrentHealth, attackerPlayerId);
    }

    public CoopEnemyState ToState()
    {
        return new CoopEnemyState
        {
            id = NetworkId,
            x = transform.position.x,
            z = transform.position.z,
            hp = health != null ? health.CurrentHealth : 0f,
            maxHp = health != null ? health.MaxHealth : 0f,
            speed = moveSpeed,
            defense = Defense,
            isBoss = IsBoss,
            goldReward = GoldReward,
            monsterCode = monsterCode,
            archetype = CoopEnemyArchetypeUtil.ToId(Archetype)
        };
    }

    private Vector3 FindMoveTarget()
    {
        CoopPlayerTowerUnit nearestTower = null;
        var best = float.MaxValue;
        foreach (var tower in FindObjectsByType<CoopPlayerTowerUnit>(FindObjectsSortMode.None))
        {
            if (tower == null)
                continue;

            var towerHealth = tower.GetComponent<Health>();
            if (towerHealth != null && !towerHealth.IsAlive)
                continue;

            var dist = Vector3.Distance(transform.position, tower.transform.position);
            if (dist >= best)
                continue;

            best = dist;
            nearestTower = tower;
        }

        return nearestTower != null ? nearestTower.transform.position : transform.position;
    }

    private void TickTankMelee()
    {
        meleeTimer -= Time.deltaTime;
        if (meleeTimer > 0f)
            return;

        meleeTimer = Mathf.Max(0.35f, meleeInterval);
        slimeVisual?.PlayAttack();
        DamageNearestTower(contactDamage);
    }

    private void StrikeAndExpire()
    {
        slimeVisual?.PlayAttack();
        DamageNearestTower(contactDamage);
        CompleteEnemy(false);
    }

    private void Detonate()
    {
        if (hasDetonated)
            return;

        hasDetonated = true;
        slimeVisual?.PlayAttack();
        CoopEnemyExplosionVfx.Spawn(
            transform.position,
            explosionRadius,
            Archetype == CoopEnemyArchetype.HeavyBomber || IsBoss);

        session?.BroadcastFx(new CoopFxEventPayload
        {
            fxKind = CoopGameProtocol.FxExplosion,
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z,
            radius = explosionRadius,
            heavy = Archetype == CoopEnemyArchetype.HeavyBomber || IsBoss
        });

        foreach (var tower in FindObjectsByType<CoopPlayerTowerUnit>(FindObjectsSortMode.None))
        {
            if (tower == null)
                continue;

            var dist = Vector3.Distance(tower.transform.position, transform.position);
            if (dist > explosionRadius + 0.35f)
                continue;

            var falloff = 1f - (dist / Mathf.Max(0.1f, explosionRadius)) * 0.3f;
            var damage = explosionDamage * Mathf.Clamp(falloff, 0.55f, 1f);
            ApplyDamageToTower(tower, damage);
        }

        CompleteEnemy(true);
    }

    private void DamageNearestTower(float damage)
    {
        foreach (var tower in FindObjectsByType<CoopPlayerTowerUnit>(FindObjectsSortMode.None))
        {
            if (tower == null)
                continue;

            if (GetSurfaceGapTo(tower.transform.position) > touchRadius + 0.25f)
                continue;

            ApplyDamageToTower(tower, damage);
            break;
        }
    }

    private void ApplyDamageToTower(CoopPlayerTowerUnit tower, float damage)
    {
        var towerHealth = tower.GetComponent<Health>();
        if (towerHealth != null)
            towerHealth.TakeDamage(damage);
        else
            session?.DamagePlayerTower(tower.PlayerId, damage);
    }

    private void CompleteEnemy(bool fromSuicide)
    {
        killedByCombat = true;
        session?.OnEnemyKilled(NetworkId);
        Destroy(gameObject);
    }

    private void HandleDeath()
    {
        if (hasDetonated)
            return;

        if (Archetype.IsSuicide())
        {
            Detonate();
            return;
        }

        killedByCombat = true;
        session?.OnEnemyKilled(NetworkId);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;

        if (!killedByCombat && !hasDetonated)
            session?.UnregisterEnemyIfPresent(NetworkId);
    }
}
