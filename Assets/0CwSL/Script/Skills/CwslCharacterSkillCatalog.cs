using System.Text;

public static class CwslCharacterSkillCatalog
{
    public const int SkillCount = CwslGameConstants.SkillsPerCharacter;

    private static readonly string[] GuideKeyOrder = { "Q", "W", "E", "R" };
    public static readonly string[] HudKeyOrder = { "Q", "W", "E", "R" };

    // 내부 슬롯 인덱스: 0=Q, 1=W, 2=E, 3=R — 스킬 코드·쿨타임·입력·HUD와 동일.
    public const int SlotQ = 0;
    public const int SlotW = 1;
    public const int SlotE = 2;
    public const int SlotR = 3;

    public readonly struct SkillSlotDefinition
    {
        public readonly string DisplayName;
        public readonly string KeyHint;
        public readonly string Description;
        public readonly CwslSkillTier Tier;
        public readonly float StaminaCost;
        public readonly float CastDuration;
        public readonly bool Implemented;

        public SkillSlotDefinition(
            string displayName,
            string keyHint,
            string description,
            CwslSkillTier tier,
            float castDuration,
            bool implemented)
        {
            DisplayName = displayName;
            KeyHint = keyHint;
            Description = description;
            Tier = tier;
            StaminaCost = CwslSkillStaminaTable.GetCost(tier);
            CastDuration = castDuration;
            Implemented = implemented;
        }
    }

    private static readonly SkillSlotDefinition[] TankSkills =
    {
        new("방패 강화", "Q", "Q/Space 홀드 — 방패를 펼쳐 방어력 2배. SP 4/초 유지. E·R·W 효과 강화.", CwslSkillTier.Passive, CwslGameConstants.FortifyShieldGrowSmoothTime, true),
        new("방패 돌진", "W", "전방으로 돌진하며 경로上的 적을 밀쳐냅니다.", CwslSkillTier.B, CwslGameConstants.TankShieldDashCastDuration, true),
        new("지진 강타", "E", "바닥을 내리쳐 주변 몬스터를 기절시키고 밀어냅니다.", CwslSkillTier.A, CwslGameConstants.TankShieldSlamCastDuration, true),
        new("방패 회전", "R", "4초간 제자리에서 회전하며 주변에 광역 피해를 줍니다.", CwslSkillTier.S, CwslGameConstants.TankShieldWhirlwindDuration, true),
    };

    private static readonly SkillSlotDefinition[] MissileTankSkills =
    {
        new("연속 사격", "Q", "양쪽 포를 빠르게 연속 발사합니다.", CwslSkillTier.C, 0.35f, true),
        new("강화 버프", "W", "일정 시간 공격력·관통이 강화됩니다.", CwslSkillTier.A, CwslGameConstants.MissileTankPowerBoostDuration, true),
        new("연막 대시", "E", "연막을 깔고 짧게 대시해 위치를 바꿉니다.", CwslSkillTier.B, CwslGameConstants.MissileTankSmokeDashDuration, true),
        new("탄환 교체", "R", "화염·독·번개 등 탄환 종류를 바꿉니다.", CwslSkillTier.C, 0.2f, true),
    };

    private static readonly SkillSlotDefinition[] RedMageSkills =
    {
        new("메테오", "Q", "지정 지점에 운석을 떨어뜨려 광역 피해와 화상을 겁니다.", CwslSkillTier.S, CwslGameConstants.MeteorCastDuration, true),
        new("라이트닝 구슬", "W", "번개 구슬을 날려 착지 지점에 연쇄 타격을 줍니다.", CwslSkillTier.A, CwslGameConstants.RedMageLightningOrbCastDuration, true),
        new("디아 오브", "E", "관통하는 얼음 구체를 발사해 동상을 겁니다.", CwslSkillTier.B, CwslGameConstants.RedMageFrozenOrbCastDuration, true),
        new("순간이동", "R", "짧은 거리를 즉시 이동합니다.", CwslSkillTier.A, CwslGameConstants.RedMageTeleportCastDuration, true),
    };

    private static readonly SkillSlotDefinition[] MomentumRammerSkills =
    {
        new("날개 돌진", "Q", "Q/Space 홀드 — 날개를 펼치며 가속하고 충돌 시 피해를 줍니다. 시작 SP + 3/초 유지.", CwslSkillTier.B, CwslGameConstants.RammerWingSpreadGrowSeconds, true),
        new("급브레이크", "W", "모은 속도로 급정지하며 주변에 피해와 넉백을 줍니다.", CwslSkillTier.B, CwslGameConstants.RammerBrakeCastDuration, true),
        new("밧줄 연결", "E", "적에게 밧줄을 걸어 당기거나 회전시킵니다.", CwslSkillTier.A, CwslGameConstants.RammerRopeCastDuration, true),
        new("불꽃 질주", "R", "질주하며 뒤에 화염 장판을 남깁니다.", CwslSkillTier.S, CwslGameConstants.RammerFireTrailCastDuration, true),
    };

