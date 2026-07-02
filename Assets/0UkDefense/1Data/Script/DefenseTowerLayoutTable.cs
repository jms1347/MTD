/// <summary>
/// 레이아웃 타워 이름 / 건설 타입 → 구글 시트 Tower_ID.
/// </summary>
public static class DefenseTowerLayoutTable
{
    public static int ResolveSheetId(TowerSpawnData data)
    {
        if (data == null)
            return 0;

        if (data.towerSheetId > 0)
            return data.towerSheetId;

        if (!string.IsNullOrWhiteSpace(data.towerName))
        {
            return data.towerName switch
            {
                "Tower_North" => 1001,
                "Tower_East" => 1201,
                "Tower_South" => 1301,
                "Tower_West" => 1401,
                "Tower_Meteor" => 1102,
                "Tower_ChainLightning" => 1401,
                _ => 0
            };
        }

        return 0;
    }
}
