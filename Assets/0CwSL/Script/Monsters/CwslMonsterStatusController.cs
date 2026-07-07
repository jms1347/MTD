using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// UkDefense MonsterStatusController 포팅 — 화상/동상/감전/중독 (서버 권한).
/// </summary>
public class CwslMonsterStatusController : MonoBehaviour
{
    private sealed class BurnState
    {
        public float EndTime;
        public float DamagePerSecond;
        public float DamageAccumulator;
        public ulong AttackerClientId;
    }

    private sealed class FrostState
    {
        public int Stacks;
        public float EndTime;
        public ulong AttackerClientId;
    }

    private sealed class ShockState
    {
        public float EndTime;
    }

    private sealed class PoisonState
    {
        public int Stacks;
        public float EndTime;
        public float TickDamagePerStack;
        public float ArmorReductionPerStack;
        public ulong AttackerClientId;
    }

    private readonly BurnState burn = new();
    private readonly FrostState frost = new();
    private readonly ShockState shock = new();
    private readonly PoisonState poison = new();

    private CwslMonsterHealth health;
    private Coroutine poisonTickRoutine;
    private bool burnActive;
    private bool frostActive;
    private bool shockActive;
    private bool poisonActive;

    public bool HasBurn => burnActive && burn.EndTime > Time.time;
    public bool HasFrost => frostActive && frost.Stacks > 0 && frost.EndTime > Time.time;
    public bool HasShock => shockActive && shock.EndTime > Time.time;
    public bool HasPoison => poisonActive && poison.Stacks > 0 && (IsPermanent(poison.EndTime) || poison.EndTime > Time.time);

    public bool BlocksMovement => HasShock;
    public bool BlocksAction => HasShock;

    public static CwslMonsterStatusController Ensure(Component target)
    {
        if (target == null)
            return null;

        var existing = target.GetComponent<CwslMonsterStatusController>();
        if (existing != null)
            return existing;

        return target.gameObject.AddComponent<CwslMonsterStatusController>();
    }

    private void Awake()
    {
        health = GetComponent<CwslMonsterHealth>();
    }

    private void Update()
    {
        if (!IsServerAuthority())
            return;

        if (health != null && !health.IsAlive)
            return;

        ExpireStatuses();
        TickBurnDamage();
    }

    public float GetMoveSpeedMultiplier()
    {
        if (!HasFrost)
            return 1f;

        var slowRatio = Mathf.Min(
            frost.Stacks * CwslMonsterElementStatusRules.FrostSlowPerStack,
            CwslMonsterElementStatusRules.MaxFrostSlow);
        return 1f - slowRatio;
    }

    public float GetFlatDefenseReduction()
    {
        return HasPoison ? poison.Stacks * poison.ArmorReductionPerStack : 0f;
    }

    public void ApplyBurnServer(ulong attackerClientId, float duration, float totalDamage)
    {
        if (!IsServerAuthority() || duration <= 0f || totalDamage <= 0f)
            return;

        if (health != null && !health.IsAlive)
            return;

        var dps = totalDamage / Mathf.Max(CwslMonsterElementStatusRules.MinBurnDuration, duration);
        burn.EndTime = Time.time + duration;
        burn.DamagePerSecond = dps;
        burn.AttackerClientId = attackerClientId;
        if (!burnActive)
        {
            burn.DamageAccumulator = 0f;
            burnActive = true;
        }

        SyncStatusVfx(CwslMonsterStatusKind.Burning, true);
    }

    public void ApplyFrostServer(ulong attackerClientId, float duration, int stacksToAdd = 1)
    {
        if (!IsServerAuthority() || duration <= 0f || stacksToAdd <= 0)
            return;

        if (health != null && !health.IsAlive)
            return;

        var newEnd = Time.time + duration;
        if (!frostActive)
        {
            frost.Stacks = Mathf.Min(CwslMonsterElementStatusRules.MaxFrostStacks, stacksToAdd);
            frost.EndTime = newEnd;
            frost.AttackerClientId = attackerClientId;
            frostActive = true;
        }
        else
        {
            frost.Stacks = Mathf.Min(
                CwslMonsterElementStatusRules.MaxFrostStacks,
                frost.Stacks + stacksToAdd);
            frost.EndTime = Mathf.Max(frost.EndTime, newEnd);
        }

        SyncStatusVfx(CwslMonsterStatusKind.Slowed, true);
    }

    public void ApplyShockServer(float duration)
    {
        if (!IsServerAuthority() || duration <= 0f)
            return;

        if (health != null && !health.IsAlive)
            return;

        var newEnd = Time.time + duration;
        if (shockActive)
            shock.EndTime = Mathf.Max(shock.EndTime, newEnd);
        else
        {
            shock.EndTime = newEnd;
            shockActive = true;
        }

        SyncStatusVfx(CwslMonsterStatusKind.Shocked, true);
    }

