#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DefenseStormCloudPrefabSetup
{
    private const string CloudPrefabPath = LinkedSkillSpawner.StormCloudPrefabPath;
    private const string CloudPrefabKey = LinkedSkillSpawner.StormCloudPrefabKey;

    public static void WireStormCloudPrefab()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(CloudPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[UkDefense] CloudBlack 프리팹을 찾을 수 없습니다: {CloudPrefabPath}");
            return;
        }

        try
        {
            if (prefabRoot.GetComponent<DefenseStormCloud>() == null)
                prefabRoot.AddComponent<DefenseStormCloud>();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, CloudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        DefenseAddressableAssetFactory.RegisterPrefab(CloudPrefabPath, CloudPrefabKey);
        RegisterLightningStrikeEffect();

        var keyTable = AssetDatabase.LoadAssetAtPath<DefenseAddressableKeyDataSo>(
            GoogleSheetDefinitions.AddressableKeyDataAssetPath);
        if (keyTable != null)
        {
            DefenseAddressableAssetFactory.UpsertPrefabKeyEntry(
                keyTable,
                CloudPrefabKey,
                CloudPrefabKey,
                "번개 구름 (Storm Cloud)");
            EditorUtility.SetDirty(keyTable);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[UkDefense] CloudBlack에 DefenseStormCloud 연결 및 Addressables 등록 완료");
    }

    private const string LightningStrikePrefabPath =
        "Assets/Epic Toon FX/Prefabs/Environment/Lightning/Sharp/LightningStrikeSharpTallBlue.prefab";
    private const string LightningStrikeEffectKey = "LightningStrikeSharpTallBlue";

    private static void RegisterLightningStrikeEffect()
    {
        DefenseAddressableAssetFactory.RegisterEffect(LightningStrikePrefabPath, LightningStrikeEffectKey);

        var keyTable = AssetDatabase.LoadAssetAtPath<DefenseAddressableKeyDataSo>(
            GoogleSheetDefinitions.AddressableKeyDataAssetPath);
        if (keyTable == null)
            return;

        DefenseAddressableAssetFactory.UpsertEffectKeyEntry(
            keyTable,
            LightningStrikeEffectKey,
            LightningStrikeEffectKey,
            "구름 낙뢰 VFX");
        EditorUtility.SetDirty(keyTable);
    }

    [InitializeOnLoadMethod]
    private static void AutoEnsureOnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EnsureStormCloudAssets();
        };
    }

    public static void EnsureStormCloudAssets()
    {
        var cloudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CloudPrefabPath);
        if (cloudPrefab != null && cloudPrefab.GetComponent<DefenseStormCloud>() == null)
        {
            WireStormCloudPrefab();
            return;
        }

        RegisterLightningStrikeEffect();
    }
}
#endif
