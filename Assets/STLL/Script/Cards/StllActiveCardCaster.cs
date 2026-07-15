using Unity.Netcode;
using UnityEngine;

/// <summary>카드 액티브 스킬 (키 1·2).</summary>
public class StllActiveCardCaster : NetworkBehaviour
{
    private const float ActiveCooldown = 18f;

    private StllPlayerCardInventory cards;
    private StllGlaiveAim aim;
    private StllPlayerHealth health;
    private StllPlayerLoadout loadout;
    private float nextSlot1Time;
    private float nextSlot2Time;

    private void Awake()
    {
        cards = GetComponent<StllPlayerCardInventory>();
        aim = GetComponent<StllGlaiveAim>();
        health = GetComponent<StllPlayerHealth>();
        loadout = GetComponent<StllPlayerLoadout>();
    }

    public bool TryCastActiveSlotServer(int slotIndex)
    {
        if (!IsServer || cards == null)
            return false;

        var cardId = slotIndex == 0 ? cards.ActiveSlot1 : cards.ActiveSlot2;
        if (cardId == StllCardId.None)
            return false;

        var cdMult = 1f - cards.GetPassiveBonus(StllPassiveBonusType.CooldownReduction);
        if (slotIndex == 0 && Time.time < nextSlot1Time)
            return false;
        if (slotIndex == 1 && Time.time < nextSlot2Time)
            return false;

        if (!CastCardServer(cardId))
            return false;

        var cd = ActiveCooldown * cdMult;
        if (slotIndex == 0)
            nextSlot1Time = Time.time + cd;
        else
            nextSlot2Time = Time.time + cd;

        return true;
    }

    private bool CastCardServer(StllCardId cardId)
    {
        var forward = aim != null ? aim.AimDirection : transform.forward;
        var origin = transform.position;

        switch (cardId)
        {
            case StllCardId.Lightning:
                return CastAoEServer(origin + forward * 6f, 4f, 80f);
            case StllCardId.FireZone:
                StllZoneEffect.SpawnFireZoneServer(origin + forward * 4f, 5f, 4f, 20f, OwnerClientId);
                return true;
            case StllCardId.IceMine:
                StllZoneEffect.SpawnIceMineServer(origin + forward * 3f, OwnerClientId);
                return true;
            case StllCardId.HealBanner:
                HealAlliesInRadiusServer(6f, 0.03f, 5f);
                return true;
            case StllCardId.ChargeHorn:
                return CastDashAttackServer(forward, 60f);
            case StllCardId.CrushingStrike:
                return CastFanAttackServer(origin, forward, 45f, 3f, 120f);
            case StllCardId.IronWall:
                cards.SetIronWallActiveServer(true);
                Invoke(nameof(ClearIronWall), 3f);
                return true;
            case StllCardId.RapidFire:
                return CastRapidFireServer(forward);
            default:
                return false;
        }
    }

    private void ClearIronWall()
    {
        if (IsServer)
            cards?.SetIronWallActiveServer(false);
    }

    private bool CastAoEServer(Vector3 center, float radius, float damage)
    {
        DamageEnemiesInRadiusServer(center, radius, damage);
        var boss = FindFirstObjectByType<StllBossLuBu>();
        if (boss != null && boss.IsAlive && Vector3.Distance(center, boss.transform.position) <= radius)
            boss.DamageServer(damage);
        return true;
    }

    private bool CastFanAttackServer(Vector3 origin, Vector3 forward, float halfAngle, float range, float damage)
    {
        var enemies = FindObjectsByType<StllEnemyHealth>(FindObjectsSortMode.None);
        for (var i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            var to = enemy.transform.position - origin;
            to.y = 0f;
            if (to.magnitude > range || Vector3.Angle(forward, to) > halfAngle)
                continue;

            enemy.TakeDamageServer(damage * GetDamageMult(), OwnerClientId, forward * 3f);
        }

        return true;
    }

    private bool CastDashAttackServer(Vector3 forward, float damage)
    {
        transform.position += forward * 8f;
        DamageEnemiesInRadiusServer(transform.position, 2.5f, damage * GetDamageMult());
        return true;
    }

    private bool CastRapidFireServer(Vector3 forward)
    {
        for (var i = 0; i < 5; i++)
        {
            var offset = Quaternion.Euler(0f, (i - 2) * 8f, 0f) * forward;
            StllQinglongWave.Spawn(transform.position + Vector3.up * 0.9f, offset, 35f * GetDamageMult(), OwnerClientId);
        }

        return true;
    }

    private void DamageEnemiesInRadiusServer(Vector3 center, float radius, float damage)
    {
        var enemies = FindObjectsByType<StllEnemyHealth>(FindObjectsSortMode.None);
        for (var i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            if (Vector3.Distance(center, enemy.transform.position) > radius)
                continue;

            enemy.TakeDamageServer(damage * GetDamageMult(), OwnerClientId, Vector3.up);
        }
    }

    private void HealAlliesInRadiusServer(float radius, float percentPerSecond, float duration)
    {
        var allies = FindObjectsByType<StllPlayerHealth>(FindObjectsSortMode.None);
        for (var i = 0; i < allies.Length; i++)
        {
            var ally = allies[i];
            if (ally == null || !ally.IsAlive)
                continue;

            if (Vector3.Distance(transform.position, ally.transform.position) > radius)
                continue;

            ally.HealPercentServer(percentPerSecond * duration);
        }
    }

    private float GetDamageMult()
    {
        var mult = loadout != null ? loadout.GetWeaponDamageMultiplier() : 1f;
        mult *= 1f + (cards?.GetPassiveBonus(StllPassiveBonusType.AttackDamage) ?? 0f);
        if (StllTeamGold.Instance != null)
            mult *= 1f + StllTeamGold.Instance.GetTeamAttackBonus();
        return mult;
    }
}
