using UnityEngine;

/// <summary>
/// 적 피격 시 데미지 적용 진입점.
/// </summary>
public static class MonsterStatusCombatResolver
{
  public static void ApplyDamageToEnemy(
    GameObject enemy,
    float baseDamage,
    DefenseSkillElement element,
    Vector3 hitPoint)
  {
    var damageElement = DefenseSkillElementUtility.ToDamageElement(element);
    ApplyDamageToEnemy(enemy, baseDamage, element, damageElement, hitPoint);
  }

  public static void ApplyDamageToEnemy(
    GameObject enemy,
    float baseDamage,
    DefenseSkillElement element,
    DamageElement damageElement,
    Vector3 hitPoint)
  {
    if (enemy == null)
      return;

    var health = enemy.GetComponent<Health>();
    if (health == null || !health.IsAlive)
      return;

    float damage = MonsterStatusReactionRules.AdjustDamage(enemy, element, baseDamage);
    health.TakeDamage(DamageInfo.Projectile(damage, damageElement, hitPoint));
  }

  public static void ApplyAoEDamageToEnemy(
    GameObject enemy,
    float baseDamage,
    DefenseSkillElement element,
    Vector3 hitPoint)
  {
    if (enemy == null)
      return;

    var health = enemy.GetComponent<Health>();
    if (health == null || !health.IsAlive)
      return;

    var damageElement = DefenseSkillElementUtility.ToDamageElement(element);
    float damage = MonsterStatusReactionRules.AdjustDamage(enemy, element, baseDamage);
    health.TakeDamage(DamageInfo.AoE(damage, damageElement, hitPoint));
  }
}
