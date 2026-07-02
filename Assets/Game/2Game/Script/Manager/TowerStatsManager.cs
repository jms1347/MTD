using System;
using UnityEngine;

/// <summary>
/// 일반(미사일) 타워의 전투 스탯입니다.
/// </summary>
[Serializable]
public class StandardTowerStats
{
    [Tooltip("미사일 1발당 피해량")]
    public float attackDamage = 1f;

    [Tooltip("적을 탐지·공격할 수 있는 반경")]
    public float attackRange = 18f;

    [Tooltip("연속 발사 사이 대기 시간(초)")]
    public float fireInterval = 1.2f;

    [Tooltip("미사일 비행 속도")]
    public float missileSpeed = 35f;
}

/// <summary>
/// 메테오 타워의 마커·낙하 공격 스탯입니다.
/// </summary>
[Serializable]
public class MeteorTowerStats
{
    [Tooltip("메테오를 시전할 수 있는 최대 거리")]
    public float targetingRange = 28f;

    [Tooltip("타워에서 적에게 날아가는 마커 큐브 속도")]
    public float markerCubeSpeed = 28f;

    [Tooltip("마커 큐브 크기")]
    public float markerCubeScale = 0.38f;

    [Tooltip("메테오 1회당 피해량")]
    public float strikeDamage = 5f;

    [Tooltip("메테오 시전 쿨다운(초)")]
    public float strikeCooldown = 4.5f;

    [Tooltip("착지 시 범위 피해 반경")]
    public float impactRadius = 5.5f;

    [Tooltip("Nuke 미사일 낙하 시작 높이")]
    public float meteorDropHeight = 22f;

    [Tooltip("Nuke 미사일 낙하 속도")]
    public float meteorFallSpeed = 38f;

    [Tooltip("폭발 VFX 스케일 = impactRadius × 이 값")]
    public float explosionVisualScalePerRadius = 0.35f;

    [Tooltip("마커 큐브가 최적 위치를 고르는 시간(초)")]
    public float targetingScoutDuration = 1.8f;

    [Tooltip("빨간 경고 반경 표시 후 메테오 낙하까지 대기 시간(초)")]
    public float strikeWarningDelay = 2f;

    [Tooltip("마커 큐브가 왔다 갔다 하는 반경")]
    public float markerWanderRadius = 2.2f;

    [Tooltip("낙하 시작 시 수평 오프셋 비율 (높이 대비)")]
    public float meteorDiagonalSpread = 0.55f;
}

/// <summary>
/// 체인 라이트닝 타워의 공격·연쇄·스턴 스탯입니다.
/// </summary>
[Serializable]
public class ChainLightningTowerStats
{
    [Tooltip("번개 1타당 피해량")]
    public float attackDamage = 1f;

    [Tooltip("첫 타겟 탐지 반경")]
    public float attackRange = 20f;

    [Tooltip("연속 시전 쿨다운(초)")]
    public float fireInterval = 1.6f;

    [Tooltip("연쇄가 튕겨 나가는 반경")]
    public float chainRadius = 6f;

    [Tooltip("한 번 시전에 맞출 수 있는 최대 적 수")]
    public int maxChainTargets = 5;

    [Tooltip("연쇄 타격 사이 간격(초)")]
    public float chainHopDelay = 0.09f;

    [Tooltip("피격 적 스턴 지속 시간(초)")]
    public float stunDuration = 1.25f;
}

/// <summary>
/// 소환 타워의 유닛 생성·전투 스탯입니다.
/// </summary>
[Serializable]
public class SummonTowerStats
{
    [Tooltip("소환 간격(초)")]
    public float spawnInterval = 1f;

    [Tooltip("동시에 유지할 수 있는 최대 소환 유닛 수")]
    public int maxMinions = 14;

    [Tooltip("소환 유닛 체력")]
    public float minionHealth = 4f;

    [Tooltip("소환 유닛 이동 속도")]
    public float minionSpeed = 3.8f;

    [Tooltip("소환 유닛 공격력")]
    public float minionDamage = 1f;

