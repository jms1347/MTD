using Unity.Netcode;
using UnityEngine;

public class CwslPlayerCombat : NetworkBehaviour
{
    private float nextAttackTime;
    private CwslPlayerController playerController;
    private CwslPlayerSelection selection;
    private CwslPlayerCharacter playerCharacter;
    private CwslMissileTankSkill missileTankSkill;
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

        var monsterHealth = chaseAttackTarget.GetComponent<CwslMonsterHealth>();
        if (monsterHealth == null || !monsterHealth.IsAlive)
        {
            chaseAttackTarget = null;
            return;
        }

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.MissileTank)
            UpdateMissileTankChase(monsterHealth);
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

    private void UpdateMeleeChase(CwslMonsterHealth monsterHealth)
    {
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

        if (Time.time < nextAttackTime)
            return;

        if (!TryResolveMeleeTarget(out var monsterHealth, out var targetObject))
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

        nextAttackTime = Time.time + CwslGameConstants.AttackCooldown;
        PlayAttackClientRpc();
        monsterHealth.DamageFromPlayer(OwnerClientId, CwslGameConstants.AttackDamage);
    }

    private bool TryResolveMeleeTarget(out CwslMonsterHealth monsterHealth, out NetworkObject targetObject)
    {
        monsterHealth = null;
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
        }

        if (!attackMoveActive)
            return false;

        var bestDistance = float.MaxValue;
        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
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
        }

        return monsterHealth != null && targetObject != null;
    }

    private float GetAttackRange(NetworkObject target)
    {
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
