using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 탱커 Q/Space 홀드 — 방패를 펼쳐 방어력 2배. SP 4/초 유지 소모. E·R·W 강화.
/// </summary>
public class CwslTankFortifySkill : CwslPlayerSkillBase
{
    private const float FortifySpeedMultiplier = 0.2f;

    private readonly NetworkVariable<bool> isFortifying = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> isShieldActive = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslPlayerMovement movement;
    private CwslPlayerVisualScale visualScale;
    private CwslPlayerShieldFortifyVisual shieldFortifyVisual;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private CwslPlayerStamina playerStamina;
    private CwslPlayerSkills playerSkills;

    public bool IsFortifying => isFortifying.Value;
    public bool IsShieldActive => isShieldActive.Value;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Charged;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.Tank;

    public override void OnNetworkSpawn()
    {
        movement = GetComponent<CwslPlayerMovement>();
        visualScale = GetComponent<CwslPlayerVisualScale>();
        shieldFortifyVisual = GetComponent<CwslPlayerShieldFortifyVisual>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        playerStamina = GetComponent<CwslPlayerStamina>();
        playerSkills = GetComponent<CwslPlayerSkills>();
    }

    public override void OnSkillPressedServer(ulong senderClientId)
    {
        if (!IsServer)
            return;

        if (skillCooldowns != null && !skillCooldowns.IsReady(0))
            return;

        if (playerStamina == null)
            playerStamina = GetComponent<CwslPlayerStamina>();

        if (playerStamina != null && playerStamina.Current <= 0f)
        {
            playerSkills?.NotifyStaminaInsufficientServer();
            return;
        }

        isFortifying.Value = true;
        visualScale?.SetScaleServer(CwslGameConstants.FortifyBodyScale);
        if (movement != null)
            movement.SpeedMultiplier = FortifySpeedMultiplier;
        RefreshShieldState();
    }

    public override void OnSkillReleasedServer(ulong senderClientId)
    {
        if (!IsServer)
            return;

        EndFortifyServer(beginCooldown: true);
    }

    public override void TickChargedServer()
    {
        if (!IsServer || !isFortifying.Value)
            return;

        if (playerStamina == null)
            playerStamina = GetComponent<CwslPlayerStamina>();

        var drain = CwslGameConstants.TankFortifyStaminaDrainPerSecond * Time.deltaTime;
        if (drain > 0f &&
            CwslGameConstants.SkillsUseStamina &&
            playerStamina != null &&
            !playerStamina.TrySpendServer(drain))
        {
            EndFortifyServer(beginCooldown: true);
            return;
        }

        RefreshShieldState();
    }

    public float GetDefenseMultiplier()
    {
        return IsShieldActive ? CwslGameConstants.TankFortifyDefenseMultiplier : 1f;
    }

    private void RefreshShieldState()
    {
        if (!IsServer)
            return;

        var shouldActivate = isFortifying.Value &&
                             (!CwslGameConstants.SkillsUseStamina ||
                              playerStamina == null ||
                              playerStamina.Current > 0f);
        SetShieldActiveServer(shouldActivate);
    }

    private void EndFortifyServer(bool beginCooldown)
    {
        if (!isFortifying.Value && !isShieldActive.Value)
            return;

        isFortifying.Value = false;
        SetShieldActiveServer(false);
        visualScale?.SetScaleServer(1f);
        if (movement != null)
            movement.SpeedMultiplier = 1f;

        if (beginCooldown)
            skillCooldowns?.BeginCooldown(0);
    }

    private void SetShieldActiveServer(bool active)
    {
        if (isShieldActive.Value == active)
            return;

        var wasActive = isShieldActive.Value;
        isShieldActive.Value = active;
        shieldFortifyVisual?.SetFortifyServer(active);

        if (!wasActive && active)
            PlayFortifyQActivatedClientRpc();
    }

    [ClientRpc]
    private void PlayFortifyQActivatedClientRpc()
    {
        CwslSkillAudioFeedback.PlayTankShieldFortifyQ(transform.position);
    }
}
