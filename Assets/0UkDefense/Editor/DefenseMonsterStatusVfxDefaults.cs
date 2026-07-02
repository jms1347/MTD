#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 4원소 몬스터 상태 VFX 기본 매핑 (DefenseCombatCatalog).
/// </summary>
public static class DefenseMonsterStatusVfxDefaults
{
    private const string StunHeadPath =
        "Assets/Epic Toon FX/Prefabs/Combat/Brawling/Stun/StunnedCirclingStarsSimple.prefab";
    private const string BurningBodyPath =
        "Assets/JMO Assets/Cartoon FX/CFX3 Prefabs/Fire/CFX3_Fireball_B 1.prefab";
    private const string PoisonBodyPath =
        "Assets/Epic Toon FX/Prefabs/Combat/Explosions/- Misc/SkullPoison.prefab";
    private const string SlowedFootPath =
        "Assets/JMO Assets/Cartoon FX/CFX3 Prefabs/Magic Dark/CFX3_DarkMagicAura_A.prefab";

    public static void ApplyFromMenu()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<DefenseCombatCatalog>(DefenseCombatCatalogFactory.CatalogPath);
        if (catalog == null)
        {
            Debug.LogError("[UkDefense] DefenseCombatCatalog.asset을 찾을 수 없습니다.");
            return;
        }

        ApplyToCatalog(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("[UkDefense] 4원소 상태 VFX 매핑 적용 완료 → DefenseCombatCatalog");
    }

    public static void ApplyToCatalog(DefenseCombatCatalog catalog)
    {
        if (catalog == null)
            return;

        catalog.monsterStatusVfx = new List<DefenseCombatCatalog.MonsterStatusVfxEntry>
        {
            Entry(MonsterStatus.Burning, bodyPath: BurningBodyPath, bodyScale: 0.55f),
            Entry(MonsterStatus.Slowed, footPath: SlowedFootPath, footLocalY: 0.04f, footScale: 0.55f),
            Entry(MonsterStatus.Shocked, headPath: StunHeadPath),
            Entry(MonsterStatus.Poisoned, bodyPath: PoisonBodyPath, bodyScale: 0.65f),
        };

        catalog.RebuildLookups();
        EditorUtility.SetDirty(catalog);
    }

    private static DefenseCombatCatalog.MonsterStatusVfxEntry Entry(
        MonsterStatus status,
        string headPath = null,
        string bodyPath = null,
        string footPath = null,
        float headLocalY = 1.15f,
        float bodyLocalY = 0.45f,
        float footLocalY = 0.08f,
        float bodyScale = 0.55f,
        float footScale = 0.42f)
    {
        return new DefenseCombatCatalog.MonsterStatusVfxEntry
        {
            status = status,
            headPrefab = LoadOptional(headPath),
            bodyPrefab = LoadOptional(bodyPath),
            footPrefab = LoadOptional(footPath),
            headLocalY = headLocalY,
            bodyLocalY = bodyLocalY,
            footLocalY = footLocalY,
            bodyScale = bodyScale,
            footScale = footScale
        };
    }

    private static GameObject LoadOptional(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
}
#endif
