/// <summary>Q 방패 강화 시 탱커 스킬 반경·효과 배율.</summary>
public static class CwslTankSkillEmpower
{
    public static bool IsEmpowered(CwslTankFortifySkill fortifySkill) =>
        fortifySkill != null && fortifySkill.IsShieldActive;

    public static float GetRadiusMultiplier(bool empowered) =>
        empowered ? CwslGameConstants.TankSkillEmpowerRadiusMultiplier : 1f;

    public static float GetPowerMultiplier(bool empowered) =>
        empowered ? CwslGameConstants.TankSkillEmpowerPowerMultiplier : 1f;
}
