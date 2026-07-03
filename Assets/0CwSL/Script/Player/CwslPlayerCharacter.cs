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

    public event Action<CwslCharacterId> OnCharacterChanged;

    public override void OnNetworkSpawn()
    {
        syncedCharacterId.OnValueChanged += HandleCharacterChanged;
        if (IsServer)
            syncedCharacterId.Value = (int)CwslCharacterId.Tank;

        NotifyCharacterChanged(CharacterId);
    }

    public override void OnNetworkDespawn()
    {
        syncedCharacterId.OnValueChanged -= HandleCharacterChanged;
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
        if (!IsServer)
            return;

        if (!Enum.IsDefined(typeof(CwslCharacterId), characterId))
            return;

        var next = (CwslCharacterId)characterId;
        if (CharacterId == next)
            return;

        GetComponent<CwslPlayerSkills>()?.ReleaseSkillServer(OwnerClientId);
        syncedCharacterId.Value = characterId;
    }

    private void HandleCharacterChanged(int previous, int current)
    {
        NotifyCharacterChanged((CwslCharacterId)current);
    }

    private void NotifyCharacterChanged(CwslCharacterId characterId)
    {
        OnCharacterChanged?.Invoke(characterId);
    }
}
