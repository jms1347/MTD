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
    private CwslPlayerGold playerGold;
    private CwslPlayerGrave playerGrave;
    private CwslPlayerVisualScale visualScale;

    public bool IsDead => isDead.Value;
    public bool IsAlive => !isDead.Value && health.Value > 0f;
    public float CurrentHealth => health.Value;

    public event System.Action<float> OnHealthChanged;
    public event System.Action OnDied;
    public event System.Action OnRevived;

    public override void OnNetworkSpawn()
    {
        fortifySkill = GetComponent<CwslTankFortifySkill>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        playerSkills = GetComponent<CwslPlayerSkills>();
        movement = GetComponent<CwslPlayerMovement>();
        playerGold = GetComponent<CwslPlayerGold>();
        playerGrave = GetComponent<CwslPlayerGrave>();
        visualScale = GetComponent<CwslPlayerVisualScale>();
        health.OnValueChanged += HandleHealthChanged;
        isDead.OnValueChanged += HandleDeadStateChanged;

        if (IsServer)
        {
            health.Value = CwslGameConstants.PlayerMaxHealth;
            isDead.Value = false;
        }

        ApplyAliveVisual(!isDead.Value);
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= HandleHealthChanged;
        isDead.OnValueChanged -= HandleDeadStateChanged;
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

        return ApplyDamageServer(damage, CwslDamagePopupKind.Projectile, hitPosition);
    }

    /// <summary>
    /// 방패 반경/콜라이더로 미사일을 가로챕니다. true면 미사일을 소멸시켜야 합니다.
    /// </summary>
    public bool TryInterceptProjectileServer(Vector3 projectilePosition, float damage)
    {
        if (!IsServer || !IsAlive)
            return false;

        if (!CanBlockNow())
            return false;

        var blockRadius = fortifySkill.BlockRadius;
        if (blockRadius <= 0f)
            return false;

        var toProjectile = projectilePosition - (transform.position + Vector3.up * 1f);
        if (toProjectile.sqrMagnitude > blockRadius * blockRadius)
            return false;

        return TryBlockHitServer(projectilePosition, damage);
    }

    public bool TryBlockHitServer(Vector3 hitPosition, float damage)
    {
        if (!IsServer || !IsAlive || !CanBlockNow())
            return false;

        if (!fortifySkill.TryBlockDamageServer())
            return false;

        PlayBlockFeedbackClientRpc(hitPosition, damage);
        return true;
    }

    public void CheatReviveServer()
    {
        if (!IsServer)
            return;

        if (isDead.Value)
        {
            playerGrave?.ForceEndTombstoneServer();
            ReviveServer(CwslGameConstants.StartingGold);
            return;
        }

        health.Value = CwslGameConstants.PlayerMaxHealth;
        playerGold?.SetGoldServer(Mathf.Max(playerGold.Gold, CwslGameConstants.StartingGold));
    }

    private bool CanBlockNow()
    {
        return fortifySkill != null &&
               playerCharacter != null &&
               playerCharacter.CharacterId == CwslCharacterId.Tank &&
               fortifySkill.IsShieldActive;
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

    private bool ApplyDamageServer(float amount, CwslDamagePopupKind popupKind, Vector3 feedbackPosition)
    {
        if (!IsServer || !IsAlive || amount <= 0f)
            return true;

        if (CanBlockNow() && fortifySkill.TryBlockDamageServer())
        {
            PlayBlockFeedbackClientRpc(feedbackPosition, amount);
            return true;
        }

        ShowDamagePopupClientRpc(ResolvePopupAnchor(popupKind, feedbackPosition), amount, (int)popupKind);
        health.Value = Mathf.Max(0f, health.Value - amount);
        if (health.Value <= 0f)
            DieServer();
        return true;
    }

    [ClientRpc]
    private void PlayBlockFeedbackClientRpc(Vector3 position, float amount)
    {
        var hitPoint = position + Vector3.up * 0.4f;
        CwslDamagePopupPool.Play(hitPoint, amount, CwslDamagePopupKind.Blocked);
        CwslVfxSpawner.SpawnFortifyBlock(position + Vector3.up * 0.35f);
        // 골드 소모 이펙트는 CwslPlayerGold.TrySpendGoldServer 에서 처리
    }

    internal void ReviveServer(int restoredGold)
    {
        if (!IsServer || !isDead.Value)
            return;

        isDead.Value = false;
        health.Value = CwslGameConstants.PlayerMaxHealth;
        playerGold?.SetGoldServer(restoredGold);
        visualScale?.SetScaleServer(1f);
        movement?.SetAgentEnabled(true);
        OnRevived?.Invoke();
        PlayReviveClientRpc();
    }

    private void DieServer()
    {
        if (isDead.Value)
            return;

        isDead.Value = true;
        var goldAtDeath = playerGold != null ? playerGold.Gold : 0;

        playerSkills?.ReleaseSkillServer(OwnerClientId);
        visualScale?.SetScaleServer(1f);
        movement?.SetAgentEnabled(false);
        playerGold?.SetGoldServer(0);

        OnDied?.Invoke();
        PlayDeathClientRpc(transform.position);
        playerGrave?.BeginTombstoneServer(goldAtDeath);
    }

    [ClientRpc]
    private void ShowDamagePopupClientRpc(Vector3 position, float amount, int kind)
    {
        CwslDamagePopupPool.Play(position, amount, (CwslDamagePopupKind)kind);
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
