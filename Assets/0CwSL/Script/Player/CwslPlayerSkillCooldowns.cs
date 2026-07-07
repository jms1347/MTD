using Unity.Netcode;
using UnityEngine;

/// <summary>Q/W/E/R 슬롯별 스킬 쿨타임 (시전 시간 × 2).</summary>
public class CwslPlayerSkillCooldowns : NetworkBehaviour
{
    private readonly NetworkVariable<float> slot0Remaining = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> slot1Remaining = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> slot2Remaining = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> slot3Remaining = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> slot0Total = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> slot1Total = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> slot2Total = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> slot3Total = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslPlayerCharacter playerCharacter;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        TickSlot(0, slot0Remaining);
        TickSlot(1, slot1Remaining);
        TickSlot(2, slot2Remaining);
        TickSlot(3, slot3Remaining);
    }

    public float GetRemaining(int slotIndex)
    {
        return slotIndex switch
        {
            0 => slot0Remaining.Value,
            1 => slot1Remaining.Value,
            2 => slot2Remaining.Value,
            3 => slot3Remaining.Value,
            _ => 0f,
        };
    }

    public float GetTotal(int slotIndex)
    {
        return slotIndex switch
        {
            0 => slot0Total.Value,
            1 => slot1Total.Value,
            2 => slot2Total.Value,
            3 => slot3Total.Value,
            _ => 1f,
        };
    }

    public float GetFillAmount(int slotIndex)
    {
        var total = GetTotal(slotIndex);
        if (total <= 0.001f)
            return 0f;

        return Mathf.Clamp01(GetRemaining(slotIndex) / total);
    }

    public bool IsReady(int slotIndex) => GetRemaining(slotIndex) <= 0.001f;

    public void BeginCooldown(int slotIndex)
    {
        if (!IsServer)
            return;

        var characterId = playerCharacter != null
            ? playerCharacter.CharacterId
            : CwslCharacterId.Tank;
        BeginCooldown(slotIndex, CwslCharacterSkillCatalog.GetCastDuration(characterId, slotIndex));
    }

    public void BeginCooldown(int slotIndex, float castDuration)
    {
        if (!IsServer)
            return;

        var total = Mathf.Max(0.05f, castDuration * CwslGameConstants.SkillCooldownMultiplier);
        SetRemaining(slotIndex, total);
        SetTotal(slotIndex, total);
    }

    private static void TickSlot(int slotIndex, NetworkVariable<float> remaining)
    {
        if (remaining.Value <= 0f)
            return;

        remaining.Value = Mathf.Max(0f, remaining.Value - Time.deltaTime);
    }

    private void SetRemaining(int slotIndex, float value)
    {
        switch (slotIndex)
        {
            case 0: slot0Remaining.Value = value; break;
            case 1: slot1Remaining.Value = value; break;
            case 2: slot2Remaining.Value = value; break;
            case 3: slot3Remaining.Value = value; break;
        }
    }

    private void SetTotal(int slotIndex, float value)
    {
        switch (slotIndex)
        {
            case 0: slot0Total.Value = value; break;
            case 1: slot1Total.Value = value; break;
            case 2: slot2Total.Value = value; break;
            case 3: slot3Total.Value = value; break;
        }
    }
}
