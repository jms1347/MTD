using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CwslPlayerProjectile : NetworkBehaviour, ICwslPooledNetworkObject
{
    private static int monsterLayerMask = -1;

    private readonly HashSet<ulong> hitMonsterIds = new();

    private Vector3 direction;
    private Vector3 spawnOrigin;
    private float speed;
    private float lifetime;
    private float spawnTime;
    private ulong ownerClientId;
    private NetworkObject ownerNetworkObject;
    private float damage;
    private bool configured;
    private bool pierce;
    private CwslMonsterHealth homingTarget;

    public void Configure(
        Vector3 fireDirection,
        float projectileSpeed,
        float maxLifetime,
        ulong attackerClientId,
        float projectileDamage,
        bool piercing = false,
        NetworkObject owner = null,
        CwslMonsterHealth target = null)
    {
        direction = fireDirection.sqrMagnitude < 0.0001f ? Vector3.forward : fireDirection.normalized;
        speed = projectileSpeed;
        lifetime = maxLifetime;
        spawnTime = Time.time;
        spawnOrigin = transform.position;
        ownerClientId = attackerClientId;
        ownerNetworkObject = owner;
        damage = projectileDamage;
        pierce = piercing;
        homingTarget = target;
        configured = true;
        hitMonsterIds.Clear();
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public void OnSpawnedFromPool()
    {
        configured = false;
        pierce = false;
        homingTarget = null;
        spawnTime = 0f;
        ownerNetworkObject = null;
        hitMonsterIds.Clear();
    }

    public void OnReturnedToPool()
    {
        configured = false;
        pierce = false;
        homingTarget = null;
        ownerNetworkObject = null;
        hitMonsterIds.Clear();
    }

    private void Update()
    {
        if (!IsServer || !configured)
            return;

        if (Time.time - spawnTime > lifetime)
        {
            DespawnSelf();
            return;
        }

        var step = speed * Time.deltaTime;
        ApplyTargetHoming();
        var from = transform.position;
        var to = from + direction * step;

        if (CanHitNow())
            TryHitAlongPath(from, to);

        transform.position = to;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !configured || !CanHitNow() || ShouldIgnoreCollider(other))
            return;

        TryDamageCollider(other);
    }

    private bool CanHitNow()
    {
        if (Time.time - spawnTime < CwslGameConstants.PlayerArrowMinHitDelay)
            return false;

        var traveled = Vector3.Distance(transform.position, spawnOrigin);
        return traveled >= CwslGameConstants.PlayerArrowMinHitDistance;
    }

    private bool ShouldIgnoreCollider(Component collider)
    {
        if (collider == null)
            return true;

        if (ownerNetworkObject != null)
        {
            var hitObject = collider.GetComponentInParent<NetworkObject>();
            if (hitObject != null && hitObject.NetworkObjectId == ownerNetworkObject.NetworkObjectId)
                return true;
        }

        var ownerHealth = collider.GetComponentInParent<CwslPlayerHealth>();
        return ownerHealth != null && ownerHealth.OwnerClientId == ownerClientId;
    }

    private void TryHitAlongPath(Vector3 from, Vector3 to)
    {
        if (monsterLayerMask < 0)
            monsterLayerMask = LayerMask.GetMask(CwslGameConstants.LayerMonster);

        var delta = to - from;
        var distance = delta.magnitude;
        if (distance > 0.0001f)
        {
            var hits = Physics.SphereCastAll(
                from,
                CwslGameConstants.PlayerBulletHitRadius,
                delta.normalized,
                distance,
                monsterLayerMask,
                QueryTriggerInteraction.Collide);

            foreach (var hit in hits)
            {
                if (ShouldIgnoreCollider(hit.collider))
                    continue;

                TryDamageCollider(hit.collider);
                if (!configured)
                    return;
            }
        }

        TryHitMonstersAt(to);
        TryFlatDirectionHit(from, to);
    }

    private void TryHitMonstersAt(Vector3 position)
    {
        if (monsterLayerMask < 0)
            monsterLayerMask = LayerMask.GetMask(CwslGameConstants.LayerMonster);

        var hits = Physics.OverlapSphere(
            position,
            CwslGameConstants.PlayerBulletHitRadius,
            monsterLayerMask,
            QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            if (ShouldIgnoreCollider(hit))
                continue;

            TryDamageCollider(hit);
            if (!configured)
                return;
        }
    }

    private void ApplyTargetHoming()
    {
        if (homingTarget == null || !homingTarget.IsAlive)
            return;

        var aimPoint = homingTarget.GetAimPoint();
        var desired = aimPoint - transform.position;
        if (desired.sqrMagnitude < 0.0001f)
            return;

        direction = Vector3.Slerp(
            direction,
            desired.normalized,
            Time.deltaTime * CwslGameConstants.PlayerBulletHomingStrength).normalized;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    /// <summary>
    /// 쿼터뷰 사격: XZ 방향이 맞으면 Y 높이 차이와 무관하게 적중.
    /// </summary>
    private void TryFlatDirectionHit(Vector3 from, Vector3 to)
    {
        if (homingTarget != null && homingTarget.IsAlive)
        {
            if (IsFlatHit(from, to, homingTarget))
                TryDamageMonster(homingTarget);
            return;
        }

        var flatDir = direction;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f)
            return;

        flatDir.Normalize();
        var flatDx = flatDir.x;
        var flatDz = flatDir.z;

        var segment = to - from;
        var segmentFlatLen = Mathf.Sqrt(segment.x * segment.x + segment.z * segment.z);
        if (segmentFlatLen < 0.0001f)
            return;

        var fromX = from.x;
        var fromZ = from.z;
        var reach = segmentFlatLen + CwslGameConstants.PlayerBulletHitRadius;
        var hitRadius = CwslGameConstants.PlayerBulletHitRadius;

        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (!IsFlatHit(from, to, monster, flatDx, flatDz, fromX, fromZ, reach, hitRadius))
                continue;

            TryDamageMonster(monster);
            if (!configured)
                return;
        }
    }

    private bool IsFlatHit(Vector3 from, Vector3 to, CwslMonsterHealth monster)
    {
        var flatDir = direction;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f)
            return false;

        flatDir.Normalize();
        var segment = to - from;
        var segmentFlatLen = Mathf.Sqrt(segment.x * segment.x + segment.z * segment.z);
        if (segmentFlatLen < 0.0001f)
            return false;

        return IsFlatHit(
            from,
            to,
            monster,
            flatDir.x,
            flatDir.z,
            from.x,
            from.z,
            segmentFlatLen + CwslGameConstants.PlayerBulletHitRadius,
            CwslGameConstants.PlayerBulletHitRadius);
    }

    private static bool IsFlatHit(
        Vector3 from,
        Vector3 to,
        CwslMonsterHealth monster,
        float flatDx,
        float flatDz,
        float fromX,
        float fromZ,
        float reach,
        float hitRadius)
    {
        var pos = monster.transform.position;
        var relX = pos.x - fromX;
        var relZ = pos.z - fromZ;
        var projected = relX * flatDx + relZ * flatDz;
        if (projected < -hitRadius || projected > reach)
            return false;

        var lateralSq = relX * relX + relZ * relZ - projected * projected;
        var lateralSlop = hitRadius + GetMonsterFlatRadius(monster);
        return lateralSq <= lateralSlop * lateralSlop;
    }

    private static float GetMonsterFlatRadius(CwslMonsterHealth monster)
    {
        var capsule = monster.GetComponent<CapsuleCollider>();
        return capsule != null
            ? capsule.radius
            : CwslGameConstants.MonsterHitMinRadius;
    }

    private void TryDamageCollider(Component collider)
    {
        if (!configured || !CanHitNow() || ShouldIgnoreCollider(collider))
            return;

        var monsterHealth = collider.GetComponentInParent<CwslMonsterHealth>();
        if (monsterHealth == null || !monsterHealth.IsAlive)
            return;

        TryDamageMonster(monsterHealth);
    }

    private void TryDamageMonster(CwslMonsterHealth monsterHealth)
    {
        if (!configured || !CanHitNow() || monsterHealth == null || !monsterHealth.IsAlive)
            return;

        if (homingTarget != null && monsterHealth != homingTarget)
            return;

        var networkObject = monsterHealth.NetworkObject;
        if (networkObject != null && !hitMonsterIds.Add(networkObject.NetworkObjectId))
            return;

        monsterHealth.DamageFromPlayer(ownerClientId, damage);

        if (!pierce)
        {
            configured = false;
            DespawnSelf();
        }
    }

    private void DespawnSelf()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }
}
