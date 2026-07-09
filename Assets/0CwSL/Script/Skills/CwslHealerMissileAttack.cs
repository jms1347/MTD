using Unity.Netcode;
using UnityEngine;

/// <summary>힐러 평타 — 초록 미사일, 3초 1발, 가까운 적 자동.</summary>
public class CwslHealerMissileAttack : NetworkBehaviour
{
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerCombat combat;
    private CwslPlayerSelection selection;
    private CwslPlayerMovement movement;
    private float nextFireTime;

    public float AttackRange => CwslGameConstants.HealerMissileRange;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        combat = GetComponent<CwslPlayerCombat>();
        selection = GetComponent<CwslPlayerSelection>();
        movement = GetComponent<CwslPlayerMovement>();
    }

    private void Update()
    {
        if (!IsServer ||
            playerCharacter == null ||
            playerCharacter.CharacterId != CwslCharacterId.Healer)
            return;

        if (combat != null && combat.IsPureMoveMode)
            return;

        TryFireServer();
    }

    public bool TryFireServer()
    {
        if (!IsServer ||
            playerCharacter == null ||
            playerCharacter.CharacterId != CwslCharacterId.Healer)
            return false;

        if (Time.time < nextFireTime)
            return false;

        if (!TryResolveMonster(out var monster))
            return false;

        var distance = Vector3.Distance(transform.position, monster.transform.position);
        if (distance > AttackRange)
        {
            if (combat != null && (combat.IsAttackMoveActive || combat.HasChaseTarget))
                movement?.RequestMoveTo(monster.transform.position);
            return false;
        }

        FaceToward(monster.GetAimPoint());
        if (!FireProjectile(monster))
            return false;

        nextFireTime = Time.time + CwslGameConstants.HealerMissileCooldown
                       / Mathf.Max(0.25f, GetComponent<CwslAttackSpeedBuff>()?.AttackSpeedMultiplier ?? 1f);
        PlayFireClientRpc();
        return true;
    }

    private bool TryResolveMonster(out CwslMonsterHealth monster)
    {
        monster = null;
        if (selection != null &&
            selection.TryGetSelectedTarget(out var selected) &&
            selected != null)
        {
            var selectedHealth = selected.GetComponent<CwslMonsterHealth>();
            if (selectedHealth != null && selectedHealth.IsAlive)
            {
                monster = selectedHealth;
                return true;
            }
        }

        var best = AttackRange;
        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var candidate in monsters)
        {
            if (candidate == null || !candidate.IsAlive)
                continue;

            var d = Vector3.Distance(transform.position, candidate.transform.position);
            if (d >= best)
                continue;

            best = d;
            monster = candidate;
        }

        return monster != null;
    }

    private void FaceToward(Vector3 aim)
    {
        var flat = aim - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            return;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(flat.normalized, Vector3.up),
            720f * Time.deltaTime);
    }

    private bool FireProjectile(CwslMonsterHealth locked)
    {
        var session = CwslGameSession.Instance;
        if (session == null || session.Assets.playerMissilePrefab == null)
            return false;

        var muzzle = transform.position + Vector3.up * 1.2f + transform.forward * 0.65f;
        var aim = locked.GetAimPoint();
        var direction = aim - muzzle;
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
            CwslCharacterId.Healer,
            CwslGameConstants.BasicAttackSkillCoeff);
        projectile?.Configure(
            direction,
            CwslGameConstants.HealerMissileSpeed,
            CwslGameConstants.HealerMissileLifetime,
            OwnerClientId,
            damage,
            piercing: false,
            owner: NetworkObject,
            target: locked);
        projectile?.SetVisualKindServer(6);
        return true;
    }

    [ClientRpc]
    private void PlayFireClientRpc()
    {
        transform.Find("Visual")?.GetComponent<CwslPlayerStaffCastVisual>()?.PlayCast();
    }
}
