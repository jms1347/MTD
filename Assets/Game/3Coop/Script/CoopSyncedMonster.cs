using UnityEngine;

[RequireComponent(typeof(Health))]
public class CoopSyncedMonster : MonoBehaviour
{
    public int NetworkId { get; private set; }
    public string MonsterCode { get; private set; }
    public bool IsBoss { get; private set; }
    public int GoldReward { get; private set; }

    private Health health;
    private Monster monster;
    private CoopGameSession session;
    private bool killedByCoop;

    public void Initialize(CoopGameSession gameSession, int networkId, MonsterData data, bool isBoss, int goldReward)
    {
        session = gameSession;
        NetworkId = networkId;
        MonsterCode = data != null ? data.code : CoopGameProtocol.EnemyVisualTypes[0];
        IsBoss = isBoss;
        GoldReward = goldReward;

        health = GetComponent<Health>();
        monster = GetComponent<Monster>();

        var pooled = GetComponent<PooledEnemy>();
        if (pooled != null)
            Destroy(pooled);

        if (health != null)
            health.OnDeath += HandleDeath;

        GetComponent<MonsterLaneFollower>()?.ClearLane();
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
        var pos = transform.position;
        return new CoopEnemyState
        {
            id = NetworkId,
            x = pos.x,
            z = pos.z,
            hp = health != null ? health.CurrentHealth : 0f,
            maxHp = health != null ? health.MaxHealth : 0f,
            speed = monster != null ? monster.MoveSpeed : 3f,
            defense = health != null ? Mathf.RoundToInt(health.Defense) : 0,
            isBoss = IsBoss,
            goldReward = GoldReward,
            monsterCode = MonsterCode,
            archetype = "grunt"
        };
    }

    public bool TryGetWorldPosition(out Vector3 position)
    {
        position = transform.position;
        return health != null && health.IsAlive;
    }

    private void HandleDeath()
    {
        killedByCoop = true;
        session?.OnEnemyKilled(NetworkId);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;

        if (!killedByCoop)
            session?.UnregisterEnemyIfPresent(NetworkId);
    }
}
