using System.Collections.Generic;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    [SerializeField] private MonsterDataSo monsterDataSo;
    [SerializeField] private BossDataSo bossDataSo;
    [SerializeField] private BossElementGroupDataSo bossElementGroupDataSo;
    [SerializeField] private TowerDataSo towerDataSo;
    [SerializeField] private DefenseSkillDataSo skillDataSo;
    [SerializeField] private DefenseEffectDataSo effectDataSo;
    [SerializeField] private DefenseEffectGroupDataSo effectGroupDataSo;
    [SerializeField] private StageDataSo stageDataSo;
    [SerializeField] private RoguelikeCardDataSo roguelikeCardDataSo;
    [SerializeField] private DefenseAddressableKeyDataSo addressableKeyDataSo;

    public MonsterDataSo Monsters => monsterDataSo;
    public BossDataSo Bosses => bossDataSo;
    public BossElementGroupDataSo BossElementGroups => bossElementGroupDataSo;
    public TowerDataSo Towers => towerDataSo;
    public DefenseSkillDataSo Skills => skillDataSo;
    public DefenseEffectDataSo Effects => effectDataSo;
    public DefenseEffectGroupDataSo EffectGroups => effectGroupDataSo;
    public StageDataSo Stages => stageDataSo;
    public RoguelikeCardDataSo RoguelikeCards => roguelikeCardDataSo;
    public DefenseAddressableKeyDataSo AddressableKeys => addressableKeyDataSo;

    public bool TryGetAddressableKey(string key, out DefenseAddressableKeyEntry entry)
    {
        entry = null;
        return addressableKeyDataSo != null && addressableKeyDataSo.TryGet(key, out entry);
    }

    public bool TryGetMonster(string code, out MonsterData data)
    {
        data = null;
        return monsterDataSo != null && monsterDataSo.TryGet(code, out data);
    }

    public bool TryGetBossByCode(string bossCode, out BossData data)
    {
        data = null;
        return bossDataSo != null && bossDataSo.TryGetByBossCode(bossCode, out data);
    }

    public bool TryGetBossByMonsterCode(string monsterCode, out BossData data)
    {
        data = null;
        return bossDataSo != null && bossDataSo.TryGetByMonsterCode(monsterCode, out data);
    }

    public bool TryGetBossElementGroup(string groupCode, out IReadOnlyList<DefenseSkillElement> elements)
    {
        elements = null;
        return bossElementGroupDataSo != null && bossElementGroupDataSo.TryGetElements(groupCode, out elements);
    }

    public bool TryGetTower(string code, out TowerData data)
    {
        data = null;
        return towerDataSo != null && towerDataSo.TryGet(code, out data);
    }

    public bool TryGetTower(int towerId, out TowerData data)
    {
        data = null;
        return towerDataSo != null && towerDataSo.TryGet(towerId, out data);
    }

    public bool TryGetSkill(int skillId, out DefenseSkillData skill)
    {
        skill = null;
        return skillDataSo != null && skillDataSo.TryGet(skillId, out skill);
    }

    public bool TryGetSkillByCode(string skillCode, out DefenseSkillData skill)
    {
        skill = null;
        return skillDataSo != null && skillDataSo.TryGetByCode(skillCode, out skill);
    }

    public bool TryGetSkillForTower(TowerData tower, out DefenseSkillData skill)
    {
        skill = null;
        if (tower == null || tower.skillId <= 0)
            return false;

        return TryGetSkill(tower.skillId, out skill);
    }

    public bool TryGetEffect(int effectId, out DefenseEffectData effect)
    {
        effect = null;
        return effectDataSo != null && effectDataSo.TryGet(effectId, out effect);
    }

    public bool TryGetEffectGroup(string effectGroupCode, out DefenseEffectGroup group)
    {
        group = null;
        return effectGroupDataSo != null && effectGroupDataSo.TryGetGroup(effectGroupCode, out group);
    }

    public bool TryGetEffectIdsForGroup(string effectGroupCode, out IReadOnlyList<int> effectIds)
    {
        effectIds = null;
        return effectGroupDataSo != null && effectGroupDataSo.TryGetEffectIds(effectGroupCode, out effectIds);
    }

    public bool TryGetPostHitEffectsForSkill(DefenseSkillData skill, List<DefenseEffectData> buffer)
    {
        return DefenseEffectGroupResolver.TryGetPostHitEffectsForSkill(skill, buffer);
    }

    public bool TryGetStage(int stageId, out StageData stage)
    {
        stage = null;
        return stageDataSo != null && stageDataSo.TryGetStage(stageId, out stage);
    }

    public void InitializeAllData()
    {
        monsterDataSo?.RebuildLookup();
        bossDataSo?.RebuildLookup();
        bossElementGroupDataSo?.RebuildLookup();
        towerDataSo?.RebuildLookup();
        skillDataSo?.RebuildLookup();
        effectDataSo?.RebuildLookup();
        effectGroupDataSo?.RebuildLookup();
        stageDataSo?.RebuildLookup();
        roguelikeCardDataSo?.RebuildLookup();
        addressableKeyDataSo?.RebuildLookup();
        DefenseAddressableLoader.ClearCache();

        ValidateAssignments();

        var monsterCount = monsterDataSo != null ? monsterDataSo.list.Count : 0;
        var bossCount = bossDataSo != null ? bossDataSo.list.Count : 0;
        var bossElementGroupCount = bossElementGroupDataSo != null ? bossElementGroupDataSo.list.Count : 0;
        var towerCount = towerDataSo != null ? towerDataSo.list.Count : 0;
        var skillCount = skillDataSo != null ? skillDataSo.list.Count : 0;
        var effectCount = effectDataSo != null ? effectDataSo.list.Count : 0;
        var effectGroupRowCount = effectGroupDataSo != null ? effectGroupDataSo.list.Count : 0;
        var stageCount = stageDataSo != null ? stageDataSo.stages.Count : 0;
        var roguelikeCardCount = roguelikeCardDataSo != null ? roguelikeCardDataSo.list.Count : 0;
        var addressableKeyCount = addressableKeyDataSo != null ? addressableKeyDataSo.list.Count : 0;
        Debug.Log($"[DataManager] tables ready - monsters: {monsterCount}, bosses: {bossCount}, bossElementGroups: {bossElementGroupCount}, towers: {towerCount}, skills: {skillCount}, effects: {effectCount}, effectGroups: {effectGroupRowCount}, stages: {stageCount}, roguelikeCards: {roguelikeCardCount}, addressableKeys: {addressableKeyCount}");
    }

    private void ValidateAssignments()
    {
        if (monsterDataSo == null)
            Debug.LogWarning("[DataManager] monsterDataSo 가 할당되지 않았습니다.");
        if (bossDataSo == null)
            Debug.LogWarning("[DataManager] bossDataSo 가 할당되지 않았습니다.");
        if (bossElementGroupDataSo == null)
            Debug.LogWarning("[DataManager] bossElementGroupDataSo 가 할당되지 않았습니다.");
        if (towerDataSo == null)
            Debug.LogWarning("[DataManager] towerDataSo 가 할당되지 않았습니다.");
        if (skillDataSo == null)
            Debug.LogWarning("[DataManager] skillDataSo 가 할당되지 않았습니다.");
        if (effectDataSo == null)
            Debug.LogWarning("[DataManager] effectDataSo 가 할당되지 않았습니다.");
        if (effectGroupDataSo == null)
            Debug.LogWarning("[DataManager] effectGroupDataSo 가 할당되지 않았습니다.");
        if (stageDataSo == null)
            Debug.LogWarning("[DataManager] stageDataSo 가 할당되지 않았습니다.");
        if (roguelikeCardDataSo == null)
            Debug.LogWarning("[DataManager] roguelikeCardDataSo 가 할당되지 않았습니다.");
        if (addressableKeyDataSo == null)
            Debug.LogWarning("[DataManager] addressableKeyDataSo 가 할당되지 않았습니다.");
    }
}
