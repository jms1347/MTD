using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CwslPlayerPillBuff : NetworkBehaviour
{
    private readonly NetworkVariable<float> speedBuffUntil = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> freeSkillUntil = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> yellowHealUntil = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslPlayerHealth playerHealth;
    private GameObject speedAuraInstance;
    private GameObject freeSkillAuraInstance;
    private GameObject yellowHealAuraInstance;
    private Coroutine yellowHealRoutine;

    public bool HasFreeSkillBuff => freeSkillUntil.Value > Time.time;
    public bool HasSpeedBuff => speedBuffUntil.Value > Time.time;
    public bool HasYellowHealBuff => yellowHealUntil.Value > Time.time;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        speedBuffUntil.OnValueChanged += HandleSpeedBuffChanged;
        freeSkillUntil.OnValueChanged += HandleFreeSkillBuffChanged;
        yellowHealUntil.OnValueChanged += HandleYellowHealChanged;
        RefreshAuraVisuals();
    }

    public override void OnNetworkDespawn()
    {
        speedBuffUntil.OnValueChanged -= HandleSpeedBuffChanged;
        freeSkillUntil.OnValueChanged -= HandleFreeSkillBuffChanged;
        yellowHealUntil.OnValueChanged -= HandleYellowHealChanged;
        StopYellowHealServer();
        ClearAura(ref speedAuraInstance);
        ClearAura(ref freeSkillAuraInstance);
        ClearAura(ref yellowHealAuraInstance);
    }

    public void ApplyPillServer(CwslPillType pillType)
    {
        if (!IsServer)
            return;

        switch (pillType)
        {
            case CwslPillType.Green:
                var maxHealth = playerHealth != null ? playerHealth.MaxHealth : CwslGameConstants.PlayerMaxHealth;
                var healAmount = maxHealth * CwslGameConstants.PillGreenHealRatio;
                playerHealth?.TryHealServer(healAmount);
                break;

            case CwslPillType.Blue:
                speedBuffUntil.Value = Time.time + CwslGameConstants.PillBuffDurationSeconds;
                CwslMoveSpeedBuff.Ensure(this)?.ApplyBuff(
                    CwslGameConstants.PillBlueSpeedMultiplier,
                    CwslGameConstants.PillBuffDurationSeconds);
                break;

            case CwslPillType.Yellow:
                StartYellowHealServer();
                break;
        }

        PlayPillBurstClientRpc(pillType);
    }

    public bool TrySpendSkillGold(CwslPlayerGold playerGold, int amount, bool playSpendEffect = true)
    {
        if (!IsServer || amount <= 0)
            return false;

        if (HasFreeSkillBuff)
            return true;

        return playerGold != null && playerGold.TrySpendGoldServer(amount, playSpendEffect);
    }

    public bool CanAffordSkillGold(CwslPlayerGold playerGold, int amount)
    {
        if (HasFreeSkillBuff)
            return true;

        return playerGold != null && playerGold.Gold >= amount;
    }

    private void StartYellowHealServer()
    {
        if (playerHealth == null || !playerHealth.IsAlive)
            return;

        var maxHealth = playerHealth != null ? playerHealth.MaxHealth : CwslGameConstants.PlayerMaxHealth;
        var missingHealth = Mathf.Max(0f, maxHealth - playerHealth.CurrentHealth);
        if (missingHealth <= 0f)
            return;

        StopYellowHealServer();

        var duration = CwslGameConstants.PillYellowHealDurationSeconds;
        yellowHealUntil.Value = Time.time + duration;
        yellowHealRoutine = StartCoroutine(YellowHealRoutine(missingHealth, duration));
    }

    private void StopYellowHealServer()
    {
        if (yellowHealRoutine != null)
        {
            StopCoroutine(yellowHealRoutine);
            yellowHealRoutine = null;
        }

        if (IsServer)
            yellowHealUntil.Value = 0f;
    }

    private IEnumerator YellowHealRoutine(float totalToHeal, float duration)
    {
        var elapsed = 0f;
        var healedSoFar = 0f;

        while (elapsed < duration)
        {
            if (playerHealth == null || !playerHealth.IsAlive)
                break;

            yield return null;
            elapsed += Time.deltaTime;

            var targetHealed = totalToHeal * Mathf.Clamp01(elapsed / duration);
            var tickHeal = targetHealed - healedSoFar;
            if (tickHeal > 0.001f)
            {
                playerHealth.TryHealServer(tickHeal, showPopup: false);
                healedSoFar = targetHealed;
            }
        }

        if (playerHealth != null && playerHealth.IsAlive)
        {
            var remainder = totalToHeal - healedSoFar;
            if (remainder > 0.001f)
                playerHealth.TryHealServer(remainder);
        }

        if (IsServer)
            yellowHealUntil.Value = 0f;

        yellowHealRoutine = null;
    }

    private void Update()
    {
        RefreshAuraVisuals();
    }

    private void HandleSpeedBuffChanged(float previous, float current) => RefreshAuraVisuals();

    private void HandleFreeSkillBuffChanged(float previous, float current) => RefreshAuraVisuals();

    private void HandleYellowHealChanged(float previous, float current) => RefreshAuraVisuals();

    private void RefreshAuraVisuals()
    {
        if (HasSpeedBuff)
        {
            if (speedAuraInstance == null)
                speedAuraInstance = CwslVfxSpawner.AttachPillSpeedAura(ResolveAuraAnchor());
        }
        else
        {
            ClearAura(ref speedAuraInstance);
        }

        if (HasFreeSkillBuff)
        {
            if (freeSkillAuraInstance == null)
                freeSkillAuraInstance = CwslVfxSpawner.AttachPillFreeSkillAura(ResolveAuraAnchor());
        }
        else
        {
            ClearAura(ref freeSkillAuraInstance);
        }

        if (HasYellowHealBuff)
        {
            if (yellowHealAuraInstance == null)
                yellowHealAuraInstance = CwslVfxSpawner.AttachPillHealAura(ResolveAuraAnchor());
        }
        else
        {
            ClearAura(ref yellowHealAuraInstance);
        }
    }

    private Transform ResolveAuraAnchor()
    {
        var visual = transform.Find("Visual");
        return visual != null ? visual : transform;
    }

    [ClientRpc]
    private void PlayPillBurstClientRpc(CwslPillType pillType)
    {
        CwslVfxSpawner.SpawnPillBurst(transform.position + Vector3.up * 0.9f, pillType);
    }

    private static void ClearAura(ref GameObject auraInstance)
    {
        if (auraInstance == null)
            return;

        Object.Destroy(auraInstance);
        auraInstance = null;
    }
}
