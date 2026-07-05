using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum CwslMidBossBuffKind : byte
{
    AttackUp = 0,
    DefenseUp = 1,
    SpeedUp = 2
}

/// <summary>중간보스 — 크기·체력↑, 주변 아군 버프.</summary>
public class CwslDefenseMidBoss : CwslMeleeMonster
{
    private const float BuffInterval = 2.5f;
    private const float BuffDuration = 4f;
    private const float AttackBuffMultiplier = 1.35f;
    private const float DefenseBuffMultiplier = 0.72f;
    private const float SpeedBuffMultiplier = 1.25f;

    private CwslMidBossBuffKind buffKind = CwslMidBossBuffKind.AttackUp;
    private float buffTimer;

    public void ConfigureBuff(CwslMidBossBuffKind kind)
    {
        buffKind = kind;
    }

    protected override void TickServerAI()
    {
        base.TickServerAI();

        buffTimer -= Time.deltaTime;
        if (buffTimer > 0f)
            return;

        buffTimer = BuffInterval;
        ApplyAuraBuffServer();
    }

    private void ApplyAuraBuffServer()
    {
        var manager = CwslMonsterManager.Instance;
        var radius = manager != null ? manager.MidBossBuffRadius : 7f;
        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive || monster.IsBoss)
                continue;

            var flat = monster.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > radius * radius)
                continue;

            var profile = monster.GetComponent<CwslMonsterRuntimeStats>();
            if (profile == null)
                profile = monster.gameObject.AddComponent<CwslMonsterRuntimeStats>();

            switch (buffKind)
            {
                case CwslMidBossBuffKind.AttackUp:
                    profile.SetTimedDamageMultiplier(AttackBuffMultiplier, BuffDuration);
                    break;
                case CwslMidBossBuffKind.DefenseUp:
                    profile.SetTimedDefenseMultiplier(DefenseBuffMultiplier, BuffDuration);
                    break;
                case CwslMidBossBuffKind.SpeedUp:
                    profile.SetTimedSpeedMultiplier(SpeedBuffMultiplier, BuffDuration);
                    break;
            }
        }

        PlayBuffClientRpc((int)buffKind);
    }

    [ClientRpc]
    private void PlayBuffClientRpc(int kind)
    {
        var color = ((CwslMidBossBuffKind)kind) switch
        {
            CwslMidBossBuffKind.AttackUp => new Color(1f, 0.35f, 0.2f),
            CwslMidBossBuffKind.DefenseUp => new Color(0.35f, 0.65f, 1f),
            _ => new Color(0.45f, 1f, 0.55f)
        };
        CwslSimpleVfx.SpawnBurst(transform.position + Vector3.up, color, 2.2f, 0.45f);
    }
}
