using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kawaii Slimes 프리팹·애니메이션 공유 에셋 캐시.
/// </summary>
public static class CoopSlimeAssetCache
{
    private static readonly Dictionary<string, string> AddressBySlimeKey = new()
    {
        ["SLIME-01"] = "Slime_01",
        ["SLIME-01-KING"] = "Slime_01_King",
        ["SLIME-01-VIKING"] = "Slime_01_Viking",
        ["SLIME-01-METAL"] = "Slime_01_MeltalHelmet",
        ["SLIME-02"] = "Slime_02",
        ["SLIME-03"] = "Slime_03",
        ["SLIME-03-KING"] = "Slime_03 King",
        ["SLIME-03-LEAF"] = "Slime_03 Leaf",
        ["SLIME-03-SPROUT"] = "Slime_03 Sprout"
    };

    private static readonly Dictionary<string, GameObject> PrefabCache = new();
    private static CoopSlimeRuntimeRefs runtimeRefs;
    private static RuntimeAnimatorController slimeController;
    private static Face faceAsset;
    private static Avatar slimeAvatar;
    private static bool initialized;

    public static RuntimeAnimatorController SlimeController
    {
        get
        {
            EnsureInitialized();
            return slimeController;
        }
    }

    public static Face FaceAsset
    {
        get
        {
            EnsureInitialized();
            return faceAsset;
        }
    }

    public static Avatar SlimeAvatar
    {
        get
        {
            EnsureInitialized();
            return slimeAvatar;
        }
    }

    public static bool TryGetPrefab(string slimeKey, out GameObject prefab)
    {
        EnsureInitialized();
        prefab = null;
        if (string.IsNullOrWhiteSpace(slimeKey))
            return false;

        var key = slimeKey.Trim();
        if (PrefabCache.TryGetValue(key, out prefab) && prefab != null)
            return true;

        if (runtimeRefs != null)
        {
            for (var i = 0; i < runtimeRefs.slimePrefabs.Count; i++)
            {
                var candidate = runtimeRefs.slimePrefabs[i];
                if (candidate == null)
                    continue;

                if (!string.Equals(candidate.name.Replace(" ", "_"), key.Replace("-", "_"), System.StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(candidate.name, ResolvePrefabName(key), System.StringComparison.OrdinalIgnoreCase))
                    continue;

                PrefabCache[key] = candidate;
                prefab = candidate;
                return true;
            }
        }

        if (AddressBySlimeKey.TryGetValue(key, out var address)
            && DefenseAddressableLoader.TryLoadByAddress(address, out prefab)
            && prefab != null)
        {
            PrefabCache[key] = prefab;
            return true;
        }

        if (MonsterSlimePrefabPaths.TryGetAssetPath(key, out var assetPath))
        {
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                PrefabCache[key] = prefab;
                return true;
            }
#endif
        }

        return false;
    }

    private static void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        runtimeRefs = Resources.Load<CoopSlimeRuntimeRefs>("CoopSlimeRuntimeRefs");

        if (runtimeRefs != null)
        {
            slimeController = runtimeRefs.slimeController;
            faceAsset = runtimeRefs.faceAsset;
            slimeAvatar = runtimeRefs.slimeAvatar;
        }

#if UNITY_EDITOR
        if (slimeController == null)
            slimeController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Kawaii Slimes/Animator/Slime.controller");

        if (faceAsset == null)
            faceAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Face>(
                "Assets/Kawaii Slimes/Scripts/AI/DataFace.asset");

        if (slimeAvatar == null)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Kawaii Slimes/Animation/Slime_Anim.fbx");
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Avatar avatar)
                {
                    slimeAvatar = avatar;
                    break;
                }
            }
        }
#endif
    }

    private static string ResolvePrefabName(string slimeKey)
    {
        return slimeKey switch
        {
            "SLIME-01-METAL" => "Slime_01_MeltalHelmet",
            "SLIME-03-KING" => "Slime_03 King",
            "SLIME-03-LEAF" => "Slime_03 Leaf",
            "SLIME-03-SPROUT" => "Slime_03 Sprout",
            _ => slimeKey.Replace("SLIME-", "Slime_").Replace("-", "_")
        };
    }
}
