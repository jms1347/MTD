using Unity.Netcode;
using UnityEngine;

/// <summary>언월도 평타·질주대회전·청룡검기.</summary>
public class StllGlaiveCombat : NetworkBehaviour
{
    private StllGlaiveAim aim;
    private StllHorseMotor motor;
    private StllMountAssembly mountAssembly;
    private StllGlaiveSwingVisual swingVisual;
    private StllBrotherhoodRoleState roleState;
    private StllPlayerMoveModifiers moveModifiers;

    private float nextBasicAttackTime;
    private float nextSpinTime;
    private float spinEndTime;
    private float spinStartTime;
    private int spinSwingsDone;

    public bool IsSpinning => spinEndTime > Time.time;

    private void Awake()
    {
        aim = GetComponent<StllGlaiveAim>();
        motor = GetComponent<StllHorseMotor>();
        mountAssembly = GetComponent<StllMountAssembly>();
        swingVisual = GetComponent<StllGlaiveSwingVisual>();
        roleState = GetComponent<StllBrotherhoodRoleState>();
        moveModifiers = GetComponent<StllPlayerMoveModifiers>();
    }

    public bool TryBasicAttackServer()
    {
        if (!IsServer || IsSpinning || Time.time < nextBasicAttackTime)
            return false;

        nextBasicAttackTime = Time.time + StllGlaiveConstants.BasicAttackCooldown;
        var origin = transform.position + Vector3.up * 0.9f;
        var forward = aim != null ? aim.AimDirection : transform.forward;
        var halfAngle = StllGlaiveConstants.BasicAttackHalfAngleDeg;

        var enemies = FindObjectsByType<StllEnemyHealth>(FindObjectsSortMode.None);
        for (var i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            if (!IsInFan(origin, forward, enemy.transform.position, halfAngle, StllGlaiveConstants.BasicAttackRange))
                continue;

            enemy.TakeDamageServer(
                StllGlaiveConstants.BasicAttackDamage * GetAttackMultiplier(),
                OwnerClientId,
                forward * StllGlaiveConstants.BasicAttackKnockback);

            if (Random.value < StllGlaiveConstants.QinglongProcChance)
                StllQinglongWave.Spawn(origin, forward, StllGlaiveConstants.QinglongProjectileDamage, OwnerClientId);
        }

        DamageBossesInFan(origin, forward, halfAngle, StllGlaiveConstants.BasicAttackRange,
            StllGlaiveConstants.BasicAttackDamage * GetAttackMultiplier());

        var cardInventory = GetComponent<StllPlayerCardInventory>();
        if (cardInventory != null)
        {
            var extraChance = cardInventory.GetPassiveBonus(StllPassiveBonusType.ExtraAttackChance);
            if (Random.value < extraChance)
                DamageBossesInFan(origin, forward, halfAngle, StllGlaiveConstants.BasicAttackRange,
                    StllGlaiveConstants.BasicAttackDamage * GetAttackMultiplier() * 0.5f);
        }

        PlayBasicSwingClientRpc(forward);
        return true;
    }

    public bool TryChargeSpinServer()
    {
        if (!IsServer || IsSpinning || Time.time < nextSpinTime)
            return false;

        nextSpinTime = Time.time + StllGlaiveConstants.ChargeSpinCooldown;
        spinStartTime = Time.time;
        spinEndTime = Time.time + StllGlaiveConstants.ChargeSpinDuration;
        spinSwingsDone = 0;
        PlayChargeSpinClientRpc();
        return true;
    }

    private void Update()
    {
        if (!IsServer || !IsSpinning)
            return;

        var interval = StllGlaiveConstants.ChargeSpinDuration / StllGlaiveConstants.ChargeSpinSwings;
        while (spinSwingsDone < StllGlaiveConstants.ChargeSpinSwings &&
               Time.time >= spinStartTime + interval * (spinSwingsDone + 1))
        {
            ApplySpinHitServer();
            spinSwingsDone++;
        }
    }

    private void ApplySpinHitServer()
    {
        var center = transform.position;
        var enemies = FindObjectsByType<StllEnemyHealth>(FindObjectsSortMode.None);
        for (var i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            var flat = enemy.transform.position - center;
            flat.y = 0f;
            if (flat.magnitude > StllGlaiveConstants.ChargeSpinRadius)
                continue;

            var knockDir = flat.sqrMagnitude > 0.01f ? flat.normalized : Random.insideUnitSphere;
            knockDir.y = 0f;
            enemy.TakeDamageServer(
                StllGlaiveConstants.ChargeSpinDamagePerHit * GetAttackMultiplier(),
                OwnerClientId,
                knockDir * StllGlaiveConstants.ChargeSpinKnockback);
        }
    }

    private float GetAttackMultiplier()
    {
        if (moveModifiers != null)
            return moveModifiers.GetDamageMultiplier();

        if (roleState == null)
            return 1f;

        return StllRoleCombatModifiers.GetAttackDamageMultiplier(roleState.Role);
    }

    private void DamageBossesInFan(Vector3 origin, Vector3 forward, float halfAngle, float range, float damage)
    {
        var minibosses = FindObjectsByType<StllMiniBossHuangYing>(FindObjectsSortMode.None);
        for (var i = 0; i < minibosses.Length; i++)
        {
            var boss = minibosses[i];
            if (boss == null || !boss.IsAlive)
                continue;

            if (!IsInFan(origin, forward, boss.transform.position, halfAngle, range))
                continue;

            boss.DamageServer(damage, OwnerClientId, forward * 2f);
        }

        var lubu = FindFirstObjectByType<StllBossLuBu>();
        if (lubu != null && lubu.IsAlive && IsInFan(origin, forward, lubu.transform.position, halfAngle, range))
            lubu.DamageServer(damage);
    }

    private static bool IsInFan(Vector3 origin, Vector3 forward, Vector3 target, float halfAngleDeg, float range)
    {
        var toTarget = target - origin;
        toTarget.y = 0f;
        var distance = toTarget.magnitude;
        if (distance > range || distance < 0.05f)
            return false;

        var angle = Vector3.Angle(forward, toTarget);
        return angle <= halfAngleDeg;
    }

    [ClientRpc]
    private void PlayBasicSwingClientRpc(Vector3 forward)
    {
        swingVisual?.PlayBasicSwing(forward);
    }

    [ClientRpc]
    private void PlayChargeSpinClientRpc()
    {
        swingVisual?.PlayChargeSpin(
            StllGlaiveConstants.ChargeSpinDuration,
            StllGlaiveConstants.ChargeSpinSwings);
    }
}
