using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스폰된 보스의 면역·약점 속성. BossData + BossElementGroupDataSo 로부터 구성됩니다.
/// </summary>
public class BossCombatProfile : MonoBehaviour
{
    public const float WeaknessDamageMultiplier = 1.35f;

    private readonly HashSet<DefenseSkillElement> immunities = new();
    private readonly HashSet<DefenseSkillElement> weaknesses = new();

    public string BossCode { get; private set; }

    public void Clear()
    {
        BossCode = null;
        immunities.Clear();
        weaknesses.Clear();
    }

    public void Configure(BossData bossData)
    {
        Clear();
        if (bossData == null)
            return;

        BossCode = bossData.bossCode;
        AddGroupElements(immunities, bossData.immunityGroupCode);
        AddGroupElements(weaknesses, bossData.weaknessGroupCode);
    }

    public bool BlocksEffect(DefenseEffectData effect)
    {
        if (effect == null || immunities.Count == 0)
            return false;

        var element = BossElementUtility.ResolveEffectElement(effect);
        return immunities.Contains(element);
    }

    public float GetWeaknessDamageMultiplier(DefenseSkillElement element)
    {
        if (weaknesses.Count == 0)
            return 1f;

        return weaknesses.Contains(element) ? WeaknessDamageMultiplier : 1f;
    }

    private static void AddGroupElements(HashSet<DefenseSkillElement> target, string groupCode)
    {
        if (target == null || string.IsNullOrWhiteSpace(groupCode))
            return;

        var dataManager = DataManager.Instance;
        if (dataManager == null || !dataManager.TryGetBossElementGroup(groupCode, out var elements))
            return;

        for (int i = 0; i < elements.Count; i++)
            target.Add(elements[i]);
    }
}
