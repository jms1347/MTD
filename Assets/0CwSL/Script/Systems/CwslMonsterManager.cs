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
    [SerializeField] private float nexusVariantHealthMultiplier = 3f;
    [SerializeField] private float nexusVariantScaleMultiplier = 1.35f;
    [SerializeField] private float nexusVariantSpeedMultiplier = 0.72f;

    [Header("중간보스")]
    [SerializeField] private float midBossHealthMultiplier = 30f;
    [SerializeField] private float midBossScaleMultiplier = 3f;
    [SerializeField] private float midBossSpeedMultiplier = 0.667f;
    [SerializeField] private float midBossBuffRadius = 7f;

    [Header("수석 코치")]
    [SerializeField] private bool spawnSeniorCoachEachMinute = true;
    [SerializeField] private float seniorCoachHealthMultiplier = 22f;
    [SerializeField] private float seniorCoachScaleMultiplier = 3f;

    [Header("보스")]
    [SerializeField] private float defenseBossHealthMultiplier = 60f;
    [SerializeField] private float defenseBossScaleMultiplier = 2.8f;
    [SerializeField] private float defenseBossSpeedMultiplier = 1f;

    public float DefenseDurationSeconds => defenseDurationSeconds;
    public float NexusMaxHealth => nexusMaxHealth;
    public int InitialBaseCountMin => initialBaseCountMin;
    public int InitialBaseCountMax => initialBaseCountMax;
    public float EnemyBaseMaxHealth => enemyBaseMaxHealth;
    public float BaseSpawnIntervalSeconds => baseSpawnIntervalSeconds;
    public int MaxBases => maxBases;
    public float SpawnIntervalPerBase => spawnIntervalPerBase;
    public int MaxAliveMonsters => maxAliveMonsters;
    public float SpawnWarningSeconds => spawnWarningSeconds;
    public float DamageIncreasePerMinute => damageIncreasePerMinute;
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

    public void ApplyMinuteEscalation()
    {
        ElapsedMinute++;
        GlobalDamageMultiplier *= 1f + damageIncreasePerMinute;
    }

    public float GetScaledDamage(float baseDamage, float localMultiplier = 1f)
    {
        return baseDamage * GlobalDamageMultiplier * Mathf.Max(0.01f, localMultiplier);
    }
}
