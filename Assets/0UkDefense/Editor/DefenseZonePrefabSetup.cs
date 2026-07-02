#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 원소별 지속 장판 프리팹 생성 + Addressables / 키 DB 등록.
/// </summary>
public static class DefenseZonePrefabSetup
{
    public const string ZoneOutputDir = "Assets/Game/2Game/Prefab/Defense/Combat/Zones";

    private static readonly ZoneDefinition[] Zones =
    {
        new ZoneDefinition(
            "Zone_FlameGround",
            "화염 장판",
            DefenseSkillElement.Fire,
            false,
            "Assets/Epic Toon FX/Prefabs/Environment/Water/Boiling/LavaBoiling.prefab",
            1f),
        new ZoneDefinition(
            "Zone_Blizzard",
            "눈보라 장판",
            DefenseSkillElement.Ice,
            false,
            "Assets/Epic Toon FX/Prefabs/Combat/Nova/Frost/NovaFrost.prefab",
            1.1f),
        new ZoneDefinition(
            "Zone_PoisonCloud",
            "역병 장판",
            DefenseSkillElement.Poison,
            false,
            "Assets/Epic Toon FX/Prefabs/Combat/Flamethrower/Cartoon/FlamethrowerCartoonyGreen.prefab",
            1f),
        new ZoneDefinition(
            "Zone_StormArc",
            "전류 구름",
            DefenseSkillElement.Lightning,
            true,
            LinkedSkillSpawner.StormCloudPrefabPath,
            0.9f),
    };

    private static readonly (string key, string path, DefenseAddressableKeyType type, string desc)[] PresentationEffects =
    {
        ("FlamethrowerCartoonyFire",
            "Assets/Epic Toon FX/Prefabs/Combat/Flamethrower/Cartoon/FlamethrowerCartoonyFire.prefab",
            DefenseAddressableKeyType.Effect,
            "화염방사 연출"),
        ("FlamethrowerCartoonyGreen",
            "Assets/Epic Toon FX/Prefabs/Combat/Flamethrower/Cartoon/FlamethrowerCartoonyGreen.prefab",
            DefenseAddressableKeyType.Effect,
            "독액 분사 연출"),
        ("LaserMissileBlue",
            "Assets/Epic Toon FX/Prefabs/Combat/Missiles/Laser/LaserMissileBlue.prefab",
            DefenseAddressableKeyType.Effect,
            "빙결 레이저 연출"),
        ("FrostExplosionBlue",
            "Assets/Epic Toon FX/Prefabs/Combat/Explosions/FrostExplosion/FrostExplosionBlue.prefab",
            DefenseAddressableKeyType.Effect,
            "눈보라 타격 VFX"),
        ("PoisonExplosion",
            "Assets/Epic Toon FX/Prefabs/Combat/Explosions/- Misc/PoisonExplosion.prefab",
            DefenseAddressableKeyType.Effect,
            "역병 타격 VFX"),
        ("ExplosionNovaBlue",
            "Assets/Epic Toon FX/Prefabs/Combat/Explosions/NovaExplosion/ExplosionNovaBlue.prefab",
            DefenseAddressableKeyType.Effect,
            "얼음 노바 폭발"),
        ("GasExplosionFire",
            "Assets/Epic Toon FX/Prefabs/Combat/Explosions/GasExplosion/GasExplosionFire.prefab",
            DefenseAddressableKeyType.Effect,
            "화염 가스 폭발"),
        ("LavaBoiling",
            "Assets/Epic Toon FX/Prefabs/Environment/Water/Boiling/LavaBoiling.prefab",
            DefenseAddressableKeyType.Effect,
            "화염 지면 장판 루프"),
        ("NovaFrost",
            "Assets/Epic Toon FX/Prefabs/Combat/Nova/Frost/NovaFrost.prefab",
            DefenseAddressableKeyType.Effect,
            "눈보라 지면 장판 루프"),
        ("GasFireOBJ",
            "Assets/Epic Toon FX/Demo/Missile Prefabs/Gas/GasFireOBJ.prefab",
            DefenseAddressableKeyType.Missile,
            "화염 분사 미사일"),
        ("GasGreenOBJ",
            "Assets/Epic Toon FX/Demo/Missile Prefabs/Gas/GasGreenOBJ.prefab",
            DefenseAddressableKeyType.Missile,
            "독 가스 미사일"),
    };

