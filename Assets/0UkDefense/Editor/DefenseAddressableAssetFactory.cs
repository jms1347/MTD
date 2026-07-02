#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// 타워·미사일 등 프로젝트 프리팹을 Addressables 그룹에 등록합니다.
/// </summary>
public static class DefenseAddressableAssetFactory
{
    public const string PrefabGroupName = "Prefab";
    public const string MissileGroupName = "Missile";
    public const string EffectGroupName = "Effect";

    public static bool RegisterPrefab(string assetPath, string address, string groupName = PrefabGroupName)
    {
        if (string.IsNullOrWhiteSpace(assetPath) || string.IsNullOrWhiteSpace(address))
            return false;

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[DefenseAddressableAssetFactory] AddressableAssetSettings를 찾을 수 없습니다.");
            return false;
        }

        var group = settings.FindGroup(groupName);
        if (group == null)
        {
            Debug.LogWarning($"[DefenseAddressableAssetFactory] Addressables 그룹이 없습니다: {groupName}");
            return false;
        }

        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogWarning($"[DefenseAddressableAssetFactory] GUID 없음: {assetPath}");
            return false;
        }

        var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
        if (entry == null)
            return false;

        entry.address = address.Trim();
        entry.SetLabel("Prefab", true, true);
        EditorUtility.SetDirty(settings);
        return true;
    }

    public static bool RegisterMissile(string assetPath, string address)
    {
        return RegisterPrefab(assetPath, address, MissileGroupName);
    }

    public static void UpsertMissileKeyEntry(
        DefenseAddressableKeyDataSo keyTable,
        string logicalKey,
        string addressKey,
        string description)
    {
        UpsertKeyEntry(keyTable, logicalKey, addressKey, description, DefenseAddressableKeyType.Missile);
    }

    public static void UpsertPrefabKeyEntry(
        DefenseAddressableKeyDataSo keyTable,
        string logicalKey,
        string addressKey,
        string description)
    {
        if (keyTable == null || string.IsNullOrWhiteSpace(logicalKey) || string.IsNullOrWhiteSpace(addressKey))
            return;

        DefenseAddressableKeyEntry existing = null;
        for (int i = 0; i < keyTable.list.Count; i++)
        {
            var row = keyTable.list[i];
            if (row != null && string.Equals(row.key, logicalKey.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                existing = row;
                break;
            }
        }

        if (existing == null)
        {
            existing = new DefenseAddressableKeyEntry();
            keyTable.list.Add(existing);
        }

        existing.key = logicalKey.Trim();
        existing.assetType = DefenseAddressableKeyType.Prefab;
        existing.addressKey = addressKey.Trim();
        existing.description = description ?? string.Empty;
        keyTable.RebuildLookup();
        EditorUtility.SetDirty(keyTable);
    }

    public static bool RegisterEffect(string assetPath, string address, string groupName = EffectGroupName)
    {
        if (string.IsNullOrWhiteSpace(assetPath) || string.IsNullOrWhiteSpace(address))
            return false;

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[DefenseAddressableAssetFactory] AddressableAssetSettings를 찾을 수 없습니다.");
            return false;
        }

        var group = settings.FindGroup(groupName);
        if (group == null)
        {
            Debug.LogWarning($"[DefenseAddressableAssetFactory] Addressables 그룹이 없습니다: {groupName}");
            return false;
        }

        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogWarning($"[DefenseAddressableAssetFactory] GUID 없음: {assetPath}");
            return false;
        }

        var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
        if (entry == null)
            return false;

        entry.address = address.Trim();
        entry.SetLabel("Effect", true, true);
        EditorUtility.SetDirty(settings);
        return true;
    }

    public static void UpsertEffectKeyEntry(
        DefenseAddressableKeyDataSo keyTable,
        string logicalKey,
        string addressKey,
        string description)
    {
        UpsertKeyEntry(keyTable, logicalKey, addressKey, description, DefenseAddressableKeyType.Effect);
    }

    private static void UpsertKeyEntry(
        DefenseAddressableKeyDataSo keyTable,
        string logicalKey,
        string addressKey,
        string description,
        DefenseAddressableKeyType assetType)
    {
        if (keyTable == null || string.IsNullOrWhiteSpace(logicalKey) || string.IsNullOrWhiteSpace(addressKey))
            return;

        DefenseAddressableKeyEntry existing = null;
        for (int i = 0; i < keyTable.list.Count; i++)
        {
            var row = keyTable.list[i];
            if (row != null && string.Equals(row.key, logicalKey.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                existing = row;
                break;
            }
        }

        if (existing == null)
        {
            existing = new DefenseAddressableKeyEntry();
            keyTable.list.Add(existing);
        }

        existing.key = logicalKey.Trim();
        existing.assetType = assetType;
        existing.addressKey = addressKey.Trim();
        existing.description = description ?? string.Empty;
        keyTable.RebuildLookup();
        EditorUtility.SetDirty(keyTable);
    }
}
#endif
