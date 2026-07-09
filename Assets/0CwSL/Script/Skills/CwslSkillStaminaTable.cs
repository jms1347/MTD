/// <summary>스킬 임팩트 등급 — 스태미너 비용과 연동.</summary>
public enum CwslSkillTier : byte
{
    /// <summary>홀드·패시브 — 사용 시작 비용 없음 (별도 틱/차단 비용 가능).</summary>
    Passive = 0,
    /// <summary>기본/빈번 — 짧은 교전, 유틸.</summary>
    C = 1,
    /// <summary>보조 — 이동·제어·중간 피해.</summary>
    B = 2,
    /// <summary>핵심 — 주력 딜·CC·팀 기여.</summary>
    A = 3,
    /// <summary>궁극 — 결정적 범위·장기 효과.</summary>
    S = 4,
}

public static class CwslSkillStaminaTable
{
    public static float GetCost(CwslSkillTier tier)
    {
        return tier switch
        {
            CwslSkillTier.S => CwslGameConstants.SkillStaminaTierS,
            CwslSkillTier.A => CwslGameConstants.SkillStaminaTierA,
            CwslSkillTier.B => CwslGameConstants.SkillStaminaTierB,
            CwslSkillTier.C => CwslGameConstants.SkillStaminaTierC,
            _ => 0f,
        };
    }

    public static string GetTierLabel(CwslSkillTier tier)
    {
        return tier switch
        {
            CwslSkillTier.S => "S",
            CwslSkillTier.A => "A",
            CwslSkillTier.B => "B",
            CwslSkillTier.C => "C",
            _ => "-",
        };
    }
}
