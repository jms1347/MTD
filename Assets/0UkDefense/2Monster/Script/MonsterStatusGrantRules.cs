/// <summary>
/// 이펙트 DB (effectType) → 몬스터 상태 매핑. 스킬/미사일 후처리 연동의 기준.
/// </summary>
public static class MonsterStatusGrantRules
{
  public static bool TryResolve(DefenseEffectData effect, out MonsterStatus status)
  {
    status = MonsterStatus.None;
    if (effect == null)
      return false;

    if (DefenseElementalStatusMapping.TryGetMonsterStatus(effect.element, out status))
      return true;

    switch (effect.effectType)
    {
      case DefenseEffectType.Stun:
        status = MonsterStatus.Shocked;
        return true;
      case DefenseEffectType.Slow:
      case DefenseEffectType.Root:
        status = MonsterStatus.Slowed;
        return true;
      case DefenseEffectType.Fire:
      case DefenseEffectType.Ground:
        status = MonsterStatus.Burning;
        return true;
      case DefenseEffectType.Poison:
        status = MonsterStatus.Poisoned;
        return true;
      case DefenseEffectType.Lightning:
        status = MonsterStatus.Shocked;
        return true;
      case DefenseEffectType.Water:
        return false;
      case DefenseEffectType.Knockback:
        return false;
      default:
        return false;
    }
  }

  public static DefenseEffectType ToLegacyEffectType(MonsterStatus status)
  {
    return status switch
    {
      MonsterStatus.Shocked => DefenseEffectType.Stun,
      MonsterStatus.Slowed => DefenseEffectType.Slow,
      MonsterStatus.Burning => DefenseEffectType.Fire,
      MonsterStatus.Poisoned => DefenseEffectType.Poison,
      MonsterStatus.Frozen => DefenseEffectType.Root,
      MonsterStatus.Ablaze => DefenseEffectType.Fire,
      MonsterStatus.Wet => DefenseEffectType.Water,
      MonsterStatus.Electrified => DefenseEffectType.Lightning,
      _ => DefenseEffectType.Fire
    };
  }
}
