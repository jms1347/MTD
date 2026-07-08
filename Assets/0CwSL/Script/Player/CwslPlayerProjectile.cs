using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CwslPlayerProjectile : NetworkBehaviour, ICwslPooledNetworkObject
{
    private static int monsterLayerMask = -1;
    private static int playerLayerMask = -1;

    private readonly HashSet<ulong> hitMonsterIds = new();
    private readonly HashSet<ulong> hitPlayerIds = new();
    private readonly NetworkVariable<byte> networkVisualKind = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

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
    private int piercesRemaining;
    private CwslMissileTankAmmoKind ammoKind = CwslMissileTankAmmoKind.Basic;
    private bool smokeBomb;
    /// <summary>
    /// 발사 시 잠근 1차 타겟(보스 집중 사격·스폰 오프셋용). 비행 중 유도는 하지 않는다.
    /// </summary>
    private CwslMonsterHealth lockedTarget;

    public byte VisualKind => networkVisualKind.Value;

    public void SetVisualKindServer(byte kind)
    {
        if (!IsServer)
            return;

        networkVisualKind.Value = kind;
    }

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
        ConfigureAdvanced(
            fireDirection,
            projectileSpeed,
            maxLifetime,
            attackerClientId,
            projectileDamage,
            owner,
            target,
            CwslMissileTankAmmoKind.Basic,
            piercing ? int.MaxValue / 4 : 0,
            false);
    }

    public void ConfigureAdvanced(
        Vector3 fireDirection,
        float projectileSpeed,
        float maxLifetime,
        ulong attackerClientId,
        float projectileDamage,
        NetworkObject owner,
        CwslMonsterHealth target,
        CwslMissileTankAmmoKind kind,
        int maxPierceHits,
        bool spawnSmokeZone)
    {
        direction = fireDirection.sqrMagnitude < 0.0001f ? Vector3.forward : fireDirection.normalized;
        speed = projectileSpeed;
        lifetime = maxLifetime;
        spawnTime = Time.time;
        spawnOrigin = transform.position;
        ownerClientId = attackerClientId;
        ownerNetworkObject = owner;
        damage = projectileDamage;
        ammoKind = kind;
        smokeBomb = spawnSmokeZone;
        piercesRemaining = Mathf.Max(0, maxPierceHits);
        pierce = piercesRemaining > 0;
        lockedTarget = target;
        configured = true;
        hitMonsterIds.Clear();
        hitPlayerIds.Clear();
        networkVisualKind.Value = ResolveVisualKind(kind, spawnSmokeZone);
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        CwslCombatRegistry.RegisterPlayerProjectile(this);
    }

    private static byte ResolveVisualKind(CwslMissileTankAmmoKind kind, bool spawnSmokeZone)
    {
        if (spawnSmokeZone)
            return 4;

        return (byte)kind;
    }

    public bool IsActiveProjectile => configured && IsSpawned;

    public void OnSpawnedFromPool()
    {
        configured = false;
        pierce = false;
        piercesRemaining = 0;
        smokeBomb = false;
        ammoKind = CwslMissileTankAmmoKind.Basic;
        networkVisualKind.Value = 0;
        lockedTarget = null;
        spawnTime = 0f;
        ownerNetworkObject = null;
        hitMonsterIds.Clear();
        hitPlayerIds.Clear();
    }

    public void OnReturnedToPool()
    {
        CwslCombatRegistry.UnregisterPlayerProjectile(this);
        configured = false;
        pierce = false;
        piercesRemaining = 0;
        smokeBomb = false;
        ammoKind = CwslMissileTankAmmoKind.Basic;
        networkVisualKind.Value = 0;
        lockedTarget = null;
        ownerNetworkObject = null;
        hitMonsterIds.Clear();
        hitPlayerIds.Clear();
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

        var step = speed * (GetComponent<CwslSlowModifier>()?.SpeedMultiplier ?? 1f) * Time.deltaTime;
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

        if (TryHitShieldBubble(other))
            return;

        TryDamageCollider(other);
    }

    private bool TryHitShieldBubble(Component collider)
    {
        if (!configured || collider == null)
            return false;

        var marker = collider.GetComponent<CwslShieldBubbleMarker>()
                     ?? collider.GetComponentInParent<CwslShieldBubbleMarker>();
        if (marker == null || marker.Bubble == null || !marker.Bubble.IsBubbleActive)
            return false;

        if (!marker.Bubble.TryBlockProjectileServer(transform.position, damage))
            return false;

        configured = false;
        DespawnSelf();
        return true;
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

    private bool ShouldSkipPlayerForProjectile(CwslPlayerHealth playerHealth)
    {
        return playerHealth == null
               || !playerHealth.IsAlive
               || playerHealth.OwnerClientId == ownerClientId;
    }

    private void TryHitAlongPath(Vector3 from, Vector3 to)
    {
        if (monsterLayerMask < 0)
            monsterLayerMask = LayerMask.GetMask(CwslGameConstants.LayerMonster);

        var monsterMask = monsterLayerMask != 0 ? monsterLayerMask : ~0;
        var delta = to - from;
        var distance = delta.magnitude;
        if (distance > 0.0001f)
        {
            var hits = Physics.SphereCastAll(
                from,
                CwslGameConstants.PlayerBulletHitRadius,
                delta.normalized,
                distance,
                monsterMask,
                QueryTriggerInteraction.Collide);

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (ShouldIgnoreCollider(hit.collider))
                    continue;

                var monsterHealth = hit.collider.GetComponentInParent<CwslMonsterHealth>();
                if (ShouldSkipMonsterForProjectile(monsterHealth))
                    continue;

                TryDamageCollider(hit.collider);
                if (!configured)
                    return;
            }
        }

        if (playerLayerMask < 0)
            playerLayerMask = LayerMask.GetMask(CwslGameConstants.LayerPlayer);

        var playerMask = playerLayerMask != 0 ? playerLayerMask : ~0;
        if (distance > 0.0001f)
        {
            var playerHits = Physics.SphereCastAll(
                from,
                CwslGameConstants.PlayerBulletHitRadius,
                delta.normalized,
                distance,
                playerMask,
                QueryTriggerInteraction.Collide);

            System.Array.Sort(playerHits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in playerHits)
            {
                if (ShouldIgnoreCollider(hit.collider))
                    continue;

                if (TryHitShieldBubble(hit.collider))
                    return;

                TryDamageCollider(hit.collider);
                if (!configured)
                    return;
            }
        }

        TryHitMonstersAt(to);
        TryHitPlayersAt(to);
        TryFlatDirectionHit(from, to);
        TryFlatDirectionHitPlayers(from, to);
    }

    private bool ShouldSkipMonsterForProjectile(CwslMonsterHealth monsterHealth)
    {
        if (monsterHealth == null || !monsterHealth.IsBoss)
            return false;

        return lockedTarget != null && lockedTarget != monsterHealth;
    }

    private void TryHitMonstersAt(Vector3 position)
    {
        if (monsterLayerMask < 0)
            monsterLayerMask = LayerMask.GetMask(CwslGameConstants.LayerMonster);

        var monsterMask = monsterLayerMask != 0 ? monsterLayerMask : ~0;
        var hits = Physics.OverlapSphere(
            position,
            CwslGameConstants.PlayerBulletHitRadius,
            monsterMask,
            QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            if (ShouldIgnoreCollider(hit))
                continue;

            var monsterHealth = hit.GetComponentInParent<CwslMonsterHealth>();
            if (ShouldSkipMonsterForProjectile(monsterHealth))
                continue;

            TryDamageCollider(hit);
            if (!configured)
                return;
        }
    }

    private void TryHitPlayersAt(Vector3 position)
    {
        if (playerLayerMask < 0)
            playerLayerMask = LayerMask.GetMask(CwslGameConstants.LayerPlayer);

        var mask = playerLayerMask != 0 ? playerLayerMask : ~0;
        var hits = Physics.OverlapSphere(
            position,
            CwslGameConstants.PlayerBulletHitRadius,
            mask,
            QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            if (ShouldIgnoreCollider(hit))
                continue;

            if (TryHitShieldBubble(hit))
                return;

            TryDamageCollider(hit);
            if (!configured)
                return;
        }

        TryHitPlayersByDistance(position);
    }

    private void TryHitPlayersByDistance(Vector3 position)
    {
        var players = CwslCombatRegistry.AlivePlayers;
        foreach (var playerHealth in players)
        {
            if (ShouldSkipPlayerForProjectile(playerHealth))
                continue;

            if (playerHealth.TryInterceptProjectileServer(position, damage))
            {
                configured = false;
                DespawnSelf();
                return;
            }

            var flat = playerHealth.transform.position - position;
            flat.y = 0f;
            var bodyRadius = playerHealth.GetComponent<CwslPlayerBodyCollider>()?.Radius
                ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;
            var hitReach = bodyRadius + CwslGameConstants.PlayerBodyHitSlop;
            if (flat.sqrMagnitude > hitReach * hitReach)
                continue;

            TryDamagePlayer(playerHealth);
            if (!configured)
                return;
        }
    }

    private void TryFlatDirectionHitPlayers(Vector3 from, Vector3 to)
    {
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

        var players = CwslCombatRegistry.AlivePlayers;
        foreach (var playerHealth in players)
        {
            if (ShouldSkipPlayerForProjectile(playerHealth))
                continue;

            if (!IsFlatHitPlayer(from, to, playerHealth, flatDx, flatDz, fromX, fromZ, reach, hitRadius))
                continue;

            TryDamagePlayer(playerHealth);
            if (!configured)
                return;
        }
    }

    private static bool IsFlatHitPlayer(
        Vector3 from,
        Vector3 to,
        CwslPlayerHealth playerHealth,
        float flatDx,
        float flatDz,
        float fromX,
        float fromZ,
        float reach,
        float hitRadius)
    {
        var pos = playerHealth.transform.position;
        var relX = pos.x - fromX;
        var relZ = pos.z - fromZ;
        var projected = relX * flatDx + relZ * flatDz;
        if (projected < -hitRadius || projected > reach)
            return false;

        var lateralSq = relX * relX + relZ * relZ - projected * projected;
        var bodyRadius = playerHealth.GetComponent<CwslPlayerBodyCollider>()?.Radius
            ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;
        var lateralSlop = hitRadius + bodyRadius + CwslGameConstants.PlayerBodyHitSlop;
        return lateralSq <= lateralSlop * lateralSlop;
    }

    /// <summary>
    /// 쿼터뷰 사격: XZ 방향이 맞으면 Y 높이 차이와 무관하게 적중.
    /// </summary>
    private void TryFlatDirectionHit(Vector3 from, Vector3 to)
    {
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

        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive || ShouldSkipMonsterForProjectile(monster))
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
        var pos = monster.GetFlatHitPoint();
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
        return monster != null
            ? monster.GetFlatHitRadius()
            : CwslGameConstants.MonsterHitMinRadius;
    }

    private void TryDamageCollider(Component collider)
    {
        if (!configured || !CanHitNow() || ShouldIgnoreCollider(collider))
            return;

        var monsterHealth = collider.GetComponentInParent<CwslMonsterHealth>();
        if (monsterHealth != null && monsterHealth.IsAlive && !ShouldSkipMonsterForProjectile(monsterHealth))
        {
            TryDamageMonster(monsterHealth);
            return;
        }

        var enemyBase = collider.GetComponentInParent<CwslEnemyBase>();
        if (enemyBase != null && enemyBase.IsAlive)
        {
            enemyBase.DamageFromPlayer(ownerClientId, damage);
            if (!pierce)
            {
                configured = false;
                DespawnSelf();
            }

            return;
        }

        var playerHealth = collider.GetComponentInParent<CwslPlayerHealth>();
        if (!ShouldSkipPlayerForProjectile(playerHealth))
            TryDamagePlayer(playerHealth);
    }

    private void TryDamagePlayer(CwslPlayerHealth playerHealth)
    {
        if (!configured || !CanHitNow() || ShouldSkipPlayerForProjectile(playerHealth))
            return;

        if (playerHealth.TryInterceptProjectileServer(transform.position, damage))
        {
            configured = false;
            DespawnSelf();
            return;
        }

        var networkObject = playerHealth.NetworkObject;
        if (networkObject != null && !hitPlayerIds.Add(networkObject.NetworkObjectId))
            return;

        playerHealth.TryReceiveProjectileHitServer(damage, transform.position);

        if (!pierce)
        {
            configured = false;
            DespawnSelf();
        }
    }

    private void TryDamageMonster(CwslMonsterHealth monsterHealth)
    {
        if (!configured || !CanHitNow() || monsterHealth == null || !monsterHealth.IsAlive)
            return;

        if (ShouldSkipMonsterForProjectile(monsterHealth))
            return;

        var networkObject = monsterHealth.NetworkObject;
        if (networkObject != null && !hitMonsterIds.Add(networkObject.NetworkObjectId))
            return;

        monsterHealth.DamageFromPlayer(ownerClientId, damage);

        if (smokeBomb)
        {
            var hitPoint = monsterHealth.GetFlatHitPoint();
            var ownerSkill = ownerNetworkObject != null
                ? ownerNetworkObject.GetComponent<CwslMissileTankSkill>()
                : null;
            CwslMissileTankSmokeZone.SpawnServer(
                hitPoint,
                CwslGameConstants.MissileTankSmokeZoneRadius,
                CwslGameConstants.MissileTankSmokeZoneDuration,
                ownerSkill);
            configured = false;
            DespawnSelf();
            return;
        }

        TryApplyProjectileStatusEffects(monsterHealth);

        if (piercesRemaining > 0)
        {
            piercesRemaining--;
            if (piercesRemaining <= 0)
            {
                configured = false;
                DespawnSelf();
            }

            return;
        }

        if (!pierce)
        {
            configured = false;
            DespawnSelf();
        }
    }

    private void TryApplyProjectileStatusEffects(CwslMonsterHealth monsterHealth)
    {
        if (!IsServer || monsterHealth == null)
            return;

        var owner = ownerNetworkObject;
        var character = owner != null ? owner.GetComponent<CwslPlayerCharacter>() : null;
        if (character == null || character.CharacterId != CwslCharacterId.MissileTank)
            return;

        var status = CwslMonsterStatusController.Ensure(monsterHealth);
        if (status == null)
            return;

        switch (ammoKind)
        {
            case CwslMissileTankAmmoKind.Fire:
                status.ApplyBurnServer(
                    ownerClientId,
                    CwslGameConstants.MonsterBurnDuration,
                    CwslGameConstants.MonsterBurnTotalDamage);
                break;
            case CwslMissileTankAmmoKind.Poison:
                status.ApplyPoisonServer(
                    ownerClientId,
                    CwslGameConstants.MonsterPoisonDuration,
                    CwslGameConstants.MonsterPoisonTickDamage,
                    CwslGameConstants.MonsterPoisonArmorPerStack);
                break;
            case CwslMissileTankAmmoKind.Lightning:
                status.ApplyShockServer(CwslGameConstants.MonsterShockDuration);
                break;
        }
    }

    private void DespawnSelf()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }
}
