using UnityEngine;

/// <summary>
/// 클라이언트 미러 적 — HP·콜라이더·타겟팅용 프록시.
/// </summary>
public class CoopMirroredEnemyProxy : MonoBehaviour
{
    public int NetworkId { get; private set; }
    public string ArchetypeId { get; private set; }

    private Health health;
    private CapsuleCollider collider;

    public void Initialize(CoopEnemyState state)
    {
        NetworkId = state.id;
        ArchetypeId = state.archetype;
        gameObject.tag = "Enemy";

        health = gameObject.GetComponent<Health>();
        if (health == null)
            health = gameObject.AddComponent<Health>();
        health.Initialize(state.maxHp, 0f, state.defense);
        health.SetDestroyOnDeath(false);

        collider = gameObject.GetComponent<CapsuleCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<CapsuleCollider>();

        var radius = (state.isBoss ? 0.7f : ResolveColliderRadius(state.archetype)) * CoopSlimeVisualFactory.MonsterScale;
        collider.center = new Vector3(0f, radius, 0f);
        collider.radius = radius;
        collider.height = radius * 2f;
        collider.direction = 1;
        collider.isTrigger = false;

        var healthBar = gameObject.GetComponent<HealthBarUI>();
        if (healthBar == null)
            healthBar = gameObject.AddComponent<HealthBarUI>();
        healthBar.ConfigureAsEnemy();
        healthBar.RefreshForSpawn();
    }

    public void ApplySyncedState(CoopEnemyState state)
    {
        if (health == null)
        {
            Initialize(state);
            return;
        }

        health.ApplyAuthoritativeHealth(state.hp, state.maxHp, state.defense);
    }

    public bool IsAlive => health != null && health.IsAlive;

    private static float ResolveColliderRadius(string archetypeId)
    {
        if (!CoopEnemyArchetypeUtil.TryParse(archetypeId, out var archetype))
            return 0.55f;

        return archetype switch
        {
            CoopEnemyArchetype.Rusher => 0.48f,
            CoopEnemyArchetype.Tank => 0.68f,
            CoopEnemyArchetype.Missile => 0.4f,
            CoopEnemyArchetype.HeavyBomber => 0.62f,
            _ => 0.55f
        };
    }
}
