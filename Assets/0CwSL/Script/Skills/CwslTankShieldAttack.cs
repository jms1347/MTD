using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>?? ?ù? ?ùù? ??3ù?ù? ?ù? ???? ?ù?. Q ?? ?? ??3?∑???</summary>
public class CwslTankShieldAttack : NetworkBehaviour
{
    private const float WindupSeconds = 0.32f;

    private float nextAttackTime;
    private Coroutine bashRoutine;
    private CwslPlayerCharacter playerCharacter;
    private CwslTankFortifySkill fortifySkill;
    private CwslTankShieldDashSkill dashSkill;
    private CwslTankShieldSlamSkill slamSkill;
    private CwslTankShieldWhirlwindSkill whirlwindSkill;

    public bool IsAttacking => bashRoutine != null;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        fortifySkill = GetComponent<CwslTankFortifySkill>();
        dashSkill = GetComponent<CwslTankShieldDashSkill>();
        slamSkill = GetComponent<CwslTankShieldSlamSkill>();
        whirlwindSkill = GetComponent<CwslTankShieldWhirlwindSkill>();

        var playerHealth = GetComponent<CwslPlayerHealth>();
        if (playerHealth != null)
            playerHealth.OnDied += HandleOwnerDied;
    }

    public override void OnNetworkDespawn()
    {
        var playerHealth = GetComponent<CwslPlayerHealth>();
        if (playerHealth != null)
            playerHealth.OnDied -= HandleOwnerDied;

        CancelSkillServer();
    }

    private void OnDisable()
    {
        CancelSkillServer();
    }

    private void HandleOwnerDied()
    {
        CancelSkillServer();
    }

    public void CancelSkillServer()
    {
        if (bashRoutine != null)
        {
            StopCoroutine(bashRoutine);
            bashRoutine = null;
        }
    }

    public bool TryPerformAttackServer(
        NetworkObject targetObject,
        CwslMonsterHealth monsterHealth,
        CwslEnemyBase enemyBase,
        float attackRange)
    {
        if (!IsServer || targetObject == null || bashRoutine != null)
            return false;

        if (dashSkill != null && dashSkill.IsDashing)
            return false;

        if (slamSkill != null && slamSkill.IsSlamming)
            return false;

        if (whirlwindSkill != null && whirlwindSkill.IsWhirlwinding)
            return false;

        if (Time.time < nextAttackTime)
            return false;

        if (!CwslPlayerShieldBashVisual.IsInStrikeRange(transform, targetObject))
            return false;

        var attackPower = playerCharacter != null
            ? CwslCharacterStatCatalog.GetAttackPower(playerCharacter.CharacterId)
            : CwslGameConstants.AttackDamage;

        var hitPoint = ResolveHitPoint(targetObject);
        bashRoutine = StartCoroutine(BashRoutine(
            targetObject,
            monsterHealth,
            enemyBase,
            attackPower,
            hitPoint));
        return true;
    }

    private IEnumerator BashRoutine(
        NetworkObject targetObject,
        CwslMonsterHealth monsterHealth,
        CwslEnemyBase enemyBase,
        float attackPower,
        Vector3 hitPoint)
    {
        try
        {
            nextAttackTime = Time.time + CwslCharacterStatCatalog.GetAttackCooldown(CwslCharacterId.Tank);
            PlayShieldWindupClientRpc(hitPoint);

            yield return new WaitForSeconds(WindupSeconds);

            if (IsEmpoweredShieldAttack())
            {
                var empoweredDamage = CwslCombatMath.ResolveSkillDamage(
                    CwslCharacterId.Tank,
                    CwslGameConstants.TankEmpoweredBashSkillCoeff);
                ApplyAreaDamageServer(empoweredDamage, ResolveEmpoweredHitCenter());
            }
            else if (targetObject != null && targetObject.IsSpawned &&
                     CwslPlayerShieldBashVisual.IsInStrikeRange(transform, targetObject))
            {
                var basicDamage = CwslCombatMath.ResolveSkillDamage(
                    CwslCharacterId.Tank,
                    CwslGameConstants.BasicAttackSkillCoeff);
                if (monsterHealth != null && monsterHealth.IsAlive)
                    monsterHealth.DamageFromPlayer(OwnerClientId, basicDamage);
                else if (enemyBase != null && enemyBase.IsAlive)
                    enemyBase.DamageFromPlayer(OwnerClientId, basicDamage);
            }

            PlayShieldImpactClientRpc(hitPoint, IsEmpoweredShieldAttack());
        }
        finally
        {
            bashRoutine = null;
        }
    }

    private bool IsEmpoweredShieldAttack()
    {
        return fortifySkill != null && fortifySkill.IsShieldActive;
    }

    private Vector3 ResolveEmpoweredHitCenter()
    {
        var forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        return transform.position + forward * CwslPlayerShieldBashVisual.StrikeReach;
    }

    private void ApplyAreaDamageServer(float damage, Vector3 center)
    {
        if (!IsServer || damage <= 0f)
            return;

        var radius = CwslGameConstants.FortifyEmpoweredAttackRadius;
        var radiusSq = radius * radius;
        var flatCenter = center;
        flatCenter.y = 0f;

        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position;
            flat.y = 0f;
            if ((flat - flatCenter).sqrMagnitude > radiusSq)
                continue;

            monster.DamageFromPlayer(OwnerClientId, damage);
        }

        var enemyBases = FindObjectsByType<CwslEnemyBase>(FindObjectsSortMode.None);
        foreach (var enemyBase in enemyBases)
        {
            if (enemyBase == null || !enemyBase.IsAlive)
                continue;

            var flat = enemyBase.transform.position;
            flat.y = 0f;
            if ((flat - flatCenter).sqrMagnitude > radiusSq)
                continue;

            enemyBase.DamageFromPlayer(OwnerClientId, damage);
        }
    }

    private static Vector3 ResolveHitPoint(NetworkObject target)
    {
        if (target == null)
            return Vector3.zero;

        var monster = target.GetComponent<CwslMonsterHealth>();
        if (monster != null)
            return monster.GetAimPoint();

        var enemyBase = target.GetComponent<CwslEnemyBase>();
        if (enemyBase != null)
            return enemyBase.GetAimPoint();

        return target.transform.position + Vector3.up * 1f;
    }

    [ClientRpc]
    private void PlayShieldWindupClientRpc(Vector3 hitPoint)
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerShieldBashVisual>()?.PlayWindup(hitPoint);
    }

    [ClientRpc]
    private void PlayShieldImpactClientRpc(Vector3 hitPoint, bool empowered)
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerShieldBashVisual>()?.PlayImpact(hitPoint, empowered);
    }
}
