#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스킬·타워 데이터에서 참조하는 미사일/이펙트/프리팹 키를
/// Addressables + DefenseAddressableKeyDataSo(리소스 키 DB)에 동기화합니다.
/// </summary>
public static class DefenseCombatResourceSync
{
    private const string ExportPath = "Assets/0UkDefense/1Data/SheetExport/AddressableKey.tsv";

    private static readonly string[] PrefabSearchRoots =
    {
        "Assets/Epic Toon FX/Demo/Missile Prefabs",
        "Assets/Epic Toon FX/Prefabs",
        "Assets/JMO Assets",
        "Assets/Game/2Game/Prefab",
        "Assets/0UkDefense",
        "Assets/Kawaii Slimes/Prefabs",
    };

    public static void SyncFromMenu()
    {
        var result = SyncFromCombatData(exportTsv: true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"[UkDefense] 리소스 동기화 완료 — 등록 {result.registered}건, 키 DB {result.keyRows}건, 누락 {result.missing.Count}건");
    }

    public static SyncResult SyncFromCombatData(bool exportTsv = false)
    {
        var skillSo = AssetDatabase.LoadAssetAtPath<DefenseSkillDataSo>(GoogleSheetDefinitions.SkillDataAssetPath);
        var towerSo = AssetDatabase.LoadAssetAtPath<TowerDataSo>(GoogleSheetDefinitions.TowerDataAssetPath);
        var keyTable = AssetDatabase.LoadAssetAtPath<DefenseAddressableKeyDataSo>(
            GoogleSheetDefinitions.AddressableKeyDataAssetPath);

        var result = new SyncResult();
        if (keyTable == null)
        {
            Debug.LogError("[DefenseCombatResourceSync] DefenseAddressableKeyDataSo를 찾을 수 없습니다.");
            return result;
        }

        DefenseStormCloudPrefabSetup.EnsureStormCloudAssets();
        DefenseZonePrefabSetup.EnsureAll();

        var requests = CollectResourceRequests(skillSo, towerSo);
        RegisterResourceRequests(keyTable, requests, result);

        EditorUtility.SetDirty(keyTable);
        keyTable.RebuildLookup();
        result.keyRows = keyTable.list.Count;

        if (exportTsv)
            ExportAddressableKeyTsv(keyTable);

        return result;
    }

