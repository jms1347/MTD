using Unity.Netcode;
using UnityEngine;

/// <summary>역할 고유 RMB 스킬 — 유비/관우/장비.</summary>
public class StllRoleSkills : NetworkBehaviour
{
    private const float SkillCooldown = 12f;

    private StllBrotherhoodRoleState roleState;
    private StllGlaiveAim aim;
    private StllPlayerHealth health;
    private StllPlayerCardInventory cards;
    private float nextSkillTime;

    private void Awake()
    {
        roleState = GetComponent<StllBrotherhoodRoleState>();
        aim = GetComponent<StllGlaiveAim>();
        health = GetComponent<StllPlayerHealth>();
        cards = GetComponent<StllPlayerCardInventory>();
    }

    public bool TryCastRoleSkillServer()
    {
        if (!IsServer || roleState == null)
            return false;

        var cdMult = 1f - (cards?.GetPassiveBonus(StllPassiveBonusType.CooldownReduction) ?? 0f);
        if (Time.time < nextSkillTime)
            return false;

        nextSkillTime = Time.time + SkillCooldown * cdMult;

        return roleState.Role switch
        {
            StllBrotherhoodRole.LiuBei => CastLiuBeiArmyCamp(),
            StllBrotherhoodRole.GuanYu => CastGuanYuCleave(),
            StllBrotherhoodRole.ZhangFei => CastZhangFeiTaunt(),
            _ => false
        };
    }

    private bool CastLiuBeiArmyCamp()
    {
        var allies = FindObjectsByType<StllPlayerHealth>(FindObjectsSortMode.None);
        for (var i = 0; i < allies.Length; i++)
        {
            var ally = allies[i];
            if (ally == null || !ally.IsAlive)
                continue;

            var flat = ally.transform.position - transform.position;
            flat.y = 0f;
            if (flat.magnitude > 8f)
                continue;

            ally.HealPercentServer(0.08f);
        }

        PlaySkillVfxClientRpc(new Color(0.2f, 0.5f, 0.95f));
        return true;
    }

    private bool CastGuanYuCleave()
    {
        var origin = transform.position + Vector3.up * 0.9f;
        var forward = aim != null ? aim.AimDirection : transform.forward;
        var damage = 55f * GetDamageMultiplier();

        var enemies = FindObjectsByType<StllEnemyHealth>(FindObjectsSortMode.None);
        for (var i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            if (!IsInFan(origin, forward, enemy.transform.position, 60f, 5f))
                continue;

            enemy.TakeDamageServer(damage, OwnerClientId, forward * 2.5f);
        }

        var boss = FindFirstObjectByType<StllBossLuBu>();
        if (boss != null && boss.IsAlive && IsInFan(origin, forward, boss.transform.position, 60f, 5f))
            boss.DamageServer(damage);

        PlaySkillVfxClientRpc(new Color(0.1f, 0.7f, 0.2f));
        return true;
    }

    private bool CastZhangFeiTaunt()
    {
        var enemies = FindObjectsByType<StllEnemyGruntAI>(FindObjectsSortMode.None);
        for (var i = 0; i < enemies.Length; i++)
            enemies[i].ForceTauntTargetServer(transform, 3f);

        cards?.SetIronWallActiveServer(true);
        Invoke(nameof(ClearIronWall), 2f);
        PlaySkillVfxClientRpc(new Color(0.85f, 0.15f, 0.1f));
        return true;
    }

    private void ClearIronWall()
    {
        if (IsServer)
            cards?.SetIronWallActiveServer(false);
    }

    private float GetDamageMultiplier()
    {
        var loadout = GetComponent<StllPlayerLoadout>();
        var mult = loadout != null ? loadout.GetWeaponDamageMultiplier() : 1f;
        mult *= 1f + (cards?.GetPassiveBonus(StllPassiveBonusType.AttackDamage) ?? 0f);
        if (StllTeamGold.Instance != null)
            mult *= 1f + StllTeamGold.Instance.GetTeamAttackBonus();
        return mult * StllRoleCombatModifiers.GetAttackDamageMultiplier(roleState.Role);
    }

    private static bool IsInFan(Vector3 origin, Vector3 forward, Vector3 target, float halfAngleDeg, float range)
    {
        var toTarget = target - origin;
        toTarget.y = 0f;
        if (toTarget.magnitude > range || toTarget.magnitude < 0.05f)
            return false;

        return Vector3.Angle(forward, toTarget) <= halfAngleDeg;
    }

    [ClientRpc]
    private void PlaySkillVfxClientRpc(Color color)
    {
        StllVisualUtil.CreatePrimitive(PrimitiveType.Sphere, transform, Vector3.up * 1.2f,
            Vector3.one * 2.5f, color);
    }
}
