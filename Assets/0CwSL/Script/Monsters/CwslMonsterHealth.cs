using System;
using Unity.Netcode;
using UnityEngine;

public class CwslMonsterHealth : NetworkBehaviour, ICwslPooledNetworkObject
{
    private readonly NetworkVariable<float> health = new(
        CwslGameConstants.MonsterMaxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<byte> syncedMonsterType = new(
        (byte)CwslMonsterType.Melee,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private int dropGoldAmount = CwslGameConstants.GoldDropNormal;
    private float maxHealth = CwslGameConstants.MonsterMaxHealth;
    private bool isExecutive;
    private bool suppressSyncedVisualCallbacks;

    public CwslMonsterType MonsterType { get; private set; }
    public bool IsAlive => health.Value > 0f;
    public float CurrentHealth => health.Value;
    public float MaxHealth => maxHealth;
    public bool IsExecutive => isExecutive;
    public bool IsBoss => MonsterType is CwslMonsterType.BossHongmyeongbo or CwslMonsterType.DefenseBoss;

    public Vector3 GetAimPoint()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
            return transform.TransformPoint(capsule.center);

        return transform.position + Vector3.up * CwslGameConstants.MonsterHitCenterY;
    }

    public Vector3 GetFlatHitPoint()
    {
        var worldCenter = GetAimPoint();
        return new Vector3(worldCenter.x, transform.position.y, worldCenter.z);
    }

    public Vector3 GetDamagePopupAnchor()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            var top = transform.TransformPoint(capsule.center + Vector3.up * (capsule.height * 0.5f));
            return top + Vector3.up * (0.22f * transform.lossyScale.y);
        }

