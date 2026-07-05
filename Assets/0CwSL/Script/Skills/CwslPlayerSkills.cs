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
    private CwslPlayerCharacter playerCharacter;
    private float nextMeteorTime;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        skills.Clear();
        skills.AddRange(GetComponents<CwslPlayerSkillBase>());
        foreach (var skill in skills)
        {
            if (skill.ActivationType == CwslSkillActivationType.Charged)
                chargedSkill = skill;
        }
    }

    private void Update()
    {
        if (!IsServer || chargedSkill == null)
            return;

        if (!IsSkillActiveForCharacter(chargedSkill))
            return;

        chargedSkill.TickChargedServer();
    }

    public void PressSkillServer(ulong senderClientId)
    {
        if (!IsServer)
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
        if (!IsServer)
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
        if (!IsServer)
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
                continue;

            if (!TrySpendSkillCost())
                return;

            skill.OnSkillGroundTargetServer(senderClientId, worldPoint);
        }
    }

    private void TryCastMeteor(ulong senderClientId, Vector3 worldPoint)
    {
        if (Time.time < nextMeteorTime)
            return;

        if (!TrySpendSkillCost())
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
            return;

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

    private bool TrySpendSkillCost()
    {
        var gold = GetComponent<CwslPlayerGold>();
        if (gold == null)
            return true;

        return gold.TrySpendGoldServer(CwslGameConstants.SkillGoldCost);
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
