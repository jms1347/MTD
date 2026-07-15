using Unity.Netcode;

public enum StllWeaponTier : byte
{
    Rusty = 0,
    Steel = 1,
    Master = 2
}

public enum StllHorseUpgrade : byte
{
    Normal = 0,
    Fast = 1,
    Heavy = 2
}

/// <summary>무기 진화·말 업그레이드.</summary>
public class StllPlayerLoadout : NetworkBehaviour
{
    private readonly NetworkVariable<byte> weaponTier = new(
        (byte)StllWeaponTier.Rusty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<byte> horseUpgrade = new(
        (byte)StllHorseUpgrade.Normal,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public StllWeaponTier WeaponTier => (StllWeaponTier)weaponTier.Value;
    public StllHorseUpgrade Horse => (StllHorseUpgrade)horseUpgrade.Value;

    public float GetWeaponDamageMultiplier()
    {
        return WeaponTier switch
        {
            StllWeaponTier.Steel => StllEaConstants.WeaponTierMultiplier1,
            StllWeaponTier.Master => StllEaConstants.WeaponTierMultiplier2,
            _ => StllEaConstants.WeaponTierMultiplier0
        };
    }

    public float GetHorseSpeedMultiplier()
    {
        return Horse switch
        {
            StllHorseUpgrade.Fast => 1.2f,
            StllHorseUpgrade.Heavy => 0.92f,
            _ => 1f
        };
    }

    public float GetHorseDefenseMultiplier()
    {
        return Horse == StllHorseUpgrade.Heavy ? 0.85f : 1f;
    }

    public bool TryUpgradeWeaponServer(StllPlayerGold gold)
    {
        if (!IsServer || gold == null)
            return false;

        if (WeaponTier == StllWeaponTier.Master)
            return false;

        var cost = WeaponTier == StllWeaponTier.Rusty
            ? StllEaConstants.WeaponUpgradeCostTier1
            : StllEaConstants.WeaponUpgradeCostTier2;

        if (!gold.TrySpendGoldServer(cost))
            return false;

        weaponTier.Value = (byte)(WeaponTier + 1);
        return true;
    }

    public bool TryBuyHorseServer(StllHorseUpgrade upgrade, StllPlayerGold gold)
    {
        if (!IsServer || gold == null || Horse != StllHorseUpgrade.Normal)
            return false;

        var cost = upgrade == StllHorseUpgrade.Fast
            ? StllEaConstants.HorseFastCost
            : StllEaConstants.HorseHeavyCost;

        if (!gold.TrySpendGoldServer(cost))
            return false;

        horseUpgrade.Value = (byte)upgrade;
        return true;
    }
}
