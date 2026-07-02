using UnityEngine;

/// <summary>
/// 로그라이크 마법 카드 — 지면 조준 후 표식 미사일 + 약 5초간 장판/낙뢰/유성 연출.
/// </summary>
public static class DefenseRoguelikeSkillCaster
{
    private const float DefaultFieldRadius = 4.5f;

    public static bool TryCastAtGround(string skillCode, Vector3 groundPoint, float baseDamage)
    {
        if (string.IsNullOrWhiteSpace(skillCode) || DataManager.Instance == null)
            return false;

        if (!DataManager.Instance.TryGetSkillByCode(skillCode.Trim(), out var skill) || skill == null)
        {
            Debug.LogWarning($"[DefenseRoguelikeSkillCaster] 스킬 코드를 찾을 수 없습니다: {skillCode}");
            return false;
        }

        CastAtGround(skill, groundPoint, baseDamage);
        return true;
    }

    public static void CastAtGround(DefenseSkillData skill, Vector3 groundPoint, float baseDamage)
    {
        if (skill == null)
            return;

        groundPoint = DefenseBallisticUtility.ProjectToGround(groundPoint);
        var tower = BuildRoguelikeTowerContext(baseDamage, skill);
        string mobility = DefenseTargetMobilityUtility.GroundLabel;

        if (TryCastKnownGroundField(skill, groundPoint, tower, mobility))
            return;

        if (skill.HasSummonPrefab)
        {
            SpawnAnchorMissile(skill, groundPoint, DefenseSkillCombatTable.RoguelikeGroundFieldDuration);
            LinkedSkillSpawner.TrySpawn(LinkedSkillSpawnContext.Create(
                skill,
                groundPoint,
                null,
                tower,
                mobility,
                0));
            return;
        }

        if (DefenseSkillCombatTable.IsStormMissileAnchorSkill(skill))
        {
            CastStormField(skill, groundPoint, tower, mobility);
            return;
        }

        if (IsMeteorStrikeSkill(skill))
        {
            CastMeteorField(skill, groundPoint, tower, mobility);
            return;
        }

        Debug.LogWarning($"[DefenseRoguelikeSkillCaster] 지원하지 않는 마법 스킬: {skill.skillCode}");
    }

