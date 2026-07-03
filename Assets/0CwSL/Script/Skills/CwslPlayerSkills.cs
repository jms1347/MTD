using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CwslPlayerSkills : NetworkBehaviour
{
    private readonly List<CwslPlayerSkillBase> skills = new();
    private CwslPlayerSkillBase chargedSkill;
    private CwslPlayerCharacter playerCharacter;

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

        foreach (var skill in skills)
        {
            if (!IsSkillActiveForCharacter(skill))
                continue;

            if (skill.ActivationType != CwslSkillActivationType.GroundTarget)
                continue;

            if (!TrySpendSkillCost())
                return;

            skill.OnSkillGroundTargetServer(senderClientId, worldPoint);
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
}
