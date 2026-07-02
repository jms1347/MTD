using System;

/// <summary>
/// 전투 페이즈 중 플레이어 이동·건설·자원 채취 허용 여부.
/// DefenseSceneSetup 인스펙터에서 조정할 수 있습니다.
/// </summary>
[Serializable]
public class DefensePlayerBattleRules
{
    public bool allowMoveDuringBattle = true;
    public bool allowBuildDuringBattle = true;
    public bool allowGatherDuringBattle = true;
    public bool lockFarmBoundaryDuringBattle = false;
}