    public static void CreateZonesFromMenu()
    {
        EnsureAll();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UkDefense] 원소 장판 프리팹 + 연출 VFX Addressables 등록 완료");
    }

    public static void EnsureAll()
    {
        EnsureFolder(ZoneOutputDir);
        var keyTable = AssetDatabase.LoadAssetAtPath<DefenseAddressableKeyDataSo>(
            GoogleSheetDefinitions.AddressableKeyDataAssetPath);

        for (int i = 0; i < Zones.Length; i++)
            CreateZonePrefab(Zones[i], keyTable);

        for (int i = 0; i < PresentationEffects.Length; i++)
            RegisterPresentationAsset(PresentationEffects[i], keyTable);

        if (keyTable != null)
            EditorUtility.SetDirty(keyTable);
    }

    private static void CreateZonePrefab(ZoneDefinition def, DefenseAddressableKeyDataSo keyTable)
    {
        string path = $"{ZoneOutputDir}/{def.Key}.prefab";
        var visualSource = AssetDatabase.LoadAssetAtPath<GameObject>(def.VisualPath);
        if (visualSource == null)
        {
            Debug.LogWarning($"[DefenseZonePrefabSetup] 비주얼 없음: {def.VisualPath}");
            return;
        }

        var root = new GameObject(def.Key);
        try
        {
            var zone = root.AddComponent<DefenseElementZoneSummon>();
            var so = new SerializedObject(zone);
            so.FindProperty("useAirAnchor").boolValue = def.UseAirAnchor;
            so.FindProperty("elementOverride").enumValueIndex = (int)def.Element;
            so.ApplyModifiedPropertiesWithoutUndo();

            var visual = Object.Instantiate(visualSource, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            if (!def.UseAirAnchor && NeedsGroundNovaRotation(visualSource))
                visual.transform.localRotation = DefenseCombatVfxSpawn.GroundNovaRotation;
            visual.transform.localScale = Vector3.one * def.VisualScale;

            DisableLegacyProjectile(visual);

            var prefab = SavePrefab(root, path);
            if (prefab == null)
                return;

            DefenseAddressableAssetFactory.RegisterPrefab(path, def.Key);
            DefenseAddressableAssetFactory.UpsertPrefabKeyEntry(keyTable, def.Key, def.Key, def.Description);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void RegisterPresentationAsset(
        (string key, string path, DefenseAddressableKeyType type, string desc) entry,
        DefenseAddressableKeyDataSo keyTable)
    {
        if (!System.IO.File.Exists(entry.path))
        {
            Debug.LogWarning($"[DefenseZonePrefabSetup] 에셋 없음: {entry.path}");
            return;
        }

        switch (entry.type)
        {
            case DefenseAddressableKeyType.Missile:
                DefenseAddressableAssetFactory.RegisterMissile(entry.path, entry.key);
                DefenseAddressableAssetFactory.UpsertMissileKeyEntry(keyTable, entry.key, entry.key, entry.desc);
                break;
            case DefenseAddressableKeyType.Effect:
                DefenseAddressableAssetFactory.RegisterEffect(entry.path, entry.key);
                DefenseAddressableAssetFactory.UpsertEffectKeyEntry(keyTable, entry.key, entry.key, entry.desc);
                break;
            default:
                DefenseAddressableAssetFactory.RegisterPrefab(entry.path, entry.key);
                DefenseAddressableAssetFactory.UpsertPrefabKeyEntry(keyTable, entry.key, entry.key, entry.desc);
                break;
        }
    }

    private static void DisableLegacyProjectile(GameObject visual)
    {
        var legacy = visual.GetComponent<ETFXProjectileScript>();
        if (legacy != null)
            Object.DestroyImmediate(legacy);

        var stormCloud = visual.GetComponent<DefenseStormCloud>();
        if (stormCloud != null)
            Object.DestroyImmediate(stormCloud);
    }

    private static bool NeedsGroundNovaRotation(GameObject source)
    {
        if (source == null)
            return false;

        float pitch = source.transform.localEulerAngles.x;
        return Mathf.Abs(Mathf.DeltaAngle(pitch, -90f)) > 1f;
    }

    private static GameObject SavePrefab(GameObject source, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        return PrefabUtility.SaveAsPrefabAsset(source, path);
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

    private readonly struct ZoneDefinition
    {
        public readonly string Key;
        public readonly string Description;
        public readonly DefenseSkillElement Element;
        public readonly bool UseAirAnchor;
        public readonly string VisualPath;
        public readonly float VisualScale;

        public ZoneDefinition(
            string key,
            string description,
            DefenseSkillElement element,
            bool useAirAnchor,
            string visualPath,
            float visualScale)
        {
            Key = key;
            Description = description;
            Element = element;
            UseAirAnchor = useAirAnchor;
            VisualPath = visualPath;
            VisualScale = visualScale;
        }
    }
}
#endif
