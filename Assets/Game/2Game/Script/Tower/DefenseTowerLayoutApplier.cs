using UnityEngine;

/// <summary>
/// Tower Layout 전투 프리팹 참조 — 시트·어드레서블 + Catalog 특수 타워 프리팹.
/// </summary>
public static class DefenseTowerLayoutApplier
{
    public static void ApplyCombatReferences(TowerSpawnData[] towers, DefenseCombatCatalog catalog)
    {
        if (towers == null)
            return;

        foreach (var tower in towers)
            ApplyCombatReferences(tower, catalog);
    }

    public static void ApplyCombatReferences(TowerSpawnData tower, DefenseCombatCatalog catalog)
    {
        if (tower == null)
            return;

        int sheetId = tower.towerSheetId > 0
            ? tower.towerSheetId
            : DefenseTowerLayoutTable.ResolveSheetId(tower);

        if (sheetId > 0 &&
            DataManager.Instance != null &&
            DataManager.Instance.TryGetTower(sheetId, out var towerData))
        {
            var inferredKind = InferTowerKind(towerData);
            if (inferredKind != TowerKind.Standard)
                tower.kind = inferredKind;
        }

        switch (tower.kind)
        {
            case TowerKind.Meteor:
                if (catalog != null)
                {
                    tower.meteorProjectilePrefab = catalog.meteorMissilePrefab;
                    tower.meteorExplosionPrefab = catalog.meteorExplosionPrefab;
                }
                break;

            case TowerKind.ChainLightning:
                if (catalog != null)
                {
                    tower.chainBoltPrefab = catalog.chainBoltPrefab;
                    tower.chainHitExplosionPrefab = catalog.chainHitExplosionPrefab;
                    tower.stunHeadEffectPrefab = catalog.stunHeadEffectPrefab;
                    tower.stunBodyEffectPrefab = catalog.stunBodyEffectPrefab;
                }
                break;

            case TowerKind.Summon:
                break;

            default:
                ApplyStandardMissileFromSheet(tower, sheetId);
                break;
        }
    }

    private static void ApplyStandardMissileFromSheet(TowerSpawnData tower, int sheetId)
    {
        if (sheetId > 0 &&
            DataManager.Instance != null &&
            DataManager.Instance.TryGetTower(sheetId, out var towerData) &&
            towerData.skillId > 0 &&
            DataManager.Instance.TryGetSkill(towerData.skillId, out var skill))
        {
            var missilePrefab = DefenseSkillCombatTable.GetMissilePrefabForSkill(skill);
            if (missilePrefab != null)
            {
                tower.missilePrefab = missilePrefab;
                return;
            }
        }

        tower.standardMissileId = DefenseTowerCombatTable.GetStandardMissileForTower(tower.towerName);
        tower.missilePrefab = DefenseMissileResolver.GetPrefab(tower.standardMissileId);
    }

    private static TowerKind InferTowerKind(TowerData tower)
    {
        if (tower == null)
            return TowerKind.Standard;

        var name = (tower.towerName + " " + tower.description).ToLowerInvariant();
        if (name.Contains("메테오") || name.Contains("meteor") || name.Contains("박격"))
            return TowerKind.Meteor;
        if (name.Contains("체인") || name.Contains("chain"))
            return TowerKind.ChainLightning;
        if (name.Contains("소환") || name.Contains("summon"))
            return TowerKind.Summon;

        return TowerKind.Standard;
    }
}
