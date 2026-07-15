using UnityEngine;

public readonly struct StllRoleVisualProfile
{
    public readonly StllBrotherhoodRole Role;
    public readonly Color AccentColor;
    public readonly Color BannerColor;
    public readonly StllWeaponKind WeaponKind;
    public readonly string RoleTag;

    public StllRoleVisualProfile(
        StllBrotherhoodRole role,
        Color accentColor,
        Color bannerColor,
        StllWeaponKind weaponKind,
        string roleTag)
    {
        Role = role;
        AccentColor = accentColor;
        BannerColor = bannerColor;
        WeaponKind = weaponKind;
        RoleTag = roleTag;
    }
}

public static class StllRoleVisualCatalog
{
    public static StllRoleVisualProfile Get(StllBrotherhoodRole role)
    {
        return role switch
        {
            StllBrotherhoodRole.LiuBei => new StllRoleVisualProfile(
                StllBrotherhoodRole.LiuBei,
                new Color(0.22f, 0.48f, 0.92f),
                new Color(0.15f, 0.32f, 0.72f),
                StllWeaponKind.TwinSwords,
                "유비"),
            StllBrotherhoodRole.GuanYu => new StllRoleVisualProfile(
                StllBrotherhoodRole.GuanYu,
                new Color(0.12f, 0.62f, 0.28f),
                new Color(0.08f, 0.42f, 0.18f),
                StllWeaponKind.Glaive,
                "관우"),
            StllBrotherhoodRole.ZhangFei => new StllRoleVisualProfile(
                StllBrotherhoodRole.ZhangFei,
                new Color(0.82f, 0.18f, 0.14f),
                new Color(0.58f, 0.1f, 0.08f),
                StllWeaponKind.SpearMace,
                "장비"),
            _ => new StllRoleVisualProfile(
                StllBrotherhoodRole.None,
                new Color(0.55f, 0.55f, 0.55f),
                new Color(0.35f, 0.35f, 0.35f),
                StllWeaponKind.Glaive,
                "?")
        };
    }
}
