using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 탱커 Q/Space 홀드 — 골드가 있을 때만 쉴드 이펙트·무적. 피격 시 골드 1 소모.
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

    private CwslPlayerGold playerGold;
    private CwslPlayerMovement movement;
    private CwslPlayerVisualScale visualScale;
    private CwslPlayerShieldFortifyVisual shieldFortifyVisual;
    private CwslPlayerPillBuff pillBuff;

    public bool IsFortifying => isFortifying.Value;
    public bool IsShieldActive => isShieldActive.Value;
    public float BlockRadius => IsShieldActive ? CwslGameConstants.FortifyShieldBlockRadius : 0f;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Charged;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.Tank;

    public override void OnNetworkSpawn()
    {
        playerGold = GetComponent<CwslPlayerGold>();
        movement = GetComponent<CwslPlayerMovement>();
        visualScale = GetComponent<CwslPlayerVisualScale>();
        shieldFortifyVisual = GetComponent<CwslPlayerShieldFortifyVisual>();
        pillBuff = GetComponent<CwslPlayerPillBuff>();
        if (GetComponent<CwslPlayerShieldBubble>() == null)
            gameObject.AddComponent<CwslPlayerShieldBubble>();

        if (playerGold != null)
            playerGold.OnGoldChanged += HandleGoldChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (playerGold != null)
            playerGold.OnGoldChanged -= HandleGoldChanged;
    }

    public override void OnSkillPressedServer(ulong senderClientId)
    {
        if (!IsServer)
            return;

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

        isFortifying.Value = false;
        SetShieldActiveServer(false);
        visualScale?.SetScaleServer(1f);
        if (movement != null)
            movement.SpeedMultiplier = 1f;
    }

    public override void TickChargedServer()
    {
        RefreshShieldState();
    }

    public bool TryBlockDamageServer()
    {
        if (!IsServer || !isShieldActive.Value || playerGold == null)
            return false;

        if (!CwslGameConstants.SkillsConsumeGold)
        {
            RefreshShieldState();
            return true;
        }

        if (pillBuff != null && pillBuff.TrySpendSkillGold(playerGold, CwslGameConstants.TankHitGoldCost))
        {
            RefreshShieldState();
            return true;
        }

        if (!playerGold.TrySpendGoldServer(CwslGameConstants.TankHitGoldCost))
        {
            RefreshShieldState();
            return false;
        }

        RefreshShieldState();
        return true;
    }

    private void HandleGoldChanged(int gold)
    {
        if (!IsServer)
            return;

        RefreshShieldState();
    }

    private void RefreshShieldState()
    {
        if (!IsServer)
            return;

        var shouldActivate = isFortifying.Value &&
                             playerGold != null &&
                             ( !CwslGameConstants.SkillsConsumeGold ||
                               playerGold.Gold >= CwslGameConstants.TankHitGoldCost);
        SetShieldActiveServer(shouldActivate);
    }

    private void SetShieldActiveServer(bool active)
    {
        if (isShieldActive.Value == active)
            return;

        isShieldActive.Value = active;
        shieldFortifyVisual?.SetFortifyServer(active);
    }
}
