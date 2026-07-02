using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 상태 이상 단일 컨트롤러 (4원소 + 물/빙결/넉백 등).
/// </summary>
[RequireComponent(typeof(Health))]
public class MonsterStatusController : MonoBehaviour
{
    private class BurnState
    {
        public float endTime;
        public float damagePerSecond;
        public float damageAccumulator;
    }

    private class FrostState
    {
        public int stacks;
        public float endTime;
    }

    private class ShockState
    {
        public float endTime;
    }

    private class PoisonState
    {
        public int stacks;
        public float endTime;
        public float tickDamagePerStack;
        public float armorReductionPerStack;
    }

    private class AuxStatus
    {
        public MonsterStatus status;
        public float endTime;
        public float magnitude;
        public float tickDamage;
        public DamageElement damageElement;
    }

    private class StatusVfxBundle
    {
        public GameObject head;
        public GameObject body;
        public GameObject foot;
    }

    [SerializeField] private float shockedJitterStrength = 0.11f;
    [SerializeField] private float electrifiedJitterStrength = 0.14f;

    private readonly BurnState burn = new();
    private readonly FrostState frost = new();
    private readonly ShockState shock = new();
    private readonly PoisonState poison = new();
    private readonly Dictionary<MonsterStatus, AuxStatus> auxStatuses = new();
    private readonly Dictionary<MonsterStatus, StatusVfxBundle> statusVfx = new();
    private readonly List<MonsterStatus> expireBuffer = new();
    private readonly List<string> displayLineBuffer = new();

    private Health health;
    private Monster monster;
    private MonsterStatusOverlayUI overlayUi;
    private Coroutine poisonTickRoutine;
    private Coroutine auxDotRoutine;
    private Coroutine knockbackRoutine;

    private Vector3 shockAnchor;
    private float groundY = 0.45f;
    private bool burnActive;
    private bool frostActive;
    private bool shockActive;
    private bool poisonActive;
    private bool knockbackActive;
    private float genericSlowPercent = 100f;
    private float genericSlowEndTime;

    public event Action StatusesChanged;

    public bool HasBurn => burnActive && burn.endTime > Time.time;
    public bool HasFrost => frostActive && frost.stacks > 0 && frost.endTime > Time.time;
    public bool HasShock => shockActive && shock.endTime > Time.time;
    public bool HasPoison => poisonActive && poison.stacks > 0 && (IsPermanent(poison.endTime) || poison.endTime > Time.time);

    public bool IsWet => HasAux(MonsterStatus.Wet);
    public bool IsFrozen => HasAux(MonsterStatus.Frozen);
    public bool IsShocked => HasShock;
    public bool IsStunned => BlocksAction;

    public int FrostStacks => HasFrost ? frost.stacks : 0;
    public int PoisonStacks => HasPoison ? poison.stacks : 0;

    public bool BlocksMovement => knockbackActive || HasShock || IsFrozen;

    public bool BlocksAction => HasShock;

    private void Awake()
    {
        health = GetComponent<Health>();
        monster = GetComponent<Monster>();
        overlayUi = GetComponent<MonsterStatusOverlayUI>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (health != null && !health.IsAlive)
            return;

        ExpireAuxStatuses();
        ExpireElementalStatuses();
        TickBurnDamage();
        ApplyShockJitterIfNeeded();
        ApplyElectrifiedJitterIfNeeded();
        ExpireGenericSlow();
    }

    public bool Has(MonsterStatus status)
    {
        return status switch
        {
            MonsterStatus.Burning => HasBurn,
            MonsterStatus.Slowed => HasFrost || (Time.time < genericSlowEndTime && genericSlowPercent < 100f),
            MonsterStatus.Shocked => HasShock,
            MonsterStatus.Poisoned => HasPoison,
            MonsterStatus.None => false,
            _ => HasAux(status)
        };
    }