    /// <summary>Monster.tsv prefabKey(SLIME-*)만 Addressables에 등록합니다.</summary>
    public static SyncResult SyncMonsterModelAddressables(bool exportTsv = false)
    {
        var keyTable = AssetDatabase.LoadAssetAtPath<DefenseAddressableKeyDataSo>(
            GoogleSheetDefinitions.AddressableKeyDataAssetPath);
        var result = new SyncResult();
        if (keyTable == null)
        {
            Debug.LogError("[DefenseCombatResourceSync] DefenseAddressableKeyDataSo를 찾을 수 없습니다.");
            return result;
        }

        RegisterResourceRequests(keyTable, CollectMonsterModelRequests(), result);

        EditorUtility.SetDirty(keyTable);
        keyTable.RebuildLookup();
        result.keyRows = keyTable.list.Count;

        if (exportTsv)
            ExportAddressableKeyTsv(keyTable);

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"[DefenseCombatResourceSync] 몬스터 모델 Addressables 등록 — 성공 {result.registered}건, 누락 {result.missing.Count}건");
        return result;
    }

    private static void RegisterResourceRequests(
        DefenseAddressableKeyDataSo keyTable,
        List<ResourceRequest> requests,
        SyncResult result)
    {
        foreach (var request in requests)
        {
            if (!TryResolveAssetPath(request.Key, request.PreferredType, out var assetPath, out var resolvedType))
            {
                result.missing.Add(request.Key);
                Debug.LogWarning($"[DefenseCombatResourceSync] 에셋 경로를 찾을 수 없습니다: key={request.Key}");
                continue;
            }

            var type = request.PreferredType != DefenseAddressableKeyType.Unknown
                ? request.PreferredType
                : resolvedType;

            var addressKey = ResolveAddressKey(request.Key, assetPath, keyTable);
            if (!RegisterAddressable(assetPath, addressKey, type))
            {
                result.missing.Add(request.Key);
                continue;
            }

            UpsertKeyEntry(keyTable, request.Key, addressKey, request.Description, type);
            result.registered++;
        }
    }

    private static string ResolveAddressKey(
        string logicalKey,
        string assetPath,
        DefenseAddressableKeyDataSo keyTable)
    {
        if (keyTable != null
            && keyTable.TryGet(logicalKey, out var entry)
            && entry != null
            && !string.IsNullOrWhiteSpace(entry.addressKey))
        {
            return entry.addressKey.Trim();
        }

        return Path.GetFileNameWithoutExtension(assetPath);
    }

    private static List<ResourceRequest> CollectResourceRequests(DefenseSkillDataSo skillSo, TowerDataSo towerSo)
    {
        var map = new Dictionary<string, ResourceRequest>(StringComparer.OrdinalIgnoreCase);

        void Add(string key, DefenseAddressableKeyType preferredType, string description)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            var trimmed = key.Trim();
            if (!map.ContainsKey(trimmed))
            {
                map.Add(trimmed, new ResourceRequest
                {
                    Key = trimmed,
                    PreferredType = preferredType,
                    Description = description ?? string.Empty
                });
            }
        }

        if (skillSo?.list != null)
        {
            for (int i = 0; i < skillSo.list.Count; i++)
            {
                var skill = skillSo.list[i];
                if (skill == null)
                    continue;

                var label = string.IsNullOrWhiteSpace(skill.skillName) ? skill.skillCode : skill.skillName;
                if (!string.IsNullOrWhiteSpace(skill.prefabKey))
                    Add(skill.prefabKey, DefenseAddressableKeyType.Missile, $"스킬 미사일 — {label}");

                if (!string.IsNullOrWhiteSpace(skill.summonPrefabKey))
                    Add(skill.summonPrefabKey, DefenseAddressableKeyType.Prefab, $"스킬 소환 — {label}");
            }
        }

        if (towerSo?.list != null)
        {
            for (int i = 0; i < towerSo.list.Count; i++)
            {
                var tower = towerSo.list[i];
                if (tower == null)
                    continue;

                var key = tower.ResolvePrefabKey();
                Add(key, DefenseAddressableKeyType.Prefab, $"타워 — {tower.towerName}");
            }
        }

        Add("LightningStrikeSharpTallBlue", DefenseAddressableKeyType.Effect, "구름 낙뢰 VFX");
        Add(LinkedSkillSpawner.StormCloudPrefabKey, DefenseAddressableKeyType.Prefab, "번개 구름");

        AddPresentationCatalogKeys(map);
        AddKnownZoneKeys(map);
        AddMonsterModelKeys(map);

        return new List<ResourceRequest>(map.Values);
    }

    private static void AddMonsterModelKeys(Dictionary<string, ResourceRequest> map)
    {
        var monsterSo = AssetDatabase.LoadAssetAtPath<MonsterDataSo>(GoogleSheetDefinitions.MonsterDataAssetPath);
        if (monsterSo?.list == null)
            return;

        for (int i = 0; i < monsterSo.list.Count; i++)
        {
            var monster = monsterSo.list[i];
            if (monster == null || string.IsNullOrWhiteSpace(monster.prefabKey))
                continue;

            var key = monster.prefabKey.Trim();
            if (map.ContainsKey(key))
                continue;

            map.Add(key, new ResourceRequest
            {
                Key = key,
                PreferredType = DefenseAddressableKeyType.Prefab,
                Description = $"몬스터 모델 — {key}"
            });
        }
    }

    private static List<ResourceRequest> CollectMonsterModelRequests()
    {
        var map = new Dictionary<string, ResourceRequest>(StringComparer.OrdinalIgnoreCase);
        AddMonsterModelKeys(map);
        return new List<ResourceRequest>(map.Values);
    }

    private static void AddPresentationCatalogKeys(Dictionary<string, ResourceRequest> map)
    {
        void AddKey(string key, DefenseAddressableKeyType type, string description)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (!map.ContainsKey(key))
            {
                map.Add(key, new ResourceRequest
                {
                    Key = key,
                    PreferredType = type,
                    Description = description
                });
            }
        }

        AddKey("FlamethrowerCartoonyFire", DefenseAddressableKeyType.Effect, "화염방사 연출");
        AddKey("FlamethrowerCartoonyBlue", DefenseAddressableKeyType.Effect, "빙결 분사 연출");
        AddKey("FrozenOrbOBJ", DefenseAddressableKeyType.Missile, "디아 오브 미사일");
        AddKey("FlamethrowerCartoonyGreen", DefenseAddressableKeyType.Effect, "독액 분사 연출");
        AddKey("LaserMissileBlue", DefenseAddressableKeyType.Effect, "빙결 레이저 연출");
        AddKey("FrostExplosionBlue", DefenseAddressableKeyType.Effect, "눈보라 타격 VFX");
        AddKey("ExplosionNovaBlue", DefenseAddressableKeyType.Effect, "얼음 노바 폭발");
        AddKey("GasExplosionFire", DefenseAddressableKeyType.Effect, "화염 가스 폭발");
        AddKey("PoisonExplosion", DefenseAddressableKeyType.Effect, "역병 타격 VFX");
        AddKey("GasFireOBJ", DefenseAddressableKeyType.Missile, "화염 가스 미사일");
        AddKey("GasGreenOBJ", DefenseAddressableKeyType.Missile, "독 가스 미사일");
        AddKey("GunFireYellow", DefenseAddressableKeyType.Effect, "총구 화염 연출");
        AddKey("BulletFatExplosionPink", DefenseAddressableKeyType.Effect, "따발총 타격 연출");
        AddKey("NukeExplosionPink", DefenseAddressableKeyType.Effect, "고폭탄 폭발");
        AddKey("NukeMissileFire", DefenseAddressableKeyType.Missile, "유성 낙하 미사일");
        AddKey("NukeExplosionFire", DefenseAddressableKeyType.Effect, "유성 낙하 폭발");
        AddKey("ExplosionDecalPink", DefenseAddressableKeyType.Effect, "폭발 지면 그을림");
        AddKey("ExplosionDecalFire", DefenseAddressableKeyType.Effect, "화염 지면 그을림");
        AddKey("ExplosionDecalBlue", DefenseAddressableKeyType.Effect, "낙뢰 지면 그을림");
        AddKey("LavaBoiling", DefenseAddressableKeyType.Effect, "화염 지면 장판 루프");
        AddKey("NovaFrost", DefenseAddressableKeyType.Effect, "눈보라 지면 장판 루프");
        AddKey("CFXM3_Snow_Storm", DefenseAddressableKeyType.Effect, "눈보라 폭풍 연출");
    }

    private static void AddKnownZoneKeys(Dictionary<string, ResourceRequest> map)
    {
        Add("Zone_FlameGround", DefenseAddressableKeyType.Prefab, "화염 지속 장판");
        Add("Zone_Blizzard", DefenseAddressableKeyType.Prefab, "눈보라 지속 장판");
        Add("Zone_BlizzardSnow", DefenseAddressableKeyType.Prefab, "스킬 소환 — 눈보라 탄");
        Add("Zone_PoisonCloud", DefenseAddressableKeyType.Prefab, "역병 지속 장판");
        Add("Zone_StormArc", DefenseAddressableKeyType.Prefab, "전류 구름 장판");

        void Add(string key, DefenseAddressableKeyType type, string description)
        {
            if (map.ContainsKey(key))
                return;

            map.Add(key, new ResourceRequest
            {
                Key = key,
                PreferredType = type,
                Description = description
            });
        }
    }

    private static bool TryResolveAssetPath(
        string key,
        DefenseAddressableKeyType preferredType,
        out string assetPath,
        out DefenseAddressableKeyType resolvedType)
    {
        assetPath = null;
        resolvedType = preferredType != DefenseAddressableKeyType.Unknown
            ? preferredType
            : GuessTypeFromKey(key);

        if (string.Equals(key, LinkedSkillSpawner.StormCloudPrefabKey, StringComparison.OrdinalIgnoreCase))
        {
            assetPath = LinkedSkillSpawner.StormCloudPrefabPath;
            resolvedType = DefenseAddressableKeyType.Prefab;
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null;
        }

        if (MonsterSlimePrefabPaths.TryGetAssetPath(key, out assetPath)
            && AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
        {
            resolvedType = DefenseAddressableKeyType.Prefab;
            return true;
        }

        if (MonsterEditorPrefabLoader.TryResolveAssetPath(key, out assetPath)
            && AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
        {
            resolvedType = DefenseAddressableKeyType.Prefab;
            return true;
        }

        if (string.Equals(key, "LightningStrikeSharpTallBlue", StringComparison.OrdinalIgnoreCase))
        {
            assetPath =
                "Assets/Epic Toon FX/Prefabs/Environment/Lightning/Sharp/LightningStrikeSharpTallBlue.prefab";
            resolvedType = DefenseAddressableKeyType.Effect;
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null;
        }

        if (TryResolveKnownPresentationPath(key, out assetPath, out resolvedType))
            return true;

        if (TryResolveZonePrefabPath(key, out assetPath))
        {
            resolvedType = DefenseAddressableKeyType.Prefab;
            return true;
        }

        var towerPath = DefenseTowerPrefabFactory.GetTowerPrefabPath(key);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(towerPath) != null)
        {
            assetPath = towerPath;
            resolvedType = DefenseAddressableKeyType.Prefab;
            return true;
        }

        var exactName = key.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ? key : key + ".prefab";
        string bestPath = null;
        int bestScore = int.MinValue;

        for (int r = 0; r < PrefabSearchRoots.Length; r++)
        {
            if (!AssetDatabase.IsValidFolder(PrefabSearchRoots[r]))
                continue;

            var guids = AssetDatabase.FindAssets($"{key} t:Prefab", new[] { PrefabSearchRoots[r] });
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                    continue;

                var fileName = Path.GetFileName(path);
                if (!fileName.Equals(exactName, StringComparison.OrdinalIgnoreCase)
                    && !fileName.Equals(key + ".prefab", StringComparison.OrdinalIgnoreCase))
                    continue;

                int score = ScorePath(path, key, resolvedType);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPath = path;
                }
            }
        }

        if (string.IsNullOrEmpty(bestPath))
            return false;

        assetPath = bestPath;
        if (preferredType == DefenseAddressableKeyType.Unknown)
            resolvedType = GuessTypeFromPath(bestPath, key);

        return true;
    }

    private static bool TryResolveKnownPresentationPath(
        string key,
        out string assetPath,
        out DefenseAddressableKeyType resolvedType)
    {
        assetPath = key switch
        {
            "FlamethrowerCartoonyFire" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Flamethrower/Cartoon/FlamethrowerCartoonyFire.prefab",
            "FlamethrowerCartoonyBlue" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Flamethrower/Cartoon/FlamethrowerCartoonyBlue.prefab",
            "FrozenOrbOBJ" =>
                "Assets/Game/2Game/Prefab/Defense/Combat/Missiles/FrozenOrbOBJ.prefab",
            "FlamethrowerCartoonyGreen" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Flamethrower/Cartoon/FlamethrowerCartoonyGreen.prefab",
            "LaserMissileBlue" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Missiles/Laser/LaserMissileBlue.prefab",
            "FrostExplosionBlue" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/FrostExplosion/FrostExplosionBlue.prefab",
            "ExplosionNovaBlue" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/NovaExplosion/ExplosionNovaBlue.prefab",
            "GasExplosionFire" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/GasExplosion/GasExplosionFire.prefab",
            "GunFireYellow" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Muzzleflash/- Misc/GunFire/GunFireYellow.prefab",
            "BulletFatExplosionPink" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/BulletFatExplosion/BulletFatExplosionPink.prefab",
            "NukeExplosionPink" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/NukeExplosion/NukeExplosionPink.prefab",
            "NukeMissileFire" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Missiles/Nuke/NukeMissileFire.prefab",
            "NukeExplosionFire" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/NukeExplosion/NukeExplosionFire.prefab",
            "ExplosionDecalPink" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Decals/Explosion Decal/ExplosionDecalPink.prefab",
            "ExplosionDecalFire" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Decals/Explosion Decal/ExplosionDecalFire.prefab",
            "ExplosionDecalBlue" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Decals/Explosion Decal/ExplosionDecalBlue.prefab",
            "LavaBoiling" =>
                "Assets/Epic Toon FX/Prefabs/Environment/Water/Boiling/LavaBoiling.prefab",
            "NovaFrost" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Nova/Frost/NovaFrost.prefab",
            "CFXM3_Snow_Storm" =>
                "Assets/JMO Assets/Cartoon FX/CFX3 Prefabs (Mobile)/Environment/CFXM3_Snow_Storm.prefab",
            "PoisonExplosion" =>
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/- Misc/PoisonExplosion.prefab",
            _ => null
        };

        if (string.IsNullOrEmpty(assetPath))
        {
            resolvedType = DefenseAddressableKeyType.Unknown;
            return false;
        }

        resolvedType = key.Equals("NukeMissileFire", System.StringComparison.OrdinalIgnoreCase)
            || key.EndsWith("OBJ", System.StringComparison.OrdinalIgnoreCase)
            ? DefenseAddressableKeyType.Missile
            : DefenseAddressableKeyType.Effect;

        return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null;
    }

    private static bool TryResolveZonePrefabPath(string key, out string assetPath)
    {
        assetPath = $"{DefenseZonePrefabSetup.ZoneOutputDir}/{key}.prefab";
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(assetPath))
        {
            assetPath = null;
            return false;
        }

        return true;
    }

    private static int ScorePath(string path, string key, DefenseAddressableKeyType type)
    {
        int score = 0;
        if (path.Contains("Missile Prefabs", StringComparison.OrdinalIgnoreCase))
            score += 20;
        if (path.Contains("Epic Toon FX", StringComparison.OrdinalIgnoreCase))
            score += 10;
        if (type == DefenseAddressableKeyType.Effect
            && (path.Contains("Explosion", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Lightning", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Environment", StringComparison.OrdinalIgnoreCase)))
            score += 15;

        if (Path.GetFileNameWithoutExtension(path).Equals(key, StringComparison.OrdinalIgnoreCase))
            score += 5;

        return score;
    }

    private static DefenseAddressableKeyType GuessTypeFromKey(string key)
    {
        if (key.EndsWith("OBJ", StringComparison.OrdinalIgnoreCase))
            return DefenseAddressableKeyType.Missile;

        return DefenseAddressableKeyType.Prefab;
    }

    private static DefenseAddressableKeyType GuessTypeFromPath(string path, string key)
    {
        if (key.EndsWith("OBJ", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Missile Prefabs", StringComparison.OrdinalIgnoreCase))
            return DefenseAddressableKeyType.Missile;

        if (path.Contains("Explosion", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Lightning", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Environment", StringComparison.OrdinalIgnoreCase))
            return DefenseAddressableKeyType.Effect;

        return DefenseAddressableKeyType.Prefab;
    }

    private static bool RegisterAddressable(string assetPath, string address, DefenseAddressableKeyType type)
    {
        return type switch
        {
            DefenseAddressableKeyType.Missile => DefenseAddressableAssetFactory.RegisterMissile(assetPath, address),
            DefenseAddressableKeyType.Effect => DefenseAddressableAssetFactory.RegisterEffect(assetPath, address),
            _ => DefenseAddressableAssetFactory.RegisterPrefab(assetPath, address)
        };
    }

    private static void UpsertKeyEntry(
        DefenseAddressableKeyDataSo keyTable,
        string logicalKey,
        string addressKey,
        string description,
        DefenseAddressableKeyType type)
    {
        switch (type)
        {
            case DefenseAddressableKeyType.Missile:
                DefenseAddressableAssetFactory.UpsertMissileKeyEntry(keyTable, logicalKey, addressKey, description);
                break;
            case DefenseAddressableKeyType.Effect:
                DefenseAddressableAssetFactory.UpsertEffectKeyEntry(keyTable, logicalKey, addressKey, description);
                break;
            default:
                DefenseAddressableAssetFactory.UpsertPrefabKeyEntry(keyTable, logicalKey, addressKey, description);
                break;
        }
    }

    private static void ExportAddressableKeyTsv(DefenseAddressableKeyDataSo keyTable)
    {
        if (keyTable?.list == null)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(ExportPath) ?? "Assets/0UkDefense/1Data/SheetExport");
        var builder = new StringBuilder(keyTable.list.Count * 48);
        for (int i = 0; i < keyTable.list.Count; i++)
        {
            var row = keyTable.list[i];
            if (row == null || string.IsNullOrWhiteSpace(row.key))
                continue;

            if (builder.Length > 0)
                builder.Append('\n');

            builder.Append(row.key).Append('\t')
                .Append(ToSheetType(row.assetType)).Append('\t')
                .Append(row.description ?? string.Empty).Append('\t')
                .Append(row.addressKey ?? row.key).Append('\t')
                .Append(row.note ?? string.Empty);
        }

        File.WriteAllText(ExportPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Debug.Log($"[DefenseCombatResourceSync] AddressableKey TSV 저장 → {ExportPath}");
    }

    private static string ToSheetType(DefenseAddressableKeyType type) => type switch
    {
        DefenseAddressableKeyType.Missile => "Missile",
        DefenseAddressableKeyType.Effect => "Effect",
        _ => "Prefab"
    };

    public sealed class SyncResult
    {
        public int registered;
        public int keyRows;
        public List<string> missing = new();
    }

    private struct ResourceRequest
    {
        public string Key;
        public DefenseAddressableKeyType PreferredType;
        public string Description;
    }
}
#endif
