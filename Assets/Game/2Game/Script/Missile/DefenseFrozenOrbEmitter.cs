using UnityEngine;

/// <summary>
/// 빙결 오브 본체(FrozenOrbOBJ) 전용 — 비행 중 8방향 순차 분사.
/// 본체는 구체 오브 비주얼 유지, 파편만 shardMissileKey(FrostMissileOBJ)로 별도 스폰.
/// </summary>
[DisallowMultipleComponent]
public class DefenseFrozenOrbEmitter : MonoBehaviour
{
    public const string OrbVisualName = "OrbVisual";
    public const string DefaultShardMissileKey = "FrostMissileOBJ";
    private const float ShardScaleRatio = 0.55f;
    private const float MinShardVisualScale = 0.45f;

    [SerializeField] private float shardEmitInterval = 0.05f;
    [SerializeField] private int directionSlots = 8;
    [SerializeField] private float shardSpeed = 16f;
    [SerializeField] private float shardDamageRatio = 0.24f;
    [SerializeField] private float scaleDrainPerShot = 0.03f;
    [SerializeField] private float shardLifetime = 0.7f;
    [SerializeField] private string shardMissileKey = DefaultShardMissileKey;
    [SerializeField] private GameObject shardMissilePrefab;
    [SerializeField] private Transform orbVisual;

    private DefenseProjectile host;
    private float nextShardTime;
    private int directionCursor;
    private float orbBodyScale = 1f;

    public Transform OrbVisual => orbVisual;

    public void BindOrbVisual(Transform visual)
    {
        orbVisual = visual;
    }

    public void OnOrbLaunched(DefenseProjectile projectile)
    {
        if (projectile == null || projectile.IsFrozenOrbShard)
            return;

        host = projectile;
        nextShardTime = Time.time;
        directionCursor = 0;
        orbBodyScale = 1f;

        if (orbVisual != null)
        {
            orbVisual.gameObject.SetActive(true);
            orbVisual.localScale = Vector3.one * 1.15f;

            var core = orbVisual.Find("OrbCore");
            if (core != null)
                core.localScale = Vector3.one * 0.95f;

            var aura = orbVisual.Find("OrbAura");
            if (aura != null)
                aura.localScale = Vector3.one * 0.42f;

            DefenseFrozenOrbVisualUtility.EnsureOrbCoreVisual(orbVisual);
        }

        ApplyOrbBodyScale();
    }

    public void OnOrbEnded()
    {
        host = null;
        directionCursor = 0;
        orbBodyScale = 1f;

        if (orbVisual != null)
        {
            orbVisual.localScale = Vector3.one;
            var aura = orbVisual.Find("OrbAura");
            if (aura != null)
                aura.localScale = Vector3.one * 0.42f;
            orbVisual.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (orbVisual != null && host != null && host.IsFlightActive && orbBodyScale > 0f)
        {
            orbVisual.Rotate(Vector3.up, 320f * Time.deltaTime, Space.Self);
            orbVisual.Rotate(Vector3.right, 95f * Time.deltaTime, Space.Self);
        }

        if (host == null || !host.IsFlightActive || orbBodyScale <= 0f || Time.time < nextShardTime)
            return;

        if (!host.TryGetFlightSkill(out var skill, out var context))
            return;

        nextShardTime = Time.time + shardEmitInterval;
        EmitNextShard(context.ResolveDamage(skill), skill, context);
    }

    private void EmitNextShard(float orbDamage, DefenseSkillData skill, DefenseSkillProjectileContext context)
    {
        if (orbBodyScale <= 0f || MissilePoolManager.Instance == null || host == null || host.SourcePrefab == null)
            return;

        Vector3 origin = orbVisual != null ? orbVisual.position : transform.position;

        Vector3 flightVelocity = host.FlightVelocity;
        flightVelocity.y = 0f;
        if (flightVelocity.sqrMagnitude > 0.25f)
            origin += flightVelocity.normalized * 0.35f;

        int slots = Mathf.Max(1, directionSlots);
        float angleStep = 360f / slots;
        int slot = directionCursor;
        float orbYaw = orbVisual != null ? orbVisual.eulerAngles.y : 0f;
        float angle = slot * angleStep + orbYaw;
        directionCursor = (directionCursor + 1) % slots;

        var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;
        else
            direction.Normalize();

        float shardVisualScale = Mathf.Max(MinShardVisualScale, orbBodyScale * ShardScaleRatio);
        float shardDamage = orbDamage * shardDamageRatio;
        var velocity = direction * shardSpeed;
        var rotation = Quaternion.LookRotation(direction, Vector3.up);

        var shardContext = context;
        shardContext.pierceRemaining = 1;
        shardContext.homingTarget = null;
        shardContext.isFrozenOrbShard = true;
        shardContext.visualScaleMultiplier = shardVisualScale;
        shardContext.frozenOrbShardLifetime = shardLifetime;
        shardContext.expDuration = 0f;

        var shardPrefab = ResolveShardPrefab();
        if (shardPrefab == null)
            return;

        MissilePoolManager.Instance.SpawnWithSkill(
            shardPrefab,
            origin,
            rotation,
            shardDamage,
            velocity,
            skill.DamageElement,
            shardContext);

        orbBodyScale = Mathf.Max(0f, orbBodyScale - scaleDrainPerShot);
        ApplyOrbBodyScale();
    }

    private GameObject ResolveShardPrefab()
    {
        if (TryResolveMissilePrefab(shardMissilePrefab, out var resolved))
            return resolved;

        shardMissilePrefab = null;

        if (!string.IsNullOrWhiteSpace(shardMissileKey)
            && DefenseAddressableLoader.TryLoadMissile(shardMissileKey.Trim(), out var loaded)
            && TryResolveMissilePrefab(loaded, out resolved))
        {
            shardMissilePrefab = resolved;
            return resolved;
        }

        Debug.LogWarning(
            $"[DefenseFrozenOrbEmitter] 파편 미사일 로드 실패 key='{shardMissileKey}'. " +
            "본체(FrozenOrbOBJ)는 그대로이며 파편만 별도 프리팹이 필요합니다.");
        return null;
    }

    private static bool TryResolveMissilePrefab(UnityEngine.Object candidate, out GameObject prefab)
    {
        prefab = candidate as GameObject;
        if (prefab == null)
            return false;

        return prefab.GetComponent<Transform>() != null;
    }

    private void ApplyOrbBodyScale()
    {
        host?.SetFlightScaleMultiplier(orbBodyScale);

        if (orbBodyScale <= 0f && orbVisual != null)
            orbVisual.gameObject.SetActive(false);
    }
}
