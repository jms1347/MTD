using System;
using Unity.Netcode;
using UnityEngine;

public class CwslPlayerCharacter : NetworkBehaviour
{
    private readonly NetworkVariable<int> syncedCharacterId = new(
        (int)CwslCharacterId.Tank,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public CwslCharacterId CharacterId => (CwslCharacterId)syncedCharacterId.Value;

    public static event Action OnAnyCharacterChanged;

    public event Action<CwslCharacterId> OnCharacterChanged;

    public override void OnNetworkSpawn()
    {
        syncedCharacterId.OnValueChanged += HandleCharacterChanged;

        if (IsServer)
            ApplyServerCharacterAssignment();

        NotifyCharacterChanged(CharacterId);
    }

    public override void OnNetworkDespawn()
    {
        syncedCharacterId.OnValueChanged -= HandleCharacterChanged;

        if (IsServer)
            CwslGameSession.Instance?.ReleaseCharacter(OwnerClientId);
    }

    internal void ApplyAssignedCharacterServer(CwslCharacterId characterId)
    {
        if (!IsServer || !Enum.IsDefined(typeof(CwslCharacterId), characterId))
            return;

        syncedCharacterId.Value = (int)characterId;
    }

    public void CheatCycleCharacterServer()
    {
        if (!IsServer || !CwslLobbyGameSettings.EnableDevCheats)
            return;

        var session = CwslGameSession.Instance;
        if (session == null)
            return;

        var all = CwslCharacterCatalog.All;
        if (all.Count == 0)
            return;

        var currentIndex = 0;
        for (var i = 0; i < all.Count; i++)
        {
            if (all[i].Id != CharacterId)
                continue;

            currentIndex = i;
            break;
        }

        var nextId = all[(currentIndex + 1) % all.Count].Id;
        session.CheatAssignCharacterServer(OwnerClientId, nextId);
    }

    public void RequestSelect(CwslCharacterId characterId)
    {
        if (!IsOwner)
            return;

        RequestCharacterServerRpc((int)characterId);
    }

    [ServerRpc]
    private void RequestCharacterServerRpc(int characterId)
    {
        // 본게임에서는 입장 시 배정된 캐릭터 고정 (C키/패널 변경 없음)
    }

    private void ApplyServerCharacterAssignment()
    {
        var session = CwslGameSession.Instance;
        if (session == null)
        {
            Debug.LogWarning("[CwSL] CwslGameSession 없음 — Tank로 폴백");
            syncedCharacterId.Value = (int)CwslCharacterId.Tank;
            return;
        }

        session.EnsureCharacterAssigned(OwnerClientId);

        if (session.TryGetAssignedCharacter(OwnerClientId, out var assigned))
            syncedCharacterId.Value = (int)assigned;
        else
            Debug.LogWarning($"[CwSL] 캐릭터 배정 실패 client={OwnerClientId}");
    }

    private void HandleCharacterChanged(int previous, int current)
    {
        NotifyCharacterChanged((CwslCharacterId)current);
    }

    private void NotifyCharacterChanged(CwslCharacterId characterId)
    {
        OnCharacterChanged?.Invoke(characterId);
        OnAnyCharacterChanged?.Invoke();
    }
}
