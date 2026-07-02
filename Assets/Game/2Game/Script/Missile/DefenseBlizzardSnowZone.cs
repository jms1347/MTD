using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 눈보라 탄 공중 폭발 후 지면에 깔리는 눈보라 장판 — 범위 내 적에게 초당 동상 1중첩 누적(최대 5).
/// </summary>
public class DefenseBlizzardSnowZone : MonoBehaviour, ILinkedSkillSpawn
{
    private const float TickInterval = 1f;
    private const float FrostRefreshDuration = 3f;

    private LinkedSkillSpawnContext spawnContext;
    private Vector3 anchorPosition;
    private float radius;
    private int maxAffectedEnemies;
    private float endTime;
    private Coroutine tickRoutine;
    private readonly List<Transform> targetBuffer = new(12);
    private readonly Dictionary<int, int> zoneSlowStacks = new(16);
    private readonly List<int> zoneSlowStackRemovalBuffer = new(16);
    private GameObject rangeIndicator;

    public void Initialize(LinkedSkillSpawnContext context)
    {
        if (tickRoutine != null)
        {
            StopCoroutine(tickRoutine);
            tickRoutine = null;
        }

        spawnContext = context;
        zoneSlowStacks.Clear();
        anchorPosition = DefenseBallisticUtility.ProjectToGround(context.spawnOrigin);
        transform.position = anchorPosition;

        radius = context.SourceSkill != null && context.SourceSkill.splashRadius > 0.05f
            ? context.SourceSkill.splashRadius
            : context.ResolveStrikeRadius();
        maxAffectedEnemies = context.SourceSkill != null && context.SourceSkill.maxHit > 0
            ? context.SourceSkill.maxHit
            : 6;
        endTime = Time.time + context.ResolveZoneLifetime();

        SpawnSnowStormVisual(endTime - Time.time);
        SpawnRangeIndicator();
        tickRoutine = StartCoroutine(RunTicks());
    }

    private void SpawnRangeIndicator()
    {
        rangeIndicator = DefenseStrikeWarningZone.CreateSustained(
            anchorPosition,
            radius,
            DefenseStrikeWarningZone.BlizzardZoneColor,
            transform);
    }

    private void SpawnSnowStormVisual(float lifetime)
    {
        if (lifetime <= 0.05f)
            return;

        float burstHeight = spawnContext.airBurstHeight > 0.05f
            ? spawnContext.airBurstHeight
            : Mathf.Clamp(radius * 0.5f, 1.5f, 3.2f);

        DefenseCombatVfxSpawn.TrySpawnBlizzardStorm(
            anchorPosition,
            radius,
            lifetime,
            transform,
            burstHeight);
    }

    private IEnumerator RunTicks()
    {
        while (Time.time < endTime)
        {
            ApplyTick();
            yield return new WaitForSeconds(TickInterval);
        }

        Destroy(gameObject);
    }

    private void ApplyTick()
    {
        CollectEnemiesInRadius(targetBuffer);
        int count = Mathf.Min(targetBuffer.Count, maxAffectedEnemies);

        var seenEnemyIds = new HashSet<int>(count);
        for (int i = 0; i < count; i++)
        {
            var enemyTransform = targetBuffer[i];
            if (enemyTransform == null)
                continue;

            var status = enemyTransform.GetComponentInParent<MonsterStatusController>();
            if (status == null)
                continue;

            int enemyId = status.gameObject.GetInstanceID();
            seenEnemyIds.Add(enemyId);

            int stacks = zoneSlowStacks.TryGetValue(enemyId, out int currentStacks) ? currentStacks + 1 : 1;
            stacks = Mathf.Min(MonsterElementStatusRules.MaxFrostStacks, stacks);
            zoneSlowStacks[enemyId] = stacks;
            status.ApplyFrostWithStackCount(FrostRefreshDuration, stacks);
        }

        zoneSlowStackRemovalBuffer.Clear();
        foreach (var pair in zoneSlowStacks)
        {
            if (!seenEnemyIds.Contains(pair.Key))
                zoneSlowStackRemovalBuffer.Add(pair.Key);
        }

        for (int i = 0; i < zoneSlowStackRemovalBuffer.Count; i++)
            zoneSlowStacks.Remove(zoneSlowStackRemovalBuffer[i]);
    }

    private void CollectEnemiesInRadius(List<Transform> results)
    {
        results.Clear();
        var targetMobility = spawnContext.ResolveTargetMobility();
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");

        for (int i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy, targetMobility: targetMobility))
                continue;

            if (!IsInsideHorizontalRange(enemy.transform.position))
                continue;

            var collider = enemy.GetComponentInChildren<Collider>();
            if (collider != null && !DefenseSkillExecutor.IsEnemyAttackable(collider, targetMobility))
                continue;

            results.Add(enemy.transform);
        }

        results.Sort((a, b) =>
        {
            float aSqr = HorizontalDistanceSqr(a.position);
            float bSqr = HorizontalDistanceSqr(b.position);
            return aSqr.CompareTo(bSqr);
        });
    }

    private float HorizontalDistanceSqr(Vector3 enemyPosition)
    {
        var zoneFlat = new Vector3(anchorPosition.x, 0f, anchorPosition.z);
        var enemyFlat = new Vector3(enemyPosition.x, 0f, enemyPosition.z);
        return (enemyFlat - zoneFlat).sqrMagnitude;
    }

    private bool IsInsideHorizontalRange(Vector3 enemyPosition)
    {
        return HorizontalDistanceSqr(enemyPosition) <= radius * radius;
    }

    private void OnDestroy()
    {
        if (tickRoutine != null)
            StopCoroutine(tickRoutine);

        zoneSlowStacks.Clear();
        DefenseStrikeWarningZone.DestroyZone(rangeIndicator);
        rangeIndicator = null;
    }
}