    public void ApplyEffect(DefenseEffectData effect, Vector3? effectSource = null)
    {
        if (effect == null)
            return;

        if (effect.effectType == DefenseEffectType.Knockback)
        {
            ApplyKnockback(effect, effectSource);
            return;
        }

        switch (effect.effectType)
        {
            case DefenseEffectType.Fire:
                ApplyBurn(effect);
                break;
            case DefenseEffectType.Poison:
                ApplyPoison(effect);
                break;
            case DefenseEffectType.Stun:
                ApplyShock(effect);
                break;
            case DefenseEffectType.Slow:
                if (effect.element == DefenseSkillElement.Ice)
                    ApplyFrost(effect);
                else
                    ApplyGenericSlow(effect);
                break;
            case DefenseEffectType.Root:
                if (effect.element == DefenseSkillElement.Ice)
                    ApplyFrost(effect);
                else
                    ApplyAuxFromEffect(MonsterStatus.Frozen, effect);
                break;
            case DefenseEffectType.Lightning:
                if (effect.element == DefenseSkillElement.Lightning && effect.duration > 0f)
                    ApplyShock(effect);
                else
                    ApplyAuxFromEffect(MonsterStatus.Electrified, effect);
                break;
            case DefenseEffectType.Water:
                ApplyAuxFromEffect(MonsterStatus.Wet, effect);
                break;
            case DefenseEffectType.Ground:
                ApplyAuxFromEffect(MonsterStatus.Ablaze, effect);
                break;
            default:
                if (MonsterStatusGrantRules.TryResolve(effect, out var status))
                    ApplyAuxFromEffect(status, effect);
                break;
        }
    }

    public void ApplyShock(float duration, GameObject headEffectPrefab = null, GameObject bodyEffectPrefab = null)
    {
        if (duration <= 0f)
            return;

        float newEnd = Time.time + duration;
        if (shockActive)
            shock.endTime = Mathf.Max(shock.endTime, newEnd);
        else
        {
            shock.endTime = newEnd;
            shockActive = true;
        }

        groundY = ResolveGroundY();
        shockAnchor = transform.position;
        shockAnchor.y = groundY;
        monster?.InterruptForStun();

        if (headEffectPrefab != null || bodyEffectPrefab != null)
        {
            SpawnStatusVfx(
                MonsterStatus.Shocked,
                headEffectPrefab,
                bodyEffectPrefab,
                null,
                1.15f,
                0.45f,
                0.08f,
                0.55f,
                0.42f);
        }
        else
        {
            EnsureStatusVfx(MonsterStatus.Shocked);
        }

        NotifyChanged();
    }

    public void ApplyBurn(float duration, float totalDamage)
    {
        if (duration <= 0f || totalDamage <= 0f)
            return;

        float dps = totalDamage / Mathf.Max(MonsterElementStatusRules.MinBurnDuration, duration);
        float newEnd = Time.time + duration;

        burn.endTime = newEnd;
        burn.damagePerSecond = dps;
        if (!burnActive)
        {
            burn.damageAccumulator = 0f;
            burnActive = true;
        }

        EnsureStatusVfx(MonsterStatus.Burning);
        NotifyChanged();
    }

    public void ApplyFrost(float duration, int stacksToAdd = 1)
    {
        if (duration <= 0f || stacksToAdd <= 0)
            return;

        float newEnd = Time.time + duration;
        if (!frostActive)
        {
            frost.stacks = Mathf.Min(MonsterElementStatusRules.MaxFrostStacks, stacksToAdd);
            frost.endTime = newEnd;
            frostActive = true;
        }
        else
        {
            frost.stacks = Mathf.Min(MonsterElementStatusRules.MaxFrostStacks, frost.stacks + stacksToAdd);
            frost.endTime = Mathf.Max(frost.endTime, newEnd);
        }

        EnsureStatusVfx(MonsterStatus.Slowed);
        NotifyChanged();
    }

    /// <summary>장판 등에서 초당 누적된 총 동상 중첩을 직접 반영합니다.</summary>
    public void ApplyFrostWithStackCount(float duration, int totalStacks)
    {
        if (duration <= 0f || totalStacks <= 0)
            return;

        frost.stacks = Mathf.Min(MonsterElementStatusRules.MaxFrostStacks, totalStacks);
        frost.endTime = Time.time + duration;
        frostActive = true;
        EnsureStatusVfx(MonsterStatus.Slowed);
        NotifyChanged();
    }

