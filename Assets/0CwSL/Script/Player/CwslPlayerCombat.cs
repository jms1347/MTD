using Unity.Netcode;
using UnityEngine;

public class CwslPlayerCombat : NetworkBehaviour
{
    private float nextAttackTime;
    private CwslPlayerController playerController;
    private CwslPlayerSelection selection;
    private CwslPlayerCharacter playerCharacter;
    private CwslMissileTankSkill missileTankSkill;
    private CwslGathererMissileAttack gathererMissileAttack;
    private CwslTankShieldAttack tankShieldAttack;
    private CwslPlayerMovement movement;

    private bool attackMoveActive;
    private Vector3 attackMoveDestination;
    private NetworkObject chaseAttackTarget;
    private bool pureMoveMode;

    public bool IsAttackMoveActive => attackMoveActive;
    public bool HasChaseTarget => chaseAttackTarget != null;
    public bool IsPureMoveMode => pureMoveMode;

    public void CancelAttackOrdersServer()
    {
        if (!IsServer)
            return;

        attackMoveActive = false;
        chaseAttackTarget = null;
        missileTankSkill?.ClearAttackFocus();
    }

    public void RequestMoveServer(Vector3 destination)
    {
        if (!IsServer)
            return;

        destination = CwslPlayerBossDebuff.ApplyReverseControlIfNeeded(
            destination,
            transform,
            GetComponent<CwslPlayerBossDebuff>());

        CancelAttackOrdersServer();
        pureMoveMode = true;
        movement?.RequestMoveTo(destination);
    }

    public override void OnNetworkSpawn()
    {
        playerController = GetComponent<CwslPlayerController>();
        selection = GetComponent<CwslPlayerSelection>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        missileTankSkill = GetComponent<CwslMissileTankSkill>();
        gathererMissileAttack = GetComponent<CwslGathererMissileAttack>();
        tankShieldAttack = GetComponent<CwslTankShieldAttack>();
        movement = GetComponent<CwslPlayerMovement>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        UpdateChaseAttackTarget();
        UpdateAttackMove();
    }

