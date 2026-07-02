using System.Collections;
using UnityEngine;

/// <summary>
/// 정전기 공중 정지탄(ExpDuration -1) — 명중 전 공중에서 멈추고 미사일에서 번개가 낙하합니다.
/// </summary>
[DisallowMultipleComponent]
public class DefenseStormMissileAnchor : MonoBehaviour
{
    private LinkedSkillSpawnContext spawnContext;
    private DefenseProjectile ownerProjectile;
    private Vector3 anchorFlatOrigin;
    private float endTime;
    private float tickInterval;
    private Coroutine tickRoutine;
    private GameObject strikeRangeIndicator;

    public void Begin(LinkedSkillSpawnContext context, DefenseProjectile owner)
    {
        if (tickRoutine != null)
        {
            StopCoroutine(tickRoutine);
            tickRoutine = null;
        }

        DefenseStrikeWarningZone.DestroyZone(strikeRangeIndicator);
        strikeRangeIndicator = null;

        spawnContext = context;
        ownerProjectile = owner;
        anchorFlatOrigin = DefenseBallisticUtility.ProjectToGround(context.spawnOrigin);
        tickInterval = context.ResolveSummonTickInterval();
        endTime = Time.time + context.ResolveLifetime();

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

    private IEnumerator RunTicks()
    {
        while (Time.time < endTime)
        {
            var target = DefenseStormStrikeLogic.ResolveStrikeTarget(spawnContext, anchorFlatOrigin);
            if (target != null)
                DefenseStormStrikeLogic.StrikeEnemy(spawnContext, anchorFlatOrigin, transform.position, target);

            yield return new WaitForSeconds(tickInterval);
        }

        Complete();
    }

    private void Complete()
    {
        tickRoutine = null;
        DefenseStrikeWarningZone.DestroyZone(strikeRangeIndicator);
        strikeRangeIndicator = null;

        if (ownerProjectile != null)
            ownerProjectile.ReturnToPoolFromStormAnchor();
        else
            Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (tickRoutine != null)
        {
            StopCoroutine(tickRoutine);
            tickRoutine = null;
        }

        DefenseStrikeWarningZone.DestroyZone(strikeRangeIndicator);
        strikeRangeIndicator = null;
    }
}
