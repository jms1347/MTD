#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 몬스터 prefab에 필수 컴포넌트(UnitCombatVFX 등)가 Unity 인스펙터에 올바르게 붙도록 검증·수정합니다.
/// </summary>
public static class UkDefenseMonsterPrefabValidator
{
    private const string DeathEffectPath = "Assets/Epic Toon FX/Prefabs/Combat/Death/Souls/SoulCuteDeath.prefab";

    private static readonly string[] MonsterCodes =
    {
        "MG-0001", "MG-0002", "MG-0003", "MG-0004", "MG-0005", "MG-0006", "MG-0007", "MG-0008",
        "MB-0001", "MB-0002", "MB-0003", "MB-0004", "MB-0005", "MB-0006",
        "MS-0001", "MS-0002", "MS-0003"
    };

    public static void FixAllMonsterPrefabs()
    {
        int fixedCount = 0;
        foreach (var code in MonsterCodes)
        {
            var path = $"{UkDefenseSetupMenu.MonsterPrefabDir}/{code}.prefab";
            if (FixMonsterPrefab(path, code))
                fixedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[UkDefense] 몬스터 prefab 컴포넌트 검증 완료 (수정: {fixedCount}개)");
    }

    private static bool FixMonsterPrefab(string assetPath, string code)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
        {
            Debug.LogWarning($"[UkDefense] prefab 없음: {assetPath}");
            return false;
        }

        bool isAir = code.StartsWith("MS");
        bool isSuicide = code.EndsWith("0003") || code == "MG-0008";

        var root = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            root.tag = "Enemy";

            Ensure<Health>(root);
            Ensure<Monster>(root);
            Ensure<MonsterStatusController>(root);
            Ensure<MonsterStatusOverlayUI>(root);
            Ensure<MonsterSlimeVisual>(root);
            if (code.StartsWith("MB"))
                Ensure<BossCombatProfile>(root);
            Ensure<PooledEnemy>(root);
            Ensure<HealthDamagePopupBridge>(root);
            Ensure<CombatHitFlash>(root);

            var healthBar = GetOrAdd<HealthBarUI>(root);
            healthBar.ConfigureAsEnemy();

            if (isAir)
            {
                Ensure<AirMonster>(root);
                Remove<GroundMonster>(root);
                Remove<UnitGridNavigator>(root);
            }
            else
            {
                Ensure<GroundMonster>(root);
                Ensure<UnitGridNavigator>(root);
                Remove<AirMonster>(root);
            }

            if (isSuicide)
            {
                Ensure<SuicideMonster>(root);
                Remove<MeleeMonster>(root);
            }
            else
            {
                Ensure<MeleeMonster>(root);
                Remove<SuicideMonster>(root);
            }

            var combatVfx = GetOrAdd<UnitCombatVFX>(root);
            var deathEffect = AssetDatabase.LoadAssetAtPath<GameObject>(DeathEffectPath);
            combatVfx.ConfigureDeathEffect(deathEffect);

            PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return true;
    }

    private static bool Ensure<T>(GameObject root) where T : Component
    {
        if (root.GetComponent<T>() != null)
            return false;

        root.AddComponent<T>();
        return true;
    }

    private static T GetOrAdd<T>(GameObject root) where T : Component
    {
        var component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
    }

    private static bool Remove<T>(GameObject root) where T : Component
    {
        var component = root.GetComponent<T>();
        if (component == null)
            return false;

        Object.DestroyImmediate(component);
        return true;
    }
}
#endif
