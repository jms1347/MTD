#if UNITY_EDITOR
using System.IO;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using AssetKits.ParticleImage;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CwslGameSceneSetup
{
    private const string RootFolder = "Assets/0CwSL";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string ScenePath = RootFolder + "/Scenes/CwslGameScene.unity";
    private const string AssetsPath = RootFolder + "/Data/CwslGameAssets.asset";
    private const string NetworkPrefabsPath = RootFolder + "/Data/CwslNetworkPrefabs.asset";

    [MenuItem("Tools/CwSL/Setup Game Scene", false, 10)]
    public static void SetupGameScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolders();
        EnsureLayers();
        EnsureGoldCoinFlyPrefab();
        var assets = EnsureGameAssets();
        var playerPrefab = BuildPlayerPrefab();
        var rangedPrefab = BuildMonsterPrefab(CwslMonsterType.Ranged, typeof(CwslRangedMonster), 0.55f);
        var suicidePrefab = BuildMonsterPrefab(CwslMonsterType.Suicide, typeof(CwslSuicideMonster), 0.5f);
        var meleePrefab = BuildMonsterPrefab(CwslMonsterType.Melee, typeof(CwslMeleeMonster), 0.6f);
        var bossPrefab = BuildBossPrefab();
        var projectilePrefab = BuildProjectilePrefab();
        var playerMissilePrefab = BuildPlayerMissilePrefab();
        var goldPickupPrefab = BuildGoldPickupPrefab();
        var graveVisualPrefab = BuildGraveVisualPrefab();

        assets.playerPrefab = playerPrefab;
        assets.rangedMonsterPrefab = rangedPrefab;
        assets.suicideMonsterPrefab = suicidePrefab;
        assets.meleeMonsterPrefab = meleePrefab;
        assets.bossPrefab = bossPrefab;
        assets.projectilePrefab = projectilePrefab;
        assets.playerMissilePrefab = playerMissilePrefab;
        assets.goldPickupPrefab = goldPickupPrefab;
        assets.graveVisualPrefab = graveVisualPrefab;
        assets.darkMissileVfx = LoadPrefab(CwslVfxPaths.RangedProjectileVisual);
        assets.playerMissileVfx = LoadPrefab(CwslVfxPaths.PlayerMissileVisual);
        assets.gunMuzzleVfx = LoadPrefab(CwslVfxPaths.GunMuzzleFlash);
        assets.fortifyAuraVfx = LoadPrefab(CwslVfxPaths.FortifyAura);
        assets.fortifyBlockVfx = LoadPrefab(CwslVfxPaths.FortifyBlock);
        assets.meteorFallVfx = LoadPrefab(CwslVfxPaths.MeteorFall);
        assets.meteorImpactVfx = LoadPrefab(CwslVfxPaths.MeteorImpact);
        assets.meteorBurnVfx = LoadPrefab(CwslVfxPaths.MeteorBurn);
        assets.suicideExplosionVfx = LoadPrefab(CwslVfxPaths.SuicideExplosion);
        assets.meleeHitVfx = LoadPrefab(CwslVfxPaths.MeleeHit);
        assets.enemyDeathVfx = LoadPrefab(CwslVfxPaths.EnemyDeath);
        assets.bossDeathVfx = LoadPrefab(CwslVfxPaths.BossDeath);
        assets.playerDeathVfx = LoadPrefab(CwslVfxPaths.PlayerDeath);
        assets.goldBurstVfx = LoadPrefab(CwslVfxPaths.GoldBurst);
        assets.goldMagnetTrailVfx = LoadPrefab(CwslVfxPaths.GoldMagnetTrail);
        assets.goldPickupSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.CoinDropSound);
        EditorUtility.SetDirty(assets);

        var networkPrefabs = EnsureNetworkPrefabsList();
        RegisterPrefab(networkPrefabs, playerPrefab);
        RegisterPrefab(networkPrefabs, rangedPrefab);
        RegisterPrefab(networkPrefabs, suicidePrefab);
        RegisterPrefab(networkPrefabs, meleePrefab);
        RegisterPrefab(networkPrefabs, bossPrefab);
        RegisterPrefab(networkPrefabs, projectilePrefab);
        RegisterPrefab(networkPrefabs, playerMissilePrefab);
        RegisterPrefab(networkPrefabs, goldPickupPrefab);
        EditorUtility.SetDirty(networkPrefabs);

        BuildScene(assets, networkPrefabs);
        UpdateBuildSettings();
        WireLobbySceneName();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CwSL] 게임 씬·프리팹·네트워크 설정 완료. LobbyScene → CwslGameScene 흐름이 연결되었습니다.");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(AssetsPath)!);
        Directory.CreateDirectory($"{RootFolder}/Resources/CwslGold");
    }

    private static void EnsureGoldCoinFlyPrefab()
    {
        const string outputPath = RootFolder + "/Resources/CwslGold/CoinFlyParticle.prefab";
        const string sourcePath = "Assets/AssetKits/ParticleImage/Demo/Prefabs/CoinAttraction.prefab";

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        if (existing != null)
        {
            var tempExisting = (GameObject)PrefabUtility.InstantiatePrefab(existing);
            SanitizeCoinFlyPrefab(tempExisting);
            PrefabUtility.SaveAsPrefabAsset(tempExisting, outputPath);
            Object.DestroyImmediate(tempExisting);
            return;
        }

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
        {
            Debug.LogWarning($"[CwSL] CoinAttraction 프리팹을 찾을 수 없습니다: {sourcePath}");
            return;
        }

        var particle = source.transform.Find("Particle Image");
        if (particle == null)
        {
            Debug.LogWarning("[CwSL] CoinAttraction 안의 Particle Image를 찾을 수 없습니다.");
            return;
        }

        var temp = Object.Instantiate(particle.gameObject);
        temp.name = "CoinFlyParticle";
        SanitizeCoinFlyPrefab(temp);
        PrefabUtility.SaveAsPrefabAsset(temp, outputPath);
        Object.DestroyImmediate(temp);
    }

    private static void SanitizeCoinFlyPrefab(GameObject root)
    {
        var particle = root.GetComponent<ParticleImage>();
        if (particle == null)
            return;

        particle.PlayMode = AssetKits.ParticleImage.Enumerations.PlayMode.None;
        particle.attractorEnabled = false;
        particle.attractorTarget = null;
    }

    private static void EnsureLayers()
    {
        EnsureLayer(CwslGameConstants.LayerPlayer);
        EnsureLayer(CwslGameConstants.LayerMonster);
        EnsureLayer(CwslGameConstants.LayerProjectile);
        EnsureLayer(CwslGameConstants.LayerGold);
    }

    private static void EnsureLayer(string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (var i = 8; i < 32; i++)
        {
            var property = layers.GetArrayElementAtIndex(i);
            if (property.stringValue == layerName)
                return;
        }

        for (var i = 8; i < 32; i++)
        {
            var property = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(property.stringValue))
                continue;

            property.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            return;
        }

        Debug.LogWarning($"[CwSL] 레이어 슬롯이 부족합니다: {layerName}");
    }

    private static CwslGameAssets EnsureGameAssets()
    {
        var assets = AssetDatabase.LoadAssetAtPath<CwslGameAssets>(AssetsPath);
        if (assets != null)
            return assets;

        assets = ScriptableObject.CreateInstance<CwslGameAssets>();
        AssetDatabase.CreateAsset(assets, AssetsPath);
        return assets;
    }

    private static NetworkPrefabsList EnsureNetworkPrefabsList()
    {
        var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
        if (list != null)
            return list;

        list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
        AssetDatabase.CreateAsset(list, NetworkPrefabsPath);
        return list;
    }

    private static void RegisterPrefab(NetworkPrefabsList list, GameObject prefab)
    {
        if (prefab == null)
            return;

        if (prefab.GetComponent<NetworkObject>() == null)
            return;

        if (list.Contains(prefab))
            return;

        list.Add(new NetworkPrefab { Prefab = prefab });
    }

    private static GameObject LoadPrefab(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    private static GameObject BuildPlayerPrefab()
    {
        var root = new GameObject("CwslPlayer");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerPlayer);

        var agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.height = 2f;
        agent.radius = 0.45f;
        agent.baseOffset = 1f;
        agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;

        var bodyCollider = root.AddComponent<CapsuleCollider>();
        bodyCollider.height = 2f;
        bodyCollider.radius = 0.45f;
        bodyCollider.center = new Vector3(0f, 1f, 0f);

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslPlayerHealth>();
        root.AddComponent<CwslPlayerGold>();
        root.AddComponent<CwslPlayerMovement>();
        root.AddComponent<CwslPlayerVisualScale>();
        root.AddComponent<CwslPlayerSelection>();
        root.AddComponent<CwslPlayerController>();
        root.AddComponent<CwslPlayerCombat>();
        root.AddComponent<CwslPlayerInput>();
        root.AddComponent<CwslPlayerSkills>();
        root.AddComponent<CwslPlayerCharacter>();
        root.AddComponent<CwslTankFortifySkill>();
        root.AddComponent<CwslMissileTankSkill>();
        root.AddComponent<CwslRedMageMeteorSkill>();
        root.AddComponent<CwslMomentumRammerSkill>();
        root.AddComponent<CwslPlayerCannonAim>();
        root.AddComponent<CwslPlayerShieldFortifyVisual>();
        root.AddComponent<CwslPlayerShieldBubble>();
        root.AddComponent<CwslPlayerFortifyVfx>();
        root.AddComponent<CwslPlayerGoldGift>();
        root.AddComponent<CwslPlayerGrave>();
        root.AddComponent<CwslPlayerHealthBar>();
        root.AddComponent<CwslPlayerSpawnVisuals>();
        root.AddComponent<CwslPlayerSpawnOffset>();
        root.AddComponent<CwslPlayerVision>();
        root.AddComponent<CwslLocalPlayerHud>();

        return SavePrefab(root, $"{PrefabFolder}/CwslPlayer.prefab");
    }

    private static GameObject BuildGoldPickupPrefab()
    {
        var root = new GameObject("CwslGoldPickup");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerGold);

        var collider = root.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.55f;
        collider.center = new Vector3(0f, 0.35f, 0f);

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coin.transform.SetParent(root.transform, false);
        coin.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        coin.transform.localScale = Vector3.one * 0.45f;
        Object.DestroyImmediate(coin.GetComponent<Collider>());
        var renderer = coin.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CwslMaterialUtil.CreateColored(new Color(1f, 0.84f, 0.1f));

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslGoldPickup>();

        return SavePrefab(root, $"{PrefabFolder}/CwslGoldPickup.prefab");
    }

    private static GameObject BuildGraveVisualPrefab()
    {
        var root = new GameObject("CwslGraveVisual");
        CwslGraveVisualBuilder.Build(root.transform);
        return SavePrefab(root, $"{PrefabFolder}/CwslGraveVisual.prefab");
    }

    private static GameObject BuildMonsterPrefab(CwslMonsterType type, System.Type behaviourType, float radius)
    {
        var root = new GameObject($"CwslMonster_{type}");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerMonster);

        var collider = root.AddComponent<CapsuleCollider>();
        collider.height = radius * 2.4f;
        collider.radius = radius;
        collider.center = new Vector3(0f, radius * 1.1f, 0f);
        collider.isTrigger = true;

        if (type == CwslMonsterType.Suicide)
        {
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslMonsterHealth>();
        root.AddComponent(behaviourType);
        if (type == CwslMonsterType.Ranged)
            root.AddComponent<CwslRangedCannonAim>();

        CwslMonsterVisualBuilder.Build(root.transform, type);
        var monster = root.GetComponent<CwslMonsterBase>();
        monster?.Initialize(type);

        return SavePrefab(root, $"{PrefabFolder}/CwslMonster_{type}.prefab");
    }

    private static GameObject BuildBossPrefab()
    {
        var root = new GameObject("CwslBoss_Hongmyeongbo");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerMonster);

        var collider = root.AddComponent<CapsuleCollider>();
        collider.height = 4.2f;
        collider.radius = 1.4f;
        collider.center = new Vector3(0f, 2.1f, 0f);
        collider.isTrigger = true;

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslMonsterHealth>();
        root.AddComponent<CwslBossHongmyeongbo>();
        CwslMonsterVisualBuilder.Build(root.transform, CwslMonsterType.BossHongmyeongbo);

        return SavePrefab(root, $"{PrefabFolder}/CwslBoss_Hongmyeongbo.prefab");
    }

    private static GameObject BuildProjectilePrefab()
    {
        var root = new GameObject("CwslProjectile");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerProjectile);

        var collider = root.AddComponent<SphereCollider>();
        collider.radius = 0.25f;
        collider.isTrigger = true;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "HitProxy";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * 0.2f;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().enabled = false;

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslMonsterProjectile>();
        root.AddComponent<CwslProjectileVisual>();

        return SavePrefab(root, $"{PrefabFolder}/CwslProjectile.prefab");
    }

    private static GameObject BuildPlayerMissilePrefab()
    {
        var root = new GameObject("CwslPlayerMissile");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerProjectile);

        var collider = root.AddComponent<SphereCollider>();
        collider.radius = 0.22f;
        collider.isTrigger = true;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = "ArrowShaft";
        shaft.transform.SetParent(root.transform, false);
        shaft.transform.localPosition = new Vector3(0f, 0f, 0.18f);
        shaft.transform.localScale = new Vector3(0.1f, 0.1f, 0.42f);
        Object.DestroyImmediate(shaft.GetComponent<Collider>());
        CwslMaterialUtil.ApplyColor(shaft.GetComponent<Renderer>(), new Color(0.85f, 0.72f, 0.35f));

        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "ArrowHead";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 0f, 0.44f);
        head.transform.localScale = new Vector3(0.14f, 0.14f, 0.12f);
        Object.DestroyImmediate(head.GetComponent<Collider>());
        CwslMaterialUtil.ApplyColor(head.GetComponent<Renderer>(), new Color(0.72f, 0.76f, 0.82f));

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "HitProxy";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * 0.12f;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().enabled = false;

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslPlayerProjectile>();
        root.AddComponent<CwslPlayerProjectileVisual>();

        return SavePrefab(root, $"{PrefabFolder}/CwslPlayerMissile.prefab");
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void BuildScene(CwslGameAssets assets, NetworkPrefabsList networkPrefabs)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "ArenaPlane";
        plane.transform.localScale = new Vector3(8f, 1f, 8f);
        var planeRenderer = plane.GetComponent<Renderer>();
        if (planeRenderer != null)
            planeRenderer.sharedMaterial = CwslMaterialUtil.CreateMatteColored(new Color(0.2f, 0.28f, 0.22f));

        var navMeshSurface = plane.AddComponent<NavMeshSurface>();
        navMeshSurface.center = new Vector3(0f, 0f, 0f);
        navMeshSurface.size = new Vector3(80f, 20f, 80f);
        navMeshSurface.BuildNavMesh();

        var light = new GameObject("Directional Light");
        var directional = light.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 0.45f;
        directional.color = new Color(0.45f, 0.5f, 0.65f);
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.13f, 0.16f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.05f);
        // 카메라 거리(~24) 기준 — 플레이어 주변은 보이고 멀리만 어두움
        RenderSettings.fogStartDistance = 26f;
        RenderSettings.fogEndDistance = 40f;

        var networkRoot = new GameObject("NetworkManager");
        var networkManager = networkRoot.AddComponent<NetworkManager>();
        var transport = networkRoot.AddComponent<UnityTransport>();
        transport.ConnectionData.Port = CwslGameConstants.GamePort;
        networkManager.NetworkConfig.PlayerPrefab = assets.playerPrefab;
        networkManager.NetworkConfig.NetworkTransport = transport;
        if (networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists == null)
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists = new System.Collections.Generic.List<NetworkPrefabsList>();
        networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
        networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(networkPrefabs);

        var bootstrap = networkRoot.AddComponent<CwslNetworkBootstrap>();
        var bootstrapSerialized = new SerializedObject(bootstrap);
        bootstrapSerialized.FindProperty("gameAssets").objectReferenceValue = assets;
        bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

        var systems = new GameObject("CwslGameSystems");
        systems.AddComponent<NetworkObject>();
        systems.AddComponent<CwslKarmaSystem>();
        systems.AddComponent<CwslMonsterSpawner>();
        systems.AddComponent<CwslGameSession>();
        systems.AddComponent<CwslGameFlow>();
        systems.AddComponent<CwslNetworkPoolService>();

        var session = systems.GetComponent<CwslGameSession>();
        var sessionSerialized = new SerializedObject(session);
        sessionSerialized.FindProperty("assets").objectReferenceValue = assets;
        sessionSerialized.FindProperty("monsterSpawner").objectReferenceValue = systems.GetComponent<CwslMonsterSpawner>();
        sessionSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void UpdateBuildSettings()
    {
        var scenes = new[]
        {
            "Assets/Game/0Scene/LobbyScene.unity",
            ScenePath,
            "Assets/Game/0Scene/SplashScene.unity",
            "Assets/Game/0Scene/LoadingScene.unity",
            "Assets/Game/0Scene/TestScene.unity"
        };

        var buildScenes = new EditorBuildSettingsScene[scenes.Length];
        for (var i = 0; i < scenes.Length; i++)
            buildScenes[i] = new EditorBuildSettingsScene(scenes[i], i <= 1);

        EditorBuildSettings.scenes = buildScenes;
    }

    private static void WireLobbySceneName()
    {
        var lobbyScene = EditorSceneManager.OpenScene("Assets/Game/0Scene/LobbyScene.unity", OpenSceneMode.Single);
        var bootstrap = Object.FindFirstObjectByType<LobbySceneBootstrap>();
        if (bootstrap == null)
            return;

        var network = Object.FindFirstObjectByType<LobbyNetworkManager>();
        if (network == null)
        {
            var managerObject = new GameObject("LobbyNetworkManager");
            network = managerObject.AddComponent<LobbyNetworkManager>();
        }

        network.GameSceneName = CwslGameConstants.GameSceneName;
        EditorUtility.SetDirty(network);
        EditorSceneManager.SaveScene(lobbyScene);
    }
}
#endif
