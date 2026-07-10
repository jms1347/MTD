using UnityEngine;

/// <summary>
/// 방어 모드 몬스터·웨이브 밸런스 설정. 인스펙터에서 조절.
/// </summary>
public class CwslMonsterManager : MonoBehaviour
{
    public static CwslMonsterManager Instance { get; private set; }

    [Header("방어 목표")]
    [SerializeField] private float defenseDurationSeconds = 300f;
    [SerializeField] private float nexusMaxHealth = 20000f;

    [Header("적 기지")]
    [SerializeField] private int initialBaseCountMin = 2;
    [SerializeField] private int initialBaseCountMax = 3;
    [SerializeField] private float enemyBaseMaxHealth = 3000f;
    [SerializeField] private float baseSpawnIntervalSeconds = 60f;
    [SerializeField] private int maxBases = 8;

    [Header("스폰")]
    [SerializeField] private float spawnIntervalPerBase = 4f;
    [SerializeField] private int maxAliveMonsters = CwslGameConstants.MaxAliveMonsters;
    [SerializeField] private float spawnWarningSeconds = 1.5f;

    [Header("분당 강화")]
    [SerializeField] private float damageIncreasePerMinute = 0.1f;
    [SerializeField] private bool spawnMidBossEachMinute = true;
    [SerializeField] private bool spawnDefenseBossEachMinute = true;

    [Header("넥서스 우선 몬스터 배율")]
    [SerializeField] private float nexusVariantHealthMultiplier = 2.2f;
    [SerializeField] private float nexusVariantScaleMultiplier = 1.35f;
    [SerializeField] private float nexusVariantSpeedMultiplier = 0.72f;

    [Header("중간보스")]
    [SerializeField] private float midBossHealthMultiplier = 3f;
    [SerializeField] private float midBossScaleMultiplier = 3f;
    [SerializeField] private float midBossSpeedMultiplier = 0.667f;
    [SerializeField] private float midBossBuffRadius = 7f;

    [Header("수석 코치")]
    [SerializeField] private bool spawnSeniorCoachEachMinute = true;
    [SerializeField] private float seniorCoachHealthMultiplier = 2.8f;
    [SerializeField] private float seniorCoachScaleMultiplier = 3f;

    [Header("보스")]
    [SerializeField] private float defenseBossHealthMultiplier = 4f;
    [SerializeField] private float defenseBossScaleMultiplier = 2.8f;
    [SerializeField] private float defenseBossSpeedMultiplier = 1f;

    public float DefenseDurationSeconds => defenseDurationSeconds;
    public float NexusMaxHealth => nexusMaxHealth;
    public int InitialBaseCountMin => initialBaseCountMin;
    public int InitialBaseCountMax => initialBaseCountMax;
    public float EnemyBaseMaxHealth => enemyBaseMaxHealth;
    public float BaseSpawnIntervalSeconds => baseSpawnIntervalSeconds;
    public int MaxBases => maxBases;
    public float SpawnWarningSeconds => spawnWarningSeconds;
    public bool SpawnMidBossEachMinute => spawnMidBossEachMinute;
    public bool SpawnDefenseBossEachMinute => spawnDefenseBossEachMinute;
    public float NexusVariantHealthMultiplier => nexusVariantHealthMultiplier;
    public float NexusVariantScaleMultiplier => nexusVariantScaleMultiplier;
    public float NexusVariantSpeedMultiplier => nexusVariantSpeedMultiplier;
    public float MidBossHealthMultiplier => midBossHealthMultiplier;
    public float MidBossScaleMultiplier => midBossScaleMultiplier;
    public float MidBossSpeedMultiplier => midBossSpeedMultiplier;
    public float MidBossBuffRadius => midBossBuffRadius;
    public bool SpawnSeniorCoachEachMinute => spawnSeniorCoachEachMinute;
    public float SeniorCoachHealthMultiplier => seniorCoachHealthMultiplier;
    public float SeniorCoachScaleMultiplier => seniorCoachScaleMultiplier;
    public float DefenseBossHealthMultiplier => defenseBossHealthMultiplier;
    public float DefenseBossScaleMultiplier => defenseBossScaleMultiplier;
    public float DefenseBossSpeedMultiplier => defenseBossSpeedMultiplier;

    public float GlobalDamageMultiplier { get; private set; } = 1f;
    public int ElapsedMinute { get; private set; }
    public int ActivePlayerCount { get; private set; } = 4;
    public CwslPlayerCountBalance.Profile ActiveBalanceProfile { get; private set; }

    private float activeSpawnIntervalPerBase;
    private int activeMaxAliveMonsters;
    private float activeDamageIncreasePerMinute;
    private float activeMonsterDamageMultiplier = 1f;
    private float activeInitialBaseCountMultiplier = 1f;

    public float SpawnIntervalPerBase =>
        activeSpawnIntervalPerBase > 0f ? activeSpawnIntervalPerBase : spawnIntervalPerBase;

    public int MaxAliveMonsters =>
        activeMaxAliveMonsters > 0 ? activeMaxAliveMonsters : maxAliveMonsters;

    public float DamageIncreasePerMinute =>
        activeDamageIncreasePerMinute > 0f ? activeDamageIncreasePerMinute : damageIncreasePerMinute;

    public int GetScaledInitialBaseCountMin()
    {
        return Mathf.Max(1, Mathf.RoundToInt(initialBaseCountMin * activeInitialBaseCountMultiplier));
    }

    public int GetScaledInitialBaseCountMax()
    {
        var min = GetScaledInitialBaseCountMin();
        return Mathf.Max(min, Mathf.RoundToInt(initialBaseCountMax * activeInitialBaseCountMultiplier));
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ResetForDefenseStart()
    {
        GlobalDamageMultiplier = 1f;
        ElapsedMinute = 0;
    }

    public void ConfigureForPlayerCount(int playerCount)
    {
        ActivePlayerCount = Mathf.Clamp(playerCount, 2, 7);
        ActiveBalanceProfile = CwslPlayerCountBalance.Get(ActivePlayerCount);

        activeSpawnIntervalPerBase = spawnIntervalPerBase * ActiveBalanceProfile.SpawnIntervalMultiplier;
        activeMaxAliveMonsters = Mathf.Max(
            24,
            Mathf.RoundToInt(maxAliveMonsters * ActiveBalanceProfile.MaxAliveMultiplier));
        activeDamageIncreasePerMinute = damageIncreasePerMinute * ActiveBalanceProfile.MinuteEscalationMultiplier;
        activeMonsterDamageMultiplier = ActiveBalanceProfile.MonsterDamageMultiplier;
        activeInitialBaseCountMultiplier = ActiveBalanceProfile.InitialBaseCountMultiplier;
    }

    public void ApplyMinuteEscalation()
    {
        ElapsedMinute++;
        GlobalDamageMultiplier *= 1f + DamageIncreasePerMinute;
    }

    public float GetScaledDamage(float baseDamage, float localMultiplier = 1f)
    {
        return baseDamage
               * GlobalDamageMultiplier
               * activeMonsterDamageMultiplier
               * Mathf.Max(0.01f, localMultiplier);
    }
}
