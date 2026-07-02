using UnityEngine;
using UnityEngine.Serialization;

public class DefenseSceneSetup : MonoBehaviour
{
    [Header("Manager Prefabs")]
    [SerializeField] private GameObject dataManagerPrefab;
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject missilePoolManagerPrefab;
    [SerializeField] private GameObject nexusManagerPrefab;
    [SerializeField] private GameObject towerStatsManagerPrefab;
    [SerializeField] private GameObject towerManagerPrefab;
    [FormerlySerializedAs("enemyManagerPrefab")]
    [SerializeField] private GameObject stageManagerPrefab;

    [Header("Combat")]
    [SerializeField] private DefenseCombatCatalog combatCatalog;

    [Header("Layout")]
    [SerializeField] private DefenseMapLayout mapLayout;
    [SerializeField] private Vector3 arenaCenter = Vector3.zero;
    [SerializeField] private DefenseTowerLayout towerLayout;
    [SerializeField] private float towerHeight = 0.6f;
    [SerializeField] private float nexusHealth = 10000f;
    [Tooltip("mapLayout 미사용 시 Plane 스케일. 1=10m, 8≈80m 한 변")]
    [SerializeField] private float groundScale = 8f;

    [Header("Farm")]
    [SerializeField] private GameObject farmGoldBurstPrefab;
    [SerializeField] private GameObject farmDrillDebrisPrefab;
    [SerializeField] private AudioClip farmDrillSound;
    [SerializeField] private AudioClip farmBuildHammerSound;
    [SerializeField] private AudioClip farmGoldCoinSound;

    [Header("Camera & HUD")]
    [SerializeField] private GameObject defenseHudPrefab;
    [Tooltip("미니맵·스폰 반경 기준 맵 반경 (월드 단위)")]
    [SerializeField] private float mapHalfExtent = 40f;
    [SerializeField] private float cameraOrthographicSize = 26f;

    [Header("전투 중 플레이어 행동")]
    [SerializeField] private DefensePlayerBattleRules battlePlayerRules = new();

    private void Awake()
    {
        EnsureDataManager();
        EnsureManagersLoaded();
        DefenseManagerLoader.EnsureStageManager(stageManagerPrefab);
        EnsureGameManager();
        FarmDrillVfx.Initialize(farmGoldBurstPrefab, farmDrillDebrisPrefab);
        FarmDrillAudio.Initialize(ResolveFarmDrillSound());
        FarmBuildAudio.Initialize(ResolveFarmBuildHammerSound());
        FarmGoldAudio.Initialize(ResolveFarmGoldCoinSound());
        ApplyMapLayoutSettings();
        BuildGroundOrMap();
        DefenseMapPathfinder.Initialize(mapLayout);
        SetupDefense();
        SetupPlayerAndFarm();
        SetupCamera();
        SetupHUD();
        SetupBuildSystem();
        SetupRoguelike();
        SetupStageTimer();
    }

    private void EnsureDataManager()
    {
        if (DataManager.Instance != null)
            return;

        if (dataManagerPrefab != null)
        {
            DataManager.Load(dataManagerPrefab);
            return;
        }

        Debug.LogWarning(
            "[DefenseSceneSetup] DataManager가 없습니다. Splash 경로로 플레이하거나 dataManagerPrefab을 연결해 주세요. " +
            "몬스터 스폰이 동작하지 않을 수 있습니다.");
    }

    private void EnsureGameManager()
    {
        if (GameManager.Instance != null)
            return;

        if (gameManagerPrefab != null)
            GameManager.Load(gameManagerPrefab);
    }

    private void EnsureManagersLoaded()
    {
        if (DefenseManagerLoader.AreAllLoaded())
            return;

        DefenseManagerLoader.LoadAll(
            missilePoolManagerPrefab,
            nexusManagerPrefab,
            towerStatsManagerPrefab,
            towerManagerPrefab,
            stageManagerPrefab);
    }

    private void ApplyMapLayoutSettings()
    {
        if (mapLayout == null)
            return;

        arenaCenter = mapLayout.GetNexusWorld();
        mapHalfExtent = mapLayout.MapHalfExtent;
        mapLayout.SyncTowerLayout();

        if (mapLayout.towerLayout != null)
            towerLayout = mapLayout.towerLayout;

        if (mapLayout.autoGenerateLanes)
            DefenseMonsterLaneRegistry.Rebuild(mapLayout, paintTiles: true);
        else
            DefenseMonsterLaneRegistry.Rebuild(mapLayout, paintTiles: false);
    }

