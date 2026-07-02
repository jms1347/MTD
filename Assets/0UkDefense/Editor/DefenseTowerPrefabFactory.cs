#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 시트 Tower DB 기준 타워 비주얼 프리팹 생성 + Addressables(Prefab 그룹) 등록.
/// </summary>
public static class DefenseTowerPrefabFactory
{
    public const string TowerPrefabDir = "Assets/Game/2Game/Prefab/Defense/TowerPrefab";
    public const string LegacyBasicTowerDir = "Assets/Game/2Game/Prefab/Defense/BasicTower";

    /// <summary>Unity -executeMethod 배치용.</summary>
    public static void BatchCreateTowerPrefabsAndAddressables()
    {
        CreateAllInternal(forceRecreate: true);
    }

    public static void RepairTowerPrefabsFromMenu()
    {
        CreateAllInternal(forceRecreate: true);
        Debug.Log("[UkDefense] 타워 프리팹 메테리얼 재생성 완료 → " + TowerPrefabDir);
    }

    public static void EnsureTowerPrefabsIfMissing()
    {
        var sample = TryLoadTowerPrefab("N-0001");
        if (sample != null && DefenseTowerVisualResolver.HasUsableMaterials(sample))
            return;

        CreateAllInternal(forceRecreate: true);
    }

    private static void CreateAllInternal(bool forceRecreate)
    {
        DefenseCombatCatalogFactory.EnsureCatalogAsset();
        EnsureTowerPrefabsFromSheet(forceRecreate);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureTowerPrefabsFromSheet(bool forceRecreate = false)
    {
        EnsureFolder(TowerPrefabDir);

        var towerSo = AssetDatabase.LoadAssetAtPath<TowerDataSo>(GoogleSheetDefinitions.TowerDataAssetPath);
        if (towerSo == null || towerSo.list == null || towerSo.list.Count == 0)
        {
            Debug.LogWarning("[DefenseTowerPrefabFactory] TowerDataSo가 비어 있습니다. 시트를 먼저 import 하세요.");
            return;
        }

        var keyTable = AssetDatabase.LoadAssetAtPath<DefenseAddressableKeyDataSo>(
            GoogleSheetDefinitions.AddressableKeyDataAssetPath);

        for (int i = 0; i < towerSo.list.Count; i++)
        {
            var tower = towerSo.list[i];
            if (tower == null || tower.towerId <= 0)
                continue;

            string prefabKey = tower.ResolvePrefabKey();
            if (string.IsNullOrWhiteSpace(prefabKey))
                continue;

            string prefabPath = GetTowerPrefabPath(prefabKey);
            var existing = LoadPrefab(prefabPath);
            bool needsRecreate = forceRecreate
                || existing == null
                || !DefenseTowerVisualResolver.HasUsableMaterials(existing);

            GameObject prefab = existing;
            if (needsRecreate)
            {
                prefab = CreateTowerVisualPrefab(
                    prefabKey,
                    tower.towerName,
                    tower.towerId,
                    forceRecreate: true);
            }

            if (prefab == null)
                continue;

            DefenseAddressableAssetFactory.RegisterPrefab(prefabPath, prefabKey);
            DefenseAddressableAssetFactory.UpsertPrefabKeyEntry(
                keyTable,
                prefabKey,
                prefabKey,
                tower.towerName);
        }

        if (keyTable != null)
            EditorUtility.SetDirty(keyTable);
    }

    public static string GetTowerPrefabPath(string prefabKey)
    {
        return $"{TowerPrefabDir}/{prefabKey.Trim()}.prefab";
    }

    public static GameObject TryLoadTowerPrefab(string prefabKey)
    {
        if (string.IsNullOrWhiteSpace(prefabKey))
            return null;

        var key = prefabKey.Trim();
        var fromNew = LoadPrefab(GetTowerPrefabPath(key));
        if (fromNew != null)
            return fromNew;

        return LoadPrefab($"{LegacyBasicTowerDir}/Tower_{key}.prefab");
    }

    public static GameObject TryLoadBasicTowerPrefab(string towerCode)
    {
        return TryLoadTowerPrefab(towerCode);
    }

    private static GameObject CreateTowerVisualPrefab(
        string towerCode,
        string towerName,
        int sheetTowerId,
        bool forceRecreate = false)
    {
        var path = GetTowerPrefabPath(towerCode);
        if (!forceRecreate && LoadPrefab(path) != null)
            return LoadPrefab(path);

        var tempShell = new GameObject($"__Build_{towerCode}");
        try
        {
            var spawn = new TowerSpawnData
            {
                towerName = string.IsNullOrWhiteSpace(towerName) ? towerCode : towerName,
                towerSheetId = sheetTowerId,
                kind = TowerKind.Standard
            };

            DefenseTowerVisualBuilder.Build(tempShell.transform, spawn, sheetTowerId, TowerKind.Standard, towerCode);
            var visual = tempShell.transform.Find("Visual");
            if (visual == null)
            {
                Debug.LogError($"[DefenseTowerPrefabFactory] Visual 생성 실패: {towerCode}");
                return null;
            }

            EnsureEmbeddedMaterials(visual);
            visual.gameObject.name = towerCode;
            return SavePrefab(visual.gameObject, path);
        }
        finally
        {
            Object.DestroyImmediate(tempShell);
        }
    }

    private static void EnsureEmbeddedMaterials(Transform root)
    {
        if (root == null)
            return;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            var source = renderer.sharedMaterial;
            if (source == null)
                continue;

            renderer.sharedMaterial = new Material(source);
        }
    }

    private static GameObject SavePrefab(GameObject source, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || renderer.sharedMaterial == null)
                continue;

            var embedded = new Material(renderer.sharedMaterial) { name = $"{renderer.gameObject.name}_Mat" };
            renderer.sharedMaterial = embedded;
        }

        var prefabAsset = PrefabUtility.SaveAsPrefabAsset(source, path);
        if (prefabAsset == null)
            return null;

        var materialsByPart = new Dictionary<string, Material>();
        foreach (var subAsset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (subAsset is not Material material || string.IsNullOrEmpty(material.name))
                continue;

            if (!material.name.EndsWith("_Mat"))
                continue;

            string partName = material.name.Substring(0, material.name.Length - "_Mat".Length);
            materialsByPart[partName] = material;
        }

        foreach (var renderer in prefabAsset.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            if (materialsByPart.TryGetValue(renderer.gameObject.name, out var material))
                renderer.sharedMaterial = material;
        }

        PrefabUtility.SavePrefabAsset(prefabAsset);
        EditorUtility.SetDirty(prefabAsset);
        return prefabAsset;
    }

    private static GameObject LoadPrefab(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    [InitializeOnLoadMethod]
    private static void AutoEnsureOnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EnsureTowerPrefabsIfMissing();
        };
    }
}
#endif
