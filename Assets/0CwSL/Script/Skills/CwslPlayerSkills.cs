using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CwslPlayerSkills : NetworkBehaviour
{
    private const float MeteorRadius = 4.8f;
    private const float MeteorFallDuration = 0.55f;

    private readonly List<CwslPlayerSkillBase> skills = new();
    private CwslPlayerSkillBase chargedSkill;
    private CwslCrowdGatherSkill crowdGatherSkill;
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerStamina playerStamina;
    private CwslPlayerSkillCooldowns skillCooldowns;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        playerStamina = GetComponent<CwslPlayerStamina>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        if (GetComponent<CwslPlayerBossDebuff>() == null)
            gameObject.AddComponent<CwslPlayerBossDebuff>();
        skills.Clear();
        skills.AddRange(GetComponents<CwslPlayerSkillBase>());
        EnsureTankDashSkill();
        EnsureTankSlamSkill();
        EnsureTankWhirlwindSkill();
        EnsureRedMageFrozenOrbSkill();
        EnsureRammerExtraSkills();
        EnsureGathererExtraSkills();
        EnsureBarricadeExtraSkills();
        EnsureHealerExtraSkills();
        RefreshSkillList();
        crowdGatherSkill = GetComponent<CwslCrowdGatherSkill>();
        foreach (var skill in skills)
        {
            if (skill.ActivationType == CwslSkillActivationType.Charged)
                chargedSkill = skill;
        }
    }

    private void Update()
    {
        if (!IsServer)
            return;

        foreach (var skill in skills)
        {
            if (skill.ActivationType != CwslSkillActivationType.Charged)
                continue;
            if (!IsSkillActiveForCharacter(skill))
                continue;

            skill.TickChargedServer();
        }
    }

    public void BeginGatherSkillServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!IsServer || crowdGatherSkill == null || BlocksSkillUseServer(senderClientId))
            return;

        if (!IsSkillActiveForCharacter(crowdGatherSkill))
            return;

        crowdGatherSkill.BeginChargeServer(worldPoint);
    }

    public void UpdateGatherSkillServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!IsServer || crowdGatherSkill == null || BlocksSkillUseServer(senderClientId))
            return;

        if (!IsSkillActiveForCharacter(crowdGatherSkill))
            return;

        crowdGatherSkill.UpdateChargeCenterServer(worldPoint);
    }

    public void PressSkillServer(ulong senderClientId)
    {
        if (!IsServer || BlocksSkillUseServer(senderClientId))
            return;

        if (!TrySpendStaminaForChargedPress())
            return;

        foreach (var skill in skills)
        {
            if (!IsSkillActiveForCharacter(skill))
                continue;

            if (skill.ActivationType == CwslSkillActivationType.Charged)
                skill.OnSkillPressedServer(senderClientId);
            else if (skill.ActivationType == CwslSkillActivationType.Instant)
                TryCastInstant(skill, senderClientId);
        }
    }

    public void ReleaseSkillServer(ulong senderClientId)
    {
        if (!IsServer || BlocksSkillUseServer(senderClientId))
            return;

        foreach (var skill in skills)
        {
            if (!IsSkillActiveForCharacter(skill))
                continue;

            if (skill.ActivationType == CwslSkillActivationType.Charged)
                skill.OnSkillReleasedServer(senderClientId);
        }
    }

    public void UseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint = default)
    {
        if (!IsServer || BlocksSkillUseServer(senderClientId))
            return;

        if (slotIndex <= 0 || slotIndex >= CwslCharacterSkillCatalog.SkillCount)
            return;

        if (skillCooldowns != null && !skillCooldowns.IsReady(slotIndex))
            return;

        var characterId = playerCharacter != null
            ? playerCharacter.CharacterId
            : CwslCharacterId.Tank;
        var definition = CwslCharacterSkillCatalog.Get(characterId, slotIndex);
        var hasSlotHandler = false;

        foreach (var skill in skills)
        {
            if (!IsSkillActiveForCharacter(skill) || skill.SkillSlotIndex != slotIndex)
                continue;

            hasSlotHandler = true;
            if (!skill.CanUseSkillSlotServer(senderClientId, slotIndex, worldPoint))
                return;
        }

        if (definition.Implemented && !hasSlotHandler)
            return;

        if (!TrySpendStaminaForSlot(slotIndex))
            return;

        foreach (var skill in skills)
        {
            if (!IsSkillActiveForCharacter(skill))
                continue;

            if (skill.TryUseSkillSlotServer(senderClientId, slotIndex, worldPoint))
                return;
        }

        if (definition.Implemented)
            return;

        NotifyPlaceholderSkillClientRpc(definition.DisplayName);
    }

    public bool TrySpendStaminaForSlot(int slotIndex)
    {
        if (!CwslGameConstants.SkillsUseStamina)
            return true;

        var characterId = playerCharacter != null
            ? playerCharacter.CharacterId
            : CwslCharacterId.Tank;
        var cost = CwslCharacterSkillCatalog.GetStaminaCost(characterId, slotIndex);
        if (playerStamina == null)
            playerStamina = GetComponent<CwslPlayerStamina>();

        if (playerStamina != null && playerStamina.TrySpendServer(cost))
            return true;

        NotifyStaminaInsufficientServer();
        return false;
    }

    private bool TrySpendStaminaForChargedPress()
    {
        var characterId = playerCharacter != null
            ? playerCharacter.CharacterId
            : CwslCharacterId.Tank;

        // 홀드 Q는 각 스킬에서 시작·유지 SP 처리
        if (characterId == CwslCharacterId.Tank ||
            characterId == CwslCharacterId.MomentumRammer)
            return true;

        return TrySpendStaminaForSlot(0);
    }

    public void CastGroundSkillServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!IsServer || BlocksSkillUseServer(senderClientId))
            return;

        // 빨간 마법사 Q(메테오) / 힐러 Q(힐 장판)
        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.RedMage)
        {
            TryCastMeteor(senderClientId, worldPoint);
            return;
        }

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.Healer)
        {
            if (skillCooldowns != null && !skillCooldowns.IsReady(0))
                return;

            var canCast = false;
            foreach (var skill in skills)
            {
                if (!IsSkillActiveForCharacter(skill) || skill.SkillSlotIndex != 0)
                    continue;

                if (!skill.CanUseSkillSlotServer(senderClientId, 0, worldPoint))
                    return;

                canCast = true;
                break;
            }

            if (!canCast)
                return;

            if (!TrySpendStaminaForSlot(0))
                return;

            foreach (var skill in skills)
            {
                if (!IsSkillActiveForCharacter(skill) || skill.SkillSlotIndex != 0)
                    continue;

                if (skill.TryUseSkillSlotServer(senderClientId, 0, worldPoint))
                    return;
            }

            return;
        }

        foreach (var skill in skills)
        {
            if (!IsSkillActiveForCharacter(skill))
                continue;

            if (skill.ActivationType != CwslSkillActivationType.GroundTarget)
                continue;

            if (!skill.CanCastServer(senderClientId))
            {
                if (IsGoldInsufficientForSkill())
                    NotifyGoldInsufficientServer();
                return;
            }

            if (!TrySpendSkillCost())
                return;

            if (!TrySpendStaminaForSlot(0))
                return;

            skill.OnSkillGroundTargetServer(senderClientId, worldPoint);
        }
    }

    private void TryCastMeteor(ulong senderClientId, Vector3 worldPoint)
    {
        if (skillCooldowns != null && !skillCooldowns.IsReady(0))
            return;

        if (!TrySpendStaminaForSlot(0))
            return;

        if (IsGoldInsufficientForSkill(CwslGameConstants.MeteorGoldCost))
        {
            NotifyGoldInsufficientServer();
            return;
        }

        if (!TrySpendSkillCost(CwslGameConstants.MeteorGoldCost))
            return;

        skillCooldowns?.BeginCooldown(0);
        var impactPoint = worldPoint;
        impactPoint.y = 0f;

        PlayMeteorFxClientRpc(impactPoint);
        PlayMageCastClientRpc();
        StartCoroutine(ApplyMeteorDamage(impactPoint, senderClientId));
    }

    private IEnumerator ApplyMeteorDamage(Vector3 impactPoint, ulong attackerClientId)
    {
        yield return new WaitForSeconds(MeteorFallDuration);

        var monsters = CwslCombatRegistry.AliveMonsters;
        var radiusSqr = MeteorRadius * MeteorRadius;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position - impactPoint;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSqr)
                continue;

            monster.DamageFromPlayer(
                attackerClientId,
                CwslCombatMath.ResolveSkillDamageForClient(attackerClientId, CwslGameConstants.MeteorSkillCoeff));
            CwslMonsterStatusController.Ensure(monster)?.ApplyBurnServer(
                attackerClientId,
                CwslGameConstants.MonsterBurnDuration,
                CwslCombatMath.ResolveSkillDamageForClient(
                    attackerClientId,
                    CwslGameConstants.MonsterBurnTotalSkillCoeff));
        }
    }

    private void TryCastInstant(CwslPlayerSkillBase skill, ulong senderClientId)
    {
        if (!skill.CanCastServer(senderClientId))
        {
            if (IsGoldInsufficientForSkill())
                NotifyGoldInsufficientServer();
            return;
        }

        if (!TrySpendSkillCost())
            return;

        skill.OnSkillPressedServer(senderClientId);
    }

    public void NotifyStaminaInsufficientServer()
    {
        if (!IsServer)
            return;

        NotifyStaminaInsufficientClientRpc();
    }

    public bool BlocksSkillUseForOwner() => BlocksSkillUseServer(OwnerClientId);

    private bool IsSkillActiveForCharacter(CwslPlayerSkillBase skill)
    {
        var characterId = playerCharacter != null
            ? playerCharacter.CharacterId
            : CwslCharacterId.Tank;
        return skill.IsActiveForCharacter(characterId);
    }

    private void EnsureTankDashSkill()
    {
        if (GetComponent<CwslTankShieldDashSkill>() != null)
            return;

        var dash = gameObject.AddComponent<CwslTankShieldDashSkill>();
        skills.Add(dash);
    }

    private void EnsureTankSlamSkill()
    {
        if (GetComponent<CwslTankShieldSlamSkill>() != null)
            return;

        var slam = gameObject.AddComponent<CwslTankShieldSlamSkill>();
        skills.Add(slam);
    }

    private void EnsureTankWhirlwindSkill()
    {
        if (GetComponent<CwslTankShieldWhirlwindSkill>() != null)
            return;

        var whirl = gameObject.AddComponent<CwslTankShieldWhirlwindSkill>();
        skills.Add(whirl);
    }

    private void EnsureRedMageFrozenOrbSkill()
    {
        if (GetComponent<CwslRedMageFrozenOrbSkill>() != null)
            return;

        var frozenOrb = gameObject.AddComponent<CwslRedMageFrozenOrbSkill>();
        skills.Add(frozenOrb);
    }

    private void EnsureRammerExtraSkills()
    {
        EnsureSkillComponent<CwslRammerBrakeSkill>();
        EnsureSkillComponent<CwslRammerRopeSkill>();
        EnsureSkillComponent<CwslRammerFireTrailSkill>();
    }

    private void EnsureGathererExtraSkills()
    {
        EnsureSkillComponent<CwslGathererYankSkill>();
        EnsureSkillComponent<CwslGathererSwapSkill>();
        EnsureSkillComponent<CwslGathererBlackHoleSkill>();
        if (GetComponent<CwslGathererMissileAttack>() == null)
            gameObject.AddComponent<CwslGathererMissileAttack>();
    }

    private void EnsureBarricadeExtraSkills()
    {
        EnsureSkillComponent<CwslBarricadeWallSkill>();
        EnsureSkillComponent<CwslBarricadeJumpPadSkill>();
        EnsureSkillComponent<CwslBarricadeRepairSkill>();
        EnsureSkillComponent<CwslBarricadeDetonateSkill>();
        if (GetComponent<CwslBarricadeMeleeAttack>() == null)
            gameObject.AddComponent<CwslBarricadeMeleeAttack>();
    }

    private void EnsureHealerExtraSkills()
    {
        EnsureSkillComponent<CwslHealerHealPadSkill>();
        EnsureSkillComponent<CwslHealerPoisonPadSkill>();
        EnsureSkillComponent<CwslHealerBurstHealSkill>();
        EnsureSkillComponent<CwslHealerHasteBuffSkill>();
        if (GetComponent<CwslHealerMissileAttack>() == null)
            gameObject.AddComponent<CwslHealerMissileAttack>();
    }

    private void EnsureSkillComponent<T>() where T : CwslPlayerSkillBase
    {
        if (GetComponent<T>() != null)
            return;

        skills.Add(gameObject.AddComponent<T>());
    }

    private void RefreshSkillList()
    {
        skills.Clear();
        skills.AddRange(GetComponents<CwslPlayerSkillBase>());
    }

    private bool BlocksSkillUseServer(ulong senderClientId)
    {
        return senderClientId == OwnerClientId && CwslBossWatchState.BlocksSkills(senderClientId);
    }

    private bool TrySpendSkillCost(int baseCost = -1)
    {
        if (!CwslGameConstants.SkillsConsumeGold)
            return true;

        if (CwslArenaGimmickSystem.AreSkillsFreeInFightPhase(transform.position))
            return true;

        var gold = GetComponent<CwslPlayerGold>();
        if (gold == null)
            return true;

        var cost = (baseCost >= 0 ? baseCost : CwslGameConstants.SkillGoldCost)
                   + CwslArenaGimmickSystem.GetExtraSkillGoldCost(transform.position);

        var pillBuff = GetComponent<CwslPlayerPillBuff>();
        if (pillBuff != null && pillBuff.TrySpendSkillGold(gold, cost))
            return true;

        if (gold.TrySpendGoldServer(cost))
            return true;

        NotifyGoldInsufficientServer();
        return false;
    }

    public void NotifyGoldInsufficientServer()
    {
        if (!IsServer)
            return;

        NotifySkillGoldInsufficientClientRpc();
    }

    private bool IsGoldInsufficientForSkill(int baseCost = -1)
    {
        if (!CwslGameConstants.SkillsConsumeGold)
            return false;

        var gold = GetComponent<CwslPlayerGold>();
        if (gold == null)
            return false;

        if (CwslArenaGimmickSystem.AreSkillsFreeInFightPhase(transform.position))
            return false;

        var pillBuff = GetComponent<CwslPlayerPillBuff>();
        if (pillBuff != null && pillBuff.HasFreeSkillBuff)
            return false;

        var cost = (baseCost >= 0 ? baseCost : CwslGameConstants.SkillGoldCost)
                   + CwslArenaGimmickSystem.GetExtraSkillGoldCost(transform.position);
        return gold.Gold < cost;
    }

    [ClientRpc]
    private void NotifyStaminaInsufficientClientRpc()
    {
        if (!IsOwner)
            return;

        CwslSkillGoldFeedback.ShowInsufficientStamina();
    }

    [ClientRpc]
    private void NotifyPlaceholderSkillClientRpc(string skillName)
    {
        if (!IsOwner)
            return;

        CwslSkillGoldFeedback.ShowMessage($"{skillName} (\uC900\uBE44 \uC911)");
    }

    [ClientRpc]
    private void NotifySkillGoldInsufficientClientRpc()
    {
        if (!IsOwner)
            return;

        CwslSkillGoldFeedback.ShowInsufficientGold();
    }

    [ClientRpc]
    public void PlayMeteorFxClientRpc(Vector3 impactPoint)
    {
        if (IsOwner)
            GetComponent<CwslPlayerVision>()?.RevealMeteorScry(impactPoint);

        var runner = new GameObject("CwslMeteorEffect");
        runner.transform.position = impactPoint;
        runner.AddComponent<CwslMeteorEffectRunner>().Play(
            impactPoint,
            18f,
            MeteorFallDuration,
            3.2f,
            MeteorRadius);
    }

    [ClientRpc]
    private void PlayMageCastClientRpc()
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerStaffCastVisual>()?.PlayCast();
    }
}
