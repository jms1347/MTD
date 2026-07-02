using System.Collections.Generic;
using UnityEngine;

public static class DefenseEffectApplicator
{
    private static readonly List<DefenseEffectData> EffectBuffer = new();

    public static void ApplySkillEffects(GameObject enemyObject, DefenseSkillData skill, Vector3? effectSource = null)
    {
        if (enemyObject == null || skill == null || DataManager.Instance == null)
            return;

        if (string.IsNullOrWhiteSpace(skill.effectGroupCode))
            return;

        if (!DataManager.Instance.TryGetPostHitEffectsForSkill(skill, EffectBuffer))
            return;

        for (int i = 0; i < EffectBuffer.Count; i++)
            ApplyEffect(enemyObject, EffectBuffer[i], effectSource);
    }

    public static void ApplyEffect(GameObject enemyObject, DefenseEffectData effect, Vector3? effectSource = null)
    {
        if (enemyObject == null || effect == null)
            return;

        var status = EnsureStatus(enemyObject);
        var profile = enemyObject.GetComponent<BossCombatProfile>();
        if (profile != null && profile.BlocksEffect(effect))
            return;

        status.ApplyEffect(effect, effectSource);
    }

    private static MonsterStatusController EnsureStatus(GameObject enemyObject)
    {
        var status = enemyObject.GetComponent<MonsterStatusController>();
        if (status == null)
            status = enemyObject.AddComponent<MonsterStatusController>();

        return status;
    }
}
