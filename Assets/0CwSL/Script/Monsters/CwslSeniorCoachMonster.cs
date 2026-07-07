using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum CwslSeniorCoachSkillKind : byte
{
    IronPotShield = 0,
    AceSpotlight = 1
}

/// <summary>중보스 수석 코치(이임생) — 맵 외곽 순회, 광란 오라 + 순환 스킬.</summary>
public class CwslSeniorCoachMonster : CwslMonsterBase
{
    private const float AuraRefreshSeconds = 0.35f;
    private const float AuraBuffDuration = 0.75f;

    private float orbitDistance;
    private float auraTimer;
    private float ironShieldCooldown;
    private float aceSpotlightCooldown;
    private CwslSeniorCoachSkillKind nextSkill = CwslSeniorCoachSkillKind.IronPotShield;

    public override void Initialize(CwslMonsterType type)
    {
        base.Initialize(type);
        moveSpeed = CwslMonsterStatCatalog.SeniorCoachMoveSpeed;
        targetingMode = CwslMonsterTargetingMode.Nearest;
        orbitDistance = Random.Range(0f, CwslArenaUtility.GetMapEdgePerimeter(CwslGameConstants.SeniorCoachOrbitInset) * 0.5f);
    }

    protected override void TickServerAI()
    {
        orbitDistance += CwslGameConstants.SeniorCoachOrbitSpeed * Time.deltaTime;
        var orbitTarget = CwslArenaUtility.GetMapEdgeOrbitPosition(
            orbitDistance,
            CwslGameConstants.SeniorCoachOrbitInset);
        MoveToward(orbitTarget, 1.1f);

        auraTimer -= Time.deltaTime;
        if (auraTimer <= 0f)
        {
            auraTimer = AuraRefreshSeconds;
            ApplyFrenzyAuraServer();
        }

        ironShieldCooldown = Mathf.Max(0f, ironShieldCooldown - Time.deltaTime);
        aceSpotlightCooldown = Mathf.Max(0f, aceSpotlightCooldown - Time.deltaTime);

        TryCastRotatingSkill();
    }

    private void ApplyFrenzyAuraServer()
    {
        var radius = CwslGameConstants.SeniorCoachFrenzyAuraRadius;
        var radiusSq = radius * radius;
        var center = transform.position;
        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);

        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive || monster.IsBoss)
                continue;

            if (monster.GetComponent<CwslSeniorCoachMonster>() != null)
                continue;

            var flat = monster.transform.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSq)
                continue;

            var profile = monster.GetComponent<CwslMonsterRuntimeStats>();
            if (profile == null)
                profile = monster.gameObject.AddComponent<CwslMonsterRuntimeStats>();

            profile.SetTimedSpeedMultiplier(
                CwslGameConstants.SeniorCoachFrenzySpeedMultiplier,
                AuraBuffDuration);
        }

        PlayAuraPulseClientRpc();
    }

    private void TryCastRotatingSkill()
    {
        if (nextSkill == CwslSeniorCoachSkillKind.IronPotShield)
        {
            if (ironShieldCooldown <= 0f && TryCastIronPotShield())
            {
                ironShieldCooldown = CwslGameConstants.SeniorCoachIronShieldInterval;
                nextSkill = CwslSeniorCoachSkillKind.AceSpotlight;
                return;
            }

            if (aceSpotlightCooldown <= 0f && TryCastAceSpotlight())
            {
                aceSpotlightCooldown = CwslGameConstants.SeniorCoachAceSpotlightCooldown;
                nextSkill = CwslSeniorCoachSkillKind.IronPotShield;
                return;
            }

            nextSkill = CwslSeniorCoachSkillKind.AceSpotlight;
            return;
        }

        if (aceSpotlightCooldown <= 0f && TryCastAceSpotlight())
        {
            aceSpotlightCooldown = CwslGameConstants.SeniorCoachAceSpotlightCooldown;
            nextSkill = CwslSeniorCoachSkillKind.IronPotShield;
            return;
        }

        if (ironShieldCooldown <= 0f && TryCastIronPotShield())
        {
            ironShieldCooldown = CwslGameConstants.SeniorCoachIronShieldInterval;
            nextSkill = CwslSeniorCoachSkillKind.AceSpotlight;
            return;
        }

        nextSkill = CwslSeniorCoachSkillKind.IronPotShield;
    }

    private bool TryCastIronPotShield()
    {
        var candidates = new List<CwslMonsterHealth>();
        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (!CwslMonsterTypeUtil.IsSuicideBomber(monster.MonsterType))
                continue;

            candidates.Add(monster);
        }

        if (candidates.Count == 0)
            return false;

        candidates.Sort((a, b) =>
        {
            var da = Vector3.SqrMagnitude(a.transform.position - transform.position);
            var db = Vector3.SqrMagnitude(b.transform.position - transform.position);
            return da.CompareTo(db);
        });

        var granted = 0;
        for (var i = 0; i < candidates.Count && granted < CwslGameConstants.SeniorCoachIronShieldTargetCount; i++)
        {
            var shield = CwslMonsterHitShield.Ensure(candidates[i].gameObject);
            shield?.GrantServer(1);
            granted++;
        }

        if (granted <= 0)
            return false;

        PlayIronShieldClientRpc();
        return true;
    }

    private bool TryCastAceSpotlight()
    {
        if (!CwslTargetQuery.TryGetRichestLivingPlayer(out var target, out _))
            return false;

        CwslMonsterGlobalAggro.SetForcedTargetServer(
            target.OwnerClientId,
            CwslGameConstants.SeniorCoachAceSpotlightDuration);

        PlayAceSpotlightClientRpc(target.OwnerClientId);
        return true;
    }

    [ClientRpc]
    private void PlayAuraPulseClientRpc()
    {
        CwslSimpleVfx.SpawnBurst(
            transform.position + Vector3.up * 0.2f,
            new Color(1f, 0.72f, 0.18f, 0.85f),
            CwslGameConstants.SeniorCoachFrenzyAuraRadius * 0.22f,
            0.28f);
    }

    [ClientRpc]
    private void PlayIronShieldClientRpc()
    {
        CwslSimpleVfx.SpawnBurst(
            transform.position + Vector3.up * 1.1f,
            new Color(0.72f, 0.78f, 0.86f, 0.95f),
            1.8f,
            0.42f);
    }

    [ClientRpc]
    private void PlayAceSpotlightClientRpc(ulong targetClientId)
    {
        var target = ResolvePlayerTransform(targetClientId);
        if (target != null)
            CwslAceSpotlightVisual.Play(target, CwslGameConstants.SeniorCoachAceSpotlightDuration);
    }

    private static Transform ResolvePlayerTransform(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
            return null;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != clientId || client.PlayerObject == null)
                continue;

            return client.PlayerObject.transform;
        }

        return null;
    }
}
