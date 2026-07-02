using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DefenseCombatCatalog", menuName = "UkDefense/Defense Combat Catalog")]
public class DefenseCombatCatalog : ScriptableObject
{
    [Serializable]
    public class MonsterStatusVfxEntry
    {
        public MonsterStatus status;
        public GameObject headPrefab;
        public GameObject bodyPrefab;
        public GameObject footPrefab;
        public float headLocalY = 1.15f;
        public float bodyLocalY = 0.45f;
        public float footLocalY = 0.08f;
        public float bodyScale = 0.55f;
        public float footScale = 0.42f;
    }

    [Header("Monster Status VFX (4원소 상태 — Burning / Slowed / Shocked / Poisoned)")]
    public List<MonsterStatusVfxEntry> monsterStatusVfx = new();

    [Header("Meteor Tower")]
    public GameObject meteorMissilePrefab;
    public GameObject meteorExplosionPrefab;

    [Header("Chain Lightning Tower")]
    public GameObject chainBoltPrefab;
    public GameObject chainHitExplosionPrefab;
    public GameObject stunHeadEffectPrefab;
    public GameObject stunBodyEffectPrefab;

    [Header("Monster Death (fallback)")]
    public GameObject defaultDeathEffectPrefab;

    private static DefenseCombatCatalog active;
    private Dictionary<MonsterStatus, MonsterStatusVfxEntry> monsterStatusLookup;

    public const string DefaultAssetPath = "Assets/0UkDefense/1Data/SO/DefenseCombatCatalog.asset";
    public const string DefaultResourceName = "DefenseCombatCatalog";

    public static DefenseCombatCatalog Active => active;

    public static DefenseCombatCatalog LoadFallback()
    {
#if UNITY_EDITOR
        var editorCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<DefenseCombatCatalog>(DefaultAssetPath);
        if (editorCatalog != null)
            return editorCatalog;
#endif
        return Resources.Load<DefenseCombatCatalog>(DefaultResourceName);
    }

    public void Activate()
    {
        active = this;
        RebuildLookups();
    }

    public void RebuildLookups()
    {
        monsterStatusLookup = new Dictionary<MonsterStatus, MonsterStatusVfxEntry>();
        for (int i = 0; i < monsterStatusVfx.Count; i++)
        {
            var entry = monsterStatusVfx[i];
            if (entry == null)
                continue;

            if (!monsterStatusLookup.ContainsKey(entry.status))
                monsterStatusLookup.Add(entry.status, entry);
        }
    }

    public bool TryGetMonsterStatusVfx(MonsterStatus status, out MonsterStatusVfxEntry entry)
    {
        EnsureLookups();
        return monsterStatusLookup.TryGetValue(status, out entry);
    }

    public bool TryGetElementalStatusVfx(DefenseSkillElement element, out MonsterStatusVfxEntry entry)
    {
        entry = null;
        if (!DefenseElementalStatusMapping.TryGetMonsterStatus(element, out var status))
            return false;

        return TryGetMonsterStatusVfx(status, out entry);
    }

    private void OnEnable()
    {
        RebuildLookups();
    }

    private void EnsureLookups()
    {
        if (monsterStatusLookup == null)
            RebuildLookups();
    }
}
