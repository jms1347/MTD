using System.Text;
using UnityEngine;

/// <summary>
/// 타워 생성·배치를 담당하는 싱글톤 매니저.
/// DefenseSceneSetup에서 전달한 TowerSpawnData 배열로 동서남북 타워를 만듭니다.
/// </summary>
public class TowerManager : Singleton<TowerManager>
{
    public const string PoolRootName = "타워POOL";

    [Header("타워 배치")]
    [Tooltip("타워들이 배치되는 맵 중심 좌표입니다.\n보통 넥서스 위치와 동일하게 설정됩니다.")]
    [SerializeField] private Vector3 arenaCenter = Vector3.zero;

    [Tooltip("생성되는 타워 큐브의 크기(가로·세로·깊이 배율)입니다.")]
    [SerializeField] private Vector3 towerScale = new Vector3(1.2f, 1.2f, 1.2f);

    private Transform towerPoolRoot;
    private bool isSceneBuilt;
    private string lastBuildSignature = string.Empty;

    public Transform TowerPoolRoot => towerPoolRoot;

    protected override void Awake()
    {
        base.Awake();
    }

    public void BuildScene(Vector3 center, TowerSpawnData[] towers)
    {
        arenaCenter = center;
        EnsurePoolRoot();

        string signature = BuildSignature(towers);
        int expectedCount = towers?.Length ?? 0;
        if (isSceneBuilt && towerPoolRoot.childCount == expectedCount && signature == lastBuildSignature)
            return;

        lastBuildSignature = signature;
        ClearExistingTowers();

        if (towers != null)
        {
            foreach (var towerData in towers)
            {
                switch (towerData.kind)
                {
                    case TowerKind.Meteor:
                        CreateMeteorTower(towerData);
                        break;
                    case TowerKind.ChainLightning:
                        CreateChainLightningTower(towerData);
                        break;
                    case TowerKind.Summon:
                        CreateSummonTower(towerData);
                        break;
                    default:
                        CreateStandardTower(towerData);
                        break;
                }
            }
        }

        isSceneBuilt = true;
    }

    public void PlaceTowerAtWorld(TowerSpawnData data, Vector3 worldPosition)
    {
        EnsurePoolRoot();

        if (data == null || string.IsNullOrWhiteSpace(data.towerName))
            return;

        data.positionOffset = worldPosition - arenaCenter;

        switch (data.kind)
        {
            case TowerKind.Meteor:
                CreateMeteorTowerAtWorld(data, worldPosition);
                break;
            case TowerKind.ChainLightning:
                CreateChainLightningTowerAtWorld(data, worldPosition);
                break;
            case TowerKind.Summon:
                CreateSummonTowerAtWorld(data, worldPosition);
                break;
            default:
                CreateStandardTowerAtWorld(data, worldPosition);
                break;
        }
    }

    private static string BuildSignature(TowerSpawnData[] towers)
    {
        if (towers == null || towers.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var tower in towers)
        {
            if (tower == null)
                continue;

            sb.Append(tower.towerName).Append('|');
            sb.Append(tower.kind).Append('|');
            sb.Append(tower.positionOffset).Append('|');
            sb.Append(tower.rotationY).Append('|');
            sb.Append(tower.scaleMultiplier).Append(';');
        }

        return sb.ToString();
    }

    private void ClearExistingTowers()
    {
        if (towerPoolRoot == null)
            return;

        for (int i = towerPoolRoot.childCount - 1; i >= 0; i--)
            Destroy(towerPoolRoot.GetChild(i).gameObject);
    }

    private GameObject CreateTowerShell(TowerSpawnData data, Vector3 worldPosition)
    {
        var tower = new GameObject(data.towerName);
        tower.tag = "Tower";
        tower.transform.SetParent(towerPoolRoot, false);
        tower.transform.position = worldPosition;
        tower.transform.rotation = Quaternion.Euler(0f, data.rotationY, 0f);
        tower.transform.localScale = Vector3.Scale(towerScale, data.scaleMultiplier);

        int sheetId = DefenseTowerLayoutTable.ResolveSheetId(data);
        DefenseTowerVisualResolver.TryInstantiateVisual(tower.transform, data, sheetId, data.kind);
        DefenseTowerCombatRange.EnsurePickCollider(tower);

        return tower;
    }

    private static void AttachTowerAim(GameObject tower, TowerController towerController)
    {
        DefenseTowerVisualBuilder.AttachAimController(tower, towerController);
    }

    private static void AttachTowerAim(GameObject tower, float attackRange, string targetMobility = null)
    {
        DefenseTowerVisualBuilder.AttachAimController(tower, attackRange, targetMobility);
    }

