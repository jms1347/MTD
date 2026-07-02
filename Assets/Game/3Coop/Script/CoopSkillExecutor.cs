using UnityEngine;

public static class CoopSkillExecutor
{
    public static bool TryCast(
        CoopGameSession session,
        CoopPlayerState player,
        Vector3 groundPoint,
        out string message)
    {
        message = string.Empty;
        if (session == null || player == null || string.IsNullOrEmpty(player.skillId))
        {
            message = "스킬이 없습니다.";
            return false;
        }

        if (player.towerHp <= 0f)
        {
            message = "탱크가 파괴되어 스킬을 사용할 수 없습니다.";
            return false;
        }

        if (player.skillCooldown > 0f)
        {
            message = $"스킬 쿨다운 {Mathf.CeilToInt(player.skillCooldown)}초";
            return false;
        }

        if (!CoopSkillCatalog.TryGet(player.skillId, out var definition))
        {
            message = "알 수 없는 스킬입니다.";
            return false;
        }

        if (definition.RequiresGroundTarget)
            groundPoint = CoopMapSpawnUtility.SnapToWalkableWorld(groundPoint);

        var origin = new Vector3(player.towerX, 0.12f, player.towerZ);
        var skillDamage = Mathf.Max(10f, player.attack * 2.5f);
        var pen = Mathf.Max(1, player.penetration);

        switch (player.skillId)
        {
            case CoopSkillCatalog.Lightning:
                CastLightning(session, player.playerId, groundPoint, skillDamage, pen);
                break;
            case CoopSkillCatalog.Blizzard:
                CastBlizzard(session, player.playerId, groundPoint, skillDamage * 0.75f, pen);
                break;
            case CoopSkillCatalog.Meteor:
                CastMeteor(session, player.playerId, groundPoint, skillDamage, pen);
                break;
            case CoopSkillCatalog.MoveBoost:
                CoopGimmickBuffs.SetMoveBoost(player.playerId, 3f, CoopSkillCatalog.BuffDurationSeconds);
                break;
            case CoopSkillCatalog.AttackSpeedBoost:
                CoopSkillBuffs.SetAttackSpeedBoost(player.playerId, 1f / 3f, CoopSkillCatalog.BuffDurationSeconds);
                break;
            case CoopSkillCatalog.AttackPowerBoost:
                CoopSkillBuffs.SetAttackPowerBoost(player.playerId, 3f, CoopSkillCatalog.BuffDurationSeconds);
                break;
            case CoopSkillCatalog.PoisonBees:
                CoopPoisonBee.SpawnSwarm(session, player.playerId, origin, skillDamage * 0.9f, pen, 3);
                break;
            default:
                message = "지원하지 않는 스킬입니다.";
                return false;
        }

        player.skillCooldown = CoopSkillCatalog.CooldownSeconds;
        message = $"{definition.DisplayName} 사용!";
        return true;
    }

    private static void CastLightning(CoopGameSession session, string playerId, Vector3 point, float damage, int pen)
    {
        CoopSkillZone.Spawn(
            session,
            playerId,
            point,
            CoopSkillZoneMode.Lightning,
            damage,
            pen,
            CoopSkillCatalog.GroundFieldDuration,
            CoopSkillCatalog.GroundFieldRadius,
            0.85f);
    }

    private static void CastBlizzard(CoopGameSession session, string playerId, Vector3 point, float damage, int pen)
    {
        CoopSkillZone.Spawn(
            session,
            playerId,
            point,
            CoopSkillZoneMode.Blizzard,
            damage,
            pen,
            CoopSkillCatalog.GroundFieldDuration,
            CoopSkillCatalog.GroundFieldRadius,
            1f);
    }

    private static void CastMeteor(CoopGameSession session, string playerId, Vector3 point, float damage, int pen)
    {
        CoopSkillZone.Spawn(
            session,
            playerId,
            point,
            CoopSkillZoneMode.Meteor,
            damage,
            pen,
            CoopSkillCatalog.GroundFieldDuration,
            CoopSkillCatalog.GroundFieldRadius,
            0.9f);
    }
}
