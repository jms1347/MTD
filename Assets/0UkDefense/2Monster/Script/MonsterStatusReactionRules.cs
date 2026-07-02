using UnityEngine;

/// <summary>
/// 데미지 속성 변환·보스 약점 보정.
/// </summary>
public static class MonsterStatusReactionRules
{
  public static DefenseSkillElement InferElementFromDamage(DamageElement damageElement)
  {
    return damageElement switch
    {
      DamageElement.Fire => DefenseSkillElement.Fire,
      DamageElement.Lightning => DefenseSkillElement.Lightning,
      DamageElement.Green => DefenseSkillElement.Water,
      DamageElement.Blue => DefenseSkillElement.Ice,
      DamageElement.Pink => DefenseSkillElement.Poison,
      _ => DefenseSkillElement.Physical
    };
  }

  public static float AdjustDamage(GameObject enemy, DefenseSkillElement element, float baseDamage)
  {
    float damage = baseDamage;

    var profile = enemy != null ? enemy.GetComponent<BossCombatProfile>() : null;
    if (profile != null)
      damage *= profile.GetWeaknessDamageMultiplier(element);

    return damage;
  }
}