    private void UpdateChaseAttackTarget()
    {
        if (chaseAttackTarget == null)
            return;

        if (!chaseAttackTarget.IsSpawned)
        {
            chaseAttackTarget = null;
            return;
        }

        if (TryGetLivingEnemyBase(chaseAttackTarget, out var enemyBase))
        {
            if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.MissileTank)
                UpdateMissileTankChaseStructure(enemyBase);
            else if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.CrowdGatherer)
                UpdateGathererChaseStructure(enemyBase);
            else
                UpdateMeleeChaseStructure(enemyBase);
            return;
        }

        var monsterHealth = chaseAttackTarget.GetComponent<CwslMonsterHealth>();
        if (monsterHealth == null || !monsterHealth.IsAlive)
        {
            chaseAttackTarget = null;
            return;
        }

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.MissileTank)
            UpdateMissileTankChase(monsterHealth);
        else if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.CrowdGatherer)
            UpdateGathererChase(monsterHealth);
        else
            UpdateMeleeChase(monsterHealth);
    }

    private void UpdateMissileTankChase(CwslMonsterHealth monsterHealth)
    {
        var range = missileTankSkill != null ? missileTankSkill.AttackRange : 24f;
        var distance = Vector3.Distance(transform.position, monsterHealth.transform.position);
        if (distance > range)
        {
            movement?.RequestMoveTo(monsterHealth.transform.position);
            return;
        }

        movement?.StopMovement();
        missileTankSkill?.TryFireAttackServer(dualWieldMode: false);
    }

    private void UpdateGathererChase(CwslMonsterHealth monsterHealth)
    {
        var range = gathererMissileAttack != null ? gathererMissileAttack.AttackRange : CwslGameConstants.GathererMissileRange;
        var distance = Vector3.Distance(transform.position, monsterHealth.transform.position);
        if (distance > range)
        {
            movement?.RequestMoveTo(monsterHealth.transform.position);
            return;
        }

        movement?.StopMovement();
        gathererMissileAttack?.TryFireServer();
    }

    private void UpdateGathererChaseStructure(CwslEnemyBase enemyBase)
    {
        var range = gathererMissileAttack != null ? gathererMissileAttack.AttackRange : CwslGameConstants.GathererMissileRange;
        var distance = Vector3.Distance(transform.position, enemyBase.transform.position);
        if (distance > range)
        {
            movement?.RequestMoveTo(enemyBase.transform.position);
            return;
        }

        movement?.StopMovement();
        gathererMissileAttack?.TryFireServer();
    }

    private void UpdateMeleeChaseStructure(CwslEnemyBase enemyBase)
    {
        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.Tank)
        {
            if (!CwslPlayerShieldBashVisual.IsInStrikeRange(transform, chaseAttackTarget))
            {
                movement?.RequestMoveTo(enemyBase.transform.position);
                return;
            }

            movement?.StopMovement();
            AttackSelectedTarget(dualWieldMode: false);
            return;
        }

        var distance = Vector3.Distance(transform.position, enemyBase.transform.position);
        var range = GetAttackRange(chaseAttackTarget);
        if (distance > range)
        {
            movement?.RequestMoveTo(enemyBase.transform.position);
            return;
        }

        movement?.StopMovement();
        AttackSelectedTarget(dualWieldMode: false);
    }

    private void UpdateMissileTankChaseStructure(CwslEnemyBase enemyBase)
    {
        var range = missileTankSkill != null ? missileTankSkill.AttackRange : 24f;
        var distance = Vector3.Distance(transform.position, enemyBase.transform.position);
        if (distance > range)
        {
            movement?.RequestMoveTo(enemyBase.transform.position);
            return;
        }

        movement?.StopMovement();
        missileTankSkill?.TryFireAttackServer(dualWieldMode: false);
    }

    private void UpdateMeleeChase(CwslMonsterHealth monsterHealth)
    {
        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.Tank)
        {
            if (!CwslPlayerShieldBashVisual.IsInStrikeRange(transform, monsterHealth.NetworkObject))
            {
                movement?.RequestMoveTo(monsterHealth.transform.position);
                return;
            }

            movement?.StopMovement();
            AttackSelectedTarget(dualWieldMode: false);
            return;
        }

        var distance = Vector3.Distance(transform.position, monsterHealth.transform.position);
        var range = GetAttackRange(chaseAttackTarget);
        if (distance > range)
        {
            movement?.RequestMoveTo(monsterHealth.transform.position);
            return;
        }

        movement?.StopMovement();
        AttackSelectedTarget(dualWieldMode: false);
    }

    private void UpdateAttackMove()
    {
        if (!attackMoveActive)
            return;

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.MissileTank)
        {
            UpdateMissileTankAttackMove();
            return;
        }

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.CrowdGatherer)
        {
            UpdateGathererAttackMove();
            return;
        }

        AttackSelectedTarget(dualWieldMode: false);

        if (HasReachedAttackMoveDestination())
            attackMoveActive = false;
    }

    private void UpdateMissileTankAttackMove()
    {
        if (missileTankSkill == null)
            return;

        if (missileTankSkill.HasEnemyInRange())
        {
            missileTankSkill.TryFireAttackServer(dualWieldMode: false);

            if (movement != null && movement.IsMoving)
                return;
        }

        movement?.RequestMoveTo(attackMoveDestination);
    }

    private void UpdateGathererAttackMove()
    {
        if (gathererMissileAttack == null)
            return;

        if (gathererMissileAttack.TryFireServer())
        {
            if (movement != null && movement.IsMoving)
                return;
        }

        movement?.RequestMoveTo(attackMoveDestination);
    }

    public void BeginAttackMoveServer(Vector3 destination)
    {
        if (!IsServer)
            return;

        pureMoveMode = false;
        attackMoveActive = true;
        attackMoveDestination = destination;
        chaseAttackTarget = null;
        missileTankSkill?.ClearAttackFocus();
        selection?.SetTargetServer(null);
        movement?.RequestMoveTo(destination);
    }

    public void AttackTargetServer(NetworkObject target, bool dualWieldMode = false)
    {
        if (!IsServer || target == null)
            return;

        attackMoveActive = false;
        pureMoveMode = false;
        selection?.SetTargetServer(target);

        if (TryGetLivingEnemyBase(target, out var enemyBase))
        {
            if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.MissileTank)
            {
                var range = missileTankSkill != null ? missileTankSkill.AttackRange : 24f;
                var distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance > range)
                {
                    chaseAttackTarget = target;
                    movement?.RequestMoveTo(target.transform.position);
                    return;
                }

                chaseAttackTarget = null;
                movement?.StopMovement();
                missileTankSkill?.TryFireAttackServer(dualWieldMode);
                return;
            }

            if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.CrowdGatherer)
            {
                var range = gathererMissileAttack != null
                    ? gathererMissileAttack.AttackRange
                    : CwslGameConstants.GathererMissileRange;
                var distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance > range)
                {
                    chaseAttackTarget = target;
                    movement?.RequestMoveTo(target.transform.position);
                    return;
                }

                chaseAttackTarget = null;
                movement?.StopMovement();
                gathererMissileAttack?.TryFireServer();
                return;
            }

            var structureRange = GetAttackRange(target);
            var structureDistance = Vector3.Distance(transform.position, target.transform.position);
            if (structureDistance > structureRange)
            {
                chaseAttackTarget = target;
                movement?.RequestMoveTo(target.transform.position);
                return;
            }

            chaseAttackTarget = null;
            movement?.StopMovement();
            AttackSelectedTarget(dualWieldMode);
            return;
        }

        var monsterHealth = target.GetComponent<CwslMonsterHealth>();
        if (monsterHealth == null || !monsterHealth.IsAlive)
            return;

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.MissileTank)
        {
            var range = missileTankSkill != null ? missileTankSkill.AttackRange : 24f;
            var distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > range)
            {
                chaseAttackTarget = target;
                movement?.RequestMoveTo(target.transform.position);
                return;
            }

            chaseAttackTarget = null;
            movement?.StopMovement();
            missileTankSkill?.TryFireAttackServer(dualWieldMode);
            return;
        }

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.CrowdGatherer)
        {
            var range = gathererMissileAttack != null
                ? gathererMissileAttack.AttackRange
                : CwslGameConstants.GathererMissileRange;
            var distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > range)
            {
                chaseAttackTarget = target;
                movement?.RequestMoveTo(target.transform.position);
                return;
            }

            chaseAttackTarget = null;
            movement?.StopMovement();
            gathererMissileAttack?.TryFireServer();
            return;
        }

        var meleeRange = GetAttackRange(target);
        var meleeDistance = Vector3.Distance(transform.position, target.transform.position);
        if (meleeDistance > meleeRange)
        {
            chaseAttackTarget = target;
            movement?.RequestMoveTo(target.transform.position);
            return;
        }

        chaseAttackTarget = null;
        movement?.StopMovement();
        AttackSelectedTarget(dualWieldMode);
    }

    public void AttackSelectedTarget(bool dualWieldMode = false)
    {
        if (!IsServer)
            return;

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.MissileTank)
        {
            missileTankSkill?.TryFireAttackServer(dualWieldMode);
            return;
        }

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.CrowdGatherer)
        {
            gathererMissileAttack?.TryFireServer();
            return;
        }

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.Healer)
        {
            GetComponent<CwslHealerMissileAttack>()?.TryFireServer();
            return;
        }

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.Barricade)
        {
            GetComponent<CwslBarricadeMeleeAttack>()?.TryAttackServer();
            return;
        }

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.Tank)
        {
            TryPerformTankShieldAttack();
            return;
        }

        if (Time.time < nextAttackTime)
            return;

        if (!TryResolveMeleeTarget(out var monsterHealth, out var enemyBase, out var targetObject))
            return;

        var distance = Vector3.Distance(transform.position, targetObject.transform.position);
        var range = GetAttackRange(targetObject);
        if (distance > range)
        {
            if (attackMoveActive)
                return;

            movement?.RequestMoveTo(targetObject.transform.position);
            return;
        }

        nextAttackTime = Time.time + ResolveAttackCooldown();
        PlayAttackClientRpc();

        var attackPower = playerCharacter != null
            ? CwslCharacterStatCatalog.GetAttackPower(playerCharacter.CharacterId)
            : CwslGameConstants.AttackDamage;

        if (monsterHealth != null)
            monsterHealth.DamageFromPlayer(OwnerClientId, attackPower);
        else if (enemyBase != null)
            enemyBase.DamageFromPlayer(OwnerClientId, attackPower);
    }

    private void TryPerformTankShieldAttack()
    {
        if (tankShieldAttack != null && tankShieldAttack.IsAttacking)
            return;

        if (!TryResolveMeleeTarget(out var monsterHealth, out var enemyBase, out var targetObject))
            return;

        if (!CwslPlayerShieldBashVisual.IsInStrikeRange(transform, targetObject))
        {
            if (attackMoveActive)
                return;

            movement?.RequestMoveTo(targetObject.transform.position);
            return;
        }

        movement?.StopMovement();
        tankShieldAttack?.TryPerformAttackServer(
            targetObject,
            monsterHealth,
            enemyBase,
            CwslPlayerShieldBashVisual.GetAttackRange(targetObject));
    }

    private float ResolveAttackCooldown()
    {
        var characterId = playerCharacter != null
            ? playerCharacter.CharacterId
            : CwslCharacterId.Tank;
        return CwslCharacterStatCatalog.GetAttackCooldown(characterId);
    }

    private bool TryResolveMeleeTarget(
        out CwslMonsterHealth monsterHealth,
        out CwslEnemyBase enemyBase,
        out NetworkObject targetObject)
    {
        monsterHealth = null;
        enemyBase = null;
        targetObject = null;

        if (selection != null &&
            selection.TryGetSelectedTarget(out var selected) &&
            selected != null)
        {
            var selectedHealth = selected.GetComponent<CwslMonsterHealth>();
            if (selectedHealth != null && selectedHealth.IsAlive)
            {
                monsterHealth = selectedHealth;
                targetObject = selected;
                return true;
            }

            if (TryGetLivingEnemyBase(selected, out var selectedBase))
            {
                enemyBase = selectedBase;
                targetObject = selected;
                return true;
            }
        }

        if (!attackMoveActive)
            return false;

        var bestDistance = float.MaxValue;
        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var distance = Vector3.Distance(transform.position, monster.transform.position);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            monsterHealth = monster;
            targetObject = monster.NetworkObject;
            enemyBase = null;
        }

        return monsterHealth != null && targetObject != null;
    }

    private static bool TryGetLivingEnemyBase(NetworkObject networkObject, out CwslEnemyBase enemyBase)
    {
        enemyBase = networkObject != null ? networkObject.GetComponent<CwslEnemyBase>() : null;
        return enemyBase != null && enemyBase.IsAlive;
    }

    private float GetAttackRange(NetworkObject target)
    {
        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.Tank)
            return CwslPlayerShieldBashVisual.GetAttackRange(target);

        var scale = target != null ? target.transform.localScale.x : 1f;
        return CwslGameConstants.AttackRange + scale;
    }

    private bool HasReachedAttackMoveDestination()
    {
        var flat = transform.position - attackMoveDestination;
        flat.y = 0f;
        return flat.sqrMagnitude <= 0.35f * 0.35f;
    }

    [ClientRpc]
    private void PlayAttackClientRpc()
    {
        playerController?.PlayAttackPulse();
    }
}
