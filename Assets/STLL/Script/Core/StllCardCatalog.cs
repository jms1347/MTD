public enum StllCardKind : byte
{
    Active = 0,
    Passive = 1,
    Military = 2
}

public readonly struct StllCardDefinition
{
    public readonly StllCardId Id;
    public readonly string Name;
    public readonly string Description;
    public readonly StllCardKind Kind;
    public readonly int Tier;

    public StllCardDefinition(StllCardId id, string name, string description, StllCardKind kind, int tier)
    {
        Id = id;
        Name = name;
        Description = description;
        Kind = kind;
        Tier = tier;
    }
}

public static class StllCardCatalog
{
    private static readonly StllCardDefinition[] All =
    {
        new(StllCardId.Lightning, "낙뢰", "조준 지점 번개 80 피해", StllCardKind.Active, 1),
        new(StllCardId.FireZone, "화염 장판", "5m 장판 4초 도트", StllCardKind.Active, 1),
        new(StllCardId.IceMine, "얼음 지뢰", "접촉 50 + 슬로우", StllCardKind.Active, 1),
        new(StllCardId.HealBanner, "치유 깃발", "6m 5초 아군 회복", StllCardKind.Active, 1),
        new(StllCardId.ChargeHorn, "돌격 호각", "전방 돌진 60 피해", StllCardKind.Active, 2),
        new(StllCardId.CrushingStrike, "분쇄 일격", "전방 120 피해", StllCardKind.Active, 2),
        new(StllCardId.IronWall, "철벽", "3초 피해 -70%", StllCardKind.Active, 2),
        new(StllCardId.RapidFire, "연속 사격", "5발 연속 35 피해", StllCardKind.Active, 3),
        new(StllCardId.SharpBlade, "날카로운 칼날", "공격력 +12%", StllCardKind.Passive, 1),
        new(StllCardId.SwiftFeet, "가벼운 발", "이동속도 +10%", StllCardKind.Passive, 1),
        new(StllCardId.IronHeart, "강철 심장", "최대 HP +15%", StllCardKind.Passive, 1),
        new(StllCardId.Tenacity, "끈질긴 체력", "스태미나 회복 +20%", StllCardKind.Passive, 1),
        new(StllCardId.VitalStrike, "급소 타격", "치명타 +15%", StllCardKind.Passive, 2),
        new(StllCardId.CooldownInsight, "전장의 통찰", "쿨다운 -12%", StllCardKind.Passive, 2),
        new(StllCardId.Unyielding, "불굴", "HP 30% 이하 피해 -20%", StllCardKind.Passive, 2),
        new(StllCardId.Unparalleled, "무쌍의 기운", "10% 추가 공격", StllCardKind.Passive, 3),
        new(StllCardId.MinionReinforce, "병력 증원", "부하 +1", StllCardKind.Military, 1),
        new(StllCardId.MinionTraining, "강화 훈련", "부하 HP/공격 +", StllCardKind.Military, 1),
        new(StllCardId.Rally, "집결", "F 명령 시 부하 가속", StllCardKind.Military, 2),
        new(StllCardId.MinionShield, "연합 방패", "부하 1회 50% 흡수", StllCardKind.Military, 3)
    };

    public static StllCardDefinition Get(StllCardId id)
    {
        for (var i = 0; i < All.Length; i++)
        {
            if (All[i].Id == id)
                return All[i];
        }

        return new StllCardDefinition(id, "?", "?", StllCardKind.Passive, 1);
    }

    public static StllCardDefinition[] GetAll() => All;

    public static StllCardId RollWeighted(System.Random rng, int pickIndex)
    {
        var tierRoll = rng.Next(0, 100);
        var targetTier = pickIndex switch
        {
            0 => tierRoll < 70 ? 1 : tierRoll < 98 ? 2 : 3,
            1 => tierRoll < 50 ? 1 : tierRoll < 95 ? 2 : 3,
            _ => tierRoll < 30 ? 1 : tierRoll < 80 ? 2 : 3
        };

        StllCardId[] pool = null;
        var count = 0;
        for (var i = 0; i < All.Length; i++)
        {
            if (All[i].Tier == targetTier)
                count++;
        }

        pool = new StllCardId[count];
        var idx = 0;
        for (var i = 0; i < All.Length; i++)
        {
            if (All[i].Tier == targetTier)
                pool[idx++] = All[i].Id;
        }

        if (pool.Length == 0)
            return All[rng.Next(0, All.Length)].Id;

        return pool[rng.Next(0, pool.Length)];
    }

    public static StllCardId RollGuaranteedForRole(System.Random rng, StllBrotherhoodRole role)
    {
        StllCardId[] pool = role switch
        {
            StllBrotherhoodRole.LiuBei => new[] { StllCardId.HealBanner, StllCardId.Rally, StllCardId.Tenacity },
            StllBrotherhoodRole.GuanYu => new[] { StllCardId.Lightning, StllCardId.CrushingStrike, StllCardId.SharpBlade },
            StllBrotherhoodRole.ZhangFei => new[] { StllCardId.ChargeHorn, StllCardId.IronWall, StllCardId.IronHeart },
            _ => new[] { StllCardId.SharpBlade }
        };

        return pool[rng.Next(0, pool.Length)];
    }
}
