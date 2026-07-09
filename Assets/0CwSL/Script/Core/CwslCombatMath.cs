using UnityEngine;

/// <summary>
/// 통합 전투 수식.
/// 기본: rawDamage = ATK × skillCoeff × buff
/// 방어: final = Max(raw × MinDamagePercent, raw × 100/(100+DEF))
/// </summary>
public static class CwslCombatMath
{
    /// <summary>방어력이 있어도 최소 이 비율만큼은 통과 (8%).</summary>
    public const float MinimumDamagePercent = 0.08f;

    public static float GetDefenseMultiplier(float defenseRating)
    {
        defenseRating = Mathf.Max(0f, defenseRating);
        return 100f / (100f + defenseRating);
    }

    public static float ResolveDamageAfterDefense(float rawDamage, float defenseRating)
    {
        if (rawDamage <= 0f)
            return 0f;

        var scaled = rawDamage * GetDefenseMultiplier(defenseRating);
        return Mathf.Max(rawDamage * MinimumDamagePercent, scaled);
    }

    public static float ResolveBasicAttack(CwslCharacterId characterId) =>
        CwslCharacterStatCatalog.GetAttackPower(characterId);

    public static float ResolveSkillDamage(CwslCharacterId characterId, float skillCoefficient) =>
        CwslCharacterStatCatalog.GetAttackPower(characterId) * Mathf.Max(0f, skillCoefficient);

    public static float ResolveSpeedScaledSkillDamage(
        CwslCharacterId characterId,
        float skillCoefficient,
        float speed,
        float maxSpeed)
    {
        var ratio = Mathf.Clamp01(speed / Mathf.Max(0.01f, maxSpeed));
        return ResolveSkillDamage(characterId, skillCoefficient * ratio);
    }

    public static bool TryGetAttackPower(ulong clientId, out float attackPower, out CwslCharacterId characterId)
    {
        attackPower = CwslGameConstants.PlayerAttackDamage;
        characterId = CwslCharacterId.Tank;

        if (Unity.Netcode.NetworkManager.Singleton == null)
            return false;

        foreach (var client in Unity.Netcode.NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != clientId || client.PlayerObject == null)
                continue;

            var character = client.PlayerObject.GetComponent<CwslPlayerCharacter>();
            if (character == null)
                return false;

            characterId = character.CharacterId;
            attackPower = CwslCharacterStatCatalog.GetAttackPower(characterId);
            return true;
        }

        return false;
    }

    public static float ResolveSkillDamageForClient(ulong clientId, float skillCoefficient)
    {
        if (!TryGetAttackPower(clientId, out var attackPower, out _))
            return CwslGameConstants.PlayerAttackDamage * Mathf.Max(0f, skillCoefficient);

        return attackPower * Mathf.Max(0f, skillCoefficient);
    }
}
