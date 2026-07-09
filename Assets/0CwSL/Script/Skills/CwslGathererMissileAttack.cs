using Unity.Netcode;
using UnityEngine;

/// <summary>링거 기본공격 — 보라/남색 마법 미사일, 2초당 1발.</summary>
public class CwslGathererMissileAttack : NetworkBehaviour
{
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerSelection selection;
    private CwslPlayerCombat combat;
    private CwslPlayerMovement movement;
    private float nextFireTime;

    public float AttackRange => CwslGameConstants.GathererMissileRange;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        selection = GetComponent<CwslPlayerSelection>();
        combat = GetComponent<CwslPlayerCombat>();
        movement = GetComponent<CwslPlayerMovement>();
    }

    private void Update()
    {
        if (!IsServer ||
            playerCharacter == null ||
            playerCharacter.CharacterId != CwslCharacterId.CrowdGatherer)
            return;

        if (combat != null && combat.IsPureMoveMode)
            return;

        if (!ShouldAutoFire())
            return;

        TryFireServer();
    }

    public bool TryFireServer()
    {
        if (!IsServer ||
            playerCharacter == null ||
            playerCharacter.CharacterId != CwslCharacterId.CrowdGatherer)
            return false;

        if (Time.time < nextFireTime)
            return false;

        if (!TryResolveTarget(out var monster, out var enemyBase, out var aimPoint))
            return false;

        FaceToward(aimPoint);
        if (!FireProjectileServer(aimPoint, monster))
            return false;

        nextFireTime = Time.time + CwslGameConstants.GathererMissileCooldown;
        PlayFireClientRpc(aimPoint);
        return true;
    }

    private bool ShouldAutoFire()
    {
        if (combat != null && (combat.IsAttackMoveActive || combat.HasChaseTarget))
            return true;

        if (selection != null &&
            selection.TryGetSelectedTarget(out var selected) &&
            selected != null)
        {
            var health = selected.GetComponent<CwslMonsterHealth>();
            if (health != null && health.IsAlive && IsInRange(selected.transform.position))
                return true;

            var enemyBase = selected.GetComponent<CwslEnemyBase>();
            if (enemyBase != null && enemyBase.IsAlive && IsInRange(selected.transform.position))
                return true;
        }

        return false;
    }

    private bool TryResolveTarget(
        out CwslMonsterHealth monster,
        out CwslEnemyBase enemyBase,
        out Vector3 aimPoint)
    {
        monster = null;
        enemyBase = null;
        aimPoint = transform.position + transform.forward * 8f;

        if (selection != null &&
            selection.TryGetSelectedTarget(out var selected) &&
            selected != null)
        {
            var selectedHealth = selected.GetComponent<CwslMonsterHealth>();
            if (selectedHealth != null && selectedHealth.IsAlive && IsInRange(selected.transform.position))
            {
                monster = selectedHealth;
                aimPoint = selectedHealth.GetAimPoint();
                return true;
            }

            var selectedBase = selected.GetComponent<CwslEnemyBase>();
            if (selectedBase != null && selectedBase.IsAlive && IsInRange(selected.transform.position))
            {
                enemyBase = selectedBase;
                aimPoint = selectedBase.GetAimPoint();
                return true;
            }
        }

        if (combat != null && (combat.IsAttackMoveActive || combat.HasChaseTarget))
        {
            var best = float.MaxValue;
            var monsters = CwslCombatRegistry.AliveMonsters;
            foreach (var candidate in monsters)
            {
                if (candidate == null || !candidate.IsAlive || !IsInRange(candidate.transform.position))
                    continue;

                var d = Vector3.Distance(transform.position, candidate.transform.position);
                if (d >= best)
                    continue;

                best = d;
                monster = candidate;
                aimPoint = candidate.GetAimPoint();
            }

            return monster != null;
        }

        return false;
    }

    private bool IsInRange(Vector3 position)
    {
        var flat = position - transform.position;
        flat.y = 0f;
        var range = CwslGameConstants.GathererMissileRange;
        return flat.sqrMagnitude <= range * range;
    }

    private void FaceToward(Vector3 aimPoint)
    {
        var flat = aimPoint - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(flat.normalized, Vector3.up),
            720f * Time.deltaTime);
    }

    private bool FireProjectileServer(Vector3 aimPoint, CwslMonsterHealth lockedTarget)
    {
        var session = CwslGameSession.Instance;
        if (session == null || session.Assets.playerMissilePrefab == null)
            return false;

        var muzzle = transform.position + Vector3.up * 1.15f + transform.forward * 0.7f;
        var direction = aimPoint - muzzle;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;
        else
            direction.Normalize();

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            session.Assets.playerMissilePrefab,
            muzzle,
            Quaternion.LookRotation(direction, Vector3.up));
        if (networkObject == null)
            return false;

        var projectile = networkObject.GetComponent<CwslPlayerProjectile>();
        var damage = CwslCombatMath.ResolveSkillDamage(
            CwslCharacterId.CrowdGatherer,
            CwslGameConstants.BasicAttackSkillCoeff);
        projectile?.Configure(
            direction,
            CwslGameConstants.GathererMissileSpeed,
            CwslGameConstants.GathererMissileLifetime,
            OwnerClientId,
            damage,
            piercing: false,
            owner: NetworkObject,
            target: lockedTarget);

        // 비주얼 kind 5 = 보라 미사일 (CwslPlayerProjectileVisual에서 처리)
        if (projectile != null)
            projectile.SetVisualKindServer(5);

        return true;
    }

    [ClientRpc]
    private void PlayFireClientRpc(Vector3 aimPoint)
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerStaffCastVisual>()?.PlayCast();
        CwslVfxSpawner.SpawnGathererMuzzle(transform.position + Vector3.up * 1.15f + transform.forward * 0.7f);
    }
}