        return transform.position + Vector3.up *
            (CwslGameConstants.MonsterHitCenterY + CwslGameConstants.MonsterHitHeight * 0.5f + 0.22f);
    }

    public float GetFlatHitRadius()
    {
        var capsule = GetComponent<CapsuleCollider>();
        var localRadius = capsule != null
            ? capsule.radius
            : CwslGameConstants.MonsterHitMinRadius;
        var scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        var radius = localRadius * scale;

        if (MonsterType is CwslMonsterType.Ranged or CwslMonsterType.NexusRanged
            or CwslMonsterType.InkSniper or CwslMonsterType.NexusInkSniper)
            radius = Mathf.Max(radius, 0.72f);

        return radius;
    }

    public event Action<CwslMonsterHealth, ulong> OnKilled;
    public event Action<CwslMonsterHealth, float, ulong> OnDamaged;

    public void Configure(CwslMonsterType type, int goldDrop = -1, bool executive = false, float healthMultiplier = 1f)
    {
        MonsterType = type;
        isExecutive = executive;
        dropGoldAmount = goldDrop >= 0
            ? goldDrop
            : executive
                ? CwslGameConstants.GoldDropExecutive
                : CwslGameConstants.GoldDropNormal;
        maxHealth = CwslMonsterStatCatalog.GetMaxHealth(type) * Mathf.Max(0.01f, healthMultiplier);
        if (IsServer)
            syncedMonsterType.Value = (byte)type;
    }

    public void ConfigureBoss(float bossMaxHealth)
    {
        MonsterType = CwslMonsterType.BossHongmyeongbo;
        isExecutive = false;
        dropGoldAmount = 0;
        maxHealth = CwslMonsterStatCatalog.BossHongmyeongboHealth;
        RefreshBossHitCollider();
        if (IsServer)
        {
            syncedMonsterType.Value = (byte)CwslMonsterType.BossHongmyeongbo;
            health.Value = maxHealth;
        }
    }

    public override void OnNetworkSpawn()
    {
        syncedMonsterType.OnValueChanged += HandleSyncedMonsterTypeChanged;
        EnsureCombatHitCollider();
        EnsureHealthBar();
        EnsureStatusController();
        SyncMonsterVisualsForNetwork();

        if (IsServer && !IsBoss)
            health.Value = maxHealth;

        CwslCombatRegistry.RegisterMonster(this);
    }

    public override void OnNetworkDespawn()
    {
        CwslCombatRegistry.UnregisterMonster(this);
        syncedMonsterType.OnValueChanged -= HandleSyncedMonsterTypeChanged;
        base.OnNetworkDespawn();
    }

    private void HandleSyncedMonsterTypeChanged(byte previousValue, byte newValue)
    {
        if (suppressSyncedVisualCallbacks || previousValue == newValue)
            return;

        ApplySyncedMonsterVisuals((CwslMonsterType)newValue);
    }

    private void SyncMonsterVisualsForNetwork()
    {
        ApplySyncedMonsterVisuals(GetResolvedVisualType());
    }

    private CwslMonsterType GetResolvedVisualType()
    {
        if (IsServer && MonsterType != default)
            return MonsterType;

        return (CwslMonsterType)syncedMonsterType.Value;
    }

    private void ApplySyncedMonsterVisuals(CwslMonsterType type)
    {
        if (suppressSyncedVisualCallbacks || !isActiveAndEnabled || health.Value <= 0f)
            return;

        MonsterType = type;
        CwslMonsterVisualRefresh.Refresh(transform, type);
        CwslMonsterMaterialFix.Refresh(transform, type);

        var monsterBase = GetComponent<CwslMonsterBase>();
        monsterBase?.EnsureClientVisuals(type);

        if (isExecutive)
            CwslMonsterExecutiveVisual.Apply(transform);
    }

    public void OnSpawnedFromPool()
    {
        EnsureCombatHitCollider();
        EnsureKnockback();
        EnsureStun();
        SyncMonsterVisualsForNetwork();
        SyncHealthAfterConfigureServer();
    }

    private void EnsureKnockback()
    {
        if (GetComponent<CwslMonsterKnockback>() == null)
            gameObject.AddComponent<CwslMonsterKnockback>();
    }

    private void EnsureStun()
    {
        if (GetComponent<CwslMonsterStun>() == null)
            gameObject.AddComponent<CwslMonsterStun>();
    }

    private void EnsureHealthBar()
    {
        if (GetComponent<CwslMonsterHealthBar>() == null)
            gameObject.AddComponent<CwslMonsterHealthBar>();
    }

    private void EnsureStatusController()
    {
        if (GetComponent<CwslMonsterStatusController>() == null)
            gameObject.AddComponent<CwslMonsterStatusController>();
    }

    public void OnReturnedToPool()
    {
        isExecutive = false;
        dropGoldAmount = CwslGameConstants.GoldDropNormal;
        maxHealth = CwslGameConstants.MonsterMaxHealth;
        MonsterType = default;
        GetComponent<CwslMonsterHitShield>()?.ClearServer();
        GetComponent<CwslMonsterStatusController>()?.ClearForPoolServer();

        if (!IsServer)
            return;

        suppressSyncedVisualCallbacks = true;
        syncedMonsterType.Value = (byte)CwslMonsterType.Melee;
        suppressSyncedVisualCallbacks = false;
    }

    private void EnsureCombatHitCollider()
    {
        RefreshCombatHitCollider();
    }

    public void RefreshCombatHitCollider()
    {
        if (GetComponent<CwslBossHongmyeongbo>() != null)
        {
            RefreshBossHitCollider();
            return;
        }

        var capsule = GetComponent<CapsuleCollider>();
        if (capsule == null)
            return;

        capsule.enabled = true;
        capsule.isTrigger = true;
        capsule.direction = 1;
        capsule.center = new Vector3(0f, CwslGameConstants.MonsterHitCenterY, 0f);
        capsule.height = CwslGameConstants.MonsterHitHeight;
        if (capsule.radius < CwslGameConstants.MonsterHitMinRadius)
            capsule.radius = CwslGameConstants.MonsterHitMinRadius;

        if (isExecutive && capsule.radius < 0.72f)
            capsule.radius = 0.72f;

        if (MonsterType is CwslMonsterType.Ranged or CwslMonsterType.NexusRanged
            or CwslMonsterType.InkSniper or CwslMonsterType.NexusInkSniper)
            capsule.radius = Mathf.Max(capsule.radius, 0.62f);

        gameObject.layer = LayerMask.NameToLayer(CwslGameConstants.LayerMonster);
    }

    public void SyncHealthAfterConfigureServer()
    {
        if (!IsServer || IsBoss)
            return;

        health.Value = maxHealth;
    }

    public void RefreshBossHitCollider()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule == null)
            return;

        var scale = CwslGameConstants.BossVisualScale;
        capsule.isTrigger = true;
        capsule.direction = 1;
        capsule.height = 4.2f * scale;
        capsule.radius = 1.4f * scale;
        capsule.center = new Vector3(0f, 2.1f * scale, 0f);
    }

    public void DamageFromPlayer(ulong attackerClientId, float amount)
    {
        DamageFromPlayer(attackerClientId, amount, (int)CwslDamagePopupKind.Monster);
    }

    public void DamageFromPlayer(ulong attackerClientId, float amount, int popupKind)
    {
        if (!IsServer || !IsAlive || amount <= 0f)
            return;

        var hitShield = GetComponent<CwslMonsterHitShield>();
        if (hitShield != null && hitShield.TryConsumeHitServer())
            return;

        var scaledAmount = ApplyAttackerBuffs(attackerClientId, amount);
        var status = GetComponent<CwslMonsterStatusController>();
        var monsterDefense = CwslMonsterStatCatalog.GetDefense(MonsterType);
        if (status != null)
            monsterDefense = Mathf.Max(0f, monsterDefense - status.GetFlatDefenseReduction());

        scaledAmount = CwslCombatMath.ResolveDamageAfterDefense(scaledAmount, monsterDefense);

        var runtime = GetComponent<CwslMonsterRuntimeStats>();
        if (runtime != null && runtime.DefenseMultiplier > 0f)
            scaledAmount *= runtime.DefenseMultiplier;

        var popup = (CwslDamagePopupKind)popupKind;

        if (IsBoss && MonsterType == CwslMonsterType.BossHongmyeongbo)
        {
            if (!CwslBossHongmyeongbo.CanReceiveDamageFrom(attackerClientId))
                return;

            health.Value = Mathf.Max(0f, health.Value - scaledAmount);
            CwslDamageFeedback.PlayFromServer(GetDamagePopupAnchor(), scaledAmount, popup);
            OnDamaged?.Invoke(this, health.Value, attackerClientId);
            GetComponent<CwslBossHongmyeongbo>()?.NotifyDamagedServer(health.Value);

            if (health.Value <= 0f)
                Die(attackerClientId, dropGold: false);

            return;
        }

        if (MonsterType is CwslMonsterType.MidBoss or CwslMonsterType.DefenseBoss or CwslMonsterType.SeniorCoach)
        {
            health.Value = Mathf.Max(0f, health.Value - scaledAmount);
            CwslDamageFeedback.PlayFromServer(GetDamagePopupAnchor(), scaledAmount, popup);
            OnDamaged?.Invoke(this, health.Value, attackerClientId);
            if (health.Value <= 0f)
                Die(attackerClientId, dropGold: false);
            return;
        }

        health.Value = Mathf.Max(0f, health.Value - scaledAmount);
        CwslDamageFeedback.PlayFromServer(GetDamagePopupAnchor(), scaledAmount, popup);
        OnDamaged?.Invoke(this, health.Value, attackerClientId);
        NotifyHitFlinchFromDamageServer(attackerClientId, scaledAmount);
        if (health.Value <= 0f)
            Die(attackerClientId);
    }

    private void NotifyHitFlinchFromDamageServer(ulong attackerClientId, float amount)
    {
        if (!IsServer || amount <= 0f)
            return;

        var direction = Vector3.forward;
        if (NetworkManager.Singleton != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId != attackerClientId || client.PlayerObject == null)
                    continue;

                direction = transform.position - client.PlayerObject.transform.position;
                break;
            }
        }

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = -transform.forward;

        NotifyHitFlinchServer(direction.normalized, Mathf.Clamp(amount * 0.004f, 0.08f, 0.35f));
    }

    private static float ApplyAttackerBuffs(ulong attackerClientId, float amount)
    {
        if (NetworkManager.Singleton == null)
            return amount;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != attackerClientId || client.PlayerObject == null)
                continue;

            return amount * CwslArenaBuffSystem.GetPlayerDamageMultiplier(client.PlayerObject.transform.position);
        }

        return amount;
    }

    public void ForceKillServer(ulong attackerClientId = ulong.MaxValue, bool dropGold = true)
    {
        if (!IsServer || !IsAlive)
            return;

        Die(attackerClientId, dropGold);
    }

    public void ForceKillWithGoldAtServer(Vector3 goldPosition, ulong attackerClientId = ulong.MaxValue)
    {
        if (!IsServer || !IsAlive)
            return;

        health.Value = 0f;
        if (dropGoldAmount > 0)
            CwslGoldDropService.SpawnDrop(goldPosition, dropGoldAmount);
        OnKilled?.Invoke(this, attackerClientId);
        PlayDeathClientRpc(transform.position, (int)MonsterType);

        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }

    private void Die(ulong attackerClientId, bool dropGold = true)
    {
        health.Value = 0f;
        if (dropGold && !IsBoss)
            CwslPillDropService.RollMonsterLoot(transform.position);
        OnKilled?.Invoke(this, attackerClientId);
        PlayDeathClientRpc(transform.position, (int)MonsterType);

        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }

    public void SyncMonsterStatusVfxServer(CwslMonsterStatusKind kind, bool active)
    {
        if (!IsServer)
            return;

        SyncMonsterStatusVfxClientRpc((byte)kind, active);
    }

    public void ClearMonsterStatusVfxServer()
    {
        if (!IsServer)
            return;

        ClearMonsterStatusVfxClientRpc();
    }

    public void NotifyMonsterStunVisualServer(Vector3 impactPosition, float durationSeconds)
    {
        if (!IsServer)
            return;

        NotifyMonsterStunClientRpc(impactPosition, durationSeconds);
    }

    public void NotifyHitFlinchServer(Vector3 worldDirection, float distance)
    {
        if (!IsServer)
            return;

        NotifyHitFlinchClientRpc(worldDirection, distance);
    }

    public void NotifyHitShieldGrantedServer()
    {
        if (!IsServer)
            return;

        NotifyHitShieldGrantedClientRpc();
    }

    public void NotifyHitShieldBlockedServer()
    {
        if (!IsServer)
            return;

        NotifyHitShieldBlockedClientRpc();
    }

    public void NotifyHitShieldBrokenServer()
    {
        if (!IsServer)
            return;

        NotifyHitShieldBrokenClientRpc();
    }

    [ClientRpc]
    private void SyncMonsterStatusVfxClientRpc(byte statusKind, bool active)
    {
        CwslMonsterStatusVfx.Ensure(gameObject)
            ?.SetStatusActive((CwslMonsterStatusKind)statusKind, active);
    }

    [ClientRpc]
    private void ClearMonsterStatusVfxClientRpc()
    {
        CwslMonsterStatusVfx.Ensure(gameObject)?.ClearAll();
    }

    [ClientRpc]
    private void NotifyMonsterStunClientRpc(Vector3 impactPosition, float durationSeconds)
    {
        CwslMonsterStunVisual.Ensure(gameObject).PlayStun(impactPosition, durationSeconds);
    }

    [ClientRpc]
    private void NotifyHitFlinchClientRpc(Vector3 worldDirection, float distance)
    {
        CwslMonsterHitFlinchVisual.Ensure(gameObject)?.PlayFlinch(worldDirection, distance);
    }

    [ClientRpc]
    private void NotifyHitShieldGrantedClientRpc()
    {
        CwslMonsterIronPotShieldVisual.Ensure(gameObject)?.ShowShield();
    }

    [ClientRpc]
    private void NotifyHitShieldBlockedClientRpc()
    {
        CwslMonsterIronPotShieldVisual.Ensure(gameObject)?.PulseBlocked();
    }

    [ClientRpc]
    private void NotifyHitShieldBrokenClientRpc()
    {
        CwslMonsterIronPotShieldVisual.Ensure(gameObject)?.HideShield();
        CwslSimpleVfx.SpawnBurst(GetDamagePopupAnchor(), new Color(0.75f, 0.8f, 0.9f), 0.9f, 0.22f);
    }

    [ClientRpc]
    private void PlayDeathClientRpc(Vector3 position, int monsterType)
    {
        CwslVfxSpawner.SpawnEnemyDeath(position, (CwslMonsterType)monsterType);
    }
}
