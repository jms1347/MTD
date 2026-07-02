using System.Collections.Generic;
using UnityEngine;

public static class DefenseEffectGroupResolver
{
    public static bool TryGetEffectsForGroup(string effectGroupCode, List<DefenseEffectData> buffer)
    {
        if (buffer == null)
            return false;

        buffer.Clear();
        if (string.IsNullOrWhiteSpace(effectGroupCode) || DataManager.Instance == null)
            return false;

        if (!DataManager.Instance.TryGetEffectIdsForGroup(effectGroupCode, out var effectIds))
            return false;

        for (int i = 0; i < effectIds.Count; i++)
        {
            if (DataManager.Instance.TryGetEffect(effectIds[i], out var effect) && effect != null)
                buffer.Add(effect);
        }

        return buffer.Count > 0;
    }

    public static bool TryGetPostHitEffectsForSkill(DefenseSkillData skill, List<DefenseEffectData> buffer)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.effectGroupCode))
        {
            buffer?.Clear();
            return false;
        }

        return TryGetEffectsForGroup(skill.effectGroupCode, buffer);
    }
}