    private void BuildGroundOrMap()
    {
        if (mapLayout != null)
        {
            DefenseMapBuilder.Build(mapLayout);
            return;
        }

        CreateGround();
    }

    private void SetupDefense()
    {
        combatCatalog = ResolveCombatCatalog();
        if (combatCatalog == null)
        {
            Debug.LogError("[DefenseSceneSetup] combatCatalog가 없습니다. Tools → UkDefense → Setup TestScene을 실행하거나 DefenseCombatCatalog SO를 연결해 주세요.");
        }
        else
        {
            combatCatalog.Activate();
        }

        MissilePoolManager.Instance.Initialize(DefenseMissileResolver.CollectPoolPrefabs());

        TowerStatsManager.RefreshFromSheetIfExists();

        NexusManager.Instance.BuildScene(arenaCenter, nexusHealth);

        var towers = ResolveTowerSpawnData();
        var towerCenter = towerLayout != null
            ? towerLayout.TowerOrigin
            : arenaCenter + new Vector3(0f, towerHeight, 0f);
        TowerManager.Instance.BuildScene(towerCenter, towers);

        StageManager.Instance.ConfigureScene(arenaCenter, mapHalfExtent * 0.88f);
    }

    private TowerSpawnData[] ResolveTowerSpawnData()
    {
        TowerSpawnData[] towers = towerLayout != null
            ? towerLayout.ToSpawnArray()
            : DefenseTowerLayoutDefaults.CreateNearFarmTowers().ToArray();

        if (combatCatalog != null)
            DefenseTowerLayoutApplier.ApplyCombatReferences(towers, combatCatalog);

        return towers;
    }

    private DefenseCombatCatalog ResolveCombatCatalog()
    {
        if (combatCatalog != null)
            return combatCatalog;

        combatCatalog = DefenseCombatCatalog.LoadFallback();
        if (combatCatalog != null)
            Debug.LogWarning("[DefenseSceneSetup] combatCatalog 슬롯이 비어 있어 기본 DefenseCombatCatalog SO를 자동 로드했습니다.");

        return combatCatalog;
    }

    public void ApplyBuildTowerPrefabs(TowerSpawnData tower)
    {
        DefenseTowerLayoutApplier.ApplyCombatReferences(tower, combatCatalog);
    }

    public DefenseCombatCatalog CombatCatalog => combatCatalog;

    private void SetupPlayerAndFarm()
    {
        if (mapLayout != null)
        {
            if (FindFirstObjectByType<PlayerCharacterController>() == null)
                PlayerCharacterController.Create(mapLayout.GetPlayerSpawnWorld());
            return;
        }

        FarmAreaBuilder.Build(arenaCenter);

        if (FindFirstObjectByType<PlayerCharacterController>() != null)
            return;

        Vector3 spawnPosition = arenaCenter + new Vector3(3.2f, 0f, -1.8f);
        PlayerCharacterController.Create(spawnPosition);
    }

