using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 디아 오브에서 흩어지는 작은 얼음 구체.
/// </summary>
public class DefenseFrozenOrbShard : MonoBehaviour
{
    private const float GroundY = 0.05f;

    private Vector3 velocity;
    private float damage;
    private DefenseSkillData skill;
    private string targetMobility;
    private float endTime;
    private float colliderRadius = 0.12f;
    private readonly HashSet<int> hitEnemyIds = new();

    public static void Spawn(
        Vector3 position,
        Vector3 velocity,
        float shardDamage,
        DefenseSkillData skillData,
        string mobility,
        float lifetime,
        float scale = 0.4f)
    {
        var shardObject = new GameObject("FrozenOrbShard");
        shardObject.transform.position = position;
        shardObject.transform.localScale = Vector3.one * Mathf.Max(0.15f, scale);

        var shard = shardObject.AddComponent<DefenseFrozenOrbShard>();
        shard.Initialize(velocity, shardDamage, skillData, mobility, lifetime);
        shard.BuildVisual();
        Object.Destroy(shardObject, lifetime + 0.05f);
    }

    private void Initialize(
        Vector3 shardVelocity,
        float shardDamage,
        DefenseSkillData skillData,
        string mobility,
        float lifetime)
    {
        velocity = shardVelocity;
        damage = shardDamage;
        skill = skillData;
        targetMobility = mobility;
        endTime = Time.time + lifetime;
    }

    private void BuildVisual()
    {
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "ShardCore";
        core.transform.SetParent(transform, false);
        core.transform.localScale = Vector3.one * 0.55f;

        var collider = core.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        var renderer = core.GetComponent<Renderer>();
        if (renderer != null)
            DefenseFrozenOrbVisualUtility.ApplyIceOrbMaterial(renderer);
    }

    private void FixedUpdate()
    {
        if (Time.time >= endTime)
            return;

        transform.position += velocity * Time.fixedDeltaTime;

        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);

        if (transform.position.y < GroundY)
        {
            var flat = velocity;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                velocity = flat.normalized * velocity.magnitude;
        }

        var overlaps = Physics.OverlapSphere(
            transform.position,
            colliderRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        foreach (var overlap in overlaps)
        {
            if (!DefenseEnemyQuery.IsAttackableCollider(overlap, out var enemy, targetMobility, requireLanded: true))
                continue;

            int enemyId = enemy.GetInstanceID();
            if (!hitEnemyIds.Add(enemyId))
                continue;

            Vector3 hitPoint = overlap.ClosestPoint(transform.position);
            MonsterStatusCombatResolver.ApplyDamageToEnemy(
                enemy,
                damage,
                DefenseSkillElement.Ice,
                hitPoint);

            if (skill != null)
                DefenseEffectApplicator.ApplySkillEffects(enemy, skill, transform.position);
        }
    }
}
