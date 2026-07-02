using System.Collections;
using UnityEngine;

/// <summary>
/// CloudBlack 등 구름 프리팹 — 미사일 명중 후 폭발 지점에 고정, 범위 내 적에게 주기 낙뢰.
/// 공격력·속성·상태이상·타겟 판정은 LinkedSkillSpawnContext(미사일 스킬 + 타워)를 따릅니다.
/// </summary>
public class DefenseStormCloud : MonoBehaviour, ILinkedSkillSpawn
{
    internal const float CloudScalePerRadius = 0.45f;
    internal const float HeadOffsetY = 3.3f;
    internal const float LightningFxLifetime = 2.2f;
    internal const string LightningEffectKey = "LightningStrikeSharpTallBlue";
    internal const string FallbackLightningEffectKey = "LightningBlueOBJ";

    private LinkedSkillSpawnContext spawnContext;
    private Vector3 anchorFlatOrigin;
    private float tickInterval;
    private float endTime;
    private Coroutine tickRoutine;
    private GameObject strikeRangeIndicator;

    public void Initialize(LinkedSkillSpawnContext context)
    {
        if (tickRoutine != null)
        {
            StopCoroutine(tickRoutine);
            tickRoutine = null;
        }

        DefenseStrikeWarningZone.DestroyZone(strikeRangeIndicator);
        strikeRangeIndicator = null;

        spawnContext = context;
        anchorFlatOrigin = DefenseBallisticUtility.ProjectToGround(context.spawnOrigin);
        transform.position = ResolveCloudPositionAtDetonation(anchorFlatOrigin);

        tickInterval = context.ResolveSummonTickInterval();
        endTime = Time.time + context.ResolveLifetime();

        ApplyCloudScale();
        SpawnStrikeRangeIndicator();
        tickRoutine = StartCoroutine(RunTicks());
    }

    private void SpawnStrikeRangeIndicator()
    {
        strikeRangeIndicator = DefenseStrikeWarningZone.CreateSustained(
            anchorFlatOrigin,
            spawnContext.ResolveStrikeRadius(),
            DefenseStrikeWarningZone.StormStrikeZoneColor);
    }

    public static Vector3 ResolveCloudPositionAtDetonation(Vector3 detonationPoint)
    {
        return detonationPoint + Vector3.up * HeadOffsetY;
    }

    private void ApplyCloudScale()
    {
        float radius = spawnContext.ResolveStrikeRadius();
        float scale = radius * Mathf.Max(0.1f, CloudScalePerRadius);
        transform.localScale = Vector3.one * scale;
    }

    private IEnumerator RunTicks()
    {
        while (Time.time < endTime)
        {
            var target = DefenseStormStrikeLogic.ResolveStrikeTarget(spawnContext, anchorFlatOrigin);
            if (target != null)
                DefenseStormStrikeLogic.StrikeEnemy(spawnContext, anchorFlatOrigin, transform.position, target);

            yield return new WaitForSeconds(tickInterval);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (tickRoutine != null)
            StopCoroutine(tickRoutine);

        DefenseStrikeWarningZone.DestroyZone(strikeRangeIndicator);
        strikeRangeIndicator = null;
    }
}
