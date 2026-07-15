using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>런 카드 보유·패시브·액티브 슬롯.</summary>
public class StllPlayerCardInventory : NetworkBehaviour
{
    private readonly List<StllCardId> ownedCards = new();
    private readonly NetworkVariable<byte> activeSlot1 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<byte> activeSlot2 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> ironWallActive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<byte> pendingA = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<byte> pendingB = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<byte> pendingC = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> pickActive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private StllPlayerHealth health;
    private StllMinionSpawner minionSpawner;

    public IReadOnlyList<StllCardId> OwnedCards => ownedCards;
    public StllCardId ActiveSlot1 => (StllCardId)activeSlot1.Value;
    public StllCardId ActiveSlot2 => (StllCardId)activeSlot2.Value;
    public bool IsIronWallActive => ironWallActive.Value;
    public bool IsPickActive => pickActive.Value;
    public StllCardId PendingA => (StllCardId)pendingA.Value;
    public StllCardId PendingB => (StllCardId)pendingB.Value;
    public StllCardId PendingC => (StllCardId)pendingC.Value;

    private void Awake()
    {
        health = GetComponent<StllPlayerHealth>();
        minionSpawner = GetComponent<StllMinionSpawner>();
    }

    public void BeginPersonalPickServer(int pickIndex, StllBrotherhoodRole role, System.Random rng)
    {
        if (!IsServer)
            return;

        var guaranteed = StllCardCatalog.RollGuaranteedForRole(rng, role);
        pendingA.Value = (byte)guaranteed;
        pendingB.Value = (byte)StllCardCatalog.RollWeighted(rng, pickIndex);
        pendingC.Value = (byte)StllCardCatalog.RollWeighted(rng, pickIndex);
        pickActive.Value = true;
    }

    public bool TrySelectPendingCardServer(StllCardId cardId)
    {
        if (!IsServer || !pickActive.Value)
            return false;

        if (cardId != PendingA && cardId != PendingB && cardId != PendingC)
            return false;

        pickActive.Value = false;
        AddCardServer(cardId);
        return true;
    }

    public void AddCardServer(StllCardId cardId)
    {
        if (!IsServer || cardId == StllCardId.None)
            return;

        ownedCards.Add(cardId);
        var def = StllCardCatalog.Get(cardId);

        if (def.Kind == StllCardKind.Active)
        {
            if (activeSlot1.Value == 0)
                activeSlot1.Value = (byte)cardId;
            else if (activeSlot2.Value == 0)
                activeSlot2.Value = (byte)cardId;
        }

        if (def.Kind == StllCardKind.Military)
            ApplyMilitaryCardServer(cardId);

        health?.RecalculateMaxHealthServer();
    }

    private void ApplyMilitaryCardServer(StllCardId cardId)
    {
        if (minionSpawner == null)
            return;

        switch (cardId)
        {
            case StllCardId.MinionReinforce:
                minionSpawner.AddBonusMinionServer(1);
                break;
            case StllCardId.MinionTraining:
                minionSpawner.SetTrainingBonusServer(0.25f, 0.15f);
                break;
        }
    }

    public float GetPassiveBonus(StllPassiveBonusType type)
    {
        var total = 0f;
        for (var i = 0; i < ownedCards.Count; i++)
        {
            var card = ownedCards[i];
            switch (card)
            {
                case StllCardId.SharpBlade when type == StllPassiveBonusType.AttackDamage:
                    total += 0.12f;
                    break;
                case StllCardId.SwiftFeet when type == StllPassiveBonusType.MoveSpeed:
                    total += 0.10f;
                    break;
                case StllCardId.IronHeart when type == StllPassiveBonusType.MaxHealth:
                    total += 0.15f;
                    break;
                case StllCardId.Tenacity when type == StllPassiveBonusType.StaminaRegen:
                    total += 0.20f;
                    break;
                case StllCardId.CooldownInsight when type == StllPassiveBonusType.CooldownReduction:
                    total += 0.12f;
                    break;
                case StllCardId.VitalStrike when type == StllPassiveBonusType.CritChance:
                    total += 0.15f;
                    break;
                case StllCardId.Unyielding when type == StllPassiveBonusType.LowHpDamageReduction:
                    total += 0.20f;
                    break;
                case StllCardId.Unparalleled when type == StllPassiveBonusType.ExtraAttackChance:
                    total += 0.10f;
                    break;
            }
        }

        return total;
    }

    public void SetIronWallActiveServer(bool active)
    {
        if (!IsServer)
            return;

        ironWallActive.Value = active;
    }

    public bool HasCard(StllCardId cardId)
    {
        return ownedCards.Contains(cardId);
    }
}
