/// <summary>
/// 4원소(불·얼음·번개·독) ↔ 몬스터 상태·데미지 속성 매핑.
/// </summary>
public static class DefenseElementalStatusMapping
{
    public static bool TryGetMonsterStatus(DefenseSkillElement element, out MonsterStatus status)
    {
        status = element switch
        {
            DefenseSkillElement.Fire => MonsterStatus.Burning,
            DefenseSkillElement.Ice => MonsterStatus.Slowed,
            DefenseSkillElement.Lightning => MonsterStatus.Shocked,
            DefenseSkillElement.Poison => MonsterStatus.Poisoned,
            _ => MonsterStatus.None
        };

        return status != MonsterStatus.None;
    }

    public static bool IsElemental(DefenseSkillElement element) =>
        element is DefenseSkillElement.Fire
            or DefenseSkillElement.Ice
            or DefenseSkillElement.Lightning
            or DefenseSkillElement.Poison;
}