    private void CreateStandardTower(TowerSpawnData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.towerName))
            return;

        if (towerPoolRoot.Find(data.towerName) != null)
            return;

        var tower = CreateTowerShell(data, arenaCenter + data.positionOffset);
        var firePoint = DefenseTowerVisualBuilder.FindFirePoint(tower.transform);

        var towerController = tower.AddComponent<TowerController>();
        int sheetId = DefenseTowerLayoutTable.ResolveSheetId(data);
        if (sheetId > 0)
            towerController.InitializeFromSheet(sheetId, firePoint);
        else
            towerController.Initialize(data.standardMissileId, firePoint);

        AttachTowerAim(tower, towerController);
    }

    private void CreateStandardTowerAtWorld(TowerSpawnData data, Vector3 worldPosition)
    {
        if (towerPoolRoot.Find(data.towerName) != null)
            return;

        var tower = CreateTowerShell(data, worldPosition);
        var firePoint = DefenseTowerVisualBuilder.FindFirePoint(tower.transform);

        var towerController = tower.AddComponent<TowerController>();
        int sheetId = DefenseTowerLayoutTable.ResolveSheetId(data);
        if (sheetId > 0)
            towerController.InitializeFromSheet(sheetId, firePoint);
        else
            towerController.Initialize(data.standardMissileId, firePoint);

        AttachTowerAim(tower, towerController);
    }

    private void CreateMeteorTower(TowerSpawnData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.towerName))
            return;

        if (towerPoolRoot.Find(data.towerName) != null)
            return;

        var tower = CreateTowerShell(data, arenaCenter + data.positionOffset);
        var meteorController = tower.AddComponent<MeteorTowerController>();
        meteorController.Initialize(data.meteorProjectilePrefab, data.meteorExplosionPrefab);
        AttachTowerAim(tower, meteorController.TargetingRange);
    }

    private void CreateMeteorTowerAtWorld(TowerSpawnData data, Vector3 worldPosition)
    {
        if (towerPoolRoot.Find(data.towerName) != null)
            return;

        var tower = CreateTowerShell(data, worldPosition);
        var meteorController = tower.AddComponent<MeteorTowerController>();
        meteorController.Initialize(data.meteorProjectilePrefab, data.meteorExplosionPrefab);
        AttachTowerAim(tower, meteorController.TargetingRange);
    }

    private void CreateChainLightningTower(TowerSpawnData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.towerName))
            return;

        if (towerPoolRoot.Find(data.towerName) != null)
            return;

        var tower = CreateTowerShell(data, arenaCenter + data.positionOffset);
        var firePoint = DefenseTowerVisualBuilder.FindFirePoint(tower.transform);
        var chainController = tower.AddComponent<ChainLightningTowerController>();
        chainController.Initialize(
            firePoint,
            data.chainBoltPrefab,
            data.chainHitExplosionPrefab,
            data.stunHeadEffectPrefab,
            data.stunBodyEffectPrefab);
        AttachTowerAim(tower, chainController.AttackRange);
    }

    private void CreateChainLightningTowerAtWorld(TowerSpawnData data, Vector3 worldPosition)
    {
        if (towerPoolRoot.Find(data.towerName) != null)
            return;

        var tower = CreateTowerShell(data, worldPosition);
        var firePoint = DefenseTowerVisualBuilder.FindFirePoint(tower.transform);
        var chainController = tower.AddComponent<ChainLightningTowerController>();
        chainController.Initialize(
            firePoint,
            data.chainBoltPrefab,
            data.chainHitExplosionPrefab,
            data.stunHeadEffectPrefab,
            data.stunBodyEffectPrefab);
        AttachTowerAim(tower, chainController.AttackRange);
    }

    private void CreateSummonTower(TowerSpawnData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.towerName))
            return;

        if (towerPoolRoot.Find(data.towerName) != null)
            return;

        var tower = CreateTowerShell(data, arenaCenter + data.positionOffset);
        var summonController = tower.AddComponent<SummonTowerController>();
        summonController.Initialize();
    }

    private void CreateSummonTowerAtWorld(TowerSpawnData data, Vector3 worldPosition)
    {
        if (towerPoolRoot.Find(data.towerName) != null)
            return;

        var tower = CreateTowerShell(data, worldPosition);
        var summonController = tower.AddComponent<SummonTowerController>();
        summonController.Initialize();
    }

    private void EnsurePoolRoot()
    {
        if (towerPoolRoot != null)
            return;

        var rootObject = transform.Find(PoolRootName);
        if (rootObject == null)
        {
            rootObject = new GameObject(PoolRootName).transform;
            rootObject.SetParent(transform, false);
        }

        towerPoolRoot = rootObject;
    }
}
