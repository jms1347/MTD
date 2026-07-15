/// <summary>도원결의 EA 3역할.</summary>
public enum StllBrotherhoodRole : byte
{
    None = 0,
    LiuBei = 1,
    GuanYu = 2,
    ZhangFei = 3
}

public static class StllBrotherhoodRoleUtil
{
    public static string GetDisplayName(StllBrotherhoodRole role)
    {
        return role switch
        {
            StllBrotherhoodRole.LiuBei => "유비 (첫째)",
            StllBrotherhoodRole.GuanYu => "관우 (둘째)",
            StllBrotherhoodRole.ZhangFei => "장비 (셋째)",
            _ => "미배정"
        };
    }

    public static StllBrotherhoodRole FromJoinOrder(int order)
    {
        return order switch
        {
            0 => StllBrotherhoodRole.LiuBei,
            1 => StllBrotherhoodRole.GuanYu,
            2 => StllBrotherhoodRole.ZhangFei,
            _ => StllBrotherhoodRole.None
        };
    }
}
