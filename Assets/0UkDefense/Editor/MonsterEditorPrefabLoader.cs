#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 리소스 키 DB(AddressableKey) → 몬스터 비주얼 프리팹 에셋.
/// </summary>
public static class MonsterEditorPrefabLoader
{
    private static readonly string[] SearchRoots =
    {
        "Assets/Kawaii Slimes/Prefabs",
        "Assets/Game/2Game/Prefab",
        "Assets/0UkDefense",
    };

    public static bool TryLoadModelPrefab(string prefabKey, out GameObject prefab)
    {
        prefab = null;
        if (string.IsNullOrWhiteSpace(prefabKey))
            return false;

        var key = prefabKey.Trim();
        if (MonsterSlimePrefabPaths.TryGetAssetPath(key, out var knownPath))
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(knownPath);
            if (prefab != null)
                return true;
        }

        if (TryResolveAddressFileName(key, out var fileName) && TryFindPrefabByFileName(fileName, out prefab))
            return true;

        return TryFindPrefabByFileName(key, out prefab);
    }

    public static bool TryResolveAssetPath(string prefabKey, out string assetPath)
    {
        assetPath = null;
        if (!TryLoadModelPrefab(prefabKey, out var prefab) || prefab == null)
            return false;

        assetPath = AssetDatabase.GetAssetPath(prefab);
        return !string.IsNullOrEmpty(assetPath);
    }

    private static bool TryResolveAddressFileName(string prefabKey, out string fileName)
    {
        fileName = null;
        var keyTable = AssetDatabase.LoadAssetAtPath<DefenseAddressableKeyDataSo>(
            GoogleSheetDefinitions.AddressableKeyDataAssetPath);
        if (keyTable == null || !keyTable.TryGet(prefabKey, out var entry) || entry == null)
            return false;

        if (string.IsNullOrWhiteSpace(entry.addressKey))
            return false;

        fileName = entry.addressKey.Trim();
        return true;
    }

    private static bool TryFindPrefabByFileName(string fileName, out GameObject prefab)
    {
        prefab = null;
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var exactName = fileName.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)
            ? fileName
            : fileName + ".prefab";

        for (int r = 0; r < SearchRoots.Length; r++)
        {
            if (!AssetDatabase.IsValidFolder(SearchRoots[r]))
                continue;

            var guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(exactName) + " t:Prefab", new[] { SearchRoots[r] });
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (Path.GetFileName(path).Equals(exactName, System.StringComparison.OrdinalIgnoreCase))
                {
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                        return true;
                }
            }
        }

        return false;
    }
}
#endif