    public void ApplyPoison(float duration, float tickDamagePerStack, float armorReductionPerStack = 0f)
    {
        if (tickDamagePerStack <= 0f)
            return;

        float newEnd = duration <= MonsterElementStatusRules.PermanentDurationThreshold
            ? float.PositiveInfinity
            : Time.time + duration;

        float armorPerStack = armorReductionPerStack > 0f
            ? armorReductionPerStack
            : MonsterElementStatusRules.DefaultPoisonArmorReductionPerStack;

        if (!poisonActive)
        {
            poison.stacks = 1;
            poison.tickDamagePerStack = tickDamagePerStack;
            poison.armorReductionPerStack = armorPerStack;
            poison.endTime = newEnd;
            poisonActive = true;
        }
        else
        {
            poison.stacks++;
            poison.tickDamagePerStack = tickDamagePerStack;
            poison.armorReductionPerStack = armorPerStack;
            poison.endTime = IsPermanent(newEnd) ? float.PositiveInfinity : Mathf.Max(poison.endTime, newEnd);
        }

        EnsurePoisonTickRoutine();
        EnsureStatusVfx(MonsterStatus.Poisoned);
        RefreshArmorReduction();
        NotifyChanged();
    }

    public float GetMoveSpeedMultiplier()
    {
        float multiplier = 1f;

        if (HasFrost)
        {
            float slowRatio = Mathf.Min(
                frost.stacks * MonsterElementStatusRules.FrostSlowPerStack,
                MonsterElementStatusRules.MaxFrostSlow);
            multiplier *= 1f - slowRatio;
        }

        if (Time.time < genericSlowEndTime)
            multiplier *= Mathf.Clamp(genericSlowPercent, 1f, 100f) / 100f;

        return multiplier;
    }

    public void RemoveStatus(MonsterStatus status)
    {
        switch (status)
        {
            case MonsterStatus.Burning:
                ClearBurn();
                break;
            case MonsterStatus.Slowed:
                frostActive = false;
                frost.stacks = 0;
                genericSlowPercent = 100f;
                genericSlowEndTime = 0f;
                ClearStatusVfx(MonsterStatus.Slowed);
                NotifyChanged();
                break;
            case MonsterStatus.Shocked:
                ClearShock();
                break;
            case MonsterStatus.Poisoned:
                ClearPoison();
                break;
            default:
                auxStatuses.Remove(status);
                ClearStatusVfx(status);
                NotifyChanged();
                break;
        }
    }

    public IReadOnlyList<string> BuildStatusDisplayLines()
    {
        displayLineBuffer.Clear();

        if (HasBurn)
            displayLineBuffer.Add("🔥 화상");
        if (HasFrost)
            displayLineBuffer.Add($"❄️ {frost.stacks}/{MonsterElementStatusRules.MaxFrostStacks}");
        if (HasShock)
            displayLineBuffer.Add("⚡ 감전");
        if (HasPoison)
            displayLineBuffer.Add($"☠️ 중독 {poison.stacks}");

        return displayLineBuffer;
    }

    public void ClearForRespawn()
    {
        StopAllCoroutines();
        poisonTickRoutine = null;
        auxDotRoutine = null;
        knockbackRoutine = null;
        knockbackActive = false;

        burnActive = false;
        frostActive = false;
        shockActive = false;
        poisonActive = false;
        burn.endTime = 0f;
        burn.damagePerSecond = 0f;
        burn.damageAccumulator = 0f;
        frost.stacks = 0;
        frost.endTime = 0f;
        shock.endTime = 0f;
        poison.stacks = 0;
        poison.endTime = 0f;
        auxStatuses.Clear();
        genericSlowPercent = 100f;
        genericSlowEndTime = 0f;

        ClearAllVfx();
        RefreshArmorReduction();
        overlayUi?.RefreshDisplay();
        NotifyChanged();
    }

    private void ApplyBurn(DefenseEffectData effect)
    {
        float duration = Mathf.Max(MonsterElementStatusRules.MinBurnDuration, effect.duration);
        float totalDamage = effect.magnitude > 0f
            ? effect.magnitude
            : effect.tickDamage > 0f && duration > 0f
                ? effect.tickDamage * duration
                : effect.tickDamage;
        ApplyBurn(duration, totalDamage);
    }