    public void ApplyPoisonServer(
        ulong attackerClientId,
        float duration,
        float tickDamagePerStack,
        float armorReductionPerStack = -1f)
    {
        if (!IsServerAuthority() || tickDamagePerStack <= 0f)
            return;

        if (health != null && !health.IsAlive)
            return;

        var newEnd = duration <= CwslMonsterElementStatusRules.PermanentDurationThreshold
            ? float.PositiveInfinity
            : Time.time + duration;
        var armorPerStack = armorReductionPerStack > 0f
            ? armorReductionPerStack
            : CwslMonsterElementStatusRules.DefaultPoisonArmorReductionPerStack;

        if (!poisonActive)
        {
            poison.Stacks = 1;
            poison.TickDamagePerStack = tickDamagePerStack;
            poison.ArmorReductionPerStack = armorPerStack;
            poison.EndTime = newEnd;
            poison.AttackerClientId = attackerClientId;
            poisonActive = true;
        }
        else
        {
            poison.Stacks++;
            poison.TickDamagePerStack = tickDamagePerStack;
            poison.ArmorReductionPerStack = armorPerStack;
            poison.EndTime = IsPermanent(newEnd) ? float.PositiveInfinity : Mathf.Max(poison.EndTime, newEnd);
        }

        EnsurePoisonTickRoutine();
        SyncStatusVfx(CwslMonsterStatusKind.Poisoned, true);
    }

    public void ClearFrostServer()
    {
        if (!IsServerAuthority() || !frostActive)
            return;

        ClearFrost();
    }

    public void ClearForPoolServer()
    {
        if (!IsServerAuthority())
            return;

        if (poisonTickRoutine != null)
        {
            StopCoroutine(poisonTickRoutine);
            poisonTickRoutine = null;
        }

        burnActive = false;
        frostActive = false;
        shockActive = false;
        poisonActive = false;
        burn.EndTime = 0f;
        burn.DamagePerSecond = 0f;
        burn.DamageAccumulator = 0f;
        frost.Stacks = 0;
        frost.EndTime = 0f;
        shock.EndTime = 0f;
        poison.Stacks = 0;
        poison.EndTime = 0f;

        if (health != null && health.IsSpawned)
            health.ClearMonsterStatusVfxServer();
    }

    private void TickBurnDamage()
    {
        if (!HasBurn || health == null || !health.IsAlive)
            return;

        burn.DamageAccumulator += burn.DamagePerSecond * Time.deltaTime;
        if (burn.DamageAccumulator < 1f)
            return;

        var damage = Mathf.Floor(burn.DamageAccumulator);
        burn.DamageAccumulator -= damage;
        health.DamageFromPlayer(burn.AttackerClientId, damage, (int)CwslDamagePopupKind.Monster);
    }

    private void EnsurePoisonTickRoutine()
    {
        if (poisonTickRoutine != null)
            return;

        poisonTickRoutine = StartCoroutine(PoisonTickRoutine());
    }

    private IEnumerator PoisonTickRoutine()
    {
        var wait = new WaitForSeconds(CwslMonsterElementStatusRules.PoisonTickInterval);
        while (HasPoison)
        {
            if (health != null && health.IsAlive)
            {
                var tickDamage = poison.TickDamagePerStack * poison.Stacks;
                health.DamageFromPlayer(
                    poison.AttackerClientId,
                    tickDamage,
                    (int)CwslDamagePopupKind.Poison);
            }

            yield return wait;
            if (!HasPoison)
                break;
        }

        poisonTickRoutine = null;
    }

    private void ExpireStatuses()
    {
        if (burnActive && burn.EndTime <= Time.time)
            ClearBurn();
        if (frostActive && frost.EndTime <= Time.time)
            ClearFrost();
        if (shockActive && shock.EndTime <= Time.time)
            ClearShock();
        if (poisonActive && !IsPermanent(poison.EndTime) && poison.EndTime <= Time.time)
            ClearPoison();
    }

    private void ClearBurn()
    {
        burnActive = false;
        burn.DamagePerSecond = 0f;
        burn.DamageAccumulator = 0f;
        SyncStatusVfx(CwslMonsterStatusKind.Burning, false);
    }

    private void ClearFrost()
    {
        frostActive = false;
        frost.Stacks = 0;
        SyncStatusVfx(CwslMonsterStatusKind.Slowed, false);
    }

    private void ClearShock()
    {
        shockActive = false;
        SyncStatusVfx(CwslMonsterStatusKind.Shocked, false);
    }

    private void ClearPoison()
    {
        poisonActive = false;
        poison.Stacks = 0;
        if (poisonTickRoutine != null)
        {
            StopCoroutine(poisonTickRoutine);
            poisonTickRoutine = null;
        }

        SyncStatusVfx(CwslMonsterStatusKind.Poisoned, false);
    }

    private void SyncStatusVfx(CwslMonsterStatusKind kind, bool active)
    {
        if (health != null && health.IsSpawned)
            health.SyncMonsterStatusVfxServer(kind, active);
    }

    private static bool IsPermanent(float endTime) => float.IsPositiveInfinity(endTime);

    private static bool IsServerAuthority()
    {
        var network = NetworkManager.Singleton;
        return network == null || network.IsServer;
    }
}
