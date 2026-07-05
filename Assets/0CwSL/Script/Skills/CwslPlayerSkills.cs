using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CwslPlayerSkills : NetworkBehaviour
{
    private const float MeteorDamage = 1f;
    private const float MeteorRadius = 4.8f;
    private const float MeteorFallDuration = 0.55f;
    private const float MeteorCooldown = 0.85f;

    private readonly List<CwslPlayerSkillBase> skills = new();
    private CwslPlayerSkillBase chargedSkill;
    private CwslCrowdGatherSkill crowdGatherSkill;
    private CwslPlayerCharacter playerCharacter;
    private float nextMeteorTime;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        skills.Clear();
        skills.AddRange(GetComponents<CwslPlayerSkillBase>());
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

    public void CastGroundSkillServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!IsServer || BlocksSkillUseServer(senderClientId))
            return;

        // 빨간 마법사 메테오 (프리팹에 스킬 컴포넌트가 없어도 동작)
        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.RedMage)
        {
            TryCastMeteor(senderClientId, worldPoint);
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

            skill.OnSkillGroundTargetServer(senderClientId, worldPoint);
        }
    }

    private void TryCastMeteor(ulong senderClientId, Vector3 worldPoint)
    {
        if (Time.time < nextMeteorTime)
            return;

        if (IsGoldInsufficientForSkill(CwslGameConstants.MeteorGoldCost))
        {
            NotifyGoldInsufficientServer();
            return;
        }

        if (!TrySpendSkillCost(CwslGameConstants.MeteorGoldCost))
            return;

        nextMeteorTime = Time.time + MeteorCooldown;
        var impactPoint = worldPoint;
        impactPoint.y = 0f;

        PlayMeteorFxClientRpc(impactPoint);
        PlayMageCastClientRpc();
        StartCoroutine(ApplyMeteorDamage(impactPoint, senderClientId));
    }

    private IEnumerator ApplyMeteorDamage(Vector3 impactPoint, ulong attackerClientId)
    {
        yield return new WaitForSeconds(MeteorFallDuration);

        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        var radiusSqr = MeteorRadius * MeteorRadius;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position - impactPoint;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSqr)
                continue;

            monster.DamageFromPlayer(attackerClientId, MeteorDamage);
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

    private bool IsSkillActiveForCharacter(CwslPlayerSkillBase skill)
    {
        var characterId = playerCharacter != null
            ? playerCharacter.CharacterId
            : CwslCharacterId.Tank;
        return skill.IsActiveForCharacter(characterId);
    }

    private bool BlocksSkillUseServer(ulong senderClientId)
    {
        return senderClientId == OwnerClientId && CwslBossWatchState.BlocksSkills(senderClientId);
    }

    private bool TrySpendSkillCost(int baseCost = -1)
    {
        if (CwslArenaGimmickSystem.AreSkillsFreeInFightPhase(transform.position))
            return true;

        var gold = GetComponent<CwslPlayerGold>();
        if (gold == null)
            return true;

        var cost = (baseCost >= 0 ? baseCost : CwslGameConstants.SkillGoldCost)
                   + CwslArenaGimmickSystem.GetExtraSkillGoldCost(transform.position);
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
        var gold = GetComponent<CwslPlayerGold>();
        if (gold == null)
            return false;

        if (CwslArenaGimmickSystem.AreSkillsFreeInFightPhase(transform.position))
            return false;

        var cost = (baseCost >= 0 ? baseCost : CwslGameConstants.SkillGoldCost)
                   + CwslArenaGimmickSystem.GetExtraSkillGoldCost(transform.position);
        return gold.Gold < cost;
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