    private void CreateGround()
    {
        var existing = GameObject.Find("DefenseGround");
        if (existing != null)
        {
            existing.tag = "Ground";
            existing.layer = LayerMask.NameToLayer("Default");
            return;
        }

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "DefenseGround";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(groundScale, 1f, groundScale);

        var collider = ground.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = false;

        var renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.35f, 0.42f, 0.32f);
            renderer.material = material;
        }

        ground.tag = "Ground";
        ground.layer = LayerMask.NameToLayer("Default");
    }

    private void SetupCamera()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        // 쿼터뷰 고정 카메라 컴포넌트 부착
        var isoCamera = camera.GetComponent<DefenseIsometricCamera>();
        if (isoCamera == null)
            isoCamera = camera.gameObject.AddComponent<DefenseIsometricCamera>();

        // 넥서스 생성 후 추적 대상 연결
        if (NexusManager.Instance?.NexusTransform != null)
            isoCamera.SetFollowTarget(NexusManager.Instance.NexusTransform, cameraOrthographicSize);

        if (camera.GetComponent<DefenseCameraControlManager>() == null)
            camera.gameObject.AddComponent<DefenseCameraControlManager>();
    }

    private void SetupBuildSystem()
    {
        var buildManager = FindFirstObjectByType<DefenseBuildManager>();
        if (buildManager == null)
        {
            var buildObject = new GameObject("DefenseBuildManager");
            buildObject.transform.SetParent(transform, false);
            buildManager = buildObject.AddComponent<DefenseBuildManager>();
            buildObject.AddComponent<DefenseBuildPreview>();
        }

        buildManager.Configure(mapLayout, this);
    }

    private void SetupHUD()
    {
        var hudSetup = FindFirstObjectByType<DefenseHUDSetup>();
        if (hudSetup == null)
        {
            if (defenseHudPrefab == null)
            {
                Debug.LogWarning("[DefenseSceneSetup] defenseHudPrefab이 비어 있습니다. HUD 프리팹을 연결해 주세요.");
                return;
            }

            var hudInstance = Instantiate(defenseHudPrefab);
            hudSetup = hudInstance.GetComponent<DefenseHUDSetup>();
        }

        hudSetup?.Configure(arenaCenter, mapHalfExtent);
    }

    private void SetupStageTimer()
    {
        DefenseStageTimerManager timer = FindFirstObjectByType<DefenseStageTimerManager>();
        if (timer == null)
        {
            var timerObject = new GameObject("DefenseStageTimerManager");
            timerObject.transform.SetParent(transform, false);
            timer = timerObject.AddComponent<DefenseStageTimerManager>();
        }

        timer.ConfigureBattlePlayerRules(battlePlayerRules);
        timer.BeginGame();

        var stageTimerUi = FindFirstObjectByType<DefenseStageTimerUI>(FindObjectsInactive.Include);
        stageTimerUi?.SyncFromManager();
    }

    private void SetupRoguelike()
    {
        RoguelikeCardManager manager = FindFirstObjectByType<RoguelikeCardManager>();
        if (manager == null)
        {
            var managerObject = new GameObject("RoguelikeCardManager");
            managerObject.transform.SetParent(transform, false);
            manager = managerObject.AddComponent<RoguelikeCardManager>();
        }

        var hud = FindFirstObjectByType<DefenseHUDSetup>(FindObjectsInactive.Include);
        if (hud == null)
            return;

        var selectUi = hud.GetComponentInChildren<RoguelikeCardSelectUI>(true);
        if (selectUi == null)
            selectUi = hud.gameObject.AddComponent<RoguelikeCardSelectUI>();

        var magicUi = hud.GetComponentInChildren<RoguelikeMagicHandUI>(true);
        if (magicUi == null)
            magicUi = hud.gameObject.AddComponent<RoguelikeMagicHandUI>();

        var magicTarget = FindFirstObjectByType<RoguelikeMagicTargetController>();
        if (magicTarget == null)
        {
            var targetObject = new GameObject("RoguelikeMagicTargetController");
            targetObject.transform.SetParent(manager.transform, false);
            magicTarget = targetObject.AddComponent<RoguelikeMagicTargetController>();
        }

        selectUi.Initialize(hud.transform);
        magicUi.Initialize(hud.transform);
        manager.Initialize(selectUi, magicUi, magicTarget);

        if (!selectUi.IsReady)
            Debug.LogWarning("[DefenseSceneSetup] RoguelikeCardSelectUI가 초기화되지 않았습니다. DefenseHUD Canvas를 확인하세요.");
    }

    private AudioClip ResolveFarmDrillSound()
    {
        if (farmDrillSound != null)
            return farmDrillSound;

        Debug.LogWarning("[DefenseSceneSetup] farmDrillSound이 비어 있습니다. Assets/Game/0Sound/drillsound.mp3를 연결하거나 Setup TestScene을 실행해 주세요.");
        return null;
    }

    private AudioClip ResolveFarmBuildHammerSound()
    {
        if (farmBuildHammerSound != null)
            return farmBuildHammerSound;

        Debug.LogWarning("[DefenseSceneSetup] farmBuildHammerSound이 비어 있습니다. Assets/Game/0Sound/hammerplay.mp3를 연결하거나 Setup TestScene을 실행해 주세요.");
        return null;
    }

    private AudioClip ResolveFarmGoldCoinSound()
    {
        if (farmGoldCoinSound != null)
            return farmGoldCoinSound;

        Debug.LogWarning("[DefenseSceneSetup] farmGoldCoinSound이 비어 있습니다. Assets/Game/0Sound/coindrop.mp3를 연결하거나 Setup TestScene을 실행해 주세요.");
        return null;
    }
}
