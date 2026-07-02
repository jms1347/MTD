using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 첫 적에게 번개를 쏜 뒤 근처 적에게 최대 5명까지 연쇄 타격합니다.
/// 피격 적은 찌직거리며 스턴 상태가 됩니다.
/// </summary>
public class ChainLightningTowerController : MonoBehaviour
{
    [Header("공격")]
    [SerializeField] private float attackRange = 20f;
    [SerializeField] private float fireInterval = 1.6f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float chainRadius = 6f;
    [SerializeField] private int maxChainTargets = 5;
    [SerializeField] private float chainHopDelay = 0.09f;
    [SerializeField] private float stunDuration = 1.25f;

    [Header("VFX")]
    [SerializeField] private GameObject chainBoltPrefab;
    [SerializeField] private GameObject chainHitExplosionPrefab;
    [SerializeField] private GameObject stunHeadEffectPrefab;
    [SerializeField] private GameObject stunBodyEffectPrefab;

    private Transform firePoint;
    private float nextFireTime;
    private bool isCasting;

    public void Initialize(
        Transform muzzlePoint,
        GameObject boltPrefab,
        GameObject hitExplosionPrefab,
        GameObject headStunPrefab,
        GameObject bodyStunPrefab)
    {
        firePoint = muzzlePoint;
        chainBoltPrefab = boltPrefab;
        chainHitExplosionPrefab = hitExplosionPrefab;
        stunHeadEffectPrefab = headStunPrefab;
        stunBodyEffectPrefab = bodyStunPrefab;

        if (TowerStatsManager.Instance != null)
            TowerStatsManager.Instance.ApplyTo(this);
    }

    public float AttackRange => attackRange;

    public void ApplyStats(ChainLightningTowerStats stats)
    {
        if (stats == null)
            return;

        attackRange = stats.attackRange;
        fireInterval = stats.fireInterval;
        attackDamage = stats.attackDamage;
        chainRadius = stats.chainRadius;
        maxChainTargets = stats.maxChainTargets;
        chainHopDelay = stats.chainHopDelay;
        stunDuration = stats.stunDuration;
    }

    private void Update()
    {
        if (isCasting || Time.time < nextFireTime)
            return;

        Transform firstTarget = FindNearestEnemy();
        if (firstTarget == null)
            return;

        StartCoroutine(CastChainRoutine(firstTarget));
        nextFireTime = Time.time + fireInterval;
    }

    private IEnumerator CastChainRoutine(Transform firstTarget)
    {
        isCasting = true;

        var hitTargets = new List<Transform>();
        Vector3 fromPosition = GetMuzzleWorldPosition();
        Transform currentTarget = firstTarget;

        while (currentTarget != null && hitTargets.Count < maxChainTargets)
        {
            hitTargets.Add(currentTarget);
            Vector3 targetPoint = GetEnemyLightningPoint(currentTarget);

            ChainLightningVisual.PlayBolt(fromPosition, targetPoint, chainBoltPrefab);
            ApplyHit(currentTarget, targetPoint);

            yield return new WaitForSeconds(chainHopDelay);

            fromPosition = targetPoint;
            currentTarget = FindNextChainTarget(fromPosition, hitTargets);
        }

        isCasting = false;
    }

    private void ApplyHit(Transform target, Vector3 hitPoint)
    {
        if (target == null)
            return;

        MonsterStatusCombatResolver.ApplyDamageToEnemy(
            target.gameObject,
            attackDamage,
            DefenseSkillElement.Lightning,
            hitPoint);

        if (chainHitExplosionPrefab != null)
        {
            var hitFx = Instantiate(chainHitExplosionPrefab, hitPoint, Quaternion.identity);
            Destroy(hitFx, 2f);
        }

        DefenseCombatVfxSpawn.TrySpawnLightningStrikeScorch(hitPoint);

        var status = target.GetComponent<MonsterStatusController>();
        if (status == null)
            status = target.gameObject.AddComponent<MonsterStatusController>();

        status.ApplyShock(stunDuration, stunHeadEffectPrefab, stunBodyEffectPrefab);
    }

    private Transform FindNextChainTarget(Vector3 origin, List<Transform> alreadyHit)
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float nearestSqr = chainRadius * chainRadius;

        foreach (var enemy in enemies)
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy, requireLanded: true))
                continue;

            if (alreadyHit.Contains(enemy.transform))
                continue;

            float sqr = (enemy.transform.position - origin).sqrMagnitude;
            if (sqr > nearestSqr)
                continue;

            nearestSqr = sqr;
            nearest = enemy.transform;
        }

        return nearest;
    }

    private Transform FindNearestEnemy()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float nearestSqr = attackRange * attackRange;
        Vector3 origin = transform.position;

        foreach (var enemy in enemies)
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy, requireLanded: true))
                continue;

            float sqr = (enemy.transform.position - origin).sqrMagnitude;
            if (sqr > nearestSqr)
                continue;

            nearestSqr = sqr;
            nearest = enemy.transform;
        }

        return nearest;
    }

    private Vector3 GetMuzzleWorldPosition()
    {
        if (firePoint != null)
            return firePoint.position;

        return transform.TransformPoint(new Vector3(0f, 0.75f, 0.55f));
    }

    private static Vector3 GetEnemyLightningPoint(Transform enemy)
    {
        return DefenseCombatTargeting.ResolveEnemyAimPoint(enemy);
    }
}
