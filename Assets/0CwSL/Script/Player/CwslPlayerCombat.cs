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
        if (!IsServer || !attackMoveActive)
            return;

        // 이동 중에도 사거리 안 적을 계속 공격
        AttackSelectedTarget(fanMode: false);

        if (HasReachedAttackMoveDestination())
            attackMoveActive = false;
    }

    public void BeginAttackMoveServer(Vector3 destination)
    {
        if (!IsServer)
            return;

        attackMoveActive = true;
        attackMoveDestination = destination;
        selection?.SetTargetServer(null);
        movement?.RequestMoveTo(destination);
    }

    public void AttackTargetServer(NetworkObject target, bool fanMode = false)
    {
        if (!IsServer || target == null)
            return;

        attackMoveActive = false;
        selection?.SetTargetServer(target);

        var monsterHealth = target.GetComponent<CwslMonsterHealth>();
        if (monsterHealth == null || !monsterHealth.IsAlive)
            return;

        // 사거리 밖이면 접근
        var distance = Vector3.Distance(transform.position, target.transform.position);
        var range = GetAttackRange(target);
        if (distance > range)
            movement?.RequestMoveTo(target.transform.position);
        else
            movement?.StopMovement();

        AttackSelectedTarget(fanMode);
    }

    public void AttackSelectedTarget(bool fanMode = false)
    {
        if (!IsServer)
            return;

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.MissileTank)
        {
            missileTankSkill?.TryFireAttackServer(fanMode);
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

        // 어택땅: 가장 가까운 적
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
