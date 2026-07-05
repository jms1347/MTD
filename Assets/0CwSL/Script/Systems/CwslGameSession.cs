using Unity.Netcode;
using UnityEngine;

public class CwslGameSession : NetworkBehaviour
{
    public static CwslGameSession Instance { get; private set; }

    [SerializeField] private CwslGameAssets assets;
    [SerializeField] private CwslMonsterSpawner monsterSpawner;

    public CwslGameAssets Assets => assets;

    private bool bossSpawned;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (assets != null)
        {
            CwslGoldFeedback.Initialize(assets.goldBurstVfx, assets.goldPickupSound);
            CwslRammerAudioFeedback.Initialize(assets.horseGallopSound, assets.rammerStunSound);
        }

        CwslDamagePopupPool.EnsureReady();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && assets != null)
            CwslNetworkPoolService.Instance?.Initialize(assets);

        if (!IsServer)
            return;

        if (CwslKarmaSystem.Instance != null)
            CwslKarmaSystem.Instance.OnKarmaChanged += HandleKarmaChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (CwslKarmaSystem.Instance != null)
            CwslKarmaSystem.Instance.OnKarmaChanged -= HandleKarmaChanged;

        if (Instance == this)
            Instance = null;
    }

    public GameObject GetMonsterPrefab(CwslMonsterType type)
    {
        if (assets == null)
            return null;

        return type switch
        {
            CwslMonsterType.Ranged => assets.rangedMonsterPrefab,
            CwslMonsterType.Suicide => assets.suicideMonsterPrefab,
            CwslMonsterType.Melee => assets.meleeMonsterPrefab,
            CwslMonsterType.BossHongmyeongbo => assets.bossPrefab,
            _ => null
        };
    }

    private void HandleKarmaChanged(long karma)
    {
        if (!IsServer || bossSpawned)
            return;

        if (karma < CwslGameConstants.BossKarmaThreshold)
            return;

        bossSpawned = true;
        if (monsterSpawner != null)
            monsterSpawner.SpawningEnabled = false;

        SpawnBoss();
    }

    private void SpawnBoss()
    {
        var prefab = GetMonsterPrefab(CwslMonsterType.BossHongmyeongbo);
        if (prefab == null)
            return;

        var boss = CwslNetworkPoolService.Instance?.Get(
            prefab,
            new Vector3(0f, 1.6f, 0f),
            Quaternion.identity);
        if (boss == null)
            return;

        var monster = boss.GetComponent<CwslMonsterBase>();
        monster?.Initialize(CwslMonsterType.BossHongmyeongbo);
    }
}
