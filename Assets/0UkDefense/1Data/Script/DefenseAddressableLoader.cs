using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 구글 시트 어드레서블 키 테이블 → Addressables 로드.
/// logical key(시트 Key 컬럼)로 Prefab/Missile/Effect를 불러옵니다.
/// </summary>
public static class DefenseAddressableLoader
{
    private static readonly Dictionary<string, GameObject> CacheByLogicalKey =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, GameObject> CacheByAddress =
        new(StringComparer.OrdinalIgnoreCase);

    public static void ClearCache()
    {
        CacheByLogicalKey.Clear();
        CacheByAddress.Clear();
    }

    public static bool TryResolveAddress(
        string logicalKey,
        DefenseAddressableKeyType requiredType,
        out string addressKey)
    {
        addressKey = null;
        var table = DataManager.Instance != null ? DataManager.Instance.AddressableKeys : null;
        return table != null && table.TryGetAddress(logicalKey, requiredType, out addressKey);
    }

    public static bool TryLoadPrefab(string logicalKey, out GameObject prefab)
    {
        return TryLoad(logicalKey, DefenseAddressableKeyType.Prefab, out prefab);
    }

    public static bool TryLoadMissile(string logicalKey, out GameObject prefab)
    {
        return TryLoad(logicalKey, DefenseAddressableKeyType.Missile, out prefab);
    }

    public static bool TryLoadEffect(string logicalKey, out GameObject prefab)
    {
        return TryLoad(logicalKey, DefenseAddressableKeyType.Effect, out prefab);
    }

    public static bool TryLoad(string logicalKey, DefenseAddressableKeyType requiredType, out GameObject prefab)
    {
        prefab = null;
        if (string.IsNullOrWhiteSpace(logicalKey))
            return false;

        var trimmedKey = logicalKey.Trim();
        if (CacheByLogicalKey.TryGetValue(trimmedKey, out prefab) && prefab != null)
            return true;

        if (!TryResolveAddress(trimmedKey, requiredType, out var addressKey))
            return false;

        if (!TryLoadByAddress(addressKey, out prefab))
            return false;

        CacheByLogicalKey[trimmedKey] = prefab;
        return true;
    }

    public static bool TryLoadByAddress(string addressKey, out GameObject prefab)
    {
        prefab = null;
        if (string.IsNullOrWhiteSpace(addressKey))
            return false;

        var address = addressKey.Trim();
        if (CacheByAddress.TryGetValue(address, out prefab) && prefab != null)
            return true;

        AsyncOperationHandle<GameObject> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<GameObject>(address);
            prefab = handle.WaitForCompletion();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[DefenseAddressableLoader] 로드 실패 address='{address}': {ex.Message}");
            if (handle.IsValid())
                Addressables.Release(handle);
            return false;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[DefenseAddressableLoader] 에셋을 찾을 수 없습니다: address='{address}'");
            if (handle.IsValid())
                Addressables.Release(handle);
            return false;
        }

        CacheByAddress[address] = prefab;
        return true;
    }

    public static async UniTask<GameObject> LoadPrefabAsync(string logicalKey)
    {
        return await LoadAsync(logicalKey, DefenseAddressableKeyType.Prefab);
    }

    public static async UniTask<GameObject> LoadMissileAsync(string logicalKey)
    {
        return await LoadAsync(logicalKey, DefenseAddressableKeyType.Missile);
    }

    public static async UniTask<GameObject> LoadEffectAsync(string logicalKey)
    {
        return await LoadAsync(logicalKey, DefenseAddressableKeyType.Effect);
    }

    public static async UniTask<GameObject> LoadAsync(string logicalKey, DefenseAddressableKeyType requiredType)
    {
        if (string.IsNullOrWhiteSpace(logicalKey))
            return null;

        var trimmedKey = logicalKey.Trim();
        if (CacheByLogicalKey.TryGetValue(trimmedKey, out var cached) && cached != null)
            return cached;

        if (!TryResolveAddress(trimmedKey, requiredType, out var addressKey))
            return null;

        var address = addressKey.Trim();
        if (CacheByAddress.TryGetValue(address, out cached) && cached != null)
        {
            CacheByLogicalKey[trimmedKey] = cached;
            return cached;
        }

        var handle = Addressables.LoadAssetAsync<GameObject>(address);
        try
        {
            var prefab = await handle.Task;
            if (prefab == null)
            {
                Debug.LogWarning($"[DefenseAddressableLoader] 에셋을 찾을 수 없습니다: address='{address}'");
                return null;
            }

            CacheByAddress[address] = prefab;
            CacheByLogicalKey[trimmedKey] = prefab;
            return prefab;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[DefenseAddressableLoader] 비동기 로드 실패 address='{address}': {ex.Message}");
            if (handle.IsValid())
                Addressables.Release(handle);
            return null;
        }
    }
}
