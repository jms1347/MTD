using Unity.Netcode;
using UnityEngine;

/// <summary>바리케이드 근접 평타 — 가까운 몬스터만 자동/수동 타격.</summary>
public class CwslBarricadeMeleeAttack : NetworkBehaviour
{
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerCombat combat;
    private CwslPlayerSelection selection;
    private CwslPlayerMovement movement;
    private float nextAttackTime;

    public float AttackRange => CwslGameConstants.BarricadeMeleeRange;

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
            playerCharacter.CharacterId != CwslCharacterId.Barricade)
            return;

        if (combat != null && combat.IsPureMoveMode)
            return;

        if (combat != null && (combat.IsAttackMoveActive || combat.HasChaseTarget))
            TryAttackServer();
        else if (ShouldAutoAttackNearest())
            TryAttackServer();
    }

    public bool TryAttackServer()
    {
        if (!IsServer ||
            playerCharacter == null ||
            playerCharacter.CharacterId != CwslCharacterId.Barricade)
            return false;

        if (Time.time < nextAttackTime)
            return false;

        if (!TryResolveMonster(out var monster))
            return false;

        var distance = Vector3.Distance(transform.position, monster.transform.position);
        if (distance > AttackRange)
        {
            movement?.RequestMoveTo(monster.transform.position);
            return false;
        }

        movement?.StopMovement();
        FaceToward(monster.transform.position);
        var damage = CwslCharacterStatCatalog.GetAttackPower(CwslCharacterId.Barricade);
        monster.DamageFromPlayer(OwnerClientId, damage);
        nextAttackTime = Time.time + CwslGameConstants.BarricadeMeleeCooldown
                         / Mathf.Max(0.25f, GetComponent<CwslAttackSpeedBuff>()?.AttackSpeedMultiplier ?? 1f);
        PlayAttackClientRpc(monster.transform.position);
        return true;
    }

    private bool ShouldAutoAttackNearest()
    {
        return TryFindNearestMonster(AttackRange * 0.95f, out _);
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

        return TryFindNearestMonster(AttackRange * 1.5f, out monster);
    }

    private bool TryFindNearestMonster(float maxDistance, out CwslMonsterHealth monster)
    {
        monster = null;
        var best = maxDistance;
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

    private void FaceToward(Vector3 point)
    {
        var flat = point - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            return;
        transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
    }

    [ClientRpc]
    private void PlayAttackClientRpc(Vector3 targetPosition)
    {
        GetComponent<CwslPlayerController>()?.PlayAttackPulse();
        CwslVfxSpawner.SpawnMeleeHit(
            Vector3.Lerp(transform.position, targetPosition, 0.55f) + Vector3.up * 1f,
            transform.rotation);
    }
}
