using System;
using UnityEngine;

/// <summary>
/// 메테오 타워가 떨어뜨리는 수직 낙하 투사체. 지면 충돌 시 폭발 이펙트와 범위 피해를 줍니다.
/// </summary>
public class MeteorStrikeProjectile : MonoBehaviour
{
    private const float GroundY = 0.05f;

    private Vector3 velocity;
    private float damage;
    private float impactRadius;
    private float explosionScalePerRadius;
    private GameObject explosionPrefab;
    private float colliderRadius = 0.6f;
    private bool hasImpacted;
    private Vector3 lastPosition;
    private Action<Vector3> onImpact;

    public void Launch(
        Vector3 velocity,
        float attackDamage,
        float radius,
        GameObject explosion,
        float visualScalePerRadius = 1f,
        Action<Vector3> onImpactCallback = null)
    {
        this.velocity = velocity;
        damage = attackDamage;
        impactRadius = radius;
        explosionPrefab = explosion;
        explosionScalePerRadius = visualScalePerRadius;
        onImpact = onImpactCallback;
        hasImpacted = false;
        lastPosition = transform.position;

        var legacy = GetComponent<ETFXProjectileScript>();
        if (legacy != null)
            Destroy(legacy);

        var sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.isTrigger = true;
        sphereCollider.radius = colliderRadius;

        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();

        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.linearVelocity = Vector3.zero;
    }

    public static void ExecuteInstantImpact(
        Vector3 groundPoint,
        float attackDamage,
        float radius,
        GameObject explosionPrefab,
        GameObject missilePrefab = null,
        float visualScalePerRadius = 1f)
    {
        Vector3 point = new Vector3(groundPoint.x, GroundY, groundPoint.z);

        if (missilePrefab != null)
        {
            var missile = UnityEngine.Object.Instantiate(missilePrefab, point + Vector3.up * 1.2f, Quaternion.LookRotation(Vector3.down));
            var legacy = missile.GetComponent<ETFXProjectileScript>();
            if (legacy != null)
                UnityEngine.Object.Destroy(legacy);

            UnityEngine.Object.Destroy(missile, 0.2f);
        }

        SpawnScaledExplosion(point, radius, explosionPrefab, visualScalePerRadius);
        ApplyAreaDamage(point, attackDamage, radius);
    }

    private void FixedUpdate()
    {
        if (hasImpacted)
            return;

        transform.position += velocity * Time.fixedDeltaTime;

        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity.normalized);

        if (transform.position.y <= GroundY)
        {
            ImpactAt(new Vector3(transform.position.x, GroundY, transform.position.z));
            return;
        }

        Vector3 displacement = transform.position - lastPosition;
        float castDistance = displacement.magnitude;
        if (castDistance > 0.0001f
            && Physics.SphereCast(
                lastPosition,
                colliderRadius,
                displacement.normalized,
                out RaycastHit hit,
                castDistance + colliderRadius,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore)
            && IsImpactSurface(hit.collider))
        {
            ImpactAt(hit.point);
            return;
        }

        lastPosition = transform.position;
    }

    private static bool IsImpactSurface(Collider collider)
    {
        if (collider == null || collider.isTrigger)
            return false;

        return collider.CompareTag("Ground")
            || collider.gameObject.name == "DefenseGround";
    }

    private void ImpactAt(Vector3 point)
    {
        if (hasImpacted)
            return;

        hasImpacted = true;
        var groundPoint = new Vector3(point.x, GroundY, point.z);
        transform.position = groundPoint;

        if (onImpact != null)
        {
            onImpact.Invoke(groundPoint);
            onImpact = null;
            Destroy(gameObject, 0.05f);
            return;
        }

        SpawnScaledExplosion(groundPoint, impactRadius, explosionPrefab, explosionScalePerRadius);
        ApplyAreaDamage(groundPoint, damage, impactRadius);

        Destroy(gameObject, 0.05f);
    }

    private static void SpawnScaledExplosion(
        Vector3 point,
        float radius,
        GameObject explosionPrefab,
        float visualScalePerRadius)
    {
        if (explosionPrefab == null)
            return;

        var explosion = UnityEngine.Object.Instantiate(
            explosionPrefab,
            point,
            DefenseCombatVfxSpawn.ResolveGroundBurstRotation(explosionPrefab));
        float scale = Mathf.Clamp(radius * visualScalePerRadius, 0.55f, 2f);
        explosion.transform.localScale = Vector3.one * scale;
        DefenseCombatVfxSpawn.EnsureVisualOnly(explosion);
        UnityEngine.Object.Destroy(explosion, 6f);
    }

    private static void ApplyAreaDamage(Vector3 center, float attackDamage, float radius)
    {
        var overlaps = Physics.OverlapSphere(center, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        foreach (var overlap in overlaps)
        {
            if (!DefenseEnemyQuery.IsAttackableCollider(overlap, out var enemy, requireLanded: true))
                continue;

            MonsterStatusCombatResolver.ApplyAoEDamageToEnemy(
                enemy,
                attackDamage,
                DefenseSkillElement.Fire,
                center);
        }
    }
}
