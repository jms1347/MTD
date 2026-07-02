#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 디아 오브 미사일 프리팹 생성 + Addressables 등록.
/// </summary>
public static class DefenseFrozenOrbPrefabSetup
{
    public const string MissileKey = "FrozenOrbOBJ";
    public const string OutputPath = "Assets/Game/2Game/Prefab/Defense/Combat/Missiles/FrozenOrbOBJ.prefab";

    private const string NovaFrostPath = "Assets/Epic Toon FX/Prefabs/Combat/Nova/Frost/NovaFrost.prefab";
    private const string FrostExplosionPath =
        "Assets/Epic Toon FX/Prefabs/Combat/Explosions/FrostExplosion/FrostExplosionBlue.prefab";

    public static void CreateFromMenu()
    {
        EnsurePrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UkDefense] FrozenOrbOBJ 미사일 프리팹 생성 완료");
    }

    public static void EnsurePrefab()
    {
        EnsureFolder("Assets/Game/2Game/Prefab/Defense/Combat/Missiles");

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPath);
        if (existing != null)
        {
            TuneExistingPrefab();
            RegisterAddressables();
            return;
        }

        var root = new GameObject(MissileKey);

        try
        {
            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            var legacy = root.AddComponent<ETFXProjectileScript>();
            var impactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FrostExplosionPath);
            legacy.impactParticle = impactPrefab;
            legacy.projectileParticle = null;
            legacy.muzzleParticle = null;
            legacy.colliderRadius = 0.52f;
            legacy.collideOffset = 0.08f;

            var emitter = root.AddComponent<DefenseFrozenOrbEmitter>();

            var orbVisual = new GameObject("OrbVisual").transform;
            orbVisual.SetParent(root.transform, false);
            orbVisual.localPosition = Vector3.zero;
            orbVisual.localRotation = Quaternion.identity;

            var orbCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbCore.name = "OrbCore";
            orbCore.transform.SetParent(orbVisual, false);
            orbCore.transform.localPosition = Vector3.zero;
            orbCore.transform.localScale = Vector3.one * 0.85f;
            Object.DestroyImmediate(orbCore.GetComponent<Collider>());

            var coreRenderer = orbCore.GetComponent<Renderer>();
            if (coreRenderer != null)
            {
                var material = CreateOrbCoreMaterialAsset();
                if (material != null)
                    coreRenderer.sharedMaterial = material;
            }

            var novaSource = AssetDatabase.LoadAssetAtPath<GameObject>(NovaFrostPath);
            if (novaSource != null)
            {
                var aura = Object.Instantiate(novaSource, orbVisual);
                aura.name = "OrbAura";
                aura.transform.localPosition = Vector3.zero;
                aura.transform.localRotation = Quaternion.identity;
                aura.transform.localScale = Vector3.one * 0.28f;
            }

            emitter.BindOrbVisual(orbVisual);

            var shardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/2Game/Prefab/Defense/Combat/Missiles/MissileIceOBJ.prefab");
            if (shardPrefab == null)
            {
                shardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Epic Toon FX/Demo/Missile Prefabs/Frost/FrostMissileOBJ.prefab");
            }

            var emitterSo = new SerializedObject(emitter);
            emitterSo.FindProperty("shardMissileKey").stringValue = DefenseFrozenOrbEmitter.DefaultShardMissileKey;
            emitterSo.FindProperty("shardMissilePrefab").objectReferenceValue = shardPrefab;
            emitterSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, OutputPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        RegisterAddressables();
    }

    private static void TuneExistingPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(OutputPath);
        try
        {
            var rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = false;
                rigidbody.useGravity = false;
            }

            var emitter = root.GetComponent<DefenseFrozenOrbEmitter>();
            if (emitter != null)
            {
                var shardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Game/2Game/Prefab/Defense/Combat/Missiles/MissileIceOBJ.prefab");
                if (shardPrefab == null)
                {
                    shardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/Epic Toon FX/Demo/Missile Prefabs/Frost/FrostMissileOBJ.prefab");
                }

                var so = new SerializedObject(emitter);
                so.FindProperty("shardEmitInterval").floatValue = 0.05f;
                so.FindProperty("directionSlots").intValue = 8;
                so.FindProperty("shardSpeed").floatValue = 16f;
                so.FindProperty("shardDamageRatio").floatValue = 0.24f;
                so.FindProperty("scaleDrainPerShot").floatValue = 0.03f;
                so.FindProperty("shardLifetime").floatValue = 0.7f;
                so.FindProperty("shardMissileKey").stringValue = DefenseFrozenOrbEmitter.DefaultShardMissileKey;
                so.FindProperty("shardMissilePrefab").objectReferenceValue = shardPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var orbVisual = root.transform.Find("OrbVisual");
            if (orbVisual != null)
            {
                var core = orbVisual.Find("OrbCore");
                if (core != null)
                {
                    core.localScale = Vector3.one * 0.85f;
                    var renderer = core.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        var material = CreateOrbCoreMaterialAsset();
                        if (material != null)
                            renderer.sharedMaterial = material;
                    }
                }

                var aura = orbVisual.Find("OrbAura");
                if (aura != null)
                    aura.localScale = Vector3.one * 0.28f;
            }

            PrefabUtility.SaveAsPrefabAsset(root, OutputPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Material CreateOrbCoreMaterialAsset()
    {
        const string materialPath = "Assets/Game/2Game/Prefab/Defense/Combat/Missiles/FrozenOrbCore.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (existing != null)
            return existing;

        var shader = Shader.Find("Standard");
        if (shader == null)
            return null;

        var material = new Material(shader)
        {
            color = new Color(0.55f, 0.88f, 1f, 0.92f)
        };
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(0.25f, 0.65f, 1f) * 1.35f);
        AssetDatabase.CreateAsset(material, materialPath);
        return material;
    }

    private static void RegisterAddressables()
    {
        DefenseAddressableAssetFactory.RegisterMissile(OutputPath, MissileKey);

        var keyTable = AssetDatabase.LoadAssetAtPath<DefenseAddressableKeyDataSo>(
            GoogleSheetDefinitions.AddressableKeyDataAssetPath);
        if (keyTable == null)
            return;

        DefenseAddressableAssetFactory.UpsertMissileKeyEntry(
            keyTable,
            MissileKey,
            MissileKey,
            "디아 오브 — 빙결 구체 미사일");
        EditorUtility.SetDirty(keyTable);
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
}
#endif
