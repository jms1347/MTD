using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum CwslDefenseBossSkillKind : byte
{
    GroundSlam = 0,
    MissileBarrage = 1
}

/// <summary>?? ?? ?? ??1~2????? ???, ??? ?????????</summary>
public class CwslDefenseBoss : CwslMonsterBase
{
    private const float MoveSpeedBase = CwslMonsterStatCatalog.DefenseBossMoveSpeed;
    private const float SlamRadius = 6f;
    private const float SlamCastSeconds = 2.4f;
    private const float SlamCooldown = 9f;
    private const float SlamDamage = CwslMonsterStatCatalog.DefenseBossSlamDamage;
    private const float MissileCastSeconds = 2.1f;
    private const float MissileCooldown = 11f;
    private const float MissileZoneRadius = 3.2f;
    private const int MissileZoneCount = 3;

    private readonly List<CwslDefenseBossSkillKind> skills = new();
    private readonly Dictionary<CwslDefenseBossSkillKind, float> cooldowns = new();
    private bool casting;

    public override void Initialize(CwslMonsterType type)
    {
        base.Initialize(type);
        moveSpeed = MoveSpeedBase;
        skills.Clear();
        cooldowns.Clear();

        var pool = new List<CwslDefenseBossSkillKind>
        {
            CwslDefenseBossSkillKind.GroundSlam,
            CwslDefenseBossSkillKind.MissileBarrage
        };

        var skillCount = Random.Range(1, 3);
        for (var i = 0; i < skillCount; i++)
        {
            var pick = pool[Random.Range(0, pool.Count)];
            if (!skills.Contains(pick))
                skills.Add(pick);
        }
    }

    protected override void TickServerAI()
    {
        if (casting)
            return;

        var nexus = CwslNexus.Instance;
        if (nexus != null && nexus.IsAlive)
            MoveToward(nexus.GetMeleeApproachPoint(transform.position, GetMovementClampRadius()), 0.9f);

        foreach (var skill in skills)
        {
            cooldowns.TryGetValue(skill, out var cd);
            if (cd > 0f)
            {
                cooldowns[skill] = cd - Time.deltaTime;
                continue;
            }

            if (TryBeginSkill(skill))
                return;
        }
    }

    private bool TryBeginSkill(CwslDefenseBossSkillKind skill)
    {
        casting = true;
        switch (skill)
        {
            case CwslDefenseBossSkillKind.GroundSlam:
                StartCoroutine(CastGroundSlam());
                return true;
            case CwslDefenseBossSkillKind.MissileBarrage:
                StartCoroutine(CastMissileBarrage());
                return true;
            default:
                casting = false;
                return false;
        }
    }

    private IEnumerator CastGroundSlam()
    {
        var center = transform.position;
        center.y = 0f;
        ShowTelegraphClientRpc(center, SlamRadius, SlamCastSeconds, 0);

        yield return new WaitForSeconds(SlamCastSeconds);

        var damage = GetScaledDamage(SlamDamage);
        DamagePlayersInRadius(center, SlamRadius, damage);
        DamageNexusIfInRadius(center, SlamRadius, damage * 1.4f);
        PlaySlamClientRpc(center, SlamRadius);
        cooldowns[CwslDefenseBossSkillKind.GroundSlam] = SlamCooldown;
        casting = false;
    }

    private IEnumerator CastMissileBarrage()
    {
        var zones = new List<Vector3>();
        var nexus = CwslNexus.Instance;
        var anchor = nexus != null ? nexus.transform.position : Vector3.zero;

        for (var i = 0; i < MissileZoneCount; i++)
        {
            var offset = Random.insideUnitCircle * 8f;
            zones.Add(new Vector3(anchor.x + offset.x, 0f, anchor.z + offset.y));
        }

        for (var i = 0; i < zones.Count; i++)
            ShowTelegraphClientRpc(zones[i], MissileZoneRadius, MissileCastSeconds, 1);

        yield return new WaitForSeconds(MissileCastSeconds);

        var damage = GetScaledDamage(CwslMonsterStatCatalog.DefenseBossMissileDamage);
        foreach (var zone in zones)
        {
            DamagePlayersInRadius(zone, MissileZoneRadius, damage);
            DamageNexusIfInRadius(zone, MissileZoneRadius, damage * 0.85f);
            PlayMissileImpactClientRpc(zone);
        }

        cooldowns[CwslDefenseBossSkillKind.MissileBarrage] = MissileCooldown;
        casting = false;
    }

    private static void DamagePlayersInRadius(Vector3 center, float radius, float damage)
    {
        var players = CwslCombatRegistry.AlivePlayers;
        foreach (var player in players)
        {
            if (player == null || !player.IsAlive)
                continue;

            var flat = player.transform.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radius * radius)
                continue;

            player.TryReceiveExplosionHitServer(damage, player.transform.position + Vector3.up);
        }
    }

    private static void DamageNexusIfInRadius(Vector3 center, float radius, float damage)
    {
        var nexus = CwslNexus.Instance;
        if (nexus == null || !nexus.IsAlive)
            return;

        var flat = nexus.transform.position - center;
        flat.y = 0f;
        if (flat.sqrMagnitude <= radius * radius)
            nexus.DamageServer(damage);
    }

    [ClientRpc]
    private void ShowTelegraphClientRpc(Vector3 center, float radius, float duration, int skillKind)
    {
        var label = skillKind == 0 ? "\uC9C0\uBA74 \uAC15\uD0C0" : "\uBBF8\uC0AC\uC77C";
        CwslSkillTelegraph.ShowCircle(center, radius, duration, label);
    }

    [ClientRpc]
    private void PlaySlamClientRpc(Vector3 center, float radius)
    {
        CwslSimpleVfx.SpawnBurst(center + Vector3.up * 0.2f, new Color(1f, 0.4f, 0.1f), radius * 0.55f, 0.5f);
    }

    [ClientRpc]
    private void PlayMissileImpactClientRpc(Vector3 center)
    {
        CwslSimpleVfx.SpawnBurst(center + Vector3.up * 0.3f, new Color(1f, 0.55f, 0.15f), 1.8f, 0.35f);
    }
}
