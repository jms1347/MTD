using Unity.Netcode;
using UnityEngine;

public class CwslPlayerHealth : NetworkBehaviour
{
    private readonly NetworkVariable<float> health = new(
        CwslGameConstants.PlayerMaxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> isDead = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslTankFortifySkill fortifySkill;
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerSkills playerSkills;
    private CwslPlayerMovement movement;
    private CwslMomentumRammerSkill momentumRammerSkill;
    private CwslPlayerGold playerGold;
    private CwslPlayerGrave playerGrave;
    private CwslPlayerVisualScale visualScale;

    public bool IsDead => isDead.Value;
    public bool IsAlive => !isDead.Value && health.Value > 0f;
    public float CurrentHealth => health.Value;
    public float MaxHealth => GetMaxHealthForCharacter();

    public event System.Action<float> OnHealthChanged;
    public event System.Action OnDied;
    public event System.Action OnRevived;

    public override void OnNetworkSpawn()
    {
        fortifySkill = GetComponent<CwslTankFortifySkill>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        playerSkills = GetComponent<CwslPlayerSkills>();
        movement = GetComponent<CwslPlayerMovement>();
        momentumRammerSkill = GetComponent<CwslMomentumRammerSkill>();
        playerGold = GetComponent<CwslPlayerGold>();
        playerGrave = GetComponent<CwslPlayerGrave>();
        visualScale = GetComponent<CwslPlayerVisualScale>();
        health.OnValueChanged += HandleHealthChanged;
        isDead.OnValueChanged += HandleDeadStateChanged;

        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged += HandleCharacterChanged;

        if (IsServer)
            ResetHealthServer();

        ApplyAliveVisual(!isDead.Value);
        CwslCombatRegistry.RegisterPlayer(this);
    }

    public override void OnNetworkDespawn()
    {
        CwslCombatRegistry.UnregisterPlayer(this);
        health.OnValueChanged -= HandleHealthChanged;
        isDead.OnValueChanged -= HandleDeadStateChanged;

        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged -= HandleCharacterChanged;
    }

    private void HandleCharacterChanged(CwslCharacterId characterId)
    {
        if (!IsServer || isDead.Value)
            return;

        ResetHealthServer();
    }

    private void ResetHealthServer()
    {
        isDead.Value = false;
        health.Value = GetMaxHealthForCharacter();
    }

    private float GetMaxHealthForCharacter()
    {
        var characterId = playerCharacter != null
            ? playerCharacter.CharacterId
            : CwslCharacterId.Tank;
        return CwslCharacterStatCatalog.GetMaxHealth(characterId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DamageServerRpc(float amount, int popupKind = (int)CwslDamagePopupKind.Player)
    {
        ApplyDamageServer(amount, (CwslDamagePopupKind)popupKind, GetDamagePopupAnchor());
    }

    public bool TryReceiveProjectileHitServer(float damage, Vector3 hitPosition)
    {
        if (!IsServer || !IsAlive)
            return true;

        if (CwslShieldBubbleProtection.IsPlayerProtectedServer(this))
            return true;

        return ApplyDamageServer(damage, CwslDamagePopupKind.Projectile, hitPosition);
    }

    /// <summary>
    /// (비활성) 과거 방패 투사체 차단용. 방패 강화는 방어력 배율만 적용합니다.
    /// </summary>
    public bool TryInterceptProjectileServer(Vector3 projectilePosition, float damage)
    {
        return false;
    }

    // TODO(릴리즈): R키 치트용 — 로비 설정으로 비활성 가능
    public void CheatReviveServer()
    {
        if (!IsServer || !CwslLobbyGameSettings.EnableDevCheats)
            return;

        if (isDead.Value)
        {
            playerGrave?.ForceEndTombstoneServer();
            ReviveServer(CwslGameConstants.StartingGold);
            return;
        }

        health.Value = GetMaxHealthForCharacter();
        playerGold?.SetGoldServer(Mathf.Max(playerGold.Gold, CwslGameConstants.StartingGold));
        GetComponent<CwslPlayerStamina>()?.RestoreFullServer();
    }

    public bool TryReceiveMeleeHitServer(float damage, Vector3 hitPosition)
    {
        if (!IsServer || !IsAlive)
            return true;

        return ApplyDamageServer(damage, CwslDamagePopupKind.Player, hitPosition);
    }

    public bool TryReceiveExplosionHitServer(float damage, Vector3 hitPosition)
    {
        if (!IsServer || !IsAlive)
            return true;

        return ApplyDamageServer(damage, CwslDamagePopupKind.Player, hitPosition);
    }

    public void TryReceiveEnvironmentHitServer(float damage, Vector3 hitPosition)
    {
        if (!IsServer || !IsAlive || damage <= 0f)
            return;

        ApplyDamageServer(damage, CwslDamagePopupKind.Poison, hitPosition);
    }

    public void TryHealServer(float amount, bool showPopup = true)
    {
        if (!IsServer || !IsAlive || amount <= 0f)
            return;

        var maxHealth = GetMaxHealthForCharacter();
        if (health.Value >= maxHealth)
            return;

        var healed = Mathf.Min(amount, maxHealth - health.Value);
        if (healed <= 0f)
            return;

        health.Value += healed;
        if (showPopup)
            CwslDamageFeedback.PlayFromServer(GetDamagePopupAnchor(), healed, CwslDamagePopupKind.Heal);
    }

    private bool ApplyDamageServer(float amount, CwslDamagePopupKind popupKind, Vector3 feedbackPosition)
    {
        if (!IsServer || !IsAlive || amount <= 0f)
            return true;

        if (CwslShieldBubbleProtection.IsPlayerProtectedServer(this))
            return true;

        var defense = playerCharacter != null
            ? CwslCharacterStatCatalog.GetDefense(playerCharacter.CharacterId)
            : CwslGameConstants.PlayerDefense;

        if (fortifySkill != null)
            defense *= fortifySkill.GetDefenseMultiplier();

        var finalAmount = CwslCombatMath.ResolveDamageAfterDefense(amount, defense);
        CwslDamageFeedback.PlayFromServer(ResolvePopupAnchor(popupKind, feedbackPosition), finalAmount, popupKind);
        health.Value = Mathf.Max(0f, health.Value - finalAmount);
        if (health.Value <= 0f)
            DieServer();
        return true;
    }

    internal void ReviveServer(int restoredGold)
    {
        if (!IsServer || !isDead.Value)
            return;

        isDead.Value = false;
        health.Value = GetMaxHealthForCharacter();
        playerGold?.SetGoldServer(restoredGold);
        GetComponent<CwslPlayerStamina>()?.RestoreFullServer();
        visualScale?.SetScaleServer(1f);
        movement?.SetAgentEnabled(true);
        OnRevived?.Invoke();
        PlayReviveClientRpc();
        CwslGameFlow.Instance?.NotifyPlayerStateChangedServer();
    }

    private void DieServer()
    {
        if (isDead.Value)
            return;

        isDead.Value = true;
        var goldAtDeath = playerGold != null ? playerGold.Gold : 0;

        playerSkills?.ReleaseSkillServer(OwnerClientId);
        momentumRammerSkill?.StopOnDeathServer();
        visualScale?.SetScaleServer(1f);
        movement?.SetAgentEnabled(false);
        playerGold?.SetGoldServer(0);

        OnDied?.Invoke();
        PlayDeathClientRpc(transform.position);
        playerGrave?.BeginTombstoneServer(goldAtDeath);
        CwslGameFlow.Instance?.NotifyPlayerStateChangedServer();
    }

    [ClientRpc]
    private void PlayDeathClientRpc(Vector3 position)
    {
        CwslVfxSpawner.SpawnPlayerDeath(position);
        ApplyAliveVisual(false);
    }

    [ClientRpc]
    private void PlayReviveClientRpc()
    {
        ApplyAliveVisual(true);
    }

    private Vector3 ResolvePopupAnchor(CwslDamagePopupKind popupKind, Vector3 feedbackPosition)
    {
        if (popupKind == CwslDamagePopupKind.Projectile)
            return feedbackPosition + Vector3.up * 0.35f;

        return GetDamagePopupAnchor();
    }

    private Vector3 GetDamagePopupAnchor()
    {
        return transform.position + Vector3.up * 2.2f;
    }

    private void HandleHealthChanged(float previous, float current)
    {
        OnHealthChanged?.Invoke(current);
    }

    private void HandleDeadStateChanged(bool previous, bool current)
    {
        ApplyAliveVisual(!current);
    }

    private void ApplyAliveVisual(bool alive)
    {
        var visual = transform.Find("Visual");
        if (visual != null)
            visual.gameObject.SetActive(alive);
    }
}