    private void ApplyFrost(DefenseEffectData effect)
    {
        int stacksToAdd = effect.magnitude > 1f ? Mathf.RoundToInt(effect.magnitude) : 1;
        ApplyFrost(Mathf.Max(MonsterElementStatusRules.MinBurnDuration, effect.duration), stacksToAdd);
    }

    private void ApplyShock(DefenseEffectData effect)
    {
        ApplyShock(Mathf.Max(MonsterElementStatusRules.MinBurnDuration, effect.duration));
    }

    private void ApplyPoison(DefenseEffectData effect)
    {
        if (effect.tickDamage <= 0f)
            return;

        float armorPerStack = effect.magnitude > 0f
            ? effect.magnitude
            : MonsterElementStatusRules.DefaultPoisonArmorReductionPerStack;
        ApplyPoison(effect.duration, effect.tickDamage, armorPerStack);
    }

    private void ApplyGenericSlow(DefenseEffectData effect)
    {
        if (effect.magnitude <= 0f)
            return;

        float endTime = Time.time + Mathf.Max(0.01f, effect.duration);
        float targetPercent = Mathf.Clamp(effect.magnitude, 1f, 100f);
        genericSlowPercent = Time.time >= genericSlowEndTime
            ? targetPercent
            : Mathf.Min(genericSlowPercent, targetPercent);
        genericSlowEndTime = Mathf.Max(genericSlowEndTime, endTime);
        ApplyAuxFromEffect(MonsterStatus.Slowed, effect);
    }

    private void ApplyAuxFromEffect(MonsterStatus status, DefenseEffectData effect)
    {
        var slot = new AuxStatus
        {
            status = status,
            endTime = status == MonsterStatus.Wet && effect.duration <= 0f
                ? Time.time + 4f
                : Time.time + Mathf.Max(0.01f, effect.duration),
            magnitude = effect.magnitude,
            tickDamage = effect.tickDamage,
            damageElement = effect.DamageElement
        };

        if (status == MonsterStatus.Frozen)
        {
            groundY = ResolveGroundY();
            monster?.InterruptForStun();
        }

        ApplyAuxSlot(slot);
        EnsureStatusVfx(status);

        if (slot.tickDamage > 0f && slot.endTime > Time.time)
            EnsureAuxDotRoutine();
    }

    private void ApplyAuxSlot(AuxStatus incoming)
    {
        if (auxStatuses.TryGetValue(incoming.status, out var existing))
        {
            existing.endTime = Mathf.Max(existing.endTime, incoming.endTime);
            if (incoming.tickDamage > 0f)
                existing.tickDamage = incoming.tickDamage;
            if (incoming.magnitude > 0f)
                existing.magnitude = incoming.magnitude;
            existing.damageElement = incoming.damageElement;
            return;
        }

        auxStatuses[incoming.status] = incoming;
    }

    private bool HasAux(MonsterStatus status) =>
        auxStatuses.TryGetValue(status, out var slot) && slot.endTime > Time.time;