    private static readonly SkillSlotDefinition[] CrowdGathererSkills =
    {
        new("끌어모으기", "Q", "Q/Space 홀드 — 슬로우+흡인, 뗄 때 이펙트가 수축하며 중심으로 모읍니다.", CwslSkillTier.A, CwslGameConstants.GatherBlackHoleZoneDuration, true),
        new("강제 소환", "W", "W — 블랙홀 영역 생성. 밧줄 연결+슬로우 흡인 3초 후 중심 수렴. 미사일/자폭 유발 시 폭발.", CwslSkillTier.B, CwslGameConstants.GathererYankCastDuration, true),
        new("자리 교환", "E", "E — 클릭 지역과 내 주변 영역을 동시에 표시한 뒤 유닛·투사체 위치를 맞바꿉니다.", CwslSkillTier.A, CwslGameConstants.GathererSwapCastDuration, true),
        new("회오리", "R", "회오리로 적을 띄운 뒤 상단에서 던져냅니다.", CwslSkillTier.S, CwslGameConstants.GathererWhirlwindDuration, true),
    };

    private static readonly SkillSlotDefinition[] BarricadeSkills =
    {
        new("방벽 설치", "Q", "Q/Space 드래그 — 벽을 설치해 길을 막습니다.", CwslSkillTier.A, CwslGameConstants.BarricadeWallCastDuration, true),
        new("점프 발판", "W", "발판을 설치해 아군이 높이 점프할 수 있게 합니다.", CwslSkillTier.C, CwslGameConstants.BarricadeJumpPadCastDuration, true),
        new("넥서스 수리", "E", "넥서스 체력을 회복합니다.", CwslSkillTier.S, CwslGameConstants.BarricadeRepairDuration, true),
        new("방벽 폭파", "R", "설치한 벽을 폭파해 주변에 피해를 줍니다.", CwslSkillTier.A, CwslGameConstants.BarricadeDetonateCastDuration, true),
    };

    private static readonly SkillSlotDefinition[] HealerSkills =
    {
        new("힐 장판", "Q", "지속 회복 장판을 깔아 아군을 치유합니다.", CwslSkillTier.B, CwslGameConstants.HealerHealPadCastDuration, true),
        new("독 장판", "W", "범위 독 장판으로 몬스터에 지속 피해를 줍니다.", CwslSkillTier.B, CwslGameConstants.HealerPoisonPadCastDuration, true),
        new("단체 치유", "E", "주변 아군에게 즉시 회복을 줍니다.", CwslSkillTier.A, CwslGameConstants.HealerBurstHealCastDuration, true),
        new("축복 가속", "R", "아군 이동·공격 속도를 올려줍니다.", CwslSkillTier.A, CwslGameConstants.HealerHasteBuffCastDuration, true),
    };

    public static SkillSlotDefinition Get(CwslCharacterId characterId, int slotIndex)
    {
        var skills = GetSkills(characterId);
        if (slotIndex < 0 || slotIndex >= skills.Length)
            return skills[0];

        return skills[slotIndex];
    }

    public static int GetSlotIndexByKey(CwslCharacterId characterId, string keyHint)
    {
        return keyHint switch
        {
            "Q" => SlotQ,
            "W" => SlotW,
            "E" => SlotE,
            "R" => SlotR,
            _ => SlotQ,
        };
    }

    public static SkillSlotDefinition[] GetSkills(CwslCharacterId characterId)
    {
        return characterId switch
        {
            CwslCharacterId.MissileTank => MissileTankSkills,
            CwslCharacterId.RedMage => RedMageSkills,
            CwslCharacterId.MomentumRammer => MomentumRammerSkills,
            CwslCharacterId.CrowdGatherer => CrowdGathererSkills,
            CwslCharacterId.Barricade => BarricadeSkills,
            CwslCharacterId.Healer => HealerSkills,
            _ => TankSkills,
        };
    }

    public static string BuildGuideText(CwslCharacterId characterId)
    {
        var skills = GetSkills(characterId);
        var builder = new StringBuilder(256);

        foreach (var key in GuideKeyOrder)
        {
            for (var i = 0; i < skills.Length; i++)
            {
                if (skills[i].KeyHint != key)
                    continue;

                builder.Append('[').Append(key).Append("] ").Append(skills[i].DisplayName);
                if (TryGetHoldSkillSpLabel(characterId, skills[i], out var holdLabel))
                    builder.Append(' ').Append(holdLabel);
                else if (skills[i].Tier != CwslSkillTier.Passive)
                    builder.Append(" (").Append(CwslSkillStaminaTable.GetTierLabel(skills[i].Tier))
                        .Append(" · SP ").Append((int)skills[i].StaminaCost).Append(')');
                if (!string.IsNullOrWhiteSpace(skills[i].Description))
                    builder.Append(" — ").Append(skills[i].Description);
                builder.AppendLine();
                break;
            }
        }

        return builder.ToString().TrimEnd();
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

    private static bool TryGetHoldSkillSpLabel(
        CwslCharacterId characterId,
        SkillSlotDefinition skill,
        out string label)
    {
        label = null;
        if (skill.KeyHint != "Q")
            return false;

        switch (characterId)
        {
            case CwslCharacterId.Tank:
                label = "(4 SP/초 유지)";
                return true;
            case CwslCharacterId.MomentumRammer:
                label = $"(시작 {(int)skill.StaminaCost} + 3 SP/초)";
                return true;
            case CwslCharacterId.CrowdGatherer:
                label = $"(시작 {(int)skill.StaminaCost} + 3 SP/초 · 5초)";
                return true;
            default:
                return false;
        }
    }
}
