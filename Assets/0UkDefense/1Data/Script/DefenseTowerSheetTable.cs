using UnityEngine;

/// <summary>
/// 건설 타입 ↔ 구글 시트 Tower_ID 매핑.
/// </summary>
public static class DefenseTowerSheetTable
{
    public const int MachineGunTowerId = 1001;
    public const int FlameMortarTowerId = 1002;
    public const int AutoLaserTowerId = 1003;

    public static int GetTowerId(DefenseBuildType buildType)
    {
        return buildType switch
        {
            DefenseBuildType.StandardTower => MachineGunTowerId,
            DefenseBuildType.MeteorTower => FlameMortarTowerId,
            DefenseBuildType.ChainLightningTower => AutoLaserTowerId,
            _ => 0
        };
    }

    public static bool TryGetData(DefenseBuildType buildType, out TowerData data)
    {
        data = null;
        var towerId = GetTowerId(buildType);
        if (towerId <= 0)
            return false;

        if (DataManager.Instance == null)
            return false;

        return DataManager.Instance.TryGetTower(towerId, out data);
    }

    public static bool TryGetData(int towerId, out TowerData data)
    {
        data = null;
        if (DataManager.Instance == null)
            return false;

        return DataManager.Instance.TryGetTower(towerId, out data);
    }

    public static bool TryGetAttackRange(int towerId, out float attackRange)
    {
        attackRange = 0f;
        if (!TryGetData(towerId, out var data) || data == null)
            return false;

        attackRange = DefenseSkillPresentationCatalog.ResolveAttackRangeForTower(data);
        return attackRange > 0.05f;
    }
}
