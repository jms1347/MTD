public static class StllRoleCombatModifiers
{
    public static float GetAttackDamageMultiplier(StllBrotherhoodRole role)
    {
        return role switch
        {
            StllBrotherhoodRole.GuanYu => 1.18f,
            StllBrotherhoodRole.ZhangFei => 0.95f,
            _ => 1f
        };
    }

    public static float GetMoveSpeedMultiplier(StllBrotherhoodRole role)
    {
        return role switch
        {
            StllBrotherhoodRole.LiuBei => 1.05f,
            _ => 1f
        };
    }

    public static float GetAllyMoveSpeedAuraBonus(StllBrotherhoodRole role)
    {
        return role == StllBrotherhoodRole.LiuBei ? 0.12f : 0f;
    }
}
