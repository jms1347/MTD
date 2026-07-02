using System;
using UnityEngine;

public class CoopEnemyActor : MonoBehaviour
{
    public int NetworkId { get; private set; }
    public bool IsBoss { get; private set; }
    public int GoldReward { get; private set; }
    public int Defense { get; private set; }

    private Health health;
    private float moveSpeed = 3f;
    private float contactDamage = 6f;
    private string monsterCode;
    private CoopGameSession session;
    private CoopEnemySlimeVisual slimeVisual;
    private bool killedByCombat;

    public float MoveSpeed => moveSpeed;

    public void Initialize(
        CoopGameSession gameSession,
        int networkId,
        Vector3 position,
        float maxHp,
        int defense,
        float speed,
        bool isBoss,
        int goldReward,
        string monsterCode,
        float contactDamage = 6f)
    {
        session = gameSession;
        NetworkId = networkId;
        IsBoss = isBoss;
        GoldReward = goldReward;
        Defense = defense;
        moveSpeed = speed;
        this.contactDamage = contactDamage;
        this.monsterCode = monsterCode;

        gameObject.tag = "Enemy";
        transform.position = position;

        health = gameObject.GetComponent<Health>();
        if (health == null)
            health = gameObject.AddComponent<Health>();
        health.Initialize(maxHp, 0.2f, defense);
        health.OnDeath += HandleDeath;

        slimeVisual = CoopSlimeVisualFactory.Build(
            transform, monsterCode, moveSpeed, isBoss, out _);
    }

    private void Update()
    {
        if (session == null || !session.IsHostAuthority || health == null || !health.IsAlive)
            return;

        var target = FindMoveTarget();
        var flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.2f)
        {
            ReachTarget(target);
            return;
        }

        transform.position += flat.normalized * (moveSpeed * Time.deltaTime);
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
        health.Heal(hp - health.CurrentHealth);
    }

    public void TakeDamage(float attack, int penetration, string attackerPlayerId)
    {
        if (health == null || !health.IsAlive)
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
            monsterCode = monsterCode
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

            var healthComponent = tower.GetComponent<Health>();
            if (healthComponent != null && !healthComponent.IsAlive)
                continue;

            var dist = Vector3.Distance(transform.position, tower.transform.position);
            if (dist >= best)
                continue;

            best = dist;
            nearestTower = tower;
        }

        return nearestTower != null ? nearestTower.transform.position : Vector3.zero;
    }

    private void ReachTarget(Vector3 target)
    {
        foreach (var tower in FindObjectsByType<CoopPlayerTowerUnit>(FindObjectsSortMode.None))
        {
            if (tower == null)
                continue;

            if (Vector3.Distance(tower.transform.position, transform.position) > 1.4f)
                continue;

            slimeVisual?.PlayAttack();

            var towerHealth = tower.GetComponent<Health>();
            if (towerHealth != null)
                towerHealth.TakeDamage(contactDamage);
            else
                session?.DamagePlayerTower(tower.PlayerId, contactDamage);
            break;
        }

        Destroy(gameObject);
    }

    private void HandleDeath()
    {
        killedByCombat = true;
        session?.OnEnemyKilled(NetworkId);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;

        if (!killedByCombat)
            session?.UnregisterEnemyIfPresent(NetworkId);
    }
}