    private void ApplyKnockback(DefenseEffectData effect, Vector3? effectSource)
    {
        if (effect.magnitude <= 0f || BlocksAction)
            return;

        Vector3 source = effectSource ?? transform.position - transform.forward;
        Vector3 direction = transform.position - source;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;
        else
            direction.Normalize();

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction, effect.magnitude));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float distance)
    {
        knockbackActive = true;
        const float duration = 0.18f;
        float elapsed = 0f;
        Vector3 start = transform.position;
        Vector3 target = start + direction * distance;
        target.y = ResolveGroundY();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(start, target, t);
            position.y = ResolveGroundY();
            transform.position = position;
            yield return null;
        }

        knockbackActive = false;
        knockbackRoutine = null;
    }

    private void TickBurnDamage()
    {
        if (!HasBurn || health == null || !health.IsAlive)
            return;

        burn.damageAccumulator += burn.damagePerSecond * Time.deltaTime;
        if (burn.damageAccumulator < 1f)
            return;

        float damage = Mathf.Floor(burn.damageAccumulator);
        burn.damageAccumulator -= damage;
        health.TakeDamage(DamageInfo.AoE(damage, DamageElement.Fire, transform.position));
    }

    private void EnsurePoisonTickRoutine()
    {
        if (poisonTickRoutine != null)
            return;
        poisonTickRoutine = StartCoroutine(PoisonTickRoutine());
    }

    private IEnumerator PoisonTickRoutine()
    {
        var wait = new WaitForSeconds(MonsterElementStatusRules.PoisonTickInterval);
        while (HasPoison)
        {
            if (health != null && health.IsAlive)
            {
                float totalTick = poison.tickDamagePerStack * poison.stacks;
                health.TakeDamage(DamageInfo.AoE(totalTick, DamageElement.Pink, transform.position));
            }

            yield return wait;
            if (!HasPoison)
                break;
        }

        poisonTickRoutine = null;
    }

    private void EnsureAuxDotRoutine()
    {
        if (auxDotRoutine != null)
            return;
        auxDotRoutine = StartCoroutine(AuxDotRoutine());
    }

    private IEnumerator AuxDotRoutine()
    {
        var wait = new WaitForSeconds(MonsterElementStatusRules.PoisonTickInterval);
        while (HasAnyAuxDot())
        {
            if (health != null && health.IsAlive)
            {
                foreach (var pair in auxStatuses)
                {
                    var slot = pair.Value;
                    if (slot.tickDamage <= 0f || slot.endTime <= Time.time)
                        continue;
                    health.TakeDamage(DamageInfo.AoE(slot.tickDamage, slot.damageElement, transform.position));
                }
            }

            yield return wait;
            if (!HasAnyAuxDot())
                break;
        }

        auxDotRoutine = null;
    }

    private bool HasAnyAuxDot()
    {
        foreach (var pair in auxStatuses)
        {
            if (pair.Value.tickDamage > 0f && pair.Value.endTime > Time.time)
                return true;
        }

        return false;
    }

    private void ExpireElementalStatuses()
    {
        if (burnActive && burn.endTime <= Time.time)
            ClearBurn();
        if (frostActive && frost.endTime <= Time.time)
        {
            frostActive = false;
            frost.stacks = 0;
            ClearStatusVfx(MonsterStatus.Slowed);
            NotifyChanged();
        }
        if (shockActive && shock.endTime <= Time.time)
            ClearShock();
        if (poisonActive && !IsPermanent(poison.endTime) && poison.endTime <= Time.time)
            ClearPoison();
    }

    private void ExpireAuxStatuses()
    {
        if (auxStatuses.Count == 0)
            return;

        expireBuffer.Clear();
        foreach (var pair in auxStatuses)
        {
            if (pair.Value.endTime <= Time.time)
                expireBuffer.Add(pair.Key);
        }

        for (int i = 0; i < expireBuffer.Count; i++)
            RemoveStatus(expireBuffer[i]);
    }

    private void ExpireGenericSlow()
    {
        if (Time.time < genericSlowEndTime || genericSlowEndTime <= 0f)
            return;

        genericSlowPercent = 100f;
        genericSlowEndTime = 0f;
        if (!HasFrost)
            ClearStatusVfx(MonsterStatus.Slowed);
    }

    private void ClearBurn()
    {
        burnActive = false;
        burn.damagePerSecond = 0f;
        burn.damageAccumulator = 0f;
        ClearStatusVfx(MonsterStatus.Burning);
        NotifyChanged();
    }

    private void ClearShock()
    {
        shockActive = false;
        ClearStatusVfx(MonsterStatus.Shocked);
        NotifyChanged();
    }

    private void ClearPoison()
    {
        poisonActive = false;
        poison.stacks = 0;
        if (poisonTickRoutine != null)
        {
            StopCoroutine(poisonTickRoutine);
            poisonTickRoutine = null;
        }

        ClearStatusVfx(MonsterStatus.Poisoned);
        RefreshArmorReduction();
        NotifyChanged();
    }

    private void RefreshArmorReduction()
    {
        if (health == null)
            return;

        float reduction = HasPoison ? poison.stacks * poison.armorReductionPerStack : 0f;
        health.SetFlatDefenseReduction(reduction);
    }

    private void ApplyShockJitterIfNeeded()
    {
        if (!HasShock)
            return;

        Vector3 jitter = new Vector3(
            UnityEngine.Random.Range(-shockedJitterStrength, shockedJitterStrength),
            0f,
            UnityEngine.Random.Range(-shockedJitterStrength, shockedJitterStrength));

        Vector3 position = shockAnchor + jitter;
        position.y = groundY;
        transform.position = position;
    }

    private void ApplyElectrifiedJitterIfNeeded()
    {
        if (!HasAux(MonsterStatus.Electrified))
            return;

        Vector3 jitter = new Vector3(
            UnityEngine.Random.Range(-electrifiedJitterStrength, electrifiedJitterStrength),
            UnityEngine.Random.Range(-electrifiedJitterStrength * 0.35f, electrifiedJitterStrength * 0.35f),
            UnityEngine.Random.Range(-electrifiedJitterStrength, electrifiedJitterStrength));

        transform.position += jitter * Time.deltaTime * 18f;
    }

    private void HandleDeath()
    {
        ClearForRespawn();
        overlayUi?.ClearAndDestroy();
    }

    private void NotifyChanged()
    {
        StatusesChanged?.Invoke();
        overlayUi?.RefreshDisplay();
    }

    private float ResolveGroundY() => monster != null ? monster.GroundY : groundY;

    private static bool IsPermanent(float endTime) => float.IsPositiveInfinity(endTime);

    private void EnsureStatusVfx(MonsterStatus status)
    {
        var catalog = DefenseCombatCatalog.Active;
        if (catalog == null)
            return;

        if (catalog.TryGetMonsterStatusVfx(status, out var entry))
        {
            SpawnStatusVfx(status, entry.headPrefab, entry.bodyPrefab, entry.footPrefab,
                entry.headLocalY, entry.bodyLocalY, entry.footLocalY, entry.bodyScale, entry.footScale);
        }
    }

    private void SpawnStatusVfx(
        MonsterStatus status,
        GameObject headPrefab,
        GameObject bodyPrefab,
        GameObject footPrefab,
        float headY,
        float bodyY,
        float footY,
        float bodyScale,
        float footScale)
    {
        if (headPrefab == null && bodyPrefab == null && footPrefab == null)
            return;

        if (!statusVfx.TryGetValue(status, out var bundle))
        {
            bundle = new StatusVfxBundle();
            statusVfx[status] = bundle;
        }

        if (bundle.head == null && headPrefab != null)
        {
            bundle.head = Instantiate(headPrefab, transform);
            bundle.head.transform.localPosition = new Vector3(0f, headY, 0f);
        }

        if (bundle.body == null && bodyPrefab != null)
        {
            bundle.body = Instantiate(bodyPrefab, transform);
            bundle.body.transform.localPosition = new Vector3(0f, bodyY, 0f);
            bundle.body.transform.localScale = Vector3.one * bodyScale;
        }

        if (bundle.foot == null && footPrefab != null)
        {
            bundle.foot = Instantiate(footPrefab, transform);
            bundle.foot.transform.localPosition = new Vector3(0f, footY, 0f);
            bundle.foot.transform.localScale = Vector3.one * footScale;
        }
    }

    private void ClearStatusVfx(MonsterStatus status)
    {
        if (!statusVfx.TryGetValue(status, out var bundle))
            return;

        if (bundle.head != null) Destroy(bundle.head);
        if (bundle.body != null) Destroy(bundle.body);
        if (bundle.foot != null) Destroy(bundle.foot);
        statusVfx.Remove(status);
    }

    private void ClearAllVfx()
    {
        foreach (var pair in statusVfx)
        {
            if (pair.Value.head != null) Destroy(pair.Value.head);
            if (pair.Value.body != null) Destroy(pair.Value.body);
            if (pair.Value.foot != null) Destroy(pair.Value.foot);
        }

        statusVfx.Clear();
    }
}