    public static float ResolvePreviewRadius(RoguelikeCardData card)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.skillCode))
            return DefaultFieldRadius;

        if (DataManager.Instance == null
            || !DataManager.Instance.TryGetSkillByCode(card.skillCode.Trim(), out var skill)
            || skill == null)
        {
            return DefaultFieldRadius;
        }

        if (skill.splashRadius > 0.05f)
            return skill.splashRadius;

        return DefaultFieldRadius;
    }

    public static Color ResolvePreviewColor(RoguelikeCardData card)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.skillCode))
            return DefenseStrikeWarningZone.StormStrikeZoneColor;

        if (DataManager.Instance != null
            && DataManager.Instance.TryGetSkillByCode(card.skillCode.Trim(), out var skill)
            && skill != null)
        {
            if (skill.element == DefenseSkillElement.Ice)
                return DefenseStrikeWarningZone.BlizzardZoneColor;

            if (skill.element == DefenseSkillElement.Fire)
                return new Color(1f, 0.2f, 0.08f, 0.42f);
        }

        return DefenseStrikeWarningZone.StormStrikeZoneColor;
    }

    private static bool TryCastKnownGroundField(
        DefenseSkillData skill,
        Vector3 groundPoint,
        DefenseTowerCombatContext tower,
        string mobility)
    {
        if (!DefenseSkillCombatTable.IsRoguelikeGroundFieldSkill(skill))
            return false;

        var code = skill.skillCode.Trim();
        if (string.Equals(code, DefenseSkillCombatTable.RoguelikeBlizzardSkillCode, System.StringComparison.OrdinalIgnoreCase))
        {
            CastBlizzardField(skill, groundPoint, tower, mobility);
            return true;
        }

        if (string.Equals(code, DefenseSkillCombatTable.RoguelikeStormSkillCode, System.StringComparison.OrdinalIgnoreCase))
        {
            CastStormField(skill, groundPoint, tower, mobility);
            return true;
        }

        if (string.Equals(code, DefenseSkillCombatTable.RoguelikeMeteorSkillCode, System.StringComparison.OrdinalIgnoreCase))
        {
            CastMeteorField(skill, groundPoint, tower, mobility);
            return true;
        }

        return false;
    }

    private static void CastBlizzardField(
        DefenseSkillData skill,
        Vector3 groundPoint,
        DefenseTowerCombatContext tower,
        string mobility)
    {
        float duration = DefenseSkillCombatTable.RoguelikeGroundFieldDuration;
        SpawnAnchorMissile(skill, groundPoint, duration);

        var context = LinkedSkillSpawnContext.Create(skill, groundPoint, null, tower, mobility, 0);
        if (!LinkedSkillSpawner.TrySpawn(context))
        {
            Debug.LogWarning("[DefenseRoguelikeSkillCaster] 눈보라 장판 스폰 실패 — Zone_BlizzardSnow 확인");
        }
    }

    private static void CastStormField(
        DefenseSkillData skill,
        Vector3 groundPoint,
        DefenseTowerCombatContext tower,
        string mobility)
    {
        float duration = DefenseSkillCombatTable.RoguelikeGroundFieldDuration;
        var context = LinkedSkillSpawnContext.Create(skill, groundPoint, null, tower, mobility, 0);
        Vector3 skyPos = DefenseStormCloud.ResolveCloudPositionAtDetonation(groundPoint);

        var anchorObject = SpawnAnchorMissile(skill, groundPoint, duration);
        if (anchorObject == null)
        {
            anchorObject = new GameObject($"RoguelikeStorm_{skill.skillCode}");
            anchorObject.transform.position = skyPos;
        }
        else
        {
            anchorObject.transform.position = skyPos;
        }

        var anchor = anchorObject.GetComponent<DefenseStormMissileAnchor>();
        if (anchor == null)
            anchor = anchorObject.AddComponent<DefenseStormMissileAnchor>();

        anchor.Begin(context, null);
    }

    private static void CastMeteorField(
        DefenseSkillData skill,
        Vector3 groundPoint,
        DefenseTowerCombatContext tower,
        string mobility)
    {
        float duration = DefenseSkillCombatTable.RoguelikeGroundFieldDuration;
        float radius = ResolveFieldRadius(skill);
        SpawnAnchorMissile(skill, groundPoint, duration);
        DefenseStrikeWarningZone.CreateSustained(
            groundPoint,
            radius,
            new Color(1f, 0.15f, 0.05f, 0.42f));

        DefenseRoguelikeMeteorBarrage.Begin(groundPoint, radius, skill, tower, mobility, duration);
    }

    private static DefenseTowerCombatContext BuildRoguelikeTowerContext(float baseDamage, DefenseSkillData skill)
    {
        float radius = ResolveFieldRadius(skill);
        float duration = DefenseSkillCombatTable.RoguelikeGroundFieldDuration;
        return new DefenseTowerCombatContext
        {
            baseDamage = Mathf.Max(1f, baseDamage),
            fireInterval = duration / LinkedSkillSpawnContext.SummonLifetimeFromTowerFireIntervalRatio,
            attackRange = radius / LinkedSkillSpawnContext.SummonRadiusFromTowerRangeRatio,
            missileSpeed = 35f
        };
    }

    private static float ResolveFieldRadius(DefenseSkillData skill)
    {
        if (skill != null && skill.splashRadius > 0.05f)
            return skill.splashRadius;

        return DefaultFieldRadius;
    }

    private static GameObject SpawnAnchorMissile(DefenseSkillData skill, Vector3 groundPoint, float lifetime)
    {
        var prefab = DefenseSkillCombatTable.GetMissilePrefabForSkill(skill);
        if (prefab == null)
            return null;

        Vector3 position = groundPoint;
        if (DefenseSkillCombatTable.IsStormMissileAnchorSkill(skill))
            position = DefenseStormCloud.ResolveCloudPositionAtDetonation(groundPoint);
        else if (string.Equals(
                     skill.skillCode,
                     DefenseSkillCombatTable.RoguelikeBlizzardSkillCode,
                     System.StringComparison.OrdinalIgnoreCase))
            position = groundPoint + Vector3.up * Mathf.Clamp(ResolveFieldRadius(skill) * 0.55f, 3f, 6f);
        else if (string.Equals(
                     skill.skillCode,
                     DefenseSkillCombatTable.RoguelikeMeteorSkillCode,
                     System.StringComparison.OrdinalIgnoreCase))
            position = groundPoint + Vector3.up * 6f;

        var instance = Object.Instantiate(prefab, position, Quaternion.identity);
        DefenseCombatVfxSpawn.DisablePhysicsAndMissileScripts(instance);
        Object.Destroy(instance, lifetime + 0.25f);
        return instance;
    }

    private static bool IsMeteorStrikeSkill(DefenseSkillData skill)
    {
        if (skill == null)
            return false;

        if (skill.element == DefenseSkillElement.Fire
            && skill.moveType == DefenseMoveType.Parabola
            && skill.splashRadius > 0.5f)
        {
            return true;
        }

        return string.Equals(
            skill.skillCode,
            DefenseSkillCombatTable.MeteorStrikeSkillCode,
            System.StringComparison.OrdinalIgnoreCase);
    }
}