    [Tooltip("소환 유닛 크기 배율")]
    public float minionScale = 0.75f;
}

/// <summary>
/// 타워 종류별 스탯을 한곳에서 관리하는 싱글톤 매니저.
/// </summary>
public class TowerStatsManager : Singleton<TowerStatsManager>
{
    [Header("일반 타워 (미사일)")]
    [SerializeField] private StandardTowerStats standard = new();

    [Header("메테오 타워")]
    [SerializeField] private MeteorTowerStats meteor = new();

    [Header("체인 라이트닝 타워")]
    [SerializeField] private ChainLightningTowerStats chainLightning = new();

    [Header("소환 타워")]
    [SerializeField] private SummonTowerStats summon = new();

    public StandardTowerStats Standard => standard;
    public MeteorTowerStats Meteor => meteor;
    public ChainLightningTowerStats ChainLightning => chainLightning;
    public SummonTowerStats Summon => summon;

    public void ApplyTo(TowerController tower)
    {
        if (tower == null)
            return;

        tower.ApplyStats(standard);
    }

    public void ApplyTo(MeteorTowerController tower)
    {
        if (tower == null)
            return;

        tower.ApplyStats(meteor);
    }

    public void ApplyTo(ChainLightningTowerController tower)
    {
        if (tower == null)
            return;

        tower.ApplyStats(chainLightning);
    }

    public void ApplyTo(SummonTowerController tower)
    {
        if (tower == null)
            return;

        tower.ApplyStats(summon);
    }

    protected override void Awake()
    {
        base.Awake();
        RefreshFromTowerSheet();
    }

    public static void RefreshFromSheetIfExists()
    {
        if (Instance != null)
            Instance.RefreshFromTowerSheet();
    }

    public void RefreshFromTowerSheet()
    {
        if (DataManager.Instance == null)
            return;

        if (DefenseTowerSheetTable.TryGetData(DefenseTowerSheetTable.MachineGunTowerId, out var machineGun))
            ApplyStandardFromSheet(machineGun);

        if (DefenseTowerSheetTable.TryGetData(DefenseTowerSheetTable.FlameMortarTowerId, out var mortar))
            ApplyMeteorFromSheet(mortar);

        if (DefenseTowerSheetTable.TryGetData(DefenseTowerSheetTable.AutoLaserTowerId, out var laser))
            ApplyChainFromSheet(laser);
    }

    private void ApplyStandardFromSheet(TowerData data)
    {
        standard.attackDamage = RoguelikeCardStatBridge.ApplyDamage(ResolveDamageWithSkill(data));
        standard.fireInterval = RoguelikeCardStatBridge.ApplyFireInterval(Mathf.Max(0.05f, data.fireInterval));
        standard.attackRange = RoguelikeCardStatBridge.ApplyRange(data.attackRange);
    }

    private void ApplyMeteorFromSheet(TowerData data)
    {
        meteor.strikeDamage = RoguelikeCardStatBridge.ApplyDamage(ResolveDamageWithSkill(data));
        meteor.strikeCooldown = RoguelikeCardStatBridge.ApplyFireInterval(Mathf.Max(0.05f, data.fireInterval));
        meteor.targetingRange = RoguelikeCardStatBridge.ApplyRange(data.attackRange);
    }

    private void ApplyChainFromSheet(TowerData data)
    {
        chainLightning.attackDamage = RoguelikeCardStatBridge.ApplyDamage(ResolveDamageWithSkill(data));
        chainLightning.fireInterval = RoguelikeCardStatBridge.ApplyFireInterval(Mathf.Max(0.05f, data.fireInterval));
        chainLightning.attackRange = RoguelikeCardStatBridge.ApplyRange(data.attackRange);
    }

    private static float ResolveDamageWithSkill(TowerData data)
    {
        if (data == null)
            return 0f;

        var damage = data.baseDamage;
        if (DataManager.Instance != null &&
            DataManager.Instance.TryGetSkillForTower(data, out var skill))
            damage *= skill.damageMultiplier;

        return damage;
    }
}
