using System.Collections;
using UnityEngine;

/// <summary>
/// 로그라이크 유성 카드 — 표식 지점 주변에 약 5초간 유성을 연속 낙하시킵니다.
/// </summary>
public static class DefenseRoguelikeMeteorBarrage
{
    private const float DefaultDuration = 5f;
    private const float DropInterval = 0.85f;

    public static void Begin(
        Vector3 center,
        float radius,
        DefenseSkillData skill,
        DefenseTowerCombatContext tower,
        string targetMobility,
        float duration = DefaultDuration)
    {
        if (skill == null)
            return;

        DefenseCombatSequenceHost.Ensure().StartCoroutine(
            Run(center, radius, skill, tower, targetMobility, duration));
    }

    private static IEnumerator Run(
        Vector3 center,
        float radius,
        DefenseSkillData skill,
        DefenseTowerCombatContext tower,
        string targetMobility,
        float duration)
    {
        center = DefenseBallisticUtility.ProjectToGround(center);
        float endTime = Time.time + duration;
        float damage = tower.baseDamage * skill.damageMultiplier;
        var context = DefenseSkillProjectileContext.Create(
            skill,
            tower,
            0,
            null,
            targetMobility);

        while (Time.time < endTime)
        {
            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0.5f, radius * 0.75f);
            Vector3 strikePoint = center + new Vector3(offset.x, 0f, offset.y);
            DefenseDelayedMeteorStrike.DropMeteorAt(strikePoint, skill, damage, targetMobility, context);
            yield return new WaitForSeconds(DropInterval);
        }
    }
}
