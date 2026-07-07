public static class CwslCharacterSkillCatalog
{
    public const int SkillCount = CwslGameConstants.SkillsPerCharacter;

    public readonly struct SkillSlotDefinition
    {
        public readonly string DisplayName;
        public readonly string KeyHint;
        public readonly float StaminaCost;
        public readonly bool Implemented;

        public SkillSlotDefinition(string displayName, string keyHint, float staminaCost, bool implemented)
        {
            DisplayName = displayName;
            KeyHint = keyHint;
            StaminaCost = staminaCost;
            Implemented = implemented;
        }
    }

    private static readonly SkillSlotDefinition[] TankSkills =
    {
        new("방패 강화", "Q", 0f, true),
        new("지진 강타", "E", CwslGameConstants.TankSkillStaminaCost, true),
        new("방패 회전", "R", CwslGameConstants.TankWhirlwindStaminaCost, true),
        new("방패 돌진", "W", CwslGameConstants.TankSkillStaminaCost, true),
    };

    private static readonly SkillSlotDefinition[] MissileTankSkills =
    {
        new("연속 사격", "Q", 18f, true),
        new("관통탄", "E", 26f, false),
        new("산탄", "R", 30f, false),
        new("포격", "F", 34f, false),
    };

    private static readonly SkillSlotDefinition[] RedMageSkills =
    {
        new("메테오", "Q", 28f, true),
        new("화염 장판", "E", 24f, false),
        new("순간이동", "R", 32f, false),
        new("보호막", "F", 26f, false),
    };

    private static readonly SkillSlotDefinition[] MomentumRammerSkills =
    {
        new("날개 돌진", "Q", 22f, true),
        new("돌파", "E", 26f, false),
        new("회전베기", "R", 30f, false),
        new("가속", "F", 20f, false),
    };

    private static readonly SkillSlotDefinition[] CrowdGathererSkills =
    {
        new("끌어모으기", "Q", 26f, true),
        new("넉백", "E", 22f, false),
        new("속박", "R", 28f, false),
        new("집결", "F", 30f, false),
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
}
