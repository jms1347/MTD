using System.Collections;
using UnityEngine;

/// <summary>
/// 명중 지점에 고정되는 원소별 지속 장판 — 구름 번개와 동일한 틱 데미지/상태이상 패턴.
/// </summary>
public class DefenseElementZoneSummon : MonoBehaviour, ILinkedSkillSpawn
{
    private const float AirAnchorHeight = 3.3f;
    private const float GroundYOffset = 0.12f;
    private const float ZoneScalePerRadius = 0.55f;
    private const float FxLifetime = 2f;

    [SerializeField] private bool useAirAnchor;
    [SerializeField] private DefenseSkillElement elementOverride = DefenseSkillElement.Physical;

    private LinkedSkillSpawnContext spawnContext;
    private Vector3 anchorPosition;
    private float radius;
    private float tickInterval;
    private float strikeDamage;
    private float endTime;
    private Coroutine tickRoutine;
    private GameObject runtimeZoneVisual;

    public void Initialize(LinkedSkillSpawnContext context)
    {
        if (tickRoutine != null)
        {
            StopCoroutine(tickRoutine);
            tickRoutine = null;
        }

        spawnContext = context;
        anchorPosition = context.spawnOrigin;
        transform.position = ResolveZonePosition(anchorPosition, useAirAnchor);

        radius = context.SourceSkill != null && context.SourceSkill.splashRadius > 0f
            ? context.SourceSkill.splashRadius
            : context.ResolveStrikeRadius();
        strikeDamage = context.ResolveDamage();
        tickInterval = context.ResolveSummonTickInterval();
        endTime = Time.time + context.ResolveLifetime();

        DisableLegacyVisual();
        ApplyZoneScale();
        EnsureRuntimeZoneVisual();
        SpawnGroundPresentation();
        tickRoutine = StartCoroutine(RunTicks());
    }

    private void DisableLegacyVisual()
    {
        var legacy = transform.Find("Visual");
        if (legacy == null)
            return;

        DefenseCombatVfxSpawn.DisablePhysicsAndMissileScripts(legacy.gameObject);
        legacy.gameObject.SetActive(false);
    }

    private void EnsureRuntimeZoneVisual()
    {
        if (runtimeZoneVisual != null)
        {
            Destroy(runtimeZoneVisual);
            runtimeZoneVisual = null;
        }

        if (useAirAnchor)
            return;

        var element = elementOverride != DefenseSkillElement.Physical
            ? elementOverride
            : spawnContext.ResolveElement();
        string loopKey = ResolveGroundLoopVfxKey(element);
        if (string.IsNullOrWhiteSpace(loopKey))
            return;

        float lifetime = Mathf.Clamp(spawnContext.ResolveLifetime(), 1.5f, 6f);
        runtimeZoneVisual = DefenseCombatVfxSpawn.TryAttachGroundZoneLoop(transform, loopKey, lifetime);
    }

    private static string ResolveGroundLoopVfxKey(DefenseSkillElement element)
    {
        return element switch
        {
            DefenseSkillElement.Fire => "LavaBoiling",
            DefenseSkillElement.Ice => "NovaFrost",
            DefenseSkillElement.Poison => "FlamethrowerCartoonyGreen",
            _ => string.Empty
        };
    }

    private void SpawnGroundPresentation()
    {
        if (useAirAnchor)
            return;

        var element = elementOverride != DefenseSkillElement.Physical
            ? elementOverride
            : spawnContext.ResolveElement();
        float lifetime = Mathf.Clamp(spawnContext.ResolveLifetime(), 1.5f, 4f);

        DefenseCombatVfxSpawn.TrySpawnGroundBurst(
            ResolveGroundBurstKey(element),
            anchorPosition,
            Mathf.Min(lifetime, 2f));

        if (element == DefenseSkillElement.Fire)
            DefenseCombatVfxSpawn.TrySpawnGroundFireMark(anchorPosition, radius, lifetime);
    }

    private static string ResolveGroundBurstKey(DefenseSkillElement element)
    {
        return element switch
        {
            DefenseSkillElement.Fire => "GasExplosionFire",
            DefenseSkillElement.Ice => "ExplosionNovaBlue",
            DefenseSkillElement.Poison => "PoisonExplosion",
            _ => "BulletFatExplosionPink"
        };
    }

