public static class CwslCharacterSkillCatalog
{
    public const int SkillCount = CwslGameConstants.SkillsPerCharacter;

    public readonly struct SkillSlotDefinition
    {
        public readonly string DisplayName;
        public readonly string KeyHint;
        public readonly float StaminaCost;
        public readonly float CastDuration;
        public readonly bool Implemented;

        public SkillSlotDefinition(
            string displayName,
            string keyHint,
            float staminaCost,
            float castDuration,
            bool implemented)
        {
            DisplayName = displayName;
            KeyHint = keyHint;
            StaminaCost = staminaCost;
            CastDuration = castDuration;
            Implemented = implemented;
        }
    }

    private static readonly SkillSlotDefinition[] TankSkills =
    {
        new("방패 강화", "Q", 0f, CwslGameConstants.FortifyShieldGrowSmoothTime, true),
        new("지진 강타", "E", CwslGameConstants.TankSkillStaminaCost, CwslGameConstants.TankShieldSlamCastDuration, true),
        new("방패 회전", "R", CwslGameConstants.TankWhirlwindStaminaCost, CwslGameConstants.TankShieldWhirlwindDuration, true),
        new("방패 돌진", "W", CwslGameConstants.TankSkillStaminaCost, CwslGameConstants.TankShieldDashCastDuration, true),
    };

    private static readonly SkillSlotDefinition[] MissileTankSkills =
    {
        new("연속 사격", "Q", 18f, 0.35f, true),
        new("연막 대시", "E", 26f, CwslGameConstants.MissileTankSmokeDashDuration, true),
        new("탄환 교체", "R", 30f, 0.2f, true),
        new("강화 버프", "W", 34f, CwslGameConstants.MissileTankPowerBoostDuration, true),
    };

    private static readonly SkillSlotDefinition[] RedMageSkills =
    {
        new("메테오", "Q", 28f, CwslGameConstants.MeteorCastDuration, true),
        new("디아 오브", "E", 24f, CwslGameConstants.RedMageFrozenOrbCastDuration, true),
        new("순간이동", "R", 32f, CwslGameConstants.RedMageTeleportCastDuration, true),
        new("라이트닝 구슬", "W", 24f, CwslGameConstants.RedMageLightningOrbCastDuration, true),
    };

    private static readonly SkillSlotDefinition[] MomentumRammerSkills =
    {
        new("날개 돌진", "Q", 22f, CwslGameConstants.RammerWingSpreadGrowSeconds, true),
        new("돌파", "E", 26f, 1f, false),
        new("회전베기", "R", 30f, 1f, false),
        new("가속", "F", 20f, 1f, false),
    };

    private static readonly SkillSlotDefinition[] CrowdGathererSkills =
    {
        new("끌어모으기", "Q", 26f, CwslGameConstants.GatherChargeSeconds, true),
        new("넉백", "E", 22f, 1f, false),
        new("속박", "R", 28f, 1f, false),
        new("집결", "F", 30f, 1f, false),
    };

    public static SkillSlotDefinition Get(CwslCharacterId characterId, int slotIndex)
    {
        var skills = GetSkills(characterId);
        if (slotIndex < 0 || slotIndex >= skills.Length)
            return skills[0];

        return skills[slotIndex];
    }

    public static SkillSlotDefinition[] GetSkills(CwslCharacterId characterId)
    {
        return characterId switch
        {
            CwslCharacterId.MissileTank => MissileTankSkills,
            CwslCharacterId.RedMage => RedMageSkills,
            CwslCharacterId.MomentumRammer => MomentumRammerSkills,
            CwslCharacterId.CrowdGatherer => CrowdGathererSkills,
            _ => TankSkills,
        };
    }

    public static float GetStaminaCost(CwslCharacterId characterId, int slotIndex)
    {
        return Get(characterId, slotIndex).StaminaCost;
    }

    public static float GetCastDuration(CwslCharacterId characterId, int slotIndex)
    {
        return Get(characterId, slotIndex).CastDuration;
    }

    public static float GetCooldown(CwslCharacterId characterId, int slotIndex)
    {
        return GetCastDuration(characterId, slotIndex) * CwslGameConstants.SkillCooldownMultiplier;
    }
}