    public static Vector3 ResolveZonePosition(Vector3 detonationPoint, bool airAnchor)
    {
        return airAnchor
            ? detonationPoint + Vector3.up * AirAnchorHeight
            : new Vector3(detonationPoint.x, GroundYOffset, detonationPoint.z);
    }

    private void ApplyZoneScale()
    {
        float scale = radius * Mathf.Max(0.2f, ZoneScalePerRadius);
        transform.localScale = Vector3.one * scale;
    }

    private IEnumerator RunTicks()
    {
        yield return new WaitForSeconds(tickInterval);

        while (Time.time < endTime)
        {
            ApplyTick();
            yield return new WaitForSeconds(tickInterval);
        }

        Destroy(gameObject);
    }

    private void ApplyTick()
    {
        var targets = CollectEnemiesInRadius();
        for (int i = 0; i < targets.Count; i++)
            StrikeEnemy(targets[i]);
    }

    private System.Collections.Generic.List<Transform> CollectEnemiesInRadius()
    {
        var result = new System.Collections.Generic.List<Transform>(4);
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

            result.Add(enemy.transform);
        }

        return result;
    }

    private bool IsInsideHorizontalRange(Vector3 enemyPosition)
    {
        var zoneFlat = new Vector3(anchorPosition.x, 0f, anchorPosition.z);
        var enemyFlat = new Vector3(enemyPosition.x, 0f, enemyPosition.z);
        return (enemyFlat - zoneFlat).sqrMagnitude <= radius * radius;
    }

    private void StrikeEnemy(Transform enemyTransform)
    {
        var skill = spawnContext.SourceSkill;
        if (enemyTransform == null || skill == null)
            return;

        var enemyCollider = enemyTransform.GetComponentInChildren<Collider>();
        if (enemyCollider == null)
            return;

        Vector3 hitPoint = DefenseCombatTargeting.ResolveEnemyAimPoint(enemyTransform);
        if (useAirAnchor)
            PlayStrikeVfx(hitPoint);

        MonsterStatusCombatResolver.ApplyDamageToEnemy(
            enemyCollider.gameObject,
            strikeDamage,
            spawnContext.ResolveElement(),
            hitPoint);

        DefenseEffectApplicator.ApplySkillEffects(enemyCollider.gameObject, skill, anchorPosition);
    }

    private void PlayStrikeVfx(Vector3 hitPoint)
    {
        var element = elementOverride != DefenseSkillElement.Physical
            ? elementOverride
            : spawnContext.ResolveElement();

        Vector3 originPoint = useAirAnchor ? transform.position : anchorPosition;

        switch (element)
        {
            case DefenseSkillElement.Lightning:
                if (DefenseCombatCatalog.Active != null)
                    DefenseLightningStrike.PlayStrikeVfx(originPoint, hitPoint);
                else
                {
                    DefenseCombatVfxSpawn.TrySpawnAt(
                        "LightningStrikeSharpTallBlue",
                        hitPoint,
                        Quaternion.identity,
                        FxLifetime);
                    DefenseCombatVfxSpawn.TrySpawnLightningStrikeScorch(hitPoint);
                }

                break;
            case DefenseSkillElement.Fire:
                DefenseCombatVfxSpawn.TrySpawnGroundBurst("GasExplosionFire", originPoint, FxLifetime);
                break;
            case DefenseSkillElement.Ice:
                DefenseCombatVfxSpawn.TrySpawnGroundBurst("ExplosionNovaBlue", hitPoint, FxLifetime);
                break;
            case DefenseSkillElement.Poison:
                DefenseCombatVfxSpawn.TrySpawnGroundBurst("PoisonExplosion", hitPoint, FxLifetime);
                break;
            default:
                DefenseCombatVfxSpawn.TrySpawnGroundBurst("BulletFatExplosionPink", hitPoint, FxLifetime);
                break;
        }
    }

    private void OnDestroy()
    {
        if (tickRoutine != null)
            StopCoroutine(tickRoutine);

        if (runtimeZoneVisual != null)
            Destroy(runtimeZoneVisual);
    }
}
